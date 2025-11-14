# PRD: UI Component Testing Strategy

**Status:** 🔴 **HIGH PRIORITY** - Quality Assurance Gap
**Priority:** P1 - Testing Infrastructure
**Estimated Time:** 6-8 hours (initial setup) + ongoing
**Generated:** 2025-11-14

---

## Executive Summary

The Balatro Seed Oracle currently has **minimal automated testing** for UI components. With complex MVVM architecture, custom controls (BalatroTabControl, SortableResultsGrid), and intricate animations, the lack of tests creates risks:

- **Regression bugs** when refactoring (e.g., recent MVVM violations fixes)
- **Visual inconsistencies** after style changes (red arrow misalignment)
- **Animation breakage** (bouncing triangles stopped working)
- **Difficult debugging** (no way to isolate component logic)

This PRD establishes a comprehensive testing strategy across three levels:

1. **Unit Tests** - ViewModel logic, business rules
2. **Integration Tests** - Component behavior, event handling
3. **Visual Tests** - Layout, styling, animations

---

## Current Testing Landscape

### What Exists (Minimal)

**Test Project:**
- Likely: `BalatroSeedOracle.Tests.csproj` (or none)
- Framework: xUnit, NUnit, or MSTest
- Coverage: Unknown (probably <10%)

**Known Gaps:**
- ❌ No ViewModel unit tests
- ❌ No control behavior tests
- ❌ No animation verification
- ❌ No layout/visual regression tests
- ❌ No accessibility tests

### What's Needed

**Critical Areas Needing Tests:**

1. **ViewModels**
   - SearchModalViewModel (search logic, progress)
   - FilterListViewModel (pagination, selection)
   - SortableResultsGridViewModel (sorting, pagination)

2. **Custom Controls**
   - BalatroTabControl (triangle positioning, tab switching)
   - SortableResultsGrid (data display, commands)
   - ItemConfigPopup (item selection, filters)

3. **Animations & Styles**
   - Bouncing triangles (vertical, horizontal)
   - Button hover states
   - Modal transitions

4. **Integration Points**
   - Search → Results flow
   - Filter selection → Visual update
   - Export → File generation

---

## Testing Architecture

### Layer 1: Unit Tests (ViewModels)

**Purpose:** Test business logic in isolation.

**Framework:** xUnit + FluentAssertions + Moq

**Example:** SortableResultsGridViewModel

```csharp
// File: tests/ViewModels/Controls/SortableResultsGridViewModelTests.cs
using Xunit;
using FluentAssertions;
using BalatroSeedOracle.ViewModels.Controls;
using BalatroSeedOracle.Models;

public class SortableResultsGridViewModelTests
{
    [Fact]
    public void AddResults_UpdatesDisplayedResults()
    {
        // Arrange
        var vm = new SortableResultsGridViewModel();
        var results = new List<SearchResult>
        {
            new() { Seed = "ABC123", TotalScore = 100 },
            new() { Seed = "DEF456", TotalScore = 200 }
        };

        // Act
        vm.AddResults(results);

        // Assert
        vm.AllResults.Should().HaveCount(2);
        vm.DisplayedResults.Should().HaveCount(2);
        vm.ResultsCountText.Should().Be("2 results");
    }

    [Fact]
    public void Sorting_ByScoreDescending_OrdersCorrectly()
    {
        // Arrange
        var vm = new SortableResultsGridViewModel();
        vm.AddResults(new[]
        {
            new SearchResult { Seed = "A", TotalScore = 50 },
            new SearchResult { Seed = "B", TotalScore = 200 },
            new SearchResult { Seed = "C", TotalScore = 100 }
        });

        // Act
        vm.SelectedSortIndex = 1; // Score ↓

        // Assert
        vm.DisplayedResults.Select(r => r.Seed).Should().ContainInOrder("B", "C", "A");
    }

    [Fact]
    public void Pagination_NextPage_UpdatesDisplayedResults()
    {
        // Arrange
        var vm = new SortableResultsGridViewModel { ItemsPerPage = 2 };
        vm.AddResults(Enumerable.Range(1, 5).Select(i => new SearchResult { Seed = $"Seed{i}", TotalScore = i }));

        // Act
        vm.NextPageCommand.Execute(null);

        // Assert
        vm.CurrentPage.Should().Be(2);
        vm.DisplayedResults.Should().HaveCount(2);
        vm.DisplayedResults.First().Seed.Should().Be("Seed3");
    }

    [Fact]
    public void PreviousPage_WhenOnFirstPage_RemainsDisabled()
    {
        // Arrange
        var vm = new SortableResultsGridViewModel();
        vm.AddResults(new[] { new SearchResult { Seed = "A", TotalScore = 1 } });

        // Assert
        vm.IsPreviousEnabled.Should().BeFalse();
        vm.PreviousPageCommand.CanExecute(null).Should().BeFalse();
    }
}
```

