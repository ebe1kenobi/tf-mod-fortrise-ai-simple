using System;
using System.Text.RegularExpressions;
using MonoMod.Utils;
using TFModFortRiseLoaderAI;
using TowerFall;

namespace TFModFortRiseAiSimple
{
  internal class AISi
  {
    public const string AINAME = "AISi";
    public static bool isAgentReady = false;
    //private static AISiAgentLevel0[] agents;
    //private static AISiAgentLevel1[] agents;
    private static AISiAgentLevelChase[] agents;
    //private static AISiAgentLevelTestMovement[] agents;
    public static PlayerInput[] AgentInputs;
    private static MatchSettings matchSettings;


    public static void CreateAgent()
    {
      if (isAgentReady) return;
      //detect first player slot free
      int max = TFModFortRiseAiSimpleModule.EightPlayerMod ? 8 : 4;
      
      //agents = new AISiAgentLevel0[max];
      //agents = new AISiAgentLevel1[max];
      agents = new AISiAgentLevelChase[max];
      //agents = new AISiAgentLevelTestMovement[max];
      AgentInputs = new PlayerInput[max];

      for (int i = 0; i < max; i++)
      {
        // create an agent for each player
        AgentInputs[i] = new TFModFortRiseLoaderAI.Input(i);
        //agents[i] = new AISiAgentLevel0(i, AINAME, AgentInputs[i]);
        //agents[i] = new AISiAgentLevel1(i, AINAME, AgentInputs[i]);
        agents[i] = new AISiAgentLevelChase(i, AINAME, AgentInputs[i]);
        //agents[i] = new AISiAgentLevelTestMovement(i, AINAME, AgentInputs[i]);
        Logger.Info("Agent " + AINAME  + " " + i + " Created");
      }


      TFGame.PlayerInputs[0] = null;
      TFGame.PlayerInputs[1] = null;
      TFGame.PlayerInputs[2] = null;
      TFGame.PlayerInputs[3] = null;
      isAgentReady = true;
      Logger.Info("addAgent1");
      LoaderAIImport.addAgent(AINAME, agents);
      Logger.Info("addAgent2");


      //SANDBOX  (had to set PlayerInputs before addAgent()!!)
      TFGame.Players[0] = true;
      TFGame.PlayerInputs[0] = agents[0].getInput();
      TFGame.Characters[0] = 0;
      TFGame.AltSelect[0] = ArcherData.ArcherTypes.Normal;

      TFGame.Players[1] = true;
      TFGame.PlayerInputs[1] = agents[1].getInput();
      TFGame.Characters[1] = 1;
      TFGame.AltSelect[1] = ArcherData.ArcherTypes.Normal;

      ////base.MainMenu.State = MainMenu.MenuState.Main;
      StartNewSession();
      //SANDBOX

    }

    public static void StartNewSession()
    {
      Logger.Info("Starting a new session.");
      CreateMatchSettings();
      Session session = new Session(matchSettings);
      //session.QuestTestWave = 1;
      session.StartGame();
      Logger.Info("Session started.");
    }


    public static void CreateMatchSettings()
    {
      Logger.Info("CreateMatchSettings.");
      //if (!IsNoConfig)
      //{
      //  Config = JsonConvert.DeserializeObject<MatchConfig>(File.ReadAllText(ConfigPath));
      //}
      MatchSettings.MatchLengths matchLength;
      //matchLength = MatchSettings.MatchLengths.Instant;
      //matchLength = MatchSettings.MatchLengths.Quick;
      //matchLength = MatchSettings.MatchLengths.Epic;
      matchLength = MatchSettings.MatchLengths.Standard;
      //System.Random rnd = new Random();
      //LevelSystem levelSystem = GameData.VersusTowers[rnd.Next(1, 17)].GetLevelSystem(); //16 levels;
      ////Logger.Info("Configuring HeadHunters mode.");
      //matchSettings = new MatchSettings(levelSystem, Modes.HeadHunters, matchLength);
      //matchSettings.Variants.TournamentRules();

      Logger.Info("Configuring Sandbox mode.");
      matchSettings = MatchSettings.GetDefaultTrials();
      matchSettings.Mode = Modes.LevelTest;

      String s = @"
11111111111111111111111111111111
10000000000000000000000000000001
10000000000000000000000000000001
10000000000000000000000000000001
10000000000000000000000000000001
10000000000000000000000000000001
10000000000000000000000000000001
10000000000000000000000000000001
10000000000000000000000000000001
10000000000000000000000000000001
10000000000000000000000000000001
10000000000000000000000000000001
10000000000000000000000000000001
10000000000000000000000000000001
10000000000000000000000000000001
10000000000000000000000000000001
10000000000000000000000000000001
10000000000000000000000000000001
10000000000000000000000000000011
10000000000000000000000000000001
10000000000000000000000000000001
10000000000000000000000000000001
10000000000000000000000000000001
11111111111111111111111111111111";

      matchSettings.LevelSystem = new SandboxLevelSystem(GameData.QuestLevels[0], convertToStringToArray(s));
      Logger.Info("set NoTreasure");
      matchSettings.Variants.NoTreasure.Value = true;

      for (int i = 0; i < agents.Length; i++)
      {
        matchSettings.Teams[i] = Allegiance.Neutral;
      }

      //hide the intro control for level 0 or trigger controle for tower N
      var dynData = DynamicData.For(matchSettings.LevelSystem);
      dynData.Set("ShowControls", false);
      dynData.Set("ShowTriggerControls", false);
      dynData.Dispose();
    }

    public static int [,] convertToStringToArray(string s)
    {
      string[] lines = Regex.Split(s.Trim(), "\n");
      int rows = lines.Length;
      int cols = lines[0].Trim().Length;
      int[,] array = new int[rows, cols];
      for (int i = 0; i < rows; i++)
      {
        string line = lines[i].Trim();
        for (int j = 0; j < cols; j++)
        {
          array[i, j] = (int)char.GetNumericValue(line[j]);
        }
      }
      return array;
    }
  }
}
