using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class FiltersModal : UserControl
    {
        public event EventHandler? CloseRequested;
        
        private int _currentTabIndex = 0;
        private string? _currentFilterPath;
        
        // Controls
        private TextBox? _filterNameInput;
        private TextBox? _filterDescriptionInput;
        private Button? _createFilterButton;
        
        // Tab controls
        private Button? _configTab;
        private Button? _filterTab;
        private Button? _scoringTab;
        private Button? _validateTab;
        private Polygon? _tabTriangle;
        
        // Panels
        private StackPanel? _configPanel;
        private Grid? _filterPanel;
        private Grid? _scoringPanel;
        private Grid? _validatePanel;

        public FiltersModal()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Find controls
            _filterNameInput = this.FindControl<TextBox>("FilterNameInput");
            _filterDescriptionInput = this.FindControl<TextBox>("FilterDescriptionInput");
            _createFilterButton = this.FindControl<Button>("CreateFilterButton");
            
            _configTab = this.FindControl<Button>("ConfigTab");
            _filterTab = this.FindControl<Button>("FilterTab");
            _scoringTab = this.FindControl<Button>("ScoringTab");
            _validateTab = this.FindControl<Button>("ValidateTab");
            _tabTriangle = this.FindControl<Polygon>("TabTriangle");
            
            _configPanel = this.FindControl<StackPanel>("ConfigPanel");
            _filterPanel = this.FindControl<Grid>("FilterPanel");
            _scoringPanel = this.FindControl<Grid>("ScoringPanel");
            _validatePanel = this.FindControl<Grid>("ValidatePanel");
            
            // Wire up create filter button
            if (_createFilterButton != null)
            {
                _createFilterButton.Click += OnCreateFilterClick;
            }
            
            // Reset validation styling when user types
            if (_filterNameInput != null)
            {
                _filterNameInput.TextChanged += (s, e) =>
                {
                    _filterNameInput.BorderBrush = null;
                    _filterNameInput.BorderThickness = new Avalonia.Thickness(1);
                };
            }
            
            // Wire up tab buttons
            if (_configTab != null) _configTab.Click += OnTabClick;
            if (_filterTab != null) _filterTab.Click += OnTabClick;
            if (_scoringTab != null) _scoringTab.Click += OnTabClick;
            if (_validateTab != null) _validateTab.Click += OnTabClick;
        }

        private async void OnCreateFilterClick(object? sender, RoutedEventArgs e)
        {
            var filterName = _filterNameInput?.Text?.Trim();
            var description = _filterDescriptionInput?.Text?.Trim();
            
            if (string.IsNullOrEmpty(filterName))
            {
                if (_filterNameInput != null)
                {
                    _filterNameInput.BorderBrush = Avalonia.Media.Brushes.Red;
                    _filterNameInput.BorderThickness = new Avalonia.Thickness(2);
                }
                DebugLogger.LogError("FiltersModal", "Filter name is required");
                return;
            }
            
            try
            {
                // Auto-save to JsonItemFilters folder with normalized filename
                var normalizedName = System.Text.RegularExpressions.Regex.Replace(filterName, @"[^\w\d_-]", "").ToLower();
                if (string.IsNullOrEmpty(normalizedName))
                    normalizedName = "filter_" + DateTime.Now.Ticks;
                    
                var baseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;
                var filtersDir = System.IO.Path.Combine(baseDir, "JsonItemFilters");
                
                // Ensure directory exists
                if (!Directory.Exists(filtersDir))
                    Directory.CreateDirectory(filtersDir);
                    
                var filePath = System.IO.Path.Combine(filtersDir, $"{normalizedName}.json");
                
                // Create basic filter JSON structure
                var filterData = new
                {
                    name = filterName,
                    description = description ?? "",
                    author = ServiceHelper.GetService<UserProfileService>()?.GetAuthorName() ?? "Anonymous",
                    dateCreated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    must = new object[0],
                    should = new object[0],
                    mustNot = new object[0],
                    deck = "Red",
                    stake = "White"
                };
                
                // Save the filter file
                var json = JsonSerializer.Serialize(filterData, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
                
                _currentFilterPath = filePath;
                                
                DebugLogger.Log("FiltersModal", $"Filter created: {filePath}");
                
                // Enable other tabs
                if (_filterTab != null) _filterTab.IsEnabled = true;
                if (_scoringTab != null) _scoringTab.IsEnabled = true;
                if (_validateTab != null) _validateTab.IsEnabled = true;
                        
                // Switch to Filter tab automatically
                SwitchToTab(1);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModal", $"Error creating filter: {ex.Message}");
            }
        }

        private void OnTabClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button clickedTab) return;
            
            var tabIndex = clickedTab.Name switch
            {
                "ConfigTab" => 0,
                "FilterTab" => 1,
                "ScoringTab" => 2,
                "ValidateTab" => 3,
                _ => 0
            };
            
            SwitchToTab(tabIndex);
        }
        
        private void SwitchToTab(int tabIndex)
        {
            _currentTabIndex = tabIndex;
            
            // Update tab button states
            var tabs = new[] { _configTab, _filterTab, _scoringTab, _validateTab };
            for (int i = 0; i < tabs.Length; i++)
            {
                var tab = tabs[i];
                if (tab != null)
                {
                    tab.Classes.Remove("active");
                    if (i == tabIndex)
                    {
                        tab.Classes.Add("active");
                    }
                }
            }
            
            // Show/hide panels
            if (_configPanel != null) _configPanel.IsVisible = tabIndex == 0;
            if (_filterPanel != null) _filterPanel.IsVisible = tabIndex == 1;
            if (_scoringPanel != null) _scoringPanel.IsVisible = tabIndex == 2;
            if (_validatePanel != null) _validatePanel.IsVisible = tabIndex == 3;
            
            // Update triangle position
            UpdateTrianglePosition();
        }
        
        private void UpdateTrianglePosition()
        {
            if (_tabTriangle == null) return;
            
            // Move triangle to correct container
            var containers = new[]
            {
                this.FindControl<Grid>("TriangleContainer0"),
                this.FindControl<Grid>("TriangleContainer1"),
                this.FindControl<Grid>("TriangleContainer2"),
                this.FindControl<Grid>("TriangleContainer3")
            };
            
            // Remove triangle from all containers
            foreach (var container in containers)
            {
                if (container != null)
                {
                    container.Children.Clear();
                }
            }
            
            // Add triangle to current container
            if (_currentTabIndex < containers.Length && containers[_currentTabIndex] != null)
            {
                containers[_currentTabIndex]!.Children.Add(_tabTriangle);
            }
        }
        
        public void LoadFilter(string filterPath)
        {
            _currentFilterPath = filterPath;
                        
            // Enable all tabs for editing existing filter
            if (_filterTab != null) _filterTab.IsEnabled = true;
            if (_scoringTab != null) _scoringTab.IsEnabled = true;
            if (_validateTab != null) _validateTab.IsEnabled = true;
            
            // TODO: Load filter data and populate UI
            SwitchToTab(1); // Go to Filter tab
        }
    }
}