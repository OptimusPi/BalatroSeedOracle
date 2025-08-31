# PRD: Filter Flow Fix - Final Build Issues

## 🎯 GOAL ACHIEVED
**Problem:** Redundant FilterCreationModal in filter selection flow  
**Solution:** Direct "Edit In Designer" + "Clone this filter" buttons in BalatroFilterSelector

## ✅ COMPLETED
- ✅ Updated BalatroFilterSelector.axaml with 2-button layout
- ✅ Implemented OnEditInDesignerClick() and OnCloneFilterClick() 
- ✅ Fixed JSON serialization switch expression (CS8506)
- ✅ Added proper null handling for Path.GetDirectoryName()
- ✅ Updated BalatroMainMenu.axaml.cs event handlers
- ✅ Removed redundant FilterCreationModal flow

## 🚨 REMAINING BUILD ERRORS

### 1. BalatroMainMenu.axaml.cs - Missing Using Statements
**Lines 182, 194, 228, 240:** `FontFamily`, `Brushes`, `TextAlignment`, `FontWeight` not found

**Fix Required:**
```csharp
using Avalonia.Media;        // For FontFamily, Brushes, FontWeight  
using Avalonia.Layout;       // For TextAlignment
```

### 2. BalatroFilterSelector.axaml.cs - CS8601 Null Warnings
**Lines 675, 708, 720, 732, 765, 777, 789:** Possible null assignments

**Fix Required:** Add null-conditional operators or explicit null checks

## 🔧 IMMEDIATE ACTIONS
1. **Add missing using statements** to BalatroMainMenu.axaml.cs
2. **Fix null reference warnings** in BalatroFilterSelector.axaml.cs  
3. **Test build** - should compile successfully
4. **Test functionality:**
   - Click "NEW FILTER" → BalatroFilterSelector opens
   - Select filter → Both buttons enabled
   - Click "Clone this filter" → Creates `-CLONE.json`, auto-selects
   - Click "Edit In Designer" → Opens Visual Filter Builder

## 📁 FILES TO MODIFY
- `src/Views/BalatroMainMenu.axaml.cs` (add using statements)
- `src/Components/BalatroFilterSelector.axaml.cs` (fix null warnings)

## 🗑️ CLEANUP AFTER SUCCESS
- Remove `src/Views/Modals/FilterCreationModal.axaml` 
- Remove `src/Views/Modals/FilterCreationModal.axaml.cs`

## 🎯 END STATE
**Clean, direct UX:** Main Menu → Filter Selector → Edit/Clone buttons → Visual Builder
**No redundant modals, proper clone functionality, auto-selection of clones**
