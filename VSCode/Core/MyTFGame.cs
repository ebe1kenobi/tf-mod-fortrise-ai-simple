using FortRise;
using HarmonyLib;
using Microsoft.Xna.Framework;
using TFModFortRiseLoaderAI;
using TowerFall;

namespace TFModFortRiseAiSimple
{
  internal class MyTFGame : IHookable
  {
    static bool RegisterAgent = false;
    public static void Load(IHarmony harmony)
    {
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(TFGame), "Update"),
          prefix: new HarmonyMethod(Update_patch)
      );
    }

    public static void Update_patch(TFGame __instance)
    {
      if (TFModFortRiseAiSimpleModule.Instance.LoaderAIModApi.CanAddAgent()&& !RegisterAgent)
      {
        Logger.Info("TFModFortRiseAiSimpleModule RegisterAgent");
        TFModFortRiseAiSimpleModule.Instance.LoaderAIModApi.RegisterAgent(
[
              new SimpleAILogic(),
              new SimpleAILogic(),
              new SimpleAILogic(),
              new SimpleAILogic(),
              new SimpleAILogic(),
              new SimpleAILogic(),
              new SimpleAILogic(),
              new SimpleAILogic(),
]
              );
        RegisterAgent = true;
      }
    }
  }
}
