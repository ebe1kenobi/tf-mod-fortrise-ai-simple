using System;
//using System.Drawing;
using System.Linq;
using System.Reflection;
using IL.MonoMod;
using Microsoft.Xna.Framework;
using Monocle;
using Newtonsoft.Json.Linq;
using Color = Microsoft.Xna.Framework.Color;
using TowerFall;
using Microsoft.Xna.Framework;


namespace TFModFortRiseAiSimple
{
  public class MyLevel {

    public static bool sandboxEntityCreated = false;

    internal static void Load()
    {
      On.TowerFall.Level.Update += Update_patch;
      On.TowerFall.Level.HandlePausing += HandlePausing_patch;
      //On.TowerFall.Level.Render += Render_patch;
    }

    internal static void Unload()
    {
      On.TowerFall.Level.Update -= Update_patch;
      On.TowerFall.Level.HandlePausing -= HandlePausing_patch;
      //On.TowerFall.Level.Render -= Render_patch;
    }

    //public static void Render_patch(On.TowerFall.Level.orig_Render orig, global::TowerFall.Level self){
    //  orig(self);


      //try
      //{
      //  if (AISi.agents[1].debugPath == null || AISi.agents[1].debugPath.Count == 0)
      //    return;

      //  foreach (var cell in AISi.agents[1].debugPath)
      //  {
      //    // conversion cellule -> coordonnées du monde
      //    float x = cell.X * AISiAgentLevelChase.BLOCK_SIZE;
      //    float y = cell.Y * AISiAgentLevelChase.BLOCK_SIZE;

      //    // couleur selon position
      //    Microsoft.Xna.Framework.Color color = Microsoft.Xna.Framework.Color.Yellow;

      //    if (cell.Equals(AISi.agents[1].debugPath[0]))
      //      color = Microsoft.Xna.Framework.Color.Green; // départ
      //    else if (cell.Equals(AISi.agents[1].debugPath[AISi.agents[1].debugPath.Count - 1]))
      //      color = Microsoft.Xna.Framework.Color.Red;   // but

      //    // dessiner un rectangle transparent
      //    Draw.Rect(x, y, AISiAgentLevelChase.BLOCK_SIZE, AISiAgentLevelChase.BLOCK_SIZE, color * 0.4f);
      //  }
      //}
      //catch (InvalidOperationException e)
      //{
      //  // Log ou ignorer
      //  Logger.Log("Render", "Tentative de Draw avant Begin()");
      //}



      //if (player != null)
      //{
      //  Point p = WorldToCell(player.Position);
      //  Draw.Rect(p.X * BLOCK_SIZE, p.Y * BLOCK_SIZE, BLOCK_SIZE, BLOCK_SIZE, Color.Blue * 0.5f);
      //}
    //}

    //TODO render
    //public override void Render()
    //{
    //  base.Render();

    //  if (debugPath == null || debugPath.Count == 0)
    //    return;

    //  foreach (var cell in debugPath)
    //  {
    //    // conversion cellule -> coordonnées du monde
    //    float x = cell.X * BLOCK_SIZE;
    //    float y = cell.Y * BLOCK_SIZE;

    //    // couleur selon position
    //    Color color = Color.Yellow;

    //    if (cell.Equals(debugPath.First()))
    //      color = Color.Green; // départ
    //    else if (cell.Equals(debugPath.Last()))
    //      color = Color.Red;   // but

    //    // dessiner un rectangle transparent
    //    Draw.Rect(x, y, BLOCK_SIZE, BLOCK_SIZE, color * 0.4f);
    //  }

    //if (player != null)
    //{
    //    Point p = WorldToCell(player.Position);
    //    Draw.Rect(p.X* BLOCK_SIZE, p.Y* BLOCK_SIZE, BLOCK_SIZE, BLOCK_SIZE, Color.Blue* 0.5f);
    //}
    //}

