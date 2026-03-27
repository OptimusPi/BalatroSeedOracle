# PRD-12: Services & Platform Layer

## Summary

The foundational services layer providing dependency injection, platform abstraction, data persistence, sprite loading, configuration management, and cross-cutting concerns. This layer has no UI of its own but is consumed by all other subsystems.

---

## Current Implementation (Legacy Reference)

### Core Services

| File | Role |
|------|------|
| `Services/ConfigurationService.cs` | App configuration (settings, preferences) |
| `Services/UserProfileService.cs` | User profile persistence (author name, prefs) |
| `Services/SpriteService.cs` | Balatro sprite/asset loading |
| `Services/NotificationService.cs` | Toast notification dispatch |
| `Services/SoundEffectsService.cs` | UI sound effects |
| `Services/FavoritesService.cs` | Favorites management |
| `Services/DaylatroSeeds.cs` | Daily challenge seed generation |
| `Services/DaylatroHighScoreService.cs` | Daily challenge scores |
| `Services/MinigameDownloadService.cs` | Minigame downloads |
| `Services/DbListExportService.cs` | Export to database/CSV/parquet |
| `Services/CircularConsoleBuffer.cs` | Debug console buffer |

### Platform Abstraction

| File | Role |
|------|------|
| `Services/IPlatformServices.cs` | Platform capability interface |
| `Services/PlatformServices.cs` | Base implementation |
| `Desktop/Services/DesktopPlatformServices.cs` | Desktop implementation |
| `Desktop/Services/DesktopPlatformServicesNative.cs` | Native desktop features |
| `Services/IApiHostService.cs` | API hosting interface |
| `Desktop/Services/DesktopApiHostService.cs` | Desktop API host |
| `Services/ISequentialLibraryInitializer.cs` | Library loading interface |
| `Desktop/Services/SequentialLibraryInitializerService.cs` | Desktop library init |
| `Services/IRestoreActiveSearchesProvider.cs` | Search restore interface |
| `Desktop/Services/RestoreActiveSearchesProviderService.cs` | Desktop search restore |

### Data Storage

| File | Role |
|------|------|
| `Desktop/Services/DesktopAppDataStore.cs` | Desktop file storage |
| `Desktop/Services/DesktopAppDataStoreNative.cs` | Native desktop storage |
| `Desktop/Services/ResultsDatabaseExporter.cs` | Results DB export |

### DI Registration

| File | Role |
|------|------|
| `Extensions/ServiceCollectionExtensions.cs` | Core service registration |
| `Desktop/DesktopAppExtensions.cs` | Desktop-specific DI setup |

### Helpers

| File | Role |
|------|------|
| `Helpers/AppPaths.cs` | File path resolution |
| `Helpers/DebugLogger.cs` | Debug logging |
| `Helpers/ServiceHelper.cs` | Service locator utilities |
| `Helpers/TopLevelHelper.cs` | Top-level window access |
| `Helpers/CategoryMapper.cs` | Game item category mapping |

---

## Requirements

### R1 — Dependency Injection

All services registered via `IServiceCollection`:

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBalatroServices(this IServiceCollection services)
    {
        // Core services (cross-platform)
        services.AddSingleton<ConfigurationService>();
        services.AddSingleton<UserProfileService>();
        services.AddSingleton<SpriteService>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<SoundEffectsService>();
        services.AddSingleton<FavoritesService>();
        services.AddSingleton<FilterService>();
        services.AddSingleton<FilterCacheService>();
        services.AddSingleton<SearchManager>();
        services.AddSingleton<TransitionService>();
        services.AddSingleton<ShaderInertiaManager>();
        services.AddSingleton<VisualizerEventManager>();
        services.AddSingleton<EventFXService>();
        services.AddSingleton<WidgetPositionService>();
        services.AddSingleton<WidgetWindowManager>();
        // ... etc

        return services;
    }
}
```

**Desktop adds:**
```csharp
public static class DesktopAppExtensions
{
    public static IServiceCollection AddDesktopServices(this IServiceCollection services)
    {
        services.AddSingleton<IAudioManager, DesktopAudioManager>();
        services.AddSingleton<IPlatformServices, DesktopPlatformServices>();
        services.AddSingleton<IApiHostService, DesktopApiHostService>();
        services.AddSingleton<ISequentialLibraryInitializer, SequentialLibraryInitializerService>();
        services.AddSingleton<IRestoreActiveSearchesProvider, RestoreActiveSearchesProviderService>();
        // Desktop-only widgets, etc.

        return services;
    }
}
```

### R2 — IPlatformServices

```csharp
public interface IPlatformServices
{
    // Platform identity
    string PlatformName { get; }  // "Desktop", "Browser", "iOS", "Android"
    bool IsDesktop { get; }
    bool IsBrowser { get; }
    bool IsMobile { get; }

    // Capabilities
    bool SupportsAudio { get; }
    bool SupportsFFT { get; }
    bool SupportsFileSystem { get; }
    bool SupportsNativeWindows { get; }
    bool SupportsClipboard { get; }

