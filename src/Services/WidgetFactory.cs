using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using BalatroSeedOracle.Components.Widgets;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.ViewModels.Widgets;
using Microsoft.Extensions.DependencyInjection;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Factory service for creating and registering widget instances
    /// </summary>
    public class WidgetFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IWidgetRegistry _widgetRegistry;

        public WidgetFactory(IServiceProvider serviceProvider, IWidgetRegistry widgetRegistry)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _widgetRegistry = widgetRegistry ?? throw new ArgumentNullException(nameof(widgetRegistry));
        }

        /// <summary>
        /// Register all built-in widget types
        /// </summary>
        public void RegisterBuiltInWidgets()
        {
            // Register Search Widget
            _widgetRegistry.RegisterWidget(new WidgetMetadata
            {
                Id = "search",
                Title = "Search",
                IconResource = "🔍",
                WidgetType = typeof(SearchWidgetAdapter),
                ViewModelType = typeof(SearchWidgetAdapter),
                AllowClose = true,
                AllowPopOut = false,
                DefaultSize = new Size(350, 450),
                Description = "Track active search progress and results",
                Category = "Core"
            });

            // Register Audio Mixer Widget
            _widgetRegistry.RegisterWidget(new WidgetMetadata
            {
                Id = "audio-mixer",
                Title = "Audio Mixer",
                IconResource = "🎵",
                WidgetType = typeof(GenericWidgetAdapter),
                ViewModelType = typeof(MusicMixerWidgetViewModel),
                AllowClose = true,
                AllowPopOut = false,
                DefaultSize = new Size(300, 400),
                Description = "Control audio levels and effects",
                Category = "Audio"
            });

            // Register Audio Visualizer Settings Widget
            _widgetRegistry.RegisterWidget(new WidgetMetadata
            {
                Id = "audio-visualizer",
                Title = "Visualizer",
                IconResource = "🎶",
                WidgetType = typeof(GenericWidgetAdapter),
                ViewModelType = typeof(AudioVisualizerSettingsWidgetViewModel),
                AllowClose = true,
                AllowPopOut = false,
                DefaultSize = new Size(300, 350),
                Description = "Configure audio visualizer settings",
                Category = "Audio"
            });

            // Register Event FX Widget
            _widgetRegistry.RegisterWidget(new WidgetMetadata
            {
                Id = "event-fx",
                Title = "Event FX",
                IconResource = "✨",
                WidgetType = typeof(GenericWidgetAdapter),
                ViewModelType = typeof(EventFXWidgetViewModel),
                AllowClose = true,
                AllowPopOut = false,
                DefaultSize = new Size(320, 380),
                Description = "Configure event visual effects",
                Category = "Effects"
            });

            // Register Transition Designer Widget
            _widgetRegistry.RegisterWidget(new WidgetMetadata
            {
                Id = "transition-designer",
                Title = "Transitions",
                IconResource = "🎭",
                WidgetType = typeof(GenericWidgetAdapter),
                ViewModelType = typeof(TransitionDesignerWidgetViewModel),
                AllowClose = true,
                AllowPopOut = false,
                DefaultSize = new Size(400, 450),
                Description = "Design custom transition effects",
                Category = "Effects"
            });

            // Register DayLatro Widget
            _widgetRegistry.RegisterWidget(new WidgetMetadata
            {
                Id = "daylatro",
                Title = "DayLatro",
                IconResource = "🃏",
                WidgetType = typeof(GenericWidgetAdapter),
                ViewModelType = typeof(DayLatroWidgetViewModel),
                AllowClose = true,
                AllowPopOut = false,
                DefaultSize = new Size(350, 400),
                Description = "Daily Balatro challenges",
                Category = "Games"
            });

            // Register Fertilizer Widget
            _widgetRegistry.RegisterWidget(new WidgetMetadata
            {
                Id = "fertilizer",
                Title = "Fertilizer",
                IconResource = "🌱",
                WidgetType = typeof(GenericWidgetAdapter),
                ViewModelType = typeof(FertilizerWidgetViewModel),
                AllowClose = true,
                AllowPopOut = false,
                DefaultSize = new Size(300, 350),
                Description = "Manage fertilizer pile of favorite seeds",
                Category = "Core"
            });

            // Register Genie Widget
            _widgetRegistry.RegisterWidget(new WidgetMetadata
            {
                Id = "genie",
                Title = "Genie",
                IconResource = "🧞",
                WidgetType = typeof(GenericWidgetAdapter),
                ViewModelType = typeof(GenieWidgetViewModel),
                AllowClose = true,
                AllowPopOut = false,
                DefaultSize = new Size(350, 400),
                Description = "AI-powered filter generator",
                Category = "AI"
            });

            // Register Host API Widget
            _widgetRegistry.RegisterWidget(new WidgetMetadata
            {
                Id = "host-api",
                Title = "Host API",
                IconResource = "🌐",
                WidgetType = typeof(GenericWidgetAdapter),
                ViewModelType = typeof(HostApiWidgetViewModel),
                AllowClose = true,
                AllowPopOut = false,
                DefaultSize = new Size(300, 350),
                Description = "Configure API hosting settings",
                Category = "Network"
            });
        }

        /// <summary>
        /// Create a widget instance by ID
        /// </summary>
        public IWidget? CreateWidget(string widgetId)
        {
            var metadata = _widgetRegistry.GetWidgetMetadata(widgetId);
            if (metadata?.WidgetType == null)
                return null;

            try
            {
                // Handle special cases first
                if (widgetId == "search")
                {
                    // Search widgets need special handling as they wrap existing SearchWidgetViewModel
                    return CreateSearchWidget();
                }

                // Create actual working widget adapters that wrap the real widgets
                return widgetId switch
                {
                    "audio-mixer" => CreateMusicMixerWidget(),
                    "audio-visualizer" => CreateVisualizerWidget(),
                    "transition-designer" => CreateTransitionWidget(),
                    "fertilizer" => CreateFertilizerWidget(),
                    "event-fx" => CreateEventFXWidget(),
                    "host-api" => CreateHostApiWidget(),
                    "genie" => CreateGenieWidget(),
                    "daylatro" => CreateDayLatroWidget(),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create widget {widgetId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create actual widget instances using the real widget controls
        /// </summary>
        private IWidget? CreateMusicMixerWidget()
        {
            var vm = _serviceProvider.GetRequiredService<MusicMixerWidgetViewModel>();
            var control = new Components.MusicMixerWidget { DataContext = vm };
            return new WidgetAdapter(control, vm);
        }

        private IWidget? CreateVisualizerWidget()
        {
            var vm = _serviceProvider.GetRequiredService<AudioVisualizerSettingsWidgetViewModel>();
            var control = new Components.AudioVisualizerSettingsWidget { DataContext = vm };
            return new WidgetAdapter(control, vm);
        }

        private IWidget? CreateTransitionWidget()
        {
            var vm = _serviceProvider.GetRequiredService<TransitionDesignerWidgetViewModel>();
            var control = new Components.TransitionDesignerWidget { DataContext = vm };
            return new WidgetAdapter(control, vm);
        }

        private IWidget? CreateFertilizerWidget()
        {
            var vm = _serviceProvider.GetRequiredService<FertilizerWidgetViewModel>();
            var control = new Components.FertilizerWidget { DataContext = vm };
            return new WidgetAdapter(control, vm);
        }

        private IWidget? CreateEventFXWidget()
        {
            var vm = _serviceProvider.GetRequiredService<EventFXWidgetViewModel>();
            var control = new Components.EventFXWidget { DataContext = vm };
            return new WidgetAdapter(control, vm);
        }

        private IWidget? CreateHostApiWidget()
        {
            var vm = _serviceProvider.GetRequiredService<HostApiWidgetViewModel>();
            var control = new Components.HostApiWidget { DataContext = vm };
            return new WidgetAdapter(control, vm);
        }

        private IWidget? CreateGenieWidget()
        {
            var vm = _serviceProvider.GetRequiredService<GenieWidgetViewModel>();
            var control = new Components.GenieWidget { DataContext = vm };
            return new WidgetAdapter(control, vm);
        }

        private IWidget? CreateDayLatroWidget()
        {
            var vm = _serviceProvider.GetRequiredService<DayLatroWidgetViewModel>();
            var control = new Components.DayLatroWidget { DataContext = vm };
            return new WidgetAdapter(control, vm);
        }

        private IWidget? CreateSearchWidget()
        {
            var vm = _serviceProvider.GetRequiredService<SearchWidgetViewModel>();
            var control = new Components.Widgets.SearchWidget { DataContext = vm };
            return new WidgetAdapter(control, vm);
        }

        /// <summary>
        /// Generic widget adapter for simple widgets
        /// </summary>
        private class GenericWidgetAdapter : WidgetViewModel
        {
            public GenericWidgetAdapter(
                string id,
                string title,
                string icon,
                Size size,
                IWidgetLayoutService layoutService,
                IDockingService dockingService) : base(layoutService, dockingService)
            {
                Id = id;
                Title = title;
                IconResource = icon;
                Size = size;
            }

            public override Avalonia.Controls.UserControl GetContentView()
            {
                // Return a placeholder content view
                var textBlock = new Avalonia.Controls.TextBlock
                {
                    Text = $"Widget: {Title}",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                return new Avalonia.Controls.UserControl
                {
                    Content = textBlock
                };
            }
        }
    }
}