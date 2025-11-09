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
//using System.Dynamic;

//namespace TFModFortRiseAiSimple
//{
//  public class AISiAgentLevelChase : TFModFortRiseLoaderAI.Agent
//  {
//    private int[,] levelGrid;
//    public const int LEVEL_WIDTH = 32;  // 64 BLOCK
//    public const int LEVEL_HEIGHT = 24; // 48 BLOCK
//    public const int BLOCK_SIZE = 10;
//    private static bool levelPrint = false;

//    private float pathRecalcTimer = 0f;
//    private const float PATH_RECALC_INTERVAL = 0.25f; // secondes
//    private List<Point> currentPath = null;
//    private int currentPathIndex = 0;
//    private Point lastGoal = new Point(-1, -1);

//    public List<Point> debugPath = new List<Point>();

//    public Player enemy;
//    public Player player;
//    private PlayerInfo playerInfo = new PlayerInfo();
//    private PlayerInfo enemyInfo = new PlayerInfo();

//    // --- MISE À JOUR DE LA MACHINE À ÉTATS ---
//    private enum AiState
//    {
//      Idle,
//      Seeking,        // Cherche le prochain mouvement
//      Jumping,        // En train de sauter (maintient la touche)
//      LedgeClimbing,  // Grimpe à un rebord
//      WallJumping,    // Saute sur un mur
//      SuperJumpDash_Phase1, // Phase 1: Sauter + Viser haut
//      SuperJumpDash_Phase2  // Phase 2: Dasher à l'apex
//    }
//    private AiState currentState = AiState.Idle;
//    private Point currentMoveTarget; // La destination du saut/mouvement actuel

//    public AISiAgentLevelChase(int index, String type, PlayerInput input) : base(index, type, input)
//    {
//      playerInfo = new PlayerInfo();
//      enemyInfo = new PlayerInfo();
//    }

//    // ... (Les fonctions getIndex, UpdateLevelGrid, DebugPrintGrid, Reset restent inchangées) ...
//    private void UpdateLevelGrid()
//    {
//      if (levelGrid == null)
//        levelGrid = new int[LEVEL_HEIGHT, LEVEL_WIDTH];

//      for (int y = 0; y < LEVEL_HEIGHT; y++)
//      {
//        for (int x = 0; x < LEVEL_WIDTH; x++)
//        {
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
//    private void DebugPrintGrid()
//    {
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
//      UpdateLevelGrid();
//      DebugPrintGrid();
//    }

//    // =================================================================
//    //
//    // MÉTHODE MOVE (Logique principale)
//    //
//    // =================================================================
//    public override void Move()
//    {
//      UpdatePerception();
//      ResetInputs(); // Réinitialise les inputs à chaque frame

//      if (player == null || enemy == null)
//      {
//        currentState = AiState.Idle;
//        return;
//      }

//      // --- 1. Gestion du Pathfinding (A*) ---
//      pathRecalcTimer -= Engine.DeltaTime;
//      Point goal = new Point(enemyInfo.X, enemyInfo.Y);
//      Point start = new Point(playerInfo.X, playerInfo.Y);

//      if (pathRecalcTimer <= 0 || currentPath == null || goal != lastGoal || playerInfo.GrabEdge)
//      {
//        currentPath = FindPath(start, goal);
//        currentPathIndex = 0;
//        pathRecalcTimer = PATH_RECALC_INTERVAL;
//        lastGoal = goal;
//        debugPath = currentPath;

//        if (currentState != AiState.LedgeClimbing)
//        {
//          currentState = AiState.Seeking;
//        }
//      }

//      if (currentPath == null || currentPathIndex >= currentPath.Count)
//      {
//        currentState = AiState.Idle;
//        return;
//      }

//      // --- 2. Logique de suivi de chemin (FSM) ---