    public static void Update_patch(On.TowerFall.Level.orig_Update orig, global::TowerFall.Level self) {
      int playerIndex;
      //SANDBOX
      if (!sandboxEntityCreated)
      {
        //var player = EntityCreator.CreatePlayer(e, playerIndex, self.Session.MatchSettings.GetPlayerAllegiance(playerIndex));
        //for (int i = 0; i < AIPython.Config.agents.Count; i++) {
        //  var player1 = EntityCreator.CreatePlayer(i, self.Session.MatchSettings.GetPlayerAllegiance(i), AIPython.Config.agents[i].X, AIPython.Config.agents[i].Y);
        //  self.Add(player1);
        //}
        self.Add(new DebugPathRenderer());
        playerIndex = 0;
        var player1 = EntityCreator.CreatePlayer(playerIndex, self.Session.MatchSettings.GetPlayerAllegiance(playerIndex), 15, 220);
        self.Add(player1);
        playerIndex++;
        var player2 = EntityCreator.CreatePlayer(playerIndex, self.Session.MatchSettings.GetPlayerAllegiance(playerIndex), 305, 220);
        self.Add(player2);
        playerIndex++;
        //var player2 = EntityCreator.CreatePlayer(playerIndex, self.Session.MatchSettings.GetPlayerAllegiance(playerIndex));
        //self.Add(player2);
        //playerIndex++;
        //var player3 = EntityCreator.CreatePlayer(playerIndex, self.Session.MatchSettings.GetPlayerAllegiance(playerIndex));
        //self.Add(player3);
        //playerIndex++;
        //var player4 = EntityCreator.CreatePlayer(playerIndex, self.Session.MatchSettings.GetPlayerAllegiance(playerIndex));
        //self.Add(player4);
        //playerIndex++;


        //Entity entity = EntityCreator.CreateSlime(new JObject());
        //self.Add(entity);

        self.UpdateEntityLists();
        MyLevel.sandboxEntityCreated = true;
      }

      orig(self);

      //detectEndgame(); //reset and add player
      playerIndex = 1;
      Player player = self.GetPlayer(playerIndex); //todo check 
      bool update = false;
      if (player == null || player.Dead)
      {
        //Logger.Info("player dead");
        var player1 = EntityCreator.CreatePlayer(playerIndex, self.Session.MatchSettings.GetPlayerAllegiance(playerIndex), 305, 220);
        self.Add(player1);
        update = true;
      } else {
        //Logger.Info("player not dead");
      }
      int enemyIndex = 0;
      Player enemy = self.GetPlayer(enemyIndex);

      if (enemy == null || enemy.Dead)
      {
        //Logger.Info("enemy dead");
        var player2 = EntityCreator.CreatePlayer(enemyIndex, self.Session.MatchSettings.GetPlayerAllegiance(playerIndex), 15, 220);
        self.Add(player2);
        update = true;
      }
      else
      {
        //Logger.Info("enemy not dead");
      }

      if (update)
      {
        //delete all corpse todo
        //foreach (PlayerCorpse item2 in self[GameTags.Corpse])
        //{
        //  Logger.Info("corpse found");
        //  item2.RemoveSelf();
        //}

        self.Remove(GameTags.Corpse); //don t work
//self.unt
//Corpse
//UntagEntity(entity, GameTags tag)
        self.UpdateEntityLists();
      }
      //disable arrow
      //disable stomhead

    }

    public static void HandlePausing_patch(On.TowerFall.Level.orig_HandlePausing orig, global::TowerFall.Level self)
    {
      return; //todo training
      orig(self);
    }
  }

  public class DebugPathRenderer : Entity
  {
    public override void Render()
    {
      base.Render();

      if (AISi.agents[1].debugPath == null || AISi.agents[1].debugPath.Count == 0)
        return;

      foreach (var cell in AISi.agents[1].debugPath)
      {
        float x = cell.X * AISiAgentLevelChase.BLOCK_SIZE;
        float y = cell.Y * AISiAgentLevelChase.BLOCK_SIZE;

        Color color = Color.Yellow;
        //if (cell.Equals(AISi.agents[1].debugPath[0]))
        if (cell.Equals(AISi.agents[1].debugPath.First()))
            color = Color.Green;
        if (cell.Equals(AISi.agents[1].debugPath.Last()))
        //else if (cell.Equals(AISi.agents[1].debugPath[1]))
          color = Color.Red;

        Draw.Rect(x, y, AISiAgentLevelChase.BLOCK_SIZE, AISiAgentLevelChase.BLOCK_SIZE, color);
      }

      Player player = AISi.agents[1].player;
      if (player != null)
      {
        Point p = AISi.agents[1].WorldToCell(player.Position);
        Draw.Rect(p.X * AISiAgentLevelChase.BLOCK_SIZE, p.Y * AISiAgentLevelChase.BLOCK_SIZE, AISiAgentLevelChase.BLOCK_SIZE, AISiAgentLevelChase.BLOCK_SIZE, Color.Blue * 0.5f);
      }

      Player enemy = AISi.agents[1].enemy;
      if (enemy != null)
      {
        Point p = AISi.agents[1].WorldToCell(enemy.Position);
        Draw.Rect(p.X * AISiAgentLevelChase.BLOCK_SIZE, p.Y * AISiAgentLevelChase.BLOCK_SIZE, AISiAgentLevelChase.BLOCK_SIZE, AISiAgentLevelChase.BLOCK_SIZE, Color.Green * 0.5f);
      }
    }
  }
  //}
}
