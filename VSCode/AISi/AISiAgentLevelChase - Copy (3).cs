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

//    public List<Point> debugPath = new List<Point>();
//    private const float DEBUG_CELL_SIZE = 10f; // correspond à BLOCK_SIZE

//    public Player enemy;
//    public Player player;
//    private PlayerInfo playerInfo = new PlayerInfo();
//    private PlayerInfo enemyInfo = new PlayerInfo();
//    public AISiAgentLevelChase(int index, String type, PlayerInput input) : base(index, type, input) { 
//      playerInfo = new PlayerInfo();
//      enemyInfo = new PlayerInfo();
//    }

//    public int getIndex(){
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
//    public override void Reset() {
//      //calculate levelGrid
//      UpdateLevelGrid();
//      DebugPrintGrid();
//    }

//    public override void Move()
//    {
//      UpdatePerception();
//      UpdateDecision();
//      //ExecuteAction();
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
//      if (player != null) {
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
//    }

//    void UpdatePlayerInfo(Player player, PlayerInfo playerInfo) {
//      var dynData = DynamicData.For(player);

//      Point cell = WorldToCell(player.Position);
//      playerInfo.X = cell.X;
//      playerInfo.Y = cell.Y;
//      //playerInfo.X = (int)player.Position.X / 5;
//      //playerInfo.Y = (int)player.Position.Y / 5;
//      playerInfo.onGround = dynData.Get<bool>("OnGround");
//      playerInfo.GrabEdge = dynData.Get<PlayerStates>("State") == PlayerStates.LedgeGrab;
//      playerInfo.Speed = player.Speed;
//      //if (0 == dynData.Get<int>("PlayerIndex"))
//        //Logger.Info(playerInfo.Speed.X.ToString());
//      playerInfo.CanWallJump = dynData.Invoke<bool>("CanWallJump", Facing.Left) || dynData.Invoke<bool>("CanWallJump", Facing.Right);
//      dynData.Dispose();
//    }

//    void UpdateDecision()
//    {
//      if (player == null || enemy == null)
//        return;

//      // 🕒 Timer de recalcul de path
//      pathRecalcTimer -= Engine.DeltaTime;
//      if (pathRecalcTimer < 0)
//        pathRecalcTimer = 0;

//      Point start = new Point(playerInfo.X, playerInfo.Y);
//      Point goal = new Point(enemyInfo.X, enemyInfo.Y);

//      start = FindNearestWalkable(start);
//      goal = FindNearestWalkable(goal);

//      bool needNewPath = false;

//      // --- 🔁 Conditions de recalcul ---
//      if (currentPath == null || currentPath.Count == 0)
//        needNewPath = true;
//      else if (goal.X != lastGoal.X || goal.Y != lastGoal.Y)
//        needNewPath = true;
//      else if (currentPathIndex >= currentPath.Count)
//        needNewPath = false; // terminé
//      else
//      {
//        Point currentTarget = currentPath[Math.Min(currentPathIndex, currentPath.Count - 1)];
//        int dx = Math.Min(Math.Abs(start.X - currentTarget.X), LEVEL_WIDTH - Math.Abs(start.X - currentTarget.X)); // wrap horizontal
//        int dy = Math.Abs(start.Y - currentTarget.Y);
//        if (dx > 2 || dy > 2)
//          needNewPath = true;
//      }

//      // --- 🕒 Fréquence max ---
//      if (needNewPath && pathRecalcTimer > 0f)
//        needNewPath = false;

//      // --- 🔄 Recalcul de chemin ---
//      if (needNewPath)
//      {
//        pathRecalcTimer = PATH_RECALC_INTERVAL;

//        List<Point> newPath = FindPlatformPath(start, goal);
//        if (newPath != null && newPath.Count > 1)
//        {
//          currentPath = newPath;
//          currentPathIndex = 0;
//          lastGoal = goal;
//          debugPath = newPath;
//          //Logger.Info($"AI recalculated path ({newPath.Count} nodes)");
//        }
//        else
//        {
//          debugPath.Clear();
//          //Logger.Info("AI: no valid path");
//          return;
//        }
//      }

//      // --- 🧗‍♂️ Gestion spéciale du ledge grab ---
//      if (ledgeJump) {
//        if (ledgeJumpCooldown > 0) {
//          ledgeJumpCooldown--;
//          input.inputState.MoveX = ledgeJumpDir;
//          input.inputState.JumpCheck = true;
//          input.inputState.JumpPressed = !input.prevInputState.JumpCheck;
//          return;
//        }
//        ledgeJump = false;
//      }
//      if (playerInfo.GrabEdge)
//      {
//        // Petit cooldown pour éviter les boucles
//        if (ledgeCooldown > 0)
//        {
//          ledgeCooldown--;
//          return;
//        }

