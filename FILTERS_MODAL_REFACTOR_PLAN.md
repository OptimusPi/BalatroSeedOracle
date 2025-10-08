# FiltersModal MVVM Refactor Plan

## THE SMOKING GUN: FiltersModal.axaml.cs has 8,727 lines but DOESN'T USE its ViewModel!

### Current State Analysis

**FiltersModalViewModel.cs (822 lines) - EXISTS BUT UNUSED!**
- Already has proper structure with commands and properties
- SaveCommand, LoadCommand, DeleteCommand all defined
- ObservableCollections for SelectedMust, SelectedShould, SelectedMustNot
- **BUT: FiltersModal.axaml.cs never sets it as DataContext!**

**FiltersModal.axaml.cs (8,727 lines) - MASSIVE ANTI-PATTERN**
- 143 private methods doing business logic in code-behind
- Duplicates ALL state that exists in ViewModel:
  - `_selectedMust` (line 54) - DUPLICATE of ViewModel.SelectedMust
  - `_selectedShould` (line 55) - DUPLICATE of ViewModel.SelectedShould
  - `_selectedMustNot` (line 56) - DUPLICATE of ViewModel.SelectedMustNot
  - `_itemConfigs` (line 63) - DUPLICATE of ViewModel.ItemConfigs
- NO DataContext = ViewModel assignment
- NO data binding to ViewModel properties

**SearchModal.axaml.cs (148 lines) - THE GOLD STANDARD**
```csharp
public SearchModal()
{
    var searchManager = ServiceHelper.GetRequiredService<SearchManager>();
    ViewModel = new SearchModalViewModel(searchManager);
    DataContext = ViewModel;  // CRITICAL!

    ViewModel.CloseRequested += (s, e) => CloseRequested?.Invoke(this, e);
    InitializeComponent();
    WireUpComponentEvents();  // ONLY event adapters
}
```

---

## Migration Steps (Execute in Order)

### PHASE 1: Make ViewModel Proper MVVM (2-3 hours)

#### Step 1.1: Convert FiltersModalViewModel to partial class with source generators

**File: `x:\BalatroSeedOracle\src\ViewModels\FiltersModalViewModel.cs`**

**Changes:**
```csharp
// LINE 19: Change class declaration
// BEFORE:
public class FiltersModalViewModel : BaseViewModel

// AFTER:
public partial class FiltersModalViewModel : ObservableObject
```

#### Step 1.2: Convert manual properties to [ObservableProperty]

**Replace lines 65-112 with:**
```csharp
[ObservableProperty]
private string _currentCategory = "Jokers";

[ObservableProperty]
private string _searchFilter = "";

[ObservableProperty]
private int _selectedTabIndex = 0;

[ObservableProperty]
private string? _currentFilterPath;

[ObservableProperty]
private Motely.Filters.MotelyJsonConfig? _loadedConfig;

[ObservableProperty]
private string _filterName = "";

[ObservableProperty]
private string _filterDescription = "";

[ObservableProperty]
private string _selectedDeck = "Red";

[ObservableProperty]
private int _selectedStake = 0;

// Remove manual property implementations - source generator creates them!
```

#### Step 1.3: Convert commands to [RelayCommand]

**Replace lines 167-173 (command declarations) with:**
```csharp
// DELETE: public ICommand SaveCommand { get; }
// DELETE: constructor initialization

// INSTEAD: Add [RelayCommand] to methods
[RelayCommand]
private async Task SaveCurrentFilter()
{
    // Existing logic from SaveCurrentFilterAsync (line 178)
}

[RelayCommand]
private async Task LoadFilter()
{
    // Existing logic from LoadFilterAsync (line 282)
}

[RelayCommand]
private async Task CreateNewFilter()
{
    // Existing logic from CreateNewFilterAsync (line 297)
}

[RelayCommand]
private async Task DeleteCurrentFilter()
{
    // Existing logic from DeleteCurrentFilterAsync (line 314)
}

[RelayCommand]
private void RefreshFromConfig()
{
    // Existing logic (line 334)
}

[RelayCommand]
private async Task ReloadVisualFromSavedFile()
{
    // Existing logic (line 350)
}
```

