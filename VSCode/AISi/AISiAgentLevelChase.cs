using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TowerFall;
using System.Linq;
using static TowerFall.Player;

namespace TFModFortRiseAiSimple
{
  public class AISiAgentLevelChase : TFModFortRiseLoaderAI.Agent
  {
    private int[,] levelGrid;
    private const int LEVEL_WIDTH = 32 * 2;  // 64 BLOCK
    private const int LEVEL_HEIGHT = 24 * 2; // 48 BLOCK
    private const int BLOCK_SIZE = 10 / 2;
    private static bool levelCalculated = false;
    private static bool levelPrint = false;

    private Player enemy;
    private Player player;
    private PlayerInfo playerInfo = new PlayerInfo();
    private PlayerInfo enemyInfo = new PlayerInfo();
    public AISiAgentLevelChase(int index, String type, PlayerInput input) : base(index, type, input) { 
      playerInfo = new PlayerInfo();
      enemyInfo = new PlayerInfo();
    }

    //private void UpdateLevelGrid()
    //{
    //  //Logger.Info("UpdateLevelGrid");
    //  if (levelGrid == null)
    //  {
    //    levelGrid = new int[LEVEL_HEIGHT, LEVEL_WIDTH];
    //  }

    //  // Réinitialiser la grille
    //  for (int y = 0; y < LEVEL_HEIGHT; y++)
    //  {
    //    for (int x = 0; x < LEVEL_WIDTH; x++)
    //    {
    //      levelGrid[y, x] = 0;

    //      if (level.CollideCheck(new Vector2(x * BLOCK_SIZE, y * BLOCK_SIZE), GameTags.Solid))
    //      {
    //        levelGrid[y, x] = 1;
    //      }
    //    }
    //  }
    //  if (!levelPrint) {
    //    DebugPrintGrid();
    //    levelPrint = true;
    //  }
    //}

    private void UpdateLevelGrid()
    {
      if (levelGrid == null)
        levelGrid = new int[LEVEL_HEIGHT, LEVEL_WIDTH];

      for (int y = 0; y < LEVEL_HEIGHT; y++)
      {
        for (int x = 0; x < LEVEL_WIDTH; x++)
        {
          // sample world point at center of cell
          float worldX = x * BLOCK_SIZE + (BLOCK_SIZE * 0.5f);
          float worldY = y * BLOCK_SIZE + (BLOCK_SIZE * 0.5f);
          levelGrid[y, x] = level.CollideCheck(new Vector2(worldX, worldY), GameTags.Solid) ? 1 : 0;
        }
      }

      if (!levelPrint)
      {
        DebugPrintGrid();
        levelPrint = true;
      }
    }

    // Méthode debug pour afficher la grille (à retirer plus tard)
    private void DebugPrintGrid()
    {
      //Logger.Info("DebugPrintGrid");
      for (int y = 0; y < LEVEL_HEIGHT; y++)
      {
        string line = "";
        for (int x = 0; x < LEVEL_WIDTH; x++)
        {
          line += levelGrid[y, x] == 1 ? "1" : "0";
        }
        Logger.Info(line);
      }
    }
    public override void Reset() {
      //calculate levelGrid
      UpdateLevelGrid();
      DebugPrintGrid();
    }

    public override void Move()
    {
      UpdatePerception();
      UpdateDecision();
      //ExecuteAction();
    }

    void UpdatePerception()
    {
      UpdateLevelGrid();
      //DebugPrintGrid();
      player = level.GetPlayer(index); //todo check 
      //search first enemy
      enemy = level.GetPlayer(index == 0 ? 1 : 0);  //todo , test for 2 players only
      if (player != null) {
        //Logger.Info("player" + index + " found");

        UpdatePlayerInfo(player, playerInfo);
      }
      if (enemy != null)
      {
        //Logger.Info("enemy" + (index == 0 ? 1 : 0) + " found");
        UpdatePlayerInfo(enemy, enemyInfo);
      }
    }

