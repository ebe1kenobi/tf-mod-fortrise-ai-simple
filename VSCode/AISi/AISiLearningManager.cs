using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace TFModFortRiseAiSimple
{
    public class AISiLearningManager
    {
        private const string LEARNING_DATA_DIR = "AI_Learning_Data";
        private const string DEFAULT_LEVEL_TYPE = "default";

        private Dictionary<string, LevelLearningData> allLearningData;
        private string currentLevelType;
        private DateTime lastAutoSave;

        public class LevelLearningData
        {
            public string LevelType { get; set; }
            public DateTime LastUpdated { get; set; }
            public Dictionary<string, float> ActionSuccessRates { get; set; }
            public Dictionary<string, int> ActionAttempts { get; set; }
            public int TotalMatches { get; set; }
            public float AverageSurvivalTime { get; set; }
            public Dictionary<string, float> LevelCharacteristics { get; set; }

            public LevelLearningData()
            {
                ActionSuccessRates = new Dictionary<string, float>();
                ActionAttempts = new Dictionary<string, int>();
                LevelCharacteristics = new Dictionary<string, float>();
                LastUpdated = DateTime.Now;
            }
        }

        public class LearningDataContainer
        {
            public Dictionary<string, LevelLearningData> LevelData { get; set; }
            public DateTime Created { get; set; }
            public string Version { get; set; }

            public LearningDataContainer()
            {
                LevelData = new Dictionary<string, LevelLearningData>();
                Created = DateTime.Now;
                Version = "1.0";
            }
        }

        public AISiLearningManager()
        {
            allLearningData = new Dictionary<string, LevelLearningData>();
            currentLevelType = DEFAULT_LEVEL_TYPE;
            lastAutoSave = DateTime.MinValue;

            EnsureDirectoryExists();
            LoadAllLearningData();
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(LEARNING_DATA_DIR))
            {
                Directory.CreateDirectory(LEARNING_DATA_DIR);
                Logger.Info($"Created learning data directory: {LEARNING_DATA_DIR}");
            }
        }

        public void SetCurrentLevelType(string levelType)
        {
            currentLevelType = levelType ?? DEFAULT_LEVEL_TYPE;
            Logger.Info($"Switched to level type: {currentLevelType}");

            if (!allLearningData.ContainsKey(currentLevelType))
            {
                allLearningData[currentLevelType] = new LevelLearningData
                {
                    LevelType = currentLevelType
                };
                Logger.Info($"Created new learning data for level type: {currentLevelType}");
            }
        }

        public LevelLearningData GetCurrentLevelData()
        {
            if (!allLearningData.ContainsKey(currentLevelType))
            {
                allLearningData[currentLevelType] = new LevelLearningData
                {
                    LevelType = currentLevelType
                };
            }
            return allLearningData[currentLevelType];
        }

        public void UpdateActionData(string action, float successRate, int attempts)
        {
            var data = GetCurrentLevelData();
            data.ActionSuccessRates[action] = successRate;
            data.ActionAttempts[action] = attempts;
            data.LastUpdated = DateTime.Now;
        }

        public void UpdateMatchStatistics(float survivalTime)
        {
            var data = GetCurrentLevelData();
            data.TotalMatches++;

            // Mise à jour de la moyenne mobile
            if (data.AverageSurvivalTime <= 0)
            {
                data.AverageSurvivalTime = survivalTime;
            }
            else
            {
                data.AverageSurvivalTime = (data.AverageSurvivalTime * 0.9f) + (survivalTime * 0.1f);
            }
        }

        public LevelLearningData GetLevelData(string levelType)
        {
            if (string.IsNullOrEmpty(levelType))
                return null;

            if (allLearningData.ContainsKey(levelType))
            {
                return allLearningData[levelType];
            }

            return null;
        }

        public void UpdateLevelData(string levelType, LevelLearningData levelData)
        {
            if (string.IsNullOrEmpty(levelType) || levelData == null)
                return;

            allLearningData[levelType] = levelData;
            Logger.Info($"Updated learning data for level type: {levelType}");
        }

        public void UpdateLevelCharacteristics(Dictionary<string, float> characteristics)
        {
            var data = GetCurrentLevelData();
            foreach (var kvp in characteristics)
            {
                data.LevelCharacteristics[kvp.Key] = kvp.Value;
            }
        }

        public bool ShouldAutoSave()
        {
            if (!TFModFortRiseAiSimpleModule.Settings.SaveLearningData)
                return false;

            var interval = TFModFortRiseAiSimpleModule.Settings.AutoSaveInterval;
            return (DateTime.Now - lastAutoSave).TotalSeconds >= interval;
        }

        public void AutoSaveIfNeeded()
        {
            if (ShouldAutoSave())
            {
                SaveAllLearningData();
                lastAutoSave = DateTime.Now;
            }
        }

        public void SaveAllLearningData()
        {
            try
            {
                var container = new LearningDataContainer();
                container.LevelData = allLearningData;

                string fileName = TFModFortRiseAiSimpleModule.Settings.LearningDataPath;
                string filePath = Path.Combine(LEARNING_DATA_DIR, fileName);

                string json = JsonConvert.SerializeObject(container, Formatting.Indented);
                File.WriteAllText(filePath, json);

                Logger.Info($"Saved learning data to: {filePath}");
                Logger.Info($"Total level types saved: {allLearningData.Count}");

                foreach (var levelData in allLearningData.Values)
                {
                    Logger.Info($"  {levelData.LevelType}: {levelData.TotalMatches} matches, " +
                              $"Avg survival: {levelData.AverageSurvivalTime:F1}s");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save learning data: {ex.Message}");
            }
        }

        public void LoadAllLearningData()
        {
            try
            {
                string fileName = TFModFortRiseAiSimpleModule.Settings.LearningDataPath;
                string filePath = Path.Combine(LEARNING_DATA_DIR, fileName);

                if (!File.Exists(filePath))
                {
                    Logger.Info("No existing learning data found, starting fresh.");
                    return;
                }

                string json = File.ReadAllText(filePath);
                var container = JsonConvert.DeserializeObject<LearningDataContainer>(json);

                if (container?.LevelData != null)
                {
                    allLearningData = container.LevelData;
                    Logger.Info($"Loaded learning data from: {filePath}");
                    Logger.Info($"Loaded {allLearningData.Count} level types");

                    foreach (var levelData in allLearningData.Values)
                    {
                        Logger.Info($"  {levelData.LevelType}: {levelData.TotalMatches} matches, " +
                                  $"Avg survival: {levelData.AverageSurvivalTime:F1}s");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load learning data: {ex.Message}");
                allLearningData = new Dictionary<string, LevelLearningData>();
            }
        }

        public string FindBestMatchingLevelType(Dictionary<string, float> currentCharacteristics)
        {
            if (!allLearningData.Any())
                return DEFAULT_LEVEL_TYPE;

            string bestMatch = DEFAULT_LEVEL_TYPE;
            float bestScore = 0f;

            foreach (var kvp in allLearningData)
            {
                float similarity = CalculateLevelSimilarity(currentCharacteristics, kvp.Value.LevelCharacteristics);
                if (similarity > bestScore)
                {
                    bestScore = similarity;
                    bestMatch = kvp.Key;
                }
            }

            Logger.Info($"Best matching level type: {bestMatch} (similarity: {bestScore:P1})");
            return bestMatch;
        }

        private float CalculateLevelSimilarity(Dictionary<string, float> current, Dictionary<string, float> stored)
        {
            if (!current.Any() || !stored.Any())
                return 0f;

            float totalDifference = 0f;
            int comparisons = 0;

            foreach (var kvp in current)
            {
                if (stored.ContainsKey(kvp.Key))
                {
                    float difference = Math.Abs(kvp.Value - stored[kvp.Key]);
                    totalDifference += difference;
                    comparisons++;
                }
            }

            if (comparisons == 0)
                return 0f;

            float averageDifference = totalDifference / comparisons;
            return Math.Max(0f, 1f - averageDifference); // Similarité inverse de la différence
        }

        public void ExportLearningReport(string filePath)
        {
            try
            {
                var report = new System.Text.StringBuilder();
                report.AppendLine("=== AI LEARNING REPORT ===");
                report.AppendLine($"Generated: {DateTime.Now}");
                report.AppendLine($"Total Level Types: {allLearningData.Count}");
                report.AppendLine();

                foreach (var levelData in allLearningData.Values)
                {
                    report.AppendLine($"Level Type: {levelData.LevelType}");
                    report.AppendLine($"  Total Matches: {levelData.TotalMatches}");
                    report.AppendLine($"  Average Survival: {levelData.AverageSurvivalTime:F1}s");
                    report.AppendLine($"  Last Updated: {levelData.LastUpdated}");
                    report.AppendLine($"  Action Success Rates:");

                    foreach (var action in levelData.ActionSuccessRates)
                    {
                        report.AppendLine($"    {action.Key}: {action.Value:P1} " +
                                          $"({(levelData.ActionAttempts.ContainsKey(action.Key) ? levelData.ActionAttempts[action.Key] : 0)} attempts)");
                    }
                    report.AppendLine();
                }

                File.WriteAllText(filePath, report.ToString());
                Logger.Info($"Exported learning report to: {filePath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to export learning report: {ex.Message}");
            }
        }
    }
}