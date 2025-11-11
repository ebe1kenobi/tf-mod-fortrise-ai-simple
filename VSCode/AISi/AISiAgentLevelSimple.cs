using System;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TFModFortRiseLoaderAI;
using TowerFall;
using static TowerFall.Player;

namespace TFModFortRiseAiSimple
{
  /// <summary>
  /// Agent basé sur une navigation par graphe généré dynamiquement à partir de la physique du joueur.
  /// Chaque cellule marcheable devient un nœud, et les arêtes (marcher, tomber, sauter, dash…) sont validées
  /// par une simulation simplifiée de la physique réelle de TowerFall.
  /// </summary>
  public class AISiAgentLevelSimple : TFModFortRiseLoaderAI.Agent
  {
    // --- Constantes liées au niveau ---
    private const int LEVEL_WIDTH = 32;
    private const int LEVEL_HEIGHT = 24;
    private const int BLOCK_SIZE = 10;

    // --- Constantes de mouvement (extraits de Player.cs) ---
    private const float GRAVITY = 0.3f;
    private const float MAX_FALL = 2.8f;
    private const float JUMP_SPEED = -3.2f;
    private const float DODGE_SPEED = 5.5f;
    private const float AIR_ACCEL = 0.1f;
    private const float TARGET_RUN_SPEED = 1.2f;

    private const int MAX_JUMP_HORIZONTAL_CELLS = 12;
    private const int MAX_JUMP_UP_CELLS = 8;
    private const int MAX_DROP_CELLS = 12;

    private const float PATH_RECALC_INTERVAL = 0.40f;
    private const float HOLD_JUMP_DURATION = 0.18f;
    private const float EARLY_DASH_TIME = 0.14f;
    private const float LATE_DASH_TIME = 0.26f;
    private const float SECOND_DASH_TIME = 0.36f;

    // Grille du niveau et liste des cellules marcheables
    private int[,] levelGrid;
    private readonly List<Point> walkableCells = new List<Point>();

    // Informations joueur / ennemi
    private Player player;
    private Player enemy;
    private readonly PlayerInfo playerInfo = new PlayerInfo();
    private readonly PlayerInfo enemyInfo = new PlayerInfo();

    // Plan en cours
    private readonly Queue<PlannedEdge> plannedEdges = new Queue<PlannedEdge>();
    private PlannedEdge? currentEdge = null;
    private bool jumpTriggered = false;
    private bool dashTriggered = false;
    private bool secondDashTriggered = false;
    private float edgeTimer = 0f;

    private Point lastPathStart = new Point(-1, -1);
    private Point lastPathGoal = new Point(-1, -1);
    private float pathRecalcTimer = 0f;

    // Structures de navigation
    private enum EdgeType
    {
      Walk,
      Drop,
      Jump,
      JumpDashEarly,
      JumpDashLate,
      JumpDoubleDash
    }

    private struct PlannedEdge
    {
      public Point Source;
      public Point Target;
      public EdgeType Type;
      public int DirectionX;
    }

    public AISiAgentLevelSimple(int index, string type, PlayerInput input) : base(index, type, input)
    {
    }

    public override void Reset()
    {
      UpdateLevelGrid();
      RebuildWalkableCells();
      plannedEdges.Clear();
      currentEdge = null;
      lastPathStart = new Point(-1, -1);
      lastPathGoal = new Point(-1, -1);
      pathRecalcTimer = 0f;
    }

    public override void Move()
    {
      ResetInputs();
      UpdatePerception();

      if (player == null || player.Dead)
        return;

      if (enemy == null || enemy.Dead)
        return;

      Point playerCell = FindClosestWalkableCell(new Point(playerInfo.X, playerInfo.Y));
      Point enemyCell = FindClosestWalkableCell(new Point(enemyInfo.X, enemyInfo.Y));

      bool validPlayerCell = IsValidCell(playerCell);
      bool validEnemyCell = IsValidCell(enemyCell);

      pathRecalcTimer += Engine.DeltaTime;
      bool needRecalc = pathRecalcTimer >= PATH_RECALC_INTERVAL ||
                        plannedEdges.Count == 0 ||
                        !playerCell.Equals(lastPathStart) ||
                        !enemyCell.Equals(lastPathGoal);

      if (needRecalc && validPlayerCell && validEnemyCell)
      {
        pathRecalcTimer = 0f;
        var newPath = FindPath(playerCell, enemyCell);
        plannedEdges.Clear();
        currentEdge = null;
        if (newPath != null && newPath.Count > 0)
        {
          foreach (var edge in newPath)
            plannedEdges.Enqueue(edge);
          lastPathStart = playerCell;
          lastPathGoal = enemyCell;
        }
        else
        {
          lastPathStart = new Point(-1, -1);
          lastPathGoal = new Point(-1, -1);
        }
      }

      if (plannedEdges.Count == 0 && currentEdge == null)
      {
        ExecuteFallback();
        return;
      }

      ExecutePath(playerCell);
    }

