# Settings Modal Improvement Plan

## Problem Statement
The current settings modal is "literally so shitty and annoying" with feature flags that need to be removed. The user wants a cleaner, more streamlined settings experience without experimental feature toggles cluttering the UI.

## Current State Analysis

### Files Involved
- `src/Services/FeatureFlagsService.cs` - Feature flag management system
- `src/ViewModels/SettingsModalViewModel.cs` - Settings modal (currently ONLY has visualizer theme picker)
- `src/Views/Modals/SettingsModal.axaml` - Settings modal UI
- `src/Components/Widgets/GenieWidget.axaml.cs` - Uses GENIE_ENABLED flag
- `src/Components/Widgets/DayLatroWidget.axaml.cs` - Uses DAYLATRO_ENABLED flag

### Current Feature Flags (from FeatureFlagsService.cs)
1. **GenieEnabled** - AI Assistant (Genie) - OFF by default
2. **DaylatroEnabled** - Daylatro Mode (daily challenges) - ON by default
3. **ShaderBackgrounds** - Animated Shader Backgrounds - ON by default
4. **VisualizerWidget** - Visualizer Widget - Not referenced
5. **ExperimentalSearch** - Experimental Search Features - OFF by default
6. **DebugMode** - Debug Mode (verbose logging) - OFF by default

### Current Settings Modal Contents
The SettingsModalViewModel ONLY contains:
- VisualizerTheme selection (0-N theme picker)
- "Advanced Settings" button that opens a separate modal
- Close button

**NO FEATURE FLAGS ARE IN THE CURRENT SETTINGS MODAL!** They must be in a different location (probably the "Advanced Settings" modal or widgets themselves).

## Problems Identified

### 1. Feature Flags are Anti-Pattern
- Feature flags are meant for A/B testing or gradual rollouts
- They create technical debt and code complexity
- Users shouldn't toggle "experimental" features - they should just work
- Creating a bifurcated codebase (flag on vs flag off) doubles testing burden

### 2. Settings Modal Too Minimal
The current settings modal only has:
- Theme picker
- "Advanced Settings" button

This is TOO minimal. Users expect to see:
- Audio settings (volume, track mixing)
- Visual settings (shader effects, animations)
- Performance settings
- UI preferences

### 3. Missing Proper Settings Architecture
No centralized settings service that manages:
- User preferences persistence
- Settings validation
- Default values
- Settings change events

## Proposed Solution

### Phase 1: Remove Feature Flags Entirely

**Goal:** Delete FeatureFlagsService and remove all flag checks from codebase.

**Approach:**
1. **Genie Widget** - Either fully implement and enable, or remove entirely
   - If keeping: Remove flag, make it always available
   - If removing: Delete GenieWidget files completely

2. **Daylatro Widget** - Already ON by default, make permanent
   - Remove flag check
   - Make feature always available

3. **Shader Backgrounds** - Already ON by default, make permanent
   - Remove flag check
   - Add proper settings toggle for "Enable Shader Effects" in settings modal

4. **Experimental Search** - Either promote to stable or remove
   - If stable: Remove flag, enable permanently
   - If broken: Remove experimental code entirely

5. **Debug Mode** - Move to proper logging configuration
   - Add `appsettings.json` with log levels
   - Remove feature flag approach

6. **Delete FeatureFlagsService.cs entirely**

**Files to Modify:**
- DELETE `src/Services/FeatureFlagsService.cs`
- `src/Components/Widgets/GenieWidget.axaml.cs` - Remove flag check or delete widget
- `src/Components/Widgets/DayLatroWidget.axaml.cs` - Remove flag check
- `src/Extensions/ServiceCollectionExtensions.cs` - Remove FeatureFlags service registration
- Any shader/background code using ShaderBackgrounds flag
- Any search code using ExperimentalSearch flag

### Phase 2: Redesign Settings Modal

**Goal:** Create a comprehensive, user-friendly settings modal with proper organization.

**New Settings Structure:**

```
SETTINGS MODAL
├─ AUDIO
│  ├─ Master Volume (slider 0-100%)
│  ├─ Individual Track Volumes
│  │  ├─ Bass 1 / Bass 2
│  │  ├─ Drums 1 / Drums 2
│  │  ├─ Chords 1 / Chords 2
│  │  └─ Melody 1 / Melody 2
│  └─ Mute All (toggle)
│
├─ VISUALS
│  ├─ Visualizer Theme (dropdown: 0-8)
│  ├─ Enable Shader Effects (toggle)
│  ├─ Animation Speed (slider: 0.5x - 2.0x)
│  └─ Background Opacity (slider: 0-100%)
│
├─ PERFORMANCE
│  ├─ Enable Hardware Acceleration (toggle)
│  ├─ Max FPS (dropdown: 30/60/120/Unlimited)
│  └─ Reduce Animations (toggle)
│
└─ PREFERENCES
   ├─ Author Name (text input)
   ├─ Auto-save Filters (toggle)
   ├─ Show Tooltips (toggle)
   └─ Confirm Before Delete (toggle)
```

**Implementation Approach:**

1. **Create UserSettingsService** (replaces FeatureFlagsService)
   - Manages ALL user settings
   - Persists to `user_settings.json` (NOT feature_flags.json)
   - Provides strongly-typed settings access
   - Raises events when settings change

