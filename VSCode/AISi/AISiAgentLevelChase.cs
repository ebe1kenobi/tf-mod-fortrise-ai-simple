//version poursuite pas mal a garder comme base https://chatgpt.com/c/690e70ba-9428-832a-a6db-eb815e160d67
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using IL.MonoMod;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TFModFortRiseLoaderAI;
using TowerFall;
using static TowerFall.Arrow;
using static TowerFall.Player;

namespace TFModFortRiseAiSimple
{
  public class AISiAgentLevelChase : TFModFortRiseLoaderAI.Agent
  {
    public int[,] levelGrid;
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

    // --- Bibliothèque de mouvements disponibles ---
    public List<MovementAction> movementLibrary;
    // --- Variables IA (à placer en haut de ta classe) ---
    private Queue<MovementAction> currentActions = new Queue<MovementAction>();
    //private MovementAction currentAction = null;
    private float actionTimer = 0f;

    private bool testMode = false;
    //public string testActionName = "None"; // Change ici pour tester une autre action
    public string testActionName = "jumplefetsurblocm1m9"; // Change ici pour tester une autre action
    //public float testActionX = 305;
    //public float testActionY = 222;
    public float testActionX = 305;
    public float testActionY = 222;
    public int testNone = 0;

    private float testPauseTimer = 0f;
    private const float TEST_PAUSE_DURATION = 1f; // durée de pause en secondes

    private MovementAction currentAction = null;
    private int currentPhaseIndex = 0;
    private float phaseTimer = 0f;
    //private bool testMode = false;

    // Variables globales à ajouter en haut de la classe :
    private float pathRecalcTimer = 0f;
    private const float PATH_RECALC_INTERVAL = 0.25f; //0.25f; // secondes
    private List<Point> currentPath = null;
    private int currentPathIndex = 0;
    private Point lastGoal = new Point(-1, -1);

    private bool isJumping = false;
    private int ledgeCooldown = 0;
    private bool ledgeJump = false;
    private int ledgeJumpCooldown = 0;
    private int ledgeJumpDir = 0;

    private const int MAX_JUMP_HEIGHT = 3;   // cases max qu’on peut sauter
    private const int MAX_FALL_HEIGHT = 50;  // cases max qu’on peut tomber

    // --- Variables à ajouter en haut de la classe ---
    private int shootState = 0; // 0 = idle, 1 = preparing, 2 = shooting, 3 = cooldown
    private int shootFrameCounter = 0;
    private Vector2 shootDirection = Vector2.Zero;
    private const int SHOOT_HOLD_FRAMES = 2;
    private const int SHOOT_COOLDOWN_FRAMES = 5;
    // Variables supplémentaires à mettre en haut de la classe
    private float shootCooldownTimer = 0f;
    private const float SHOOT_COOLDOWN = 0.25f; // secondes entre deux tirs

    private const int ARROW_CATCH_RANGE = 1; // nombre de cases autour du joueur pour tenter le catch


    public List<Point> debugPath = new List<Point>();
    private const float DEBUG_CELL_SIZE = 10f; // correspond à BLOCK_SIZE

    public Player enemy;
    public Player player;
    private PlayerInfo playerInfo = new PlayerInfo();
    private List<ArrowInfo> arrows = new List<ArrowInfo>();
    private PlayerInfo enemyInfo = new PlayerInfo();
    public AISiAgentLevelChase(int index, String type, PlayerInput input) : base(index, type, input)
    {
      playerInfo = new PlayerInfo();
      enemyInfo = new PlayerInfo();
    }

    public int getIndex()
    {
      return index;
    }

