using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Oracle.Helpers;
using Oracle.Models;
using Oracle.Views.Modals;

namespace Oracle.Views.Modals
{
    public partial class BrowseFiltersModal : UserControl
    {
        private ObservableCollection<FilterItem> _allFilters = new();
        private ObservableCollection<FilterItem> _displayedFilters = new();
        private FilterItem? _selectedFilter;
        
        public event EventHandler<string>? FilterSelected;
        public event EventHandler<string>? EditRequested;
        
        public BrowseFiltersModal()
        {
            InitializeComponent();
            InitializeControls();
            _ = LoadFiltersAsync();
        }
        
        private void InitializeControls()
        {
            var filterList = this.FindControl<ListBox>("FilterList");
            if (filterList != null)
            {
                filterList.ItemsSource = _displayedFilters;
            }
        }
        
        private async Task LoadFiltersAsync()
        {
            try
            {
                _allFilters.Clear();
                
                // Load all filters from various sources
                await LoadLocalFilters();
                await LoadOuijaConfigs();
                
                ApplySearch();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BrowseFiltersModal", $"Error loading filters: {ex.Message}");
            }
        }
        
        private async Task LoadLocalFilters()
        {
            // Load from current directory
            var currentDir = Directory.GetCurrentDirectory();
            await LoadFiltersFromDirectory(currentDir, "*.ouija.json", maxDepth: 1);
        }
        
        private async Task LoadOuijaConfigs()
        {
            // Load from ouija_configs directory
            var ouijaDir = Path.Combine(Directory.GetCurrentDirectory(), "ouija_configs");
            if (Directory.Exists(ouijaDir))
            {
                await LoadFiltersFromDirectory(ouijaDir, "*.ouija.json");
            }
        }
        
        private async Task LoadExampleFilters()
        {
            // Load from Motely examples
            var examplesDir = Path.Combine(Directory.GetCurrentDirectory(), "external", "Motely", "JsonItemFilters");
            if (Directory.Exists(examplesDir))
            {
                await LoadFiltersFromDirectory(examplesDir, "*.ouija.json");
            }
        }
        
