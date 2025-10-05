# 🎯 Avalonia MVVM 2025 Best Practices

## The SIMPLE, MODERN Way (No Over-Engineering!)

This guide shows you the **2025 standard** for Avalonia MVVM using `CommunityToolkit.Mvvm` source generators.

**Philosophy:** Write LESS code, get MORE safety, keep it SIMPLE.

---

## ✅ **WHAT'S ALREADY PERFECT IN YOUR CODEBASE**

### BaseWidgetViewModel.cs - **THE GOLD STANDARD!**

```csharp
public partial class BaseWidgetViewModel : ObservableObject  // ✅ partial + ObservableObject
{
    [ObservableProperty]  // ✅ Source generator
    private bool _isMinimized = true;

    [ObservableProperty]
    private string _widgetTitle = "Widget";

    [RelayCommand]        // ✅ Source generator
    private void Expand()
    {
        IsMinimized = false;
        OnExpanded();
    }
}
```

**Why this is PERFECT:**
- Uses `ObservableObject` base (from CommunityToolkit.Mvvm)
- Uses `[ObservableProperty]` - source generator creates the public property
- Uses `[RelayCommand]` - source generator creates the command
- **Result:** 80% less boilerplate!

---

## 🔧 **THE MIGRATION CHECKLIST**

Your codebase is 80% modern already! Here's what to finish:

### ✅ Step 1: Add `partial` Keyword

**FILES TO FIX:**
- `DayLatroWidgetViewModel.cs`
- `MainWindowViewModel.cs`
- `FilterSelectorViewModel.cs`
- `SearchModalViewModel.cs`
- ALL ViewModels except `BaseWidgetViewModel` (already correct!)

**CHANGE:**
```csharp
// BEFORE:
public class DayLatroWidgetViewModel : BaseWidgetViewModel

// AFTER:
public partial class DayLatroWidgetViewModel : BaseWidgetViewModel
//     ↑↑↑↑↑↑ ADD THIS!
```

**WHY:** Source generators need `partial` to inject generated code.

---

### ✅ Step 2: Convert Manual Properties

**BEFORE (Current Pattern):**
```csharp
private string _todaySeed = "LOADING...";
public string TodaySeed
{
    get => _todaySeed;
    set => SetProperty(ref _todaySeed, value);
}
```

**AFTER (2025 Way):**
```csharp
[ObservableProperty]
private string _todaySeed = "LOADING...";

// That's it! Public property auto-generated!
```

**Bonus - Property Change Notifications:**
```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(FullName))]  // Also notify FullName
private string _firstName;

[ObservableProperty]
[NotifyPropertyChangedFor(nameof(FullName))]
private string _lastName;

public string FullName => $"{FirstName} {LastName}";
```

**Bonus - Command State Updates:**
```csharp
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(SubmitScoreCommand))]  // Update command!
private string _scoreInput = string.Empty;

[RelayCommand(CanExecute = nameof(CanSubmitScore))]
private void SubmitScore() { }

private bool CanSubmitScore() => !string.IsNullOrWhiteSpace(ScoreInput);
```

---

### ✅ Step 3: Convert Manual Commands

**BEFORE (Current Pattern):**
```csharp
public ICommand SubmitScoreCommand { get; }

public DayLatroWidgetViewModel(...)
{
    SubmitScoreCommand = new RelayCommand(OnSubmitScore, CanSubmitScore);
}

private void OnSubmitScore() { /* logic */ }
private bool CanSubmitScore() { /* validation */ }
```

**AFTER (2025 Way):**
```csharp
[RelayCommand(CanExecute = nameof(CanSubmitScore))]
private void SubmitScore()  // Note: Removed "On" prefix!
{
    // Logic here
}

private bool CanSubmitScore() => !string.IsNullOrWhiteSpace(ScoreInput);

// SubmitScoreCommand is auto-generated!
```

**Async Commands:**
```csharp
[RelayCommand]
private async Task RefreshScores()
{
    await FetchAndDisplayHighScoresAsync(forceRefresh: true);
}
// RefreshScoresCommand (AsyncRelayCommand) is auto-generated!
```

---

### ✅ Step 4: Enable Compiled Bindings

**Add to EVERY UserControl/Window:**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:BalatroSeedOracle.ViewModels"
             x:Class="BalatroSeedOracle.Views.MyView"
             x:DataType="vm:MyViewModel"       ← ADD THIS!
             x:CompileBindings="True">         ← ADD THIS!

    <TextBlock Text="{Binding TodaySeed}"/>   ← Now compile-time checked!