    // Platform operations
    Task SetClipboardTextAsync(string text);
    Task<string?> GetClipboardTextAsync();
    Task OpenUrlAsync(string url);
    Task<string?> ShowOpenFileDialogAsync(string filter);
    Task<string?> ShowSaveFileDialogAsync(string defaultName, string filter);
}
```

### R3 — ConfigurationService

```csharp
public class ConfigurationService
{
    // App settings (persisted to JSON)
    T GetSetting<T>(string key, T defaultValue);
    void SetSetting<T>(string key, T value);
    void Save();

    // Observable for UI binding
    event Action<string>? SettingChanged;
}
```

Settings stored in `User/` directory or platform-appropriate location.

### R4 — UserProfileService

```csharp
public class UserProfileService
{
    UserProfile CurrentProfile { get; }
    void SaveProfile();
    void LoadProfile();
}

public class UserProfile
{
    string AuthorName { get; set; }
    // Preferences, display settings, etc.
}
```

Stored in `userprofile.json`.

### R5 — SpriteService

Loads Balatro game sprites for display in UI:

```csharp
public class SpriteService
{
    // Load sprite by category and name
    IImage? GetSprite(SpriteCategory category, string name);
    IImage? GetJokerSprite(string jokerName);
    IImage? GetTarotSprite(string tarotName);
    IImage? GetPlanetSprite(string planetName);
    IImage? GetDeckSprite(string deckName);
    IImage? GetStakeSprite(string stakeName);
    IImage? GetEditionOverlay(string edition);
    IImage? GetPlayingCard(string suit, string rank);

    // Initialization
    Task InitializeAsync();  // Load sprite sheets
    bool IsInitialized { get; }
}
```

Sprite categories: Jokers, Tarots, Planets, Spectrals, Vouchers, Decks, Stakes, Playing Cards, Editions, Tags, Bosses, Blinds (20+ types).

### R6 — NotificationService

```csharp
public class NotificationService
{
    void ShowSuccess(string message, TimeSpan? duration = null);
    void ShowError(string message, TimeSpan? duration = null);
    void ShowWarning(string message, TimeSpan? duration = null);
    void ShowInfo(string message, TimeSpan? duration = null);

    event Action<NotificationItem>? OnNotification;
}
```

Toast-style notifications, auto-dismiss after duration.

### R7 — FavoritesService

```csharp
public class FavoritesService
{
    void AddFavorite(string type, string id);
    void RemoveFavorite(string type, string id);
    bool IsFavorite(string type, string id);
    IReadOnlyList<string> GetFavorites(string type);
    void Save();
}
```

Types: "seed", "filter", "preset".

### R8 — AppPaths

```csharp
public static class AppPaths
{
    static string DataDirectory { get; }      // App data root
    static string FiltersDirectory { get; }   // JamlFilters/
    static string PresetsDirectory { get; }   // Presets/
    static string VisualizerPresetsDirectory { get; }  // VisualizerPresets/
    static string MixerPresetsDirectory { get; }       // MixerPresets/
    static string WordListsDirectory { get; } // WordLists/
    static string UserDirectory { get; }      // User/
}
```

### R9 — DebugLogger

```csharp
public static class DebugLogger
{
    static void Log(string category, string message);
    static void LogError(string category, string message);
    static void LogImportant(string category, string message);
    static void LogWarning(string category, string message);
}
```

- Writes to `CircularConsoleBuffer` (fixed-size ring buffer)
- Optional file output
- Filtered console output via `FilteredConsoleWriter`

### R10 — DayLatro (Daily Challenge)

```csharp
public class DaylatroSeeds
{
    string GetTodaysSeed();
    string GetSeedForDate(DateTime date);
}

public class DaylatroHighScoreService
{
    void RecordScore(string seed, int score);
    DaylatroHighScore? GetHighScore(string seed);
    DaylatroDailyScores GetDailyScores();
}
```

Deterministic daily seed generation + local high score tracking.

### R11 — Export Services

```csharp
public class DbListExportService
{
    Task ExportToCsvAsync(IEnumerable<SearchResult> results, string filePath);
    Task ExportToParquetAsync(IEnumerable<SearchResult> results, string filePath);
}
```

### R12 — Sequential Library Initialization

```csharp
public interface ISequentialLibraryInitializer
{
    Task InitializeAsync(IProgress<string>? progress = null);
}
```

- Desktop: loads native libraries (DuckDB, audio libs) in sequence
- Reports progress for loading screen
- Handles missing library errors gracefully

### R13 — IApiHostService

```csharp
public interface IApiHostService
{
    Task StartAsync(string url);
    Task StopAsync();
    bool IsRunning { get; }
    string? CurrentUrl { get; }
    event Action<bool>? OnStatusChanged;
}
```

Hosts a local API endpoint for remote search clients.

---

## Acceptance Criteria

- [ ] All services register correctly via DI
- [ ] Platform services detect correct platform
- [ ] Configuration persists across app restarts
- [ ] User profile saves/loads correctly
- [ ] Sprites load for all 20+ categories
- [ ] Notifications display and auto-dismiss
- [ ] Favorites persist across sessions
- [ ] AppPaths resolve correctly on all platforms
- [ ] DebugLogger captures categorized messages
- [ ] Daily seeds are deterministic (same seed for same date)
- [ ] Export to CSV/Parquet works
- [ ] Library initialization reports progress
- [ ] API host starts/stops cleanly
