# Filter & Score UI - ULTIMATE REFACTOR PRD

**Date:** 2025-11-04
**Status:** READY FOR IMPLEMENTATION
**Priority:** 🔥 **CRITICAL** - Makes UX 10x Better
**Complexity:** HIGH - But Worth It

---

## 🎯 Executive Summary

**THE BREAKTHROUGH:** The Configure Filter and Configure Score tabs are 99% the same! By creating reusable MVVM components and implementing smart UI states, we can:

1. Fix current layout issues (OR/AND stacking, drop zones)
2. Make editing way more spacious and comfortable
3. Add visual edition selectors that apply to entire shelf
4. Reduce code duplication massively
5. Make the whole thing feel professional and polished

**USER QUOTE:**
> "I am a fucking GENIUS! :D"
> "REALLY THUINK!!!!!! THEY ARE 99% THE SAME!!!!"
> "Make this into a REAL PRD MARKDOWNFILE and do whatever it takes Claude, I mea it, dont lie to me!!! Do whatever is the most proper tha is most likely to WORK and MAKE ME HAPPY!"

---

## 🐛 Part 1: Fix Current Issues

### Issue 1: OR/AND Layout - Stack Vertically!

**Current (BAD):**
```
┌─────────────────────────────┐
│ [OR   ]  [AND  ]            │ ← Side by side
│ [card] [card]               │
└─────────────────────────────┘
```

**New (GENIUS):**
```
┌─────────────────────────────┐
│ [──                         │ ← Bracket on LEFT
│ [OR                         │
│ [Blueprint                  │
│ [──                         │
│                             │
│ [──                         │
│ [AND                        │
│ [Brainstorm                 │
│ [──                         │
└─────────────────────────────┘
```

**Rules:**
- OR tray and AND tray stacked vertically
- Bracket "[" decoration on LEFT side (not bottom)
- When you drop in OR tray → hide AND tray
- When you drop in AND tray → hide OR tray
- Only build ONE clause at a time
- Make entire clause (bracket + cards) draggable as a group!

### Issue 2: Drop Zone Overlay Boundaries

**Current Problem:**
- Overlay covers ENTIRE right column (accidentally includes OR/AND trays)
- User can drop but visual feedback is wrong

**Fix:**
- OR tray gets its own drop zone overlay (when dragging)
- AND tray gets its own drop zone overlay (when dragging)
- Score list gets its own drop zone overlay (when dragging)
- Overlays do NOT overlap each other
- Each overlay only covers its specific drop target

**Visual:**
```
When dragging:
┌─────────────────────────────┐
│ [OR  ] ← Overlay A (green)  │
│                             │
│ [AND ] ← Overlay B (blue)   │
│                             │
│ ─────────────────────────── │
│ Score Items ← Overlay C     │
│ [card]                      │
│ [card]                      │
└─────────────────────────────┘
```

---

## 🎨 Part 2: UI States System (THE GAME CHANGER)

### State 1: Default State
**What:** Normal view, no interaction
**Layout:**
```
┌──────────┬────────────────┐
│          │ [OR ]          │
│  Item    │ [AND]          │
│  Shelf   │ ─────────────  │
│          │ Score Items    │
│  [cards] │ [card]         │
│  [cards] │ [card]         │
└──────────┴────────────────┘
```

### State 2: Drag State
**What:** User is dragging an item from shelf
**Behavior:**
- Appropriate drop zone overlays appear (OR, AND, Score)
- Overlays pulse with color (green/blue)
- Everything else dims slightly

### State 3: Clause Editing State
**What:** User clicked "Edit" or double-clicked OR/AND clause
**Behavior:**
- Clause expands to take ENTIRE right column
- Left column (item shelf) stays visible (for adding more items)
- Shows expanded config for each card in clause:
  - Antes (checkboxes)
  - Edition (radio buttons)
  - Sources (checkboxes)
- "Done" button to exit editing mode