    #region Perception et niveau

    private void UpdatePerception()
    {
      UpdateLevelGrid();
      RebuildWalkableCells();

      player = level.GetPlayer(index);
      int enemyIndex = index == 0 ? 1 : 0;
      enemy = level.GetPlayer(enemyIndex);

      if (player != null)
        UpdatePlayerInfo(player, playerInfo);

      if (enemy != null)
        UpdatePlayerInfo(enemy, enemyInfo);
    }

    private void UpdatePlayerInfo(Player target, PlayerInfo info)
    {
      var data = DynamicData.For(target);
      Point cell = WorldToCell(target.Position);
      info.X = cell.X;
      info.Y = cell.Y;
      info.onGround = data.Get<bool>("OnGround");
      info.GrabEdge = data.Get<PlayerStates>("State") == PlayerStates.LedgeGrab;
      info.Speed = target.Speed;
      info.CanWallJump = data.Invoke<bool>("CanWallJump", Facing.Left) || data.Invoke<bool>("CanWallJump", Facing.Right);
      data.Dispose();
    }

    private void UpdateLevelGrid()
    {
      if (levelGrid == null || levelGrid.GetLength(0) != LEVEL_HEIGHT || levelGrid.GetLength(1) != LEVEL_WIDTH)
        levelGrid = new int[LEVEL_HEIGHT, LEVEL_WIDTH];

      for (int y = 0; y < LEVEL_HEIGHT; y++)
      {
        for (int x = 0; x < LEVEL_WIDTH; x++)
        {
          float worldX = x * BLOCK_SIZE + (BLOCK_SIZE * 0.5f);
          float worldY = y * BLOCK_SIZE + (BLOCK_SIZE * 0.5f);
          levelGrid[y, x] = level.CollideCheck(new Vector2(worldX, worldY), GameTags.Solid) ? 1 : 0;
        }
      }
    }

    private void RebuildWalkableCells()
    {
      walkableCells.Clear();
      for (int y = 0; y < LEVEL_HEIGHT; y++)
      {
        for (int x = 0; x < LEVEL_WIDTH; x++)
        {
          if (IsWalkableCell(x, y))
            walkableCells.Add(new Point(x, y));
        }
      }
    }

    #endregion

    #region Navigation et planification

    private List<PlannedEdge> FindPath(Point start, Point goal)
    {
      if (start.Equals(goal))
        return new List<PlannedEdge>();

      var queue = new Queue<Point>();
      var visited = new HashSet<Point>();
      var parent = new Dictionary<Point, Point>();
      var edgeForNode = new Dictionary<Point, PlannedEdge>();

      queue.Enqueue(start);
      visited.Add(start);

      int safety = 0;
      const int MAX_ITER = 4000;

      while (queue.Count > 0 && safety++ < MAX_ITER)
      {
        Point current = queue.Dequeue();

        foreach (var edge in GenerateEdges(current))
        {
          if (!visited.Add(edge.Target))
            continue;

          parent[edge.Target] = current;
          edgeForNode[edge.Target] = edge;

          if (edge.Target.Equals(goal))
            return ReconstructPath(start, goal, parent, edgeForNode);

          queue.Enqueue(edge.Target);
        }
      }

      if (edgeForNode.ContainsKey(goal))
        return ReconstructPath(start, goal, parent, edgeForNode);

      return null;
    }

    private List<PlannedEdge> ReconstructPath(Point start, Point goal, Dictionary<Point, Point> parent, Dictionary<Point, PlannedEdge> edgeForNode)
    {
      var list = new List<PlannedEdge>();
      Point current = goal;

      while (!current.Equals(start))
      {
        if (!edgeForNode.TryGetValue(current, out PlannedEdge edge))
          break;
        list.Add(edge);
        current = parent[current];
      }

      list.Reverse();
      return list;
    }