**Coverage Goal:** >80% for all ViewModels

---

### Layer 2: Integration Tests (Controls)

**Purpose:** Test control behavior with mocked interactions.

**Framework:** xUnit + Avalonia.Headless (or Avalonia.ReactiveUI.Testing)

**Example:** BalatroTabControl

```csharp
// File: tests/Controls/BalatroTabControlTests.cs
using Xunit;
using FluentAssertions;
using Avalonia.Headless.XUnit;
using BalatroSeedOracle.Controls;

public class BalatroTabControlTests
{
    [AvaloniaFact]
    public void TabSwitch_UpdatesTrianglePosition()
    {
        // Arrange
        using var app = AvaloniaApp.BuildAvaloniaApp().UseHeadless(new AvaloniaHeadlessPlatformOptions());
        var tabControl = new BalatroTabControl();
        tabControl.Items = new[] { "Tab 1", "Tab 2", "Tab 3" };

        var window = new Window { Content = tabControl, Width = 800, Height = 600 };
        window.Show();

        // Act
        tabControl.SelectedIndex = 1; // Select Tab 2

        // Assert - Triangle should be centered over Tab 2
        var triangle = tabControl.FindControl<Polygon>("PART_TriangleIndicator");
        var tab2 = (TabItem)tabControl.ItemContainerGenerator.ContainerFromIndex(1);

        var expectedLeft = tab2.Bounds.Left + (tab2.Bounds.Width / 2) - 6; // Half triangle width
        Canvas.GetLeft(triangle).Should().BeApproximately(expectedLeft, 1.0);
    }

    [AvaloniaFact]
    public void TriangleAnimation_Enabled_BouncesVertically()
    {
        // Arrange
        using var app = AvaloniaApp.BuildAvaloniaApp().UseHeadless(new AvaloniaHeadlessPlatformOptions());
        var tabControl = new BalatroTabControl();
        tabControl.Items = new[] { "Tab 1" };

        var window = new Window { Content = tabControl };
        window.Show();

        var triangle = tabControl.FindControl<Polygon>("PART_TriangleIndicator");

        // Assert
        triangle.Classes.Should().Contain("balatro-bounce-vertical");
    }
}
```

**Coverage Goal:** >60% for custom controls

---

### Layer 3: Visual Regression Tests

**Purpose:** Catch visual bugs (misalignment, spacing, colors).

**Framework:** Playwright + Percy (or Applitools)

**Approach:**
1. Render component in isolated test app
2. Take screenshot
3. Compare with baseline
4. Flag differences

**Example Setup:**

```csharp
// File: tests/Visual/VisualTests.cs
using Microsoft.Playwright;
using Xunit;

public class VisualTests
{
    [Fact]
    public async Task BalatroTabControl_MatchesBaseline()
    {
        // Arrange
        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();

        // Navigate to test page with isolated BalatroTabControl
        await page.GotoAsync("http://localhost:5000/test/BalatroTabControl");

        // Act - Take screenshot
        var screenshot = await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = "screenshots/BalatroTabControl.png"
        });

        // Assert - Compare with baseline (Percy API or local comparison)
        // Percy.Snapshot(page, "BalatroTabControl");
    }

    [Fact]
    public async Task FilterModal_JokerSpacing_Correct()
    {
        // Test the joker spacing issue from FILTER_MODAL_UI_BUGS_PRD
        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();

        await page.GotoAsync("http://localhost:5000/test/FilterModal");

        var screenshot = await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = "screenshots/FilterModal_JokerSpacing.png",
            FullPage = false,
            Clip = new Clip { X = 100, Y = 100, Width = 800, Height = 150 } // Crop to joker strip
        });

        // Visual comparison would catch spacing issues
    }
}
```

**Alternative:** Use Avalonia's built-in screenshot capabilities

```csharp
// Avalonia-native screenshot
var control = new BalatroTabControl();
control.Measure(new Size(800, 60));
control.Arrange(new Rect(0, 0, 800, 60));

var bitmap = new RenderTargetBitmap(new PixelSize(800, 60));
bitmap.Render(control);
bitmap.Save("BalatroTabControl_baseline.png");
```