//      // Gérer les états prioritaires
//      if (playerInfo.GrabEdge)
//      {
//        currentState = AiState.LedgeClimbing;
//      }
//      // Si on touche le sol, on arrête les manœuvres aériennes
//      else if (playerInfo.onGround && (currentState == AiState.Jumping || currentState == AiState.LedgeClimbing || currentState == AiState.WallJumping || currentState == AiState.SuperJumpDash_Phase1 || currentState == AiState.SuperJumpDash_Phase2))
//      {
//        currentState = AiState.Seeking;
//      }
//      // Si on est en l'air mais pas en train de faire une manœuvre, on "seek"
//      else if (!playerInfo.onGround && currentState == AiState.Idle)
//      {
//        currentState = AiState.Seeking;
//      }


//      Point currentNode = start;
//      Point nextNode = currentPath[currentPathIndex];

//      // Avancer dans le chemin si on est assez proche du noeud suivant
//      if (Math.Abs(currentNode.X - nextNode.X) <= 1 && Math.Abs(currentNode.Y - nextNode.Y) <= 1)
//      {
//        currentPathIndex++;
//        if (currentPathIndex >= currentPath.Count)
//        {
//          currentState = AiState.Idle;
//          return;
//        }
//        nextNode = currentPath[currentPathIndex];
//        // On ré-évalue uniquement si on n'est pas en pleine manœuvre
//        if (currentState == AiState.Seeking || currentState == AiState.Idle)
//        {
//          currentState = AiState.Seeking;
//        }
//      }

//      int moveX = GetWrappedDirection(currentNode.X, nextNode.X, LEVEL_WIDTH);
//      int moveY = GetWrappedDirection(currentNode.Y, nextNode.Y, LEVEL_HEIGHT);

//      // --- 3. Machine à États (FSM) pour l'Exécution ---

//      switch (currentState)
//      {
//        case AiState.Idle:
//          // Ne rien faire
//          break;

//        case AiState.LedgeClimbing:
//          if (moveX != 0)
//          {
//            SetMoveX(moveX);
//            SetJump();
//          }
//          break;

//        case AiState.Seeking:
//          if (playerInfo.onGround)
//          {
//            // ----- Sur le sol -----
//            if (moveY == 0) // Mouvement horizontal
//            {
//              SetMoveX(moveX);
//            }
//            else if (moveY < 0) // Besoin de monter (Saut)
//            {
//              // --- NOUVELLE LOGIQUE D'ANALYSE DE SAUT ---

//              // Scanner le chemin pour trouver la plateforme d'atterrissage
//              Point landingSpot = nextNode;
//              int pathScanIndex = currentPathIndex;
//              // On cherche le prochain noeud du chemin qui est "au sol"
//              while (pathScanIndex < currentPath.Count && !IsOnGround(currentPath[pathScanIndex]))
//              {
//                pathScanIndex++;
//              }

//              if (pathScanIndex < currentPath.Count)
//              {
//                landingSpot = currentPath[pathScanIndex];
//              }
//              else
//              {
//                landingSpot = nextNode; // Failsafe
//              }

//              currentMoveTarget = landingSpot; // Mémoriser la VRAIE cible
//              int deltaY = currentNode.Y - landingSpot.Y; // Y=0 en haut, donc Y_actuel > Y_cible
//              int wrappedDeltaX = Math.Min(Math.Abs(currentNode.X - landingSpot.X), LEVEL_WIDTH - Math.Abs(currentNode.X - landingSpot.X));

//              // "sauter sur plateforme qui est a 4 ou 5 ou 6 case plus haut"
//              if (deltaY >= 4 && deltaY <= 6 && wrappedDeltaX <= 2) // Saut vertical (peu de X)
//              {
//                // "condition : onground = true, speed.x = 0"
//                SetJump();
//                SetAimUp();
//                SetMoveX(0); // On ne bouge pas horizontalement
//                currentState = AiState.SuperJumpDash_Phase1;
//              }
//              else // Saut normal (horizontal, ou petit saut vertical)
//              {
//                SetJump();
//                SetMoveX(moveX);
//                currentState = AiState.Jumping;
//              }
//            }
//            else // Besoin de descendre
//            {
//              SetMoveX(moveX);
//            }
//          }
//          else
//          {
//            // ----- En l'air -----
//            if (playerInfo.CanWallJump)
//            {
//              SetJump();
//              currentState = AiState.WallJumping;
//            }
//            else
//            {
//              // On se dirige vers la cible en tombant
//              SetMoveX(moveX);
//            }
//          }
//          break;

