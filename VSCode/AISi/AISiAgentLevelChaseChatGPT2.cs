////version poursuite avec tir et catch mais tir dans mur et ne sais pas sauter obstacle haut
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Threading;
//using Microsoft.Xna.Framework;
//using Monocle;
//using MonoMod.Utils;
//using TowerFall;
//using System.Linq;
//using static TowerFall.Player;
//using static TowerFall.Arrow;
//using System.Dynamic;

//namespace TFModFortRiseAiSimple
//{
//  public class AISiAgentLevelChase : TFModFortRiseLoaderAI.Agent
//  {
//    private int[,] levelGrid;
//    public const int LEVEL_WIDTH = 32;  // 64 BLOCK
//    public const int LEVEL_HEIGHT = 24; // 48 BLOCK
//    public const int BLOCK_SIZE = 10;
//    //private const int LEVEL_WIDTH = 32 * 2;  // 64 BLOCK
//    //private const int LEVEL_HEIGHT = 24 * 2; // 48 BLOCK
//    //private const int BLOCK_SIZE = 10 / 2;
//    private static bool levelCalculated = false;
//    private static bool levelPrint = false;

//    //private List<Point> currentPath = null;
//    //private int currentPathIndex = 0;
//    private Point lastStart;
//    //private Point lastGoal;

//    // Variables globales à ajouter en haut de la classe :
//    private float pathRecalcTimer = 0f;
//    private const float PATH_RECALC_INTERVAL = 0.25f; // secondes
//    private List<Point> currentPath = null;
//    private int currentPathIndex = 0;
//    private Point lastGoal = new Point(-1, -1);

//    private bool isJumping = false;
//    private int ledgeCooldown = 0;
//    private bool ledgeJump = false;
//    private int ledgeJumpCooldown = 0;
//    private int ledgeJumpDir = 0;

//    // --- Variables à ajouter en haut de la classe ---
//    private int shootState = 0; // 0 = idle, 1 = preparing, 2 = shooting, 3 = cooldown
//    private int shootFrameCounter = 0;
//    private Vector2 shootDirection = Vector2.Zero;
//    private const int SHOOT_HOLD_FRAMES = 2;
//    private const int SHOOT_COOLDOWN_FRAMES = 5;
//    // Variables supplémentaires à mettre en haut de la classe
//    private float shootCooldownTimer = 0f;
//    private const float SHOOT_COOLDOWN = 0.25f; // secondes entre deux tirs

//    private const int ARROW_CATCH_RANGE = 1; // nombre de cases autour du joueur pour tenter le catch


//    public List<Point> debugPath = new List<Point>();
//    private const float DEBUG_CELL_SIZE = 10f; // correspond à BLOCK_SIZE

//    public Player enemy;
//    public Player player;
//    private PlayerInfo playerInfo = new PlayerInfo();
//    private List<ArrowInfo> arrows = new List<ArrowInfo>();
//    private PlayerInfo enemyInfo = new PlayerInfo();
//    public AISiAgentLevelChase(int index, String type, PlayerInput input) : base(index, type, input)
//    {
//      playerInfo = new PlayerInfo();
//      enemyInfo = new PlayerInfo();
//    }

//    public int getIndex()
//    {
//      return index;
//    }

//    private void UpdateLevelGrid()
//    {
//      if (levelGrid == null)
//        levelGrid = new int[LEVEL_HEIGHT, LEVEL_WIDTH];

//      for (int y = 0; y < LEVEL_HEIGHT; y++)
//      {
//        for (int x = 0; x < LEVEL_WIDTH; x++)
//        {
//          // sample world point at center of cell
//          float worldX = x * BLOCK_SIZE + (BLOCK_SIZE * 0.5f);
//          float worldY = y * BLOCK_SIZE + (BLOCK_SIZE * 0.5f);
//          levelGrid[y, x] = level.CollideCheck(new Vector2(worldX, worldY), GameTags.Solid) ? 1 : 0;
//        }
//      }

//      if (!levelPrint)
//      {
//        DebugPrintGrid();
//        levelPrint = true;
//      }
//    }