    private void UpdateMiasmaGrid()
    {

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
          levelGrid[y, x] = level.CollideCheck(new Vector2(worldX, worldY), GameTags.JumpPad) ? 2 : levelGrid[y, x];
          levelGrid[y, x] = level.CollideCheck(new Vector2(worldX, worldY), GameTags.TreasureChest) ? 3 : levelGrid[y, x];
          levelGrid[y, x] = level.CollideCheck(new Vector2(worldX, worldY), GameTags.LavaCollider) ? 4 : levelGrid[y, x];
          levelGrid[y, x] = level.CollideCheck(new Vector2(worldX, worldY), GameTags.Brambles) ? 5 : levelGrid[y, x];
          //levelGrid[y, x] = level.CollideCheck(new Vector2(worldX, worldY), GameTags.Solid) ? 1 : 0;
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

    private void DebugPrintPath()
    {
      if (levelGrid == null) return;

      StringWriter output = new StringWriter();

      for (int y = 0; y < LEVEL_HEIGHT; y++)
      {
        string line = "";
        for (int x = 0; x < LEVEL_WIDTH; x++)
        {
          // Détection du contenu de la cellule
          char cellChar = ' ';

          switch (levelGrid[y, x])
          {
            case 1: cellChar = '#'; break; // Mur
            case 2: cellChar = 'J'; break; // JumpPad
            case 3: cellChar = 'T'; break; // Treasure
            case 4: cellChar = 'L'; break; // Lava
            case 5: cellChar = 'B'; break; // Brambles
            default: cellChar = '.'; break; // Case libre
          }

          // --- Superposition d'informations supplémentaires ---
          // Position du joueur
          if (playerInfo.X == x && playerInfo.Y == y)
            cellChar = 'P';

          // Position de l’ennemi
          else if (enemyInfo.X == x && enemyInfo.Y == y)
            cellChar = 'E';

          // Chemin de debug
          else if (debugPath != null && debugPath.Any(p => p.X == x && p.Y == y))
            cellChar = '*';

          line += cellChar;
        }

        Logger.Info(line);
      }

      Logger.Info("=== DEBUG PATH GRID ===");
      Logger.Info(output.ToString());
    }

    public override void Reset()
    {
      //calculate levelGrid
      UpdateLevelGrid();
      DebugPrintGrid();
    }

    public override void Move()
    {
      UpdatePerception();

      // --- MODE TEST ---
      if (testMode)
      {
        if (currentAction == null || currentAction.Name == "None")
        {
          TestMovementAction(testActionName);
          currentPhaseIndex = 0;
          if (currentAction != null && currentAction.Phases.Count > 0)
            phaseTimer = currentAction.Phases[currentPhaseIndex].Duration;
        }

        if (currentAction != null)
        {
          MovementPhase phase = currentAction.Phases[currentPhaseIndex];
          phaseTimer -= Engine.DeltaTime;

          ApplyPhaseInputs(phase);
          if (phase.IsFinished(this, currentAction.StartPoint, currentAction.StartPosition, phaseTimer))
          {
            currentPhaseIndex++;
            if (currentPhaseIndex >= currentAction.Phases.Count)
            {
              currentAction = null;
              testPauseTimer = TEST_PAUSE_DURATION;
            }
            else
            {
              phaseTimer = currentAction.Phases[currentPhaseIndex].Duration;
            }
          }
        }
        return;
      }

      // --- SI UNE ACTION EST EN COURS ---
      if (currentAction != null)
      {
        MovementPhase phase = currentAction.Phases[currentPhaseIndex];
        phaseTimer -= Engine.DeltaTime;

        ApplyPhaseInputs(phase);

        if (phase.IsFinished(this, currentAction.StartPoint, currentAction.StartPosition, phaseTimer))
        {
          currentPhaseIndex++;
          if (currentPhaseIndex >= currentAction.Phases.Count)
          {
            currentAction = null;
          }
          else
          {
            phaseTimer = currentAction.Phases[currentPhaseIndex].Duration;
          }
        }
      }

      // --- RECALCUL D’ACTIONS TOUTES LES PATH_RECALC_INTERVAL SECONDES ---
      pathRecalcTimer += Engine.DeltaTime;
      if (pathRecalcTimer >= PATH_RECALC_INTERVAL)
      {
        pathRecalcTimer = 0f;

        // Recalcule la séquence d’actions même si currentActions n’est pas vide
        ComputeNewActionSequence();
      }

      // --- LANCER LA PROCHAINE ACTION DE LA SÉQUENCE ---
      if (currentAction == null && currentActions.Count > 0)
      {
        currentAction = currentActions.Dequeue();
        Logger.Info($"currentAction = {currentAction.Name}");
        currentAction.StartPoint = WorldToCell(player.Position);
        currentAction.StartPosition = player.Position;
        currentPhaseIndex = 0;
        if (currentAction.Phases.Count > 0)
          phaseTimer = currentAction.Phases[currentPhaseIndex].Duration;

        ApplyPhaseInputs(currentAction.Phases[currentPhaseIndex]);
      }
    }

    //public override void Move()
    //{
    //  UpdatePerception();

    //  // --- MODE TEST ---
    //  if (testMode)
    //  {
    //    // Si on n'a pas d'action en cours
    //    if (currentAction == null || currentAction.Name == "None")
    //    {
    //      // Si pause active, on attend
    //      //if (testPauseTimer > 0)
    //      //{
    //      //  TestMovementAction("None");
    //      //  testPauseTimer -= Engine.DeltaTime;
    //      //  return;
    //      //}

    //      // Lance le test du mouvement
    //      TestMovementAction(testActionName);
    //      currentPhaseIndex = 0;
    //      if (currentAction != null && currentAction.Phases.Count > 0)
    //        phaseTimer = currentAction.Phases[currentPhaseIndex].Duration;
    //    }

    //    if (currentAction != null)
    //    {
    //      MovementPhase phase = currentAction.Phases[currentPhaseIndex];
    //      phaseTimer -= Engine.DeltaTime;

    //      ApplyPhaseInputs(phase);
    //      if (phase.IsFinished(this, currentAction.StartPoint, currentAction.StartPosition, phaseTimer))
    //      {
    //        currentPhaseIndex++;
    //        if (currentPhaseIndex >= currentAction.Phases.Count)
    //        {
    //          //Logger.Info($"✅ Fin du mouvement {currentAction.Name}");
    //          currentAction = null;
    //          testPauseTimer = TEST_PAUSE_DURATION; // démarre la pause avant le prochain test
    //        }
    //        else
    //        {
    //          phaseTimer = currentAction.Phases[currentPhaseIndex].Duration;
    //        }
    //      }
    //    }
    //    return;
    //  }

    //  // --- SI UNE ACTION EST EN COURS ---
    //  if (currentAction != null)
    //  {
    //    MovementPhase phase = currentAction.Phases[currentPhaseIndex];
    //    phaseTimer -= Engine.DeltaTime;

    //    ApplyPhaseInputs(phase);

    //    if (phase.IsFinished(this, currentAction.StartPoint, currentAction.StartPosition, phaseTimer))
    //    {
    //      currentPhaseIndex++;
    //      Logger.Info($"currentphase = {currentPhaseIndex}");
    //      if (currentPhaseIndex >= currentAction.Phases.Count)
    //      {
    //        currentAction = null; // mouvement terminé
    //      }
    //      else
    //      {
    //        phaseTimer = currentAction.Phases[currentPhaseIndex].Duration;
    //      }
    //    }
    //    return; // ne fait rien d'autre tant que le mouvement n'est pas fini
    //  }

    //  // --- RECALCUL D’ACTIONS SI NÉCESSAIRE ---
    //  pathRecalcTimer += Engine.DeltaTime;
    //  if (pathRecalcTimer >= PATH_RECALC_INTERVAL || currentActions.Count == 0)
    //  {
    //    pathRecalcTimer = 0f;
    //    ComputeNewActionSequence(); // génère une nouvelle séquence d'actions vers la cible
    //  }

    //  // --- LANCER LA PROCHAINE ACTION DE LA SÉQUENCE ---
    //  if (currentActions.Count > 0 && currentAction == null)
    //  {
    //    currentAction = currentActions.Dequeue();
    //    currentAction.StartPoint = WorldToCell(player.Position);
    //    currentAction.StartPosition = player.Position;
    //    Logger.Info($"currentaction = {currentAction.Name}");
    //    currentPhaseIndex = 0;
    //    Logger.Info($"currentphase = {currentPhaseIndex}");
    //    if (currentAction.Phases.Count > 0)
    //      phaseTimer = currentAction.Phases[currentPhaseIndex].Duration;

    //    // applique directement les inputs de la première phase
    //    ApplyPhaseInputs(currentAction.Phases[currentPhaseIndex]);
    //  }
    //}

    // --- Nouvelle fonction : calcul du chemin en actions physiques ---
    private void ComputeNewActionSequence()
    {
      Point start = new Point(playerInfo.X, playerInfo.Y);
      Point goal = new Point(enemyInfo.X, enemyInfo.Y);

      // Trouve un chemin de cellules basé sur les mouvements possibles
      List<Point> path = FindPathUsingMovementLibrary(start, goal);
      this.debugPath = path != null ? new List<Point>(path) : new List<Point>();
      if (path == null || path.Count == 0)
        return;

      currentActions.Clear();

      // Convertit le path en une séquence d’actions
      for (int i = 1; i < path.Count; i++)
      {
        Point from = path[i - 1];
        Point to = path[i];
        MovementAction act = FindActionLeadingTo(from, to);
        if (act != null)
          currentActions.Enqueue(act);
      }
    }


    // --- Fonction pour retrouver quelle action correspond à un déplacement ---
    private MovementAction FindActionLeadingTo(Point from, Point to)
    {
      foreach (var move in movementLibrary)
      {
        List<Point> possible = move.ResultPositions(from, this);
        if (possible.Any(p => p == to))
          return move;
      }
      return null;
    }


    // --- Application des inputs physiques selon l’action ---
    private void ApplyPhaseInputs(MovementPhase phase)
    {
      this.input.inputState.MoveX = phase.MoveX;
      this.input.inputState.MoveY = phase.MoveY;
      this.input.inputState.JumpCheck = phase.Jump;
      this.input.inputState.JumpPressed = phase.Jump && !this.input.prevInputState.JumpCheck;
      this.input.inputState.DodgeCheck = phase.Dash;
      this.input.inputState.DodgePressed = phase.Dash && !this.input.prevInputState.DodgeCheck;
      this.input.inputState.AimAxis = new Vector2(phase.MoveX, phase.MoveY);

    }
    private void HandleArrowCatch()
    {
      if (player == null || arrows == null || arrows.Count == 0) return;

      // Parcours toutes les flèches
      foreach (var arrow in arrows)
      {
        // On ne tente de rattraper que si la flèche est encore en vol
        if (arrow.state == ArrowStates.Shooting ||
            arrow.state == ArrowStates.Drilling ||
            arrow.state == ArrowStates.Gravity ||
            arrow.state == ArrowStates.Falling)
        {
          // Calculer direction relative de la flèche par rapport au joueur
          Vector2 toPlayer = player.Position - arrow.Position;

          // Vérifier si la flèche va vers le joueur
          if (Vector2.Dot(toPlayer, arrow.Speed) > 0)
          {
            // Vérifier la proximité (X et Y en case)
            Point arrowCell = new Point(arrow.X, arrow.Y);
            Point playerCell = new Point(playerInfo.X, playerInfo.Y);

            if (Math.Abs(arrowCell.X - playerCell.X) <= ARROW_CATCH_RANGE &&
                Math.Abs(arrowCell.Y - playerCell.Y) <= ARROW_CATCH_RANGE)
            {
              // Activer le catch
              this.input.inputState.DodgeCheck = true;
              this.input.inputState.DodgePressed = !this.input.prevInputState.DodgeCheck;
              return; // on catch une flèche à la fois
            }
          }
        }
      }

      // Sinon, pas de flèche à attraper
      this.input.inputState.DodgeCheck = false;
      this.input.inputState.DodgePressed = false;
    }


    // --- Méthode pour gérer le tir ---
    private void HandleShooting()
    {
      if (player == null || enemy == null || playerInfo.NbArrows <= 0) return;

      // Cooldown entre les tirs
      shootCooldownTimer += Engine.DeltaTime;
      if (shootCooldownTimer < SHOOT_COOLDOWN && shootState == 0) return;

      // Calcul direction vers l'ennemi
      Vector2 dir = enemy.Position - player.Position;
      if (dir != Vector2.Zero) dir.Normalize();
      shootDirection = dir;

      // Vérifier la ligne de visée
      if (!CanShootLineOfSight(new Point(playerInfo.X, playerInfo.Y), new Point(enemyInfo.X, enemyInfo.Y)))
        return; // mur sur le trajet, ne pas tirer

      // Déterminer le type de tir et vérifier la portée
      float deltaX = enemyInfo.X - playerInfo.X;
      float deltaY = enemyInfo.Y - playerInfo.Y;
      bool canShoot = false;

      // Tir horizontal
      if (Math.Abs(deltaY) <= 1 && Math.Abs(deltaX) <= 13) canShoot = true;

      // Tir vertical haut
      else if (deltaX == 0 && deltaY < 0 && Math.Abs(deltaY) <= 13) canShoot = true;

      // Tir diagonale haut
      else if (deltaX != 0 && deltaY < 0)
      {
        int maxDiagonalX = 17;
        int maxDiagonalY = 9; // flèche atteint Y+9 avant de retomber
        if (Math.Abs(deltaX) <= maxDiagonalX && Math.Abs(deltaY) <= maxDiagonalY) canShoot = true;
      }

      if (!canShoot) return; // hors portée, ne pas tirer

      // --- Cycle multi-frame ---
      if (shootState == 0)
      {
        shootState = 1; // préparer le tir
        shootFrameCounter = 0;
        shootCooldownTimer = 0f; // reset cooldown
      }

      if (shootState == 1) // préparer
      {
        shootFrameCounter++;
        this.input.inputState.AimAxis = shootDirection;
        this.input.inputState.ShootCheck = true;
        this.input.inputState.ShootPressed = true;

        if (shootFrameCounter >= SHOOT_HOLD_FRAMES)
        {
          shootState = 2;
          shootFrameCounter = 0;
        }
      }
      else if (shootState == 2) // relâchement
      {
        shootFrameCounter++;
        this.input.inputState.AimAxis = shootDirection;
        this.input.inputState.ShootCheck = false;
        this.input.inputState.ShootPressed = false;

        if (shootFrameCounter >= SHOOT_COOLDOWN_FRAMES)
        {
          shootState = 0; // prêt pour prochain tir
          shootFrameCounter = 0;
          playerInfo.NbArrows--; // décrémente le nombre de flèches
        }
      }
    }

    // --- Méthode pour vérifier si le tir est possible sans mur ---
    private bool CanShootLineOfSight(Point start, Point end)
    {
      int x0 = start.X;
      int y0 = start.Y;
      int x1 = end.X;
      int y1 = end.Y;

      int dx = Math.Abs(x1 - x0);
      int dy = Math.Abs(y1 - y0);
      int sx = x0 < x1 ? 1 : -1;
      int sy = y0 < y1 ? 1 : -1;
      int err = dx - dy;

      while (true)
      {
        // Vérifie la cellule actuelle
        if (!IsCellWalkable(x0, y0)) return false;

        if (x0 == x1 && y0 == y1) break;

        int e2 = 2 * err;
        if (e2 > -dy)
        {
          err -= dy;
          x0 += sx;
        }
        if (e2 < dx)
        {
          err += dx;
          y0 += sy;
        }
      }

      return true; // pas de mur sur la ligne
    }
    public bool IsCellWalkable(int x, int y)
    {
      if (x < 0 || y < 0 || x >= LEVEL_WIDTH || y >= LEVEL_HEIGHT)
        return false;
      return levelGrid[y, x] == 0; // 0 = vide, 1+ = obstacle
    }

    public bool IsCellsWalkable(int fromX, int toX, int fromY, int toY)
    {
      // On s’assure que les indices sont dans les bornes du niveau
      // Permet de gérer un intervalle dans les deux sens (gauche ou droite)
      int startX = Math.Min(fromX, toX);
      int endX = Math.Max(fromX, toX);
      int startY = Math.Min(fromY, toY);
      int endY = Math.Max(fromY, toY);

      // On parcourt toutes les cellules entre les deux X
      for (int x = startX; x <= endX; x++)
      {
        for (int y = startY; y <= endY; y++)
        {
          // Si la case est un mur, ce n’est pas walkable
          if (!IsCellWalkable(x, y))
            return false;
        }
        // Optionnel : vérifier aussi qu’il y a un sol dessous
        // si tu veux t’assurer qu’on ne traverse pas un vide
        // if (!HasGroundBelow(x, y))
        //     return false;
      }

      return true;
    }

    public bool IsSolid(int x, int y)
    {
      if (x < 0 || y < 0 || x >= LEVEL_WIDTH || y >= LEVEL_HEIGHT)
        return false;
      return levelGrid[y, x] == 1; // 0 = vide, 1+ = obstacle
    }

    public bool IsSolid(int fromX, int toX, int y)
    {
      //Logger.Info($"IsGroundsWalkable {fromX} to {toX} y={y}");
      // On s’assure que les indices sont dans les bornes du niveau
      // Permet de gérer un intervalle dans les deux sens (gauche ou droite)
      int start = Math.Min(fromX, toX);
      int end = Math.Max(fromX, toX);

      // On parcourt toutes les cellules entre les deux X
      for (int x = start; x <= end; x++)
      {
        // Si la case est un mur, ce n’est pas walkable
        //Logger.Info($"IsGroundWalkable({x}, {y}) = {IsGroundWalkable(x, y)}");
        if (!IsSolid(x, y))
          return false;

        // Optionnel : vérifier aussi qu’il y a un sol dessous
        // si tu veux t’assurer qu’on ne traverse pas un vide
        // if (!HasGroundBelow(x, y))
        //     return false;
      }

      return true;
    }

    public bool IsSolid(int fromX, int toX, int fromY, int toY)
    {
      // On s’assure que les indices sont dans les bornes du niveau
      // Permet de gérer un intervalle dans les deux sens (gauche ou droite)
      //int startX = Math.Min(fromX, toX);
      //int endX = Math.Max(fromX, toX);
      int startY = Math.Min(fromY, toY);
      int endY = Math.Max(fromY, toY);

      // On parcourt toutes les cellules entre les deux X
      //for (int x = startX; x <= endX; x++)
      //{
        for (int y = startY; y <= endY; y++)
        {
          // Si la case est un mur, ce n’est pas walkable
          if (!IsSolid(fromX, toX, y))
            return false;
        }
        // Optionnel : vérifier aussi qu’il y a un sol dessous
        // si tu veux t’assurer qu’on ne traverse pas un vide
        // if (!HasGroundBelow(x, y))
        //     return false;
      //}

      return true;
    }

    public bool IsAreaFree(int fromX, int toX, int fromY, int toY)
    {
      int startX = Math.Min(fromX, toX);
      int endX = Math.Max(fromX, toX);
      int startY = Math.Min(fromY, toY);
      int endY = Math.Max(fromY, toY);

      for (int x = startX; x <= endX; x++)
      {
        for (int y = startY; y <= endY; y++)
        {
          if (IsSolid(x, y))
            return false; // il y a un mur -> pas libre
        }
      }
      return true; // toutes les cases sont libres
    }

    private List<Point> FindPathUsingMovementLibrary(Point start, Point goal)
    {
      if (movementLibrary == null)
        InitMovementLibrary();

      var open = new List<Node>();
      var closed = new HashSet<Point>();

      Node startNode = new Node(start, 0, Heuristic(start, goal), null) { PhaseIndex = 0 };
      open.Add(startNode);

      int safety = 0;
      const int MAX_ITER = 8000;

      while (open.Count > 0 && safety++ < MAX_ITER)
      {
        //Logger.Info("boucle ITER " + safety);
        // Trier selon le coût total F = G + H
        open.Sort((a, b) => a.F.CompareTo(b.F));
        Node current = open[0];
        open.RemoveAt(0);

        // Objectif atteint
        if (current.Position.Equals(goal))
        {
          var foundPath = ReconstructPath(current);
          this.debugPath = foundPath != null ? new List<Point>(foundPath) : new List<Point>();
          return foundPath;
        }

        if (closed.Contains(current.Position)) continue;
        closed.Add(current.Position);

        foreach (var move in movementLibrary)
        {
          //Logger.Info("move " + move.Name);

          if (!move.Condition(current.Position, this)) continue;

          //Logger.Info("condition ok");

          // Récupère toutes les positions possibles pour ce mouvement
          List<Point> possibleDestinations = move.ResultPositions != null
              ? move.ResultPositions(current.Position, this)
              : new List<Point> { new Point(0, 0) };

          foreach (Point dest in possibleDestinations)
          {
            Point d = dest;
            //Logger.Info("dest " + d.X + "," + d.Y);   
            // Vérifier les limites
            if (d.X < 0 || d.X >= LEVEL_WIDTH || d.Y < 0 || d.Y >= LEVEL_HEIGHT)
              continue;

            if (!HasGroundBelow(d.X, d.Y)) d = SimulateFall(d);

            // Ne pas se poser sur un mur
            if (!IsCellWalkable(d.X, d.Y)) continue;  //todo comment to have multiple option of movement
            //Logger.Info("IsCellWalkable true");

            // Déjà visité
            if (closed.Contains(d)) continue;
            //Logger.Info("pas deja visité");

            float g = current.G + move.Cost;
            Node existing = open.FirstOrDefault(n => n.Position.Equals(d));
            if (existing == null)
            {
              Node newNode = new Node(d, g, Heuristic(d, goal), current) { PhaseIndex = 0 };
              open.Add(newNode);
            }
            else if (g < existing.G)
            {
              existing.G = g;
              existing.Parent = current;
            }
          }
        }
      }

      return null; // pas de chemin trouvé
    }




    private float Heuristic(Point a, Point b)
    {
      return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
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


    void UpdatePerception()
    {
      UpdateLevelGrid();
      //UpdateMiasmaGrid();
      //Lava
      //Miasma
      //MoonGlassBlock
      //MovingPlatform
      //ProximityBlock
      //ShiftBlock
      //Spikeball
      //SwitchBlock
      //TreasureChest
      //UpdateMovingBlockGrid();   // CrackedPlatform  CrackedWall  CrumbleBlock CrumbleWall  GraniteBlock  LoopPlatform 
      //JumpPad
      //UpdatePlatformTraversableGrid();
      //DebugPrintGrid();
      int playerIndex = index;
      player = level.GetPlayer(index); //todo check 
                                       //search first enemy
      int enemyIndex = index == 0 ? 1 : 0;
      enemy = level.GetPlayer(index == 0 ? 1 : 0);  //todo , test for 2 players only
      if (player != null)
      {
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
      UpdateArrowInfo();
    }

    void UpdatePlayerInfo(Player player, PlayerInfo playerInfo)
    {
      var dynData = DynamicData.For(player);

      Point cell = WorldToCell(player.Position);
      playerInfo.X = cell.X;
      playerInfo.Y = cell.Y;
      //playerInfo.X = (int)player.Position.X / 5;
      //playerInfo.Y = (int)player.Position.Y / 5;
      playerInfo.onGround = dynData.Get<bool>("OnGround");
      playerInfo.GrabEdge = dynData.Get<PlayerStates>("State") == PlayerStates.LedgeGrab;
      playerInfo.Speed = player.Speed;
      playerInfo.NbArrows = player.Arrows.Count;
      //if (0 == dynData.Get<int>("PlayerIndex"))
      //Logger.Info(playerInfo.Speed.X.ToString());
      playerInfo.CanWallJump = dynData.Invoke<bool>("CanWallJump", Facing.Left) || dynData.Invoke<bool>("CanWallJump", Facing.Right);
      dynData.Dispose();
    }

    void UpdateArrowInfo()
    {
      arrows.Clear();
      foreach (Arrow arrow in level[GameTags.Arrow])
      {
        ArrowInfo arrowInfo = new ArrowInfo();
        arrowInfo.state = arrow.State;
        arrowInfo.Position = arrow.Position;
        arrowInfo.Speed = arrow.Speed;
        Point cell = WorldToCell(arrow.Position);
        arrowInfo.X = cell.X;
        arrowInfo.Y = cell.Y;
        arrows.Add(arrowInfo);
      }
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



    public void TestMovementAction(string actionName)
    {
      if (movementLibrary == null)
        InitMovementLibrary();
      // Cherche le mouvement dans la bibliothèque
      MovementAction action = movementLibrary.FirstOrDefault(a => a.Name == actionName);
      if (action == null)
      {
        Logger.Info($"❌ Mouvement {actionName} introuvable !");
        return;
      }

      //Logger.Info($"▶️ Test du mouvement : {action.Name}");

      // Réinitialise les timers et files
      currentActions.Clear();
      currentAction = action;
      currentAction.StartPoint = WorldToCell(player.Position);
      currentAction.StartPosition = player.Position;
      currentPhaseIndex = 0;

      // Initialise le timer sur la première phase si elle existe
      if (currentAction.Phases.Count > 0)
        phaseTimer = currentAction.Phases[currentPhaseIndex].Duration;

      // Place le joueur au point de départ "propre"
      if (player != null)
      {
        player.Speed = Vector2.Zero;
        if (actionName != "None")
        {
          player.Position = new Vector2(testActionX, testActionY); // ajuster selon le test
          currentAction.StartPoint = WorldToCell(player.Position);
          currentAction.StartPosition = player.Position;
        }
      }

      // Force le test à commencer : applique les inputs de la première phase
      if (currentAction.Phases.Count > 0)
        ApplyPhaseInputs(currentAction.Phases[currentPhaseIndex]);
    }

    // --- Description d’un mouvement possible ---
    public class MovementPhase
    {
      public float Duration;
      public int MoveX;
      public int MoveY;
      public bool Jump;
      public bool Dash;
      public Vector2 AimAxis;
      public Func<AISiAgentLevelChase, Point, Vector2, bool> EndCondition; // <- modifié

      public MovementPhase(
          float duration,
          int moveX = 0,
          int moveY = 0,
          bool jump = false,
          bool dash = false,
          Func<AISiAgentLevelChase, Point, Vector2, bool> endCondition = null)
      {
        Duration = duration;
        MoveX = moveX;
        MoveY = moveY;
        Jump = jump;
        Dash = dash;
        AimAxis = Vector2.Zero;
        EndCondition = endCondition;
      }

      public bool IsFinished(AISiAgentLevelChase ai, Point startPoint, Vector2 startPosition, float timer)
      {
        if (EndCondition != null && EndCondition(ai, startPoint, startPosition))
          return true;
        //Logger.Info($"{timer}");
        return timer <= 0f;
      }
    }




    public class MovementAction
    {
      public string Name;                        // nom du mouvement pour debug
      public List<MovementPhase> Phases;         // phases du mouvement
      public Func<Point, AISiAgentLevelChase, bool> Condition; // condition pour lancer le mouvement
      public Func<Point, AISiAgentLevelChase, List<Point>> ResultPositions;   // position finale approximative
      public float Cost;                          // coût du mouvement (temps total)
                                                  // Nouvelle propriété :
      public Point StartPoint;
      public Vector2 StartPosition;

      public MovementAction(string name)
      {
        Name = name;
        Phases = new List<MovementPhase>();
      }

      public MovementAction AddPhase(MovementPhase phase)
      {
        Phases.Add(phase);
        return this;
      }

      // calcule le coût total du mouvement (par défaut somme des durées)
      public void CalculateCost()
      {
        Cost = 0f;
        foreach (var phase in Phases)
        {
          Cost += phase.Duration;
        }
      }
    }



    private void InitMovementLibrary()
    {
      movementLibrary = new List<MovementAction>();
      ////////////////////////
      //var action = new MovementAction("None")
      //    .AddPhase(new MovementPhase(1f));  // ne pas bouger
      //action.CalculateCost();
      //action.Condition = (pos, ai) => true;
      //action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X, pos.Y) };
      //movementLibrary.Add(action);
      //////////////////////// ok
      var action = new MovementAction("leftm1")
          .AddPhase(new MovementPhase(1f, moveX: -1, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.X == startPoint.X - 1)) // ne pas bouger
              .AddPhase(new MovementPhase(1f, moveX: 0))
      ;  // ne pas bouger
      action.CalculateCost();
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) && IsSolid(pos.X - 1, pos.Y + 1) && !IsSolid(pos.X - 1, pos.Y);
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 1, pos.Y) };
      movementLibrary.Add(action);
      //////////////////////// ok
      action = new MovementAction("rightp1")
          .AddPhase(new MovementPhase(1f, moveX: 1, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.X == startPoint.X + 1))  // ne pas bouger
              .AddPhase(new MovementPhase(1f, moveX: 0))
      ;// ne pas bouger
      action.CalculateCost();
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) && IsSolid(pos.X + 1, pos.Y + 1) && !IsSolid(pos.X + 1, pos.Y);
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X + 1, pos.Y) };
      movementLibrary.Add(action);
      /////////////////////// ok
      action = new MovementAction("jumplefetholem1m0")
          .AddPhase(new MovementPhase(0.03f, moveX: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.2f, moveX: -1, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X - 1, pos.Y, pos.Y - 2) //air libre pour sauté
                                      && IsSolid(pos.X - 1, pos.Y + 1)//sol a l arrivé
            ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 1, pos.Y) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      /////////////////////// ok
      action = new MovementAction("jumplefetholem2m0")
          .AddPhase(new MovementPhase(0.1f, moveX: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.2f, moveX: -1, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X - 2, pos.Y, pos.Y - 3)  //air libre pour sauté
                                      && IsSolid(pos.X - 2, pos.Y + 1) //sol a l arrivé
            ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 2, pos.Y) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      /////////////////////// ok quand mur a pos.Y - 3 alors ledgegrab , ajouté phase saut
      action = new MovementAction("jumplefetholem2m0ledge")
          .AddPhase(new MovementPhase(0.1f, moveX: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.2f, moveX: -1, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.GrabEdge))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.2f, moveX: -1))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.2f, moveX: -1, jump: true, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X - 1, pos.Y, pos.Y - 1)  //air libre pour sauté
                                      && IsAreaFree(pos.X, pos.X - 1, pos.Y, pos.Y - 2)  //air libre pour sauté
                                      && IsSolid(pos.X - 2, pos.Y + 1) //sol a l arrivé
            ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 2, pos.Y) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      /////////////////////// ok
      action = new MovementAction("jumplefetholem3m0")
          .AddPhase(new MovementPhase(0.15f, moveX: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.2f, moveX: -1, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X - 3, pos.Y, pos.Y - 3)  //air libre pour sauté
                                      && IsSolid(pos.X - 3, pos.Y + 1) //sol a l arrivé
            ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 3, pos.Y) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      /////////////////////// ok
      action = new MovementAction("jumplefetholem4m0")
          .AddPhase(new MovementPhase(0.5f, moveX: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.2f, moveX: -1, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X - 4, pos.Y, pos.Y - 3)  //air libre pour sauté
                                      && IsSolid(pos.X - 4, pos.Y + 1) //sol a l arrivé
            ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 4, pos.Y) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      /////////////////////// ok
      action = new MovementAction("jumplefetholem5m0")
          .AddPhase(new MovementPhase(0.37f, moveX: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.2f, moveX: -1, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X - 5, pos.Y, pos.Y - 4)  //air libre pour sauté
                                      && IsSolid(pos.X - 5, pos.Y + 1) //sol a l arrivé
            ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 5, pos.Y) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      ///////////////////////  ok
      action = new MovementAction("jumplefetholem6m0")
          .AddPhase(new MovementPhase(0.75f, moveX: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.5f, moveX: -1, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X - 6, pos.Y, pos.Y - 4)  //air libre pour sauté
                                      && IsSolid(pos.X - 6, pos.Y + 1) //sol a l arrivé
            ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 6, pos.Y) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      ///////////////////////  ok
      action = new MovementAction("jumplefetholem7m0")
          .AddPhase(new MovementPhase(0.3f, moveX: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.4f, moveX: -1, dash: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.5f, moveX: -1, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X - 7, pos.Y, pos.Y - 4)  //air libre pour sauté
                                      && IsSolid(pos.X - 7, pos.Y + 1) //sol a l arrivé
            ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 7, pos.Y) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      ///////////////////////  ok
      action = new MovementAction("jumplefetholem7m0dash")
          .AddPhase(new MovementPhase(0.3f, moveX: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.1f, moveX: -1, dash: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.001f, moveX: -1))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.28f, moveX: -1, dash: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.5f, moveX: -1, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X - 7, pos.Y, pos.Y - 3)  //air libre pour sauté
                                      && IsSolid(pos.X - 7, pos.Y + 1) //sol a l arrivé
            ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 7, pos.Y) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      ///////////////////////  ok
      action = new MovementAction("jumplefetholem8m0dash")
          .AddPhase(new MovementPhase(0.3f, moveX: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.05f, moveX: -1, dash: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.001f, moveX: -1))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.24f, moveX: -1, dash: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.5f, moveX: -1, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X - 8, pos.Y, pos.Y - 3)  //air libre pour sauté
                                      && IsSolid(pos.X - 8, pos.Y + 1) //sol a l arrivé
            ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 8, pos.Y) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      /////////////////////// ok
      action = new MovementAction("jumplefetholem9m0dash")
          .AddPhase(new MovementPhase(0.3f, moveX: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.001f, moveX: -1, dash: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.001f, moveX: -1))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.001f, moveX: -1, dash: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.5f, moveX: -1, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X - 9, pos.Y, pos.Y - 3)  //air libre pour sauté
                                      && IsSolid(pos.X - 9, pos.Y + 1) //sol a l arrivé
            ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 9, pos.Y) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      /////////////////////// ok
      action = new MovementAction("jumplefetsurblocm1m1")
          .AddPhase(new MovementPhase(0.05f, moveX: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.5f, moveX: -1, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X, pos.Y, pos.Y - 2)  //air libre pour sauté
                                      && IsAreaFree(pos.X - 1, pos.X - 1, pos.Y - 1, pos.Y - 2)  //air libre pour sauté
                                      && IsSolid(pos.X - 1, pos.Y) //sol a l arrivé
            ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 1, pos.Y - 1) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      /////////////////////// ok
      action = new MovementAction("jumplefetsurblocm1m2")
          .AddPhase(new MovementPhase(0.08f, moveX: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.5f, moveX: -1, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X, pos.Y, pos.Y - 3)  //air libre pour sauté
                                      && IsAreaFree(pos.X - 1, pos.X - 1, pos.Y - 2, pos.Y - 3)  //air libre pour sauté
                                      && IsSolid(pos.X - 1, pos.Y - 1) //sol a l arrivé
                ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 1, pos.Y - 2) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      /////////////////////// ok todo mauvaus calcul concidtion au dessus
      action = new MovementAction("jumplefetsurblocm1m3")
          .AddPhase(new MovementPhase(0.27f, moveX: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.5f, moveX: -1, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X, pos.Y, pos.Y - 4)  //air libre pour sauté
                                      && IsAreaFree(pos.X - 1, pos.X - 1, pos.Y - 3, pos.Y - 4)  //air libre pour sauté
                                      && IsSolid(pos.X - 1, pos.Y - 2) //sol a l arrivé
                ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 1, pos.Y - 3) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      /////////////////////// ok
      action = new MovementAction("jumplefetsurblocm1m4")
          .AddPhase(new MovementPhase(0.1f, moveY: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.27f, moveY: -1, dash: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.33f, moveX: -1, endCondition: null))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X, pos.Y, pos.Y - 5)  //air libre pour sauté
                                      && IsAreaFree(pos.X - 1, pos.X - 1, pos.Y - 4, pos.Y - 5)  //air libre pour sauté
                                      && IsSolid(pos.X - 1, pos.Y - 3) //sol a l arrivé
                ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 1, pos.Y - 4) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      /////////////////////// ok
      action = new MovementAction("jumplefetsurblocm1m5")
          .AddPhase(new MovementPhase(0.15f, moveY: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.27f, moveY: -1, dash: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.30f, moveX: -1, endCondition: null))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X, pos.Y, pos.Y - 6)  //air libre pour sauté
                                      && IsAreaFree(pos.X - 1, pos.X - 1, pos.Y - 5, pos.Y - 6)  //air libre pour sauté
                                      && IsSolid(pos.X - 1, pos.Y - 4) //sol a l arrivé
                ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 1, pos.Y - 5) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      /////////////////////// ok
      action = new MovementAction("jumplefetsurblocm1m6")
          .AddPhase(new MovementPhase(0.25f, moveY: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.27f, moveY: -1, dash: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.30f, moveX: -1, endCondition: null))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X, pos.Y, pos.Y - 7)  //air libre pour sauté
                                      && IsAreaFree(pos.X - 1, pos.X - 1, pos.Y - 6, pos.Y - 7)  //air libre pour sauté
                                      && IsSolid(pos.X - 1, pos.Y - 5) //sol a l arrivé
                ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 1, pos.Y - 6) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      /////////////////////// ok
      action = new MovementAction("jumplefetsurblocm1m7")
          .AddPhase(new MovementPhase(0.33f, moveY: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.001f, moveY: -1, dash: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.001f, moveY: -1))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.001f, moveY: -1, dash: true))
          .AddPhase(new MovementPhase(0.30f, moveX: -1, endCondition: null))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X, pos.Y, pos.Y - 8)  //air libre pour sauté
                                      && IsAreaFree(pos.X - 1, pos.X - 1, pos.Y - 7, pos.Y - 8)  //air libre pour sauté
                                      && IsSolid(pos.X - 1, pos.Y - 6) //sol a l arrivé
                ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 1, pos.Y - 7) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      /////////////////////// ok
      action = new MovementAction("jumplefetsurblocm1m8")
          .AddPhase(new MovementPhase(0.33f, moveY: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.001f, moveY: -1, dash: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.001f, moveY: -1))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.001f, moveY: -1, dash: true))
          .AddPhase(new MovementPhase(0.40f, moveX: -1, endCondition: null))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X, pos.Y, pos.Y - 9)  //air libre pour sauté
                                      && IsAreaFree(pos.X - 1, pos.X - 1, pos.Y - 8, pos.Y - 9)  //air libre pour sauté
                                      && IsSolid(pos.X - 1, pos.Y - 7) //sol a l arrivé
                ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 1, pos.Y - 8) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      /////////////////////// ok
      action = new MovementAction("jumplefetsurblocm1m9")
          .AddPhase(new MovementPhase(0.33f, moveY: -1, jump: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.001f, moveY: -1, dash: true))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.001f, moveY: -1))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(0.001f, moveY: -1, dash: true))
          .AddPhase(new MovementPhase(0.40f, moveX: -1, endCondition: null))              // continue à avancer en l’air
          .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      ;
      action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
                                      && IsAreaFree(pos.X, pos.X, pos.Y, pos.Y - 10)  //air libre pour sauté
                                      && IsAreaFree(pos.X - 1, pos.X - 1, pos.Y - 9, pos.Y - 10)  //air libre pour sauté
                                      && IsSolid(pos.X - 1, pos.Y - 8) //sol a l arrivé
                ;
      action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 1, pos.Y - 9) }; // position finale approximative
      action.CalculateCost();
      movementLibrary.Add(action);
      //      case 3: duration = 0.015f; break;
      //      case 4: duration = 0.16f; break;
      //      case 5: duration = 0.28f; break;
      //      case 6: duration = 0.73f; break;
      ///////////////////////
      //action = new MovementAction("jumplefetholem2m0")
      //    .AddPhase(new MovementPhase(0.2f, moveX: -1, jump: true))              // continue à avancer en l’air
      //    .AddPhase(new MovementPhase(0.2f, moveX: -1, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))              // continue à avancer en l’air
      //    .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      //;
      //action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
      //                                && !IsSolid(pos.X, pos.X - 1, pos.Y, pos.Y - 2)
      //      ;
      //action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 1, pos.Y) }; // position finale approximative
      //action.CalculateCost();
      //movementLibrary.Add(action);
      ////////////////////////
      //action = new MovementAction("ledgegrableftout")
      //    .AddPhase(new MovementPhase(0.5f, jump: true, moveX: -1));  // ne pas bouger
      //action.CalculateCost();
      //action.Condition = (pos, ai) => IsSolid(pos.X - 1, pos.Y) && !IsSolid(pos.X - 1, pos.Y - 1); //mur a gauche et vide dessous, possible grab
      //action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 1, pos.Y - 1) };
      //movementLibrary.Add(action);
      ////////////////////////
      //action = new MovementAction("ledgegrabrightout")
      //    .AddPhase(new MovementPhase(0.5f, jump: true, moveX: 1));  // ne pas bouger
      //action.CalculateCost();
      //action.Condition = (pos, ai) => IsSolid(pos.X + 1, pos.Y) && !IsSolid(pos.X + 1, pos.Y - 1); //mur a gauche et vide dessous, possible grab
      //action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X + 1, pos.Y - 1) };
      //movementLibrary.Add(action);
      ////////////////////////
      //foreach (String dirType in new string[] { "leftm", "rightp" })
      //{
      //  int moveX = dirType == "leftm" ? -1 : 1;///
      //  for (int i = 1; i <= 2; i++)
      //  {
      //    int iteration = i; // 
      //    action = new MovementAction($"{dirType}{iteration}m0")
      //        .AddPhase(new MovementPhase(0.12f * iteration, moveX: moveX, endCondition: (ai, startPoint, startPosition) =>
      //        {
      //          //Logger.Info($"{ai.player.X} <= {startPosition.X} - ({iteration} * {BLOCK_SIZE})  {startPosition.X - (iteration * BLOCK_SIZE)}");
      //          return ai.player.X <= startPosition.X - (iteration * BLOCK_SIZE);
      //        }))
      //    //.AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      //    ;

      //    //todo verifier que air est cellwakbale pour pos.Y et posY - 1
      //    action.Condition = (pos, ai) => HasGroundBelow(pos.X, pos.Y)
      //                                    && IsSolid(pos.X, moveX == -1 ? pos.X - iteration : pos.X + iteration, pos.Y + 1)      // ne peut sauter que si au sol
      //                                    && IsCellsWalkable(pos.X, moveX == -1 ? pos.X - iteration : pos.X + iteration, pos.Y, pos.Y - 1); // pas de mur sur le chemin
      //    action.ResultPositions = (pos, ai) => new List<Point> { new Point(moveX == -1 ? pos.X - iteration : pos.X + iteration, pos.Y) }; // position finale approximative
      //    action.CalculateCost();
      //    movementLibrary.Add(action);
      //  }
      //}
      ////////////////////////
      //foreach (String dirType in new string[] { "leftfallm", "rightfallp" })
      //{
      //  int moveX = dirType == "leftfallm" ? -1 : 1;///
      //  for (int i = 2; i <= 2; i++)
      //  {
      //    int iteration = i; // 
      //    action = new MovementAction($"{dirType}{iteration}m0")
      //        .AddPhase(new MovementPhase(0.12f * iteration, moveX: moveX, endCondition: (ai, startPoint, startPosition) =>
      //        {
      //          //Logger.Info($"{ai.player.X} <= {startPosition.X} - ({iteration} * {BLOCK_SIZE})  {startPosition.X - (iteration * BLOCK_SIZE)}");
      //          return ai.player.X <= startPosition.X - (iteration * BLOCK_SIZE);
      //        }))
      //    //.AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      //    ;

      //    //todo verifier que air est cellwakbale pour pos.Y et posY - 1
      //    action.Condition = (pos, ai) => !HasGroundBelow(pos.X, pos.Y);
      //    action.ResultPositions = (pos, ai) => new List<Point> {
      //          new Point(moveX == -1 ? pos.X - iteration : pos.X + iteration, pos.Y + 1),
      //          new Point(moveX == -1 ? pos.X - iteration : pos.X + iteration, pos.Y + 2),
      //          new Point(moveX == -1 ? pos.X - iteration : pos.X + iteration, pos.Y + 3),
      //    }; // position finale approximative
      //    action.CalculateCost();
      //    movementLibrary.Add(action);
      //  }
      //}
      ////////////////////////
      //foreach (String jumpType in new string[] { "jumpleftholem", "jumprightholep" })
      //{
      //  int moveX = jumpType == "jumpleftholem" ? -1 : 1;
      //  for (int i = 3; i <= 6; i++)
      //  {
      //    int iteration = i;
      //    float duration = 0;
      //    switch (i)
      //    {
      //      case 3: duration = 0.015f; break;
      //      case 4: duration = 0.16f; break;
      //      case 5: duration = 0.28f; break;
      //      case 6: duration = 0.73f; break;
      //    }
      //    action = new MovementAction($"{jumpType}{iteration}m0")
      //        .AddPhase(new MovementPhase(duration, moveX: moveX, jump: true))              // continue à avancer en l’air
      //        .AddPhase(new MovementPhase(0.2f * iteration, moveX: moveX, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))              // continue à avancer en l’air
      //        .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      //    ;

      //    action.Condition = (pos, ai) => IsSolid(pos.X, pos.Y + 1) // ne peut sauter que si au sol
      //                                    && !IsSolid(pos.X, moveX == -1 ? pos.X - iteration : pos.X + iteration, pos.Y, pos.Y - 3)
      //          ;
      //    action.ResultPositions = (pos, ai) => new List<Point> { new Point(moveX == -1 ? pos.X - iteration : pos.X + iteration, pos.Y) }; // position finale approximative
      //    action.CalculateCost();
      //    movementLibrary.Add(action);
      //  }
      //}

      ///////////////////////

      //foreach (String dirType in new string[] { "jumpverticalleftm", "jumpverticalrightp" })
      //{
      //  int moveX = dirType == "jumpverticalleftm" ? -1 : 1;///
      //  for (int i = 1; i <= 1; i++)
      //  {
      //    int iteration = i; // 
      //    action = new MovementAction($"{dirType}{iteration}m3")
      //        .AddPhase(new MovementPhase(2f, jump: true, endCondition: (ai, startPoint, startPosition) =>
      //                {
      //                  //Logger.Info($"{ai.player.X} <= {startPosition.X} - ({iteration} * {BLOCK_SIZE})  {startPosition.X - (iteration * BLOCK_SIZE)}");
      //                  //Stop when ledt or rigth on a plateform
      //                  if (moveX == -1)
      //                  {
      //                    return ai.IsCellWalkable(ai.playerInfo.X - 1, ai.playerInfo.Y)
      //                        && ai.HasGroundBelow(ai.playerInfo.X - 1, ai.playerInfo.Y);
      //                  }
      //                  else
      //                  {
      //                    return ai.IsCellWalkable(ai.playerInfo.X + 1, ai.playerInfo.Y)
      //                           && ai.HasGroundBelow(ai.playerInfo.X + 1, ai.playerInfo.Y);
      //                  }
      //                }))
      //        .AddPhase(new MovementPhase(1f, moveX: moveX)) //aller dans la direction pour se poser sur la plateforme
      //                                                       //.AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      //    ;

      //    //todo verifier que air est cellwakbale pour pos.Y et posY - 1
      //    action.Condition = (pos, ai) => HasGroundBelow(pos.X, pos.Y) // ne peut sauter que si au sol
      //                                    && IsCellsWalkable(pos.X, pos.X, pos.Y, pos.Y - 2)
      //          ;
      //    action.ResultPositions = (pos, ai) => new List<Point> {
      //            new Point(moveX == -1 ? pos.X - 1 : pos.X + 1, pos.Y - 1),
      //            new Point(moveX == -1 ? pos.X - 1 : pos.X + 1, pos.Y - 2),
      //            new Point(moveX == -1 ? pos.X - 1 : pos.X + 1, pos.Y - 3),

      //            new Point(moveX == -1 ? pos.X - 1 : pos.X + 2, pos.Y - 1),
      //            new Point(moveX == -1 ? pos.X - 1 : pos.X + 2, pos.Y - 2),
      //            new Point(moveX == -1 ? pos.X - 1 : pos.X + 2, pos.Y - 3),
      //    }; // position finale approximative
      //    action.CalculateCost();
      //    movementLibrary.Add(action);
      //  }
      //}
      ////////////////////////  ok
      //action = new MovementAction("superjumpleft")
      //    .AddPhase(new MovementPhase(0.01f, moveY: 1))
      //    .AddPhase(new MovementPhase(0.01f, moveY: 1, moveX: -1))
      //    .AddPhase(new MovementPhase(0.15f, moveY: 1, moveX: -1, dash: true))
      //    //.AddPhase(new MovementPhase(0.001f, moveY: 1, moveX: -1))
      //    //.AddPhase(new MovementPhase(0.001f, moveY: 1, moveX: -1, dash: true))
      //    .AddPhase(new MovementPhase(1.5f, moveX: -1, dash: true, jump: true, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))
      //    .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      //;
      //int maxJumpDistance = 10; //max 10
      ////int maxJumpDistance = 5;
      //action.Condition = (pos, ai) =>
      //{
      //  return HasGroundBelow(pos.X, pos.Y)      // ne peut sauter que si au sol
      //          && ai.IsCellsWalkable(pos.X - maxJumpDistance, pos.X - 1, pos.Y, pos.Y - 2) //ne peut sauter que si les case sont libre devant
      //  ;
      //};
      //action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - maxJumpDistance, pos.Y) }; // position finale approximative
      //action.CalculateCost();
      //movementLibrary.Add(action);
      ////////////////////////  ok
      //action = new MovementAction("superjumpright")
      //    .AddPhase(new MovementPhase(0.01f, moveY: 1))
      //    .AddPhase(new MovementPhase(0.01f, moveY: 1, moveX: 1))
      //    .AddPhase(new MovementPhase(0.15f, moveY: 1, moveX: 1, dash: true))
      //    //.AddPhase(new MovementPhase(0.001f, moveY: 1, moveX: -1))
      //    //.AddPhase(new MovementPhase(0.001f, moveY: 1, moveX: -1, dash: true))
      //    .AddPhase(new MovementPhase(1.5f, moveX: 1, dash: true, jump: true, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))
      //    .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      //;
      //action.Condition = (pos, ai) =>
      //{
      //  return HasGroundBelow(pos.X, pos.Y)      // ne peut sauter que si au sol
      //          && ai.IsCellsWalkable(pos.X + maxJumpDistance, pos.X + 1, pos.Y, pos.Y - 2) //ne peut sauter que si les case sont libre devant
      //  ;
      //};
      //action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X + 10, pos.Y) }; // position finale approximative
      //action.CalculateCost();
      //movementLibrary.Add(action);
      ////////////////////////  ok
      //action = new MovementAction("hyperjumpleft")
      //    .AddPhase(new MovementPhase(0.01f, moveY: 1))
      //    .AddPhase(new MovementPhase(0.01f, moveY: 1, moveX: -1))
      //    .AddPhase(new MovementPhase(0.001f, moveY: 1, moveX: -1, dash: true))
      //    .AddPhase(new MovementPhase(0.001f, moveY: 1, moveX: -1))
      //    .AddPhase(new MovementPhase(0.001f, moveY: 1, moveX: -1, dash: true))
      //    .AddPhase(new MovementPhase(1.5f, moveX: -1, dash: true, jump: true, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))
      //    .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      //;
      //action.Condition = (pos, ai) => HasGroundBelow(pos.X, pos.Y);      // ne peut sauter que si au sol
      //action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X - 28, pos.Y) }; // position finale approximative
      //action.CalculateCost();
      //movementLibrary.Add(action);
      ////////////////////////  ok
      //action = new MovementAction("hyperjumpright")
      //    .AddPhase(new MovementPhase(0.01f, moveY: 1))
      //    .AddPhase(new MovementPhase(0.01f, moveY: 1, moveX: 1))
      //    .AddPhase(new MovementPhase(0.001f, moveY: 1, moveX: 1, dash: true))
      //    .AddPhase(new MovementPhase(0.001f, moveY: 1, moveX: 1))
      //    .AddPhase(new MovementPhase(0.001f, moveY: 1, moveX: 1, dash: true))
      //    .AddPhase(new MovementPhase(1.5f, moveX: 1, dash: true, jump: true, endCondition: (ai, startPoint, startPosition) => ai.playerInfo.onGround))
      //    .AddPhase(new MovementPhase(1f, moveX: 0));  // ne pas bouger
      //;
      //action.Condition = (pos, ai) => HasGroundBelow(pos.X, pos.Y);      // ne peut sauter que si au sol
      //action.ResultPositions = (pos, ai) => new List<Point> { new Point(pos.X + 28, pos.Y) }; // position finale approximative
      //action.CalculateCost();
      //movementLibrary.Add(action);
    }

    // --- Aides de physique simple ---
    private bool HasGroundBelow(int x, int y)
    {
      if (y + 1 >= LEVEL_HEIGHT) return true;
      return !IsCellWalkable(x, y + 1);
    }

    private bool ClearHeadspace(int x, int y, int height)
    {
      for (int i = 1; i <= height; i++)
      {
        if (!IsCellWalkable(x, y - i)) return false;
      }
      return true;
    }

    private Point SimulateFall(Point from)
    {
      for (int f = 1; f < 24; f++) // chute max 10 cases
      {
        int fy = from.Y + f;
        if (fy >= LEVEL_HEIGHT - 1) break;
        if (!IsCellWalkable(from.X, fy))
        {
          return new Point(from.X, fy - 1);
        }
      }
      return new Point(from.X, LEVEL_HEIGHT - 2);
    }

  }


  public class Node
  {
    public Point Position;      // position de la case
    public float G;             // coût depuis le départ
    public float H;             // heuristique vers le but
    public float F => G + H;    // coût total
    public Node Parent;         // pour reconstruire le chemin
    public int PhaseIndex;      // index de phase si mouvement multi-phase

    public Node(Point position, float g, float h, Node parent = null, int phaseIndex = 0)
    {
      this.Position = position;
      this.G = g;
      this.H = h;
      this.Parent = parent;
      this.PhaseIndex = phaseIndex;
    }
  }
}