#### Step 1.4: Add missing ViewModel methods

**Add to FiltersModalViewModel.cs (after line 821):**

```csharp
// Tab switching
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(IsVisualTabVisible), nameof(IsJsonTabVisible), nameof(IsTestTabVisible), nameof(IsSaveTabVisible))]
private int _activeTabIndex = 0;

public bool IsLoadTabVisible => ActiveTabIndex == 0;
public bool IsVisualTabVisible => ActiveTabIndex == 1;
public bool IsJsonTabVisible => ActiveTabIndex == 2;
public bool IsTestTabVisible => ActiveTabIndex == 3;
public bool IsSaveTabVisible => ActiveTabIndex == 4;

[RelayCommand]
private void SwitchToTab(int tabIndex)
{
    ActiveTabIndex = tabIndex;
}

// JSON editor
[ObservableProperty]
private string _jsonEditorText = "";

[ObservableProperty]
[NotifyPropertyChangedFor(nameof(JsonValidationColor))]
private string _jsonValidationMessage = "✓ Valid JSON";

[ObservableProperty]
[NotifyPropertyChangedFor(nameof(JsonValidationColor))]
private bool _isJsonValid = true;

public string JsonValidationColor => IsJsonValid ? "Green" : "Red";

[RelayCommand]
private void FormatJson()
{
    try
    {
        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var config = System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(JsonEditorText);
        if (config != null)
        {
            JsonEditorText = System.Text.Json.JsonSerializer.Serialize(config, options);
            JsonValidationMessage = "✓ Valid JSON - Formatted!";
            IsJsonValid = true;
        }
    }
    catch (Exception ex)
    {
        JsonValidationMessage = $"✗ Format failed: {ex.Message}";
        IsJsonValid = false;
    }
}

[RelayCommand]
private void ValidateJson()
{
    try
    {
        var config = System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(JsonEditorText);
        if (config != null)
        {
            var errors = Motely.Filters.MotelyJsonConfigValidator.Validate(config);
            if (errors.Any())
            {
                JsonValidationMessage = $"✗ Validation errors: {string.Join(", ", errors)}";
                IsJsonValid = false;
            }
            else
            {
                JsonValidationMessage = "✓ Valid JSON!";
                IsJsonValid = true;
            }
        }
    }
    catch (Exception ex)
    {
        JsonValidationMessage = $"✗ Invalid: {ex.Message}";
        IsJsonValid = false;
    }
}

[RelayCommand]
private async Task SaveJson()
{
    try
    {
        ValidateJson();
        if (!IsJsonValid) return;

        var config = System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(JsonEditorText);
        if (config != null)
        {
            LoadedConfig = config;
            LoadConfigIntoState(config);
            await SaveCurrentFilter();
        }
    }
    catch (Exception ex)
    {
        DebugLogger.LogError("FiltersModalViewModel", $"Save JSON failed: {ex.Message}");
    }
}

// Drag/drop handling
[RelayCommand]
private void HandleDrop((string zone, object data) args)
{
    var (zone, dragData) = args;

    // Extract item info from drag data
    if (dragData is not Dictionary<string, object> data) return;
    if (!data.TryGetValue("ItemName", out var itemNameObj)) return;
    if (!data.TryGetValue("Category", out var categoryObj)) return;

    var itemName = itemNameObj?.ToString() ?? "";
    var category = categoryObj?.ToString() ?? "";

    if (string.IsNullOrEmpty(itemName) || string.IsNullOrEmpty(category)) return;

    // Create unique key and config
    var itemKey = GenerateNextItemKey();
    var config = new ItemConfig
    {
        ItemKey = itemKey,
        ItemName = itemName,
        ItemType = category
    };

    ItemConfigs[itemKey] = config;

    // Add to appropriate collection
    if (zone == "must") SelectedMust.Add(itemKey);
    else if (zone == "should") SelectedShould.Add(itemKey);
    else if (zone == "mustnot") SelectedMustNot.Add(itemKey);

    DebugLogger.Log("FiltersModalViewModel", $"Added {itemName} to {zone}");
}

[RelayCommand]
private void RemoveItem((string zone, string itemKey) args)
{
    var (zone, itemKey) = args;

    if (zone == "must") SelectedMust.Remove(itemKey);
    else if (zone == "should") SelectedShould.Remove(itemKey);
    else if (zone == "mustnot") SelectedMustNot.Remove(itemKey);

    ItemConfigs.Remove(itemKey);
}

// Quick test
[ObservableProperty]
private ObservableCollection<TestResult> _testResults = new();

[ObservableProperty]
private int _testResultCount = 0;

[RelayCommand]
private async Task QuickTest(int seedCount = 100)
{
    try
    {
        TestResults.Clear();
        TestResultCount = 0;

        var config = BuildConfigFromCurrentState();

        // TODO: Integrate with Motely search for quick test
        // For now, just validate the config
        var errors = Motely.Filters.MotelyJsonConfigValidator.Validate(config);
        if (errors.Any())
        {
            DebugLogger.LogError("FiltersModalViewModel", $"Config validation failed: {string.Join(", ", errors)}");
            return;
        }

        DebugLogger.Log("FiltersModalViewModel", $"Quick test started for {seedCount} seeds");

        // Placeholder - actual implementation would run Motely search
        await Task.Delay(1000);

        DebugLogger.Log("FiltersModalViewModel", "Quick test completed");
    }
    catch (Exception ex)
    {
        DebugLogger.LogError("FiltersModalViewModel", $"Quick test failed: {ex.Message}");
    }
}

// Test result model
public class TestResult
{
    public string Seed { get; set; } = "";
    public int Score { get; set; }
    public string Details { get; set; } = "";
}
```