//        //Logger.Info("AI ledge grab detected");

//        int dir = playerInfo.Facing == Facing.Right ? 1 : -1;
//        Point above = new Point(playerInfo.X, playerInfo.Y - 1);
//        Point topCell = new Point(playerInfo.X + dir, playerInfo.Y - 1);

//        bool canClimb = IsInside(topCell) && IsWalkable(topCell) && IsSolid(topCell.X, topCell.Y + 1);

//        if (canClimb)
//        {
//          // 👆 Grimper : saut + direction vers le rebord
//          input.inputState.JumpCheck = true;
//          input.inputState.JumpPressed = !input.prevInputState.JumpCheck;
//          input.inputState.MoveX = dir;
//          //Logger.Info("AI climbs ledge");
//          ledgeJump = true;
//          ledgeJumpCooldown = 80;
//          ledgeJumpDir = dir;
//        }
//        else
//        {
//          // 🚫 Se lâcher
//          input.inputState.MoveX = 0;
//          ledgeCooldown = 20; // empêche de se raccrocher trop vite
//          //Logger.Info("AI drops ledge");
//        }

//        return; // on sort ici
//      }

//      // --- 🚶‍♂️ Avancement du chemin existant ---
//      if (currentPath == null || currentPath.Count < 2)
//        return;

//      Point target = currentPath[Math.Min(currentPathIndex + 1, currentPath.Count - 1)];

//      // Proximité : passer au point suivant (Cela évite que le saut soit interrompu par un changement de cible prématuré.)
//      if (playerInfo.onGround && Math.Abs(start.X - target.X) <= 1 && Math.Abs(start.Y - target.Y) <= 1)
//        currentPathIndex++;

//      // --- 🔧 Contrôle du mouvement ---
//      input.inputState.MoveX = 0;
//      input.inputState.JumpCheck = false;
//      input.inputState.JumpPressed = false;

//      if (currentPath == null || currentPath.Count == 0)
//        return;

//      // Direction horizontale
//      int moveDir = Math.Sign(target.X - start.X);
//      input.inputState.MoveX = moveDir;

//      // --- 🚀 Décision de saut ---
//      bool shouldJump = false;


//      // --- Vérifie si un obstacle bloque le chemin (petite marche) ---
//      int nextX = start.X + moveDir;
//      int nextY = start.Y;

//      // Si il y a un mur directement devant mais de l’espace au-dessus → sauter
//      if (IsSolid(nextX, nextY) && !IsSolid(nextX, nextY - 1))
//      {
//        shouldJump = true;
//      }

//      // 1️⃣ Trou devant
//      //int nextX = start.X + moveDir;
//      //int groundY = start.Y + 1;
//      // Vérifie plusieurs cases en avant pour estimer la longueur du vide
//      int lookAhead = 3;
//      bool edgeDetected = false;
//      for (int i = 1; i <= lookAhead; i++)
//      {
//        int checkX = start.X + moveDir * i;
//        if (!IsSolid(checkX, start.Y + 1))
//        {
//          edgeDetected = true;
//          break;
//        }
//      }
//      if (edgeDetected)
//      {
//        // on saute seulement si on est proche du bord (1 ou 2 cases avant le vide)
//        int distanceToEdge = 0;
//        while (distanceToEdge < lookAhead && IsSolid(start.X + moveDir * distanceToEdge, start.Y + 1))
//          distanceToEdge++;

//        if (distanceToEdge <= 1) // saute seulement à 1 case du bord
//          shouldJump = true;
//      }

//      // 2️⃣ Monter vers une plateforme
//      if (target.Y < start.Y)
//        shouldJump = true;

//      // 3️⃣ Saut effectif
//      if (playerInfo.onGround)
//      {
//        if (shouldJump)
//        {
//          input.inputState.JumpCheck = true;
//          input.inputState.JumpPressed = !input.prevInputState.JumpCheck;
//          isJumping = true;
//        }
//        else
//        {
//          isJumping = false;
//        }
//      }
//      else if (isJumping)
//      {
//        input.inputState.JumpCheck = true;
//        input.inputState.JumpPressed = !input.prevInputState.JumpCheck;
//      }

