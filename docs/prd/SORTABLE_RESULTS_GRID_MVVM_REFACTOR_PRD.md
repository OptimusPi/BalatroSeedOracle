# PRD: SortableResultsGrid MVVM Refactor

## Executive Summary
The `SortableResultsGrid` control currently violates MVVM architecture by implementing business logic and state management directly in the code-behind (`.axaml.cs`). This PRD defines the refactoring needed to move all logic to a proper ViewModel while maintaining existing functionality.

## Problem Statement
Current issues with SortableResultsGrid:
1. **Code-behind logic**: Lines 25-573 contain business logic that should be in a ViewModel
2. **Direct UI manipulation**: Methods like `UpdatePageInfo()` (lines 289-303), `UpdateResultsCount()` (271-287), `UpdateStats()` (305-320) directly manipulate UI controls
3. **No data binding**: Uses `FindControl<T>()` to find and update UI elements instead of property bindings
4. **Manual state management**: Private fields (`_currentPage`, `_totalPages`, `_currentSortColumn`, etc.) should be observable properties
5. **Event-driven updates**: Uses events instead of reactive property bindings
6. **Mixed concerns**: Sorting, pagination, filtering, and UI updates all in code-behind

## Current Architecture (WRONG)

### File: `SortableResultsGrid.axaml.cs`
**Lines 25-33**: Private state fields
```csharp
private readonly ObservableCollection<SearchResult> _allResults = new();
private readonly ObservableCollection<SearchResult> _displayedResults = new();
private string _currentSortColumn = "TotalScore";
private bool _sortDescending = true;
private int _currentPage = 1;
private int _itemsPerPage = 100;
private int _totalPages = 1;
private ObservableCollection<SearchResult>? _itemsSource;
private bool _tallyColumnsInitialized = false;
```

**Lines 289-303**: Direct UI manipulation (BAD!)
```csharp
private void UpdatePageInfo()
{
    // Support either legacy TextBlock or new badge-style Button
    var pageBadge = this.FindControl<Button>("PageInfoBadge");
    if (pageBadge is not null)
    {
        pageBadge.Content = $"Page {_currentPage} of {_totalPages}";
    }
    // ...
}
```

**Lines 271-287**: More direct UI manipulation
```csharp
private void UpdateResultsCount()
{
    var resultsCountText = this.FindControl<TextBlock>("ResultsCountText")!;
    if (_allResults.Count == 0)
    {
        resultsCountText.Text = "No results";
    }
    // ...
}
```

### File: `SortableResultsGrid.axaml`
**Lines 35-39**: TextBlock with no binding
```xml
<TextBlock Name="ResultsCountText"
           Text="0 results"
           Foreground="{StaticResource Gold}"
           FontSize="14"
           VerticalAlignment="Center"/>
```

**Lines 176-182**: Button used as label with no binding
```xml
<Button Name="PageInfoBadge"
        Content="Page 1 of 1"
        Classes="btn-red"
        MinWidth="100"
        Height="32"
        IsHitTestVisible="False"
        VerticalAlignment="Center"/>
```

**Lines 49-58**: ComboBox with event handler instead of binding
```xml
<ComboBox Name="SortComboBox"
          Background="{StaticResource DarkBackground}"
          Foreground="{StaticResource Gold}"
          BorderBrush="{StaticResource ModalBorder}"
          SelectionChanged="SortComboBox_SelectionChanged"
          MinWidth="160">
```

## Target Architecture (CORRECT MVVM)

