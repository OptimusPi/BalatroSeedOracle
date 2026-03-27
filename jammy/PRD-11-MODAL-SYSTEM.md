# PRD-11: Modal System

## Summary

A centralized modal system that handles all popup/overlay UIs in the application. Modals appear over a dimmed background, are template-driven (ViewModel-first navigation), and support a stack for nested modals. The system manages open/close animations, back navigation, and keyboard dismissal.

---

## Current Implementation (Legacy Reference)

| File | Role |
|------|------|
| `Views/Modals/StandardModal.axaml` | Base modal template |
| `Views/Modals/SearchModal.axaml` | Search interface |
| `Views/Modals/FiltersModal.axaml` | Filter management |
| `Views/Modals/FilterSelectionModal.axaml` | Filter picker |
| `Views/Modals/AnalyzeModal.axaml` | Seed analysis |
| `Views/Modals/SettingsModal.axaml` | App settings |
| `Views/Modals/CreditsModal.axaml` | Credits/attribution |
| `Views/Modals/ToolsModal.axaml` | Tools & utilities |
| `Views/Modals/WidgetPickerModal.axaml` | Widget selection |
| `Views/Modals/WordListsModal.axaml` | Word list management |
| `Views/Modals/AudioVisualizerSettingsModal.axaml` | Visualizer settings |
| `Helpers/ModalHelper.cs` | Modal operations |
| `Helpers/IModalBackNavigable.cs` | Back navigation interface |
| `ViewModels/MainWindowViewModel.cs` | Modal hosting state |

---

## Requirements

### R1 — Modal Architecture

```
┌─────────────────────────────────────────┐
│ Main Content (blurred when modal open)  │
│                                         │
│   ┌─────────────────────────────────┐   │
│   │ Semi-transparent black overlay  │   │  ZIndex: 100
│   │                                 │   │
│   │   ┌─────────────────────────┐   │   │
│   │   │     Modal Content       │   │   │
│   │   │   (DataTemplate-driven) │   │   │
│   │   └─────────────────────────┘   │   │
│   │                                 │   │
│   └─────────────────────────────────┘   │
│                                         │
└─────────────────────────────────────────┘
```

### R2 — ViewModel-First Navigation

Modals are rendered using `DataTemplate` mapping:

```xml
<ContentControl Content="{Binding ActiveModal}">
    <ContentControl.DataTemplates>
        <DataTemplate DataType="vm:SearchModalViewModel">
            <modals:SearchModal />
        </DataTemplate>
        <DataTemplate DataType="vm:FiltersModalViewModel">
            <modals:FiltersModal />
        </DataTemplate>
        <!-- ... more mappings ... -->
    </ContentControl.DataTemplates>
</ContentControl>
```

- Set `ActiveModal` to a ViewModel instance → correct View auto-renders
- Set `ActiveModal` to null → modal closes
- `IsModalVisible` property controls overlay visibility

### R3 — Modal Host Properties

```csharp
// On MainWindowViewModel or BalatroMainMenuViewModel
ObservableObject ActiveModal { get; set; }  // Current modal VM
bool IsModalVisible { get; }                // True when any modal is open
```

### R4 — Modal Lifecycle

```
Open:
  1. Create ViewModel for target modal
  2. Set ActiveModal = viewModel
  3. IsModalVisible becomes true
  4. Overlay fades in
  5. Modal content appears (slide-up optional)

Close:
  1. Modal content disappears
  2. Overlay fades out
  3. Set ActiveModal = null
  4. IsModalVisible becomes false
```

### R5 — Modal Inventory

| Modal | ViewModel | Purpose | Opened By |
|-------|-----------|---------|-----------|
| SearchModal | `SearchModalViewModel` | Seed search workflow | SEARCH button |
| FiltersModal | `FiltersModalViewModel` | Filter editor | DESIGNER button |
| FilterSelectionModal | `FilterSelectionModalViewModel` | Pick a saved filter | Search tab filter picker |
| AnalyzeModal | `AnalyzeModalViewModel` | Seed analysis | ANALYZER button |
| SettingsModal | `SettingsModalViewModel` | App settings | SETTINGS button |
| CreditsModal | `CreditsModalViewModel` | Credits/about | Settings or menu |
| ToolsModal | — | Tools & utilities | SETTINGS button |
| WidgetPickerModal | — | Widget toggle list | Widget dock |
| WordListsModal | — | Manage word lists | Settings |
| AudioVisualizerSettingsModal | `AudioVisualizerSettingsModalViewModel` | Visualizer config | Widget or settings |

### R6 — StandardModal Template

Base layout for all modals:
- Dark panel background (`ModalGrey`)
- Rounded corners (12px)
- Border (`ModalBorder`, 2px)
- Close button (top-right X)
- Title bar (optional)
- Content area
- Max width/height constraints
- Centered in viewport

### R7 — Back Navigation

`IModalBackNavigable` interface:
```csharp
public interface IModalBackNavigable
{
    bool CanGoBack { get; }
    void GoBack();
}
```

- Some modals support internal back navigation (e.g., filter selection → filter details → back)
- Back button appears when `CanGoBack` is true
- Escape key triggers back or close

### R8 — Keyboard Handling

| Key | Action |
|-----|--------|
| `Escape` | Close modal (or go back if navigable) |
| `Enter` | Confirm/submit (context-dependent) |

### R9 — Overlay Dimmer

- `SemiTransparentBlack` background
- Click on overlay outside modal = close (light-dismiss)
- Optional: disable light-dismiss for critical modals

### R10 — Nested Modals

- Support opening a modal from within a modal (e.g., FilterSelectionModal from SearchModal)
- Stack-based: closing inner modal returns to outer modal
- Only topmost modal is interactive

---

## Acceptance Criteria

- [ ] Setting ActiveModal to a ViewModel renders the correct View
- [ ] Overlay dims the background when modal is open
- [ ] Close button (X) closes the modal
- [ ] Escape key closes the modal
- [ ] Light-dismiss (click overlay) closes the modal
- [ ] All 11 modals render correctly
- [ ] StandardModal template provides consistent styling
- [ ] Back navigation works for modals that support it
- [ ] Nested modals work (modal from within modal)
- [ ] No UI interaction possible behind the modal overlay