    private IEnumerable<PlannedEdge> GenerateEdges(Point from)
    {
      // Marcher gauche/droite
      for (int dir = -1; dir <= 1; dir += 2)
      {
        Point candidate = new Point(from.X + dir, from.Y);
        if (IsWalkableCell(candidate.X, candidate.Y))
        {
          yield return new PlannedEdge
          {
            Source = from,
            Target = candidate,
            Type = EdgeType.Walk,
            DirectionX = dir
          };
        }
      }

      // Tomber
      Point? drop = FindDropLanding(from);
      if (drop.HasValue)
      {
        yield return new PlannedEdge
        {
          Source = from,
          Target = drop.Value,
          Type = EdgeType.Drop,
          DirectionX = 0
        };
      }

      // Sauts
      foreach (var candidate in GetCandidateJumpTargets(from))
      {
        if (!IsWalkableCell(candidate.X, candidate.Y))
          continue;

        EdgeType action;
        if (TryGetJumpAction(from, candidate, out action))
        {
          int dir = Math.Sign(candidate.X - from.X);
          yield return new PlannedEdge
          {
            Source = from,
            Target = candidate,
            Type = action,
            DirectionX = dir
          };
        }
      }
    }

    private IEnumerable<Point> GetCandidateJumpTargets(Point from)
    {
      foreach (var cell in walkableCells)
      {
        if (cell.Equals(from))
          continue;

        int dx = Math.Abs(cell.X - from.X);
        int dy = from.Y - cell.Y; // positif si la cible est au-dessus

        if (dx > MAX_JUMP_HORIZONTAL_CELLS)
          continue;

        if (dy > MAX_JUMP_UP_CELLS)
          continue;

        if (dy < -2)
          continue; // Much lower, on gère avec drop

        // Évite les cibles immédiatement adjacentes sur la même ligne (déjà gérées par Walk)
        if (dx <= 1 && Math.Abs(cell.Y - from.Y) <= 1)
          continue;

        yield return cell;
      }
    }

    private bool TryGetJumpAction(Point from, Point target, out EdgeType action)
    {
      action = EdgeType.Jump;

      Vector2 startPos = CellToWorldCenter(from);
      Vector2 targetPos = CellToWorldCenter(target);

      if (SimulateJumpProfile(startPos, targetPos, -1, -1))
      {
        action = EdgeType.Jump;
        return true;
      }

      if (SimulateJumpProfile(startPos, targetPos, 6, -1))
      {
        action = EdgeType.JumpDashEarly;
        return true;
      }

      if (SimulateJumpProfile(startPos, targetPos, 12, -1))
      {
        action = EdgeType.JumpDashLate;
        return true;
      }

      if (SimulateJumpProfile(startPos, targetPos, 6, 18))
      {
        action = EdgeType.JumpDoubleDash;
        return true;
      }

      return false;
    }

    private bool SimulateJumpProfile(Vector2 startPos, Vector2 targetPos, int dashFrame1, int dashFrame2)
    {
      Vector2 pos = startPos;
      Vector2 speed = new Vector2(0f, JUMP_SPEED);
      float dirX = Math.Sign(targetPos.X - startPos.X);
      float desiredSpeedX = dirX * TARGET_RUN_SPEED;

      bool dash1Used = dashFrame1 < 0;
      bool dash2Used = dashFrame2 < 0;

      for (int frame = 0; frame < 160; frame++)
      {
        if (!dash1Used && frame >= dashFrame1)
        {
          Vector2 dashDir = targetPos - pos;
          if (dashDir != Vector2.Zero)
            dashDir.Normalize();
          speed = dashDir * DODGE_SPEED;
          dash1Used = true;
        }
        else if (!dash2Used && frame >= dashFrame2)
        {
          Vector2 dashDir = targetPos - pos;
          if (dashDir != Vector2.Zero)
            dashDir.Normalize();
          speed = dashDir * DODGE_SPEED;
          dash2Used = true;
        }
        else
        {
          speed.Y = Math.Min(speed.Y + GRAVITY, MAX_FALL);
          speed.X = Approach(speed.X, desiredSpeedX, AIR_ACCEL);
        }

        pos += speed;

        if (IsOutOfBounds(pos))
          return false;

        Point cell = WorldToCell(pos);
        if (IsSolid(cell.X, cell.Y))
          return false;

        if (frame > 6 && speed.Y > 0f)
        {
          if (IsWalkableCell(cell.X, cell.Y))
          {
            Vector2 landingCenter = CellToWorldCenter(cell);
            if (Math.Abs(landingCenter.X - targetPos.X) <= BLOCK_SIZE &&
                Math.Abs(landingCenter.Y - targetPos.Y) <= BLOCK_SIZE * 0.8f)
            {
              return true;
            }
          }
        }
      }

      return false;
    }