//    // Méthode debug pour afficher la grille (à retirer plus tard)
//    private void DebugPrintGrid()
//    {
//      //Logger.Info("DebugPrintGrid");
//      for (int y = 0; y < LEVEL_HEIGHT; y++)
//      {
//        string line = "";
//        for (int x = 0; x < LEVEL_WIDTH; x++)
//        {
//          line += levelGrid[y, x] == 1 ? "1" : "0";
//        }
//        Logger.Info(line);
//      }
//    }
//    public override void Reset()
//    {
//      //calculate levelGrid
//      UpdateLevelGrid();
//      DebugPrintGrid();
//    }

//    public override void Move()
//    {
//      UpdatePerception();

//      if (player == null || enemy == null || levelGrid == null)
//      {
//        ApplyInputsToPlayerInput(0, false, false, false, false);
//        return;
//      }

//      // --- Recalcul du chemin ---
//      pathRecalcTimer += Engine.DeltaTime;
//      if (pathRecalcTimer >= PATH_RECALC_INTERVAL || lastGoal.X != enemyInfo.X || lastGoal.Y != enemyInfo.Y || currentPath == null)
//      {
//        pathRecalcTimer = 0f;
//        lastGoal = new Point(enemyInfo.X, enemyInfo.Y);
//        Point start = new Point(playerInfo.X, playerInfo.Y);
//        Point goal = new Point(enemyInfo.X, enemyInfo.Y);

//        currentPath = FindPath(start, goal);
//        currentPathIndex = 0;
//        debugPath = currentPath != null ? new List<Point>(currentPath) : new List<Point>();
//      }

//      // --- Déplacements ---
//      bool wantJump = false;
//      bool wantDash = false;
//      int desiredDir = 0;
//      bool aimUp = false;
//      bool aimDown = false;

//      // Si pas de chemin, simple poursuite horizontale
//      if (currentPath == null || currentPath.Count == 0)
//      {
//        desiredDir = Math.Sign(enemyInfo.X - playerInfo.X);
//        wantJump = (enemyInfo.Y < playerInfo.Y && playerInfo.onGround);
//        wantDash = false;
//        ApplyInputsToPlayerInput(desiredDir, wantJump, wantDash, false, false);
//      }
//      else
//      {
//        if (currentPathIndex < 0) currentPathIndex = 0;
//        if (currentPathIndex >= currentPath.Count) currentPathIndex = currentPath.Count - 1;

//        Point myCell = new Point(playerInfo.X, playerInfo.Y);
//        while (currentPathIndex < currentPath.Count && currentPath[currentPathIndex].Equals(myCell))
//          currentPathIndex++;

//        if (currentPathIndex >= currentPath.Count)
//        {
//          ApplyInputsToPlayerInput(0, false, false, false, false);
//        }
//        else
//        {
//          Point nextCell = currentPath[currentPathIndex];
//          int deltaX = nextCell.X - playerInfo.X;
//          int deltaY = nextCell.Y - playerInfo.Y;

//          // --- LedgeGrab ---
//          if (playerInfo.GrabEdge)
//          {
//            if (nextCell.Y < playerInfo.Y)
//              ApplyInputsToPlayerInput(Math.Sign(nextCell.X - playerInfo.X), true, false, false, false);
//            else
//              ApplyInputsToPlayerInput(0, false, false, false, false);
//          }
//          else
//          {
//            // --- Déplacements simples ---
//            desiredDir = Math.Sign(deltaX);
//            if (deltaY < 0 && playerInfo.onGround) wantJump = true;
//            if (Math.Abs(deltaX) >= 6 && playerInfo.onGround) wantDash = true;
//            if (deltaY < 0) aimUp = true;
//            if (deltaY > 0) aimDown = true;

//            ApplyInputsToPlayerInput(desiredDir, wantJump, wantDash, aimUp, aimDown);

//            // Avancement du path index
//            if (Math.Abs(playerInfo.X - nextCell.X) <= 0 && Math.Abs(playerInfo.Y - nextCell.Y) <= 0)
//              currentPathIndex++;
//          }
//        }
//      }

//      // --- Gestion du tir ---
//      HandleShooting();
//      HandleArrowCatch();
//    }



//    private void HandleArrowCatch()
//    {
//      if (player == null || arrows == null || arrows.Count == 0) return;