2. **Create Settings Models**
   ```csharp
   public class UserSettings
   {
       public AudioSettings Audio { get; set; } = new();
       public VisualSettings Visual { get; set; } = new();
       public PerformanceSettings Performance { get; set; } = new();
       public PreferencesSettings Preferences { get; set; } = new();
   }

   public class AudioSettings
   {
       public double MasterVolume { get; set; } = 100.0;
       public Dictionary<string, double> TrackVolumes { get; set; } = new();
       public bool MuteAll { get; set; } = false;
   }

   public class VisualSettings
   {
       public int ThemeIndex { get; set; } = 0;
       public bool ShaderEffectsEnabled { get; set; } = true;
       public double AnimationSpeed { get; set; } = 1.0;
       public double BackgroundOpacity { get; set; } = 100.0;
   }

   public class PerformanceSettings
   {
       public bool HardwareAcceleration { get; set; } = true;
       public int MaxFPS { get; set; } = 60;
       public bool ReduceAnimations { get; set; } = false;
   }

   public class PreferencesSettings
   {
       public string AuthorName { get; set; } = "";
       public bool AutoSaveFilters { get; set; } = true;
       public bool ShowTooltips { get; set; } = true;
       public bool ConfirmBeforeDelete { get; set; } = true;
   }
   ```

3. **Redesign SettingsModal.axaml**
   - Use TabControl for categories (Audio, Visuals, Performance, Preferences)
   - Clean, Balatro-themed UI with proper spacing
   - Real-time preview for visual changes
   - "Reset to Defaults" button per category
   - "Save" and "Cancel" buttons

4. **Update SettingsModalViewModel.cs**
   - Add properties for all settings categories
   - Implement save/cancel logic
   - Implement reset to defaults
   - Raise events for settings changes
   - Validate settings values

### Phase 3: Migrate Existing Settings

**Goal:** Move existing settings from UserProfileService to new UserSettingsService.

**Current Settings in UserProfile:**
- Author name
- Visualizer theme
- Audio volume/mute state

**Migration Strategy:**
1. Keep UserProfileService for filter-related data (filters, search history)
2. Move UI/app settings to UserSettingsService
3. Add migration code to copy settings from old location to new on first run
4. Update all code that accesses settings to use new service

## Implementation Order

### Step 1: Feature Flag Removal (High Priority)
1. Audit all usages of FeatureFlagsService in codebase
2. For each feature:
   - Decide: Keep permanently enabled OR remove entirely
   - Remove flag check
   - Test functionality
3. Delete FeatureFlagsService.cs
4. Remove from ServiceCollectionExtensions

**Estimated Time:** 2-3 hours

### Step 2: Create UserSettingsService (Medium Priority)
1. Create `src/Services/UserSettingsService.cs`
2. Create settings models (AudioSettings, VisualSettings, etc.)
3. Implement JSON persistence
4. Add to dependency injection
5. Write unit tests

**Estimated Time:** 3-4 hours

### Step 3: Redesign Settings Modal UI (Medium Priority)
1. Create new SettingsModal.axaml with tabbed layout
2. Add all settings controls (sliders, toggles, dropdowns)
3. Apply Balatro theme styling
4. Add real-time previews

**Estimated Time:** 4-5 hours

### Step 4: Update SettingsModalViewModel (Medium Priority)
1. Add properties for all settings
2. Implement save/cancel/reset logic
3. Add validation
4. Wire up events

**Estimated Time:** 2-3 hours

### Step 5: Migrate Existing Settings (Low Priority)
1. Add migration code for first run
2. Update all code accessing old settings
3. Test thoroughly
4. Remove old settings locations

**Estimated Time:** 2-3 hours

## Success Criteria

### Must Have
- [ ] FeatureFlagsService completely removed from codebase
- [ ] All feature flag checks removed
- [ ] Settings modal has Audio, Visual, Performance, Preferences tabs
- [ ] All settings persist to user_settings.json
- [ ] Settings changes apply immediately (no app restart)
- [ ] "Reset to Defaults" works for each category

### Nice to Have
- [ ] Real-time visual preview of theme/shader changes
- [ ] Settings search/filter
- [ ] Import/Export settings
- [ ] Settings presets (Performance, Balanced, Quality)

## Testing Plan

1. **Feature Flag Removal**
   - Verify each feature works without flag checks
   - Test both "enabled" and "removed" paths

2. **Settings Persistence**
   - Change settings, close app, reopen - verify persistence
   - Delete user_settings.json, verify defaults apply
   - Corrupt user_settings.json, verify graceful fallback

3. **Settings UI**
   - Test all controls (sliders, toggles, dropdowns)
   - Verify real-time updates
   - Test Reset to Defaults
   - Test Save/Cancel behavior

4. **Migration**
   - Test first run with existing userprofile.json
   - Verify settings migrate correctly
   - Verify no data loss

## Risks & Mitigation

### Risk 1: Breaking Existing Features
**Mitigation:** Thorough testing of each feature before/after flag removal

### Risk 2: Settings Migration Failures
**Mitigation:**
- Add extensive logging
- Provide manual migration tool
- Keep backups of old settings files

### Risk 3: UI Complexity
**Mitigation:**
- Use tabbed layout to organize settings
- Add tooltips and descriptions
- Provide "Simple" vs "Advanced" view modes

## Notes
- User explicitly wants feature flags GONE - this is non-negotiable
- Settings modal should feel polished and professional
- Consider adding settings documentation/help
- May want to add settings import/export for power users
