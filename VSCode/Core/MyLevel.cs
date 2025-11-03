using IL.MonoMod;
using Microsoft.Xna.Framework;
using Monocle;
using Newtonsoft.Json.Linq;

namespace TFModFortRiseAiSimple
{
  public class MyLevel {

    public static bool sandboxEntityCreated = false;

    internal static void Load()
    {
      On.TowerFall.Level.Update += Update_patch;
      On.TowerFall.Level.HandlePausing += HandlePausing_patch;
    }

    internal static void Unload()
    {
      On.TowerFall.Level.Update -= Update_patch;
      On.TowerFall.Level.HandlePausing -= HandlePausing_patch;
    }


		public static void Update_patch(On.TowerFall.Level.orig_Update orig, global::TowerFall.Level self) {
      
      //SANDBOX
      if (!sandboxEntityCreated)
      {
        //var player = EntityCreator.CreatePlayer(e, playerIndex, self.Session.MatchSettings.GetPlayerAllegiance(playerIndex));
        //for (int i = 0; i < AIPython.Config.agents.Count; i++) {
        //  var player1 = EntityCreator.CreatePlayer(i, self.Session.MatchSettings.GetPlayerAllegiance(i), AIPython.Config.agents[i].X, AIPython.Config.agents[i].Y);
        //  self.Add(player1);
        //}
        int playerIndex = 0;
        var player1 = EntityCreator.CreatePlayer(playerIndex, self.Session.MatchSettings.GetPlayerAllegiance(playerIndex), 50, 50);
        self.Add(player1);
        playerIndex++;
        var player2 = EntityCreator.CreatePlayer(playerIndex, self.Session.MatchSettings.GetPlayerAllegiance(playerIndex), 100, 100);
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


        Entity entity = EntityCreator.CreateSlime(new JObject());
        self.Add(entity);

        self.UpdateEntityLists();
        MyLevel.sandboxEntityCreated = true;
      }

      orig(self);
    }

    public static void HandlePausing_patch(On.TowerFall.Level.orig_HandlePausing orig, global::TowerFall.Level self)
    {
      return; //todo training
      orig(self);
    }
  }
}
