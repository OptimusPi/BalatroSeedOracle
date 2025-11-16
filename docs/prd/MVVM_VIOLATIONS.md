# MVVM ARCHITECTURE VIOLATIONS - Separation of Concerns

**Status:** 🟡 MEDIUM PRIORITY
**Priority:** P2 - Technical Debt Reduction
**Estimated Time:** 4-6 hours
**Generated:** 2025-11-03

---

## Overview

Fix MVVM pattern violations where Views directly access ViewModels, Views contain business logic, and tight coupling breaks testability and maintainability.

---

## Violation #1: Direct ViewModel Property Manipulation from View

### Problem
**File:** `src/Views/BalatroMainMenu.axaml.cs`
**Severity:** MEDIUM

View code-behind directly accesses and manipulates ViewModel properties:

```csharp
// ❌ BAD - View setting ViewModel properties
searchContent.ViewModel.MainMenu = this;
searchContent.ViewModel.SelectedTabIndex = 0;
filtersModal.ViewModel.CurrentFilterPath = filterPath;
await filtersModal.ViewModel.ReloadVisualFromSavedFileCommand.ExecuteAsync(null);
```

### Impact
- Tight coupling between View and ViewModel
- Cannot test ViewModel independently
- Cannot reuse ViewModel with different Views
- Breaks MVVM separation of concerns

### Why This Happened
Quick prototyping led to shortcuts. Instead of creating proper abstractions (commands, behaviors, attached properties), developers directly accessed ViewModel from View code-behind.

### Acceptance Criteria
- [ ] Remove direct ViewModel property access from View code-behind
- [ ] Use Commands for user actions
- [ ] Use Behaviors for UI interactions
- [ ] Use attached properties for cross-view communication
- [ ] ViewModels should NOT reference Views at all

### Implementation Strategy

**Pattern 1: Use Commands Instead of Property Setting**

```csharp
// BEFORE (BAD):
searchContent.ViewModel.SelectedTabIndex = 0;

// AFTER (GOOD) - In View XAML:
<Button Command="{Binding SelectTabCommand}" CommandParameter="0"/>

// In ViewModel:
[RelayCommand]
private void SelectTab(int tabIndex)
{
    SelectedTabIndex = tabIndex;
}
```

**Pattern 2: Use Dependency Injection for Cross-Component Communication**

```csharp
// BEFORE (BAD):
searchContent.ViewModel.MainMenu = this;

// AFTER (GOOD) - Register service:
public interface INavigationService
{
    void NavigateToSearch();
    void NavigateToFilters();
}

// Inject in ViewModel constructor:
public SearchModalViewModel(INavigationService navigationService)
{
    _navigationService = navigationService;
}
```

**Pattern 3: Use Messenger Pattern for Loose Coupling**

```csharp
// Instead of direct reference:
filtersModal.ViewModel.CurrentFilterPath = filterPath;

// Use messenger:
WeakReferenceMessenger.Default.Send(new LoadFilterMessage(filterPath));

// In FiltersModalViewModel:
WeakReferenceMessenger.Default.Register<LoadFilterMessage>(this, (r, m) =>
{
    CurrentFilterPath = m.FilterPath;
    ReloadVisualFromSavedFileCommand.Execute(null);
});
```

---

## Violation #2: FindControl in Code-Behind - Tight XAML Coupling

### Problem
**Files:** 35+ files with 202 occurrences
**Severity:** MEDIUM (but common in Avalonia)

Heavy use of `this.FindControl<T>()` in View code-behind:

```csharp
var modalContainer = this.FindControl<Grid>("ModalContainer");
_cardName = this.FindControl<TextBlock>("CardName");
_deckImage = this.FindControl<Image>("DeckImage");
```

### Impact
- If XAML element names change → code breaks
- Cannot change XAML structure without updating code
- Difficult to test View logic

### Mitigation
While `FindControl` is acceptable in Avalonia (not all UI frameworks support full MVVM), we can reduce usage:

### Acceptance Criteria
- [ ] Move business logic OUT of View code-behind
- [ ] Use `x:Name` bindings sparingly
- [ ] Prefer ViewModel bindings over FindControl
- [ ] Use Attached Behaviors for complex UI interactions

