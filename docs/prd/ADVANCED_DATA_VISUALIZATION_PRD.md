# Advanced Data Visualization & Analytics - Product Requirements Document

**Date**: 2025-11-16
**Feature**: Interactive Charts & Statistical Analysis for Seed Search Results
**Status**: READY FOR IMPLEMENTATION
**Priority**: HIGH - Major UX Enhancement

---

## Executive Summary

Transform **BalatroSeedOracle** from a simple results grid into a **data visualization powerhouse** using:
- **ScottPlot** - Interactive plotting library (RECOMMENDED)
- **LiveCharts2** - Beautiful animated charts
- **OxyPlot** - Versatile charting (backup option)

### What Users Get:
- 📊 **Score Distribution Graphs** - Histogram of seed scores
- 📈 **Trend Analysis** - Score over time/seed number
- 🎯 **Filter Effectiveness** - Compare filters side-by-side
- 🔥 **Heatmaps** - Joker frequency, deck popularity
- 📉 **Statistical Insights** - Mean, median, percentiles, outliers

---

## Why This is AWESOME

### Current State:
```
[Table of Results]
Seed 1: 15,234 pts
Seed 2: 18,456 pts
Seed 3: 12,789 pts
...
```
Users have to mentally analyze patterns.

### With Visualization:
```
[Interactive Chart]
  Score
   ^
   |     ┌─────┐
   |     │     │ ← Most seeds here!
   |   ┌─┼──┐  │
   |   │ │  │  │
   +───┴─┴──┴──┴─> Seed #
```
**Instant insights** - see patterns at a glance!

---

## Core Features

### 1. Score Distribution Histogram

**What**: Bar chart showing how many seeds fall into each score range

**Example**:
```
  Count
    ^
 80 │         ██
 60 │      ██ ██
 40 │   ██ ██ ██ ██
 20 │██ ██ ██ ██ ██ ██
  0 └──────────────────> Score Range
     0-5K 5-10K 10-15K 15-20K 20-25K 25K+
```

**Insights**:
- "Most seeds score between 10-15K"
- "Very few seeds exceed 25K (outliers!)"
- "This filter has a normal distribution"

**Implementation (ScottPlot)**:
```csharp
var plt = new ScottPlot.Plot(600, 400);

// Get score data from search results
var scores = searchResults.Select(r => r.Score).ToArray();

// Create histogram
var hist = ScottPlot.Statistics.Histogram(scores, min: 0, max: 30000, binSize: 2500);
plt.AddBar(hist.counts, hist.bins);

plt.Title("Seed Score Distribution");
plt.XLabel("Score Range");
plt.YLabel("Number of Seeds");

// Convert to Avalonia Image
var image = plt.Render();
HistogramImage = ConvertToAvaloniaImage(image);
```

---

### 2. Score Trend Line Chart

**What**: Line graph showing score progression over seed numbers

**Example**:
```
  Score
    ^
25K │         ●
    │       ●   ●
15K │   ● ●       ● ●
    │ ●             ●
 5K │
    └─────────────────> Seed Number
      1K  2K  3K  4K  5K
```

**Insights**:
- "Scores increase with higher seed numbers"
- "There's a spike around seed 3500"
- "Outlier at seed 4127 (investigate!)"

**Implementation (ScottPlot)**:
```csharp
var plt = new ScottPlot.Plot(800, 400);

var xs = searchResults.Select((r, i) => (double)i).ToArray();
var ys = searchResults.Select(r => (double)r.Score).ToArray();

plt.AddScatter(xs, ys, lineWidth: 2, markerSize: 5);
plt.Title("Score Trend Across Seeds");
plt.XLabel("Seed Index");
plt.YLabel("Score");

TrendChartImage = ConvertToAvaloniaImage(plt.Render());
```

---

### 3. Joker Frequency Heatmap

**What**: Visual representation of which jokers appear most often in high-scoring seeds

**Example**:
```
        Joker Frequency in Top 100 Seeds
        ┌───────────────────────────────┐
  Joker │ 10%  20%  30%  40%  50%  60% │
────────┼───────────────────────────────┤
Blueprint│██████████████████████████████│ 60%
Brainstorm│████████████████████         │ 40%
Baron    │██████████████                │ 30%
Joker    │████████                      │ 15%
```

**Insights**:
- "Blueprint appears in 60% of top seeds"
- "Brainstorm is key for high scores"
- "Joker (base) is rare in winning seeds"

