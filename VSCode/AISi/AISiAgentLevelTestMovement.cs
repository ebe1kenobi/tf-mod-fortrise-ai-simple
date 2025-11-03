using TowerFall;
using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using TFModFortRiseLoaderAI;
using Monocle;
using MonoMod.Utils;
using System.Runtime.InteropServices;

namespace TFModFortRiseAiSimple
{
  public class AISiAgentLevelTestMovement : TFModFortRiseLoaderAI.Agent
  {
    protected List<InputState> movement = new List<InputState>();
    protected int step = 0;
    protected Player player;
    private int[,] levelGrid;
    private const int LEVEL_WIDTH = 32;
    private const int LEVEL_HEIGHT = 24;
    private const int BLOCK_SIZE = 10;
    private bool ok = false;
    public AISiAgentLevelTestMovement(int index, String type, PlayerInput input) : base(index, type, input)
    {
    }

    public Player getPlayer() {
      foreach (Entity entity in level[GameTags.Player])
      {
        Player p = entity as Player;
        //Logger.Info("p.PlayerIndex = " + p.PlayerIndex);
        if (p.PlayerIndex == this.index)
        {
          return p;
        }
      }
      //Logger.Info("return null");
      return null;
    }

    // Méthode pour initialiser ou mettre à jour la grille du niveau
    private void UpdateLevelGrid()
    {
      Logger.Info("UpdateLevelGrid");
      if (levelGrid == null)
      {
        levelGrid = new int[LEVEL_WIDTH, LEVEL_HEIGHT];
      }

      // Réinitialiser la grille
      for (int x = 0; x < LEVEL_WIDTH; x++)
      {
        for (int y = 0; y < LEVEL_HEIGHT; y++)
        {
          levelGrid[x, y] = 0;
          if (level.CollideCheck(new Vector2(x * 10, 240 - y * 10), GameTags.Solid)) {
            levelGrid[x, y] = 1;
          }
        }
      }
      //Logger.Info("empty grid");
      ////DebugPrintGrid();
      //// Parcourir toutes les entités solides
      //foreach (Entity item in level.Tags[(int)GameTags.Solid])
      //{
      //  // Convertir la position de l'entité en indices de grille
      //  int gridX = (int)(item.Position.X / BLOCK_SIZE);
      //  int gridY = (int)(item.Position.Y / BLOCK_SIZE);
      //  Logger.Info("item : " + gridX + ":" + gridY);

      //  // Vérifier que la position est dans les limites
      //  if (gridX >= 0 && gridX < LEVEL_WIDTH && gridY >= 0 && gridY < LEVEL_HEIGHT)
      //  {
      //    levelGrid[gridX, gridY] = 1; // Marquer comme solide
      //  }
      //}
      //Logger.Info("fill grid");
      DebugPrintGrid();
    }

    // Méthode debug pour afficher la grille (à retirer plus tard)
    private void DebugPrintGrid()
    {
      Logger.Info("DebugPrintGrid");
      for (int y = LEVEL_HEIGHT - 1; y >= 0; y--)
      {
        string line = "";
        for (int x = 0; x < LEVEL_WIDTH; x++)
        {
          line += levelGrid[x, y] == 1 ? "1" : "0";
        }
        Logger.Info(line);
      }
    }


