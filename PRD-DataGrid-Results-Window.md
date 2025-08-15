# Product Requirements Document: Advanced DataGrid Results Window

## Executive Summary
Create a powerful, fullscreen-capable popup window featuring Avalonia's DataGrid control to display search results with advanced sorting, filtering, and export capabilities. This replaces the limited results display in SearchModal with a professional-grade data viewer.

## Problem Statement
Current results display in SearchModal is limited:
- Can't view all columns simultaneously
- No proper sorting across all tally columns
- Limited to small viewport within modal
- No filtering or advanced search within results
- Poor performance with large result sets

## Solution Overview
A dedicated popup window with Avalonia DataGrid that provides:
- Full spreadsheet-like experience
- Resizable, maximizable window
- Horizontal/vertical scrolling for unlimited columns
- Professional data manipulation capabilities
- Export functionality

## Core Requirements

### 1. Popup Window Implementation

#### 1.1 Launch Button
- **Location**: Northeast corner of Results tab in SearchModal
- **Icon**: External link/pop-out icon (↗️ or similar)
- **Tooltip**: "Open in Advanced View"
- **Behavior**: Opens new window with current results
- **State**: Disabled when no results, enabled when results > 0

#### 1.2 Window Properties
```csharp
{
    Title = $"Search Results - {filterName} ({resultCount} seeds)",
    Width = 640,
    Height = 480,
    WindowStartupLocation = WindowStartupLocation.CenterScreen,
    CanResize = true,
    ShowInTaskbar = true,
    Icon = ApplicationIcon
}
```

#### 1.3 Window Controls
- Minimize, Maximize, Close buttons
- F11 for fullscreen toggle
- ESC to exit fullscreen
- Ctrl+W to close window

### 2. DataGrid Configuration

#### 2.1 Core Setup
```xaml
<DataGrid Name="ResultsGrid"
          AutoGenerateColumns="False"
          CanUserReorderColumns="True"
          CanUserResizeColumns="True"
          CanUserSortColumns="True"
          GridLinesVisibility="All"
          HeadersVisibility="Column"
          IsReadOnly="True"
          SelectionMode="Extended"
          VirtualizationMode="Recycling">
```

#### 2.2 Column Structure
```csharp
// Fixed columns
- Seed (string) - Width: 150, Frozen
- Total Score (int) - Width: 100
- Rank (int) - Width: 60

// Dynamic tally columns (from search instance)
- Tally_0 through Tally_N - Width: 80 each
  Headers from search instance tally names
  Cell values from result.TallyScores array
```

#### 2.3 Column Features
- **Sortable**: Click header to sort asc/desc
- **Resizable**: Drag column borders
- **Reorderable**: Drag headers to reorder
- **Freeze/Pin**: Right-click to freeze columns
- **Auto-fit**: Double-click border to auto-size

### 3. Data Virtualization

#### 3.1 Performance Requirements
- Handle 100,000+ rows smoothly
- Lazy load data in chunks of 1000
- Virtual scrolling (only render visible rows)
- Smooth 60fps scrolling

#### 3.2 Implementation
```csharp
public class VirtualizedResultsCollection : INotifyCollectionChanged
{
    private readonly SearchInstance _searchInstance;
    private readonly int _pageSize = 1000;
    private readonly Dictionary<int, SearchResult> _cache;
    
    public async Task<SearchResult> GetItemAsync(int index)
    {
        if (!_cache.ContainsKey(index))
        {
            var page = index / _pageSize;
            await LoadPageAsync(page);
        }
        return _cache[index];
    }
}
```

### 4. Search & Filter Bar

#### 4.1 Layout
```
┌─────────────────────────────────────────────────────────┐
│ 🔍 [Quick Search...] [Column ▼] [Operator ▼] [Value]   │
│ Active Filters: [Seed contains "420"] [Score > 50] ✕    │
└─────────────────────────────────────────────────────────┘
```

#### 4.2 Quick Search
- Global search across all visible columns
- Highlight matching cells
- Navigate with F3/Shift+F3
- Case-insensitive by default

#### 4.3 Advanced Filters
- Column-specific filters
- Operators: =, !=, >, <, >=, <=, contains, starts with, ends with
- Multiple filters with AND/OR logic
- Save/load filter presets