//      // Parcours toutes les flèches
//      foreach (var arrow in arrows)
//      {
//        // On ne tente de rattraper que si la flèche est encore en vol
//        if (arrow.state == ArrowStates.Shooting ||
//            arrow.state == ArrowStates.Drilling ||
//            arrow.state == ArrowStates.Gravity ||
//            arrow.state == ArrowStates.Falling)
//        {
//          // Calculer direction relative de la flèche par rapport au joueur
//          Vector2 toPlayer = player.Position - arrow.Position;

//          // Vérifier si la flèche va vers le joueur
//          if (Vector2.Dot(toPlayer, arrow.Speed) > 0)
//          {
//            // Vérifier la proximité (X et Y en case)
//            Point arrowCell = new Point(arrow.X, arrow.Y);
//            Point playerCell = new Point(playerInfo.X, playerInfo.Y);

//            if (Math.Abs(arrowCell.X - playerCell.X) <= ARROW_CATCH_RANGE &&
//                Math.Abs(arrowCell.Y - playerCell.Y) <= ARROW_CATCH_RANGE)
//            {
//              // Activer le catch
//              this.input.inputState.DodgeCheck = true;
//              this.input.inputState.DodgePressed = !this.input.prevInputState.DodgeCheck;
//              return; // on catch une flèche à la fois
//            }
//          }
//        }
//      }

//      // Sinon, pas de flèche à attraper
//      this.input.inputState.DodgeCheck = false;
//      this.input.inputState.DodgePressed = false;
//    }


//    // --- Méthode pour gérer le tir ---
//    private void HandleShooting()
//    {
//      if (enemy == null || player == null) return;
//      if (playerInfo.NbArrows <= 0) return; // pas de tir possible

//      shootCooldownTimer += Engine.DeltaTime;

//      if (shootCooldownTimer < SHOOT_COOLDOWN && shootState == 0) return;

//      // Calcul direction vers l'ennemi
//      Vector2 dir = enemy.Position - player.Position;
//      if (dir != Vector2.Zero) dir.Normalize();
//      shootDirection = dir;

//      // Déterminer type de tir et portée maximale
//      float deltaX = enemyInfo.X - playerInfo.X;
//      float deltaY = enemyInfo.Y - playerInfo.Y;
//      bool canShoot = false;

//      // Tir horizontal
//      if (Math.Abs(deltaY) <= 1 && Math.Abs(deltaX) <= 13) canShoot = true;

//      // Tir vertical haut
//      else if (deltaX == 0 && deltaY < 0 && Math.Abs(deltaY) <= 13) canShoot = true;

//      // Tir diagonale haut
//      else if (deltaX != 0 && deltaY < 0)
//      {
//        int maxDiagonalX = 17;
//        int maxDiagonalY = 9;
//        if (Math.Abs(deltaX) <= maxDiagonalX && Math.Abs(deltaY) <= maxDiagonalY) canShoot = true;
//      }

//      if (!canShoot) return; // hors portée, ne pas tirer

//      // Cycle multi-frame
//      if (shootState == 0)
//      {
//        shootState = 1; // préparer le tir
//        shootFrameCounter = 0;
//        shootCooldownTimer = 0f; // reset cooldown
//      }

//      if (shootState == 1) // préparer
//      {
//        shootFrameCounter++;
//        this.input.inputState.AimAxis = shootDirection;
//        this.input.inputState.ShootCheck = true;
//        this.input.inputState.ShootPressed = true;

//        if (shootFrameCounter >= SHOOT_HOLD_FRAMES)
//        {
//          shootState = 2;
//          shootFrameCounter = 0;
//        }
//      }
//      else if (shootState == 2) // relâchement
//      {
//        shootFrameCounter++;
//        this.input.inputState.AimAxis = shootDirection;
//        this.input.inputState.ShootCheck = false;
//        this.input.inputState.ShootPressed = false;

//        if (shootFrameCounter >= SHOOT_COOLDOWN_FRAMES)
//        {
//          shootState = 0; // prêt pour prochain tir
//          shootFrameCounter = 0;
//          playerInfo.NbArrows--; // diminuer le nombre de flèches
//        }
//      }
//    }



