//using TowerFall;
//using System;
//using Microsoft.Xna.Framework;
//using Monocle;

//namespace TFModFortRiseAiSimple
//{
//  public class AISiAgentLevel1 : TFModFortRiseLoaderAI.Agent
//  {
//    private const float DODGE_REACTION_DISTANCE = 100f; // Distance à laquelle l'IA réagit aux flèches
//    private const float HEAD_STOMP_DISTANCE = 30f; // Distance pour le head stomp
//    private const float ARROW_COLLECT_DISTANCE = 50f; // Distance pour ramasser les flèches

//    public AISiAgentLevel1(int index, String type, PlayerInput input) : base(index, type, input) { }

//    protected override void Move()
//    {
//      // Reset input state
//      this.input.inputState = new InputState();
//      this.input.inputState.AimAxis = Vector2.Zero;
//      this.input.inputState.MoveX = 0;
//      this.input.inputState.MoveY = 0;

//      Player agent = level.GetPlayer(index);
//      if (agent == null || agent.Dead) return;

//      // 1. Vérifier les flèches qui arrivent pour les esquiver/attraper
//      if (TryDodgeIncomingArrows(agent))
//        return;

//      // 2. Si pas de flèches, chercher des pickups de flèches
//      if (agent.Arrows.Count == 0)
//      {
//        if (TryCollectArrows(agent))
//          return;
//      }

//      // Trouver l'ennemi le plus proche
//      Player nearestEnemy = FindNearestEnemy(agent);
//      if (nearestEnemy == null) return;

//      // 3. Si pas de flèches, essayer le head stomp
//      if (agent.Arrows.Count == 0)
//      {
//        if (TryHeadStomp(agent, nearestEnemy))
//          return;
//      }

//      // 4. Comportement normal de combat
//      CombatBehavior(agent, nearestEnemy);
//    }

//    private bool TryDodgeIncomingArrows(Player agent)
//    {
//      // Rechercher les flèches proches
//      foreach (Entity entity in level[GameTags.Player])
//      {
//        Arrow arrow = entity as Arrow;
//        if (arrow.PlayerIndex == agent.PlayerIndex) continue;

//        float distance = Vector2.Distance(arrow.Position, agent.Position);
//        if (distance < DODGE_REACTION_DISTANCE)
//        {
//          Vector2 arrowDirection = arrow.Speed.SafeNormalize();

//          // Si la flèche se dirige vers nous
//          if (Vector2.Dot(arrowDirection, (agent.Position - arrow.Position).SafeNormalize()) > 0.7f)
//          {
//            // Maintenir le bouton dodge pour attraper
//            this.input.inputState.DodgePressed = true;

//            // Se déplacer légèrement vers la flèche pour mieux l'attraper
//            this.input.inputState.MoveX = arrow.Position.X > agent.Position.X ? 1 : -1;
//            return true;
//          }
//        }
//      }
//      return false;
//    }

//    private bool TryCollectArrows(Player agent)
//    {
//      // Rechercher les flèches qui peuvent être ramassées
//      Arrow nearestArrow = null;
//      float nearestDistance = float.MaxValue;

//      foreach (Entity entity in level[GameTags.Arrow])
//      {
//        Arrow arrow = entity as Arrow;
//        if (arrow == null) continue;

//        // Vérifier si la flèche peut être ramassée
//        if (arrow.State == Arrow.ArrowStates.Stuck ||
//            arrow.State == Arrow.ArrowStates.LayingOnGround ||
//            arrow.State == Arrow.ArrowStates.Buried)
//        {
//          float distance = Vector2.Distance(arrow.Position, agent.Position);
//          if (distance < nearestDistance)
//          {
//            nearestDistance = distance;
//            nearestArrow = arrow;
//          }
//        }
//      }

//      if (nearestArrow != null && nearestDistance < ARROW_COLLECT_DISTANCE)
//      {
//        // Se déplacer vers la flèche
//        this.input.inputState.MoveX = nearestArrow.Position.X > agent.Position.X ? 1 : -1;

//        // Sauter si la flèche est plus haute
//        if (nearestArrow.Position.Y < agent.Position.Y - 10)
//          this.input.inputState.JumpPressed = true;