---

### PHASE 2: Wire Up ViewModel in Code-Behind (1 hour)

#### Step 2.1: Minimal FiltersModal.axaml.cs

**File: `x:\BalatroSeedOracle\src\Views\Modals\FiltersModal.axaml.cs`**

**REPLACE ENTIRE FILE with:**

```csharp
using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using Avalonia.Interactivity;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Components;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class FiltersModalContent : UserControl
    {
        public FiltersModalViewModel ViewModel { get; }

        public FiltersModalContent()
        {
            // Get services from DI
            var configService = ServiceHelper.GetRequiredService<IConfigurationService>();
            var filterService = ServiceHelper.GetRequiredService<IFilterService>();

            // Create and set ViewModel
            ViewModel = new FiltersModalViewModel(configService, filterService);
            DataContext = ViewModel;

            InitializeComponent();

            // Initialize tabs on UI thread
            ViewModel.InitializeTabs();

            // Wire up component events
            WireUpComponentEvents();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Wire up component events to ViewModel commands
        /// PROPER MVVM: Components communicate via events, we forward to ViewModel
        /// </summary>
        private void WireUpComponentEvents()
        {
            // FilterSelectorControl -> ViewModel.LoadFilterCommand
            var filterSelector = this.FindControl<FilterSelectorControl>("FilterSelector");
            if (filterSelector != null)
            {
                filterSelector.FilterSelected += (s, path) =>
                {
                    ViewModel.LoadFilterCommand.Execute(path);
                };

                filterSelector.FilterCopyRequested += (s, path) =>
                {
                    // TODO: Implement copy logic in ViewModel
                };

                filterSelector.NewFilterRequested += (s, e) =>
                {
                    ViewModel.CreateNewFilterCommand.Execute(null);
                };
            }

            // DeckAndStakeSelector -> ViewModel properties
            var deckStakeSelector = this.FindControl<Components.DeckAndStakeSelector>("PreferredDeckStakeSelector");
            if (deckStakeSelector != null)
            {
                deckStakeSelector.SelectionChanged += (s, selection) =>
                {
                    var deckNames = new[] { "Red", "Blue", "Yellow", "Green", "Black", "Magic", "Nebula", "Ghost",
                                           "Abandoned", "Checkered", "Zodiac", "Painted", "Anaglyph", "Plasma", "Erratic" };

                    if (selection.deckIndex >= 0 && selection.deckIndex < deckNames.Length)
                        ViewModel.SelectedDeck = deckNames[selection.deckIndex];

                    ViewModel.SelectedStake = selection.stakeIndex;
                };
            }

            // Setup drag/drop handlers
            SetupDragDropHandlers();
        }

        /// <summary>
        /// Setup drag/drop event handlers for visual feedback only
        /// Business logic handled by ViewModel
        /// </summary>
        private void SetupDragDropHandlers()
        {
            // MUST zone
            var needsPanel = this.FindControl<Avalonia.Controls.WrapPanel>("NeedsPanel");
            var needsBorder = this.FindControl<Border>("NeedsBorder");
            if (needsPanel != null && needsBorder != null)
            {
                needsBorder.DragEnter += (s, e) => needsBorder.Classes.Add("drag-over");
                needsBorder.DragLeave += (s, e) => needsBorder.Classes.Remove("drag-over");
                needsPanel.Drop += (s, e) =>
                {
                    needsBorder.Classes.Remove("drag-over");
                    ViewModel.HandleDropCommand.Execute(("must", e.Data));
                };
            }

            // SHOULD zone
            var wantsPanel = this.FindControl<Avalonia.Controls.WrapPanel>("WantsPanel");
            var wantsBorder = this.FindControl<Border>("WantsBorder");
            if (wantsPanel != null && wantsBorder != null)
            {
                wantsBorder.DragEnter += (s, e) => wantsBorder.Classes.Add("drag-over");
                wantsBorder.DragLeave += (s, e) => wantsBorder.Classes.Remove("drag-over");
                wantsPanel.Drop += (s, e) =>
                {
                    wantsBorder.Classes.Remove("drag-over");
                    ViewModel.HandleDropCommand.Execute(("should", e.Data));
                };
            }

            // MUST NOT zone
            var mustNotPanel = this.FindControl<Avalonia.Controls.WrapPanel>("MustNotPanel");
            var mustNotBorder = this.FindControl<Border>("MustNotBorder");
            if (mustNotPanel != null && mustNotBorder != null)
            {
                mustNotBorder.DragEnter += (s, e) => mustNotBorder.Classes.Add("drag-over");
                mustNotBorder.DragLeave += (s, e) => mustNotBorder.Classes.Remove("drag-over");
                mustNotPanel.Drop += (s, e) =>
                {
                    mustNotBorder.Classes.Remove("drag-over");
                    ViewModel.HandleDropCommand.Execute(("mustnot", e.Data));
                };
            }
        }

        /// <summary>
        /// Tab switching - updates ViewModel state and triangle animation
        /// </summary>
        private void OnTabClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var tabIndex = button.Name switch
            {
                "LoadSaveTab" => 0,
                "VisualTab" => 1,
                "JsonTab" => 2,
                "TestTab" => 3,
                "SaveFilterTab" => 4,
                _ => 0
            };

            ViewModel.SwitchToTabCommand.Execute(tabIndex);
            UpdateTrianglePosition(tabIndex);
        }

        /// <summary>
        /// Animate triangle indicator to active tab
        /// </summary>
        private void UpdateTrianglePosition(int tabIndex)
        {
            var triangle = this.FindControl<Avalonia.Controls.Shapes.Polygon>("TabTriangle");
            if (triangle?.Parent is Grid triangleGrid)
            {
                Grid.SetColumn(triangleGrid, tabIndex);
            }
        }

        // JSON Editor button handlers (forward to ViewModel)
        private void OnFormatJsonClick(object? sender, RoutedEventArgs e) =>
            ViewModel.FormatJsonCommand.Execute(null);

        private void OnValidateJsonClick(object? sender, RoutedEventArgs e) =>
            ViewModel.ValidateJsonCommand.Execute(null);

        private void OnSaveJsonClick(object? sender, RoutedEventArgs e) =>
            ViewModel.SaveJsonCommand.Execute(null);

        // Test panel handlers
        private void OnQuickTestClick(object? sender, RoutedEventArgs e) =>
            ViewModel.QuickTestCommand.Execute(100);

        private void OnOpenFullSearchClick(object? sender, RoutedEventArgs e)
        {
            // TODO: Open SearchModal with current filter
            DebugLogger.Log("FiltersModal", "Open full search requested");
        }

        // Save panel handlers
        private void OnSaveChangesClick(object? sender, RoutedEventArgs e) =>
            ViewModel.SaveCurrentFilterCommand.Execute(null);

        private void OnSearchClick(object? sender, RoutedEventArgs e)
        {
            // TODO: Open SearchModal
            DebugLogger.Log("FiltersModal", "Search requested");
        }

        private void OnExportClick(object? sender, RoutedEventArgs e)
        {
            // TODO: Export filter
            DebugLogger.Log("FiltersModal", "Export requested");
        }

        // FilterSelector event handlers
        private void OnFilterSelected(object? sender, string filterPath) =>
            ViewModel.LoadFilterCommand.Execute(filterPath);

        private void OnFilterCopyRequested(object? sender, string filterPath)
        {
            // TODO: Implement copy in ViewModel
            DebugLogger.Log("FiltersModal", $"Copy requested: {filterPath}");
        }

        private void OnNewFilterRequested(object? sender, EventArgs e) =>
            ViewModel.CreateNewFilterCommand.Execute(null);
    }
}
```