**Layout:**
```
┌──────────┬────────────────┐
│          │ ┌────────────┐ │
│  Item    │ │ OR CLAUSE  │ │ ← Takes full column
│  Shelf   │ ├────────────┤ │
│          │ │ Blueprint  │ │
│  [cards] │ │ ▼ Antes    │ │
│  [cards] │ │ ☑1 ☑2 ☑3   │ │
│          │ │ Edition:   │ │
│          │ │ ○Foil ○Holo│ │
│          │ ├────────────┤ │
│          │ │ Brainstorm │ │
│          │ │ ...        │ │
│          │ └────────────┘ │
└──────────┴────────────────┘
```

### State 4: Score Editing State
**What:** User clicked "Gear/Settings" icon on Score list
**Behavior:**
- Score list expands to take ENTIRE right column
- Maybe even hide left column temporarily? (more room!)
- Shows ALL score items with expanded configs:
  - Label
  - Weight slider
  - Antes
  - Edition
  - Sources
- "Done" button to exit editing mode

**Layout:**
```
┌────────────────────────────┐
│ ┌────────────────────────┐ │ ← Full width
│ │ SCORE CONFIGURATION    │ │
│ ├────────────────────────┤ │
│ │ Yorick                 │ │
│ │ Label: [........]      │ │
│ │ Weight: ■■■■ [10]      │ │
│ │ Antes: ☑1 ☑2 ☑3        │ │
│ │ Edition: ○Foil ○Holo   │ │
│ ├────────────────────────┤ │
│ │ Triboulet              │ │
│ │ ...                    │ │
│ └────────────────────────┘ │
└────────────────────────────┘
```

**Advantages:**
- WAY more room for configuration
- Less cramped UI
- Clear focus on what you're editing
- Professional feel

---

## 🎴 Part 3: Edition Selector Buttons (VISUAL MAGIC)

### Concept
**Small buttons ACROSS THE TOP of the Item Shelf**
- When clicked, applies edition to ALL items in shelf
- Visual feedback (button highlights, items update)
- Works for BOTH Configure Filter and Configure Score tabs

### For Jokers

**Editions:**
```
┌─────────────────────────────────────────┐
│ [🃏] [✨] [🌈] [🎨] [⚫]  [💀] │ ← Edition buttons
│  None Foil Holo Poly Neg  Ban │
└─────────────────────────────────────────┘
```

- **None** - Regular Jimbo (no edition)
- **Foil** - Foil edition sprite
- **Holographic** - Holo edition sprite
- **Polychrome** - Poly edition sprite
- **Negative** - Regular joker with inverted colors
- **Debuffed/Ban** - Debuffed Enhancer sprite (ONLY on Filter tab, not Score tab)

**Stickers:**
```
┌─────────────────────────────────────────┐
│ [🃏] [⏱️] [♾️] [💰] │ ← Sticker buttons
│  None Per. Etrn Rent │
└─────────────────────────────────────────┘
```

- **None** - Regular Jimbo (no sticker)
- **Perishable** - Joker with Perishable sticker
- **Eternal** - Joker with Eternal sticker (respects CanBeEternal logic)
- **Rental** - Joker with Rental sticker

**Eternal Logic:**
```csharp
// Jokers that CANNOT be Eternal:
- Cavendish
- DietCola
- GrosMichel
- IceCream
- InvisibleJoker
- Luchador
- MrBones
- Popcorn
- Ramen
- Seltzer
- TurtleBean
```

When Eternal button clicked:
- Apply to all jokers EXCEPT above list
- Don't apply to Soul/Legendary jokers
- Visual feedback (button disabled for incompatible jokers)

### For Standard Cards

**Editions:**
```
┌─────────────────────────────────────────┐
│ [🂺] [✨] [🌈] [🎨] │ ← Edition buttons
│  None Foil Holo Poly │
└─────────────────────────────────────────┘
```