//      //Logger.Info($"AI path step: ({target.X},{target.Y}) idx={currentPathIndex}/{currentPath.Count}");
//    }

//    float DistanceInGrid(Point a, Point b)
//    {
//      return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
//    }

//    //void UpdateDecision()
//    //{
//    //  if (player == null || enemy == null)
//    //    return;

//    //  Point start = new Point(playerInfo.X, playerInfo.Y);
//    //  Point goal = new Point(enemyInfo.X, enemyInfo.Y);

//    //  start = FindNearestWalkable(start);
//    //  goal = FindNearestWalkable(goal);

//    //  List<Point> path = FindPlatformPath(start, goal);
//    //  if (path == null || path.Count < 2){
//    //    Logger.Info("No path found"); 
//    //    return;
//    //  }

//    //  Point next = path[1];

//    //  input.inputState.MoveX = 0;
//    //  input.inputState.JumpCheck = false;
//    //  input.inputState.JumpPressed = false;

//    //  if (next.X > start.X) input.inputState.MoveX = 1;
//    //  else if (next.X < start.X) input.inputState.MoveX = -1;

//    //  if (next.Y < start.Y && playerInfo.onGround)
//    //  {
//    //    input.inputState.JumpCheck = true;
//    //    input.inputState.JumpPressed = !input.prevInputState.JumpCheck;
//    //  }

//    //  Logger.Info($"AI path step: {next.X},{next.Y}");
//    //}


//    //void ExecuteAction()
//    //{
//    //  if (OnSamePlatform(self, enemy))
//    //    EngageEnemy(enemy);
//    //  else
//    //    MoveToward(target);
//    //}

//    List<Point> ReconstructPath(Node node)
//    {
//      var path = new List<Point>();
//      while (node != null)
//      {
//        path.Add(node.Position);
//        node = node.Parent;
//      }
//      path.Reverse();
//      return path;
//    }

//    float Heuristic(Point a, Point b)
//    {
//      return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y); // distance Manhattan
//    }

//    float Distance(Point a, Point b)
//    {
//      // diagonales un peu plus longues
//      return (a.X != b.X && a.Y != b.Y) ? 1.4f : 1f;
//    }

//    List<Point> FindPlatformPath(Point start, Point goal)
//    {
//      if (start == goal)
//      {
//        return new List<Point> { start }; // ou empty selon ton usage
//      }
//      var openList = new List<Node>();
//      var closedList = new HashSet<Point>();

//      Node startNode = new Node(start, 0, Heuristic(start, goal));
//      openList.Add(startNode);

//      int maxJumpHeight = 3;   //  4 pour arriver audesus de la plateforme (30px), 4 on s'acrroche à un rebord (40px), on peut pas monter directement audessus de 40px
//      int maxStepWidth = 8;    // 6 si vitesse 0, 8 si vitesse de marche max
//      int maxIterations = 20000;
//      int iterations = 0;

//      //Logger.Info($"Start cell = {start.X},{start.Y} value={levelGrid[start.Y, start.X]}");
//      //Logger.Info($"Goal  cell = {goal.X},{goal.Y} value={levelGrid[goal.Y, goal.X]}");

//      while (openList.Count > 0)
//      {
//        iterations++;
//        if (iterations > maxIterations)
//        {
//          Logger.Info($"A*: exceeded max iterations ({maxIterations}). explored={closedList.Count} open={openList.Count}");
//          break;
//        }

//        Node current = openList.OrderBy(n => n.F).First();
//        if (current.Position == goal)
//        {
//          Logger.Info($"A*: path found with {closedList.Count} explored nodes and {iterations} iterations");
//          var path = ReconstructPath(current);
//          DebugPrintGridWithPath(path, start, goal);
//          //Logger.Info("Path: " + string.Join(" -> ", path.Select(p => $"({p.X},{p.Y})")));
//          return path;
//        }

//        openList.Remove(current);
//        closedList.Add(current.Position);

//        // --- 1️⃣ MOUVEMENT LATÉRAL (Gauche/Droite) ---
//        foreach (int dirX in new int[] { -1, 1 })
//        {
//          Point next = WrapPoint(new Point(current.Position.X + dirX, current.Position.Y));

//          if (!IsWalkable(next))
//            continue;

//          // Si on est dans un mur, ignore
//          if (IsSolid(current.Position.X, current.Position.Y))
//            continue;

