using FortRise;
using HarmonyLib;
using Microsoft.Xna.Framework;
using TFModFortRiseLoaderAI;
using TowerFall;

namespace TFModFortRiseAiSimple
{
  internal class MyTFGame : IHookable
  {
    public static void Load(IHarmony harmony)
    {
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(TFGame), "Update"),
          prefix: new HarmonyMethod(Update_patch)
      );
    }

    public static void Update_patch(TFGame __instance)
    {
      if (TFModFortRiseAiSimpleModule.Instance.LoaderAIModApi.CanAddAgent())
      {
        AISi.CreateAgent();
      }
    }
  }
}
