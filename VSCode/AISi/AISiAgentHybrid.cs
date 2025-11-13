using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using Newtonsoft.Json;
using TFModFortRiseLoaderAI;
using TowerFall;
using static TowerFall.Arrow;
using static TowerFall.Player;

namespace TFModFortRiseAiSimple
{
    public class AISiAgentHybrid : TFModFortRiseLoaderAI.Agent
    {
        // État de l'IA
        private enum AIState
        {
            Idle, Approaching, Shooting, Dodging, Positioning, WallClimbing,
            AirControl, Retreat, Aggressive, Defensive, CatchingArrow
        }

        private AIState currentState = AIState.Idle;
        private AIState previousState = AIState.Idle;
        private float stateTimer = 0f;
        private const float STATE_TIMEOUT = 2.0f;

        // Informations de perception
        public PlayerInfo playerInfo = new PlayerInfo();
        public PlayerInfo enemyInfo = new PlayerInfo();
        public List<ArrowInfo> arrows = new List<ArrowInfo>();

        public Player enemy;
        public Player player;

        // Système d'apprentissage adaptatif
        private Dictionary<string, float> actionSuccessRates = new Dictionary<string, float>();
        private Dictionary<string, int> actionAttempts = new Dictionary<string, int>();
        private string lastAction = "none";
        private bool lastActionSuccess = false;

        // Gestionnaire d'apprentissage persistant et analyse de niveau
        private AISiLearningManager learningManager;
        private AISiLevelAnalyzer levelAnalyzer;
        private string currentLevelType = "unknown";
        private string currentLevelId = "unknown";
        private float autoSaveTimer = 0f;

        // Micro-movements et réactivité
        private Vector2 lastEnemyPosition;
        private float enemyVelocityPrediction = 0f;
        private Vector2 optimalShootingPosition;
        private bool shouldDodge = false;
        private Vector2 dodgeDirection;

        // Constants
        private const float SHOOT_COOLDOWN = 0.3f;
        private const float DODGE_COOLDOWN = 0.5f;
        private const float APPROACH_DISTANCE = 80f;
        private const float OPTIMAL_SHOOT_RANGE = 120f;
        private const float ARROW_DANGER_RADIUS = 40f;

        private float shootTimer = 0f;
        private float dodgeTimer = 0f;

        // Debug properties for path rendering
        public List<Point> debugPath = new List<Point>();
        public const int BLOCK_SIZE = 16;

        public AISiAgentHybrid(int index, String type, PlayerInput input) : base(index, type, input)
        {
            InitializeLearningSystem();
            InitializePersistentLearning();
        }

        private void InitializeLearningSystem()
        {
            var actions = new[] { "shoot", "dodge_left", "dodge_right", "jump", "wall_climb", "approach", "retreat" };
            foreach (var action in actions)
            {
                actionSuccessRates[action] = 0.5f;
                actionAttempts[action] = 0;
            }
        }

        private void InitializePersistentLearning()
        {
            // Initialiser le gestionnaire d'apprentissage persistant
            learningManager = new AISiLearningManager();
            learningManager.LoadAllLearningData();

            // Initialiser l'analyseur de niveau
            levelAnalyzer = new AISiLevelAnalyzer();

            // Charger les données d'apprentissage pour le niveau actuel si disponible
            LoadLearningDataForCurrentLevel();
        }

