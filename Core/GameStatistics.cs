using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Tetris.Core
{
    public sealed class ModeStatistics
    {
        public int BestScore { get; set; }
        public int MaxComboStreak { get; set; }
        public int TotalLinesCleared { get; set; }
        public int GamesPlayed { get; set; }
    }

    public sealed class GameStatisticsRepository
    {
        private const string AppFolderName = "Tetris";
        private const string StatsFileName = "stats.json";
        private const int SingleGameIncrement = 1;

        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private readonly string storageFilePath;
        private readonly Dictionary<GameMode, ModeStatistics> statsByMode;

        public GameStatisticsRepository()
        {
            string appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string directoryPath = Path.Combine(appDataRoot, AppFolderName);
            storageFilePath = Path.Combine(directoryPath, StatsFileName);
            statsByMode = LoadFromDisk();
        }

        public ModeStatistics GetStats(GameMode mode)
        {
            if (!statsByMode.TryGetValue(mode, out ModeStatistics? stats))
            {
                stats = new ModeStatistics();
                statsByMode[mode] = stats;
            }

            return stats;
        }

        public bool RecordFinishedSession(GameMode mode, int score, int maxComboStreak, int linesCleared)
        {
            ModeStatistics stats = GetStats(mode);
            bool isNewHighScore = score > stats.BestScore;

            if (isNewHighScore)
            {
                stats.BestScore = score;
            }

            if (maxComboStreak > stats.MaxComboStreak)
            {
                stats.MaxComboStreak = maxComboStreak;
            }

            stats.TotalLinesCleared += linesCleared;
            stats.GamesPlayed += SingleGameIncrement;

            SaveToDisk();
            return isNewHighScore;
        }

        private Dictionary<GameMode, ModeStatistics> LoadFromDisk()
        {
            try
            {
                if (!File.Exists(storageFilePath))
                {
                    return new Dictionary<GameMode, ModeStatistics>();
                }

                string json = File.ReadAllText(storageFilePath);
                return JsonSerializer.Deserialize<Dictionary<GameMode, ModeStatistics>>(json, SerializerOptions)
                       ?? new Dictionary<GameMode, ModeStatistics>();
            }
            catch (Exception)
            {
                return new Dictionary<GameMode, ModeStatistics>();
            }
        }

        private void SaveToDisk()
        {
            try
            {
                string? directory = Path.GetDirectoryName(storageFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(statsByMode, SerializerOptions);
                File.WriteAllText(storageFilePath, json);
            }
            catch (Exception)
            {
                // Writing to disk failed due to permission issues or other reasons; ignore and continue without crashing the game.
            }
        }
    }
}