</UserControl>
```

**Benefits:**
- **Compile-time errors** for typos (no more runtime crashes!)
- **IntelliSense** in XAML (autocomplete FTW!)
- **Better performance** (no reflection)

**DataTemplates:**
```xml
<ItemsControl ItemsSource="{Binding HighScores}">
    <ItemsControl.ItemTemplate>
        <DataTemplate DataType="vm:HighScoreDisplayItem">  ← Type it!
            <Grid>
                <TextBlock Text="{Binding Rank}"/>  ← IntelliSense works!
                <TextBlock Text="{Binding Score}"/>
            </Grid>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

---

## 🚫 **ANTI-PATTERNS TO AVOID**

### ❌ 1. Forgetting `partial` Keyword

```csharp
// ❌ WRONG:
public class MyViewModel : ObservableObject
{
    [ObservableProperty]  // Won't work!
    private string _name;
}

// ✅ CORRECT:
public partial class MyViewModel : ObservableObject
//     ↑↑↑↑↑↑
{
    [ObservableProperty]  // Works!
    private string _name;
}
```

---

### ❌ 2. Wrong Field Naming

```csharp
// ❌ WRONG:
[ObservableProperty]
private string name;  // No underscore = conflict!

// ✅ CORRECT:
[ObservableProperty]
private string _name;  // Generates public "Name" property
```

---

### ❌ 3. Manual NotifyCanExecuteChanged Calls

```csharp
// ❌ WRONG:
public string ScoreInput
{
    get => _scoreInput;
    set
    {
        if (SetProperty(ref _scoreInput, value))
        {
            ((RelayCommand)SubmitScoreCommand).NotifyCanExecuteChanged();  // Ugly!
        }
    }
}

// ✅ CORRECT:
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(SubmitScoreCommand))]  // Automatic!
private string _scoreInput = string.Empty;
```

---

### ❌ 4. Business Logic in Code-Behind

```csharp
// ❌ WRONG (MyView.axaml.cs):
private async void OnButtonClick(object sender, RoutedEventArgs e)
{
    var data = await _apiService.GetDataAsync();  // NOPE!
    ProcessData(data);                             // NOPE!
}

// ✅ CORRECT (MyViewModel.cs):
[RelayCommand]
private async Task LoadData()
{
    var data = await _apiService.GetDataAsync();  // YES!
    ProcessData(data);                             // Testable!
}

// XAML:
<Button Command="{Binding LoadDataCommand}" Content="Load"/>
```

---

## 🎯 **PRACTICAL EXAMPLE: DayLatroWidgetViewModel Refactor**

### BEFORE (Current - 250 lines):

```csharp
public class DayLatroWidgetViewModel : BaseWidgetViewModel, IDisposable
{
    // Manual properties
    private string _todaySeed = "LOADING...";
    public string TodaySeed
    {
        get => _todaySeed;
        set => SetProperty(ref _todaySeed, value);
    }

    private string _scoreInput = string.Empty;
    public string ScoreInput
    {
        get => _scoreInput;
        set
        {
            if (SetProperty(ref _scoreInput, value))
            {
                ((RelayCommand)SubmitScoreCommand).NotifyCanExecuteChanged();
            }
        }
    }

    // ... 10 more manual properties ...

    // Manual commands
    public ICommand SubmitScoreCommand { get; }
    public ICommand CopySeedCommand { get; }
    public ICommand RefreshScoresCommand { get; }
    public ICommand AnalyzeSeedCommand { get; }

    public DayLatroWidgetViewModel(...)
    {
        SubmitScoreCommand = new RelayCommand(OnSubmitScore, CanSubmitScore);
        CopySeedCommand = new AsyncRelayCommand(OnCopySeed);
        RefreshScoresCommand = new AsyncRelayCommand(OnRefreshScores);
        AnalyzeSeedCommand = new RelayCommand(OnAnalyzeSeed);
    }

    private void OnSubmitScore() { /* ... */ }
    private bool CanSubmitScore() { /* ... */ }
    private async Task OnCopySeed() { /* ... */ }
    private async Task OnRefreshScores() { /* ... */ }
    private void OnAnalyzeSeed() { /* ... */ }
}
```

### AFTER (2025 - 120 lines):

