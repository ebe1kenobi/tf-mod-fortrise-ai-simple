using TFModFortRiseLoaderAI;
using TowerFall;

namespace TFModFortRiseAiSimple
{
  internal class AISi
  {
    public const string AINAME = "AISi";
    public static bool isAgentReady = false;
    private static AISiAgent[] agents;// = new NAIAgent[8];
    public static PlayerInput[] AgentInputs;// = new PlayerInput[8];

    public static void CreateAgent()
    {
      if (isAgentReady) return;
      //detect first player slot free
      int max = TFModFortRiseAiSimpleModule.EightPlayerMod ? 8 : 4;
      agents = new AISiAgent[max];
      AgentInputs = new PlayerInput[max];

      for (int i = 0; i < max; i++)
      {
        // create an agent for each player
        AgentInputs[i] = new TFModFortRiseLoaderAI.Input(i);
        agents[i] = new AISiAgent(i, AINAME, AgentInputs[i]);
        Logger.Info("Agent " + AINAME  + " " + i + " Created");
        //if (null != TFGame.PlayerInputs[i]) continue;
      }

      isAgentReady = true;
      LoaderAIImport.addAgent(AINAME, agents);
    }
  }
}
