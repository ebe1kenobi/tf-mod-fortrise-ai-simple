using TowerFall;
using System;
using Microsoft.Xna.Framework;

namespace TFModFortRiseAiSimple
{
  public class AISiAgent : TFModFortRiseLoaderAI.Agent
  {

    public AISiAgent(int index, String type, PlayerInput input) : base(index, type, input)
    {
    }

    protected override void Move()
    {
      this.input.inputState = new InputState();
      this.input.inputState.AimAxis.X = 0;
      this.input.inputState.MoveX = 0;
      this.input.inputState.AimAxis.Y = 0;
      this.input.inputState.MoveY = 0;

      /////////////////////////////////////////////
      //# If the agent is not present, it means it is dead.
      //if self.my_state == None:
      //# You are required to reply with actions, or the agent will get disconnected.
      //# logging.info('send_actions')
      //self.send_actions()
      //return True
      /////////////////////////////////////////////

      Player agent = level.GetPlayer(index);
      if (agent == null) return; //TODO don't know if works when player died ... apparently yes
      if (agent.Dead)
      {
        this.input.prevInputState = this.input.GetCopy(this.input.inputState);
        return;
      }

      /////////////////////////////////////////////
      //enemy_state = None
      /////////////////////////////////////////////
      Player enemy = null;

      /////////////////////////////////////////////
      //  if (self.state_scenario['mode'] != "Quest" and self.state_scenario['mode'] != "DarkWorld") \
      //    and((state['team'] == 'neutral') or state['team'] != self.my_state['team']):
      //    enemy_state = state
      //    break
      /////////////////////////////////////////////

      if (level.Session.MatchSettings.Mode != Modes.Quest && level.Session.MatchSettings.Mode != Modes.DarkWorld)
      {
        foreach (Player player in level.Players)
        {
          if (player.PlayerIndex == index) continue;
          // TODO playtag support
          //if (agent.playTagCountDownOn && player.playTag)
          //if (PlayTagImport.IsPlayTagCountDownOn(agent.PlayerIndex) && PlayTagImport.IsPlayerPlayTag(player.PlayerIndex))
          //{
          //  enemy = player;
          //  break;
          //}
          if (agent.TeamColor == Allegiance.Neutral || player.TeamColor != agent.TeamColor)
          {
            // TODO each agent tak ethe same enemy if they all start from the beginning
            // Try to take the closest
            enemy = player;
            break;
          }
        }
      }
      else
      {
        throw new Exception("AI for Quest and DarkWorld not supported");
      }

      /////////////////////////////////////////////
      //# If no enemy archer is found, try to find another enemy.
      //if not enemy_state:
      //    for state in self.state_update['entities']:
      //    if state['isEnemy']:
      //      enemy_state = state
      //if (enemy == null) {
      // throw new Exception("AI for Quest and DarkWorld not supported");
      //}
      /////////////////////////////////////////////

      /////////////////////////////////////////////
      //# If no enemy is found, means all are dead.
      //if enemy_state == None:
      //  self.send_actions()
      //  return
      /////////////////////////////////////////////
      if (enemy == null)
      {
        this.input.prevInputState = this.input.GetCopy(this.input.inputState);
        return;
      }

      //if (moves.Count > 0) {
      //  moves = Moves.Move(ref this.input.inputState, moves);
      //  this.input.prevInputState = this.input.GetCopy(this.input.inputState);
      //  return;
      //}
      /////////////////////////////////////////////
      //my_pos = self.my_state['pos']
      //enemy_pos = enemy_state['pos']
      /////////////////////////////////////////////
      Vector2 agentPosition = agent.Position;
      Vector2 enemyPosition = enemy.Position;
      /////////////////////////////////////////////
      //if enemy_pos['y'] >= my_pos['y'] and enemy_pos['y'] <= my_pos['y'] + 50:
      //  # Runs away if enemy is right above
      //  if my_pos['x'] < enemy_pos['x']:
      //    self.press('l')
      //  else:
      //    self.press('r')
      //else:
      //  # Runs to enemy if they are below
      //  if my_pos['x'] < enemy_pos['x']:
      //    self.press('r')
      //  else:
      //    self.press('l')
      /////////////////////////////////////////////
      //Game mode playtag TODO  (TFModFortRiseAIModule.IsModPlaytagExists
      //if (agent.playTagCountDownOn)
      //if (PlayTagImport.IsPlayTagCountDownOn(agent.PlayerIndex))
      //{
      //  //if (agent.playTag)
      //  if (PlayTagImport.IsPlayerPlayTag(agent.PlayerIndex))
      //  {
      //    // pursue
      //    if (enemyPosition.X > agentPosition.X)
      //    {
      //      //right
      //      this.input.inputState.MoveX += 1;
      //      this.input.inputState.AimAxis.X += 1;
      //    }
      //    else
      //    {
      //      //left
      //      this.input.inputState.AimAxis.X -= 1;
      //      this.input.inputState.MoveX -= 1;
      //    }
      //  }
      //  else
      //  {
      //    // run away
      //    if (enemyPosition.X > agentPosition.X)
      //    {
      //      //right
      //      this.input.inputState.MoveX -= 1;
      //      this.input.inputState.AimAxis.X += 1;
      //    }
      //    else
      //    {
      //      //left
      //      this.input.inputState.AimAxis.X += 1;
      //      this.input.inputState.MoveX += 1;
      //    }
      //  }
      //}
      ////Game Mode versus
      //else 
      if (shoot.Count == 0 &&
          enemyPosition.Y >= agentPosition.Y && enemyPosition.Y - agentPosition.Y < 50
        && (enemyPosition.X > agentPosition.X && enemyPosition.X - agentPosition.X < 100)
           || (enemyPosition.X < agentPosition.X && agentPosition.X - enemyPosition.X < 100))
      {
        if (agentPosition.X < enemyPosition.X)
        {
          //left
          this.input.inputState.AimAxis.X -= 1;
          this.input.inputState.MoveX -= 1;
        }
        else
        {
          //right
          this.input.inputState.MoveX += 1;
          this.input.inputState.AimAxis.X += 1;
        }
      }
      else if (shoot.Count == 0)
      {
        if (agentPosition.X < enemyPosition.X)
        {
          //right
          this.input.inputState.MoveX += 1;
          this.input.inputState.AimAxis.X += 1;
        }
        else
        {
          //left
          this.input.inputState.AimAxis.X -= 1;
          this.input.inputState.MoveX -= 1;
        }
      }


      /////////////////////////////////////////////
      //  # If in the same line shoots,
      //  if abs(my_pos['y'] - enemy_pos['y']) < enemy_state['size']['y']:
      //    if random.randint(0, 1) == 0:
      //      self.press('s')
      /////////////////////////////////////////////
      //if (!agent.playTagCountDownOn && agent.Arrows.Count > 0) //TODO playtag
      if (agent.Arrows.Count > 0)
      //if (!PlayTagImport.IsPlayTagCountDownOn(agent.PlayerIndex) && agent.Arrows.Count > 0) 
      //if (agent.Arrows.Count > 0)
      {
        if (shoot.Count > 0)
        {
          Moves.Shoot(ref this.input.inputState, shoot);
        }
        else if (0 == random.Next(0, 2))
        {
          string directionShoot = "";

          // If enemy in same line +/- sprite height
          if (Math.Abs(agentPosition.Y - enemyPosition.Y) < enemy.Height)
          //if (true)
          {
            // if enemy left
            if (enemyPosition.X < agentPosition.X)
            {
              directionShoot += "l";
            }
            else
            {
              //enemy right
              directionShoot += "r";

            }
          }
          //If enemy in same vertical +/- sprite width
          if (Math.Abs(agentPosition.X - enemyPosition.X) < enemy.Width)
          {
            // enemy above
            if (agentPosition.Y < enemyPosition.Y)
            {
              directionShoot += "u";

            }
            else if (!agent.OnGround)
            { //enemy above
              directionShoot += "d";
            }
          }

          if (directionShoot.Length > 0)
          {
            Moves.Shoot(ref this.input.inputState, shoot, directionShoot);
          }
        }
      }
      else
      {
        shoot.Clear();
      }

      /////////////////////////////////////////////
      //# Presses dash in 1/10 of the frames.
      //if random.randint(0, 9) == 0:
      //  self.press('z')
      /////////////////////////////////////////////
      if (shoot.Count == 0 && 0 == random.Next(0, 9))
      //if (0 == random.Next(0, 9))
      {
        this.input.inputState.DodgeCheck = true;
        this.input.inputState.DodgePressed = !this.input.prevInputState.DodgeCheck;
      }

      /////////////////////////////////////////////
      //# Presses jump in 1/20 of the frames.
      //if random.randint(0, 19) == 0:
      //  self.press('j')
      /////////////////////////////////////////////
      if (shoot.Count == 0 && 0 == random.Next(0, 19))
      //if (0 == random.Next(0, 19))
      {
        this.input.inputState.JumpCheck = true;
        this.input.inputState.JumpPressed = !this.input.prevInputState.JumpCheck;
      }

      this.input.prevInputState = this.input.GetCopy(this.input.inputState);
    }
  }
}