### 5. Status Bar

#### 5.1 Information Display
```
│ Showing 1-100 of 42,069 results | Selected: 3 rows | Filtered: 1,337 of 42,069 │
```

#### 5.2 Components
- Current view range
- Total results count
- Selection count
- Filter status
- Sort indicator (column + direction)

### 6. Context Menu (Right-Click)

#### 6.1 Row Actions
- Copy Seed
- Copy Row (Tab-delimited)
- Copy Row (JSON)
- View in Analyzer
- Export Selected Rows

#### 6.2 Column Actions
- Sort Ascending
- Sort Descending
- Clear Sort
- Freeze Column
- Hide Column
- Auto-fit Width
- Filter by Value

### 7. Toolbar

#### 7.1 Actions
```
[Export ▼] [Copy] [Select All] [Clear] [Settings] [Help]
```

#### 7.2 Export Options
- CSV (with headers)
- TSV (Tab-separated)
- JSON
- Excel (.xlsx)
- Clipboard

#### 7.3 Settings
- Row height (Compact/Normal/Comfortable)
- Alternating row colors
- Grid lines (None/Horizontal/Vertical/Both)
- Font size
- Number formatting

### 8. Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+C | Copy selected cells |
| Ctrl+A | Select all |
| Ctrl+F | Focus search box |
| F3 | Find next |
| Shift+F3 | Find previous |
| Ctrl+E | Export |
| Delete | Clear filters |
| Home/End | Navigate to first/last row |
| Page Up/Down | Navigate page |
| Space | Select/deselect row |

### 9. Data Binding Model

#### 9.1 SearchResultViewModel
```csharp
public class SearchResultViewModel : ViewModelBase
{
    public string Seed { get; set; }
    public int TotalScore { get; set; }
    public int Rank { get; set; }
    public ObservableCollection<int> TallyScores { get; set; }
    
    // Dynamic properties for each tally
    public int this[int tallyIndex] => TallyScores[tallyIndex];
}
```

#### 9.2 DataGridResultsViewModel
```csharp
public class DataGridResultsViewModel : ViewModelBase
{
    public ObservableCollection<SearchResultViewModel> Results { get; }
    public CollectionViewSource FilteredView { get; }
    public string SearchText { get; set; }
    public List<FilterCriteria> ActiveFilters { get; }
    public SortDescription? CurrentSort { get; set; }
}
```

### 10. Integration with SearchModal

#### 10.1 Data Synchronization
- Share same SearchInstance
- Real-time updates when search is running
- Bidirectional selection sync
- Maintain sort/filter state

#### 10.2 Launch Flow
```csharp
private async void OnOpenAdvancedView()
{
    var window = new DataGridResultsWindow(_searchInstance)
    {
        DataContext = new DataGridResultsViewModel(_searchResults)
    };
    
    // Pass current configuration
    window.LoadTallyColumns(_tallyHeaders);
    window.LoadResults(_searchResults);
    
    // Subscribe to updates
    _searchInstance.ResultFound += window.OnNewResult;
    
    await window.ShowDialog(this);
}
```

### 11. Visual Design

#### 11.1 Theme
- Clean, professional appearance
- Default Avalonia Fluent theme
- No Balatro styling (pure data view)
- High contrast for readability

#### 11.2 Colors
- Header: Light gray background
- Alternating rows: White/Light gray
- Selection: Blue highlight
- Grid lines: Light gray
- Sorted column: Slightly darker background

### 12. Performance Metrics

#### 12.1 Target Performance
- Initial load: < 500ms for 10,000 rows
- Scroll FPS: 60fps minimum
- Sort operation: < 1s for 100,000 rows
- Filter application: < 500ms
- Memory usage: < 200MB for 100,000 rows

#### 12.2 Optimization Strategies
- Virtual scrolling
- Lazy loading
- Column virtualization
- Compiled bindings
- Async data operations
- Caching frequently accessed data

### 13. Error Handling

#### 13.1 Scenarios
- Database connection lost
- Out of memory
- Export failure
- Invalid filter syntax

#### 13.2 User Feedback
- Toast notifications for errors
- Progress bars for long operations
- Cancel buttons for exports
- Retry options for failed operations

