# Test Button Freeze Fix Plan

## Problem Statement
The "Test Filter" button in the Filter Designer modal freezes the app instead of actually testing the filter. Users expect to click "Test" and see immediate feedback that their filter works, but instead the UI becomes unresponsive.

## Current State Analysis

### Files Involved
- `src/ViewModels/FilterTabs/SaveFilterTabViewModel.cs` - Contains `TestFilter()` method (line 234-290)
- `src/Services/SearchManager.cs` - Manages search operations
- `src/Views/FilterTabs/SaveFilterTab.axaml` - UI with Test button

### How Test Button Currently Works

**Location:** `SaveFilterTabViewModel.cs:234-290`

```csharp
private void TestFilter()
{
    // 1. Build filter config from current state
    var config = BuildConfigFromCurrentState();

    // 2. Save to temp file
    var tempPath = _configurationService.GetTempFilterPath();
    var saved = _configurationService.SaveFilterAsync(tempPath, config)
        .GetAwaiter().GetResult(); // ⚠️ BLOCKING CALL ON UI THREAD

    // 3. Build search criteria with BatchSize=2
    var criteria = new SearchCriteria
    {
        ConfigPath = tempPath,
        BatchSize = 2, // 2-char batch = 35^6 = 1,838,265,625 batches!
        StartBatch = 0,
        EndBatch = GetMaxBatchesForBatchSize(2),
        // ...
    };

    // 4. Start search (fire and forget)
    _ = searchManager.StartSearchAsync(criteria, config);

    // 5. Update status
    UpdateStatus($"🔍 Testing '{config.Name}'...", false);
}
```

### Root Causes of Freeze

#### Problem 1: Blocking Async Call on UI Thread
**Line 250:** `.GetAwaiter().GetResult()`

This BLOCKS the UI thread waiting for the file save to complete. In Avalonia (and WPF/WinUI), blocking the UI thread causes the entire app to freeze.

**Why it's bad:**
- UI thread can't process user input
- No visual feedback (spinner, progress bar)
- App appears crashed
- User has no idea what's happening

#### Problem 2: Massive Search Space
**Lines 262-271:** Search criteria with `BatchSize = 2`

```csharp
BatchSize = 2,
StartBatch = 0,
EndBatch = GetMaxBatchesForBatchSize(2), // Returns 35^6 = 1,838,265,625
```

The comment says "small-batch search" but **BatchSize=2 is ENORMOUS**:
- 35 characters per position (0-9, A-Z)
- 2 characters per batch = 35^2 = 1,225 combinations per batch
- 6 remaining characters = 35^6 = 1,838,265,625 total batches
- At ~10 batches/second, this would take **5.8 YEARS** to complete!

**This is NOT a test - it's a full search!**

#### Problem 3: Fire-and-Forget Search
**Line 282:** `_ = searchManager.StartSearchAsync(criteria, config);`

The search is started but:
- No await (fire and forget)
- No cancellation token
- No progress updates
- No completion callback
- Method returns immediately but search continues in background

**User sees:** Status message appears, then nothing happens (because search is running in background for years).

#### Problem 4: No Modal Context
The test starts a search but doesn't:
- Switch to search UI
- Show search results modal
- Provide cancel button
- Show progress bar

User is left in the filter designer with no way to see results or cancel.

### What Users Actually Expect

When clicking "Test Filter":
1. **Immediate Visual Feedback** - Loading spinner, "Testing..." message
2. **Quick Validation** - Does the filter parse correctly? Are all items valid?
3. **Sample Results** - Find 5-10 matching seeds QUICKLY (not billions of batches)
4. **Clear Success/Failure** - Green checkmark or red error message
5. **Details** - If successful: "Found 8 seeds in 2.3 seconds"
6. **Action Options** - "View Results", "Run Full Search", "Close"

## Proposed Solution

### Approach 1: Real Test (Validation Only) - RECOMMENDED

**Goal:** Test button validates filter without running search.

