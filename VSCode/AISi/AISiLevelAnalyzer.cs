using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseAiSimple
{
    public class AISiLevelAnalyzer
    {
        private Level currentLevel;
        private Dictionary<string, float> levelCharacteristics;
        private DateTime analysisStartTime;
        private bool analysisComplete;

        public AISiLevelAnalyzer()
        {
            levelCharacteristics = new Dictionary<string, float>();
            analysisComplete = false;
        }

        public void AnalyzeLevel(Level level)
        {
            if (level == null) return;

            currentLevel = level;
            analysisStartTime = DateTime.Now;
            analysisComplete = false;

            Logger.Info("=== Starting Level Analysis ===");

            // Analyse de base
            AnalyzeLevelGeometry();
            AnalyzePlatformDistribution();
            AnalyzeWallDensity();
            AnalyzeSpawnPoints();
            AnalyzeMovementSpace();

            // Analyse avancée
            AnalyzeChokePoints();
            AnalyzeCoverSpots();
            AnalyzeShootingAngles();

            analysisComplete = true;
            LogAnalysisResults();
        }

        private void AnalyzeLevelGeometry()
        {
            try
            {
                // Calculer les dimensions approximatives du niveau
                var solidTiles = new List<Vector2>();
                var emptyTiles = new List<Vector2>();

                // Scanner une grille représentative
                for (int x = 0; x < 320; x += 16) // Largeur maximale estimée
                {
                    for (int y = 0; y < 240; y += 16) // Hauteur maximale estimée
                    {
                        Vector2 pos = new Vector2(x, y);
                        if (currentLevel.CollideCheck(pos, GameTags.Solid))
                        {
                            solidTiles.Add(pos);
                        }
                        else
                        {
                            emptyTiles.Add(pos);
                        }
                    }
                }

                float totalTiles = solidTiles.Count + emptyTiles.Count;
                float wallDensity = totalTiles > 0 ? solidTiles.Count / totalTiles : 0f;

                levelCharacteristics["wall_density"] = wallDensity;
                levelCharacteristics["playable_area"] = emptyTiles.Count / totalTiles;

                Logger.Info($"Wall density: {wallDensity:P1}");
                Logger.Info($"Playable area: {levelCharacteristics["playable_area"]:P1}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error analyzing level geometry: {ex.Message}");
                levelCharacteristics["wall_density"] = 0.3f; // Valeur par défaut
                levelCharacteristics["playable_area"] = 0.7f;
            }
        }

        private void AnalyzePlatformDistribution()
        {
            try
            {
                var platforms = new List<float>();

                // Analyser la distribution horizontale des plateformes
                for (int y = 40; y < 200; y += 20)
                {
                    int platformCount = 0;
                    for (int x = 20; x < 300; x += 16)
                    {
                        Vector2 pos = new Vector2(x, y);
                        if (currentLevel.CollideCheck(pos, GameTags.Solid))
                        {
                            platformCount++;
                        }
                    }
                    platforms.Add(platformCount);
                }

                float avgPlatforms = platforms.Count > 0 ? platforms.Average() : 0f;
                float platformVariance = platforms.Count > 0 ? CalculateVariance(platforms) : 0f;

                levelCharacteristics["avg_platforms_per_row"] = avgPlatforms;
                levelCharacteristics["platform_variance"] = platformVariance;
                levelCharacteristics["platform_complexity"] = Math.Min(1f, platformVariance / 10f);

                Logger.Info($"Average platforms per row: {avgPlatforms:F1}");
                Logger.Info($"Platform variance: {platformVariance:F1}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error analyzing platform distribution: {ex.Message}");
                levelCharacteristics["avg_platforms_per_row"] = 5f;
                levelCharacteristics["platform_variance"] = 2f;
                levelCharacteristics["platform_complexity"] = 0.5f;
            }
        }

        private void AnalyzeWallDensity()
        {
            try
            {
                // Analyser la densité des murs par zones
                var zoneDensities = new List<float>();

                for (int zoneX = 0; zoneX < 4; zoneX++)
                {
                    for (int zoneY = 0; zoneY < 3; zoneY++)
                    {
                        int zoneWallCount = 0;
                        int zoneTotalCount = 0;

                        for (int x = zoneX * 80; x < (zoneX + 1) * 80; x += 16)
                        {
                            for (int y = zoneY * 80; y < (zoneY + 1) * 80; y += 16)
                            {
                                Vector2 pos = new Vector2(x, y);
                                zoneTotalCount++;
                                if (currentLevel.CollideCheck(pos, GameTags.Solid))
                                {
                                    zoneWallCount++;
                                }
                            }
                        }

                        float zoneDensity = zoneTotalCount > 0 ? (float)zoneWallCount / zoneTotalCount : 0f;
                        zoneDensities.Add(zoneDensity);
                    }
                }

                float avgDensity = zoneDensities.Average();
                float densityVariance = CalculateVariance(zoneDensities);

                levelCharacteristics["wall_density_variance"] = densityVariance;
                levelCharacteristics["level_symmetry"] = CalculateSymmetry(zoneDensities);

                Logger.Info($"Wall density variance: {densityVariance:F2}");
                Logger.Info($"Level symmetry: {levelCharacteristics["level_symmetry"]:P1}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error analyzing wall density: {ex.Message}");
                levelCharacteristics["wall_density_variance"] = 0.1f;
                levelCharacteristics["level_symmetry"] = 0.8f;
            }
        }

        private void AnalyzeSpawnPoints()
        {
            try
            {
                // Analyser les positions de spawn (approximatif)
                var spawnAreas = new List<Vector2>();

                // Zones de spawn typiques (coins du niveau)
                spawnAreas.Add(new Vector2(20, 180));  // Spawn 1
                spawnAreas.Add(new Vector2(280, 180)); // Spawn 2
                spawnAreas.Add(new Vector2(20, 60));   // Spawn 3
                spawnAreas.Add(new Vector2(280, 60));  // Spawn 4

                float spawnSafety = 0f;
                float spawnOpenness = 0f;

                foreach (var spawn in spawnAreas)
                {
                    // Vérifier la sécurité du spawn (murs proches)
                    bool hasNearbyWalls = false;
                    bool hasNearbyPlatforms = false;

                    for (int dx = -32; dx <= 32; dx += 16)
                    {
                        for (int dy = -32; dy <= 32; dy += 16)
                        {
                            Vector2 checkPos = spawn + new Vector2(dx, dy);
                            if (currentLevel.CollideCheck(checkPos, GameTags.Solid))
                            {
                                if (Math.Abs(dx) + Math.Abs(dy) < 32)
                                {
                                    hasNearbyWalls = true;
                                }
                                hasNearbyPlatforms = true;
                            }
                        }
                    }

                    spawnSafety += hasNearbyWalls ? 1f : 0f;
                    spawnOpenness += hasNearbyPlatforms ? 1f : 0f;
                }

                levelCharacteristics["spawn_safety"] = spawnSafety / spawnAreas.Count;
                levelCharacteristics["spawn_openness"] = spawnOpenness / spawnAreas.Count;

                Logger.Info($"Spawn safety: {levelCharacteristics["spawn_safety"]:P1}");
                Logger.Info($"Spawn openness: {levelCharacteristics["spawn_openness"]:P1}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error analyzing spawn points: {ex.Message}");
                levelCharacteristics["spawn_safety"] = 0.7f;
                levelCharacteristics["spawn_openness"] = 0.8f;
            }
        }

        private void AnalyzeMovementSpace()
        {
            try
            {
                // Analyser l'espace de mouvement horizontal
                var movementSpaces = new List<float>();

                for (int y = 60; y < 200; y += 40)
                {
                    float continuousSpace = 0f;
                    float maxContinuousSpace = 0f;

                    for (int x = 20; x < 300; x += 16)
                    {
                        Vector2 pos = new Vector2(x, y);
                        if (!currentLevel.CollideCheck(pos, GameTags.Solid))
                        {
                            continuousSpace += 16f;
                            maxContinuousSpace = Math.Max(maxContinuousSpace, continuousSpace);
                        }
                        else
                        {
                            continuousSpace = 0f;
                        }
                    }

                    movementSpaces.Add(maxContinuousSpace);
                }

                levelCharacteristics["avg_movement_space"] = movementSpaces.Average();
                levelCharacteristics["max_movement_space"] = movementSpaces.Max();

                Logger.Info($"Average movement space: {levelCharacteristics["avg_movement_space"]:F1}");
                Logger.Info($"Max movement space: {levelCharacteristics["max_movement_space"]:F1}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error analyzing movement space: {ex.Message}");
                levelCharacteristics["avg_movement_space"] = 100f;
                levelCharacteristics["max_movement_space"] = 200f;
            }
        }

        private void AnalyzeChokePoints()
        {
            try
            {
                // Identifier les points de passage étroits
                var chokePoints = new List<float>();

                for (int y = 80; y < 180; y += 20)
                {
                    for (int x = 40; x < 260; x += 20)
                    {
                        // Vérifier si c'est un point de passage étroit
                        int blockedDirections = 0;
                        int totalDirections = 0;

                        Vector2[] directions = new Vector2[]
                        {
                            new Vector2(-1, 0), new Vector2(1, 0), // Horizontal
                            new Vector2(0, -1), new Vector2(0, 1),   // Vertical
                            new Vector2(-1, -1), new Vector2(1, -1), // Diagonal
                            new Vector2(-1, 1), new Vector2(1, 1)
                        };

                        foreach (var dir in directions)
                        {
                            totalDirections++;
                            Vector2 checkPos = new Vector2(x, y) + dir * 20f;
                            if (currentLevel.CollideCheck(checkPos, GameTags.Solid))
                            {
                                blockedDirections++;
                            }
                        }

                        float chokeRatio = (float)blockedDirections / totalDirections;
                        if (chokeRatio > 0.5f) // Plus de 50% bloqué
                        {
                            chokePoints.Add(chokeRatio);
                        }
                    }
                }

                levelCharacteristics["choke_point_density"] = chokePoints.Count > 0 ? chokePoints.Average() : 0f;
                levelCharacteristics["choke_point_count"] = chokePoints.Count;

                Logger.Info($"Choke point density: {levelCharacteristics["choke_point_density"]:P1}");
                Logger.Info($"Choke point count: {levelCharacteristics["choke_point_count"]}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error analyzing choke points: {ex.Message}");
                levelCharacteristics["choke_point_density"] = 0.2f;
                levelCharacteristics["choke_point_count"] = 3f;
            }
        }

        private void AnalyzeCoverSpots()
        {
            try
            {
                // Identifier les zones de couverture
                var coverSpots = new List<float>();

                for (int y = 60; y < 200; y += 30)
                {
                    for (int x = 30; x < 270; x += 30)
                    {
                        Vector2 pos = new Vector2(x, y);

                        // Vérifier s'il y a de la couverture
                        bool hasHorizontalCover = false;
                        bool hasVerticalCover = false;

                        // Couverture horizontale
                        if (currentLevel.CollideCheck(pos + new Vector2(-30, 0), GameTags.Solid) ||
                            currentLevel.CollideCheck(pos + new Vector2(30, 0), GameTags.Solid))
                        {
                            hasHorizontalCover = true;
                        }

                        // Couverture verticale
                        if (currentLevel.CollideCheck(pos + new Vector2(0, -30), GameTags.Solid) ||
                            currentLevel.CollideCheck(pos + new Vector2(0, 30), GameTags.Solid))
                        {
                            hasVerticalCover = true;
                        }

                        float coverValue = (hasHorizontalCover ? 0.5f : 0f) + (hasVerticalCover ? 0.5f : 0f);
                        if (coverValue > 0f)
                        {
                            coverSpots.Add(coverValue);
                        }
                    }
                }

                levelCharacteristics["cover_density"] = coverSpots.Count > 0 ? coverSpots.Average() : 0f;
                levelCharacteristics["cover_spots"] = coverSpots.Count;

                Logger.Info($"Cover density: {levelCharacteristics["cover_density"]:P1}");
                Logger.Info($"Cover spots: {levelCharacteristics["cover_spots"]}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error analyzing cover spots: {ex.Message}");
                levelCharacteristics["cover_density"] = 0.4f;
                levelCharacteristics["cover_spots"] = 8f;
            }
        }

        private void AnalyzeShootingAngles()
        {
            try
            {
                // Analyser les angles de tir possibles
                var shootingAngles = new List<float>();

                // Points de test représentatifs
                Vector2[] testPoints = new Vector2[]
                {
                    new Vector2(50, 100), new Vector2(150, 100), new Vector2(250, 100),
                    new Vector2(50, 150), new Vector2(150, 150), new Vector2(250, 150)
                };

                foreach (var point in testPoints)
                {
                    int clearAngles = 0;
                    int totalAngles = 0;

                    // Tester différents angles
                    for (float angle = 0; angle < Math.PI * 2; angle += (float)(Math.PI / 8))
                    {
                        totalAngles++;

                        Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                        bool hasClearShot = true;

                        // Vérifier la ligne de tir
                        for (float distance = 20; distance < 100; distance += 10)
                        {
                            Vector2 checkPos = point + direction * distance;
                            if (currentLevel.CollideCheck(checkPos, GameTags.Solid))
                            {
                                hasClearShot = false;
                                break;
                            }
                        }

                        if (hasClearShot)
                        {
                            clearAngles++;
                        }
                    }

                    float angleRatio = (float)clearAngles / totalAngles;
                    shootingAngles.Add(angleRatio);
                }

                levelCharacteristics["shooting_angle_coverage"] = shootingAngles.Average();
                levelCharacteristics["shooting_complexity"] = 1f - shootingAngles.Average();

                Logger.Info($"Shooting angle coverage: {levelCharacteristics["shooting_angle_coverage"]:P1}");
                Logger.Info($"Shooting complexity: {levelCharacteristics["shooting_complexity"]:P1}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error analyzing shooting angles: {ex.Message}");
                levelCharacteristics["shooting_angle_coverage"] = 0.6f;
                levelCharacteristics["shooting_complexity"] = 0.4f;
            }
        }

        private float CalculateVariance(List<float> values)
        {
            if (values.Count == 0) return 0f;

            float average = values.Average();
            float sumSquaredDifferences = values.Sum(val => (val - average) * (val - average));
            return sumSquaredDifferences / values.Count;
        }

        private float CalculateSymmetry(List<float> zoneDensities)
        {
            if (zoneDensities.Count < 12) return 0.8f; // Valeur par défaut

            // Comparer zones opposées (simplifié pour 4x3 grille)
            float symmetry = 0f;

            // Symétrie horizontale
            for (int i = 0; i < 6; i++)
            {
                float diff = Math.Abs(zoneDensities[i] - zoneDensities[11 - i]);
                symmetry += 1f - diff;
            }

            return symmetry / 6f;
        }

        public Dictionary<string, float> GetLevelCharacteristics()
        {
            return new Dictionary<string, float>(levelCharacteristics);
        }

        public string GetLevelType()
        {
            if (!analysisComplete) return "unknown";

            // Classification simple basée sur les caractéristiques
            float wallDensity = levelCharacteristics.ContainsKey("wall_density") ? levelCharacteristics["wall_density"] : 0.3f;
            float platformComplexity = levelCharacteristics.ContainsKey("platform_complexity") ? levelCharacteristics["platform_complexity"] : 0.5f;
            float chokePointDensity = levelCharacteristics.ContainsKey("choke_point_density") ? levelCharacteristics["choke_point_density"] : 0.2f;
            float shootingComplexity = levelCharacteristics.ContainsKey("shooting_complexity") ? levelCharacteristics["shooting_complexity"] : 0.4f;

            // Scoring pour différents types
            float openScore = (1f - wallDensity) * 0.4f + (1f - platformComplexity) * 0.3f + shootingComplexity * 0.3f;
            float complexScore = wallDensity * 0.3f + platformComplexity * 0.4f + chokePointDensity * 0.3f;
            float tacticalScore = (1f - wallDensity) * 0.2f + chokePointDensity * 0.4f + shootingComplexity * 0.4f;

            if (openScore > complexScore && openScore > tacticalScore)
                return "open";
            else if (complexScore > tacticalScore)
                return "complex";
            else
                return "tactical";
        }

        private void LogAnalysisResults()
        {
            Logger.Info("=== Level Analysis Complete ===");
            Logger.Info($"Analysis time: {(DateTime.Now - analysisStartTime).TotalMilliseconds:F0}ms");
            Logger.Info($"Level type: {GetLevelType()}");

            foreach (var kvp in levelCharacteristics.OrderBy(x => x.Key))
            {
                Logger.Info($"{kvp.Key}: {kvp.Value:F3}");
            }
        }
    }
}