### Implementation

```csharp
// BEFORE (QUESTIONABLE):
private void OnLoaded(object? sender, RoutedEventArgs e)
{
    var grid = this.FindControl<Grid>("ModalContainer");
    grid.Opacity = 0;
    // Animate opacity...
}

// AFTER (BETTER) - Use Behavior:
public class FadeInBehavior : Behavior<Control>
{
    protected override void OnAttached()
    {
        if (AssociatedObject is { } control)
        {
            control.Opacity = 0;
            // Animate opacity...
        }
    }
}

// In XAML:
<Grid>
    <i:Interaction.Behaviors>
        <behaviors:FadeInBehavior />
    </i:Interaction.Behaviors>
</Grid>
```

---

## Violation #3: Business Logic in View Code-Behind

### Problem
**File:** `src/Views/BalatroMainMenu.axaml.cs`
**Severity:** HIGH

View contains modal management logic, animation logic, and state management - all business concerns:

```csharp
// ❌ 50+ lines of modal transition logic in View
private async Task ShowSearchModal()
{
    // Complex animation logic
    // State management
    // ViewModel manipulation
    // Should all be in ViewModel or Service
}
```

### Impact
- Cannot unit test modal logic
- Cannot reuse modal logic in other Views
- View is doing too much (god class anti-pattern)

### Acceptance Criteria
- [ ] Extract modal management into `IModalService`
- [ ] Move animation logic to Behaviors
- [ ] Move state management to ViewModels
- [ ] View should ONLY handle UI rendering

### Implementation

**Create Modal Service:**

```csharp
public interface IModalService
{
    Task ShowModalAsync<TViewModel>(TViewModel viewModel, ModalOptions? options = null)
        where TViewModel : class;

    Task<bool> ShowConfirmationAsync(string title, string message);

    Task CloseModalAsync();
}

public class ModalService : IModalService
{
    public async Task ShowModalAsync<TViewModel>(TViewModel viewModel, ModalOptions? options = null)
        where TViewModel : class
    {
        // Modal creation logic
        // Animation logic
        // Lifecycle management
    }

    // Implementation...
}
```

**Use in ViewModel:**

```csharp
public class MainMenuViewModel : ObservableObject
{
    private readonly IModalService _modalService;

    public MainMenuViewModel(IModalService modalService)
    {
        _modalService = modalService;
    }

    [RelayCommand]
    private async Task ShowSearch()
    {
        var searchVM = new SearchModalViewModel();
        await _modalService.ShowModalAsync(searchVM);
    }
}
```

**View becomes simple:**

```csharp
// View code-behind is now minimal:
public partial class BalatroMainMenu : UserControl
{
    public BalatroMainMenu()
    {
        InitializeComponent();
    }

    // That's it! No business logic.
}
```

---

## Violation #4: View References in ViewModel

### Problem
**Files:** Some ViewModels
**Severity:** HIGH

Some ViewModels have references to View objects:

```csharp
// ❌ ViewModel should NOT reference View
public IView? MainMenu { get; set; }
```

### Impact
- Breaks MVVM completely
- ViewModel depends on View (should be opposite)
- Cannot test ViewModel without View
- Cannot swap View implementations

### Acceptance Criteria
- [ ] Remove ALL View references from ViewModels
- [ ] Use services for cross-component communication
- [ ] Use Messenger pattern for loose coupling
- [ ] ViewModels should only reference interfaces

### Implementation

```csharp
// BEFORE (WRONG):
public class SearchModalViewModel
{
    public IView? MainMenu { get; set; }

    public void NavigateBack()
    {
        MainMenu?.Close();  // ViewModel calling View method!
    }
}

// AFTER (CORRECT):
public class SearchModalViewModel
{
    private readonly INavigationService _navigationService;

    public SearchModalViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public void NavigateBack()
    {
        _navigationService.CloseModal();  // ViewModel calls service
    }
}
```

---

## Implementation Plan

### Phase 1: Create Service Abstractions (2 hours)
1. Create `IModalService` interface
2. Create `INavigationService` interface
3. Create `IDialogService` interface
4. Implement services with proper async/await
5. Register in DI container