//        case AiState.Jumping:
//          // On maintient le saut et la direction
//          SetJump();
//          int jumpDirX = GetWrappedDirection(currentNode.X, currentMoveTarget.X, LEVEL_WIDTH);
//          SetMoveX(jumpDirX);
//          break;

//        case AiState.WallJumping:
//          SetJump();
//          SetMoveX(moveX);
//          break;

//        // --- NOUVEAUX ÉTATS POUR LE SUPER SAUT ---

//        case AiState.SuperJumpDash_Phase1:
//          // "mouvement maintenu saut + direction vers le haut maintenu"
//          SetJump();
//          SetAimUp();
//          SetMoveX(0); // Important: speed.x = 0

//          // "quand la vitesse Speed.Y = 0... appuyer sur dash"
//          // (On utilise une petite tolérance)
//          if (Math.Abs(playerInfo.Speed.Y) < 0.1f && !playerInfo.onGround)
//          {
//            currentState = AiState.SuperJumpDash_Phase2;
//          }
//          break;

//        case AiState.SuperJumpDash_Phase2:
//          // "maintenir la direction vers le haut et appuyer sur dash"
//          SetAimUp();
//          SetDash();

//          // "quand on arrive a la hauteur... aller dans la direction de la plateforme"
//          if (currentNode.Y <= currentMoveTarget.Y)
//          {
//            int dirX = GetWrappedDirection(currentNode.X, currentMoveTarget.X, LEVEL_WIDTH);
//            SetMoveX(dirX);
//            // On relâche le dash (ResetInputs) et on finit en contrôlant la chute
//            currentState = AiState.Jumping;
//          }
//          break;
//      }
//    }

//    // =================================================================
//    //
//    // HELPERS POUR LES INPUTS (Selon votre API)
//    //
//    // =================================================================

//    private void ResetInputs()
//    {
//      this.input.inputState.MoveX = 0;
//      this.input.inputState.AimAxis.X = 0;
//      this.input.inputState.AimAxis.Y = 0;
//      this.input.inputState.MoveY = 0;
//      input.inputState.JumpCheck = false;
//      this.input.inputState.DodgeCheck = false;
//    }

//    private void SetMoveX(int direction)
//    {
//      this.input.inputState.MoveX = direction;
//      this.input.inputState.AimAxis.X = direction;
//    }

//    private void SetJump()
//    {
//      input.inputState.JumpCheck = true;
//      input.inputState.JumpPressed = !input.prevInputState.JumpCheck;
//    }

//    // --- NOUVEAUX HELPERS ---
//    private void SetAimUp()
//    {
//      this.input.inputState.AimAxis.Y = -1;
//      this.input.inputState.MoveY = -1;
//    }

//    private void SetDash()
//    {
//      this.input.inputState.DodgeCheck = true;
//      this.input.inputState.DodgePressed = !this.input.prevInputState.DodgeCheck;
//    }

//    // =================================================================
//    //
//    // PATHFINDING A* (Avec gestion du wrapping)
//    //
//    // =================================================================

//    // ... (FindPath, ReconstructPath, GetNeighbors, CalculateHeuristic, GetWrappedDirection restent inchangés) ...
//    private List<Point> FindPath(Point start, Point goal)
//    {
//      var openList = new List<Node>();
//      var closedSet = new HashSet<Point>();
//      var cameFrom = new Dictionary<Point, Point>();

//      var startNode = new Node(start, 0, CalculateHeuristic(start, goal));
//      openList.Add(startNode);

//      while (openList.Count > 0)
//      {
//        openList.Sort((a, b) => a.F.CompareTo(b.F));
//        var currentNode = openList[0];
//        openList.RemoveAt(0);

//        if (currentNode.Position == goal)
//        {
//          return ReconstructPath(cameFrom, currentNode.Position);
//        }

//        closedSet.Add(currentNode.Position);

