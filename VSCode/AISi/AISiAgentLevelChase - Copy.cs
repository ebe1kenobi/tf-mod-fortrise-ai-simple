//using TowerFall;
//using System;
//using Microsoft.Xna.Framework;
//using Monocle;
//using System.Runtime.InteropServices;
//using MonoMod.Utils;

//namespace TFModFortRiseAiSimple
//{
//  public class AISiAgentLevelChase : TFModFortRiseLoaderAI.Agent
//  {
//    private int[,] levelGrid;
//    private const int LEVEL_WIDTH = 32;  // 32 BLOCK
//    private const int LEVEL_HEIGHT = 24; // 24 BLOCK
//    private const int BLOCK_SIZE = 10;
//    private static bool levelCalculated = false;
//    public AISiAgentLevelChase(int index, String type, PlayerInput input) : base(index, type, input) { }

//    //Move() is called at each update frame
//    protected override void Move()
//    {
//      if (!levelCalculated)
//      {
//        UpdateLevelGrid();
//        levelCalculated = true;
//      }
//      //get the current Player instance for this agent with index
//      Player agent = level.GetPlayer(index);
//      if (agent == null) return; //TODO don't know if works when player died ... apparently yes
//      if (agent.Dead)
//      {
//        this.input.prevInputState = this.input.GetCopy(this.input.inputState);
//        return;
//      }

//      this.input.prevInputState = this.input.GetCopy(this.input.inputState);

//      this.input.inputState = new InputState();
//      this.input.inputState.AimAxis.X = 0;
//      this.input.inputState.MoveX = 0;
//      this.input.inputState.AimAxis.Y = 0;
//      this.input.inputState.MoveY = 0;

//      //1. go left
//      //this.input.inputState.AimAxis.X -= 1;
//      //this.input.inputState.MoveX -= 1; 

//      //2.go right
//      //this.input.inputState.MoveX += 1;
//      //this.input.inputState.AimAxis.X += 1;

//      //3.look up  // direction shoot up
//      //this.input.inputState.AimAxis.Y -= 1;
//      //this.input.inputState.MoveY -= 1;

//      //4.look down // crouch // direction shoot down
//      //this.input.inputState.MoveY += 1;
//      //this.input.inputState.AimAxis.Y += 1;

//      //5.no movement to left or right 
//      //this.input.inputState.AimAxis.X -= 0;
//      //this.input.inputState.MoveX -= 0;

//      //6dash (to the left or right or top or bottom with MoveX  MoveY AimAxis.
//      // if MoveX  MoveY AimAxis =0, then dash to the directionthe player face at ,this value is in player.Facing (enum {Right, Left})
//      //this.input.inputState.DodgeCheck = true;
//      //this.input.inputState.DodgePressed = !this.input.prevInputState.DodgeCheck;

//      //7.super dash , it's a quickestand longest dash, 
//      // do a dash for a frame
//      //frame 1 this.input.inputState.DodgeCheck = true;
//      //this.input.inputState.DodgePressed = !this.input.prevInputState.DodgeCheck;
//      //frame 2 reset the dash for a frame
//      //this.input.inputState.DodgeCheck = false;
//      //this.input.inputState.DodgePressed = !this.input.prevInputState.DodgeCheck;
//      //frame 3 redo a dash and maintain it the next frames
//      //this.input.inputState.DodgeCheck = true;
//      //this.input.inputState.DodgePressed = !this.input.prevInputState.DodgeCheck;

//      //8. draw the bow (with the arrow)
//      //this.input.inputState.ShootCheck = true;
//      //this.input.inputState.ShootPressed = true;

//      //9. release the arrow is more complicated, it's a succession of movement
//      //// 1. draw the bow
//      //this.input.inputState.ShootCheck = true;
//      //this.input.inputState.ShootPressed = true;
//      //// 2. todo
//      //this.input.inputState.ShootCheck = false;
//      //this.input.inputState.ShootPressed = false;

//      //10. jump, if no directionjump vertically, else use  MoveX  MoveY AimAxis to give a direction
//      //this.input.inputState.JumpCheck = true;
//      //this.input.inputState.JumpPressed = !this.input.prevInputState.JumpCheck;
//      //jump against a wall will not jump vertically, it will always jump with the opposed direction of the wall, like a bounce
//      // jump vertically jump 4 blocksize
//      // if there is a block 4 block up, you can't jump on the fourth block, you need to jumps and grabs onto the edge of the wall to the drection of the block, then jump again and maintain the direction
//      //if you jump then dash with the up direction you can attain block 7 and grab the edge of the wall
//      //if you jump to the left you go the the 4 block if you stop pressing the direction
//      //if you jump to the left you go the the 7 block if you press the direction until the end of the jump
//      //You knew the Player will rebound against a wall when it will jump with CanWallJump(agent)

//      //You knew the entity is On ground with isOnGround(agent)

        




//    }

//    bool isOnGround(Player agent)
//    {
//      return agent.OnGround;

//    }

//    bool CanWallJump(Player agent)
//    {
//      var dynData = DynamicData.For(agent);
//      bool CanWallJump = dynData.Invoke<bool>("CanWallJump", Facing.Left) || dynData.Invoke<bool>("CanWallJump", Facing.Right);
//      dynData.Dispose();
//      return CanWallJump;
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

//    private void UpdateLevelGrid()
//    {
//      Logger.Info("UpdateLevelGrid");
//      if (levelGrid == null)
//      {
//        levelGrid = new int[LEVEL_WIDTH, LEVEL_HEIGHT];
//      }

//      // Réinitialiser la grille
//      for (int x = 0; x < LEVEL_WIDTH; x++)
//      {
//        for (int y = 0; y < LEVEL_HEIGHT; y++)
//        {
//          levelGrid[x, y] = 0;
//          if (level.CollideCheck(new Vector2(x * 10, 240 - y * 10), GameTags.Solid))
//          {
//            levelGrid[x, y] = 1;
//          }
//        }
//      }
//      DebugPrintGrid();
//    }

//    // Méthode debug pour afficher la grille (à retirer plus tard)
//    private void DebugPrintGrid()
//    {
//      Logger.Info("DebugPrintGrid");
//      for (int y = LEVEL_HEIGHT - 1; y >= 0; y--)
//      {
//        string line = "";
//        for (int x = 0; x < LEVEL_WIDTH; x++)
//        {
//          line += levelGrid[x, y] == 1 ? "1" : "0";
//        }
//        Logger.Info(line);
//      }
//    }
//  }
//}
