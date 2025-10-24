# Balatro Seed Oracle – Copilot Instructions

## Core Architecture
- Desktop Avalonia 11 app; entry point in src/Program.cs initializes Velopack updates, enables DebugLogger, then starts App.
- App.OnFrameworkInitializationCompleted wires DI via Extensions/ServiceCollectionExtensions.AddBalatroSeedOracleServices, ensures JsonItemFilters/SearchResults directories, starts SoundFlow audio, and cleans up SearchManager on shutdown.
- ServiceHelper.GetRequiredService<T>() bridges views to the DI container when constructor injection is not possible.

## Key Services & Data
- SearchManager manages one SearchInstance per {filter}_{deck}_{stake}; each gets a DuckDB database under SearchResults/ and must be disposed via StopSearchesForFilter when configs change.
- FilterSerializationService centralizes MotelyJsonConfig serialization (score defaults, compact arrays, hardened JSON); use it instead of ad-hoc json writes.
- ConfigurationService + FilterService resolve paths under JsonItemFilters/, filter naming, and validation; rely on these for file I/O.
- UserProfileService, FavoritesService, FeatureFlagsService persist JSON alongside the executable or in %AppData%; reuse their APIs for new preferences rather than rolling custom storage.

## UI & MVVM Patterns
- ViewModels use CommunityToolkit.Mvvm source generators—mark classes partial and prefer [ObservableProperty]/[RelayCommand] (see BaseWidgetViewModel).
- Set view DataContext via DI; Visuals typically call ServiceHelper in code-behind only for hookup (e.g., Components/FilterTabs/VisualBuilderTab).
- Add x:DataType and x:CompileBindings="True" per view because AvaloniaUseCompiledBindingsByDefault=false; compiled bindings catch typos at build time.
- Desktop widgets inherit BaseWidgetViewModel to get minimize/drag/notification behavior; follow WIDGET_STYLE_GUIDE.md for size and styling.
- VisualBuilderTab keeps heavy UI behavior (TopLevel drag/drop workarounds, sound cues) in the UserControl due to Avalonia bugs—leave business state in VisualBuilderTabViewModel.

## Search Pipeline
- SearchModalViewModel produces SearchCriteria and MotelyJsonConfig, then calls SearchManager.StartSearchAsync; the search writes results asynchronously.
- SearchInstance uses thread-local DuckDB appenders; call ForceFlush or the provided GetTopResultsAsync before reading to ensure buffered rows are visible.
- SearchResult models live in Models/SearchResult* and align with Motely output; updates should respect existing ObservableCollection usage for UI refresh.
- StopAllSearches runs during shutdown; call SearchManager.StopSearchesForFilter when filters are edited to avoid corrupting the per-filter databases.

## Assets & Feature Flags
- SpriteService loads sprites via avares:// URIs with disk fallbacks; new art needs both metadata JSON (Assets/**/metadata.json) and the sheet added to the csproj.
- Audio goes through SoundFlowAudioManager (DI singleton) and SoundEffectService.Instance; dispose audio on shutdown to prevent lockups.
- Feature flags live in feature_flags.json; query or toggle them through FeatureFlagsService.Instance so UI and persistence stay in sync.
- FavoritesService and WordLists/ support prebuilt combos and word lists used by the visual builder and CLI; keep naming normalized (lowercase keys).

## Build & Run
- Initialize the Motely submodule (`git submodule update --init --recursive`) before building; BalatroSeedOracle.csproj references external/Motely/Motely.csproj.
- Build or run with `dotnet build src/BalatroSeedOracle.csproj` and `dotnet run -c Debug|Release --project src/BalatroSeedOracle.csproj`; Release is significantly faster for seed searches.
- App.EnsureDirectoriesExist creates JsonItemFilters/ and SearchResults/ relative to the exe; tests or tooling creating filter files should mirror that layout.

## When Extending
- Register new services/viewmodels in Extensions/ServiceCollectionExtensions.cs so they resolve via DI and can be fetched with ServiceHelper.
- Use FilterSerializationService + JsonEditorTabViewModel.AutoGenerateFromVisual to keep Visual Builder, JSON editor, and persisted filters aligned.
- Prefer DebugLogger.Log* for instrumentation—calls compile away outside DEBUG builds, so avoid Console.WriteLine.
- Marshal UI changes through Avalonia.Threading.Dispatcher.UIThread; background work (file I/O, HTTP) should stay off the UI thread.
- Follow existing async patterns (fire-and-forget Task.Run + UIThread.Invoke) when loading sprite data or filter catalogs to avoid blocking startup.
