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
using System.Dynamic;

namespace TFModFortRiseAiSimple
{
  public class AISiAgentLevelChase : TFModFortRiseLoaderAI.Agent
  {
    private int[,] levelGrid;
    public const int LEVEL_WIDTH = 32;  // 64 BLOCK
    public const int LEVEL_HEIGHT = 24; // 48 BLOCK
    public const int BLOCK_SIZE = 10;
    //private const int LEVEL_WIDTH = 32 * 2;  // 64 BLOCK
    //private const int LEVEL_HEIGHT = 24 * 2; // 48 BLOCK
    //private const int BLOCK_SIZE = 10 / 2;
    private static bool levelCalculated = false;
    private static bool levelPrint = false;

    //private List<Point> currentPath = null;
    //private int currentPathIndex = 0;
    private Point lastStart;
    //private Point lastGoal;

    // Variables globales à ajouter en haut de la classe :
    private float pathRecalcTimer = 0f;
    private const float PATH_RECALC_INTERVAL = 0.25f; // secondes
    private List<Point> currentPath = null;
    private int currentPathIndex = 0;
    private Point lastGoal = new Point(-1, -1);

    private bool isJumping = false;
    private int ledgeCooldown = 0;
    private bool ledgeJump = false;
    private int ledgeJumpCooldown = 0;
    private int ledgeJumpDir = 0;

    public List<Point> debugPath = new List<Point>();
    private const float DEBUG_CELL_SIZE = 10f; // correspond à BLOCK_SIZE

    public Player enemy;
    public Player player;
    private PlayerInfo playerInfo = new PlayerInfo();
    private PlayerInfo enemyInfo = new PlayerInfo();
    public AISiAgentLevelChase(int index, String type, PlayerInput input) : base(index, type, input) { 
      playerInfo = new PlayerInfo();
      enemyInfo = new PlayerInfo();
    }

    public int getIndex(){
      return index;
    }

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

      if (player == null || enemy == null || levelGrid == null)
      {
        ApplyInputsToPlayerInput(0, false, false, false, false);
        return;
      }

      pathRecalcTimer += Engine.DeltaTime;
      if (pathRecalcTimer >= PATH_RECALC_INTERVAL || lastGoal.X != enemyInfo.X || lastGoal.Y != enemyInfo.Y || currentPath == null)
      {
        pathRecalcTimer = 0f;
        lastGoal = new Point(enemyInfo.X, enemyInfo.Y);
        Point start = new Point(playerInfo.X, playerInfo.Y);
        Point goal = new Point(enemyInfo.X, enemyInfo.Y);

        if (playerInfo.GrabEdge)
        {
          // Recalculer un chemin pour sortir de ledgegrab
        }

        currentPath = FindPath(start, goal);
        currentPathIndex = 0;
        debugPath = currentPath != null ? new List<Point>(currentPath) : new List<Point>();
      }

      bool wantJump = false;
      bool wantDash = false;
      // Si pas de chemin, simple poursuite horizontale
      if (currentPath == null || currentPath.Count == 0)
      {
        int dir = Math.Sign(enemyInfo.X - playerInfo.X);
        wantJump = (enemyInfo.Y < playerInfo.Y && playerInfo.onGround);
        wantDash = false;

        if (playerInfo.GrabEdge)
        {
          if (enemyInfo.Y < playerInfo.Y)
            ApplyInputsToPlayerInput(dir, true, true, false, false); // grimpe
          else
            ApplyInputsToPlayerInput(0, false, false, false, false); // lâche prise
          return;
        }

        ApplyInputsToPlayerInput(dir, wantJump, wantDash, false, false);
        return;
      }

      // Suivi du chemin
      if (currentPathIndex < 0) currentPathIndex = 0;
      if (currentPathIndex >= currentPath.Count) currentPathIndex = currentPath.Count - 1;

      Point myCell = new Point(playerInfo.X, playerInfo.Y);
      while (currentPathIndex < currentPath.Count && currentPath[currentPathIndex].Equals(myCell))
        currentPathIndex++;