```csharp
public partial class DayLatroWidgetViewModel : BaseWidgetViewModel, IDisposable
//     ↑↑↑↑↑↑ Added partial
{
    // Source generator properties (10 properties = 10 lines instead of 80!)
    [ObservableProperty]
    private string _todaySeed = "LOADING...";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitScoreCommand))]
    private string _scoreInput = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitScoreCommand))]
    private string _initialsInput = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitScoreCommand))]
    private string _anteInput = string.Empty;

    [ObservableProperty]
    private ObservableCollection<HighScoreDisplayItem> _highScores = new();

    // ... 5 more properties ...

    // Source generator commands (4 commands = 4 methods instead of 12!)
    [RelayCommand(CanExecute = nameof(CanSubmitScore))]
    private async Task SubmitScore()  // Async now!
    {
        // Logic here (same as before)
    }

    [RelayCommand]
    private async Task CopySeed()
    {
        await ClipboardService.CopyToClipboardAsync(TodaySeed);
        ShowSubmissionMessage("Seed copied!", false);
    }

    [RelayCommand]
    private async Task RefreshScores()
    {
        await FetchAndDisplayHighScoresAsync(forceRefresh: true);
    }

    [RelayCommand]
    private void AnalyzeSeed()
    {
        AnalyzeSeedRequested?.Invoke(this, TodaySeed);
    }

    private bool CanSubmitScore() =>
        !string.IsNullOrWhiteSpace(ScoreInput) &&
        !string.IsNullOrWhiteSpace(InitialsInput) &&
        !string.IsNullOrWhiteSpace(AnteInput);

    // Constructor is now TINY!
    public DayLatroWidgetViewModel(
        DaylatroHighScoreService scoreService,
        UserProfileService profileService)
    {
        _scoreService = scoreService;
        _profileService = profileService;
        // Commands auto-generated - no manual setup!
    }
}
```

**Result:**
- **130 lines removed** (52% reduction!)
- **Compile-time safety** for command state
- **No manual NotifyCanExecuteChanged** calls
- **Cleaner, more maintainable**

---

## 📋 **STEP-BY-STEP MIGRATION PLAN**

### Week 1: BaseWidgetViewModel Pattern Everywhere
1. ✅ Add `partial` to all ViewModel classes
2. ✅ Convert properties to `[ObservableProperty]`
3. ✅ Convert commands to `[RelayCommand]`
4. ✅ Remove manual constructor initialization

**Files:** Start with smaller ViewModels
- `FilterSelectorViewModel.cs`
- `MainWindowViewModel.cs`

### Week 2: Compiled Bindings
1. ✅ Add `x:DataType` to all UserControls
2. ✅ Add `x:CompileBindings="True"`
3. ✅ Fix any compile errors (these were runtime bugs!)
4. ✅ Add `DataType` to DataTemplates

**Files:** Start with completed ViewModels from Week 1

### Week 3: Complex ViewModels
1. ✅ Refactor `DayLatroWidgetViewModel.cs`
2. ✅ Refactor `SearchModalViewModel.cs`
3. ✅ Test thoroughly - behavior should be identical!

### Week 4: Cleanup
1. ✅ Delete `BaseViewModel.cs` (replaced by `ObservableObject`)
2. ✅ Remove any remaining FindControl() calls
3. ✅ Add XML documentation to public APIs

---

## 🔍 **QUICK REFERENCE CARD**

### Property Declaration

```csharp
// OLD WAY (8 lines):
private string _name;
public string Name
{
    get => _name;
    set => SetProperty(ref _name, value);
}

// NEW WAY (2 lines):
[ObservableProperty]
private string _name;
```

### Command Declaration

```csharp
// OLD WAY (10 lines):
public ICommand SaveCommand { get; }
public MyViewModel()
{
    SaveCommand = new AsyncRelayCommand(SaveAsync);
}
private async Task SaveAsync() { await _service.SaveAsync(); }

// NEW WAY (4 lines):
[RelayCommand]
private async Task Save()
{
    await _service.SaveAsync();
}
```

### Compiled Binding Declaration

```xml
<!-- Always include these attributes: -->
<UserControl xmlns:vm="using:MyApp.ViewModels"
             x:DataType="vm:MyViewModel"
             x:CompileBindings="True">
```

---

## 🎓 **LEARNING RESOURCES**

### Official Docs
- **Avalonia MVVM:** https://docs.avaloniaui.net/docs/concepts/the-mvvm-pattern/
- **CommunityToolkit.Mvvm:** https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/

### Your Codebase Examples
- **✅ GOOD:** `BaseWidgetViewModel.cs` - Perfect 2025 pattern!
- **✅ GOOD:** `DayLatroWidget.axaml` - Compiled bindings example!
- **⚠️ OLD:** `DayLatroWidgetViewModel.cs` - Needs refactoring
- **⚠️ OLD:** `MainWindowViewModel.cs` - Manual command setup

---

## 💡 **KEY PRINCIPLES**

1. **KISS (Keep It Simple, Stupid)**
   - Use source generators - they're simpler AND safer
   - Don't over-engineer - if `[ObservableProperty]` works, use it!

2. **Consistency**
   - ALL ViewModels should use the same pattern
   - No mixing manual properties with source generators

3. **Compile-Time Safety**
   - Enable compiled bindings everywhere
   - Catch typos at build time, not runtime

4. **Testability**
   - ViewModels should have NO Avalonia dependencies
   - All business logic in ViewModels (not code-behind)

5. **Maintainability**
   - Less code = fewer bugs
   - Source generators = consistent implementation
   - Future devs will thank you!

---

**Remember:** Your codebase is already 80% modern! These changes will get you to 100% while **reducing** code by ~40%. Win-win! 🎉
