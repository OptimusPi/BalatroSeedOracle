using System;
using System.Collections.Generic;
using System.Linq;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for search results analytics and data visualization
    /// </summary>
    public partial class SearchAnalyticsViewModel : ObservableObject
    {
        private readonly StatisticsService _statisticsService = new();

        [ObservableProperty]
        private int _totalSeeds;

        [ObservableProperty]
        private string _avgScore = "0";

        [ObservableProperty]
        private string _medianScore = "0";

        [ObservableProperty]
        private string _stdDev = "0";

        [ObservableProperty]
        private string _minScore = "0";

        [ObservableProperty]
        private string _maxScore = "0";

        [ObservableProperty]
        private string _percentile95 = "0";

        [ObservableProperty]
        private string _percentile99 = "0";

        [ObservableProperty]
        private SKBitmap? _histogramImage;

        [ObservableProperty]
        private SKBitmap? _trendChartImage;

        /// <summary>
        /// Update analytics with new search results
        /// </summary>
        public void UpdateAnalytics(List<SearchResult> results)
        {
            if (results == null || results.Count == 0)
            {
                ClearAnalytics();
                return;
            }

            var stats = _statisticsService.CalculateStatistics(results);

            TotalSeeds = stats.TotalSeeds;
            AvgScore = FormatNumber(stats.AvgScore);
            MedianScore = FormatNumber(stats.MedianScore);
            StdDev = FormatNumber(stats.StdDev);
            MinScore = FormatNumber(stats.MinScore);
            MaxScore = FormatNumber(stats.MaxScore);
            Percentile95 = FormatNumber(stats.Percentile95);
            Percentile99 = FormatNumber(stats.Percentile99);

            // Generate charts (TODO: Fix ScottPlot 5.x API later)
            // GenerateHistogram(results);
            // GenerateTrendChart(results);
        }

        private void GenerateHistogram(List<SearchResult> results)
        {
            // TODO: Implement ScottPlot 5.x histogram when API is figured out
            return;
        }

        private void GenerateTrendChart(List<SearchResult> results)
        {
            // TODO: Implement ScottPlot 5.x trend chart when API is figured out
            return;
        }

        private void ClearAnalytics()
        {
            TotalSeeds = 0;
            AvgScore = "0";
            MedianScore = "0";
            StdDev = "0";
            MinScore = "0";
            MaxScore = "0";
            Percentile95 = "0";
            Percentile99 = "0";
            HistogramImage = null;
            TrendChartImage = null;
        }

        private string FormatNumber(double value)
        {
            return value.ToString("N0");
        }
    }
}
