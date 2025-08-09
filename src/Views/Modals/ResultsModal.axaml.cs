using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Oracle.Helpers;
using Oracle.Services;

namespace Oracle.Views.Modals
{
    public partial class ResultsModal : UserControl
    {
        private const int FILTERS_PER_PAGE = 12;
        private readonly List<string> _filterFiles = new();
        private int _currentPage = 0;
        private int _totalPages = 1;
        private string? _selectedFilter;

        public ResultsModal()
        {
            InitializeComponent();
            LoadFilterFiles();
        }

        private void LoadFilterFiles()
        {
            try
            {
                // Get the directory where configs are stored
                var configDir = Path.Combine(AppContext.BaseDirectory, "configs");
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                // Find all .json files
                var files = Directory.GetFiles(configDir, "*.json")
                    .OrderBy(f => Path.GetFileName(f))
                    .ToList();

                _filterFiles.Clear();
                _filterFiles.AddRange(files);

                // Calculate pages
                _totalPages = Math.Max(1, (int)Math.Ceiling(_filterFiles.Count / (double)FILTERS_PER_PAGE));
                _currentPage = 0;

                // Display first page
                DisplayCurrentPage();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("ResultsModal", $"Error loading filter files: {ex.Message}");
            }
        }

        private void DisplayCurrentPage()
        {
            var filterPanel = this.FindControl<WrapPanel>("FilterListPanel");
            if (filterPanel == null) return;

            filterPanel.Children.Clear();

            // Calculate range for current page
            var startIndex = _currentPage * FILTERS_PER_PAGE;
            var endIndex = Math.Min(startIndex + FILTERS_PER_PAGE, _filterFiles.Count);

            // Add filter buttons for current page
            for (int i = startIndex; i < endIndex; i++)
            {
                var filterPath = _filterFiles[i];
                var fileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(filterPath));

                var button = new Button
                {
                    Classes = { "filter-button" },
                    Width = 200,
                    Height = 80
                };

                var content = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };

                // Icon
                var icon = new TextBlock
                {
                    Text = GetFilterIcon(fileName),
                    Classes = { "filter-icon" }
                };
                content.Children.Add(icon);

                // Text content
                var textPanel = new StackPanel();
                var nameText = new TextBlock
                {
                    Text = fileName,
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
                };
                var descText = new TextBlock
                {
                    Text = "Click to load",
                    Classes = { "filter-desc" }
                };
                textPanel.Children.Add(nameText);
                textPanel.Children.Add(descText);
                content.Children.Add(textPanel);

                button.Content = content;
                button.Tag = filterPath;
                button.Click += OnFilterButtonClick;

                filterPanel.Children.Add(button);
            }

            // Update navigation
            UpdateNavigation();
        }

        private string GetFilterIcon(string fileName)
        {
            // Return different icons based on filter name patterns
            if (fileName.ToLower().Contains("joker")) return "🃏";
            if (fileName.ToLower().Contains("tag")) return "🏷️";
            if (fileName.ToLower().Contains("soul")) return "👻";
            if (fileName.ToLower().Contains("rare")) return "💎";
            if (fileName.ToLower().Contains("uncommon")) return "✨";
            if (fileName.ToLower().Contains("common")) return "🎴";
            if (fileName.ToLower().Contains("spectral")) return "🌟";
            if (fileName.ToLower().Contains("tarot")) return "🔮";
            if (fileName.ToLower().Contains("voucher")) return "🎫";
            if (fileName.ToLower().Contains("planet")) return "🪐";
            return "📄";
        }

        private void UpdateNavigation()
        {
            var prevButton = this.FindControl<Button>("PrevPageButton");
            var nextButton = this.FindControl<Button>("NextPageButton");
            var pageIndicator = this.FindControl<TextBlock>("PageIndicator");

            if (prevButton != null)
                prevButton.IsEnabled = _currentPage > 0;

            if (nextButton != null)
                nextButton.IsEnabled = _currentPage < _totalPages - 1;

            if (pageIndicator != null)
                pageIndicator.Text = $"Page {_currentPage + 1} of {_totalPages}";
        }

        private async void OnFilterButtonClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filterPath)
            {
                _selectedFilter = filterPath;

                // Get the main menu
                var mainWindow = TopLevel.GetTopLevel(this) as Window;
                var mainMenu = mainWindow?.Content as Views.BalatroMainMenu;

                if (mainMenu != null)
                {
                    // Hide the modal
                    mainMenu.HideModalContent();

                    // Create a new search instance and show desktop icon
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        var searchManager = App.GetService<Services.SearchManager>();
                        if (searchManager != null)
                        {
                            var searchId = searchManager.CreateSearch();
                            mainMenu.ShowSearchDesktopIcon(searchId, filterPath);
                        }
                    });
                }
            }
        }

        private void OnPrevPageClick(object? sender, RoutedEventArgs e)
        {
            if (_currentPage > 0)
            {
                _currentPage--;
                DisplayCurrentPage();
            }
        }

        private void OnNextPageClick(object? sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages - 1)
            {
                _currentPage++;
                DisplayCurrentPage();
            }
        }


    }
}