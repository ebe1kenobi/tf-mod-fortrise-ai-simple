using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TFModFortRiseAI.Abstractions;
using TFModFortRiseAiSimple;
using TowerFall;

namespace TFModFortRiseAiSimple
{
  public class SimpleAILogic : IAgentLogic
  {
    public string Type => "SimpleAI";

    private Level level;
    private int index;
    private KeyboardInput input;
    protected List<InputState> shoot = new List<InputState>();
    protected Random random;
    public InputState prevInputState;
    public InputState inputState;

    //const int X = 0;
    //const int Y = 1;
    //const int JUMP = 2;
    //const int DODGE = 3;
    //const int SHOOT = 4;

    public void Initialize(int index, KeyboardInput input)
    {
      //Logger.Info($"SimpleAILogic.Initialize {index}");
      this.index = index;
      this.input = input;
      random = new Random(index * 666);
    }

    public void SetLevel(Level level)
    {
      //Logger.Info("SimpleAILogic.SetLevel");
      this.level = level;
    }

    public System.Collections.Generic.List<int> Update()
    {
      //Logger.Info($"SimpleAILogic.Update  {index}");
      // your Move() code goes here
      return Move();
    }

    public void setX(int x) {
      inputState.AimAxis.X = x;
      inputState.MoveX = x;
    }
    public void setY(int y)
    {
      inputState.AimAxis.Y = y;
      inputState.MoveY = y;
    }

    public void setJump(bool jump) {
      inputState.JumpCheck = jump;
      inputState.JumpPressed = !inputState.JumpCheck;
    }

    public void setDodge(bool dodge)
    {
      inputState.DodgeCheck = dodge;
      inputState.DodgePressed = !inputState.DodgeCheck;
    }


    List<int> actions = new List<int>() { 0, 0, 0, 0, 0, 0, 0 };

    List<int> GetAction() {
      actions[IAgentLogic.X] = inputState.MoveX;
      actions[IAgentLogic.Y] = inputState.MoveY;
      actions[IAgentLogic.JUMP] = inputState.JumpCheck ? 1 : 0;
      actions[IAgentLogic.DODGE] = inputState.DodgeCheck ? 1 : 0;
      actions[IAgentLogic.SHOOT] = inputState.ShootCheck ? 1 : 0;
      return actions;     
    }
    public void setCopy() {
      //this.input.prevInputState = this.input.GetCopy(this.input.inputState);
      prevInputState = new InputState
      {
        AimAxis = inputState.AimAxis,
        ArrowsPressed = inputState.ArrowsPressed,
        DodgeCheck = inputState.DodgeCheck,
        DodgePressed = inputState.DodgePressed,
        JumpCheck = inputState.JumpCheck,
        JumpPressed = inputState.JumpPressed,
        MoveX = inputState.MoveX,
        MoveY = inputState.MoveY,
        ShootCheck = inputState.ShootCheck,
        ShootPressed = inputState.ShootPressed
      };
    }

