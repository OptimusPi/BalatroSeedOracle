using System;
using System.Collections.Generic;
using System.Linq;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Service for calculating statistical analysis of search results
    /// </summary>
    public class StatisticsService
    {
        public class SearchStatistics
        {
            public int TotalSeeds { get; set; }
            public double AvgScore { get; set; }
            public double MedianScore { get; set; }
            public double StdDev { get; set; }
            public int MinScore { get; set; }
            public int MaxScore { get; set; }
            public double Percentile95 { get; set; }
            public double Percentile99 { get; set; }
            public List<(string Joker, double Frequency)> TopJokers { get; set; } = new();
        }

        /// <summary>
        /// Calculate comprehensive statistics from search result scores
        /// </summary>
        public SearchStatistics CalculateStatistics(List<SearchResult> results)
        {
            if (results == null || results.Count == 0)
            {
                return new SearchStatistics();
            }

            var scores = results.Select(r => (double)r.TotalScore).ToList();
            scores.Sort();

            var stats = new SearchStatistics
            {
                TotalSeeds = results.Count,
                AvgScore = scores.Average(),
                MedianScore = CalculateMedian(scores),
                StdDev = CalculateStandardDeviation(scores),
                MinScore = (int)scores.First(),
                MaxScore = (int)scores.Last(),
                Percentile95 = CalculatePercentile(scores, 95),
                Percentile99 = CalculatePercentile(scores, 99)
            };

            return stats;
        }

        private double CalculateMedian(List<double> sortedScores)
        {
            int count = sortedScores.Count;
            if (count == 0) return 0;

            if (count % 2 == 0)
            {
                // Even number: average of two middle values
                return (sortedScores[count / 2 - 1] + sortedScores[count / 2]) / 2.0;
            }
            else
            {
                // Odd number: middle value
                return sortedScores[count / 2];
            }
        }

        private double CalculateStandardDeviation(List<double> scores)
        {
            if (scores.Count == 0) return 0;

            double avg = scores.Average();
            double sumOfSquares = scores.Sum(score => Math.Pow(score - avg, 2));
            return Math.Sqrt(sumOfSquares / scores.Count);
        }

        private double CalculatePercentile(List<double> sortedScores, int percentile)
        {
            if (sortedScores.Count == 0) return 0;

            double index = (percentile / 100.0) * (sortedScores.Count - 1);
            int lowerIndex = (int)Math.Floor(index);
            int upperIndex = (int)Math.Ceiling(index);

            if (lowerIndex == upperIndex)
            {
                return sortedScores[lowerIndex];
            }

            double lowerValue = sortedScores[lowerIndex];
            double upperValue = sortedScores[upperIndex];
            double weight = index - lowerIndex;

            return lowerValue + (upperValue - lowerValue) * weight;
        }
    }
}
