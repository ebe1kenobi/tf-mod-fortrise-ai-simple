using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;
using TFModFortRiseLoaderAI;
using TowerFall;

namespace TFModFortRiseAiSimple
{
    public class AISiHybridTest
    {
        private AISiAgentHybrid agent;
        private List<TestResult> testResults;
        private int testRound;
        private DateTime testStartTime;

        public class TestResult
        {
            public int Round { get; set; }
            public string AIState { get; set; }
            public float SurvivalTime { get; set; }
            public int ArrowsShot { get; set; }
            public int ArrowsHit { get; set; }
            public int Deaths { get; set; }
            public Dictionary<string, float> SuccessRates { get; set; }
            public float AverageReactionTime { get; set; }
        }

        public AISiHybridTest(AISiAgentHybrid agent)
        {
            this.agent = agent;
            this.testResults = new List<TestResult>();
            this.testRound = 0;
        }

        public void StartTest()
        {
            testStartTime = DateTime.Now;
            testRound++;
            Logger.Info($"=== Starting Hybrid AI Test Round {testRound} ===");
        }

        public void EndTest()
        {
            var survivalTime = (float)(DateTime.Now - testStartTime).TotalSeconds;
            var result = new TestResult
            {
                Round = testRound,
                AIState = agent.GetCurrentState(),
                SurvivalTime = survivalTime,
                SuccessRates = agent.GetAllSuccessRates(),
                ArrowsShot = GetArrowsShot(),
                ArrowsHit = GetArrowsHit(),
                Deaths = GetDeathCount(),
                AverageReactionTime = CalculateAverageReactionTime()
            };

            testResults.Add(result);
            LogTestResult(result);
        }

        private int GetArrowsShot()
        {
            // This would need to be tracked by the agent or game state
            return 0; // Placeholder
        }

        private int GetArrowsHit()
        {
            // This would need to be tracked by the agent or game state
            return 0; // Placeholder
        }

        private int GetDeathCount()
        {
            // This would need to be tracked by the agent or game state
            return 0; // Placeholder
        }

        private float CalculateAverageReactionTime()
        {
            // This would calculate average reaction time based on dodge success rates
            var dodgeRate = agent.GetSuccessRate("dodge_right");
            return dodgeRate > 0.7f ? 0.1f : 0.3f; // Simplified calculation
        }

        private void LogTestResult(TestResult result)
        {
            Logger.Info($"=== Test Round {result.Round} Results ===");
            Logger.Info($"Survival Time: {result.SurvivalTime:F1}s");
            Logger.Info($"Final State: {result.AIState}");
            Logger.Info($"Action Success Rates:");

            foreach (var kvp in result.SuccessRates)
            {
                Logger.Info($"  {kvp.Key}: {kvp.Value:P1}");
            }

            Logger.Info($"Estimated Reaction Time: {result.AverageReactionTime:F2}s");
            Logger.Info($"Overall Performance: {CalculateOverallPerformance(result):P1}");
        }

        private float CalculateOverallPerformance(TestResult result)
        {
            // Simple performance metric combining multiple factors
            float survivalScore = Math.Min(result.SurvivalTime / 30f, 1.0f); // Normalize to 30s max
            float dodgeScore = ((result.SuccessRates.ContainsKey("dodge_right") ? result.SuccessRates["dodge_right"] : 0f) +
                              (result.SuccessRates.ContainsKey("dodge_left") ? result.SuccessRates["dodge_left"] : 0f)) / 2f;
            float shootScore = result.SuccessRates.ContainsKey("shoot") ? result.SuccessRates["shoot"] : 0f;

            return (survivalScore * 0.4f + dodgeScore * 0.4f + shootScore * 0.2f);
        }

        public TestResult GetBestPerformance()
        {
            if (testResults.Count == 0) return null;

            return testResults.OrderByDescending(r => CalculateOverallPerformance(r)).First();
        }

        public TestResult GetAveragePerformance()
        {
            if (testResults.Count == 0) return null;

            var avgResult = new TestResult
            {
                Round = -1, // Indicates average
                SurvivalTime = testResults.Average(r => r.SurvivalTime),
                AverageReactionTime = testResults.Average(r => r.AverageReactionTime),
                SuccessRates = new Dictionary<string, float>()
            };

            // Calculate average success rates
            var actions = new[] { "shoot", "dodge_left", "dodge_right", "approach", "wall_climb" };
            foreach (var action in actions)
            {
                avgResult.SuccessRates[action] = testResults.Average(r =>
                    r.SuccessRates.ContainsKey(action) ? r.SuccessRates[action] : 0f);
            }

            return avgResult;
        }

        public void GenerateReport()
        {
            Logger.Info("=== HYBRID AI PERFORMANCE REPORT ===");
            Logger.Info($"Total Test Rounds: {testResults.Count}");

            var best = GetBestPerformance();
            var avg = GetAveragePerformance();

            if (best != null)
            {
                Logger.Info($"Best Performance: {CalculateOverallPerformance(best):P1}");
                Logger.Info($"  Best Survival Time: {best.SurvivalTime:F1}s");
                Logger.Info($"  Best Dodge Rate: {((best.SuccessRates.ContainsKey("dodge_right") ? best.SuccessRates["dodge_right"] : 0f) + (best.SuccessRates.ContainsKey("dodge_left") ? best.SuccessRates["dodge_left"] : 0f)) / 2f:P1}");
            }

            if (avg != null)
            {
                Logger.Info($"Average Performance: {CalculateOverallPerformance(avg):P1}");
                Logger.Info($"  Average Survival Time: {avg.SurvivalTime:F1}s");
                Logger.Info($"  Average Shoot Success: {(avg.SuccessRates.ContainsKey("shoot") ? avg.SuccessRates["shoot"] : 0f):P1}");
            }

            Logger.Info("=== RECOMMENDATIONS ===");
            GenerateRecommendations();
        }

        private void GenerateRecommendations()
        {
            if (testResults.Count == 0) return;

            var avg = GetAveragePerformance();
            if (avg == null) return;

            if ((avg.SuccessRates.ContainsKey("shoot") ? avg.SuccessRates["shoot"] : 0f) < 0.3f)
            {
                Logger.Info("- Improve shooting accuracy by better prediction algorithms");
            }

            if (((avg.SuccessRates.ContainsKey("dodge_left") ? avg.SuccessRates["dodge_left"] : 0f) + (avg.SuccessRates.ContainsKey("dodge_right") ? avg.SuccessRates["dodge_right"] : 0f)) / 2f < 0.5f)
            {
                Logger.Info("- Enhance dodge timing and direction selection");
            }

            if (avg.SurvivalTime < 15f)
            {
                Logger.Info("- Focus on defensive positioning and threat awareness");
            }

            Logger.Info("- Consider adjusting state transition thresholds for better performance");
        }
    }
}