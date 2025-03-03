using TFModFortRiseLoaderAI;
using TowerFall;

namespace TFModFortRiseAiSimple
{
  internal class AISi
  {
    public const string AINAME = "AISi";
    public static bool isAgentReady = false;
    private static AISiAgentLevel0[] agents;
    //private static AISiAgentLevel1[] agents;
    public static PlayerInput[] AgentInputs;

    public static void CreateAgent()
    {
      if (isAgentReady) return;
      //detect first player slot free
      int max = TFModFortRiseAiSimpleModule.EightPlayerMod ? 8 : 4;
      agents = new AISiAgentLevel0[max];
      //agents = new AISiAgentLevel1[max];
      AgentInputs = new PlayerInput[max];

      for (int i = 0; i < max; i++)
      {
        // create an agent for each player
        AgentInputs[i] = new TFModFortRiseLoaderAI.Input(i);
        agents[i] = new AISiAgentLevel0(i, AINAME, AgentInputs[i]);
        //agents[i] = new AISiAgentLevel1(i, AINAME, AgentInputs[i]);
        Logger.Info("Agent " + AINAME  + " " + i + " Created");
      }

      isAgentReady = true;
      LoaderAIImport.addAgent(AINAME, agents);
    }
  }
}