**Implementation (LiveCharts2)**:
```csharp
var series = new RowSeries<JokerFrequency>
{
    Values = jokerStats,
    Mapping = (item, point) =>
    {
        point.PrimaryValue = item.Frequency;
        point.SecondaryValue = item.Index;
    },
    DataLabelsPaint = new SolidColorPaint(SKColors.White),
    DataLabelsFormatter = (point) => $"{point.PrimaryValue:P0}"
};

var chart = new CartesianChart
{
    Series = new[] { series },
    XAxes = new[] { new Axis { Labels = jokerNames } }
};
```

---

### 4. Filter Comparison (Side-by-Side)

**What**: Compare 2+ filters to see which performs better

**Example**:
```
Filter A: "Blueprint + Brainstorm"
  Avg Score: 18,234
  Top Score: 28,567
  Seeds Found: 342

Filter B: "Baron + Madness"
  Avg Score: 15,123
  Top Score: 24,891
  Seeds Found: 198

Winner: Filter A (20% higher avg score!)
```

**Implementation**:
```csharp
var plt = new ScottPlot.Plot(600, 400);

var filterNames = new[] { "Filter A", "Filter B" };
var avgScores = new[] { 18234.0, 15123.0 };
var topScores = new[] { 28567.0, 24891.0 };

var bar1 = plt.AddBar(avgScores);
bar1.Label = "Avg Score";

var bar2 = plt.AddBar(topScores);
bar2.Label = "Top Score";
bar2.BarWidth = 0.4;

plt.XTicks(filterNames);
plt.Legend();

ComparisonChartImage = ConvertToAvaloniaImage(plt.Render());
```

---

### 5. Statistical Summary Card

**What**: Key stats at a glance

**Example**:
```
┌─────────────────────────────────┐
│  SEARCH STATISTICS              │
├─────────────────────────────────┤
│  Seeds Found:        1,234      │
│  Average Score:      16,789     │
│  Median Score:       15,234     │
│  Std Deviation:      4,567      │
│  Min Score:          8,234      │
│  Max Score:          28,567     │
│  95th Percentile:    24,123     │
├─────────────────────────────────┤
│  Top Jokers:                    │
│    1. Blueprint (72%)           │
│    2. Brainstorm (58%)          │
│    3. Baron (43%)               │
└─────────────────────────────────┘
```

**Implementation (ViewModel Properties)**:
```csharp
public SearchStatistics Statistics { get; set; }

public class SearchStatistics
{
    public int TotalSeeds { get; set; }
    public double AvgScore { get; set; }
    public double MedianScore { get; set; }
    public double StdDev { get; set; }
    public int MinScore { get; set; }
    public int MaxScore { get; set; }
    public double Percentile95 { get; set; }
    public List<(string Joker, double Frequency)> TopJokers { get; set; }
}
```

---

## UI Layout Proposal

### Tab 1: Results Grid (Existing)
- Keep current grid view
- Add "📊 Show Analytics" button

### Tab 2: Analytics Dashboard (NEW)
```
┌─────────────────────────────────────────────┐
│  SEARCH ANALYTICS                           │
├──────────────────┬──────────────────────────┤
│                  │                          │
│  [Score Dist]    │   [Stats Summary]        │
│  Histogram       │   - Avg: 16,789          │
│                  │   - Med: 15,234          │
│                  │   - Top: 28,567          │
├──────────────────┴──────────────────────────┤
│  [Score Trend Line Chart]                   │
│  Shows score over seed progression          │
├─────────────────────────────────────────────┤
│  [Joker Frequency Heatmap]                  │
│  Blueprint ████████████████ 60%             │
│  Brainstorm ███████████ 40%                 │
└─────────────────────────────────────────────┘

[Export as PNG] [Save Report]
```

---

## Library Comparison

### ScottPlot (RECOMMENDED)

**Pros**:
- ✅ **Fast** - Renders 1M+ points smoothly
- ✅ **Interactive** - Pan, zoom, click
- ✅ **Avalonia Support** - Native integration
- ✅ **Easy API** - `plt.AddScatter(x, y)`
- ✅ **Export** - PNG, SVG, bitmap

**Cons**:
- ⚠️ Less "pretty" than LiveCharts (more functional)

**Example**:
```csharp
var plt = new ScottPlot.Plot(600, 400);
plt.AddScatter(xs, ys);
plt.SaveFig("chart.png");
```

**NuGet**: `ScottPlot.Avalonia`

---

### LiveCharts2 (PRETTY & ANIMATED)

**Pros**:
- ✅ **Beautiful** - Modern, animated charts
- ✅ **Responsive** - Reactive data binding
- ✅ **MVVM-friendly** - Works great with Avalonia
- ✅ **Many chart types** - Pie, donut, polar, etc.

**Cons**:
- ⚠️ Slower with large datasets (10K+ points)
- ⚠️ More complex API