//        foreach (var neighborPos in GetNeighbors(currentNode.Position))
//        {
//          if (closedSet.Contains(neighborPos))
//            continue;

//          if (!IsWalkable(neighborPos))
//          {
//            closedSet.Add(neighborPos);
//            continue;
//          }

//          // --- NOUVELLE LOGIQUE DE COÛT ---
//          float moveCost = 1.0f; // Coût de base (pour marcher)
//          Point currentPos = currentNode.Position;
//          bool neighborIsOnGround = IsOnGround(neighborPos);

//          if (!neighborIsOnGround)
//          {
//            // Le voisin est en l'air. C'est un "saut".
//            if (neighborPos.X == currentPos.X)
//            {
//              // Mouvement VERTICAL pur dans l'air (le chemin jaune !)
//              // C'est physiquement très difficile ou impossible.
//              moveCost = 30.0f; // Pénalité ÉLEVÉE
//            }
//            else
//            {
//              // Mouvement DIAGONAL dans l'air (saut normal)
//              // C'est un mouvement normal.
//              moveCost = 1.5f; // Légère pénalité
//            }
//          }
//          // Si neighborIsOnGround, le coût reste 1.0f (marcher).

//          float tentativeG = currentNode.G + moveCost;
//          // --- FIN DE LA NOUVELLE LOGIQUE DE COÛT ---


//          var neighborNode = openList.FirstOrDefault(n => n.Position == neighborPos);
//          if (neighborNode == null)
//          {
//            neighborNode = new Node(neighborPos, tentativeG, CalculateHeuristic(neighborPos, goal));
//            openList.Add(neighborNode);
//            cameFrom[neighborPos] = currentNode.Position;
//          }
//          else if (tentativeG < neighborNode.G)
//          {
//            neighborNode.G = tentativeG;
//            cameFrom[neighborPos] = currentNode.Position;
//          }
//        }
//      }

//      return null; // Pas de chemin trouvé
//    }

//    private List<Point> ReconstructPath(Dictionary<Point, Point> cameFrom, Point current)
//    {
//      var path = new List<Point>();
//      path.Add(current);
//      while (cameFrom.ContainsKey(current))
//      {
//        current = cameFrom[current];
//        path.Add(current);
//      }
//      path.Reverse();
//      if (path.Count > 0)
//        path.RemoveAt(0); // Enlève le point de départ
//      return path;
//    }

//    private IEnumerable<Point> GetNeighbors(Point p)
//    {
//      int x_plus = (p.X + 1) % LEVEL_WIDTH;
//      int x_minus = (p.X - 1 + LEVEL_WIDTH) % LEVEL_WIDTH;
//      int y_plus = (p.Y + 1) % LEVEL_HEIGHT;
//      int y_minus = (p.Y - 1 + LEVEL_HEIGHT) % LEVEL_HEIGHT;

//      yield return new Point(x_plus, p.Y);
//      yield return new Point(x_minus, p.Y);
//      yield return new Point(p.X, y_plus);
//      yield return new Point(p.X, y_minus);
//    }

//    private float CalculateHeuristic(Point a, Point b)
//    {
//      int dx = Math.Abs(a.X - b.X);
//      int dy = Math.Abs(a.Y - b.Y);
//      int wrappedDx = Math.Min(dx, LEVEL_WIDTH - dx);
//      int wrappedDy = Math.Min(dy, LEVEL_HEIGHT - dy);
//      return wrappedDx + wrappedDy;
//    }

//    private int GetWrappedDirection(int from, int to, int max)
//    {
//      if (from == to) return 0;
//      float dist = to - from;
//      float distWrapPos = (to - from) + max;
//      float distWrapNeg = (to - from) - max;
//      float absDist = Math.Abs(dist);
//      float absWrapPos = Math.Abs(distWrapPos);
//      float absWrapNeg = Math.Abs(distWrapNeg);

//      if (absDist <= absWrapPos && absDist <= absWrapNeg) { return Math.Sign(dist); }
//      if (absWrapPos < absWrapNeg) { return Math.Sign(distWrapPos); }
//      return Math.Sign(distWrapNeg);
//    }