**Code-behind size: ~150 lines (vs 8,727 before = 98.3% reduction!)**

---

### PHASE 3: Update XAML for Compiled Bindings (1 hour)

#### Step 3.1: Add x:DataType and x:CompileBindings

**File: `x:\BalatroSeedOracle\src\Views\Modals\FiltersModal.axaml`**

**Line 1-7: Update UserControl declaration**
```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:avalonedit="https://github.com/avaloniaui/avaloniaedit"
             xmlns:controls="using:BalatroSeedOracle.Controls"
             xmlns:components="using:BalatroSeedOracle.Components"
             xmlns:vm="using:BalatroSeedOracle.ViewModels"
             x:Class="BalatroSeedOracle.Views.Modals.FiltersModalContent"
             x:DataType="vm:FiltersModalViewModel"
             x:CompileBindings="True">
```

#### Step 3.2: Add bindings to key controls

**Search Box (line 413-421):**
```xml
<TextBox Grid.Column="0"
         Name="SearchBox"
         Text="{Binding SearchFilter}"
         Watermark="Search items..."
         Height="28"
         VerticalContentAlignment="Center"/>
```

**Filter Name Input (line 1000-1005):**
```xml
<TextBox Grid.Column="2"
         Name="SaveFilterNameInput"
         Text="{Binding FilterName}"
         Watermark="Enter filter name..."
         FontSize="14"
         Height="32"
         VerticalContentAlignment="Center"/>
```