- **None** - Ten of Spades (no edition)
- **Foil** - Foil edition
- **Holographic** - Holo edition
- **Polychrome** - Poly edition
- **NO Negative** (cards can't be negative)

**Seals:**
```
┌─────────────────────────────────────────┐
│ [🂺] [🟣] [🟡] [🔴] [🔵] │ ← Seal buttons
│  None Purp Gold Red Blue │
└─────────────────────────────────────────┘
```

- **None** - Ten of Spades (no seal)
- **Purple Seal** - Purple seal sprite
- **Gold Seal** - Gold seal sprite
- **Red Seal** - Red seal sprite
- **Blue Seal** - Blue seal sprite

### For Consumables
**NO edition selector needed** (consumables don't have editions)

### Behavior
1. User clicks edition button
2. ALL items in shelf that support that edition change
3. Button highlights to show current selection
4. Items update visually immediately
5. Setting persists when dragging items to drop zones

---

## 🏗️ Part 4: Reusable Component Architecture

### Problem
Configure Filter and Configure Score tabs are 99% the same but currently duplicated code!

### Solution: Shared Components

#### 1. ItemShelfControl (Reusable)
**Purpose:** Left column with cards and edition selectors

**Properties:**
- `ItemType` (Joker/StandardCard/Consumable)
- `Items` (ObservableCollection)
- `SelectedEdition` (None/Foil/Holo/Poly/Negative)
- `SelectedSticker` (None/Perishable/Eternal/Rental)
- `SelectedSeal` (None/Purple/Gold/Red/Blue)
- `ShowBanButton` (bool - only true on Filter tab)

**Events:**
- `ItemDragStarted`
- `EditionChanged`
- `StickerChanged`
- `SealChanged`

**UI:**
```xml
<UserControl x:Class="ItemShelfControl">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/> <!-- Edition buttons -->
            <RowDefinition Height="*"/>    <!-- Item grid -->
        </Grid.RowDefinitions>

        <!-- Edition Selector Buttons -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" ...>
            <!-- Dynamically generated based on ItemType -->
        </StackPanel>

        <!-- Item Grid -->
        <ItemsControl Grid.Row="1" ItemsSource="{Binding Items}">
            <!-- Card sprites with drag/drop -->
        </ItemsControl>
    </Grid>
</UserControl>
```

#### 2. ClauseTrayControl (Reusable)
**Purpose:** OR/AND tray that can expand

**Properties:**
- `ClauseType` (OR/AND)
- `Items` (ObservableCollection<FilterItem>)
- `IsExpanded` (bool)
- `IsVisible` (bool - hide when other clause active)

**Events:**
- `ItemDropped`
- `ItemRemoved`
- `EditClicked`
- `DragStarted` (drag entire clause!)

**UI:**
```xml
<UserControl x:Class="ClauseTrayControl">
    <Border Classes="clause-tray {ClauseType}">
        <!-- Bracket decoration on LEFT -->
        <Border Classes="bracket-left" />

        <!-- Header -->
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="{Binding ClauseType}" />
            <Button Content="⚙️" Command="{Binding EditCommand}" />
        </StackPanel>

        <!-- Items (collapsed or expanded) -->
        <ItemsControl ItemsSource="{Binding Items}">
            <!-- Cards with inline or expanded config -->
        </ItemsControl>
    </Border>
</UserControl>
```

#### 3. ScoreListControl (Reusable)
**Purpose:** Score items list that can expand

**Properties:**
- `Items` (ObservableCollection<FilterItem>)
- `IsExpanded` (bool)

**Events:**
- `ItemDropped`
- `ItemRemoved`
- `EditClicked`

**UI:**
```xml
<UserControl x:Class="ScoreListControl">
    <Grid>
        <!-- Header with gear icon -->
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="SCORE ITEMS" />
            <Button Content="⚙️" Command="{Binding EditCommand}" />
        </StackPanel>

        <!-- Items -->
        <ItemsControl ItemsSource="{Binding Items}">
            <!-- Cards with weight + config -->
        </ItemsControl>
    </Grid>
</UserControl>
```

#### 4. DropZoneOverlay (Reusable)
**Purpose:** Proper overlay that doesn't cover everything

**Properties:**
- `TargetControl` (reference to control it overlays)
- `IsActive` (bool)
- `Color` (Green/Blue/Red)

**Behavior:**
- Positioned directly over target control only
- Pulsing animation when active
- Does NOT extend beyond target bounds

---

## 📐 Part 5: New Tab Layout

### Configure Score Tab (Refactored)

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="300"/> <!-- Item Shelf -->
        <ColumnDefinition Width="*"/>   <!-- Right Column -->
    </Grid.ColumnDefinitions>

    <!-- Item Shelf (reusable component) -->
    <controls:ItemShelfControl Grid.Column="0"
                               ItemType="Joker"
                               Items="{Binding AvailableJokers}" />

    <!-- Right Column (state-dependent) -->
    <Grid Grid.Column="1">
        <!-- Default State: OR + AND + Score List -->
        <StackPanel IsVisible="{Binding EditingState, Converter={StaticResource IsDefaultState}}">
            <controls:ClauseTrayControl ClauseType="OR"
                                        Items="{Binding OrItems}"
                                        IsVisible="{Binding !HasAndItems}" />

            <controls:ClauseTrayControl ClauseType="AND"
                                        Items="{Binding AndItems}"
                                        IsVisible="{Binding !HasOrItems}" />

            <controls:ScoreListControl Items="{Binding ShouldItems}" />
        </StackPanel>

        <!-- Clause Editing State: Expanded Clause -->
        <controls:ClauseTrayControl IsVisible="{Binding EditingState, Converter={StaticResource IsClauseEditState}}"
                                    IsExpanded="True"
                                    ClauseType="{Binding EditingClauseType}"
                                    Items="{Binding EditingClauseItems}" />

        <!-- Score Editing State: Expanded Score List -->
        <controls:ScoreListControl IsVisible="{Binding EditingState, Converter={StaticResource IsScoreEditState}}"
                                   IsExpanded="True"
                                   Items="{Binding ShouldItems}" />
    </Grid>

    <!-- Drop Zone Overlays (only shown during drag) -->
    <controls:DropZoneOverlay TargetControl="{Binding #OrTray}"
                              IsActive="{Binding IsDragging}"
                              Color="Green" />
    <controls:DropZoneOverlay TargetControl="{Binding #AndTray}"
                              IsActive="{Binding IsDragging}"
                              Color="Blue" />
    <controls:DropZoneOverlay TargetControl="{Binding #ScoreList}"
                              IsActive="{Binding IsDragging}"
                              Color="Green" />
</Grid>
```

### Configure Filter Tab (Refactored - 99% Same!)

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="300"/> <!-- Item Shelf -->
        <ColumnDefinition Width="*"/>   <!-- Right Column -->
    </Grid.ColumnDefinitions>

    <!-- Item Shelf (SAME reusable component, just ShowBanButton=true) -->
    <controls:ItemShelfControl Grid.Column="0"
                               ItemType="Joker"
                               Items="{Binding AvailableJokers}"
                               ShowBanButton="True" /> <!-- Only difference! -->

    <!-- Right Column -->
    <Grid Grid.Column="1">
        <!-- Default State: MUST + MUST NOT -->
        <StackPanel IsVisible="{Binding EditingState, Converter={StaticResource IsDefaultState}}">
            <controls:ClauseTrayControl ClauseType="MUST"
                                        Items="{Binding MustItems}" />

            <controls:ClauseTrayControl ClauseType="MUST NOT"
                                        Items="{Binding MustNotItems}" />
        </StackPanel>

        <!-- Clause Editing State -->
        <controls:ClauseTrayControl IsVisible="{Binding EditingState, Converter={StaticResource IsClauseEditState}}"
                                    IsExpanded="True"
                                    ClauseType="{Binding EditingClauseType}"
                                    Items="{Binding EditingClauseItems}" />
    </Grid>

    <!-- Drop Zone Overlays -->
    <controls:DropZoneOverlay TargetControl="{Binding #MustTray}"
                              IsActive="{Binding IsDragging}"
                              Color="Blue" />
    <controls:DropZoneOverlay TargetControl="{Binding #MustNotTray}"
                              IsActive="{Binding IsDragging}"
                              Color="Red" />
</Grid>
```

**See how similar they are?!** 90% of the code is IDENTICAL!

---

## 🎯 Part 6: ViewModels

### Shared Base ViewModel

```csharp
public abstract class FilterConfigBaseViewModel : ObservableObject
{
    // UI State
    [ObservableProperty]
    private EditingState _editingState = EditingState.Default;

    [ObservableProperty]
    private bool _isDragging = false;

    // Item Shelf
    [ObservableProperty]
    private ObservableCollection<BalatroCard> _availableJokers = new();

    [ObservableProperty]
    private EditionType _selectedEdition = EditionType.None;

    [ObservableProperty]
    private StickerType _selectedSticker = StickerType.None;

    [ObservableProperty]
    private SealType _selectedSeal = SealType.None;

    // Commands
    public IRelayCommand<EditionType> ApplyEditionCommand { get; }
    public IRelayCommand<StickerType> ApplyStickerCommand { get; }
    public IRelayCommand<SealType> ApplySealCommand { get; }
    public IRelayCommand<ClauseType> EditClauseCommand { get; }
    public IRelayCommand EditScoreCommand { get; }
    public IRelayCommand ExitEditingCommand { get; }

    // Methods
    protected abstract void OnItemDropped(FilterItem item, DropZoneType zone);
    protected abstract void OnItemRemoved(FilterItem item);

    private void ApplyEditionToAll(EditionType edition)
    {
        foreach (var card in AvailableJokers)
        {
            if (CanApplyEdition(card, edition))
            {
                card.Edition = edition;
            }
        }
    }

    private bool CanApplyEdition(BalatroCard card, EditionType edition)
    {
        // Eternal logic
        if (edition == EditionType.Eternal)
        {
            return CanBeEternal(card.Type);
        }

        // Negative logic
        if (edition == EditionType.Negative)
        {
            return card.CardType == CardType.Joker;
        }

        return true;
    }

    private bool CanBeEternal(MotelyItemType joker)
    {
        return joker != MotelyItemType.Cavendish &&
               joker != MotelyItemType.DietCola &&
               joker != MotelyItemType.GrosMichel &&
               joker != MotelyItemType.IceCream &&
               joker != MotelyItemType.InvisibleJoker &&
               joker != MotelyItemType.Luchador &&
               joker != MotelyItemType.MrBones &&
               joker != MotelyItemType.Popcorn &&
               joker != MotelyItemType.Ramen &&
               joker != MotelyItemType.Seltzer &&
               joker != MotelyItemType.TurtleBean;
    }
}

public enum EditingState
{
    Default,      // Normal view
    DragActive,   // Dragging item
    ClauseEdit,   // Editing OR/AND/MUST/MUST NOT clause
    ScoreEdit     // Editing score list (Configure Score only)
}

public enum EditionType
{
    None,
    Foil,
    Holographic,
    Polychrome,
    Negative,
    Debuffed  // For banned items
}

public enum StickerType
{
    None,
    Perishable,
    Eternal,
    Rental
}

public enum SealType
{
    None,
    Purple,
    Gold,
    Red,
    Blue
}
```

### ConfigureScoreTabViewModel (Extends Base)

```csharp
public class ConfigureScoreTabViewModel : FilterConfigBaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<FilterItem> _orItems = new();

    [ObservableProperty]
    private ObservableCollection<FilterItem> _andItems = new();

    [ObservableProperty]
    private ObservableCollection<FilterItem> _shouldItems = new();

    public bool HasOrItems => OrItems.Count > 0;
    public bool HasAndItems => AndItems.Count > 0;

    protected override void OnItemDropped(FilterItem item, DropZoneType zone)
    {
        switch (zone)
        {
            case DropZoneType.OR:
                OrItems.Add(item);
                AndItems.Clear(); // Hide AND when OR active
                break;
            case DropZoneType.AND:
                AndItems.Add(item);
                OrItems.Clear(); // Hide OR when AND active
                break;
            case DropZoneType.Score:
                ShouldItems.Add(item);
                break;
        }
    }
}
```

### ConfigureFilterTabViewModel (Extends Base)

```csharp
public class ConfigureFilterTabViewModel : FilterConfigBaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<FilterItem> _mustItems = new();

    [ObservableProperty]
    private ObservableCollection<FilterItem> _mustNotItems = new();

    protected override void OnItemDropped(FilterItem item, DropZoneType zone)
    {
        switch (zone)
        {
            case DropZoneType.Must:
                MustItems.Add(item);
                break;
            case DropZoneType.MustNot:
                MustNotItems.Add(item);
                break;
        }
    }
}
```

---

## 🎨 Part 7: Styling

### Clause Tray Styles

```xml
<Style Selector="Border.clause-tray">
    <Setter Property="Background" Value="{StaticResource DarkGrey}"/>
    <Setter Property="CornerRadius" Value="8"/>
    <Setter Property="Padding" Value="12"/>
    <Setter Property="Margin" Value="0,4"/>
</Style>

<Style Selector="Border.clause-tray.OR">
    <Setter Property="BorderBrush" Value="{StaticResource Green}"/>
    <Setter Property="BorderThickness" Value="2"/>
</Style>

<Style Selector="Border.clause-tray.AND">
    <Setter Property="BorderBrush" Value="{StaticResource Blue}"/>
    <Setter Property="BorderThickness" Value="2"/>
</Style>

<Style Selector="Border.clause-tray.MUST">
    <Setter Property="BorderBrush" Value="{StaticResource Blue}"/>
    <Setter Property="BorderThickness" Value="2"/>
</Style>

<Style Selector="Border.clause-tray.MUST_NOT">
    <Setter Property="BorderBrush" Value="{StaticResource Red}"/>
    <Setter Property="BorderThickness" Value="2"/>
</Style>

<!-- Bracket decoration on left -->
<Style Selector="Border.bracket-left">
    <Setter Property="Width" Value="4"/>
    <Setter Property="Height" Value="100%"/>
    <Setter Property="Background" Value="{StaticResource Gold}"/>
    <Setter Property="HorizontalAlignment" Value="Left"/>
    <Setter Property="CornerRadius" Value="2,0,0,2"/>
</Style>
```

### Edition Button Styles

```xml
<Style Selector="Button.edition-button">
    <Setter Property="Width" Value="48"/>
    <Setter Property="Height" Value="48"/>
    <Setter Property="Margin" Value="2"/>
    <Setter Property="Background" Value="{StaticResource DarkGrey}"/>
    <Setter Property="BorderBrush" Value="{StaticResource ModalBorder}"/>
    <Setter Property="BorderThickness" Value="2"/>
    <Setter Property="CornerRadius" Value="4"/>
    <Setter Property="Cursor" Value="Hand"/>
</Style>

<Style Selector="Button.edition-button:pointerover">
    <Setter Property="Background" Value="{StaticResource ModalBorder}"/>
</Style>

<Style Selector="Button.edition-button.selected">
    <Setter Property="BorderBrush" Value="{StaticResource Gold}"/>
    <Setter Property="BorderThickness" Value="3"/>
</Style>
```

---

## 📋 Part 8: Implementation Order

### Phase 1: Fix Current Issues (1-2 hours)
1. ✅ Fix OR/AND tray layout (vertical stack)
2. ✅ Fix bracket position (left side)
3. ✅ Fix drop zone overlay boundaries
4. ✅ Implement "hide other tray" logic

### Phase 2: UI States System (2-3 hours)
1. ✅ Create EditingState enum
2. ✅ Add state management to ViewModels
3. ✅ Implement Default/Drag/ClauseEdit/ScoreEdit states
4. ✅ Add expand/collapse transitions
5. ✅ Test state transitions

### Phase 3: Edition Selectors (3-4 hours)
1. ✅ Create edition button UI
2. ✅ Add edition sprites (Foil/Holo/Poly/Negative/Debuffed)
3. ✅ Add sticker sprites (Perishable/Eternal/Rental)
4. ✅ Add seal sprites (Purple/Gold/Red/Blue)
5. ✅ Implement CanBeEternal logic
6. ✅ Wire up "apply to all" commands
7. ✅ Test edition application

### Phase 4: Reusable Components (3-4 hours)
1. ✅ Create ItemShelfControl component
2. ✅ Create ClauseTrayControl component
3. ✅ Create ScoreListControl component
4. ✅ Create DropZoneOverlay component
5. ✅ Create FilterConfigBaseViewModel
6. ✅ Refactor ConfigureScoreTabViewModel
7. ✅ Refactor ConfigureFilterTabViewModel

### Phase 5: Integration & Testing (2 hours)
1. ✅ Replace old tabs with new components
2. ✅ Test all drag/drop scenarios
3. ✅ Test edition selectors
4. ✅ Test UI state transitions
5. ✅ Fix any bugs
6. ✅ Polish animations

**Total Estimated Time:** 11-15 hours

---

## ✅ Success Criteria

### Must Work:
- ✅ OR and AND trays stack vertically
- ✅ Bracket "[" on left side
- ✅ Only one clause active at a time
- ✅ Drop zone overlays don't overlap incorrectly
- ✅ Clause editing state expands to full column
- ✅ Score editing state expands to full column
- ✅ Edition buttons apply to all items in shelf
- ✅ Eternal respects CanBeEternal logic
- ✅ Both tabs use same components
- ✅ Build succeeds with 0 errors

### Must Feel Good:
- ✅ Smooth transitions between states
- ✅ Clear visual feedback
- ✅ Spacious editing experience
- ✅ Professional, polished UI
- ✅ Balatro-styled components

---

## 🎉 Expected Results

**Before:**
- Cramped UI
- Confusing layout
- OR/AND side by side (weird)
- Drop zones overlap everything
- Duplicate code between tabs
- No edition selectors

**After:**
- Spacious, comfortable editing
- Clear, intuitive layout
- OR/AND vertical stack (genius!)
- Proper drop zone boundaries
- Shared components (DRY)
- Quick edition application
- Professional UX

**USER HAPPINESS:** 📈📈📈

---

## 🚨 Important Notes

### Don't Fuck Up:
1. **Preserve JSON format** - Must/Should/MustNot unchanged
2. **Maintain MVVM** - No code-behind hacks
3. **Use real components** - No copy/paste duplication
4. **Test thoroughly** - All drag/drop/edit scenarios
5. **Keep Balatro styling** - Use existing color resources
6. **Build must succeed** - Zero errors, zero warnings

### Eternal Logic Reference:
```csharp
// These jokers CANNOT be Eternal:
MotelyItemType[] cannotBeEternal =
{
    MotelyItemType.Cavendish,
    MotelyItemType.DietCola,
    MotelyItemType.GrosMichel,
    MotelyItemType.IceCream,
    MotelyItemType.InvisibleJoker,
    MotelyItemType.Luchador,
    MotelyItemType.MrBones,
    MotelyItemType.Popcorn,
    MotelyItemType.Ramen,
    MotelyItemType.Seltzer,
    MotelyItemType.TurtleBean
};
```

---

## 📝 Future Enhancements (Post-Release)

### Possible Improvements:
1. "Ban?" checkbox instead of MUST NOT zone (simplifies Filter tab)
2. Drag entire clause to reorder
3. Duplicate clause button
4. Import/Export individual clauses
5. Preset edition combos ("All Negative", "All Eternal Poly", etc.)

---

**STATUS:** READY TO IMPLEMENT
**COMPLEXITY:** HIGH
**PAYOFF:** MASSIVE
**USER QUOTE:** "I am a fucking GENIUS! :D"

---

**LET'S FUCKING GO!** 🚀