    void UpdatePlayerInfo(Player player, PlayerInfo playerInfo) {
      var dynData = DynamicData.For(player);

      Point cell = WorldToCell(player.Position);
      playerInfo.X = cell.X;
      playerInfo.Y = cell.Y;
      //playerInfo.X = (int)player.Position.X / 5;
      //playerInfo.Y = (int)player.Position.Y / 5;
      playerInfo.onGround = dynData.Get<bool>("OnGround");
      playerInfo.GrabEdge = dynData.Get<PlayerStates>("State") == PlayerStates.LedgeGrab;
      playerInfo.Speed = player.Speed;
      playerInfo.CanWallJump = dynData.Invoke<bool>("CanWallJump", Facing.Left) || dynData.Invoke<bool>("CanWallJump", Facing.Right);
      dynData.Dispose();
    }

    void UpdateDecision()
    {
      if (player == null || enemy == null)
        return;

      Point start = new Point(playerInfo.X, playerInfo.Y);
      Point goal = new Point(enemyInfo.X, enemyInfo.Y);
      Logger.Info($"Start cell = {start.X},{start.Y} value={levelGrid[start.Y, start.X]}");
      Logger.Info($"Goal  cell = {goal.X},{goal.Y} value={levelGrid[goal.Y, goal.X]}");

      List<Point> path = FindPlatformPath(start, goal);
      if (path == null || path.Count < 2){
        Logger.Info("No path found"); 
        return;
      }

      Point next = path[1];

      input.inputState.MoveX = 0;
      input.inputState.JumpCheck = false;
      input.inputState.JumpPressed = false;

      if (next.X > start.X) input.inputState.MoveX = 1;
      else if (next.X < start.X) input.inputState.MoveX = -1;

      if (next.Y < start.Y && playerInfo.onGround)
      {
        input.inputState.JumpCheck = true;
        input.inputState.JumpPressed = !input.prevInputState.JumpCheck;
      }

      Logger.Info($"AI path step: {next.X},{next.Y}");
    }


    //void ExecuteAction()
    //{
    //  if (OnSamePlatform(self, enemy))
    //    EngageEnemy(enemy);
    //  else
    //    MoveToward(target);
    //}

    List<Point> ReconstructPath(Node node)
    {
      var path = new List<Point>();
      while (node != null)
      {
        path.Add(node.Position);
        node = node.Parent;
      }
      path.Reverse();
      return path;
    }

    float Heuristic(Point a, Point b)
    {
      return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y); // distance Manhattan
    }

    float Distance(Point a, Point b)
    {
      // diagonales un peu plus longues
      return (a.X != b.X && a.Y != b.Y) ? 1.4f : 1f;
    }

    //bool IsWalkable(Point p) //todo sauf si trou dans mur non ?
    //{
    //  if (p.X < 0 || p.X >= LEVEL_WIDTH || p.Y < 0 || p.Y >= LEVEL_HEIGHT)
    //    return false;
    //  return levelGrid[p.Y, p.X] == 0;
    //}

    //bool IsSolid(int gridX, int gridY)
    //{
    //  if (gridX < 0 || gridX >= LEVEL_WIDTH || gridY < 0 || gridY >= LEVEL_HEIGHT)
    //    return true;
    //  return levelGrid[gridY, gridX] == 1;
    //}