      if (currentPathIndex >= currentPath.Count)
      {
        ApplyInputsToPlayerInput(0, false, false, false, false);
        return;
      }

      Point nextCell = currentPath[currentPathIndex];
      int deltaX = nextCell.X - playerInfo.X;
      int deltaY = nextCell.Y - playerInfo.Y;

      int desiredDir = 0;
      wantJump = false;
      wantDash = false;
      bool aimUp = false;
      bool aimDown = false;

      // --- LedgeGrab ---
      if (playerInfo.GrabEdge)
      {
        if (nextCell.Y < playerInfo.Y)
          ApplyInputsToPlayerInput(Math.Sign(nextCell.X - playerInfo.X), true, false, false, false);
        else
          ApplyInputsToPlayerInput(0, false, false, false, false);
        return;
      }

      // --- Déplacements simples ---
      if (deltaX != 0 && deltaY == 0)
      {
        desiredDir = Math.Sign(deltaX);
      }
      else if (deltaY < 0) // Monter
      {
        desiredDir = Math.Sign(deltaX);
        if (playerInfo.onGround)
          wantJump = true;
        else
          aimUp = true;
      }
      else if (deltaY > 0) // Descendre
      {
        desiredDir = Math.Sign(deltaX);
        aimDown = true;
      }

      if (Math.Abs(deltaX) >= 6 && playerInfo.onGround)
        wantDash = true;

      ApplyInputsToPlayerInput(desiredDir, wantJump, wantDash, aimUp, aimDown);

      // Avancement du path index
      if (Math.Abs(playerInfo.X - nextCell.X) <= 0 && Math.Abs(playerInfo.Y - nextCell.Y) <= 0)
        currentPathIndex++;
    }


    private bool IsCellWalkable(int cellX, int cellY)
    {
      // We represent the player as 1 cell wide and 2 cells tall.
      // playerInfo.Y corresponds to bottom cell -> so to be walkable,
      // both cellY (bottom) and cellY-1 (top) must be free (0).
      if (cellX < 0 || cellX >= LEVEL_WIDTH || cellY < 0 || cellY >= LEVEL_HEIGHT) return false;
      int topY = cellY - 1;
      if (topY < 0) return false;
      if (levelGrid[cellY, cellX] == 1) return false;
      if (levelGrid[topY, cellX] == 1) return false;
      return true;
    }

    private List<Point> FindPath(Point start, Point goal)
    {
      // Simple A* on bottom-cell coordinates, with walkable check for 2-high player.
      // Heuristic: Manhattan
      var open = new List<Node>();
      var closed = new HashSet<Point>();

      Node startNode = new Node(start, 0, Manhattan(start, goal), null);
      open.Add(startNode);

      while (open.Count > 0)
      {
        // pop lowest F
        open.Sort((a, b) => a.F.CompareTo(b.F));
        Node current = open[0];
        open.RemoveAt(0);

        if (current.Position.Equals(goal))
          return ReconstructPath(current);

        closed.Add(current.Position);

        // neighbors: left, right, up1/up2/up3 (if reachable), down (drop)
        List<Point> neighbors = new List<Point>();
        neighbors.Add(new Point(current.Position.X - 1, current.Position.Y)); // left
        neighbors.Add(new Point(current.Position.X + 1, current.Position.Y)); // right
        neighbors.Add(new Point(current.Position.X, current.Position.Y - 1)); // up (one)
        neighbors.Add(new Point(current.Position.X, current.Position.Y + 1)); // down

        // Allow up to 3 cells up (short jump)
        neighbors.Add(new Point(current.Position.X - 1, current.Position.Y - 1)); // diag up-left
        neighbors.Add(new Point(current.Position.X + 1, current.Position.Y - 1)); // diag up-right

        foreach (var nb in neighbors)
        {
          if (nb.X < 0 || nb.X >= LEVEL_WIDTH || nb.Y <= 0 || nb.Y >= LEVEL_HEIGHT - 1) continue;
          if (closed.Contains(nb)) continue;

          // If neighbor is walkable (player fits)
          if (!IsCellWalkable(nb.X, nb.Y)) continue;

          float tentativeG = current.G + 1f; // every move cost 1 (could be tuned)

          // If moving up several cells, give extra cost so A* prefers flat moves
          if (nb.Y < current.Position.Y) tentativeG += 0.5f * (current.Position.Y - nb.Y);

          Node existing = open.FirstOrDefault(n => n.Position.Equals(nb));
          if (existing == null)
          {
            Node newNode = new Node(nb, tentativeG, Manhattan(nb, goal), current);
            open.Add(newNode);
          }
          else if (tentativeG < existing.G)
          {
            existing.G = tentativeG;
            existing.Parent = current;
          }
        }
      }

      // no path
      return null;
    }