### Phase 2: Extract Business Logic from Views (2 hours)
1. Move modal management to `ModalService`
2. Move animation logic to Behaviors
3. Move state management to ViewModels
4. Update BalatroMainMenu to use services

### Phase 3: Remove ViewModel → View References (1 hour)
1. Find all View references in ViewModels
2. Replace with service calls
3. Use Messenger for loose coupling
4. Test each replacement

### Phase 4: Reduce FindControl Usage (1 hour)
1. Convert complex FindControl logic to Behaviors
2. Use ViewModel bindings where possible
3. Document remaining FindControl usage as "acceptable for Avalonia"

---

## Files Requiring Changes

### High Priority
- `src/Views/BalatroMainMenu.axaml.cs` - Extract modal service
- `src/ViewModels/SearchModalViewModel.cs` - Remove View references
- `src/ViewModels/FiltersModalViewModel.cs` - Remove View references

### Medium Priority
- `src/Components/FilterSelector.axaml.cs` - Convert to behaviors
- All widget code-behinds - Move logic to ViewModels

### New Files to Create
- `src/Services/IModalService.cs`
- `src/Services/ModalService.cs`
- `src/Services/INavigationService.cs`
- `src/Services/NavigationService.cs`
- `src/Services/IDialogService.cs`
- `src/Services/DialogService.cs`
- `src/Behaviors/ModalAnimationBehavior.cs`

---

## Test Plan

### Service Tests
1. Test ModalService.ShowModalAsync() creates modal
2. Test CloseModalAsync() cleans up resources
3. Test NavigationService routes correctly
4. Test DialogService shows confirmation dialogs

### Integration Tests
1. Open each modal via ViewModel command
2. Verify no direct ViewModel access from View
3. Verify View code-behind is minimal
4. Verify modal animations still work

### Regression Tests
1. Test all user flows (search, filters, widgets, etc.)
2. Verify no features broke during refactor
3. Verify performance is same or better

---

## Success Metrics

- ✅ Zero View references in ViewModels
- ✅ Zero ViewModel property manipulation from View code-behind
- ✅ All modal management in ModalService
- ✅ View code-behind < 50 lines for all Views
- ✅ 100% ViewModel unit test coverage possible

---

## Benefits

### Testability
- Can unit test ViewModels without Views
- Can mock services for isolated testing
- Can test business logic independently

### Maintainability
- Clear separation of concerns
- Easy to find business logic (in ViewModels/Services)
- Easy to find UI logic (in Views/Behaviors)

### Reusability
- ViewModels can be reused with different Views
- Services can be used by multiple ViewModels
- Behaviors can be applied to any control

---

## Migration Strategy

**Incremental migration - don't break existing functionality:**

1. Create new services alongside existing code
2. Migrate one modal at a time
3. Test each migration thoroughly
4. Remove old code only after new code is proven
5. Document migration progress in this file

**Priority order:**
1. SearchModal (most used)
2. FiltersModal (most complex)
3. SettingsModal
4. CollectionModal
5. WidgetsModal

---

## Dependencies

- CommunityToolkit.Mvvm (already installed)
- Microsoft.Extensions.DependencyInjection (already installed)
- No new dependencies needed

---

## Estimated Effort

- Service creation: 2 hours
- Business logic extraction: 2 hours
- Remove View references: 1 hour
- Reduce FindControl usage: 1 hour
- Testing: 1 hour
- Documentation: 30 minutes
- **Total: 7.5 hours**

---

## Assignee

coding-agent (automated via Claude Code)

---

## Notes

**This is technical debt payoff work.** It won't add new features, but it will:
- Make code more maintainable
- Make features easier to add in the future
- Make code easier to test
- Reduce bugs from tight coupling

**Priority**: P2 - Do after critical bugs are fixed, before next feature sprint.

---

## References

- [MVVM Pattern Documentation](https://docs.microsoft.com/en-us/dotnet/architecture/maui/mvvm)
- [Avalonia MVVM Guide](https://docs.avaloniaui.net/docs/concepts/mvvm)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