### New File: `ViewModels/Controls/SortableResultsGridViewModel.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.ViewModels.Controls
{
    public partial class SortableResultsGridViewModel : ObservableObject
    {
        // Observable collections
        [ObservableProperty]
        private ObservableCollection<SearchResult> _allResults = new();

        [ObservableProperty]
        private ObservableCollection<SearchResult> _displayedResults = new();

        // Pagination properties
        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private int _itemsPerPage = 100;

        // Computed properties (updated automatically)
        [ObservableProperty]
        private string _pageInfoText = "Page 1 of 1";

        [ObservableProperty]
        private string _resultsCountText = "0 results";

        [ObservableProperty]
        private string _statsText = "Ready to search";

        [ObservableProperty]
        private bool _isPreviousEnabled = false;

        [ObservableProperty]
        private bool _isNextEnabled = false;

        // Sorting properties
        [ObservableProperty]
        private string _currentSortColumn = "TotalScore";

        [ObservableProperty]
        private bool _sortDescending = true;

        [ObservableProperty]
        private int _selectedSortIndex = 1; // Default to "Score ↓"

        // Commands
        public IRelayCommand<string> CopySeedCommand { get; }
        public IRelayCommand<SearchResult> SearchSimilarCommand { get; }
        public IRelayCommand<SearchResult> AddToFavoritesCommand { get; }
        public IRelayCommand<SearchResult> ExportSeedCommand { get; }
        public IRelayCommand<SearchResult> AnalyzeCommand { get; }
        public IRelayCommand ExportAllCommand { get; }
        public IRelayCommand PreviousPageCommand { get; }
        public IRelayCommand NextPageCommand { get; }

        // Events (for parent communication)
        public event EventHandler<SearchResult>? SeedCopied;
        public event EventHandler<SearchResult>? SearchSimilarRequested;
        public event EventHandler<SearchResult>? AddToFavoritesRequested;
        public event EventHandler<SearchResult>? ExportSeedRequested;
        public event EventHandler<IEnumerable<SearchResult>>? ExportAllRequested;
        public event EventHandler<SearchResult>? AnalyzeRequested;

        public SortableResultsGridViewModel()
        {
            // Initialize commands
            CopySeedCommand = new RelayCommand<string>(CopySeed);
            SearchSimilarCommand = new RelayCommand<SearchResult>(SearchSimilar);
            AddToFavoritesCommand = new RelayCommand<SearchResult>(AddToFavorites);
            ExportSeedCommand = new RelayCommand<SearchResult>(ExportSeed);
            AnalyzeCommand = new RelayCommand<SearchResult>(Analyze);
            ExportAllCommand = new RelayCommand(ExportAll);
            PreviousPageCommand = new RelayCommand(PreviousPage, () => IsPreviousEnabled);
            NextPageCommand = new RelayCommand(NextPage, () => IsNextEnabled);

            // Listen to property changes for auto-updates
            AllResults.CollectionChanged += (s, e) => UpdateDisplay();
        }

        // Sorting logic
        partial void OnSelectedSortIndexChanged(int value)
        {
            // Map index to sort column and direction
            switch (value)
            {
                case 0: // Seed
                    CurrentSortColumn = "Seed";
                    SortDescending = false;
                    break;
                case 1: // Score ↓
                    CurrentSortColumn = "TotalScore";
                    SortDescending = true;
                    break;
                case 2: // Score ↑
                    CurrentSortColumn = "TotalScore";
                    SortDescending = false;
                    break;
            }
            ApplySorting();
            UpdateDisplay();
        }

        private void ApplySorting()
        {
            var sorted = CurrentSortColumn switch
            {
                "Seed" => SortDescending
                    ? AllResults.OrderByDescending(r => r.Seed)
                    : AllResults.OrderBy(r => r.Seed),
                "TotalScore" => SortDescending
                    ? AllResults.OrderByDescending(r => r.TotalScore)
                    : AllResults.OrderBy(r => r.TotalScore),
                _ => AllResults.OrderByDescending(r => r.TotalScore)
            };

            AllResults = new ObservableCollection<SearchResult>(sorted);
        }

        private void UpdateDisplay()
        {
            // Calculate pagination
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)AllResults.Count / ItemsPerPage));
            CurrentPage = Math.Min(CurrentPage, TotalPages);

            // Get current page items
            var startIndex = (CurrentPage - 1) * ItemsPerPage;
            var pageItems = AllResults.Skip(startIndex).Take(ItemsPerPage);

            // Update displayed results
            DisplayedResults = new ObservableCollection<SearchResult>(pageItems);

            // Update computed properties
            UpdatePageInfo();
            UpdateResultsCount();
            UpdateStats();
            UpdatePaginationButtons();
        }

        private void UpdatePageInfo()
        {
            PageInfoText = $"Page {CurrentPage} of {TotalPages}";
        }

        private void UpdateResultsCount()
        {
            ResultsCountText = AllResults.Count switch
            {
                0 => "No results",
                1 => "1 result",
                _ => $"{AllResults.Count:N0} results"
            };
        }

        private void UpdateStats()
        {
            if (AllResults.Count == 0)
            {
                StatsText = "Ready to search";
                return;
            }

            var highestScore = AllResults.Max(r => r.TotalScore);
            var averageScore = AllResults.Average(r => r.TotalScore);
            StatsText = $"Best: {highestScore} • Avg: {averageScore:F1} • Count: {AllResults.Count}";
        }

        private void UpdatePaginationButtons()
        {
            IsPreviousEnabled = CurrentPage > 1;
            IsNextEnabled = CurrentPage < TotalPages;
        }

        // Command implementations
        private async void CopySeed(string? seed)
        {
            if (string.IsNullOrWhiteSpace(seed)) return;

            await ClipboardService.CopyToClipboardAsync(seed);

            var result = AllResults.FirstOrDefault(r => r.Seed == seed);
            if (result != null)
            {
                SeedCopied?.Invoke(this, result);
            }
        }

        private void SearchSimilar(SearchResult? result)
        {
            if (result == null) return;
            SearchSimilarRequested?.Invoke(this, result);
        }

        private void AddToFavorites(SearchResult? result)
        {
            if (result == null) return;
            AddToFavoritesRequested?.Invoke(this, result);
        }

        private void ExportSeed(SearchResult? result)
        {
            if (result == null) return;
            ExportSeedRequested?.Invoke(this, result);
        }

        private void Analyze(SearchResult? result)
        {
            if (result == null) return;
            AnalyzeRequested?.Invoke(this, result);
        }

        private void ExportAll()
        {
            ExportAllRequested?.Invoke(this, AllResults);
        }

        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                UpdateDisplay();
            }
        }

        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                UpdateDisplay();
            }
        }

        // Public methods for external control
        public void AddResults(IEnumerable<SearchResult> results)
        {
            foreach (var result in results)
            {
                AllResults.Add(result);
            }
        }

        public void AddResult(SearchResult result)
        {
            AllResults.Add(result);
        }

        public void ClearResults()
        {
            AllResults.Clear();
            DisplayedResults.Clear();
            CurrentPage = 1;
        }

        public IEnumerable<SearchResult> GetAllResults() => AllResults.ToList();
        public IEnumerable<SearchResult> GetDisplayedResults() => DisplayedResults.ToList();
    }
}
```

### Updated File: `SortableResultsGrid.axaml`

**Key Changes:**
1. Add `x:DataType` for compiled bindings
2. Replace all `Name=` references with `{Binding}` expressions
3. Replace event handlers with Command bindings
4. Remove all hardcoded `Text=` and `Content=` values

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:BalatroSeedOracle.ViewModels.Controls"
             x:Class="BalatroSeedOracle.Controls.SortableResultsGrid"
             x:DataType="vm:SortableResultsGridViewModel"
             x:CompileBindings="True">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header with sorting controls -->
        <Border Grid.Row="0"
                Background="{StaticResource DarkBackground}"
                BorderBrush="{StaticResource ModalBorder}"
                BorderThickness="0,0,0,2"
                Padding="12">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <StackPanel Grid.Column="0" Orientation="Horizontal" Spacing="15">
                    <TextBlock Text="🔍 SEARCH RESULTS"
                               Foreground="{StaticResource White}"
                               FontFamily="{StaticResource BalatroFont}"
                               FontSize="18"
                               VerticalAlignment="Center"
                               IsTextSelectionEnabled="False"/>

                    <!-- BINDING INSTEAD OF Name + manual update -->
                    <TextBlock Text="{Binding ResultsCountText}"
                               Foreground="{StaticResource Gold}"
                               FontSize="14"
                               VerticalAlignment="Center"
                               IsTextSelectionEnabled="False"/>
                </StackPanel>

                <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="10" VerticalAlignment="Center">
                    <TextBlock Text="Sort by:"
                               Foreground="{StaticResource White}"
                               FontSize="12"
                               VerticalAlignment="Center"
                               IsTextSelectionEnabled="False"/>

                    <!-- BINDING INSTEAD OF SelectionChanged event -->
                    <ComboBox SelectedIndex="{Binding SelectedSortIndex}"
                              Background="{StaticResource DarkBackground}"
                              Foreground="{StaticResource Gold}"
                              BorderBrush="{StaticResource ModalBorder}"
                              MinWidth="160">
                        <ComboBoxItem Content="Seed"/>
                        <ComboBoxItem Content="Score ↓"/>
                        <ComboBoxItem Content="Score ↑"/>
                    </ComboBox>

                    <!-- COMMAND BINDING INSTEAD OF Click event -->
                    <Button Content="📤 Export"
                            Command="{Binding ExportAllCommand}"/>
                </StackPanel>
            </Grid>
        </Border>

        <!-- Main DataGrid -->
        <DataGrid Grid.Row="1"
                  ItemsSource="{Binding DisplayedResults}"
                  AutoGenerateColumns="False"
                  IsReadOnly="True"
                  SelectionMode="Single">
            <!-- Context menu with proper command bindings -->
            <DataGrid.ContextMenu>
                <ContextMenu>
                    <MenuItem Header="📋 Copy seed"
                              Command="{Binding CopySeedCommand}"
                              CommandParameter="{Binding Seed}"/>
                    <MenuItem Header="⭐ Add to favorites"
                              Command="{Binding AddToFavoritesCommand}"
                              CommandParameter="{Binding}"/>
                    <MenuItem Header="📤 Export this seed"
                              Command="{Binding ExportSeedCommand}"
                              CommandParameter="{Binding}"/>
                    <MenuItem Header="🧪 Analyze"
                              Command="{Binding AnalyzeCommand}"
                              CommandParameter="{Binding}"/>
                </ContextMenu>
            </DataGrid.ContextMenu>

            <!-- Columns remain the same but use ViewModel commands -->
            <DataGrid.Columns>
                <!-- ... (same as before) ... -->
            </DataGrid.Columns>
        </DataGrid>

        <!-- Footer with pagination and stats -->
        <Border Grid.Row="2"
                Background="{StaticResource DarkBackground}"
                BorderBrush="{StaticResource ModalBorder}"
                BorderThickness="0,2,0,0"
                Padding="12">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <!-- Stats - BINDING INSTEAD OF manual update -->
                <TextBlock Grid.Column="0"
                           Text="{Binding StatsText}"
                           Foreground="{StaticResource White}"
                           FontSize="12"
                           VerticalAlignment="Center"
                           IsTextSelectionEnabled="False"/>

                <!-- Pagination - BINDINGS AND COMMANDS -->
                <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="8">
                    <Button Content="◀"
                            Command="{Binding PreviousPageCommand}"
                            IsEnabled="{Binding IsPreviousEnabled}"
                            Classes="btn-red"
                            Width="40"
                            Height="32"
                            Padding="0"/>

                    <Button Content="{Binding PageInfoText}"
                            Classes="btn-red"
                            MinWidth="100"
                            Height="32"
                            IsHitTestVisible="False"/>

                    <Button Content="▶"
                            Command="{Binding NextPageCommand}"
                            IsEnabled="{Binding IsNextEnabled}"
                            Classes="btn-red"
                            Width="40"
                            Height="32"
                            Padding="0"/>
                </StackPanel>
            </Grid>
        </Border>
    </Grid>
</UserControl>
```