---

## Test Infrastructure Setup

### Step 1: Create Test Project (30 minutes)

```bash
# Create test project
dotnet new xunit -n BalatroSeedOracle.Tests -o tests/BalatroSeedOracle.Tests

# Add references
cd tests/BalatroSeedOracle.Tests
dotnet add reference ../../src/BalatroSeedOracle.csproj

# Add packages
dotnet add package FluentAssertions
dotnet add package Moq
dotnet add package Avalonia.Headless.XUnit
```

**Project structure:**
```
tests/
└── BalatroSeedOracle.Tests/
    ├── ViewModels/
    │   ├── SearchModalViewModelTests.cs
    │   └── Controls/
    │       └── SortableResultsGridViewModelTests.cs
    ├── Controls/
    │   ├── BalatroTabControlTests.cs
    │   └── SortableResultsGridTests.cs
    ├── Behaviors/
    │   └── BalatroCardSwayBehaviorTests.cs
    ├── Converters/
    │   └── ItemNameToSpriteConverterTests.cs
    └── Visual/
        └── VisualTests.cs
```

---

### Step 2: Configure Test Runner (15 minutes)

**Add to CI/CD pipeline:**
```yaml
# .github/workflows/tests.yml (or Azure Pipelines)
name: Run Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - name: Run Tests
        run: dotnet test --logger "console;verbosity=detailed"
      - name: Upload Coverage
        uses: codecov/codecov-action@v3
```

---

### Step 3: Write Initial Tests (2-3 hours)

**Priority order:**

1. **SortableResultsGridViewModel** (highest value)
   - Sorting, pagination, results display
   - 10-15 test cases

2. **FilterListViewModel** (if exists)
   - Filter selection, pagination
   - 8-10 test cases

3. **SearchModalViewModel**
   - Search flow, progress updates
   - 10-12 test cases

4. **BalatroTabControl**
   - Triangle positioning, tab switching
   - 5-7 test cases

---

### Step 4: Visual Baseline Creation (1 hour)

**Create test app for isolated component testing:**

```csharp
// File: tests/TestApp/MainWindow.axaml.cs
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.DataContext = new TestAppViewModel();
    }
}

// ViewModel with all testable components
public class TestAppViewModel
{
    public ObservableCollection<string> TestComponents { get; } = new()
    {
        "BalatroTabControl",
        "SortableResultsGrid",
        "FilterSelectionModal",
        "ItemConfigPopup"
    };

    public Control CreateComponent(string name)
    {
        return name switch
        {
            "BalatroTabControl" => CreateTestTabControl(),
            "SortableResultsGrid" => CreateTestResultsGrid(),
            _ => new TextBlock { Text = "Unknown component" }
        };
    }

    private BalatroTabControl CreateTestTabControl()
    {
        var control = new BalatroTabControl();
        control.Items = new[] { "Filter Config", "Scoring Config", "Preferred Deck" };
        control.SelectedIndex = 0;
        return control;
    }
}
```

**Take baseline screenshots:**
- Run TestApp
- For each component, take screenshot
- Store in `tests/Visual/Baselines/`

---

## Testing Checklist (By Priority)

### P0 - Critical (Must Have)

#### ViewModel Tests
- [ ] SortableResultsGridViewModel
  - [ ] AddResults updates collections
  - [ ] Sorting works (ascending, descending)
  - [ ] Pagination (next, previous, page count)
  - [ ] Results count text updates
  - [ ] Stats text updates
  - [ ] Button enable/disable states

- [ ] SearchModalViewModel
  - [ ] Search starts correctly
  - [ ] Progress updates handled
  - [ ] Results accumulated
  - [ ] Search cancellation
  - [ ] Error handling

#### Integration Tests
- [ ] BalatroTabControl
  - [ ] Triangle positions on selected tab
  - [ ] Triangle updates on tab switch
  - [ ] Animation class applied
  - [ ] Window resize updates position

---

### P1 - High (Should Have)

#### ViewModel Tests
- [ ] FilterListViewModel
  - [ ] Filter selection
  - [ ] Pagination
  - [ ] Search/filter

- [ ] ItemConfigPopupViewModel
  - [ ] Item selection
  - [ ] Filter updates
  - [ ] Must/Should/MustNot logic

#### Integration Tests
- [ ] SortableResultsGrid
  - [ ] DataGrid displays results
  - [ ] Context menu works
  - [ ] Copy command executes
  - [ ] Export command executes

---

### P2 - Medium (Nice to Have)