//          // ✅ il faut un sol sous le voisin, sinon on "tombe"
//          if (IsSolid(next.X, next.Y + 1))
//          {
//            // voisin stable sur le sol
//            TryAddNode(current, next, goal, openList, closedList);
//          }
//          else
//          {
//            // sinon, on simule la chute
//            Point fall = SimulateFall(next);
//            if (IsInside(fall) && !IsSolid(fall.X, fall.Y))
//              TryAddNode(current, fall, goal, openList, closedList);
//          }
//        }

//        // --- 2️⃣ SAUT (depuis le sol) ---
//        if (IsSolid(current.Position.X, current.Position.Y + 1))
//        {
//          for (int dx = -maxStepWidth; dx <= maxStepWidth; dx++)
//          {
//            for (int dy = 1; dy <= maxJumpHeight; dy++)
//            {
//              Point jump = WrapPoint(new Point(current.Position.X + dx, current.Position.Y - dy));
//              if (!IsWalkable(jump))
//                continue;

//              // Simuler la chute après le saut (atterrissage)
//              Point fall = SimulateFall(jump);
//              if (!fall.Equals(jump))
//                jump = fall;

//              if (IsInside(jump))
//                TryAddNode(current, jump, goal, openList, closedList);
//            }
//          }
//        }

//        // --- 3️⃣ CHUTE (infinie jusqu’à sol) ---
//        if (!IsSolid(current.Position.X, current.Position.Y + 1))
//        {
//          Point fall = SimulateFall(current.Position);
//          if (!fall.Equals(current.Position))
//            TryAddNode(current, fall, goal, openList, closedList);
//        }
//      }

//      Logger.Info($"A*: no path found (explored={closedList.Count})");
//      return new List<Point>();
//    }

//    List<Point> ExpandPath(List<Point> coarsePath)
//    {
//      var expanded = new List<Point>();
//      if (coarsePath == null || coarsePath.Count == 0)
//        return expanded;

//      for (int i = 0; i < coarsePath.Count - 1; i++)
//      {
//        Point a = coarsePath[i];
//        Point b = coarsePath[i + 1];

//        // On calcule la direction (pas forcément diagonale parfaite)
//        int dx = Math.Sign(b.X - a.X);
//        int dy = Math.Sign(b.Y - a.Y);

//        Point current = a;
//        expanded.Add(current);

//        // On interpole jusqu’à atteindre b
//        while (current != b)
//        {
//          int nextX = current.X + dx;
//          int nextY = current.Y + dy;

//          // Wrap horizontal si besoin (comme ton pathfinding)
//          if (nextX < 0) nextX = LEVEL_WIDTH - 1;
//          if (nextX >= LEVEL_WIDTH) nextX = 0;
//          if (nextY < 0) nextY += LEVEL_HEIGHT;
//          if (nextY >= LEVEL_HEIGHT) nextY -= LEVEL_HEIGHT;

//          current = new Point(nextX, nextY);
//          expanded.Add(current);

//          // Sécurité : éviter boucle infinie
//          if (expanded.Count > 2000)
//            break;
//        }
//      }

//      // Ajoute le dernier point
//      expanded.Add(coarsePath.Last());
//      return expanded;
//    }


//    // ✅ Simule une chute verticale jusqu’à atteindre un sol
//    Point SimulateFall(Point start)
//    {
//      int x = start.X;
//      int y = start.Y;

//      for (int i = 0; i < LEVEL_HEIGHT; i++) // empêche boucle infinie
//      {
//        int belowY = (y + 1) % LEVEL_HEIGHT;
//        if (IsSolid(x, belowY))
//          return new Point(x, y);
//        y = belowY;
//      }

//      return start; // aucune collision trouvée (tout vide)
//    }

//    // ✅ Ajout du voisin dans A*
//    void TryAddNode(Node current, Point next, Point goal, List<Node> openList, HashSet<Point> closed)
//    {
//      if (closed.Contains(next))
//        return;

//      float newG = current.G + Distance(current.Position, next);
//      Node existing = openList.FirstOrDefault(n => n.Position == next);
//      if (existing == null)
//      {
//        openList.Add(new Node(next, newG, Heuristic(next, goal), current));
//      }
//      else if (newG < existing.G)
//      {
//        existing.G = newG;
//        existing.Parent = current;
//      }
//    }

//    // ✅ Wrap horizontal (écran bouclant)
//    Point WrapPoint(Point p)
//    {
//      if (p.X < 0) p.X = LEVEL_WIDTH - 1;
//      else if (p.X >= LEVEL_WIDTH) p.X = 0;
//      return p;
//    }