//    private bool IsCellWalkable(int cellX, int cellY)
//    {
//      // We represent the player as 1 cell wide and 2 cells tall.
//      // playerInfo.Y corresponds to bottom cell -> so to be walkable,
//      // both cellY (bottom) and cellY-1 (top) must be free (0).
//      if (cellX < 0 || cellX >= LEVEL_WIDTH || cellY < 0 || cellY >= LEVEL_HEIGHT) return false;
//      int topY = cellY - 1;
//      if (topY < 0) return false;
//      if (levelGrid[cellY, cellX] == 1) return false;
//      if (levelGrid[topY, cellX] == 1) return false;
//      return true;
//    }

//    private List<Point> FindPath(Point start, Point goal)
//    {
//      // Simple A* on bottom-cell coordinates, with walkable check for 2-high player.
//      // Heuristic: Manhattan
//      var open = new List<Node>();
//      var closed = new HashSet<Point>();

//      Node startNode = new Node(start, 0, Manhattan(start, goal), null);
//      open.Add(startNode);

//      while (open.Count > 0)
//      {
//        // pop lowest F
//        open.Sort((a, b) => a.F.CompareTo(b.F));
//        Node current = open[0];
//        open.RemoveAt(0);

//        if (current.Position.Equals(goal))
//          return ReconstructPath(current);

//        closed.Add(current.Position);

//        // neighbors: left, right, up1/up2/up3 (if reachable), down (drop)
//        List<Point> neighbors = new List<Point>();
//        neighbors.Add(new Point(current.Position.X - 1, current.Position.Y)); // left
//        neighbors.Add(new Point(current.Position.X + 1, current.Position.Y)); // right
//        neighbors.Add(new Point(current.Position.X, current.Position.Y - 1)); // up (one)
//        neighbors.Add(new Point(current.Position.X, current.Position.Y + 1)); // down

//        // Allow up to 3 cells up (short jump)
//        neighbors.Add(new Point(current.Position.X - 1, current.Position.Y - 1)); // diag up-left
//        neighbors.Add(new Point(current.Position.X + 1, current.Position.Y - 1)); // diag up-right

//        foreach (var nb in neighbors)
//        {
//          if (nb.X < 0 || nb.X >= LEVEL_WIDTH || nb.Y <= 0 || nb.Y >= LEVEL_HEIGHT - 1) continue;
//          if (closed.Contains(nb)) continue;

//          // If neighbor is walkable (player fits)
//          if (!IsCellWalkable(nb.X, nb.Y)) continue;

//          float tentativeG = current.G + 1f; // every move cost 1 (could be tuned)

//          // If moving up several cells, give extra cost so A* prefers flat moves
//          if (nb.Y < current.Position.Y) tentativeG += 0.5f * (current.Position.Y - nb.Y);

//          Node existing = open.FirstOrDefault(n => n.Position.Equals(nb));
//          if (existing == null)
//          {
//            Node newNode = new Node(nb, tentativeG, Manhattan(nb, goal), current);
//            open.Add(newNode);
//          }
//          else if (tentativeG < existing.G)
//          {
//            existing.G = tentativeG;
//            existing.Parent = current;
//          }
//        }
//      }

//      // no path
//      return null;
//    }


//    private List<Point> ReconstructPath(Node node)
//    {
//      List<Point> path = new List<Point>();
//      Node cur = node;
//      while (cur != null)
//      {
//        path.Insert(0, cur.Position);
//        cur = cur.Parent;
//      }
//      return path;
//    }

//    private int Manhattan(Point a, Point b)
//    {
//      return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
//    }

//    private void ApplyInputsToPlayerInput(int dirX, bool jump, bool dash, bool aimUp, bool aimDown)
//    {
//      // Remettre à zéro avant tout
//      this.input.inputState.MoveX = 0;
//      this.input.inputState.MoveY = 0;
//      this.input.inputState.AimAxis.X = 0;
//      this.input.inputState.AimAxis.Y = 0;
//      this.input.inputState.JumpCheck = false;
//      this.input.inputState.DodgeCheck = false;

//      // --- Déplacements horizontaux ---
//      if (dirX < 0)
//      {
//        this.input.inputState.MoveX = -1;
//        this.input.inputState.AimAxis.X = -1;
//      }
//      else if (dirX > 0)
//      {
//        this.input.inputState.MoveX = 1;
//        this.input.inputState.AimAxis.X = 1;
//      }

