# Balatro Seed Oracle - Final Polish Checklist 🎰

> Pre-release polish items for v2.0.0  
> Reference: [Avalonia UI Architecture](https://deepwiki.com/AvaloniaUI/Avalonia)

---

## TOP 10 MUST-FIX ITEMS

### 1. 🟢 **API Host Widget Integration** (COMPLETE!)
**Status:** DONE ✅  
**Files:** `ApiHostWidget.axaml`, `ApiHostWidgetViewModel.cs`, `DesktopApiHostService.cs`, `BrowserApiHostService.cs`

- [x] Created ApiHostWidget with Start/Stop server controls
- [x] Platform abstraction via `IApiHostService` interface (removed `#if BROWSER` from ViewModel)
- [x] Integrated with Motely.API via `DesktopApiHostService` (Desktop) / `BrowserApiHostService` stub (Browser)
- [x] Log output panel for server activity
- [x] Port configuration with URL display/copy
- [x] Wire up ApiHostWidget to main menu / widget system (via `BalatroMainMenu.axaml` and `WidgetDock`)
- [x] Toggle in Widget Picker modal (Settings → Add Widgets)
- [ ] Add toggle in Settings to auto-start API on launch (stretch goal)

**Why:** BSO can now host the same Motely API that TUI does for ecosystem compatibility. Uses proper Avalonia DI patterns (constructor injection, no Service Locator anti-pattern).

---

### 2. 🔴 **High-Level Motely Abstraction** (MVP BLOCKER)
**Status:** NOT STARTED

BSO currently mixes low-level DB access with Motely. Should use:
```csharp
// ❌ BAD - BSO knowing about DuckDB internals
var connection = duckDbService.CreateConnection();
connection.Execute("SELECT * FROM...");

// ✅ GOOD - Using Motely's high-level API
var results = await Motely.API.SearchManager.Instance.StartSearchAsync(
    jamlConfig, deck, stake, seedCount);
```

- [ ] Audit all DuckDB references in BSO
- [ ] Replace with Motely interface calls where possible
- [ ] BSO should ONLY interact with Motely abstractions, NOT raw SQL

---

### 3. 🟠 **JAML Editor Live Validation** 
**Status:** PARTIAL

- [x] Fix `JamlTypeAsKeyConverter` event type handling (DONE ✅)
- [x] Simplify `JamlTypeAsKeyConverter` type mappings (removed redundant 1:1 mappings, kept only aliases)
- [ ] Add real-time JAML syntax highlighting
- [ ] Show validation errors inline (not just in error panel)
- [ ] Add autocomplete for joker names, event types, sources
- [ ] Integrate `jaml.schema.json` v2.0.0 for IDE support

---

### 4. 🟠 **Event Filter Full Integration**
**Status:** 90% COMPLETE

- [x] `MotelyJsonEventFilterClause` extends `MotelyJsonFilterClause`
- [x] `FromJsonClause()`, `ConvertClauses()`, `CreateCriteria()` methods
- [x] Event type-as-key in JamlTypeAsKeyConverter (supports `event: LuckyMoney` shorthand)
- [x] Schema updated to v2.0.0 with Event type documentation
- [x] All source types documented (Judgement, RiffRaff, Seance, SixthSense, Emperor, PurpleSealOrEightBall, RareTag, UncommonTag)
- [ ] Test all event types: LuckyMoney, LuckyMult, MisprintMult, WheelOfFortune, CavendishExtinct, GrosMichelExtinct
- [ ] Add Event filter to BSO filter builder UI

---

### 5. 🟠 **Search Results Performance**
**Status:** NEEDS OPTIMIZATION

Per Avalonia's [UI Virtualization System](https://deepwiki.com/AvaloniaUI/Avalonia):
- [ ] Ensure `VirtualizingStackPanel` is used for large result lists
- [ ] Implement container recycling for seed results
- [ ] Add pagination or "load more" for 1000+ results
- [ ] Profile memory usage during large searches

---

### 6. 🟡 **Widget System Polish**
**Status:** FUNCTIONAL BUT ROUGH

Following Avalonia's [Control Framework](https://deepwiki.com/AvaloniaUI/Avalonia):
- [ ] Standardize all widgets to use `BaseWidgetControl`
- [ ] Implement proper `IDisposable` pattern for cleanup
- [ ] Save/restore widget states on app restart
- [ ] Add widget dock/snap functionality
- [ ] Fix z-index issues when multiple widgets overlap

---

### 7. 🟡 **Cross-Platform Testing**
**Status:** WINDOWS ONLY TESTED

Per Avalonia's [Platform Implementations](https://deepwiki.com/AvaloniaUI/Avalonia):
- [ ] Test on macOS (native rendering via libAvaloniaNative)
- [ ] Test on Linux (X11 input handling)
- [ ] Test Browser/WASM build
- [ ] Verify Android build works
- [ ] Document platform-specific issues

---

### 8. 🟡 **Error Handling & User Feedback**
**Status:** INCONSISTENT

- [ ] Add toast notifications for success/error states
- [ ] Standardize error messages across all modals
- [ ] Add loading spinners during async operations
- [ ] Implement retry logic for failed API calls
- [ ] Add "Report Bug" button that exports logs

---

### 9. 🟢 **Theme & Styling Consistency**
**Status:** MOSTLY DONE

Following Avalonia's [Styling and Theming](https://deepwiki.com/AvaloniaUI/Avalonia):
- [x] Audit all hardcoded colors → use StaticResource (ApiHostWidget uses StaticResource for colors)
- [x] Use pseudo-classes for state-based styling (`:running`, `:stopped`, etc.)
- [ ] Verify Balatro theme colors match game exactly
- [ ] Add dark/light mode toggle (stretch goal)
- [ ] Polish button hover/pressed states
- [ ] Ensure all icons use consistent sizing

---

### 10. 🟢 **Documentation & Schema**
**Status:** NEEDS FINAL PASS

- [x] Update `jaml.schema.json` to v2.0.0
- [x] Add Event type documentation
- [x] Document all source types (Judgement, RiffRaff, Seance, SixthSense, etc.)
- [ ] Add in-app help/tooltip system
- [ ] Create "Getting Started" modal for new users
- [ ] Write CHANGELOG.md for v2.0.0

---

## Quick Reference: Avalonia Best Practices

### Property System (from [AvaloniaUI docs](https://deepwiki.com/AvaloniaUI/Avalonia))
```csharp
// Use ObservableProperty for ViewModel properties
[ObservableProperty]
private string _serverStatus = "Stopped";

// Use StyledProperty for custom controls
public static readonly StyledProperty<string> TitleProperty =
    AvaloniaProperty.Register<MyControl, string>(nameof(Title));
```

### MVVM Pattern
```csharp
// ViewModel with CommunityToolkit.Mvvm
public partial class MyViewModel : ObservableObject
{
    [RelayCommand]
    private async Task DoSomethingAsync() { }
}
```

### Dependency Injection (✅ IMPLEMENTED)
```csharp
// ✅ GOOD - Constructor injection
public ApiHostWidgetViewModel(IApiHostService apiHostService) { }

// ❌ BAD - Service Locator anti-pattern
var service = ServiceHelper.GetService<IApiHostService>();
```

### Platform Abstraction (✅ IMPLEMENTED)
```csharp
// ✅ GOOD - Runtime check with interface abstraction
public bool IsSupported => _apiHostService.IsSupported;

// ❌ BAD - Conditional compilation in ViewModel
#if !BROWSER
    // platform-specific code
#endif
```

### Async UI Updates
```csharp
// Always update UI on dispatcher thread
Avalonia.Threading.Dispatcher.UIThread.Post(() =>
{
    StatusText = "Updated!";
});
```

### Styling with StaticResource (✅ IMPLEMENTED)
```xml
<!-- ✅ GOOD - Use StaticResource for theme consistency -->
<Border Background="{StaticResource Green}" />

<!-- ❌ BAD - Hardcoded hex colors -->
<Border Background="#00FF00" />
```

---

## Priority Legend
- 🔴 **MVP BLOCKER** - Must fix before any release
- 🟠 **HIGH** - Should fix for v2.0.0
- 🟡 **MEDIUM** - Nice to have for v2.0.0
- 🟢 **LOW** - Can defer to v2.1.0

## Recent Accomplishments (January 2026)

### ✅ Completed This Session
- **API Host Widget**: Fully integrated with proper Avalonia DI patterns (no Service Locator, no `#if BROWSER` in ViewModels)
- **JAML Type Mappings**: Simplified `JamlTypeAsKeyConverter` - removed redundant 1:1 mappings, kept only aliases (tarot→tarotcard, planet→planetcard, spectral→spectralcard)
- **Event Filter Support**: Full JAML support for Event type with type-as-key shorthand (`event: LuckyMoney`)
- **Platform Abstraction**: Created `IApiHostService` interface with `DesktopApiHostService` and `BrowserApiHostService` implementations
- **Styling**: Migrated hardcoded colors to `StaticResource` in ApiHostWidget, added pseudo-class support
- **Build System**: Fixed `NETSDK1150` error by making `Motely.API` conditionally compile as Library when referenced by BSO Desktop (via `BSO_LIBRARY` MSBuild property)

### 🔄 In Progress
- Unit test verification for simplified JAML converter
- Cross-platform testing (Windows verified, macOS/Linux/Browser pending)

---

*Last Updated: January 2026*  
*Author: pifreak + Claude*