//    // ✅ Vérifie si on reste dans la carte
//    bool IsInside(Point p)
//    {
//      return p.X >= 0 && p.X < LEVEL_WIDTH && p.Y >= 0 && p.Y < LEVEL_HEIGHT;
//    }

//    int GetPathLength(Node node)
//    {
//      int len = 0;
//      while (node != null)
//      {
//        len++;
//        node = node.Parent;
//      }
//      return len;
//    }

///// ----------------- UTILITAIRES -----------------

//// Use consistent BLOCK_SIZE sampling & convert world position -> cell
//public Point WorldToCell(Vector2 pos)
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

//    Point WorldToCellCenterSample(Vector2 pos)
//    {
//      // If you prefer sampling by center, still same conversion for player pos
//      return WorldToCell(pos);
//    }

//    void DebugPrintGridWithPath(List<Point> path, Point start, Point goal)
//    {
//      // construction d'un set pour recherche rapide
//      var pathSet = new HashSet<(int x, int y)>();
//      if (path != null)
//        foreach (var p in path) pathSet.Add((p.X, p.Y));

//      for (int y = 0; y < LEVEL_HEIGHT; y++)
//      {
//        var sb = new System.Text.StringBuilder(LEVEL_WIDTH);
//        for (int x = 0; x < LEVEL_WIDTH; x++)
//        {
//          if (start.X == x && start.Y == y) { sb.Append('S'); continue; }
//          if (goal.X == x && goal.Y == y) { sb.Append('G'); continue; }
//          if (pathSet.Contains((x, y))) { sb.Append('x'); continue; }
//          sb.Append(levelGrid[y, x] == 1 ? '#' : '.');
//        }
//        Logger.Info(sb.ToString());
//      }
//    }

//    bool IsSolid(int gridX, int gridY)
//    {
//      // --- wrap horizontalement ---
//      if (gridX < 0) gridX += LEVEL_WIDTH;
//      if (gridX >= LEVEL_WIDTH) gridX -= LEVEL_WIDTH;

//      // --- wrap verticalement ---
//      if (gridY < 0) gridY += LEVEL_HEIGHT;
//      if (gridY >= LEVEL_HEIGHT) gridY -= LEVEL_HEIGHT;

//      return levelGrid[gridY, gridX] == 1;
//    }

//    bool IsWalkable(Point p)
//    {
//      if (!IsInside(p)) return false;
//      return levelGrid[p.Y, p.X] == 0;
//    }

//    // Find nearest walkable cell if start/goal fall into solid
//    Point FindNearestWalkable(Point p, int maxRadius = 6)
//    {
//      if (IsWalkable(p)) return p;
//      for (int r = 1; r <= maxRadius; r++)
//      {
//        for (int dy = -r; dy <= r; dy++)
//        {
//          for (int dx = -r; dx <= r; dx++)
//          {
//            var np = WrapPoint(new Point(p.X + dx, p.Y + dy));
//            if (IsInside(np) && IsWalkable(np))
//              return np;
//          }
//        }
//      }
//      // fallback: return original (may be solid)
//      return p;
//    }
//    /////////////////////////LATER
//    //void UpdatePerception(GameState state)
//    //{
//    //    visibleEnemies.Clear();
//    //    incomingProjectiles.Clear();

//    //    foreach (var enemy in state.Enemies)
//    //    {
//    //        if (IsVisible(enemy.Position))
//    //            visibleEnemies.Add(enemy);
//    //    }

//    //    foreach (var projectile in state.Projectiles)
//    //    {
//    //        if (WillHitMe(projectile))
//    //            incomingProjectiles.Add(projectile);
//    //    }
//    //    Detect nearby platforms
//    //    nearbyPlatforms = DetectPlatformsAround(player.Position);
//    //}
//  }
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
//    //HasShield
//    //HasWings
//    //List arrows
//    public Vector2 Speed = Vector2.Zero;

//    public PlayerInfo()
//    {
//      Reset();
//    }

//    public void Reset()
//    {
//      state = PlayerState.Idle;
//      X = 0;
//      Y = 0;
//      onGround = false;
//      GrabEdge = false;
//      CanWallJump = false;
//      Speed = Vector2.Zero;
//      Facing = Facing.Right;
//    }
//  }

//  enum PlayerState
//  {
//    Idle,
//    Moving,
//    Attacking,
//    Jumping,
//    Falling
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