    #endregion

    #region Exécution du plan

    private void ExecutePath(Point playerCell)
    {
      if (currentEdge == null && plannedEdges.Count > 0)
        StartNextEdge();

      if (currentEdge == null)
      {
        ExecuteFallback();
        return;
      }

      edgeTimer += Engine.DeltaTime;

      Vector2 aimDir = enemy != null ? (enemy.Position - player.Position) : Vector2.Zero;
      if (aimDir != Vector2.Zero)
        aimDir.Normalize();

      var edge = currentEdge.Value;

      switch (edge.Type)
      {
        case EdgeType.Walk:
          this.input.inputState.MoveX = edge.DirectionX;
          this.input.inputState.AimAxis = aimDir;
          if (playerCell.Equals(edge.Target) || (edge.DirectionX > 0 && playerCell.X >= edge.Target.X) || (edge.DirectionX < 0 && playerCell.X <= edge.Target.X))
            CompleteEdge();
          break;

        case EdgeType.Drop:
          this.input.inputState.MoveY = 1;
          this.input.inputState.AimAxis = aimDir;
          if (playerCell.Equals(edge.Target) && playerInfo.onGround)
            CompleteEdge();
          else if (edgeTimer > 3f)
            MarkPathInvalid();
          break;

        case EdgeType.Jump:
          ExecuteJumpEdge(edge, aimDir, holdDash: false, dashTime1: -1f, dashTime2: -1f, playerCell);
          break;

        case EdgeType.JumpDashEarly:
          ExecuteJumpEdge(edge, aimDir, holdDash: true, dashTime1: EARLY_DASH_TIME, dashTime2: -1f, playerCell);
          break;

        case EdgeType.JumpDashLate:
          ExecuteJumpEdge(edge, aimDir, holdDash: true, dashTime1: LATE_DASH_TIME, dashTime2: -1f, playerCell);
          break;

        case EdgeType.JumpDoubleDash:
          ExecuteJumpEdge(edge, aimDir, holdDash: true, dashTime1: EARLY_DASH_TIME, dashTime2: SECOND_DASH_TIME, playerCell);
          break;
      }
    }

    private void ExecuteJumpEdge(PlannedEdge edge, Vector2 aimDir, bool holdDash, float dashTime1, float dashTime2, Point playerCell)
    {
      this.input.inputState.MoveX = edge.DirectionX;
      this.input.inputState.AimAxis = aimDir;

      if (!jumpTriggered)
      {
        this.input.inputState.JumpCheck = true;
        this.input.inputState.JumpPressed = true;
        jumpTriggered = true;
      }
      else if (edgeTimer < HOLD_JUMP_DURATION)
      {
        this.input.inputState.JumpCheck = true;
      }

      if (holdDash)
      {
        if (!dashTriggered && dashTime1 >= 0f && edgeTimer >= dashTime1)
        {
          this.input.inputState.DodgeCheck = true;
          this.input.inputState.DodgePressed = true;
          dashTriggered = true;
        }
        else if (dashTriggered && dashTime2 < 0f)
        {
          this.input.inputState.DodgeCheck = false;
        }

        if (dashTime2 >= 0f && !secondDashTriggered && edgeTimer >= dashTime2)
        {
          this.input.inputState.DodgeCheck = true;
          this.input.inputState.DodgePressed = true;
          secondDashTriggered = true;
        }
      }

      if (playerCell.Equals(edge.Target) && playerInfo.onGround)
      {
        CompleteEdge();
      }
      else if (edgeTimer > 3f)
      {
        MarkPathInvalid();
      }
    }

    private void StartNextEdge()
    {
      if (plannedEdges.Count == 0)
      {
        currentEdge = null;
        return;
      }

      currentEdge = plannedEdges.Dequeue();
      edgeTimer = 0f;
      jumpTriggered = false;
      dashTriggered = false;
      secondDashTriggered = false;
    }

    private void CompleteEdge()
    {
      currentEdge = null;
      jumpTriggered = false;
      dashTriggered = false;
      secondDashTriggered = false;
      edgeTimer = 0f;
    }

    private void MarkPathInvalid()
    {
      plannedEdges.Clear();
      currentEdge = null;
      lastPathStart = new Point(-1, -1);
      lastPathGoal = new Point(-1, -1);
    }

