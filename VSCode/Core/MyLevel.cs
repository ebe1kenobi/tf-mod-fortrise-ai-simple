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

    public static void Update_patch(On.TowerFall.Level.orig_Update orig, global::TowerFall.Level self) {

      if (!MyTFGame.sandbox)
      {
        orig(self);
        if (MyTFGame.displayPath && !sandboxEntityCreated) { 
          self.Add(new DebugPathRenderer(AISi.agents[1]));
          self.UpdateEntityLists();
          sandboxEntityCreated = true;
        }
        return;
      }


      int playerIndex;
      //SANDBOX
      if (!sandboxEntityCreated)
      {
        if (MyTFGame.displayPath)
          self.Add(new DebugPathRenderer(AISi.agents[1]));
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

      if (update)
      {
        //        //delete all corpse todo
        //        //foreach (PlayerCorpse item2 in self[GameTags.Corpse])
        //        //{
        //        //  Logger.Info("corpse found");
        //        //  item2.RemoveSelf();
        //        //}

        //        self.Remove(GameTags.Corpse); //don t work
        ////self.unt
        ////Corpse
        ////UntagEntity(entity, GameTags tag)
        self.UpdateEntityLists();
      }
      //disable arrow
      //disable stomhead

    }

    public static void HandlePausing_patch(On.TowerFall.Level.orig_HandlePausing orig, global::TowerFall.Level self)
    {
      if (MyTFGame.sandbox)
      {
        return; //todo training
      }
      sandboxEntityCreated = false;

      orig(self);
    }
  }

  
  //}
}
