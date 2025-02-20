using System;
using FortRise;
using MonoMod.ModInterop;

namespace TFModFortRiseAiSimple
{
  [Fort("com.ebe1.kenobi.TFModFortRiseAiExample", "TFModFortRiseAiExample")]
  public class TFModFortRiseAiSimpleModule : FortModule
  {
    public static TFModFortRiseAiSimpleModule Instance;
    public static bool EightPlayerMod;
    public static bool PlayTagMod;

    public override Type SettingsType => typeof(TFModFortRiseAiSimpleSettings);
    public static TFModFortRiseAiSimpleSettings Settings => (TFModFortRiseAiSimpleSettings)Instance.InternalSettings;

    public TFModFortRiseAiSimpleModule()
    {
      Instance = this;
      //Logger.Init("TFModFortRiseAiSimpleLOG");
    }

    public override void LoadContent()
    {
    }

    public override void Load()
    {
      MyTFGame.Load();
      typeof(LoaderAIImport).ModInterop();
      EightPlayerMod = IsModExists("WiderSetMod");
      PlayTagMod = IsModExists("PlayTag");
    }

    public override void Unload()
    {
      MyTFGame.Unload();
    }
  }
}