**Filter Description (line 1016-1023):**
```xml
<TextBox Grid.Row="8"
         Name="FilterDescriptionInput"
         Text="{Binding FilterDescription}"
         AcceptsReturn="True"
         TextWrapping="Wrap"
         Watermark="Enter filter description..."
         FontSize="14"
         VerticalContentAlignment="Top"
         Padding="8"/>
```

**JSON Editor (line 580-586):**
```xml
<avalonedit:TextEditor Name="JsonEditor"
                       Text="{Binding JsonEditorText}"
                       SyntaxHighlighting="Json"
                       ShowLineNumbers="True"
                       FontFamily="Cascadia Code, Consolas, monospace"
                       FontSize="14"
                       Padding="8"/>
```

**JSON Validation Status (line 605-610):**
```xml
<TextBlock Grid.Column="0"
           Name="JsonValidationStatus"
           Text="{Binding JsonValidationMessage}"
           FontFamily="{StaticResource BalatroFont}"
           FontSize="13"
           Foreground="{Binding JsonValidationColor}"
           VerticalAlignment="Center"/>
```

**Save Changes Button (line 1061-1067):**
```xml
<Button Grid.Column="0"
        Name="SaveChangesButton"
        Content="Save Changes"
        Command="{Binding SaveCurrentFilterCommand}"
        Classes="btn-green"
        Height="40"
        FontSize="16"/>
```

---

### PHASE 4: Testing and Validation (4 hours)

#### Test Checklist

**Filter Creation:**
- [ ] Create new filter
- [ ] Enter name and description
- [ ] Select deck and stake
- [ ] Add items to MUST zone
- [ ] Add items to SHOULD zone
- [ ] Add items to MUST NOT zone
- [ ] Save filter
- [ ] Verify JSON file created