//      // --- Orientation verticale ---
//      if (aimUp)
//      {
//        this.input.inputState.AimAxis.Y = -1;
//        this.input.inputState.MoveY = -1;
//      }
//      else if (aimDown)
//      {
//        this.input.inputState.AimAxis.Y = 1;
//        this.input.inputState.MoveY = 1;
//      }

//      // --- Jump ---
//      if (jump)
//      {
//        this.input.inputState.JumpCheck = true;
//        this.input.inputState.JumpPressed = !this.input.prevInputState.JumpCheck;
//      }

//      // --- Dash ---
//      if (dash)
//      {
//        this.input.inputState.DodgeCheck = true;
//        this.input.inputState.DodgePressed = !this.input.prevInputState.DodgeCheck;
//      }
//    }


//    void UpdatePerception()
//    {
//      UpdateLevelGrid();
//      //DebugPrintGrid();
//      int playerIndex = index;
//      player = level.GetPlayer(index); //todo check 
//                                       //search first enemy
//      int enemyIndex = index == 0 ? 1 : 0;
//      enemy = level.GetPlayer(index == 0 ? 1 : 0);  //todo , test for 2 players only
//      if (player != null)
//      {
//        //Logger.Info("player" + index + " found");

//        UpdatePlayerInfo(player, playerInfo);
//        //Logger.Info("IA " + playerIndex + " pos: " + playerInfo.X + "," + playerInfo.Y);
//      }
//      if (enemy != null)
//      {
//        //Logger.Info("enemy" + (index == 0 ? 1 : 0) + " found");
//        UpdatePlayerInfo(enemy, enemyInfo);
//        //Logger.Info("enemy" + enemyIndex + " pos: " + enemyInfo.X + "," + enemyInfo.Y);
//      }
//      UpdateArrowInfo();
//    }

//    void UpdatePlayerInfo(Player player, PlayerInfo playerInfo)
//    {
//      var dynData = DynamicData.For(player);

//      Point cell = WorldToCell(player.Position);
//      playerInfo.X = cell.X;
//      playerInfo.Y = cell.Y;
//      //playerInfo.X = (int)player.Position.X / 5;
//      //playerInfo.Y = (int)player.Position.Y / 5;
//      playerInfo.onGround = dynData.Get<bool>("OnGround");
//      playerInfo.GrabEdge = dynData.Get<PlayerStates>("State") == PlayerStates.LedgeGrab;
//      playerInfo.Speed = player.Speed;
//      playerInfo.NbArrows = player.Arrows.Count;
//      //if (0 == dynData.Get<int>("PlayerIndex"))
//      //Logger.Info(playerInfo.Speed.X.ToString());
//      playerInfo.CanWallJump = dynData.Invoke<bool>("CanWallJump", Facing.Left) || dynData.Invoke<bool>("CanWallJump", Facing.Right);
//      dynData.Dispose();
//    }

//    void UpdateArrowInfo() {
//      arrows.Clear();
//      foreach  (Arrow arrow in level[GameTags.Arrow]) {
//        ArrowInfo arrowInfo = new ArrowInfo();
//        arrowInfo.state = arrow.State;
//        arrowInfo.Position = arrow.Position;
//        arrowInfo.Speed = arrow.Speed;
//        Point cell = WorldToCell(arrow.Position);
//        arrowInfo.X = cell.X;
//        arrowInfo.Y = cell.Y;
//        arrows.Add(arrowInfo);
//      }
//    }

//    public Point WorldToCell(Vector2 pos)
//    {
//      int cellX = (int)(pos.X / BLOCK_SIZE);
//      int cellY = (int)(pos.Y / BLOCK_SIZE);
//      // clamp inside
//      if (cellX < 0) cellX = 0;
//      if (cellX >= LEVEL_WIDTH) cellX = LEVEL_WIDTH - 1;
//      if (cellY < 0) cellY = 0;
//      if (cellY >= LEVEL_HEIGHT) cellY = LEVEL_HEIGHT - 1;
//      return new Point(cellX, cellY);
//    }
//  }

  
//  class Node
//  {
//    public Point Position;
//    public float G; // coût depuis le départ
//    public float H; // heuristique (distance estimée au but)
//    public float F => G + H;
//    public Node Parent;

//    public Node(Point pos, float g, float h, Node parent = null)
//    {
//      Position = pos;
//      G = g;
//      H = h;
//      Parent = parent;
//    }
//  }
//}