### Updated File: `SortableResultsGrid.axaml.cs`

**Minimal code-behind - ONLY wiring:**

```csharp
using Avalonia.Controls;
using BalatroSeedOracle.ViewModels.Controls;

namespace BalatroSeedOracle.Controls
{
    public partial class SortableResultsGrid : UserControl
    {
        public SortableResultsGridViewModel ViewModel { get; }

        public SortableResultsGrid()
        {
            ViewModel = new SortableResultsGridViewModel();
            DataContext = ViewModel;
            InitializeComponent();
        }

        // Expose ViewModel events for backward compatibility
        public event EventHandler<SearchResult>? SeedCopied
        {
            add => ViewModel.SeedCopied += value;
            remove => ViewModel.SeedCopied -= value;
        }

        public event EventHandler<SearchResult>? SearchSimilarRequested
        {
            add => ViewModel.SearchSimilarRequested += value;
            remove => ViewModel.SearchSimilarRequested -= value;
        }

        public event EventHandler<SearchResult>? AddToFavoritesRequested
        {
            add => ViewModel.AddToFavoritesRequested += value;
            remove => ViewModel.AddToFavoritesRequested -= value;
        }

        public event EventHandler<SearchResult>? ExportSeedRequested
        {
            add => ViewModel.ExportSeedRequested += value;
            remove => ViewModel.ExportSeedRequested -= value;
        }

        public event EventHandler<IEnumerable<SearchResult>>? ExportAllRequested
        {
            add => ViewModel.ExportAllRequested += value;
            remove => ViewModel.ExportAllRequested -= value;
        }

        public event EventHandler<SearchResult>? AnalyzeRequested
        {
            add => ViewModel.AnalyzeRequested += value;
            remove => ViewModel.AnalyzeRequested -= value;
        }

        // Expose ViewModel methods for backward compatibility
        public void AddResults(IEnumerable<SearchResult> results) => ViewModel.AddResults(results);
        public void AddResult(SearchResult result) => ViewModel.AddResult(result);
        public void ClearResults() => ViewModel.ClearResults();
        public IEnumerable<SearchResult> GetAllResults() => ViewModel.GetAllResults();
        public IEnumerable<SearchResult> GetDisplayedResults() => ViewModel.GetDisplayedResults();
    }
}
```

