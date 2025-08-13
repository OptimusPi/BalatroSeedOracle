# TODO List for BalatroSeedOracle

## CODE FREEZE - BUG FIXES ONLY!

## Future Enhancements (POST-RELEASE)

### Performance Optimizations

#### Add Min Property to Filter Items
- **Location**: `OuijaConfig.FilterItem` class
- **Purpose**: Allow filters to specify minimum count requirements (e.g., "at least 2 Blueprints")
- **Implementation**:
  - Add `int? Min` property to `OuijaConfig.FilterItem`
  - Update filter checking logic to early-exit when Min requirement is met
  - Example: `if (foundCount >= clause.Min) return true;`
- **Benefit**: Significant performance improvement by avoiding unnecessary checks
- **Example Config**:
  ```json
  {
    "Should": [
      {
        "Type": "Joker",
        "Value": "Blueprint", 
        "Min": 2,  // Need at least 2
        "Score": 10
      }
    ]
  }
  ```

### Auto-Cutoff Improvements
- Fix the overly aggressive auto-cutoff checking (currently checks every batch - wasteful!)
- Should check every ~1000 batches or every 10 seconds, not every single batch
- The constant "2.86%" hit rate spam indicates something is broken in the calculation
- Consider implementing a 60-second time limit on auto-cutoff adjustments

### Known Issues to Fix
- Auto-cutoff is too chatty in console output
- Auto-cutoff seems to get stuck at specific hit rates (2.86% = 1/35)
- Duplicate seed reporting when auto-cutoff increases

### UI/UX Improvements
- Better visual feedback when search is resumed vs fresh start
- Show batch progress in UI (current batch / total batches)
- Add ability to clear saved batch progress

### Database Improvements
- Add index on scores column for faster sorting
- Consider vacuum/analyze on database periodically
- Add ability to merge databases from different search sessions