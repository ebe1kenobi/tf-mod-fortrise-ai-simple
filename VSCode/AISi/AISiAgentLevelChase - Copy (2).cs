//using TowerFall;
//using System;
//using Microsoft.Xna.Framework;
//using Monocle;
//using System.Runtime.InteropServices;
//using MonoMod.Utils;
//using System.Collections.Generic;

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
//      const int GRID_W = 32;
//      const int GRID_H = 24;
//      const int BLOCK_SIZE = 10;

//      const int JUMP_HEIGHT_BLOCKS = 4;      // saut vertical normal
//      const int DIRECTIONAL_JUMP_BLOCKS = 7; // saut directionnel ou dash
//      const float TILE_COST_WALK = 1f;
//      const float TILE_COST_JUMP = 2f;
//      const float TILE_COST_COMBO = 3f;

//      if (!levelCalculated)
//      {
//        UpdateLevelGrid();
//        levelCalculated = true;
//      }

//      Player agent = level.GetPlayer(index);
//      if (agent == null || agent.Dead)
//      {
//        this.input.prevInputState = this.input.GetCopy(this.input.inputState);
//        return;
//      }

//      this.input.prevInputState = this.input.GetCopy(this.input.inputState);
//      this.input.inputState = new InputState();
//      this.input.inputState.MoveX = 0;
//      this.input.inputState.MoveY = 0;
//      this.input.inputState.AimAxis.X = 0;
//      this.input.inputState.AimAxis.Y = 0;

//      // 🟢 1. Trouver l'ennemi le plus proche
//      Player enemy = FindNearestEnemy(agent);
//      if (enemy == null) return;

//      //TODO nex move
//      //1. go left
//      this.input.inputState.AimAxis.X -= 1;
//      this.input.inputState.MoveX -= 1; 

//      //2.go right
//      this.input.inputState.MoveX += 1;
//      this.input.inputState.AimAxis.X += 1;

//      //3.look up  // direction shoot up
//      this.input.inputState.AimAxis.Y -= 1;
//      this.input.inputState.MoveY -= 1;

//      //4.look down // crouch // direction shoot down
//      this.input.inputState.MoveY += 1;
//      this.input.inputState.AimAxis.Y += 1;

//      //5.no movement to left or right 
//      this.input.inputState.AimAxis.X -= 0;
//      this.input.inputState.MoveX -= 0;

//      //6dash (to the left or right or top or bottom with MoveX  MoveY AimAxis.
//      // if MoveX  MoveY AimAxis =0, then dash to the directionthe player face at ,this value is in player.Facing (enum {Right, Left})
//      this.input.inputState.DodgeCheck = true;
//      this.input.inputState.DodgePressed = !this.input.prevInputState.DodgeCheck;

//      //10. jump, if no directionjump vertically, else use  MoveX  MoveY AimAxis to give a direction
//      this.input.inputState.JumpCheck = true;
//      this.input.inputState.JumpPressed = !this.input.prevInputState.JumpCheck;
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
//        levelGrid = new int[LEVEL_HEIGHT, LEVEL_WIDTH];
//      }

//      // Réinitialiser la grille
//      for (int y = 0; y < LEVEL_HEIGHT; y++)
//      {
//        for (int x = 0; x < LEVEL_WIDTH; x++)
//        {
//          levelGrid[y, x] = 0;
//          if (level.CollideCheck(new Vector2(x * 10, y * 10), GameTags.Solid))
//          {
//            levelGrid[y, x] = 1;
//          }
//        }
//      }
//      DebugPrintGrid();
//    }

//    // Méthode debug pour afficher la grille (à retirer plus tard)
//    private void DebugPrintGrid()
//    {
//      Logger.Info("DebugPrintGrid");
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
//  }
//}
