using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Persists Daylatro high scores locally in a JSON file and fetches from daylatro.fly.dev.
    /// Simple append + in-memory cache; not concurrency safe across multiple processes.
    /// </summary>
    public class DaylatroHighScoreService
    {
        private static readonly Lazy<DaylatroHighScoreService> _lazy = new(() =>
            new DaylatroHighScoreService()
        );
        public static DaylatroHighScoreService Instance => _lazy.Value;

        private const string DAYLATRO_URL = "https://daylatro.fly.dev";
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10),
        };

        private readonly string _dataPath;
        private readonly string _submissionsPath;
        private readonly Dictionary<string, DaylatroDailyScores> _cache = new();
        private bool _loaded;

        // Web cache for fetched scores (5 minute expiration)
        private readonly Dictionary<
            string,
            (List<DaylatroHighScore> scores, DateTime fetchTime)
        > _webCache = new();
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        // Track submissions per day (UTC)
        private readonly Dictionary<string, DateTime> _lastSubmissionDates = new();

        private DaylatroHighScoreService()
        {
            var baseDir = AppContext.BaseDirectory;
            _dataPath = Path.Combine(baseDir, "daylatro_scores.json");
            _submissionsPath = Path.Combine(baseDir, "daylatro_submissions.json");
            LoadSubmissionDates();
        }

        private void EnsureLoaded()
        {
            if (_loaded)
                return;
#if BROWSER
            // Browser: Skip file loading
            _loaded = true;
            return;
#else
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
                DebugLogger.LogError(
                    "DaylatroHighScoreService",
                    $"Error loading scores: {ex.Message}"
                );
            }
            _loaded = true;
