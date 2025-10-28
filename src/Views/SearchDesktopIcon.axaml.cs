using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views
{
    public partial class SearchDesktopIcon : UserControl
    {
        private SearchDesktopIconViewModel? ViewModel => DataContext as SearchDesktopIconViewModel;

        public SearchDesktopIcon()
        {
            InitializeComponent();

            // Create ViewModel
            var searchManager = App.GetService<SearchManager>();
            var userProfileService =
                ServiceHelper.GetService<UserProfileService>() ?? new UserProfileService();
            var spriteService = ServiceHelper.GetRequiredService<SpriteService>();

            DataContext = new SearchDesktopIconViewModel(
                searchManager,
                userProfileService,
                spriteService
            );

            // Subscribe to ViewModel events
            if (ViewModel != null)
            {
                ViewModel.ViewResultsRequested += OnViewResultsRequested;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Initialize the icon with search details - called from parent view
        /// </summary>
        public void Initialize(string searchId, string configPath, string filterName)
        {
            ViewModel?.Initialize(searchId, configPath, filterName);
        }

        /// <summary>
        /// Handle ViewResultsRequested event from ViewModel
        /// </summary>
        private void OnViewResultsRequested(
            object? sender,
            (string searchId, string configPath) args
        )
        {
            DebugLogger.Log(
                "SearchDesktopIcon",
                $"ViewResultsRequested - SearchId: {args.searchId}, ConfigPath: {args.configPath}"
            );

            // Find the BalatroMainMenu by traversing up the visual tree
            BalatroMainMenu? mainMenu = null;
            var current = this as Visual;

            while (current != null && mainMenu == null)
            {
                current = current.GetVisualParent<Visual>();
                mainMenu = current as BalatroMainMenu;
            }

            DebugLogger.Log(
                "SearchDesktopIcon",
                $"MainMenu found via parent traversal: {mainMenu != null}"
            );

            if (mainMenu == null)
            {
                // Try window content as fallback
                var window = TopLevel.GetTopLevel(this) as Window;
                mainMenu = window?.Content as BalatroMainMenu;
                DebugLogger.Log(
                    "SearchDesktopIcon",
                    $"MainMenu found via window content: {mainMenu != null}"
                );
            }

            if (mainMenu == null)
            {
                DebugLogger.LogError(
                    "SearchDesktopIcon",
                    "Could not find BalatroMainMenu to show search modal"
                );
                return;
            }

            try
            {
                // Open search modal and load the filter using existing method
                mainMenu.ShowSearchModalForInstance(args.searchId, args.configPath);
                DebugLogger.Log(
                    "SearchDesktopIcon",
                    "Opened search modal with filter loaded successfully"
                );

                // Remove this desktop icon since we're returning to the modal
                RemoveDesktopIcon();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchDesktopIcon", $"Error showing search modal: {ex}");
            }
        }

        /// <summary>
        /// Handle context menu request - delegate to ViewModel commands
        /// </summary>
        private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            if (ViewModel == null)
                return;

            var contextMenu = new ContextMenu();

            var viewResultsItem = new MenuItem
            {
                Header = "View Results",
                Command = ViewModel.ViewResultsCommand,
            };
            contextMenu.Items.Add(viewResultsItem);

            contextMenu.Items.Add(new Separator());

            if (ViewModel.IsSearching)
            {
                var pauseItem = new MenuItem
                {
                    Header = "Pause Search",
                    Command = ViewModel.PauseSearchCommand,
                };
                contextMenu.Items.Add(pauseItem);

                var stopItem = new MenuItem
                {
                    Header = "Stop Search",
                    Command = ViewModel.StopSearchCommand,
                };
                contextMenu.Items.Add(stopItem);
            }
            else
            {
                var resumeItem = new MenuItem
                {
                    Header = "Resume Search",
                    Command = ViewModel.ResumeSearchCommand,
                };
                contextMenu.Items.Add(resumeItem);
            }

            contextMenu.Items.Add(new Separator());

            var deleteItem = new MenuItem
            {
                Header = "Remove Icon",
                Command = ViewModel.DeleteIconCommand,
            };
            contextMenu.Items.Add(deleteItem);

            contextMenu.Open(sender as Control);
            e.Handled = true;
        }

        /// <summary>
        /// Remove the desktop icon from the parent panel
        /// Called when returning to search modal
        /// </summary>
        private void RemoveDesktopIcon()
        {
            // Trigger the ViewModel cleanup
            ViewModel?.DeleteIconCommand.Execute(null);

            // Remove this icon from parent panel
            if (this.Parent is Panel parent)
            {
                DebugLogger.Log("SearchDesktopIcon", "Removing icon from parent panel");
                parent.Children.Remove(this);
            }

            DebugLogger.Log("SearchDesktopIcon", "Desktop icon removed from UI");
        }
    }
}