#### Visual Tests
- [ ] BalatroTabControl baseline
- [ ] FilterModal joker spacing
- [ ] Button hover states
- [ ] Scrollbar appearance

#### Behavior Tests
- [ ] BalatroCardSwayBehavior (animation triggers)
- [ ] LazyImageLoadBehavior (deferred loading)

---

### P3 - Low (Optional)

#### Converter Tests
- [ ] ItemNameToSpriteConverter
- [ ] BoolToVisibilityConverter
- [ ] AntesFormatterConverter

#### Accessibility Tests
- [ ] Screen reader compatibility
- [ ] Keyboard navigation
- [ ] High contrast mode

---

## Acceptance Criteria

### Code Coverage
- [ ] **ViewModel coverage: >80%**
- [ ] **Control coverage: >60%**
- [ ] **Converter coverage: >70%**
- [ ] **Overall coverage: >70%**

### CI Integration
- [ ] Tests run on every commit
- [ ] PR blocked if tests fail
- [ ] Coverage report generated
- [ ] Coverage trend tracked

### Visual Testing
- [ ] Baseline images stored in repo
- [ ] Visual diff on PR
- [ ] Manual approval for intentional changes

---

## Success Metrics

- ✅ **Zero regressions** in tested components
- ✅ **95% of bugs caught** before production
- ✅ **Refactoring confidence** - safe to change code
- ✅ **Documentation value** - tests serve as examples

---

## Tooling Recommendations

### Unit Testing
- **xUnit** - Modern, fast, parallel execution
- **FluentAssertions** - Readable assertions
- **Moq** - Service mocking
- **AutoFixture** - Test data generation

### Integration Testing
- **Avalonia.Headless.XUnit** - Render-less control testing
- **FakeItEasy** - Simpler mocking alternative

### Visual Testing
- **Avalonia RenderTargetBitmap** - Native screenshots
- **ImageSharp** - Pixel comparison
- **Playwright** - E2E testing (if web version exists)
- **Percy** - Cloud-based visual diff (paid)

### Coverage
- **Coverlet** - Code coverage for .NET
- **ReportGenerator** - Coverage HTML reports
- **Codecov** - Coverage tracking & PR comments

---

## Implementation Timeline

### Week 1: Setup (2 hours)
- [ ] Create test project
- [ ] Add packages
- [ ] Configure CI

### Week 2: ViewModel Tests (3 hours)
- [ ] SortableResultsGridViewModel tests
- [ ] SearchModalViewModel tests

### Week 3: Integration Tests (2 hours)
- [ ] BalatroTabControl tests
- [ ] SortableResultsGrid tests

### Week 4: Visual Baselines (1 hour)
- [ ] Create TestApp
- [ ] Capture baseline screenshots
- [ ] Set up comparison script

### Week 5: Refinement (1 hour)
- [ ] Add remaining tests
- [ ] Improve coverage
- [ ] Document testing guide

**Total:** ~9 hours initial setup + ongoing test writing

---

## Maintenance

### On Every PR
- [ ] Add tests for new ViewModels
- [ ] Add tests for new Controls
- [ ] Update visual baselines if UI changed
- [ ] Maintain >70% coverage

### Monthly
- [ ] Review flaky tests (fix or delete)
- [ ] Update test data
- [ ] Refactor slow tests

### Quarterly
- [ ] Evaluate new testing tools
- [ ] Update testing docs
- [ ] Analyze coverage trends

---

## Benefits

### Developer Confidence
- **Safe refactoring** - Tests catch breaks
- **Faster debugging** - Isolate issues quickly
- **Better design** - Testable code is cleaner code

### Quality Assurance
- **Fewer regressions** - Automated safety net
- **Documented behavior** - Tests show how to use APIs
- **Performance tracking** - Benchmark tests

### Team Velocity
- **Faster reviews** - Tests prove correctness
- **Less manual testing** - Automation saves time
- **Knowledge sharing** - Tests document intent

---

## Related PRDs

- [FILTER_MODAL_UI_BUGS_PRD.md](./FILTER_MODAL_UI_BUGS_PRD.md) - Visual tests would catch these
- [TAB_CONTROL_ANIMATION_SYSTEM_PRD.md](./TAB_CONTROL_ANIMATION_SYSTEM_PRD.md) - Integration tests for animations
- [MVVM_VIOLATIONS.md](../archive/MVVM_VIOLATIONS.md) - Unit tests validate MVVM patterns

---

## Assignee

coding-agent (automated via Claude Code)

---

**END OF PRD**