**Example**:
```csharp
var series = new LineSeries<double>
{
    Values = new[] { 2, 1, 3, 5, 3, 4, 6 },
    Fill = null
};

var chart = new CartesianChart
{
    Series = new ISeries[] { series }
};
```

**NuGet**: `LiveChartsCore.SkiaSharpView.Avalonia`

---

### OxyPlot (MATURE)

**Pros**:
- ✅ Very stable
- ✅ Lots of chart types
- ✅ Good docs

**Cons**:
- ⚠️ Less modern look
- ⚠️ Slower updates

**Verdict**: Use if ScottPlot/LiveCharts don't work

---

## Implementation Plan

### Phase 1: Infrastructure (1 hour)
- [ ] Add ScottPlot.Avalonia NuGet package
- [ ] Create `AnalyticsTab.axaml` view
- [ ] Create `AnalyticsTabViewModel.cs`
- [ ] Add "Analytics" tab to search results modal

### Phase 2: Basic Charts (2 hours)
- [ ] Implement Score Distribution Histogram
- [ ] Implement Score Trend Line Chart
- [ ] Add export to PNG button
- [ ] Style charts to match Balatro theme

### Phase 3: Statistics (1 hour)
- [ ] Calculate mean, median, std dev
- [ ] Calculate percentiles (95th, 99th)
- [ ] Create stats summary card UI
- [ ] Add top jokers frequency analysis

### Phase 4: Advanced Features (2 hours)
- [ ] Joker frequency heatmap
- [ ] Filter comparison mode
- [ ] Interactive chart tooltips
- [ ] Zoom/pan on charts

### Phase 5: Polish (1 hour)
- [ ] Dark theme for charts
- [ ] Balatro color scheme (red/blue/gold)
- [ ] Loading states
- [ ] Empty state ("No data to visualize")

**Total Time**: 7 hours

---

## Files to Create/Modify

| File | Change Type | Description |
|------|-------------|-------------|
| `Components/Tabs/AnalyticsTab.axaml` | CREATE | New analytics view |
| `ViewModels/AnalyticsTabViewModel.cs` | CREATE | ViewModel with chart data |
| `Services/StatisticsService.cs` | CREATE | Calculate stats from results |
| `Views/SearchResultsModal.axaml` | MODIFY | Add Analytics tab |
| `Styles/ChartStyles.axaml` | CREATE | Dark theme for charts |

---

## Example: Score Distribution Histogram

**XAML**:
```xml
<UserControl xmlns:scottPlot="clr-namespace:ScottPlot.Avalonia;assembly=ScottPlot.Avalonia">
    <Grid RowDefinitions="Auto,*">
        <TextBlock Grid.Row="0"
                   Text="Score Distribution"
                   Classes="section-header"
                   Margin="0,0,0,12"/>

        <scottPlot:AvaPlot Grid.Row="1"
                           x:Name="ScorePlot"
                           Height="300"/>
    </Grid>
</UserControl>
```

**Code-Behind (or ViewModel)**:
```csharp
public void UpdateHistogram(List<SearchResult> results)
{
    var scores = results.Select(r => (double)r.Score).ToArray();

    var hist = ScottPlot.Statistics.Histogram(
        values: scores,
        min: 0,
        max: 30000,
        binSize: 2500
    );

    ScorePlot.Plot.Clear();
    ScorePlot.Plot.AddBar(hist.counts, hist.bins);
    ScorePlot.Plot.XLabel("Score Range");
    ScorePlot.Plot.YLabel("Number of Seeds");
    ScorePlot.Plot.Title("Score Distribution");

    // Balatro theme colors
    ScorePlot.Plot.Style(
        figureBackground: SKColors.Parse("#1e2b2d"),
        dataBackground: SKColors.Parse("#2a3f41"),
        grid: SKColors.Parse("#3a4f51")
    );

    ScorePlot.Refresh();
}
```

---

## Success Criteria

1. ✅ Users can view score distribution histogram
2. ✅ Score trend chart shows progression clearly
3. ✅ Stats summary displays correct calculations
4. ✅ Charts render in <1 second for 1000 results
5. ✅ Export to PNG works
6. ✅ Dark theme matches Balatro aesthetic
7. ✅ Mobile-responsive (charts scale down)

---

## Future Enhancements

- **Live Updates**: Charts update as search runs (realtime)
- **3D Plots**: Score vs Joker Count vs Deck Type
- **Machine Learning**: "Predict best filter" based on patterns
- **A/B Testing**: Built-in filter comparison mode
- **Export Reports**: PDF with all charts + analysis

---

**Status**: Ready to Rock 🎸
**Estimated Time**: 1 week
**Impact**: Users can find optimal filters 10x faster
