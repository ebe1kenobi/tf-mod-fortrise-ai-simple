using Microsoft.Xna.Framework;
using TowerFall;

namespace TFModFortRiseAiSimple
{
  internal class MyTFGame
  {
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
    }
  }
}