        private async Task LoadFiltersFromDirectory(string directory, string pattern, int maxDepth = 3)
        {
            await Task.Run(() =>
            {
                try
                {
                    var files = GetFilesRecursive(directory, pattern, maxDepth);
                    
                    foreach (var file in files)
                    {
                        try
                        {
                            var name = Path.GetFileNameWithoutExtension(file);
                            var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
                            
                            // Try to read filter data from file
                            string? description = null;
                            var needs = new List<string>();
                            var wants = new List<string>();
                            var mustNots = new List<string>();
                            
                            try
                            {
                                var content = File.ReadAllText(file);
                                
                                // Parse description
                                if (content.Contains("\"description\""))
                                {
                                    var start = content.IndexOf("\"description\"") + 14;
                                    var end = content.IndexOf("\"", start);
                                    if (end > start)
                                    {
                                        description = content.Substring(start, end - start).Trim();
                                    }
                                }
                                
                                // Parse filter items - look for needs, wants, must_nots
                                using var doc = System.Text.Json.JsonDocument.Parse(content);
                                var root = doc.RootElement;
                                
                                if (root.TryGetProperty("filters", out var filters))
                                {
                                    if (filters.TryGetProperty("needs", out var needsArray))
                                    {
                                        foreach (var need in needsArray.EnumerateArray())
                                        {
                                            if (need.TryGetProperty("Item", out var item))
                                                needs.Add(item.GetString() ?? "");
                                        }
                                    }
                                    
                                    if (filters.TryGetProperty("wants", out var wantsArray))
                                    {
                                        foreach (var want in wantsArray.EnumerateArray())
                                        {
                                            if (want.TryGetProperty("Item", out var item))
                                                wants.Add(item.GetString() ?? "");
                                        }
                                    }
                                    
                                    if (filters.TryGetProperty("must_nots", out var mustNotsArray))
                                    {
                                        foreach (var mustNot in mustNotsArray.EnumerateArray())
                                        {
                                            if (mustNot.TryGetProperty("Item", out var item))
                                                mustNots.Add(item.GetString() ?? "");
                                        }
                                    }
                                }
                            }
                            catch { }
                            
                            _allFilters.Add(new FilterItem
                            {
                                Name = FormatFilterName(name),
                                FilePath = relativePath,
                                FullPath = file,
                                Description = description,
                                Needs = needs,
                                Wants = wants,
                                MustNots = mustNots
                            });
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogError("BrowseFiltersModal", $"Error loading filter {file}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("BrowseFiltersModal", $"Error scanning directory {directory}: {ex.Message}");
                }
            });
        }
        
        private IEnumerable<string> GetFilesRecursive(string directory, string pattern, int maxDepth, int currentDepth = 0)
        {
            if (currentDepth >= maxDepth)
                yield break;
                
            foreach (var file in Directory.GetFiles(directory, pattern))
            {
                yield return file;
            }
            
            if (currentDepth < maxDepth - 1)
            {
                foreach (var subDir in Directory.GetDirectories(directory))
                {
                    // Skip hidden directories and common non-relevant paths
                    var dirName = Path.GetFileName(subDir);
                    if (dirName.StartsWith(".") || dirName == "node_modules" || dirName == "bin" || dirName == "obj")
                        continue;
                        
                    foreach (var file in GetFilesRecursive(subDir, pattern, maxDepth, currentDepth + 1))
                    {
                        yield return file;
                    }
                }
            }
        }
        
        private string FormatFilterName(string name)
        {
            // Convert snake_case and kebab-case to Title Case
            name = name.Replace("-", " ").Replace("_", " ");
            
            // Remove common prefixes
            if (name.StartsWith("test ", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(5);
            if (name.StartsWith("config ", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(7);
                
            // Title case
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
        }
        
        private void ApplySearch()
        {
            var searchBox = this.FindControl<TextBox>("SearchBox");
            var searchText = searchBox?.Text?.ToLowerInvariant() ?? "";
            
            _displayedFilters.Clear();
            
            var filtered = string.IsNullOrWhiteSpace(searchText) 
                ? _allFilters 
                : _allFilters.Where(f => 
                    f.Name.ToLowerInvariant().Contains(searchText) ||
                    f.FilePath.ToLowerInvariant().Contains(searchText) ||
                    (f.Description?.ToLowerInvariant().Contains(searchText) ?? false));
            
            foreach (var filter in filtered.OrderBy(f => f.Name))
            {
                _displayedFilters.Add(filter);
            }
            
            // Show/hide empty message
            var emptyMessage = this.FindControl<TextBlock>("EmptyMessage");
            if (emptyMessage != null)
            {
                emptyMessage.IsVisible = _displayedFilters.Count == 0;
            }
        }
        
        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            ApplySearch();
        }
        
        private void OnFilterSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var filterList = sender as ListBox;
            _selectedFilter = filterList?.SelectedItem as FilterItem;
            
            // Update visual selection state
            UpdateCardSelection();
            
            // Enable/disable action buttons
            var launchButton = this.FindControl<Button>("LaunchButton");
            var editButton = this.FindControl<Button>("EditButton");
            
            var hasSelection = _selectedFilter != null;
            if (launchButton != null) launchButton.IsEnabled = hasSelection;
            if (editButton != null) editButton.IsEnabled = hasSelection;
        }
        
        private void UpdateCardSelection()
        {
            var filterList = this.FindControl<ListBox>("FilterList");
            if (filterList == null) return;
            
            // Get all card borders in the list
            var items = filterList.GetVisualDescendants().OfType<Border>()
                .Where(b => b.Classes.Contains("filter-card"));
            
            foreach (var card in items)
            {
                var isSelected = card.Tag == _selectedFilter;
                card.Classes.Set("selected", isSelected);
            }
        }
        
        private void OnLaunchClick(object? sender, RoutedEventArgs e)
        {
            if (_selectedFilter != null)
            {
                FilterSelected?.Invoke(this, _selectedFilter.FullPath);
            }
        }
        
        private void OnEditClick(object? sender, RoutedEventArgs e)
        {
            if (_selectedFilter != null)
            {
                EditRequested?.Invoke(this, _selectedFilter.FullPath);
            }
        }
        
        private async void OnNativeBrowseClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;
                
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select Filter File",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Ouija Filter Files")
                        {
                            Patterns = new[] { "*.ouija.json" },
                            MimeTypes = new[] { "application/json" }
                        },
                        new FilePickerFileType("All Files")
                        {
                            Patterns = new[] { "*" }
                        }
                    }
                });
                
                if (files.Count > 0)
                {
                    var filePath = files[0].Path.LocalPath;
                    FilterSelected?.Invoke(this, filePath);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("BrowseFiltersModal", $"Error opening file picker: {ex.Message}");
            }
        }
    }
    
    public class FilterItem : INotifyPropertyChanged
    {
        private string _name = "";
        private string _filePath = "";
        private string _fullPath = "";
        private string? _description;
        private List<string> _needs = new();
        private List<string> _wants = new();
        private List<string> _mustNots = new();
        
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }
        
        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilePath)));
            }
        }
        
        public string FullPath
        {
            get => _fullPath;
            set
            {
                _fullPath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FullPath)));
            }
        }
        
        public string? Description
        {
            get => _description;
            set
            {
                _description = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasDescription)));
            }
        }
        
        public List<string> Needs
        {
            get => _needs;
            set
            {
                _needs = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Needs)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasNeeds)));
            }
        }
        
        public List<string> Wants
        {
            get => _wants;
            set
            {
                _wants = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Wants)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasWants)));
            }
        }
        
        public List<string> MustNots
        {
            get => _mustNots;
            set
            {
                _mustNots = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MustNots)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasMustNots)));
            }
        }
        
        public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
        public bool HasNeeds => Needs?.Count > 0;
        public bool HasWants => Wants?.Count > 0;
        public bool HasMustNots => MustNots?.Count > 0;
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        public override string ToString()
        {
            return Name;
        }
    }
}