    List<Point> FindPlatformPath(Point start, Point goal)
    {
      var openList = new List<Node>();
      var closedList = new HashSet<Point>();

      Node startNode = new Node(start, 0, Heuristic(start, goal));
      openList.Add(startNode);

      int maxJumpHeight = 6;   // hauteur max de saut (plus réaliste pour TowerFall)
      int maxStepWidth = 5;    // portée horizontale max du saut

      Logger.Info($"Start cell = {start.X},{start.Y} value={levelGrid[start.Y, start.X]}");
      Logger.Info($"Goal  cell = {goal.X},{goal.Y} value={levelGrid[goal.Y, goal.X]}");

      while (openList.Count > 0)
      {
        Node current = openList.OrderBy(n => n.F).First();
        if (current.Position == goal)
        {
          Logger.Info($"A*: path found with {closedList.Count} explored nodes");
          return ReconstructPath(current);
        }

        openList.Remove(current);
        closedList.Add(current.Position);

        // --- 1️⃣ MOUVEMENT LATÉRAL (Gauche/Droite) ---
        foreach (int dirX in new int[] { -1, 1 })
        {
          Point next = WrapPoint(new Point(current.Position.X + dirX, current.Position.Y));

          if (!IsWalkable(next))
            continue;

          // Si on est dans un mur, ignore
          if (IsSolid(current.Position.X, current.Position.Y))
            continue;

          // ✅ Si pas de sol dessous, on simule la chute avant de continuer
          if (!IsSolid(next.X, next.Y + 1))
          {
            Point fall = SimulateFall(next);
            if (!fall.Equals(next))
              next = fall;
          }

          TryAddNode(current, next, goal, openList, closedList);
        }

        // --- 2️⃣ SAUT (depuis le sol) ---
        if (IsSolid(current.Position.X, current.Position.Y + 1))
        {
          for (int dx = -maxStepWidth; dx <= maxStepWidth; dx++)
          {
            for (int dy = 1; dy <= maxJumpHeight; dy++)
            {
              Point jump = WrapPoint(new Point(current.Position.X + dx, current.Position.Y - dy));
              if (!IsWalkable(jump))
                continue;

              // Simuler la chute après le saut (atterrissage)
              Point fall = SimulateFall(jump);
              if (!fall.Equals(jump))
                jump = fall;

              if (IsInside(jump))
                TryAddNode(current, jump, goal, openList, closedList);
            }
          }
        }

        // --- 3️⃣ CHUTE (infinie jusqu’à sol) ---
        if (!IsSolid(current.Position.X, current.Position.Y + 1))
        {
          Point fall = SimulateFall(current.Position);
          if (!fall.Equals(current.Position))
            TryAddNode(current, fall, goal, openList, closedList);
        }
      }

      Logger.Info($"A*: no path found (explored={closedList.Count})");
      return new List<Point>();
    }

    // ✅ Simule une chute verticale jusqu’à atteindre un sol
    Point SimulateFall(Point start)
    {
      Point pos = start;
      for (int y = start.Y; y < LEVEL_HEIGHT - 1; y++)
      {
        if (IsSolid(pos.X, y + 1))
          return new Point(pos.X, y);
      }
      return pos; // chute infinie (aucun sol trouvé)
    }

    // ✅ Ajout du voisin dans A*
    void TryAddNode(Node current, Point next, Point goal, List<Node> openList, HashSet<Point> closed)
    {
      if (closed.Contains(next))
        return;

      float newG = current.G + Distance(current.Position, next);
      Node existing = openList.FirstOrDefault(n => n.Position == next);
      if (existing == null)
      {
        openList.Add(new Node(next, newG, Heuristic(next, goal), current));
      }
      else if (newG < existing.G)
      {
        existing.G = newG;
        existing.Parent = current;
      }
    }

    // ✅ Wrap horizontal (écran bouclant)
    Point WrapPoint(Point p)
    {
      if (p.X < 0) p.X = LEVEL_WIDTH - 1;
      else if (p.X >= LEVEL_WIDTH) p.X = 0;
      return p;
    }

    // ✅ Vérifie si on reste dans la carte
    bool IsInside(Point p)
    {
      return p.Y >= 0 && p.Y < LEVEL_HEIGHT;
    }

    int GetPathLength(Node node)
    {
      int len = 0;
      while (node != null)
      {
        len++;
        node = node.Parent;
      }
      return len;
    }

    //void TryAddNode(Node current, Point next, Point goal, List<Node> openList, HashSet<Point> closed)
    //{
    //  if (closed.Contains(next))
    //    return;

    //  float newG = current.G + Distance(current.Position, next);
    //  Node existing = openList.FirstOrDefault(n => n.Position == next);
    //  if (existing == null)
    //  {
    //    openList.Add(new Node(next, newG, Heuristic(next, goal), current));
    //  }
    //  else if (newG < existing.G)
    //  {
    //    existing.G = newG;
    //    existing.Parent = current;
    //  }
    //}

    // ✅ Nouvelle fonction wrap horizontale
    //Point WrapPoint(Point p)
    //{
    //  if (p.X < 0) p.X = LEVEL_WIDTH - 1;
    //  else if (p.X >= LEVEL_WIDTH) p.X = 0;
    //  return p;
    //}

