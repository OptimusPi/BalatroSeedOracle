using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Persists Daylatro high scores locally in a JSON file.
    /// Simple append + in-memory cache; not concurrency safe across multiple processes.
    /// </summary>
    public class DaylatroHighScoreService
    {
        private static readonly Lazy<DaylatroHighScoreService> _lazy = new(() => new DaylatroHighScoreService());
        public static DaylatroHighScoreService Instance => _lazy.Value;

        private readonly string _dataPath;
        private readonly Dictionary<string, DaylatroDailyScores> _cache = new();
        private bool _loaded;

        private DaylatroHighScoreService()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _dataPath = Path.Combine(baseDir, "daylatro_scores.json");
        }

        private void EnsureLoaded()
        {
            if (_loaded) return;
            try
            {
                if (File.Exists(_dataPath))
                {
                    var json = File.ReadAllText(_dataPath);
                    var list = JsonSerializer.Deserialize<List<DaylatroDailyScores>>(json) ?? new();
                    _cache.Clear();
                    foreach (var d in list)
                    {
                        if (!string.IsNullOrEmpty(d.Seed))
                            _cache[d.Seed] = d;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DaylatroHighScoreService", $"Error loading scores: {ex.Message}");
            }
            _loaded = true;
        }

        private void Save()
        {
            try
            {
                var list = _cache.Values.OrderByDescending(d => d.DateUtc).ToList();
                var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_dataPath, json);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DaylatroHighScoreService", $"Error saving scores: {ex.Message}");
            }
        }

        public DaylatroDailyScores GetOrCreate(string seed, DateTime dateUtc)
        {
            EnsureLoaded();
            if (!_cache.TryGetValue(seed, out var daily))
            {
                daily = new DaylatroDailyScores { Seed = seed, DateUtc = dateUtc.Date };
                _cache[seed] = daily;
            }
            return daily;
        }

        public DaylatroHighScore? GetTopScore(string seed)
        {
            EnsureLoaded();
            return _cache.TryGetValue(seed, out var daily) ? daily.GetTopScore() : null;
        }

        public DaylatroHighScore SubmitScore(string seed, DateTime dateUtc, string player, long score)
        {
            EnsureLoaded();
            var daily = GetOrCreate(seed, dateUtc);
            var entry = new DaylatroHighScore
            {
                Seed = seed,
                Player = string.IsNullOrWhiteSpace(player) ? "Anonymous" : player.Trim(),
                Score = score,
                SubmittedAtUtc = DateTime.UtcNow
            };
            daily.Scores.Add(entry);
            // Keep only top 100 to prevent unbounded growth
            daily.Scores = daily.Scores
                .OrderByDescending(s => s.Score)
                .ThenBy(s => s.SubmittedAtUtc)
                .Take(100)
                .ToList();
            Save();
            return entry;
        }
    }
}
