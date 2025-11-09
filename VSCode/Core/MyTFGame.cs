using Microsoft.Xna.Framework;
using TowerFall;

namespace TFModFortRiseAiSimple
{
  internal class MyTFGame
  {
    static bool sessionStarted = false;
    static int counter = 0;
    public static bool sandbox = true;
    public static bool displayPath = true;
    public static int level = 1;
    public static int sublevel = 1;
    public static bool customLevel = true;
    internal static void Load()
    {
      On.TowerFall.TFGame.Update += Update_patch;
    }

    internal static void Unload()
    {
      On.TowerFall.TFGame.Update -= Update_patch;
    }

    public static void Update_patch(On.TowerFall.TFGame.orig_Update orig, global::TowerFall.TFGame self, GameTime gameTime)
    {
      orig(self, gameTime);
      if (LoaderAIImport.CanAddAgent())
      {
        AISi.CreateAgent();
      }

      if (MyTFGame.sandbox) {
        if (TFGame.GameLoaded && AISi.isAgentReady && !sessionStarted && counter > 5000) //wait 5s to sfx load
        {
          //base.MainMenu.State = MainMenu.MenuState.Main;
          AISi.StartNewSession();
          sessionStarted = true;
        }
        counter++;
      }
    }
  }
}
