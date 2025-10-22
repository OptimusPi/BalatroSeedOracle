using System.Linq;

namespace BalatroSeedOracle.Models;

/// <summary>
/// Represents a single search result from MotelyJson
/// </summary>
public class SearchResult
{
    public string Seed { get; set; } = "";
    public int TotalScore { get; set; }
    public int[]? Scores { get; set; }
    public string[]? Labels { get; set; }  // Only used temporarily for first result to establish column headers

    /// <summary>
    /// Display string for the scores array
    /// </summary>
    public string ScoresDisplay
    {
        get
        {
            if (Scores == null || Scores.Length == 0)
                return "No details";

            if (Labels != null && Labels.Length == Scores.Length)
            {
                // Show labels with scores
                var pairs = Labels.Zip(Scores, (label, score) => $"{label}: {score}");
                return string.Join(", ", pairs);
            }
            else
            {
                // Just show scores
                return string.Join(", ", Scores);
            }
        }
    }
}