        private void LoadLearningDataForCurrentLevel()
        {
            if (level != null && learningManager != null)
            {
                // Analyser le niveau actuel
                levelAnalyzer.AnalyzeLevel(level);
                currentLevelType = levelAnalyzer.GetLevelType();

                // Obtenir les caractéristiques du niveau
                var levelCharacteristics = levelAnalyzer.GetLevelCharacteristics();

                // Trouver le meilleur type de niveau correspondant
                string bestMatchingType = learningManager.FindBestMatchingLevelType(levelCharacteristics);

                if (!string.IsNullOrEmpty(bestMatchingType) && bestMatchingType != "unknown")
                {
                    currentLevelType = bestMatchingType;
                    
                    // Définir le type de niveau courant dans le learning manager

                    var levelData = learningManager.GetLevelData(bestMatchingType);

                    if (levelData != null && levelData.ActionSuccessRates != null && levelData.ActionAttempts != null)
                    {
                        // Charger les taux de réussite pour ce type de niveau
                        foreach (var kvp in levelData.ActionSuccessRates)
                        {
                            if (actionSuccessRates.ContainsKey(kvp.Key))
                            {
                                actionSuccessRates[kvp.Key] = kvp.Value;
                            }
                        }

                        foreach (var kvp in levelData.ActionAttempts)
                        {
                            if (actionAttempts.ContainsKey(kvp.Key))
                            {
                                actionAttempts[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }

                Console.WriteLine($"Niveau détecté: {currentLevelType} (meilleure correspondance: {bestMatchingType})");
            }
        }

        private void SaveLearningDataForCurrentLevel()
        {
            if (learningManager != null && !string.IsNullOrEmpty(currentLevelType) && currentLevelType != "unknown")
            {
                var levelData = new AISiLearningManager.LevelLearningData
                {
                    LevelType = currentLevelType,
                    ActionSuccessRates = new Dictionary<string, float>(actionSuccessRates),
                    ActionAttempts = new Dictionary<string, int>(actionAttempts),
                    LastUpdated = DateTime.Now
                };

                learningManager.UpdateLevelData(currentLevelType, levelData);
                learningManager.SaveAllLearningData();
            }
        }

        public override void Reset()
        {
            currentState = AIState.Idle;
            stateTimer = 0f;
            shootTimer = 0f;
            dodgeTimer = 0f;
        }

        public override void Move()
        {
            UpdatePerception();
            UpdateTimers();
            UpdateAutoSave();

            // Évaluation de la situation actuelle
            var situation = EvaluateSituation();

            // Transition d'état basée sur la situation
            UpdateState(situation);

            // Exécution de l'état actuel
            ExecuteCurrentState();

            // Mise à jour de l'apprentissage
            UpdateLearning();
        }

        private void UpdateAutoSave()
        {
            autoSaveTimer += Engine.DeltaTime;

            // Auto-save toutes les 30 secondes par défaut
            if (autoSaveTimer >= 30f)
            {
                SaveLearningDataForCurrentLevel();
                autoSaveTimer = 0f;
            }
        }

        private void UpdatePerception()
        {
            player = level.GetPlayer(index);
            enemy = level.GetPlayer(index == 0 ? 1 : 0);

            if (player != null)
            {
                UpdatePlayerInfo(player, playerInfo);
            }

            if (enemy != null)
            {
                UpdatePlayerInfo(enemy, enemyInfo);
                enemyVelocityPrediction = (enemy.Position.X - lastEnemyPosition.X) / 0.016f; // 60 FPS
                lastEnemyPosition = enemy.Position;
            }

            UpdateArrowInfo();
        }

        private void UpdatePlayerInfo(Player player, PlayerInfo info)
        {


            var dynData = DynamicData.For(player);
            //info.X = (int)(player.Position.X / 10);
            //info.Y = (int)(player.Position.Y / 10);
            Point cell = WorldToCell(player.Position);
      info.X = cell.X;
      info.Y = cell.Y;
            info.onGround = player.OnGround;
            info.GrabEdge = dynData.Get<PlayerStates>("State") == PlayerStates.LedgeGrab;
            info.Speed = player.Speed;
            info.NbArrows = player.Arrows.Count;
            //info.CanWallJump = player.CanWallJump(Facing.Left) ||
                               //player.CanWallJump(Facing.Right);
            info.CanWallJump = dynData.Invoke<bool>("CanWallJump", Facing.Left) || dynData.Invoke<bool>("CanWallJump", Facing.Right);
            dynData.Dispose();
        }

          private void UpdateArrowInfo()
        {
            arrows.Clear();
            foreach (Arrow arrow in level[GameTags.Arrow])
            {
                ArrowInfo arrowInfo = new ArrowInfo();
                arrowInfo.state = arrow.State;
                arrowInfo.Position = arrow.Position;
                arrowInfo.Speed = arrow.Speed;
                arrowInfo.X = (int)(arrow.Position.X / 10);
                arrowInfo.Y = (int)(arrow.Position.Y / 10);
                arrows.Add(arrowInfo);
            }
        }

        private void UpdateTimers()
        {
            stateTimer += Engine.DeltaTime;
            shootTimer += Engine.DeltaTime;
            dodgeTimer += Engine.DeltaTime;

            // Timeout de sécurité pour éviter le blocage
            if (stateTimer > STATE_TIMEOUT)
            {
                currentState = AIState.Idle;
                stateTimer = 0f;
            }
        }

        private SituationEvaluation EvaluateSituation()
        {
            var eval = new SituationEvaluation();

            if (player == null || enemy == null) return eval;

            Vector2 toEnemy = enemy.Position - player.Position;
            float distanceToEnemy = toEnemy.Length();

            // Évaluation des dangers
            eval.ArrowThreat = GetArrowThreatLevel();
            eval.IsInDanger = eval.ArrowThreat > 0.3f;

            // Évaluation de la position
            eval.DistanceToEnemy = distanceToEnemy;
            eval.CanShoot = CanShootSafely() && shootTimer >= SHOOT_COOLDOWN;
            eval.ShouldRetreat = ShouldRetreat();
            eval.IsInOptimalRange = distanceToEnemy < OPTIMAL_SHOOT_RANGE && distanceToEnemy > 40f;

            // Évaluation du terrain
            eval.CanWallJump = playerInfo.CanWallJump;
            eval.IsOnGround = playerInfo.onGround;
            eval.EnemyIsAbove = enemy.Position.Y < player.Position.Y - 20;
            eval.EnemyIsBelow = enemy.Position.Y > player.Position.Y + 20;

            return eval;
        }

        private void UpdateState(SituationEvaluation eval)
        {
            previousState = currentState;

            // Priorité 1: Sécurité
            if (eval.IsInDanger)
            {
                currentState = AIState.Dodging;
            }
            // Priorité 2: Opportunités de tir
            else if (eval.CanShoot && eval.IsInOptimalRange)
            {
                currentState = AIState.Shooting;
            }
            // Priorité 3: Positionnement
            else if (eval.EnemyIsAbove && playerInfo.onGround)
            {
                currentState = AIState.WallClimbing;
            }
            else if (!eval.IsInOptimalRange && !eval.ShouldRetreat)
            {
                currentState = AIState.Approaching;
            }
            else if (eval.ShouldRetreat)
            {
                currentState = AIState.Retreat;
            }
            else
            {
                currentState = AIState.Positioning;
            }

            if (currentState != previousState)
            {
                stateTimer = 0f;
            }
        }

        private void ExecuteCurrentState()
        {
            switch (currentState)
            {
                case AIState.Dodging:
                    ExecuteDodge();
                    break;
                case AIState.Shooting:
                    ExecuteShoot();
                    break;
                case AIState.Approaching:
                    ExecuteApproach();
                    break;
                case AIState.WallClimbing:
                    ExecuteWallClimb();
                    break;
                case AIState.Retreat:
                    ExecuteRetreat();
                    break;
                case AIState.Positioning:
                    ExecutePositioning();
                    break;
                default:
                    ExecuteIdle();
                    break;
            }
        }

        private void ExecuteDodge()
        {
            if (dodgeTimer < DODGE_COOLDOWN)
            {
                // Attendre le cooldown
                return;
            }

            Vector2 bestDodgeDir = GetBestDodgeDirection();

            // Appliquer le dodge
            input.inputState.MoveX = Math.Sign(bestDodgeDir.X);
            input.inputState.MoveY = bestDodgeDir.Y < -0.5f ? -1 : (bestDodgeDir.Y > 0.5f ? 1 : 0);
            input.inputState.DodgeCheck = true;
            input.inputState.DodgePressed = true;

            dodgeTimer = 0f;
            lastAction = "dodge_" + (bestDodgeDir.X < 0 ? "left" : "right");
        }

        private void ExecuteShoot()
        {
            if (shootTimer < SHOOT_COOLDOWN || playerInfo.NbArrows <= 0)
            {
                return;
            }

            Vector2 toEnemy = enemy.Position - player.Position;
            if (toEnemy != Vector2.Zero) toEnemy.Normalize();

            // Prédiction de mouvement
            Vector2 predictedPos = enemy.Position + new Vector2(enemyVelocityPrediction * 0.1f, 0);
            Vector2 aimDirection = predictedPos - player.Position;
            if (aimDirection != Vector2.Zero) aimDirection.Normalize();

            // Tir
            input.inputState.AimAxis = aimDirection;
            input.inputState.ShootCheck = true;
            input.inputState.ShootPressed = true;

            shootTimer = 0f;
            lastAction = "shoot";
        }

        private void ExecuteApproach()
        {
            Vector2 toEnemy = enemy.Position - player.Position;

            // Mouvement de base
            if (Math.Abs(toEnemy.X) > 20f)
            {
                input.inputState.MoveX = Math.Sign(toEnemy.X);
            }

            // Sauter si l'ennemi est au-dessus
            if (toEnemy.Y < -30f && playerInfo.onGround)
            {
                input.inputState.JumpCheck = true;
                input.inputState.JumpPressed = true;
            }

            // Dash si l'ennemi est loin
            if (toEnemy.Length() > 150f && !playerInfo.onGround)
            {
                input.inputState.DodgeCheck = true;
                input.inputState.DodgePressed = true;
            }

            lastAction = "approach";
        }

        private void ExecuteWallClimb()
        {
            // Chercher un mur proche
            bool wallOnLeft = CheckWallAt(player.Position + new Vector2(-10, 0));
            bool wallOnRight = CheckWallAt(player.Position + new Vector2(10, 0));

            if (wallOnLeft || wallOnRight)
            {
                // Saut mural
                input.inputState.MoveX = wallOnLeft ? -1 : 1;
                input.inputState.JumpCheck = true;
                input.inputState.JumpPressed = true;
            }
            else
            {
                // Chercher un mur
                input.inputState.MoveX = enemy.Position.X < player.Position.X ? -1 : 1;
            }

            lastAction = "wall_climb";
        }

        private void ExecuteRetreat()
        {
            Vector2 toEnemy = enemy.Position - player.Position;

            // S'éloigner de l'ennemi
            input.inputState.MoveX = -Math.Sign(toEnemy.X);

            // Sauter si possible
            if (playerInfo.onGround && toEnemy.Y < -20f)
            {
                input.inputState.JumpCheck = true;
                input.inputState.JumpPressed = true;
            }

            lastAction = "retreat";
        }

        private void ExecutePositioning()
        {
            // Trouver une position optimale
            Vector2 toEnemy = enemy.Position - player.Position;

            // Se positionner pour un meilleur angle de tir
            if (Math.Abs(toEnemy.X) < 30f && toEnemy.Y > 20f)
            {
                // Reculer pour avoir un meilleur angle
                input.inputState.MoveX = -Math.Sign(toEnemy.X);
            }
            else if (Math.Abs(toEnemy.X) > OPTIMAL_SHOOT_RANGE)
            {
                // Se rapprocher un peu
                input.inputState.MoveX = Math.Sign(toEnemy.X);
            }

            lastAction = "positioning";
        }

        private void ExecuteIdle()
        {
            // Petits mouvements aléatoires pour éviter d'être une cible facile
            if (stateTimer > 0.5f)
            {
                input.inputState.MoveX = (Monocle.Calc.Random.NextFloat() - 0.5f) > 0 ? 1 : -1;
            }

            lastAction = "idle";
        }

        private float GetArrowThreatLevel()
        {
            float maxThreat = 0f;

            foreach (var arrow in arrows)
            {
                if (arrow.state == ArrowStates.Shooting || arrow.state == ArrowStates.Falling)
                {
                    float distance = Vector2.Distance(player.Position, arrow.Position);
                    if (distance < ARROW_DANGER_RADIUS)
                    {
                        // Calculer la trajectoire de la flèche
                        Vector2 toPlayer = player.Position - arrow.Position;
                        float dotProduct = Vector2.Dot(Vector2.Normalize(toPlayer), Vector2.Normalize(arrow.Speed));

                        if (dotProduct > 0.7f) // Flèche va vers le joueur
                        {
                            float threat = 1f - (distance / ARROW_DANGER_RADIUS);
                            maxThreat = Math.Max(maxThreat, threat);
                        }
                    }
                }
            }

            return maxThreat;
        }

        private Vector2 GetBestDodgeDirection()
        {
            Vector2 bestDir = Vector2.Zero;
            float bestScore = float.MinValue;

            // Tester différentes directions de dodge
            Vector2[] testDirections = new Vector2[]
            {
                new Vector2(-1, 0),   // Gauche
                new Vector2(1, 0),    // Droite
                new Vector2(0, -1),   // Haut
                new Vector2(-1, -1),  // Haut-gauche
                new Vector2(1, -1),   // Haut-droite
            };

            foreach (var dir in testDirections)
            {
                float score = EvaluateDodgeDirection(dir);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDir = dir;
                }
            }

            return bestDir;
        }

        private float EvaluateDodgeDirection(Vector2 direction)
        {
            float score = 0f;
            Vector2 futurePos = player.Position + direction * 30f; // Distance de dodge

            // Éviter les murs
            if (CheckWallAt(futurePos))
            {
                score -= 100f;
            }

            // Éloignement des flèches
            foreach (var arrow in arrows)
            {
                if (arrow.state == ArrowStates.Shooting || arrow.state == ArrowStates.Falling)
                {
                    float oldDistance = Vector2.Distance(player.Position, arrow.Position);
                    float newDistance = Vector2.Distance(futurePos, arrow.Position);
                    score += (newDistance - oldDistance) * 2f;
                }
            }

            // Se rapprocher de l'ennemi si c'est sûr
            float oldEnemyDist = Vector2.Distance(player.Position, enemy.Position);
            float newEnemyDist = Vector2.Distance(futurePos, enemy.Position);
            if (GetArrowThreatLevel() < 0.3f)
            {
                score += (oldEnemyDist - newEnemyDist) * 0.5f; // Bonus pour se rapprocher
            }

            return score;
        }

        private bool CanShootSafely()
        {
            if (playerInfo.NbArrows <= 0) return false;

            Vector2 toEnemy = enemy.Position - player.Position;
            if (toEnemy.Length() > OPTIMAL_SHOOT_RANGE) return false;

            // Vérifier la ligne de vue
            return !CheckWallBetween(player.Position, enemy.Position);
        }

        private bool ShouldRetreat()
        {
            Vector2 toEnemy = enemy.Position - player.Position;

            // Reculer si l'ennemi est trop proche et a des flèches
            if (toEnemy.Length() < 50f && enemyInfo.NbArrows > 0)
            {
                return true;
            }

            // Reculer si en danger et pas en position avantageuse
            if (GetArrowThreatLevel() > 0.5f && !playerInfo.onGround)
            {
                return true;
            }

            return false;
        }

        private bool CheckWallAt(Vector2 position)
        {
            return level.CollideCheck(position, GameTags.Solid);
        }

        private bool CheckWallBetween(Vector2 start, Vector2 end)
        {
            int steps = (int)(end - start).Length() / 5;
            for (int i = 0; i < steps; i++)
            {
                Vector2 checkPos = Vector2.Lerp(start, end, (float)i / steps);
                if (CheckWallAt(checkPos))
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateLearning()
        {
            // Évaluer le succès de la dernière action
            if (!string.IsNullOrEmpty(lastAction))
            {
                bool currentSuccess = EvaluateActionSuccess();

                if (actionAttempts.ContainsKey(lastAction))
                {
                    actionAttempts[lastAction]++;
                    float currentRate = actionSuccessRates[lastAction];
                    float newRate = MathHelper.Lerp(currentRate, currentSuccess ? 1f : 0f, 0.1f);
                    actionSuccessRates[lastAction] = newRate;
                }
            }
        }

        private bool EvaluateActionSuccess()
        {
            // Évaluer si la dernière action était bénéfique
            switch (lastAction)
            {
                case "shoot":
                    // Succès si l'ennemi est blessé ou mort
                    return enemyInfo.NbArrows < enemy.Arrows.Count;

                case "dodge_left":
                case "dodge_right":
                    // Succès si on a évité les flèches
                    return GetArrowThreatLevel() < 0.2f;

                case "approach":
                    // Succès si on s'est rapproché
                    Vector2 toEnemy = enemy.Position - player.Position;
                    return toEnemy.Length() < OPTIMAL_SHOOT_RANGE;

                default:
                    return true;
            }
        }

        private class SituationEvaluation
        {
            public float ArrowThreat { get; set; }
            public bool IsInDanger { get; set; }
            public float DistanceToEnemy { get; set; }
            public bool CanShoot { get; set; }
            public bool ShouldRetreat { get; set; }
            public bool IsInOptimalRange { get; set; }
            public bool CanWallJump { get; set; }
            public bool IsOnGround { get; set; }
            public bool EnemyIsAbove { get; set; }
            public bool EnemyIsBelow { get; set; }
        }

        // Méthodes publiques pour le débogage et les statistiques
        public string GetCurrentState()
        {
            return currentState.ToString();
        }

        public float GetSuccessRate(string action)
        {
            if (actionSuccessRates.ContainsKey(action))
            {
                return actionSuccessRates[action];
            }
            return 0f;
        }

        public Dictionary<string, float> GetAllSuccessRates()
        {
            return new Dictionary<string, float>(actionSuccessRates);
        }

        public int GetActionAttempts(string action)
        {
            if (actionAttempts.ContainsKey(action))
            {
                return actionAttempts[action];
            }
            return 0;
        }

        // Nouvelles méthodes pour la gestion persistante
        public void ForceSaveLearningData()
        {
            SaveLearningDataForCurrentLevel();
        }

        public string GetCurrentLevelType()
        {
            return currentLevelType;
        }

        public Dictionary<string, float> GetLevelCharacteristics()
        {
            return levelAnalyzer != null ? levelAnalyzer.GetLevelCharacteristics() : new Dictionary<string, float>();
        }

        // Helper method for debug path rendering
        public Point WorldToCell(Vector2 position)
        {
            return new Point((int)(position.X / BLOCK_SIZE), (int)(position.Y / BLOCK_SIZE));
        }
    }
}
//}