    //bool IsInside(Point p)
    //{
    //  return p.Y >= 0 && p.Y < LEVEL_HEIGHT;
    //}

/// ----------------- UTILITAIRES -----------------

// Use consistent BLOCK_SIZE sampling & convert world position -> cell
Point WorldToCell(Vector2 pos)
    {
      int cellX = (int)(pos.X / BLOCK_SIZE);
      int cellY = (int)(pos.Y / BLOCK_SIZE);
      // clamp inside
      if (cellX < 0) cellX = 0;
      if (cellX >= LEVEL_WIDTH) cellX = LEVEL_WIDTH - 1;
      if (cellY < 0) cellY = 0;
      if (cellY >= LEVEL_HEIGHT) cellY = LEVEL_HEIGHT - 1;
      return new Point(cellX, cellY);
    }

    Point WorldToCellCenterSample(Vector2 pos)
    {
      // If you prefer sampling by center, still same conversion for player pos
      return WorldToCell(pos);
    }

    // Wrap horizontal X only
    //Point WrapPoint(Point p)
    //{
    //  int x = p.X;
    //  if (x < 0) x = LEVEL_WIDTH - 1;
    //  else if (x >= LEVEL_WIDTH) x = 0;
    //  return new Point(x, p.Y);
    //}

    //bool IsInside(Point p)
    //{
    //  return p.X >= 0 && p.X < LEVEL_WIDTH && p.Y >= 0 && p.Y < LEVEL_HEIGHT;
    //}

    bool IsSolid(int gridX, int gridY)
    {
      if (gridX < 0 || gridX >= LEVEL_WIDTH || gridY < 0 || gridY >= LEVEL_HEIGHT)
        return true;
      return levelGrid[gridY, gridX] == 1;
    }

    bool IsWalkable(Point p)
    {
      if (!IsInside(p)) return false;
      return levelGrid[p.Y, p.X] == 0;
    }

    // Find nearest walkable cell if start/goal fall into solid
    Point FindNearestWalkable(Point p, int maxRadius = 6)
    {
      if (IsWalkable(p)) return p;
      for (int r = 1; r <= maxRadius; r++)
      {
        for (int dy = -r; dy <= r; dy++)
        {
          for (int dx = -r; dx <= r; dx++)
          {
            var np = WrapPoint(new Point(p.X + dx, p.Y + dy));
            if (IsInside(np) && IsWalkable(np))
              return np;
          }
        }
      }
      // fallback: return original (may be solid)
      return p;
    }
    /////////////////////////LATER

    //void UpdatePerception(GameState state)
    //{
    //    visibleEnemies.Clear();
    //    incomingProjectiles.Clear();

    //    foreach (var enemy in state.Enemies)
    //    {
    //        if (IsVisible(enemy.Position))
    //            visibleEnemies.Add(enemy);
    //    }

    //    foreach (var projectile in state.Projectiles)
    //    {
    //        if (WillHitMe(projectile))
    //            incomingProjectiles.Add(projectile);
    //    }
    //    Detect nearby platforms
    //    nearbyPlatforms = DetectPlatformsAround(player.Position);
    //}
  }
  class PlayerInfo
  {
    public PlayerState state;
    public Player.PlayerStates towerFallState;
    public int X;
    public int Y;
    public bool onGround = false;
    public bool GrabEdge = false;
    public bool CanWallJump = false;
    public Facing Facing;
    //HasShield
    //HasWings
    //List arrows
    public Vector2 Speed = Vector2.Zero;

    public PlayerInfo()
    {
      Reset();
    }

    public void Reset()
    {
      state = PlayerState.Idle;
      X = 0;
      Y = 0;
      onGround = false;
      GrabEdge = false;
      CanWallJump = false;
      Speed = Vector2.Zero;
      Facing = Facing.Right;
    }
  }

  enum PlayerState
  {
    Idle,
    Moving,
    Attacking,
    Jumping,
    Falling
  }
  class Node
  {
    public Point Position;
    public float G; // coût depuis le départ
    public float H; // heuristique (distance estimée au but)
    public float F => G + H;
    public Node Parent;

    public Node(Point pos, float g, float h, Node parent = null)
    {
      Position = pos;
      G = g;
      H = h;
      Parent = parent;
    }
  }
}