    private void ExecuteFallback()
    {
      Vector2 direction = enemy.Position - player.Position;
      if (direction != Vector2.Zero)
        direction.Normalize();

      int moveX = Math.Sign(direction.X);
      this.input.inputState.MoveX = moveX;
      this.input.inputState.AimAxis = direction;

      if (direction.Y < -0.4f && playerInfo.onGround)
      {
        this.input.inputState.JumpCheck = true;
        this.input.inputState.JumpPressed = true;
      }
    }

    #endregion

    #region Utilitaires navigation

    private bool IsValidCell(Point cell)
    {
      return cell.X >= 0 && cell.Y >= 0 && cell.X < LEVEL_WIDTH && cell.Y < LEVEL_HEIGHT && IsWalkableCell(cell.X, cell.Y);
    }

    private Point FindClosestWalkableCell(Point start)
    {
      if (IsWalkableCell(start.X, start.Y))
        return start;

      float best = float.MaxValue;
      Point bestCell = new Point(-1, -1);

      foreach (var cell in walkableCells)
      {
        float dx = cell.X - start.X;
        float dy = cell.Y - start.Y;
        float dist = Math.Abs(dx) + Math.Abs(dy) * 1.2f;
        if (dist < best)
        {
          best = dist;
          bestCell = cell;
        }
      }

      return bestCell;
    }

    private Point? FindDropLanding(Point from)
    {
      bool encounteredSolid = false;
      for (int offset = 1; offset <= MAX_DROP_CELLS; offset++)
      {
        int y = from.Y + offset;
        if (y >= LEVEL_HEIGHT)
          break;

        if (IsSolid(from.X, y))
        {
          encounteredSolid = true;
          break;
        }

        if (IsWalkableCell(from.X, y))
          return new Point(from.X, y);
      }

      return encounteredSolid ? (Point?)null : null;
    }

    private Vector2 CellToWorldCenter(Point cell)
    {
      return new Vector2(cell.X * BLOCK_SIZE + BLOCK_SIZE * 0.5f, cell.Y * BLOCK_SIZE + BLOCK_SIZE * 0.5f);
    }

    private bool IsWalkableCell(int x, int y)
    {
      if (!IsCellInside(x, y))
        return false;
      if (!IsCellWalkable(x, y))
        return false;
      if (!HasGroundBelow(x, y))
        return false;
      if (y > 0 && !IsCellWalkable(x, y - 1))
        return false;
      return true;
    }

    private bool IsCellInside(int x, int y)
    {
      return x >= 0 && y >= 0 && x < LEVEL_WIDTH && y < LEVEL_HEIGHT;
    }

    private bool IsCellWalkable(int x, int y)
    {
      if (!IsCellInside(x, y))
        return false;
      return levelGrid[y, x] == 0;
    }

    private bool HasGroundBelow(int x, int y)
    {
      if (y + 1 >= LEVEL_HEIGHT)
        return true;
      return levelGrid[y + 1, x] == 1;
    }

    private bool IsSolid(int x, int y)
    {
      if (!IsCellInside(x, y))
        return true;
      return levelGrid[y, x] == 1;
    }

    private Point WorldToCell(Vector2 world)
    {
      int cellX = Clamp((int)(world.X / BLOCK_SIZE), 0, LEVEL_WIDTH - 1);
      int cellY = Clamp((int)(world.Y / BLOCK_SIZE), 0, LEVEL_HEIGHT - 1);
      return new Point(cellX, cellY);
    }

    private bool IsOutOfBounds(Vector2 pos)
    {
      return pos.X < -BLOCK_SIZE || pos.X > LEVEL_WIDTH * BLOCK_SIZE + BLOCK_SIZE ||
             pos.Y < -BLOCK_SIZE || pos.Y > LEVEL_HEIGHT * BLOCK_SIZE + BLOCK_SIZE;
    }

    private float Approach(float value, float target, float delta)
    {
      if (value < target)
      {
        value += delta;
        if (value > target)
          value = target;
      }
      else if (value > target)
      {
        value -= delta;
        if (value < target)
          value = target;
      }

      return value;
    }

    private int Clamp(int value, int min, int max)
    {
      if (value < min)
        return min;
      if (value > max)
        return max;
      return value;
    }

    #endregion

    #region Reset Inputs

    private void ResetInputs()
    {
      this.input.inputState = new InputState();
      this.input.inputState.AimAxis = Vector2.Zero;
    }

    #endregion
  }
}
