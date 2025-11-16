# PRD: Fix Edition Buttons - Immediate Visual Update

## Executive Summary
Edition buttons (Foil, Holo, Polychrome, Negative) correctly set the `Edition` property on items but the UI doesn't update until after drag-and-drop. Stickers and Seals buttons work correctly (apply immediately). Edition buttons should behave identically - clicking them must immediately show the edition overlay on all cards in the item shelf.

## Problem Statement
**User behavior:**
1. User clicks Edition button (e.g., "Foil")
2. Nothing visible happens
3. User drags a card
4. Edition overlay suddenly appears on the dragged card

**Expected behavior:**
1. User clicks Edition button (e.g., "Foil")
2. Edition overlay immediately appears on all applicable cards in item shelf
3. Same behavior as Stickers/Seals buttons

**Root cause (suspected):**
The `SetEdition` command sets `item.Edition = editionValue`, which triggers `OnPropertyChanged()` and `OnPropertyChanged(nameof(EditionImage))`, but the UI binding isn't refreshing until a drag operation forces a re-render.

## Current Implementation

### SetEdition Command
Located in `VisualBuilderTabViewModel.cs` (lines 2452-2497):

```csharp
[RelayCommand]
public void SetEdition(string edition)
{
    if (SelectedEdition == edition)
        return;

    SelectedEdition = edition;
    string? editionValue = edition == "None" ? null : edition.ToLower();

    foreach (var group in GroupedItems)
    {
        foreach (var item in group.Items)
        {
            if (item.Category != "Joker" && item.Category != "StandardCard")
                continue;

            item.Edition = editionValue;  // Sets property, triggers OnPropertyChanged

            // Also update ItemConfig
            if (_parentViewModel != null &&
                _parentViewModel.ItemConfigs.TryGetValue(item.ItemKey, out var config))
            {
                config.Edition = editionValue;
            }
        }
    }
    // ... continues to update drop zone items
}
```

### EditionImage Property
Located in `SelectableItem.cs` (lines 111-132):

```csharp
public IImage? EditionImage
{
    get
    {
        if (string.IsNullOrEmpty(Edition) || Edition == "None")
            return null;

        return Services.SpriteService.Instance.GetEditionImage(Edition);
    }
}
```

### Edition Property
Located in `SelectableItem.cs` (lines 229-242):

```csharp
private string? _edition;
public string? Edition
{
    get => _edition;
    set
    {
        if (_edition != value)
        {
            _edition = value;
            OnPropertyChanged();  // Notifies UI
            OnPropertyChanged(nameof(EditionImage));  // Forces EditionImage refresh
        }
    }
}
```

### UI Binding
Located in `FilterItemCard.axaml` (line 43):

```xml
<!-- Edition overlay (Foil/Holo/Polychrome/Negative) -->
<Image Source="{Binding EditionImage}"
       Width="71"
       Height="95"
       Stretch="Uniform"
       HorizontalAlignment="Center"
       VerticalAlignment="Center"
       IsVisible="{Binding EditionImage, Converter={x:Static ObjectConverters.IsNotNull}}"
       IsHitTestVisible="False"/>
```

## Debugging Steps

### 1. Add Logging (ALREADY DONE)
Logging added to `SetEdition` command to verify:
- Command is being called
- Items are being found
- Edition property is being set

### 2. Test with Running App
Run app, click Edition button, check console for:
```
[SetEdition] 🔥 BUTTON CLICKED! Edition='Foil'
[SetEdition] Processing 12 groups
[SetEdition] Updated Joker: Edition=foil
```

### 3. Compare with Working Stickers/Seals
Identify why Stickers/Seals buttons work but Edition doesn't. Likely differences:
- Property notification timing
- Binding mode (OneWay vs TwoWay)
- UI thread marshaling

## Potential Solutions

### Solution 1: Force UI Thread Update
Add explicit UI thread dispatcher call after setting Edition:

```csharp
item.Edition = editionValue;

// Force UI update on dispatcher
Dispatcher.UIThread.Post(() =>
{
    item.OnPropertyChanged(nameof(item.EditionImage));
}, DispatcherPriority.Render);
```

### Solution 2: ObservableCollection Notification
If items are in an `ObservableCollection`, ensure collection change notification fires:

```csharp
// After updating all items
OnPropertyChanged(nameof(GroupedItems));
```

### Solution 3: Binding Mode Fix
Change binding from OneWay to TwoWay if needed:

```xml
<Image Source="{Binding EditionImage, Mode=OneWay}"
```

### Solution 4: SpriteService Caching Issue
Check if `SpriteService.GetEditionImage()` is caching incorrectly. May need to invalidate cache or force reload.

## Testing Requirements
1. Click Foil button → Edition overlay appears immediately on all Jokers/Cards
2. Click Holo button → Edition overlay switches immediately
3. Click None button → Edition overlay disappears immediately
4. Drag card → Edition overlay persists through drag
5. Drop card → Edition overlay remains on dropped card
6. Behavior matches Stickers/Seals buttons exactly

## Success Criteria
- Edition buttons apply visual changes immediately (no drag required)
- No regression in drag-and-drop functionality
- Edition overlays persist correctly
- Identical UX to Stickers/Seals buttons

## File Locations
- **ViewModel**: `src/ViewModels/FilterTabs/VisualBuilderTabViewModel.cs` (SetEdition command)
- **Model**: `src/Models/SelectableItem.cs` (Edition property, EditionImage getter)
- **View**: `src/Components/FilterItemCard.axaml` (EditionImage binding)
- **Service**: `src/Services/SpriteService.cs` (GetEditionImage method)

## Time Estimate
Should be a quick fix once root cause is identified (15-30 minutes).
