# Remove ALL FontWeight Properties (Constitution Violation)

## Problem
m6x11plusplus font doesn't support Bold/ExtraBold. FontWeight properties do nothing and violate project constitution.

User removes them daily but they keep getting re-added by AI.

## Evidence
Found 17 instances:
- VisualBuilderTab.axaml: Lines 27, 142, 465, 512, 595, 628
- ConfigureFilterTab.axaml: Lines 26, 180, 536, 707, 848
- MusicMixerWidget.axaml: Lines 133, 213, 285, 357, 402, 447, 492, 537

## Solution
Remove EVERY instance of `FontWeight="Bold"` and `FontWeight="ExtraBold"`.

Use `sed` for bulk removal:
```bash
sed -i 's/ FontWeight="[^"]*"//g' src/Components/FilterTabs/VisualBuilderTab.axaml
sed -i 's/ FontWeight="[^"]*"//g' src/Components/FilterTabs/ConfigureFilterTab.axaml
sed -i 's/ FontWeight="[^"]*"//g' src/Components/Widgets/MusicMixerWidget.axaml
```

Then fix the 2 remaining Style Setter instances manually.

## Files
- X:\BalatroSeedOracle\src\Components\FilterTabs\VisualBuilderTab.axaml
- X:\BalatroSeedOracle\src\Components\FilterTabs\ConfigureFilterTab.axaml
- X:\BalatroSeedOracle\src\Components\Widgets\MusicMixerWidget.axaml

## Acceptance
- [ ] `grep -r "FontWeight" src --include="*.axaml"` returns 0 results
- [ ] Visual appearance unchanged (font doesn't support bold anyway)
- [ ] Build succeeds