    protected System.Collections.Generic.List<int> Move()
    {
      //var dynData = DynamicData.For(this.input);
      inputState = new InputState();

      //this.input.inputState = new InputState();
      //this.input.inputState.AimAxis.X = 0;
      //this.input.inputState.MoveX = 0;
      //this.input.inputState.AimAxis.Y = 0;
      //this.input.inputState.MoveY = 0;

      //inputState = new InputState();
      inputState.AimAxis.X = 0;
      inputState.MoveX = 0;
      inputState.AimAxis.Y = 0;
      inputState.MoveY = 0;
      //inputState.JumpCheck = true;


      //dynData.Set("inputState", inputState);
      //dynData.Dispose();
      //return;
      //dynData.Set("inputState", inputState);
      /////////////////////////////////////////////
      //# If the agent is not present, it means it is dead.
      //if self.my_state == None:
      //# You are required to reply with actions, or the agent will get disconnected.
      //# logging.info('send_actions')
      //self.send_actions()
      //return True
      /////////////////////////////////////////////
      //Logger.Info($"SimpleAILogic.1  {index}");

      Player agent = level.GetPlayer(index);
      if (agent == null) return GetAction(); //TODO don't know if works when player died ... apparently yes
      if (agent.Dead)
      {
        //this.input.prevInputState = this.input.GetCopy(this.input.inputState);
        setCopy();
      //Logger.Info("SimpleAILogic.2");
        //dynData.Set("inputState", inputState);
        return GetAction();
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
      //Logger.Info("SimpleAILogic.3");

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
      //Logger.Info("SimpleAILogic.4");

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
        //this.input.prevInputState = this.input.GetCopy(this.input.inputState);
        setCopy();

        //dynData.Set("inputState", inputState);
        return GetAction();
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
          //this.input.inputState.AimAxis.X -= 1;
          //this.input.inputState.MoveX -= 1;

          setX(-1);
        }
        else
        {
          //right
          //this.input.inputState.MoveX += 1;
          //this.input.inputState.AimAxis.X += 1;

          setX(1);

        }
      }
      else if (shoot.Count == 0)
      {
        if (agentPosition.X < enemyPosition.X)
        {
          //right
          //this.input.inputState.MoveX += 1;
          //this.input.inputState.AimAxis.X += 1;

          setX(1);

        }
        else
        {
          //left
          //this.input.inputState.AimAxis.X -= 1;
          //this.input.inputState.MoveX -= 1;

          setX(-1);


        }
      }

      //Logger.Info("SimpleAILogic.5");

      /////////////////////////////////////////////
      //  # If in the same line shoots,
      //  if abs(my_pos['y'] - enemy_pos['y']) < enemy_state['size']['y']:
      //    if random.randint(0, 1) == 0:
      //      self.press('s')
      /////////////////////////////////////////////
      //if (!agent.playTagCountDownOn && agent.Arrows.Count > 0) //TODO playtag
      if (false && agent.Arrows.Count > 0)
      //if (!PlayTagImport.IsPlayTagCountDownOn(agent.PlayerIndex) && agent.Arrows.Count > 0) 
      //if (agent.Arrows.Count > 0)
      {
        if (shoot.Count > 0)
        {
          Moves.Shoot(ref inputState, shoot);
          Moves.Shoot(ref inputState, shoot);
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
            Moves.Shoot(ref inputState, shoot, directionShoot);
            Moves.Shoot(ref inputState, shoot, directionShoot);
          }
        }
      }
      else
      {
        shoot.Clear();
      }
      //Logger.Info("SimpleAILogic.6");

      /////////////////////////////////////////////
      //# Presses dash in 1/10 of the frames.
      //if random.randint(0, 9) == 0:
      //  self.press('z')
      /////////////////////////////////////////////
      if (shoot.Count == 0 && 0 == random.Next(0, 9))
      //if (0 == random.Next(0, 9))
      {
        //this.input.inputState.DodgeCheck = true;
        //this.input.inputState.DodgePressed = !this.input.prevInputState.DodgeCheck;
        Logger.Info("ok dodge");

        setDodge(true);

      }

      /////////////////////////////////////////////
      //# Presses jump in 1/20 of the frames.
      //if random.randint(0, 19) == 0:
      //  self.press('j')
      /////////////////////////////////////////////
      if (shoot.Count == 0 && 0 == random.Next(0, 19))
      //if (0 == random.Next(0, 19))
      {
        Logger.Info("ok jump");
        //this.input.inputState.JumpCheck = true;
        //this.input.inputState.JumpPressed = !this.input.prevInputState.JumpCheck;

        //setJump(true);
        inputState.JumpCheck = true;
        inputState.JumpPressed = !inputState.JumpCheck;

      }

      //Logger.Info("SimpleAILogic.7");

      //this.input.prevInputState = this.input.GetCopy(this.input.inputState);
      setCopy();
      //dynData.Set("inputState", inputState);
      //dynData.Dispose();
      return GetAction();
    }
  }
}