//        return true;
//      }

//      return false;
//    }

//    private bool TryHeadStomp(Player agent, Player enemy)
//    {
//      if (Math.Abs(agent.Position.X - enemy.Position.X) < HEAD_STOMP_DISTANCE)
//      {
//        if (agent.Position.Y > enemy.Position.Y)
//        {
//          // On est au-dessus, on essaie de tomber sur l'ennemi
//          this.input.inputState.MoveX = enemy.Position.X > agent.Position.X ? 1 : -1;
//          return true;
//        }
//        else
//        {
//          // On est en-dessous, on saute pour faire le head stomp
//          this.input.inputState.JumpPressed = true;
//          this.input.inputState.MoveX = enemy.Position.X > agent.Position.X ? 1 : -1;
//          return true;
//        }
//      }
//      return false;
//    }

//    private void CombatBehavior(Player agent, Player enemy)
//    {
//      Vector2 agentPosition = agent.Position;
//      Vector2 enemyPosition = enemy.Position;

//      // Vérifier s'il y a un mur entre l'agent et l'ennemi
//      bool hasLineOfSight = !HasWallBetween(agentPosition, enemyPosition);

//      // Distance de sécurité pour le combat
//      bool tooClose = IsEnemyTooClose(agentPosition, enemyPosition);

//      if (tooClose)
//      {
//        // S'éloigner
//        this.input.inputState.MoveX = enemyPosition.X > agentPosition.X ? -1 : 1;
//        this.input.inputState.AimAxis.X = enemyPosition.X > agentPosition.X ? 1 : -1;
//      }
//      else
//      {
//        // Se rapprocher
//        this.input.inputState.MoveX = enemyPosition.X > agentPosition.X ? 1 : -1;
//        this.input.inputState.AimAxis.X = enemyPosition.X > agentPosition.X ? 1 : -1;
//      }

//      // Tirer seulement si on a une ligne de vue dégagée
//      if (hasLineOfSight && agent.Arrows.Count > 0)
//      {
//        if (CanShoot(agent, enemy))
//        {
//          this.input.inputState.ShootPressed = true;
//        }
//      }
//    }

//    private bool HasWallBetween(Vector2 start, Vector2 end)
//    {
//      // Utiliser un raycast pour vérifier les murs
//      Vector2 direction = (end - start).SafeNormalize();
//      float distance = Vector2.Distance(start, end);

//      return level.CollideCheck(start, end, GameTags.Solid);
//    }

//    private bool IsEnemyTooClose(Vector2 agentPos, Vector2 enemyPos)
//    {
//      float distanceX = Math.Abs(enemyPos.X - agentPos.X);
//      float distanceY = Math.Abs(enemyPos.Y - agentPos.Y);
//      return distanceX < 50 && distanceY < 50;
//    }

//    private bool CanShoot(Player agent, Player enemy)
//    {
//      // Vérifier si l'ennemi est à portée de tir
//      float distanceX = Math.Abs(enemy.Position.X - agent.Position.X);
//      float distanceY = Math.Abs(enemy.Position.Y - agent.Position.Y);

//      return (distanceY < enemy.Height && distanceX < 200) ||
//             (distanceX < enemy.Width && distanceY < 150);
//    }

//    private Player FindNearestEnemy(Player agent)
//    {
//      Player nearestEnemy = null;
//      float nearestDistance = float.MaxValue;

//      foreach (Entity entity in level[GameTags.Player])
//      {
//        Player player = entity as Player;
//        if (player.PlayerIndex == agent.PlayerIndex) continue;
//        if (player.Dead) continue;

//        // Vérifier si c'est un ennemi (mode équipe ou FFA)
//        if (agent.TeamColor == Allegiance.Neutral || player.TeamColor != agent.TeamColor)
//        {
//          float distance = Vector2.Distance(player.Position, agent.Position);
//          if (distance < nearestDistance)
//          {
//            nearestDistance = distance;
//            nearestEnemy = player;
//          }
//        }
//      }

//      return nearestEnemy;
//    }
//  }
//}
