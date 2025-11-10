# Convert All Buttons from TextBlock Elements to Content Property

## Problem
30 files have buttons with explicit `<TextBlock>` child elements instead of using Content property. This enables text selection and is over-engineered.

## Evidence
Working pattern (tabs): `<TabItem Header="Filter Config"/>`
Broken pattern (30 files): `<Button><TextBlock Text="OK"/></Button>`

## Root Cause
Over-engineering. Explicit TextBlock elements create selectable text surface.

## Solution
Convert all buttons to use Content property pattern.

**Before:**
```xml
<Button Classes="btn-red">
    <TextBlock Text="OK" FontSize="16" FontFamily="{StaticResource BalatroFont}"/>
</Button>
```

**After:**
```xml
<Button Classes="btn-red"
        Content="OK"
        FontSize="16"/>
```

FontFamily flows from global Button style. No explicit binding needed.

## Files (30 total)
1. ✅ src/Views/BalatroMainMenu.axaml - DONE
2. src/Components/FilterTabs/ConfigureFilterTab.axaml
3. src/Components/FilterTabs/VisualBuilderTab.axaml
4. src/Components/Widgets/MusicMixerWidget.axaml
5. src/Components/Widgets/AudioVisualizerSettingsWidget.axaml
6. src/Components/Widgets/FrequencyDebugWidget.axaml
7. src/Components/Widgets/AudioMixerWidget.axaml
8. src/Views/Modals/FilterSelectionModal.axaml
9. src/Components/FilterTabs/SaveFilterTab.axaml
10. src/Views/SearchModalTabs/SearchTab.axaml
11. src/Views/Modals/SettingsModal.axaml
12. src/Controls/SortableResultsGrid.axaml
13. src/Components/Widgets/DayLatroWidget.axaml
14. src/Components/Widgets/GenieWidget.axaml
15. src/Components/FilterSelectorControl.axaml
16. src/Components/Widgets/BaseWidget.axaml
17. src/Views/Modals/AudioVisualizerSettingsModal.axaml
18. src/Components/PaginatedFilterBrowser.axaml
19. src/Controls/SpinnerControl.axaml
20. src/Features/Analyzer/AnalyzerView.axaml
21. src/Views/Modals/AnalyzeModal.axaml
22. src/Views/Modals/WordListsModal.axaml
23. src/Controls/PanelSpinner.axaml
24. src/Views/Modals/ToolsModal.axaml
25. src/Controls/MaximizeButton.axaml
26. src/Controls/AnteSelector.axaml
27. src/Components/BalatroWidget.axaml
28. src/Controls/PlayingCardSelector.axaml
29. src/Windows/DataGridResultsWindow.axaml
30. SearchModal_backup.axaml

## Strategy
Process files in order. For each file:
1. Read file, find all `<Button>` tags
2. Check if they have `<TextBlock>` child elements
3. Convert to Content property, preserving FontSize from TextBlock
4. Remove FontFamily (flows from global style)
5. Test build

## Acceptance
- [ ] No buttons have explicit TextBlock children
- [ ] All button text displays at correct sizes
- [ ] No text selection possible on any buttons
- [ ] Build succeeds
- [ ] Visual appearance unchanged