    private List<Point> ReconstructPath(Node node)
    {
      List<Point> path = new List<Point>();
      Node cur = node;
      while (cur != null)
      {
        path.Insert(0, cur.Position);
        cur = cur.Parent;
      }
      return path;
    }

    private int Manhattan(Point a, Point b)
    {
      return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    private void ApplyInputsToPlayerInput(int dirX, bool jump, bool dash, bool aimUp, bool aimDown)
    {
      // Remettre à zéro avant tout
      this.input.inputState.MoveX = 0;
      this.input.inputState.MoveY = 0;
      this.input.inputState.AimAxis.X = 0;
      this.input.inputState.AimAxis.Y = 0;
      this.input.inputState.JumpCheck = false;
      this.input.inputState.DodgeCheck = false;

      // --- Déplacements horizontaux ---
      if (dirX < 0)
      {
        this.input.inputState.MoveX = -1;
        this.input.inputState.AimAxis.X = -1;
      }
      else if (dirX > 0)
      {
        this.input.inputState.MoveX = 1;
        this.input.inputState.AimAxis.X = 1;
      }

      // --- Orientation verticale ---
      if (aimUp)
      {
        this.input.inputState.AimAxis.Y = -1;
        this.input.inputState.MoveY = -1;
      }
      else if (aimDown)
      {
        this.input.inputState.AimAxis.Y = 1;
        this.input.inputState.MoveY = 1;
      }

      // --- Jump ---
      if (jump)
      {
        this.input.inputState.JumpCheck = true;
        this.input.inputState.JumpPressed = !this.input.prevInputState.JumpCheck;
      }

      // --- Dash ---
      if (dash)
      {
        this.input.inputState.DodgeCheck = true;
        this.input.inputState.DodgePressed = !this.input.prevInputState.DodgeCheck;
      }
    }


    void UpdatePerception()
    {
      UpdateLevelGrid();
      //DebugPrintGrid();
      int playerIndex = index;
      player = level.GetPlayer(index); //todo check 
                                       //search first enemy
      int enemyIndex = index == 0 ? 1 : 0;
      enemy = level.GetPlayer(index == 0 ? 1 : 0);  //todo , test for 2 players only
      if (player != null) {
        //Logger.Info("player" + index + " found");

        UpdatePlayerInfo(player, playerInfo);
        //Logger.Info("IA " + playerIndex + " pos: " + playerInfo.X + "," + playerInfo.Y);
      }
      if (enemy != null)
      {
        //Logger.Info("enemy" + (index == 0 ? 1 : 0) + " found");
        UpdatePlayerInfo(enemy, enemyInfo);
        //Logger.Info("enemy" + enemyIndex + " pos: " + enemyInfo.X + "," + enemyInfo.Y);
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
      //if (0 == dynData.Get<int>("PlayerIndex"))
        //Logger.Info(playerInfo.Speed.X.ToString());
      playerInfo.CanWallJump = dynData.Invoke<bool>("CanWallJump", Facing.Left) || dynData.Invoke<bool>("CanWallJump", Facing.Right);
      dynData.Dispose();
    }

    public Point WorldToCell(Vector2 pos)
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
