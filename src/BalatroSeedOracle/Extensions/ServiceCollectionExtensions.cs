using System;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Services.Storage;
using BalatroSeedOracle.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BalatroSeedOracle.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBalatroSeedOracleServices(
            this IServiceCollection services
        )
        {
            // Services
            services.AddSingleton<IConfigurationService>(sp => new ConfigurationService(
                sp.GetRequiredService<IAppDataStore>(),
                sp.GetService<UserProfileService>(),
                sp.GetService<IFilterCacheService>(),
                sp.GetService<IPlatformServices>()
            ));

            // Note: IAppDataStore and IPlatformServices are registered by platform-specific projects (Desktop/Browser)
            // IApiHostService is also registered by platform-specific projects

            services.AddSingleton<IFilterCacheService>(sp => new FilterCacheService(
                sp.GetRequiredService<IAppDataStore>(),
                sp.GetService<IPlatformServices>()
            ));
            services.AddSingleton<IFilterService>(sp => new FilterService(
                sp.GetRequiredService<IConfigurationService>(),
                sp.GetRequiredService<IAppDataStore>(),
                sp.GetRequiredService<IFilterCacheService>(),
                sp.GetRequiredService<UserProfileService>(),
                sp.GetService<IPlatformServices>()
            ));
            services.AddSingleton<SpriteService>();
            services.AddSingleton<UserProfileService>(sp => new UserProfileService(
                sp.GetRequiredService<IAppDataStore>(),
                sp.GetRequiredService<IPlatformServices>()
            ));
            services.AddSingleton<FilterConfigurationService>();
            services.AddSingleton<SearchManager>();

            // Note: SoundFlowAudioManager and SoundEffectsService are registered by Desktop Program.cs only
            services.AddSingleton<TransitionService>();
            services.AddSingleton<TriggerService>();
            services.AddSingleton<EventFXService>();
            services.AddSingleton<SearchTransitionManager>(sp => new SearchTransitionManager(
                sp.GetRequiredService<TransitionService>(),
                sp.GetRequiredService<UserProfileService>(),
                sp.GetService<Views.MainWindow>(),
                sp.GetService<Views.BalatroMainMenu>()
            ));
            services.AddSingleton<FavoritesService>(sp => new FavoritesService(
                sp.GetService<IPlatformServices>()
            ));
            services.AddSingleton<DaylatroHighScoreService>(sp => new DaylatroHighScoreService(
                sp.GetService<IPlatformServices>()
            ));
            services.AddSingleton<FilterSerializationService>();
            services.AddSingleton<WidgetPositionService>();
            services.AddSingleton<WidgetWindowManager>();
            services.AddSingleton<NotificationService>();
            services.AddTransient<ClauseConversionService>();
            services.AddSingleton<Services.Export.ResultsExportService>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>(sp => new MainWindowViewModel(
                sp.GetRequiredService<UserProfileService>(),
                sp.GetRequiredService<SearchManager>()
            ));
            services.AddTransient<BalatroMainMenuViewModel>(sp => new BalatroMainMenuViewModel(
                sp.GetRequiredService<UserProfileService>(),
                sp.GetRequiredService<SearchModalViewModel>(),
                sp.GetRequiredService<FiltersModalViewModel>(),
                sp.GetRequiredService<CreditsModalViewModel>(),
                sp.GetRequiredService<Func<AnalyzeModalViewModel>>(),
                sp.GetRequiredService<WidgetWindowManager>(),
                sp.GetService<IAudioManager>(),
                sp.GetService<EventFXService>(),
                sp.GetService<WidgetPositionService>(),
                sp.GetService<IPlatformServices>(),
                () => sp.GetRequiredService<FilterSelectionModalViewModel>()
            ));

            // Views
            // Keep these as singletons so there is exactly one MainMenu instance shared by:
            // - MainWindow (desktop)
            // - ISingleViewApplicationLifetime.MainView (browser/mobile)
            // - Services that need to talk to the active menu (SearchTransitionManager)
            // View receives ViewModel via constructor (MVVM + DI per docs/ARCHITECTURE.md)
            services.AddSingleton<Views.BalatroMainMenu>(sp => new Views.BalatroMainMenu(
                sp.GetRequiredService<BalatroMainMenuViewModel>()
            ));
            services.AddSingleton<Views.MainWindow>(sp => new Views.MainWindow(
                sp.GetRequiredService<MainWindowViewModel>(),
                sp.GetRequiredService<Views.BalatroMainMenu>(),
                sp.GetService<NotificationService>()
            ));
            // Factory for AnalyzeModalViewModel so Views/ViewModels get it via injection, not ServiceHelper
            services.AddSingleton<Func<AnalyzeModalViewModel>>(sp =>
                () => sp.GetRequiredService<AnalyzeModalViewModel>()
            );
            services.AddSingleton<FiltersModalViewModel>(sp => new FiltersModalViewModel(
                sp.GetRequiredService<IConfigurationService>(),
                sp.GetRequiredService<IFilterService>(),
                sp.GetRequiredService<IPlatformServices>(),
                sp.GetService<NotificationService>(),
                sp.GetService<UserProfileService>(),
                sp.GetService<SearchManager>(),
                sp.GetService<FilterSerializationService>(),
                () => sp.GetRequiredService<ViewModels.FilterTabs.ValidateFilterTabViewModel>()
            ));
            services.AddSingleton<SearchModalViewModel>(sp => new SearchModalViewModel(
                sp.GetRequiredService<SearchManager>(),
                sp.GetRequiredService<UserProfileService>(),
                sp.GetRequiredService<Services.Storage.IAppDataStore>(),
                sp.GetRequiredService<IPlatformServices>(),
                sp.GetRequiredService<Func<AnalyzeModalViewModel>>(),
                sp.GetRequiredService<Services.Export.ResultsExportService>()
            ));
            services.AddTransient<Views.Modals.SearchModal>(sp => new Views.Modals.SearchModal(
                sp.GetRequiredService<SearchModalViewModel>()
            ));
            services.AddTransient<Views.Modals.ToolsModal>(sp => new Views.Modals.ToolsModal(
                sp.GetService<UserProfileService>(),
                sp.GetService<IConfigurationService>(),
                sp.GetService<IFilterService>(),
                sp.GetService<IFilterCacheService>(),
                sp.GetService<IPlatformServices>()
            ));
            services.AddTransient<Views.Modals.WidgetPickerModal>(sp => new Views.Modals.WidgetPickerModal(
                sp.GetRequiredService<UserProfileService>()
            ));

            services.AddTransient<AnalyzeModalViewModel>(sp => new AnalyzeModalViewModel(
                sp.GetRequiredService<SpriteService>(),
                sp.GetRequiredService<UserProfileService>()
            ));
            // Note: AnalyzerViewModel is registered by Desktop Program.cs only
            // Note: AudioVisualizerSettingsWidgetViewModel and MusicMixerWidgetViewModel are desktop-only and registered by Desktop Program.cs
            services.AddTransient<CreditsModalViewModel>();
            services.AddTransient<TransitionDesignerWidgetViewModel>(
                sp => new TransitionDesignerWidgetViewModel(
                    sp.GetService<TransitionService>(),
                    sp.GetService<TriggerService>()
                )
            );
            services.AddTransient<EventFXWidgetViewModel>();
            services.AddTransient<DeckAndStakeViewModel>();
            services.AddTransient<BaseWidgetViewModel>();
            services.AddTransient<FilterListViewModel>(sp => new FilterListViewModel(
                sp.GetRequiredService<IFilterCacheService>(),
                sp.GetService<SpriteService>()
            ));
            services.AddTransient<PaginatedFilterBrowserViewModel>(sp => new PaginatedFilterBrowserViewModel(
                sp.GetRequiredService<IFilterCacheService>(),
                sp.GetService<UserProfileService>()
            ));
            services.AddTransient<FilterSelectionModalViewModel>(sp => new FilterSelectionModalViewModel(
                sp.GetRequiredService<IFilterService>(),
                sp.GetRequiredService<PaginatedFilterBrowserViewModel>()
            ));

            // Filter Tab ViewModels
            services.AddTransient<ViewModels.FilterTabs.VisualBuilderTabViewModel>(sp =>
                new ViewModels.FilterTabs.VisualBuilderTabViewModel(
                    sp.GetService<FiltersModalViewModel>(),
                    sp.GetService<FavoritesService>(),
                    sp.GetService<IConfigurationService>(),
                    sp.GetService<IFilterService>()
                )
            );
            services.AddTransient<ViewModels.FilterTabs.DeckStakeTabViewModel>();
            services.AddTransient<ViewModels.FilterTabs.JsonEditorTabViewModel>(sp =>
                new ViewModels.FilterTabs.JsonEditorTabViewModel(
                    sp.GetService<FiltersModalViewModel>(),
                    sp.GetService<FilterSerializationService>()
                )
            );
            services.AddTransient<ViewModels.FilterTabs.ValidateFilterTabViewModel>(sp =>
                new ViewModels.FilterTabs.ValidateFilterTabViewModel(
                    sp.GetRequiredService<FiltersModalViewModel>(),
                    sp.GetRequiredService<IConfigurationService>(),
                    sp.GetRequiredService<IFilterService>(),
                    sp.GetRequiredService<IPlatformServices>(),
                    sp.GetService<FilterSerializationService>(),
                    sp.GetService<SearchManager>()
                )
            );
            services.AddTransient<ViewModels.FilterTabs.SaveFilterTabViewModel>(
                sp => new ViewModels.FilterTabs.SaveFilterTabViewModel(
                    sp.GetRequiredService<FiltersModalViewModel>(),
                    sp.GetRequiredService<IConfigurationService>(),
                    sp.GetRequiredService<IFilterService>(),
                    sp.GetRequiredService<IPlatformServices>(),
                    sp.GetService<NotificationService>(),
                    sp.GetService<FilterSerializationService>(),
                    sp.GetService<SearchManager>()
                )
            );
            services.AddTransient<ViewModels.FilterTabs.ConfigureFilterTabViewModel>(sp =>
                new ViewModels.FilterTabs.ConfigureFilterTabViewModel(
                    sp.GetService<FiltersModalViewModel>(),
                    sp.GetService<IConfigurationService>()
                )
            );

            return services;
        }
    }
}