    public override void Move()
    {
      if (!ok) {
        UpdateLevelGrid();
        //ok = true;
      }

      //Logger.Info("Move");
      this.input.inputState = new InputState();
      this.input.inputState.AimAxis.X = 0;
      this.input.inputState.MoveX = 0;
      this.input.inputState.AimAxis.Y = 0;
      this.input.inputState.MoveY = 0;
      this.input.inputState.DodgeCheck = false;
      this.input.inputState.DodgePressed = false;

      player = getPlayer();
      if (player == null) return;

      if (player.State == Player.PlayerStates.Frozen)
      {
        Logger.Info(player.State.ToString());
        return;
      }
      //if (this.level.Frozen) return;
      //gauche

      if (movement.Count == 0 && step == 0) {
        //Logger.Info("movement.Count == 0 && step == 0");
        //this.input.inputState.AimAxis.X -= 1;
        //this.input.inputState.MoveX -= 1;
        InputState move = this.input.inputState;
        move.AimAxis.X = -1;
        move.MoveX = -1;
        for (int i = 0; i < 100; i++) {
          movement.Add(move);
        }
      } 
      if (step == 0) {
        //Logger.Info("step == 0");
        this.input.inputState.AimAxis = movement[0].AimAxis;
        this.input.inputState.MoveX = movement[0].MoveX;
        movement.RemoveAt(0);
        if (movement.Count == 0)
        {
          step++;
        }
      }

      //droite
      if (movement.Count == 0 && step == 1) {
        //Logger.Info("movement.Count == 0 && step == 1");
        InputState move = this.input.inputState;
        move.AimAxis.X = 1;
        move.MoveX = 1;
        for (int i = 0; i < 100; i++)
        {
          movement.Add(move);
        }

      }
      if (step == 1)
      {
        //Logger.Info("step == 1");
        this.input.inputState.AimAxis = movement[0].AimAxis;
        this.input.inputState.MoveX = movement[0].MoveX;
        movement.RemoveAt(0);
        if (movement.Count == 0)
        {
          step++;
        }
      }


      //haut
      if (movement.Count == 0 && step == 2)
      {
        //Logger.Info("movement.Count == 0 && step == 1");
        InputState move = this.input.inputState;
        move.AimAxis.Y = -1;
        move.MoveY = -1;
        for (int i = 0; i < 100; i++)
        {
          movement.Add(move);
        }

      }
      if (step == 2)
      {
        //Logger.Info("step == 1");
        this.input.inputState.AimAxis = movement[0].AimAxis;
        this.input.inputState.MoveY = movement[0].MoveY;
        movement.RemoveAt(0);
        if (movement.Count == 0)
        {
          step++;
        }
      }
      //accroupir
      if (movement.Count == 0 && step == 3)
      {
        //Logger.Info("movement.Count == 0 && step == 1");
        InputState move = this.input.inputState;
        move.AimAxis.Y = 1;
        move.MoveY = 1;
        for (int i = 0; i < 100; i++)
        {
          movement.Add(move);
        }

      }
      if (step == 3)
      {
        //Logger.Info("step == 1");
        this.input.inputState.AimAxis = movement[0].AimAxis;
        this.input.inputState.MoveY = movement[0].MoveY;
        movement.RemoveAt(0);
        if (movement.Count == 0)
        {
          step++;
        }
      }

      //dash gauche
      if (movement.Count == 0 && step == 4)
      {
        InputState move = this.input.inputState;
        move.AimAxis.X = -1;
        move.MoveX = -1;

        //if (!this.dodgeCooldown && this.input.DodgePressed && !base.Level.Session.MatchSettings.Variants.NoDodging[this.PlayerIndex])
        move.DodgeCheck = true;
        move.DodgePressed = !move.DodgeCheck;
        for (int i = 0; i < 3; i++)
        {
          movement.Add(move);

          if (i == 1)
          {
            move.DodgeCheck = false;
            move.DodgePressed = !move.DodgeCheck;
          }
          if (i == 2)
          {
            // no need anymore 
            move.AimAxis.X = 0;
            move.MoveX = 0;
          }
        }

      }
      if (step == 4)
      {
        //Logger.Info("movement.count =" + movement.Count);
        //Logger.Info("dash gauche player speed = " +  player.Speed.ToString());
        this.input.inputState.DodgeCheck = movement[0].DodgeCheck;
        this.input.inputState.DodgePressed = movement[0].DodgePressed;
        this.input.inputState.AimAxis = movement[0].AimAxis;
        this.input.inputState.MoveX = movement[0].MoveX;
        movement.RemoveAt(0);
        if (movement.Count == 0)
        {
          step++;
        }
      }

      //dash droite
      if (movement.Count == 0 && step == 5)
      {
        //Logger.Info("movement.Count == 0 && step == 1");
        InputState move = this.input.inputState;
        for (int i = 0; i < 40; i++)
        {
          //delay needed after previous dash : dodgeCooldown in TowerFall.Player
          //private void LeaveDodge()
          //this.scheduler.ScheduleAction(new Action(this.DodgeCooldown), 25, false);
          movement.Add(move);
        }
        move.AimAxis.X = 1;
        move.MoveX = 1;
        move.DodgeCheck = true;
        move.DodgePressed = !move.DodgeCheck;
        for (int i = 0; i < 50; i++)
        {
          movement.Add(move);

          if (i == 1)
          {
            move.DodgeCheck = false;
            move.DodgePressed = !move.DodgeCheck;
          }
          //if (i == 2)
          //{
          //  // no need anymore after release dodge
          //  move.AimAxis.X = 0;
          //  move.MoveX = 0;
          //}
        }

      }
      if (step == 5)
      {
        //Logger.Info("step == 1");
        //wait cooldown end
        //player.Speed.X = 0.7036178f;
        //Logger.Info("dash droite player speed = " + player.Speed.ToString());
        this.input.inputState.DodgeCheck = movement[0].DodgeCheck;
        this.input.inputState.DodgePressed = movement[0].DodgePressed;
        this.input.inputState.AimAxis = movement[0].AimAxis;
        this.input.inputState.MoveX = movement[0].MoveX;
        movement.RemoveAt(0);
        if (movement.Count == 0)
        {
          step++;
        }
      }


      //super dash gauche
      //this.dodgeStallCounter.Set(5)  (in enterDodge()
      //if (this.dodgeStallCounter && this.input.DodgeCheck)
      //if (movement.Count == 0 && step == 6)
      //{
      //  //Logger.Info("movement.Count == 0 && step == 1");
      //  InputState move = this.input.inputState;
      //  for (int i = 0; i < 100; i++)
      //  {
      //    //delay needed after previous dash : dodgeCooldown in TowerFall.Player
      //    //private void LeaveDodge()
      //    //this.scheduler.ScheduleAction(new Action(this.DodgeCooldown), 25, false);
      //    movement.Add(move);
      //  }
      //  move.AimAxis.X = -1;
      //  move.MoveX = -1;
      //  move.DodgeCheck = true;
      //  move.DodgePressed = !move.DodgeCheck;
      //  for (int i = 0; i < 100; i++)
      //  {
      //    movement.Add(move);

      //    if (i == 1)
      //    {
      //      move.DodgeCheck = false;
      //      move.DodgePressed = !move.DodgeCheck;
      //    }

      //    //if (this.dodgeStallCounter && this.input.DodgeCheck)
      //    if (i == 5)
      //    {
      //      move.DodgeCheck = true;
      //      move.DodgePressed = !move.DodgeCheck;
      //    }

      //    if (i == 10)
      //    {
      //      move.DodgeCheck = false;
      //      move.DodgePressed = !move.DodgeCheck;
      //    }
      //    //if (i == 2)
      //    //{
      //    //  // no need anymore after release dodge
      //    //  move.AimAxis.X = 0;
      //    //  move.MoveX = 0;
      //    //}
      //  }

      //}
      //if (step == 6)
      //{
      //  //Logger.Info("step == 1");
      //  //wait cooldown end
      //  //player.Speed.X = 0.7036178f;
      //  //Logger.Info("dash droite player speed = " + player.Speed.ToString());
      //  this.input.inputState.DodgeCheck = movement[0].DodgeCheck;
      //  this.input.inputState.DodgePressed = movement[0].DodgePressed;
      //  this.input.inputState.AimAxis = movement[0].AimAxis;
      //  this.input.inputState.MoveX = movement[0].MoveX;
      //  movement.RemoveAt(0);
      //  if (movement.Count == 0)
      //  {
      //    step++;
      //  }
      //}

      //step = 0;

      //super dash droite

      //sauter haut

      //sauter haut gauche

      //sauter haut droite

      //saut plus dash haut

      //saut plus super dash haut

    }
  }
}