**What it does:**
1. Parse filter config - catch JSON errors
2. Validate all items exist (jokers, consumables, etc.)
3. Check for logical errors (e.g., must + must not same item)
4. Display validation results

**Pros:**
- Instant feedback (< 100ms)
- No UI freeze
- Catches 99% of user errors
- No search manager dependency

**Cons:**
- Doesn't test if filter finds seeds
- Users might want "sample seeds"

### Approach 2: Quick Sample Search

**Goal:** Find a small number of matching seeds quickly.

**What it does:**
1. Validate filter (Approach 1)
2. Run TINY search (BatchSize=6, max 100 batches = ~2 seconds)
3. Show results in modal: "Found 3 seeds in 1.8s"
4. Offer "Run Full Search" button

**Pros:**
- Validates filter works
- Shows real results
- Still fast (< 5 seconds)
- No UI freeze (proper async)

**Cons:**
- More complex
- Still depends on SearchManager
- Might find 0 results (doesn't mean filter is bad)

### Approach 3: Test Modal with Real Search UI

**Goal:** Test button opens search modal with preset criteria.

**What it does:**
1. Save filter
2. Open SearchModal with filter pre-loaded
3. Let user configure batch size, deck, stake
4. User clicks "Start Search" when ready

**Pros:**
- Reuses existing search UI
- User controls search parameters
- Full cancel/progress/results support

**Cons:**
- Extra modal to dismiss
- Feels less immediate
- Not really a "test" anymore

## Recommended Implementation: Approach 2 (Quick Sample Search)

### Phase 1: Fix Blocking Calls (Critical)

**Change TestFilter() from synchronous to async:**

```csharp
// OLD (freezes UI):
private void TestFilter()
{
    var saved = _configurationService.SaveFilterAsync(tempPath, config)
        .GetAwaiter().GetResult(); // ⚠️ BLOCKS UI THREAD
}

// NEW (async):
private async Task TestFilterAsync()
{
    var saved = await _configurationService.SaveFilterAsync(tempPath, config);
    // UI thread is free while waiting!
}
```

**Update RelayCommand:**
```csharp
// OLD:
TestFilterCommand = new RelayCommand(TestFilter);

// NEW:
TestFilterCommand = new AsyncRelayCommand(TestFilterAsync);
```

### Phase 2: Add Quick Test Mode

**Create new method for quick validation:**

```csharp
private async Task<(bool Success, string Message, int SeedCount)> QuickTestFilterAsync()
{
    try
    {
        // 1. Validate filter config
        var config = BuildConfigFromCurrentState();
        if (config == null || string.IsNullOrWhiteSpace(config.Name))
        {
            return (false, "Please enter a filter name", 0);
        }

        // 2. Validate all items exist
        var validation = await ValidateFilterItemsAsync(config);
        if (!validation.IsValid)
        {
            return (false, $"Invalid items: {validation.Error}", 0);
        }

        // 3. Save to temp file (for search to load)
        var tempPath = _configurationService.GetTempFilterPath();
        var saved = await _configurationService.SaveFilterAsync(tempPath, config);
        if (!saved)
        {
            return (false, "Failed to save temp filter", 0);
        }

        // 4. Run QUICK test search (BatchSize=7, max 50 batches = ~1 second)
        var criteria = new SearchCriteria
        {
            ConfigPath = tempPath,
            BatchSize = 7, // 7 chars = only 35 batches total
            StartBatch = 0,
            EndBatch = 50, // Stop after 50 batches (not billions!)
            Deck = GetDeckName(_parentViewModel.SelectedDeckIndex),
            Stake = GetStakeName(_parentViewModel.SelectedStakeIndex),
            MinScore = 0,
            MaxResults = 10, // Stop after finding 10 seeds
        };

        // 5. Run search synchronously (wait for results)
        var searchManager = ServiceHelper.GetService<SearchManager>();
        var results = await searchManager.RunQuickSearchAsync(criteria, config);

        return (true, $"Found {results.Count} seeds in {results.ElapsedTime:F1}s", results.Count);
    }
    catch (Exception ex)
    {
        return (false, $"Error: {ex.Message}", 0);
    }
}
```

### Phase 3: Update UI for Test Results

**Add to SaveFilterTab.axaml:**

```xml
<!-- Test Results Panel (shows after test completes) -->
<Border Background="{StaticResource Green}"
        IsVisible="{Binding ShowTestSuccess}"
        Padding="12"
        CornerRadius="4"
        Margin="0,8">
    <StackPanel Orientation="Horizontal" Spacing="8">
        <TextBlock Text="✓" FontSize="16" Foreground="White"/>
        <TextBlock Text="{Binding TestResultMessage}"
                   FontSize="14"
                   Foreground="White"/>
    </StackPanel>
</Border>

<Border Background="{StaticResource Red}"
        IsVisible="{Binding ShowTestError}"
        Padding="12"
        CornerRadius="4"
        Margin="0,8">
    <StackPanel Orientation="Horizontal" Spacing="8">
        <TextBlock Text="✗" FontSize="16" Foreground="White"/>
        <TextBlock Text="{Binding TestResultMessage}"
                   FontSize="14"
                   Foreground="White"/>
    </StackPanel>
</Border>

<!-- Loading Spinner -->
<Border Background="{StaticResource DarkGray}"
        IsVisible="{Binding IsTestRunning}"
        Padding="12"
        CornerRadius="4"
        Margin="0,8">
    <StackPanel Orientation="Horizontal" Spacing="8">
        <ProgressBar IsIndeterminate="True" Width="100"/>
        <TextBlock Text="Testing filter..."
                   FontSize="14"
                   Foreground="White"/>
    </StackPanel>
</Border>
```

**Add properties to SaveFilterTabViewModel:**

```csharp
[ObservableProperty]
private bool _isTestRunning = false;

[ObservableProperty]
private bool _showTestSuccess = false;

[ObservableProperty]
private bool _showTestError = false;

[ObservableProperty]
private string _testResultMessage = "";
```

### Phase 4: Create SearchManager.RunQuickSearchAsync()

**Add new method to SearchManager.cs:**

```csharp
/// <summary>
/// Runs a quick synchronous search for testing filters
/// Returns results directly instead of fire-and-forget
/// </summary>
public async Task<QuickSearchResults> RunQuickSearchAsync(
    SearchCriteria criteria,
    MotelyJsonConfig config)
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var seeds = new List<string>();
    var batchesChecked = 0;

    try
    {
        // Ensure MaxResults is set to limit search time
        criteria.MaxResults = criteria.MaxResults > 0 ? criteria.MaxResults : 10;

        // Run search on background thread
        await Task.Run(() =>
        {
            for (ulong batch = criteria.StartBatch;
                 batch <= criteria.EndBatch && seeds.Count < criteria.MaxResults;
                 batch++)
            {
                batchesChecked++;
                var batchResults = ProcessBatch(batch, criteria, config);
                seeds.AddRange(batchResults);

                // Stop early if we have enough results
                if (seeds.Count >= criteria.MaxResults)
                    break;
            }
        });

        stopwatch.Stop();

        return new QuickSearchResults
        {
            Seeds = seeds,
            Count = seeds.Count,
            BatchesChecked = batchesChecked,
            ElapsedTime = stopwatch.Elapsed.TotalSeconds,
            Success = true
        };
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        return new QuickSearchResults
        {
            Seeds = new List<string>(),
            Count = 0,
            BatchesChecked = batchesChecked,
            ElapsedTime = stopwatch.Elapsed.TotalSeconds,
            Success = false,
            Error = ex.Message
        };
    }
}

public class QuickSearchResults
{
    public List<string> Seeds { get; set; } = new();
    public int Count { get; set; }
    public int BatchesChecked { get; set; }
    public double ElapsedTime { get; set; }
    public bool Success { get; set; }
    public string Error { get; set; } = "";
}
```

## Implementation Order

### Step 1: Fix Blocking Call (Critical - 30 minutes)
1. Change `TestFilter()` to `async Task TestFilterAsync()`
2. Change `.GetAwaiter().GetResult()` to `await`
3. Update `TestFilterCommand` to use `AsyncRelayCommand`
4. Test - should no longer freeze

**Files:** `SaveFilterTabViewModel.cs`

### Step 2: Add Quick Search Mode (High Priority - 2 hours)
1. Create `RunQuickSearchAsync()` in SearchManager
2. Add `QuickSearchResults` model
3. Update `TestFilterAsync()` to use quick search
4. Limit BatchSize and total batches

**Files:** `SearchManager.cs`, `SaveFilterTabViewModel.cs`

### Step 3: Add UI Feedback (High Priority - 1 hour)
1. Add loading spinner during test
2. Add success/error result panels
3. Show seed count and time elapsed
4. Add "Run Full Search" button if results found

**Files:** `SaveFilterTab.axaml`, `SaveFilterTabViewModel.cs`

### Step 4: Add Validation (Medium Priority - 1 hour)
1. Create `ValidateFilterItemsAsync()` method
2. Check all items exist in game data
3. Check for logical conflicts
4. Show specific validation errors

**Files:** `SaveFilterTabViewModel.cs`

### Step 5: Add Advanced Options (Low Priority - 1 hour)
1. Add "Test Settings" expander
2. Let user choose test batch count
3. Add "View Sample Seeds" button
4. Add "Copy Seeds to Clipboard" button

**Files:** `SaveFilterTab.axaml`, `SaveFilterTabViewModel.cs`

## Success Criteria

### Must Have
- [ ] Test button does NOT freeze app
- [ ] Shows loading spinner during test
- [ ] Shows success/failure message
- [ ] Completes test in < 5 seconds
- [ ] Finds 5-10 sample seeds (if filter is valid)

### Nice to Have
- [ ] Shows validation errors before searching
- [ ] Displays seed count and time elapsed
- [ ] "Run Full Search" button to launch full search modal
- [ ] "View Results" button to see seed details
- [ ] Progress bar during test

## Testing Plan

1. **No Freeze Test**
   - Click Test button
   - Try clicking other UI elements during test
   - Verify app remains responsive

2. **Quick Results Test**
   - Create simple filter (e.g., "Blueprint")
   - Click Test
   - Verify finds results in < 5 seconds

3. **Empty Filter Test**
   - Create filter with no items
   - Click Test
   - Verify shows appropriate error

4. **Invalid Item Test**
   - Create filter with typo ("Bluepront")
   - Click Test
   - Verify shows validation error

5. **Complex Filter Test**
   - Create filter with many clauses
   - Click Test
   - Verify completes in reasonable time

## Risks & Mitigation

### Risk 1: Search Still Slow
**Mitigation:**
- Use BatchSize=7 (only 35 batches total)
- Hard limit to 50 batches max
- Add timeout (5 seconds)
- Run on background thread

### Risk 2: No Results Found
**Mitigation:**
- Explain that test is limited scope
- Add "Run Full Search" option
- Show "Tested X batches, no matches yet"

### Risk 3: SearchManager Refactor Needed
**Mitigation:**
- Make RunQuickSearchAsync() independent method
- Don't modify existing search flow
- Keep changes isolated

## Alternative Approaches Considered

### ❌ Approach A: Just Add Loading Spinner
**Why rejected:** Still runs full search in background, user has no results.

### ❌ Approach B: Mock Test (No Actual Search)
**Why rejected:** Doesn't prove filter actually works, just validates syntax.

### ❌ Approach C: Test Opens Search Modal
**Why rejected:** Breaks user's workflow, not really a "test" anymore.

## Notes
- User explicitly said test button "freezes the app" - this is the #1 priority
- Test should be FAST (< 5 seconds) and give immediate feedback
- Don't confuse "test" with "full search" - they are different operations
- Consider adding analytics to see how often test is used