## Implementation Checklist

### Step 1: Create ViewModel
- [ ] Create `ViewModels/Controls/SortableResultsGridViewModel.cs`
- [ ] Add all observable properties using `[ObservableProperty]`
- [ ] Implement all commands using `IRelayCommand`
- [ ] Move business logic from code-behind to ViewModel
- [ ] Add property change handlers for auto-updates

### Step 2: Update XAML
- [ ] Add `x:DataType="vm:SortableResultsGridViewModel"`
- [ ] Add `x:CompileBindings="True"`
- [ ] Replace `Name="ResultsCountText"` with `Text="{Binding ResultsCountText}"`
- [ ] Replace `Name="PageInfoBadge"` with `Content="{Binding PageInfoText}"`
- [ ] Replace `Name="StatsText"` with `Text="{Binding StatsText}"`
- [ ] Replace `SelectionChanged="SortComboBox_SelectionChanged"` with `SelectedIndex="{Binding SelectedSortIndex}"`
- [ ] Replace `Click="ExportButton_Click"` with `Command="{Binding ExportAllCommand}"`
- [ ] Replace `Click="PreviousButton_Click"` with `Command="{Binding PreviousPageCommand}"`
- [ ] Replace `Click="NextButton_Click"` with `Command="{Binding NextPageCommand}"`
- [ ] Replace `IsEnabled="False"` with `IsEnabled="{Binding IsPreviousEnabled}"` and `IsEnabled="{Binding IsNextEnabled}"`
- [ ] Add `IsTextSelectionEnabled="False"` to all TextBlocks

