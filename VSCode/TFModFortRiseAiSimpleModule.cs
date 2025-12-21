using System;
using System.Diagnostics;
using FortRise;
using Microsoft.Extensions.Logging;
using MonoMod.ModInterop;
using TFModFortRiseLoaderAI;

namespace TFModFortRiseAiSimple
{
  public class TFModFortRiseAiSimpleModule : Mod
  {
    public static TFModFortRiseAiSimpleModule Instance;

    internal Type[] Hookables = [
        typeof(MyTFGame),
    ];
    public static bool EightPlayerMod = false; //todo
    public static bool PlayTagMod = false; //todo

    public ILoaderAIModApi LoaderAIModApi { get; private set; }

    //public override Type SettingsType => typeof(TFModFortRiseAiSimpleSettings);
    //public static TFModFortRiseAiSimpleSettings Settings => (TFModFortRiseAiSimpleSettings)Instance.InternalSettings;

    public TFModFortRiseAiSimpleModule(IModContent content, IModuleContext context, ILogger logger) : base(content, context, logger)
    {
      if (!Debugger.IsAttached)
      {
        //Debugger.Launch(); // Proposera d’attacher Visual Studio
      }
      Instance = this;
      TFModFortRiseAiSimple.Logger.Init("TFModFortRiseAiSimpleLOG");
      foreach (var hookable in Hookables)
      {
        hookable.GetMethod(nameof(IHookable.Load))!.Invoke(null, [context.Harmony]);
      }
      //typeof(LoaderAIImport).ModInterop();
      LoaderAIModApi = context.Interop.GetApi<ILoaderAIModApi>("LoaderAI");
    }

    //public override void Load()
    //{
    //  MyTFGame.Load();
    //  typeof(LoaderAIImport).ModInterop();
    //  //EightPlayerMod = IsModExists("WiderSetMod");
    //  //PlayTagMod = IsModExists("PlayTag");
    //}
  }
}