**Filter Loading:**
- [ ] Load existing filter from list
- [ ] Verify name/description populated
- [ ] Verify deck/stake selection
- [ ] Verify items in drop zones
- [ ] Switch to JSON tab - verify JSON matches
- [ ] Edit JSON - verify visual updates

**Drag and Drop:**
- [ ] Drag item from palette to MUST zone
- [ ] Drag item to SHOULD zone
- [ ] Drag item to MUST NOT zone
- [ ] Remove item from zone
- [ ] Clear all zones

**JSON Editor:**
- [ ] Format JSON (Ctrl+Shift+F)
- [ ] Validate JSON
- [ ] Edit JSON manually
- [ ] Save JSON changes
- [ ] Verify visual builder updates

**Quick Test:**
- [ ] Run quick test (100 seeds)
- [ ] View results
- [ ] Open full search

---

## Benefits of This Refactor

### Code Quality
- **8,199 lines removed** (86% reduction)
- **Compile-time safety** for all bindings
- **Testable** ViewModel (no Avalonia dependencies)
- **Maintainable** code (clear separation of concerns)

### Performance
- **Compiled bindings** faster than reflection-based
- **Less code** = faster load times
- **HashSet optimizations** preserved in ViewModel

### Developer Experience
- **IntelliSense** in XAML for all bindings
- **Compile errors** catch typos immediately
- **Clear pattern** - easy to understand and extend
- **Follows SearchModal** - consistent architecture

---

## Migration Timeline

**Total Estimated Time: 8-12 hours**

| Phase | Time | Description |
|-------|------|-------------|
| Phase 1 | 2-3 hours | Convert ViewModel to partial class with source generators |
| Phase 2 | 1 hour | Wire up ViewModel in code-behind |
| Phase 3 | 1 hour | Update XAML with compiled bindings |
| Phase 4 | 4 hours | Testing and bug fixes |
| Buffer | 2-4 hours | Unexpected issues |

**Recommended Approach:**
1. Do Phase 1 in one sitting (don't break it up)
2. Test after each phase (don't stack changes)
3. Keep SearchModal open as reference (copy its patterns)
4. If stuck, compare with SearchModal side-by-side

---

## Success Criteria

- [ ] FiltersModal.axaml.cs is < 200 lines
- [ ] All business logic in FiltersModalViewModel
- [ ] All XAML bindings compile-time checked
- [ ] All existing functionality works
- [ ] No FindControl() calls except in WireUpComponentEvents()
- [ ] No state duplication between code-behind and ViewModel
- [ ] Build has ZERO warnings

---

## Common Pitfalls to Avoid

### ❌ DON'T: Keep state in code-behind
```csharp
// WRONG - code-behind
private List<string> _selectedMust = new();  // DUPLICATE!
```

### ✅ DO: Use ViewModel state
```csharp
// RIGHT - ViewModel only
ViewModel.SelectedMust.Add(itemKey);
```

### ❌ DON'T: Business logic in code-behind
```csharp
// WRONG - code-behind
private void OnSaveClick(object? sender, RoutedEventArgs e)
{
    var config = BuildConfig();  // NOPE!
    SaveToFile(config);           // NOPE!
}
```

### ✅ DO: Forward to ViewModel command
```csharp
// RIGHT - code-behind
private void OnSaveClick(object? sender, RoutedEventArgs e) =>
    ViewModel.SaveCurrentFilterCommand.Execute(null);
```

### ❌ DON'T: Create UI controls in ViewModel
```csharp
// WRONG - ViewModel
var button = new Button { Content = "Click" };  // NOPE!
```

### ✅ DO: Use data binding with ViewModels
```csharp
// RIGHT - ViewModel
[ObservableProperty]
private string _buttonText = "Click";

// XAML
<Button Content="{Binding ButtonText}" Command="{Binding ClickCommand}"/>
```

---

## Reference: SearchModal Pattern

**SearchModal follows perfect MVVM:**

1. **Constructor**: Get services, create ViewModel, set DataContext
2. **WireUpComponentEvents()**: ONLY event adapters
3. **Code-behind**: < 150 lines, no business logic
4. **ViewModel**: ALL business logic, commands, state
5. **XAML**: x:DataType + x:CompileBindings

**Copy this pattern EXACTLY for FiltersModal!**