### Step 3: Minimal Code-Behind
- [ ] Delete all business logic methods
- [ ] Delete all private fields
- [ ] Keep ONLY:
  - Constructor that creates ViewModel
  - Event wrappers for backward compatibility
  - Public method wrappers for backward compatibility

### Step 4: Handle Tally Columns
- [ ] Move `EnsureTallyColumns()` logic to ViewModel or use ItemTemplate
- [ ] Consider using DataTemplate with converter for dynamic columns
- [ ] Remove `_tallyColumnsInitialized` flag

### Step 5: Testing
- [ ] Verify pagination works
- [ ] Verify sorting works
- [ ] Verify all commands work
- [ ] Verify results count updates automatically
- [ ] Verify stats update automatically
- [ ] Verify page info updates automatically
- [ ] Verify button enable/disable states work

## Benefits of This Refactor

1. **Testable**: ViewModel can be unit tested without UI
2. **Maintainable**: Business logic in one place (ViewModel)
3. **Reactive**: UI updates automatically when properties change
4. **Reusable**: ViewModel can be used with different views
5. **Debuggable**: Easier to trace property changes
6. **Type-safe**: Compiled bindings catch errors at compile time
7. **Performant**: No FindControl lookups, direct property bindings

## Compatibility

The refactored control will maintain 100% backward compatibility by:
1. Exposing same public methods (forwarding to ViewModel)
2. Exposing same events (forwarding to ViewModel)
3. Maintaining same API surface

Existing code using `SortableResultsGrid` will continue to work without changes.

## Timeline

Estimated effort: **2-3 hours**
1. Create ViewModel: 1 hour
2. Update XAML: 30 minutes
3. Refactor code-behind: 30 minutes
4. Testing: 1 hour

---

**End of PRD**
