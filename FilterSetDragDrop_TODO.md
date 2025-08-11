# Filter Set Drag & Drop Feature - TODO List

## Feature Overview
Enable dragging filter sets (pre-defined and user-defined) from the Favorites tab and dropping them onto a unified drop zone to apply all items in the set at once.

## Current Implementation Status

### ✅ Already Implemented:
1. `_isDraggingSet` boolean field to track when a set is being dragged
2. `_draggingSet` field to store the current `FavoritesService.JokerSet` being dragged
3. `MergeDropZonesForSet()` method that creates a single unified drop zone with "Drop to apply set" text
4. `RestoreNormalDropZones()` method to restore the original 3-column layout after drag
5. Drag event handlers for the merged zone:
   - `OnMergedZoneDragEnter`
   - `OnMergedZoneDragLeave`
   - `OnMergedZoneDragOver`
   - `OnMergedZoneDrop`
6. Set drag initiation in the Favorites panel
7. Drop handler that distributes items to their appropriate zones (Must/Should/MustNot)

### ❌ Issues Found:
1. Regular drop zones (Needs/Wants/MustNot) still accept `JokerSet` drops when they should be hidden/inactive
2. The `_isDraggingSet` flag is set but never actually used to control behavior
3. Conflicting `DragDropEffects`:
   - Regular zones use `DragDropEffects.Move`
   - Merged zone uses `DragDropEffects.Copy`
4. This causes the 🚫 cursor when hovering over where the regular zones would be

## TODO List

### High Priority:
1. **Fix Regular Drop Zone Handlers**
   - Modify `OnNeedsDragOver`, `OnWantsDragOver`, `OnMustNotDragOver` to check `_isDraggingSet`
   - When `_isDraggingSet` is true, set `e.DragEffects = DragDropEffects.None`
   - This will prevent the regular zones from accepting drops when a set is being dragged

2. **Fix DragDropEffects Consistency**
   - Change set dragging to use `DragDropEffects.Copy` consistently
   - Update the drag initiation code in Favorites panel

### Medium Priority:
3. **Test Merged Drop Zone**
   - Verify that sets can be dropped successfully
   - Ensure items are distributed correctly to Must/Should/MustNot zones
   - Test with both zone-aware sets and legacy sets

4. **Handle Edge Cases**
   - What happens if drag is cancelled?
   - Ensure `RestoreNormalDropZones()` is always called
   - Handle window losing focus during drag

### Low Priority:
5. **Visual Enhancements**
   - Add better visual feedback for the merged drop zone
   - Consider animation when merging/restoring zones
   - Style the "Drop to apply set" text better

6. **Code Cleanup**
   - Remove the unused `_isDraggingSet` warning by implementing the checks
   - Consider refactoring the zone merging logic for clarity

## Implementation Notes
- The merged drop zone completely replaces the 3-column layout during set dragging
- The original drop zones are stored in `_originalDropZones` for restoration
- Sets can have zone-specific information (HasZoneInfo) or be legacy sets (all items go to Should/Wants)