#endif
        }

        private void Save()
        {
            try
            {
                var list = _cache.Values.OrderByDescending(d => d.DateUtc).ToList();
                var json = JsonSerializer.Serialize(
                    list,
                    new JsonSerializerOptions { WriteIndented = true }
                );
                File.WriteAllText(_dataPath, json);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "DaylatroHighScoreService",
                    $"Error saving scores: {ex.Message}"
                );
            }
        }

        private void LoadSubmissionDates()
        {
#if BROWSER
            // Browser: Skip file loading
            return;
#else
            try
            {
                if (File.Exists(_submissionsPath))
                {
                    var json = File.ReadAllText(_submissionsPath);
                    var dates = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json);
                    if (dates != null)
                    {
                        _lastSubmissionDates.Clear();
                        foreach (var kvp in dates)
                        {
                            _lastSubmissionDates[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "DaylatroHighScoreService",
                    $"Error loading submission dates: {ex.Message}"
                );
            }
#endif
        }

        private void SaveSubmissionDates()
        {
            try
            {
                var json = JsonSerializer.Serialize(
                    _lastSubmissionDates,
                    new JsonSerializerOptions { WriteIndented = true }
                );
                File.WriteAllText(_submissionsPath, json);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "DaylatroHighScoreService",
                    $"Error saving submission dates: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Check if user can submit a score today (once per UTC day)
        /// </summary>
        public bool CanSubmitToday()
        {
            var todayUtc = DateTime.UtcNow.Date;
            var todayKey = todayUtc.ToString("yyyy-MM-dd");

            if (_lastSubmissionDates.TryGetValue(todayKey, out var lastSubmission))
            {
                // Already submitted today
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get the last submission date for today's challenge
        /// </summary>
        public DateTime? GetLastSubmissionDate()
        {
            var todayKey = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            return _lastSubmissionDates.TryGetValue(todayKey, out var date) ? date : null;
        }

        private void RecordSubmission()
        {
            var todayKey = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            _lastSubmissionDates[todayKey] = DateTime.UtcNow;
            SaveSubmissionDates();
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

        public DaylatroHighScore SubmitScore(
            string seed,
            DateTime dateUtc,
            string player,
            long score
        )
        {
            EnsureLoaded();
            var daily = GetOrCreate(seed, dateUtc);
            var entry = new DaylatroHighScore
            {
                Seed = seed,
                Player = string.IsNullOrWhiteSpace(player) ? "Anonymous" : player.Trim(),
                Score = score,
                SubmittedAtUtc = DateTime.UtcNow,
            };
            daily.Scores.Add(entry);
            // Keep only top 100 to prevent unbounded growth
            daily.Scores = daily
                .Scores.OrderByDescending(s => s.Score)
                .ThenBy(s => s.SubmittedAtUtc)
                .Take(100)
                .ToList();
            Save();

            // Also try to submit to the actual Daylatro site
            _ = SubmitToDaylatroAsync(entry);

            return entry;
        }

        /// <summary>
        /// Fetches the current day's leaderboard from daylatro.fly.dev with caching
        /// </summary>
        public async Task<List<DaylatroHighScore>> FetchDaylatroScoresAsync(
            string? day = null,
            bool forceRefresh = false
        )
        {
            try
            {
                day ??= DateTime.UtcNow.ToString("yyyy-MM-dd");

                // Check cache first unless force refresh is requested
                if (!forceRefresh && _webCache.TryGetValue(day, out var cached))
                {
                    if (DateTime.UtcNow - cached.fetchTime < _cacheExpiration)
                    {
                        DebugLogger.Log(
                            "DaylatroHighScoreService",
                            $"Returning cached scores for {day} (cached at {cached.fetchTime})"
                        );
                        return cached.scores;
                    }
                }

                // Fetch the page with the specific day parameter
                var url = $"{DAYLATRO_URL}?day={day}";
                var response = await _httpClient.GetStringAsync(url);

                DebugLogger.Log(
                    "DaylatroHighScoreService",
                    $"Fetched page from {url} (length: {response.Length})"
                );

                // Parse the HTML to extract scores from the table
                var scores = ParseDaylatroHtml(response);

                // Update cache
                _webCache[day] = (scores, DateTime.UtcNow);

                DebugLogger.Log(
                    "DaylatroHighScoreService",
                    $"Parsed {scores.Count} scores from Daylatro and cached"
                );
                return scores;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "DaylatroHighScoreService",
                    $"Error fetching from Daylatro: {ex.Message}"
                );

                // Try to return cached data even if expired
                if (
                    _webCache.TryGetValue(
                        day ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        out var cached
                    )
                )
                {
                    DebugLogger.Log(
                        "DaylatroHighScoreService",
                        "Returning expired cache due to fetch error"
                    );
                    return cached.scores;
                }

                return new List<DaylatroHighScore>();
            }
        }

        /// <summary>
        /// Parses the Daylatro HTML to extract high scores
        /// </summary>
        private List<DaylatroHighScore> ParseDaylatroHtml(string html)
        {
            var scores = new List<DaylatroHighScore>();

            try
            {
                // Look for the table containing scores
                // The table has rows with: Name, Ante, Best Hand
                // Example HTML structure:
                // <tr><td>ABC</td><td>8</td><td>123456</td></tr>

                // Find the table - try with tbody first, then without
                var tableStart = html.IndexOf("<tbody");
                var tableEnd = -1;

                if (tableStart == -1)
                {
                    // No tbody, look for table directly
                    tableStart = html.IndexOf("<table");
                    if (tableStart == -1)
                    {
                        DebugLogger.Log("DaylatroHighScoreService", "No table found in HTML");
                        return scores;
                    }
                    tableEnd = html.IndexOf("</table>", tableStart);
                }
                else
                {
                    tableEnd = html.IndexOf("</tbody>", tableStart);
                }

                if (tableEnd == -1)
                    return scores;

                var tableContent = html.Substring(tableStart, tableEnd - tableStart);

                // Parse each row (skip header row with <th> tags)
                var rowStart = 0;

                while ((rowStart = tableContent.IndexOf("<tr", rowStart)) != -1)
                {
                    var rowEnd = tableContent.IndexOf("</tr>", rowStart);
                    if (rowEnd == -1)
                        break;

                    var row = tableContent.Substring(rowStart, rowEnd - rowStart);

                    // Skip header row (contains <th> tags)
                    if (row.Contains("<th"))
                    {
                        rowStart = rowEnd;
                        continue;
                    }

                    // Extract cells from the row
                    var cells = new List<string>();
                    var cellStart = 0;
                    while ((cellStart = row.IndexOf("<td", cellStart)) != -1)
                    {
                        var cellContentStart = row.IndexOf(">", cellStart) + 1;
                        var cellEnd = row.IndexOf("</td>", cellStart);
                        if (cellEnd == -1)
                            break;

                        var cellContent = row.Substring(
                            cellContentStart,
                            cellEnd - cellContentStart
                        );
                        cells.Add(cellContent.Trim());
                        cellStart = cellEnd;
                    }

                    // Parse the cells: Name, Ante, Best Hand (no rank column)
                    if (cells.Count >= 3)
                    {
                        if (
                            int.TryParse(cells[1], out var ante)
                            && long.TryParse(cells[2].Replace(",", ""), out var bestHand)
                        )
                        {
                            scores.Add(
                                new DaylatroHighScore
                                {
                                    Player = cells[0],
                                    Ante = ante,
                                    Score = bestHand,
                                    SubmittedAtUtc = DateTime.UtcNow,
                                }
                            );

                            DebugLogger.Log(
                                "DaylatroHighScoreService",
                                $"Parsed score: {cells[0]} - Ante {ante} - {bestHand}"
                            );
                        }
                    }

                    rowStart = rowEnd;
                }

                DebugLogger.Log("DaylatroHighScoreService", $"Total scores parsed: {scores.Count}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "DaylatroHighScoreService",
                    $"Error parsing HTML: {ex.Message}"
                );
            }

            return scores;
        }

        /// <summary>
        /// Submits a score to daylatro.fly.dev with specific initials and ante
        /// </summary>
        public async Task<(bool success, string message)> SubmitToDaylatroAsync(
            string initials,
            int ante,
            long score
        )
        {
            try
            {
                // Check if already submitted today
                if (!CanSubmitToday())
                {
                    var lastSubmission = GetLastSubmissionDate();
                    var timeUntilReset = DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow;
                    var hoursUntilReset = (int)timeUntilReset.TotalHours;
                    var minutesUntilReset = (int)timeUntilReset.Minutes;

                    DebugLogger.Log("DaylatroHighScoreService", "User already submitted today");
                    return (
                        false,
                        $"You've already submitted today's score. You can submit again in {hoursUntilReset}h {minutesUntilReset}m (UTC midnight)"
                    );
                }

                // Form data format from actual site: day=2025-08-14&name=BSO&ante=2&bestHand=12287
                // Name must be 3 characters
                var playerName =
                    initials.Length > 3
                        ? initials.Substring(0, 3).ToUpper()
                        : initials.ToUpper().PadRight(3);
                var day = DateTime.UtcNow.ToString("yyyy-MM-dd");

                DebugLogger.Log(
                    "DaylatroHighScoreService",
                    $"Submitting to Daylatro: day={day}, name={playerName}, ante={ante}, bestHand={score}"
                );

                var content = new FormUrlEncodedContent(
                    new[]
                    {
                        new KeyValuePair<string, string>("day", day),
                        new KeyValuePair<string, string>("name", playerName),
                        new KeyValuePair<string, string>("ante", ante.ToString()),
                        new KeyValuePair<string, string>("bestHand", score.ToString()),
                    }
                );

                var response = await _httpClient.PostAsync(DAYLATRO_URL, content);

                if (response.IsSuccessStatusCode)
                {
                    DebugLogger.Log(
                        "DaylatroHighScoreService",
                        $"Successfully submitted score to Daylatro: {score} by {playerName}"
                    );

                    // Record the submission
                    RecordSubmission();

                    // Clear cache for today's scores so they'll be refreshed
                    var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
                    if (_webCache.ContainsKey(today))
                    {
                        _webCache.Remove(today);
                        DebugLogger.Log(
                            "DaylatroHighScoreService",
                            "Cleared today's cache after submission"
                        );
                    }

                    return (true, $"Score submitted successfully! {playerName}: {score:N0}");
                }
                else
                {
                    DebugLogger.LogError(
                        "DaylatroHighScoreService",
                        $"Failed to submit to Daylatro: {response.StatusCode}"
                    );
                    return (
                        false,
                        $"Failed to submit score (server returned {response.StatusCode})"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "DaylatroHighScoreService",
                    $"Error submitting to Daylatro: {ex.Message}"
                );
                return (false, $"Error submitting score: {ex.Message}");
            }
        }

        /// <summary>
        /// Submits a score to daylatro.fly.dev (old overload for compatibility)
        /// </summary>
        private async Task<bool> SubmitToDaylatroAsync(DaylatroHighScore score)
        {
            try
            {
                // Form data format from actual site: day=2025-08-14&name=BSO&ante=2&bestHand=12287
                // Name must be 3 characters
                var playerName =
                    score.Player.Length > 3
                        ? score.Player.Substring(0, 3).ToUpper()
                        : score.Player.ToUpper().PadRight(3);
                var day = DateTime.UtcNow.ToString("yyyy-MM-dd");

                DebugLogger.Log(
                    "DaylatroHighScoreService",
                    $"Submitting to Daylatro: day={day}, name={playerName}, ante={score.Ante}, bestHand={score.Score}"
                );

                var content = new FormUrlEncodedContent(
                    new[]
                    {
                        new KeyValuePair<string, string>("day", day),
                        new KeyValuePair<string, string>("name", playerName),
                        new KeyValuePair<string, string>("ante", score.Ante.ToString()),
                        new KeyValuePair<string, string>("bestHand", score.Score.ToString()),
                    }
                );

                var response = await _httpClient.PostAsync(DAYLATRO_URL, content);

                if (response.IsSuccessStatusCode)
                {
                    DebugLogger.Log(
                        "DaylatroHighScoreService",
                        $"Successfully submitted score to Daylatro: {score.Score} by {score.Player}"
                    );
                    return true;
                }
                else
                {
                    DebugLogger.LogError(
                        "DaylatroHighScoreService",
                        $"Failed to submit to Daylatro: {response.StatusCode}"
                    );
                    return false;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "DaylatroHighScoreService",
                    $"Error submitting to Daylatro: {ex.Message}"
                );
                return false;
            }
        }
    }
}