### 14. Accessibility

#### 14.1 Features
- Full keyboard navigation
- Screen reader support
- High contrast mode
- Adjustable font sizes
- Focus indicators

### 15. SQL Query Editor (KILLER FEATURE!)

#### 15.1 Overview
Direct DuckDB SQL query execution against the results database with syntax highlighting and basic autocomplete. Users can write custom queries to analyze their data.

#### 15.2 Implementation
**Using AvaloniaEdit** - The Avalonia port of the powerful WPF text editor

##### Dependencies
```xml
<PackageReference Include="Avalonia.AvaloniaEdit" Version="11.x.x" />
<PackageReference Include="AvaloniaEdit.TextMate" Version="11.x.x" />
```

##### UI Layout
```
┌─────────────────────────────────────────────────────────┐
│ [▶ Run] [Clear] [Save Query] [Load Query] [Examples ▼]  │
├─────────────────────────────────────────────────────────┤
│ -- DuckDB SQL Editor                                     │
│ SELECT seed, total_score,                                │
│        tally_0 + tally_1 as combined_score              │
│ FROM search_results                                      │
│ WHERE total_score > 100                                  │
│ ORDER BY total_score DESC                                │
│ LIMIT 100;                                                │
├─────────────────────────────────────────────────────────┤
│ Query Results (42 rows, 0.023s)                         │
│ [DataGrid with query results]                           │
└─────────────────────────────────────────────────────────┘
```

#### 15.3 Features

##### SQL Syntax Highlighting
```csharp
// Configure AvaloniaEdit for SQL
var textEditor = new TextEditor
{
    ShowLineNumbers = true,
    WordWrap = false,
    FontFamily = "Cascadia Code, Consolas, monospace",
    FontSize = 13
};

// Load SQL syntax highlighting
var registryOptions = new RegistryOptions(ThemeName.Dark);
var textMateInstallation = textEditor.InstallTextMate(registryOptions);
textMateInstallation.SetGrammar("source.sql");
```

##### Basic Autocomplete
```csharp
public class DuckDbCompletionData : ICompletionData
{
    // Tables
    private readonly string[] _tables = { "search_results" };
    
    // Columns from SearchInstance
    private readonly List<string> _columns;
    
    // SQL Keywords
    private readonly string[] _keywords = {
        "SELECT", "FROM", "WHERE", "ORDER BY", "GROUP BY",
        "HAVING", "LIMIT", "OFFSET", "JOIN", "LEFT JOIN",
        "RIGHT JOIN", "INNER JOIN", "ON", "AS", "AND", "OR",
        "NOT", "IN", "EXISTS", "BETWEEN", "LIKE", "DESC", "ASC"
    };
    
    // DuckDB Functions
    private readonly string[] _functions = {
        "COUNT", "SUM", "AVG", "MIN", "MAX", "MEDIAN",
        "STDDEV", "VARIANCE", "STRING_AGG", "LIST_AGG",
        "FIRST", "LAST", "ANY_VALUE", "PERCENTILE_CONT"
    };
}
```

##### Query Execution
```csharp
public async Task<QueryResult> ExecuteQueryAsync(string sql)
{
    try
    {
        using var connection = _searchInstance.GetDuckDbConnection();
        using var command = connection.CreateCommand(sql);
        
        var stopwatch = Stopwatch.StartNew();
        var result = await command.ExecuteReaderAsync();
        stopwatch.Stop();
        
        var data = new DataTable();
        data.Load(result);
        
        return new QueryResult
        {
            Data = data,
            RowCount = data.Rows.Count,
            ExecutionTime = stopwatch.ElapsedMilliseconds,
            Success = true
        };
    }
    catch (Exception ex)
    {
        return new QueryResult
        {
            Error = ex.Message,
            Success = false
        };
    }
}
```