//    // --- HELPERS DE GRILLE (CORRIGÉS POUR WRAPPING) ---

//    private bool IsWalkable(Point p)
//    {
//      if (p.X < 0 || p.X >= LEVEL_WIDTH || p.Y < 0 || p.Y >= LEVEL_HEIGHT)
//        return false;

//      // Gère le wrapping vertical pour la tête
//      int headY = (p.Y - 1 + LEVEL_HEIGHT) % LEVEL_HEIGHT;

//      bool selfEmpty = levelGrid[p.Y, p.X] == 0;
//      bool headEmpty = levelGrid[headY, p.X] == 0;

//      return selfEmpty && headEmpty;
//    }

//    private bool IsOnGround(Point p)
//    {
//      if (p.X < 0 || p.X >= LEVEL_WIDTH || p.Y < 0 || p.Y >= LEVEL_HEIGHT)
//        return false;

//      // Gère le wrapping vertical pour le sol
//      int groundY = (p.Y + 1) % LEVEL_HEIGHT;

//      return levelGrid[groundY, p.X] == 1;
//    }


//    // =================================================================
//    //
//    // LE RESTE DE VOTRE CLASSE (UpdatePerception, etc.)
//    //
//    // =================================================================

//    void UpdatePerception()
//    {
//      UpdateLevelGrid();
//      player = level.GetPlayer(index);
//      enemy = level.GetPlayer(index == 0 ? 1 : 0);
//      if (player != null)
//      {
//        UpdatePlayerInfo(player, playerInfo);
//      }
//      if (enemy != null)
//      {
//        UpdatePlayerInfo(enemy, enemyInfo);
//      }
//    }

//    void UpdatePlayerInfo(Player player, PlayerInfo playerInfo)
//    {
//      var dynData = DynamicData.For(player);

//      Point cell = WorldToCell(player.Position); // Utilise la version corrigée
//      playerInfo.X = cell.X;
//      playerInfo.Y = cell.Y;
//      playerInfo.onGround = dynData.Get<bool>("OnGround");
//      playerInfo.GrabEdge = dynData.Get<PlayerStates>("State") == PlayerStates.LedgeGrab;
//      playerInfo.Speed = player.Speed;
//      playerInfo.CanWallJump = dynData.Invoke<bool>("CanWallJump", Facing.Left) || dynData.Invoke<bool>("CanWallJump", Facing.Right);
//      dynData.Dispose();
//    }

//    // --- CORRIGÉ POUR LE WRAPPING ---
//    public Point WorldToCell(Vector2 pos)
//    {
//      int cellX = (int)(pos.X / BLOCK_SIZE);
//      int cellY = (int)(pos.Y / BLOCK_SIZE);

//      // Gérer le wrapping
//      cellX = (cellX % LEVEL_WIDTH + LEVEL_WIDTH) % LEVEL_WIDTH;
//      cellY = (cellY % LEVEL_HEIGHT + LEVEL_HEIGHT) % LEVEL_HEIGHT;

//      return new Point(cellX, cellY);
//    }
//  }

//  // ... (Les classes PlayerInfo, PlayerState, et Node restent inchangées) ...
//  class PlayerInfo
//  {
//    public PlayerState state;
//    public Player.PlayerStates towerFallState;
//    public int X;
//    public int Y;
//    public bool onGround = false;
//    public bool GrabEdge = false;
//    public bool CanWallJump = false;
//    public Facing Facing;
//    public Vector2 Speed = Vector2.Zero;

//    public PlayerInfo() { Reset(); }
//    public void Reset()
//    {
//      state = PlayerState.Idle; X = 0; Y = 0; onGround = false;
//      GrabEdge = false; CanWallJump = false; Speed = Vector2.Zero; Facing = Facing.Right;
//    }
//  }
//  enum PlayerState { Idle, Moving, Attacking, Jumping, Falling }
//  class Node
//  {
//    public Point Position;
//    public float G, H;
//    public float F => G + H;
//    public Node Parent;
//    public Node(Point pos, float g, float h, Node parent = null)
//    { Position = pos; G = g; H = h; Parent = parent; }
//  }
//}