#### 15.4 Example Queries Menu
```sql
-- Top 100 Seeds by Score
SELECT seed, total_score FROM search_results
ORDER BY total_score DESC LIMIT 100;

-- Seeds with specific tally combinations
SELECT * FROM search_results
WHERE tally_0 > 10 AND tally_1 > 5;

-- Statistical Analysis
SELECT 
    COUNT(*) as total_seeds,
    AVG(total_score) as avg_score,
    MIN(total_score) as min_score,
    MAX(total_score) as max_score,
    MEDIAN(total_score) as median_score,
    STDDEV(total_score) as stddev_score
FROM search_results;

-- Find seeds with extreme values
SELECT seed, total_score,
    RANK() OVER (ORDER BY total_score DESC) as rank
FROM search_results
WHERE total_score > (
    SELECT PERCENTILE_CONT(0.99) WITHIN GROUP (ORDER BY total_score)
    FROM search_results
);

-- Export to CSV directly
COPY (SELECT * FROM search_results WHERE total_score > 100)
TO 'high_score_seeds.csv' (HEADER, DELIMITER ',');
```

#### 15.5 Safety & UX
- **No SQL injection concerns** - Local database, user's own data
- **Query history** - Store last 50 queries
- **Keyboard shortcuts**:
  - F5 or Ctrl+Enter: Execute query
  - Ctrl+Space: Trigger autocomplete
  - Ctrl+/ : Comment/uncomment lines
- **Error display** - Clear error messages below editor
- **Result limit** - Default LIMIT 1000 for safety (configurable)

#### 15.6 Integration
```csharp
public partial class DataGridResultsWindow : Window
{
    private readonly SearchInstance _searchInstance;
    private TextEditor _sqlEditor;
    private DataGrid _queryResultsGrid;
    
    public DataGridResultsWindow(SearchInstance searchInstance)
    {
        _searchInstance = searchInstance;
        InitializeSqlEditor();
        LoadTableSchema();
    }
    
    private void LoadTableSchema()
    {
        // Get column names from search instance
        var columns = _searchInstance.GetTallyNames();
        // Configure autocomplete with actual schema
    }
}
```

### 16. Future Enhancements

#### Phase 2
- Full SQL autocomplete with table/column awareness
- Query plan visualization
- Query performance profiling
- Saved query library
- Query parameterization

#### Phase 3
- Visual query builder (drag & drop)
- Real-time collaboration on queries
- Export query results to various formats
- Integration with external BI tools
- Custom DuckDB extensions

## Success Criteria

### Functional
- [ ] Can display 100,000+ results without lag
- [ ] Sort any column in < 1 second
- [ ] Export to CSV/JSON/Excel
- [ ] Filter results with multiple criteria
- [ ] Copy/paste integration works

### Performance
- [ ] 60fps scrolling maintained
- [ ] < 500ms initial load time
- [ ] < 200MB memory for large datasets
- [ ] No UI freezing during operations

### Usability
- [ ] Users can find specific seeds quickly
- [ ] Sorting is intuitive and fast
- [ ] Export formats are useful
- [ ] Window is responsive and resizable
- [ ] Keyboard shortcuts improve efficiency

## Technical Implementation Notes

### Dependencies
```xml
<PackageReference Include="Avalonia.Controls.DataGrid" Version="11.x.x" />
<PackageReference Include="ClosedXML" Version="0.x.x" /> <!-- For Excel export -->
```

### File Structure
```
/src/Windows/
    DataGridResultsWindow.axaml
    DataGridResultsWindow.axaml.cs
/src/ViewModels/
    DataGridResultsViewModel.cs
    SearchResultViewModel.cs
/src/Services/
    ResultsExportService.cs
    DataVirtualizationService.cs
```

## Acceptance Tests

1. **Load Test**: Load 100,000 results, verify smooth scrolling
2. **Sort Test**: Sort by each column type, verify correctness
3. **Filter Test**: Apply complex filters, verify results
4. **Export Test**: Export to each format, verify data integrity
5. **Memory Test**: Monitor memory usage with large datasets
6. **Responsiveness Test**: Resize window, verify layout adapts

## Risk Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Performance with huge datasets | High | Implement proper virtualization |
| Memory leaks | High | Use weak event patterns |
| Export taking too long | Medium | Background thread + progress |
| Complex filters slow | Medium | Compile filter expressions |

---

*This PRD ensures the DataGrid Results Window provides a professional, high-performance solution for viewing and analyzing search results, matching the capabilities of enterprise-grade data tools while maintaining ease of use.*