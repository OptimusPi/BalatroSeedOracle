using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.ViewModels
{
    public class PaginatedFilterBrowserViewModel : BaseViewModel
    {
        private const int ITEMS_PER_PAGE = 10;
        
        private List<FilterBrowserItem> _allFilters = new();
        private FilterBrowserItem? _selectedFilter;
        private int _currentPage = 0;
        private string _mainButtonText = "Select";
        private string _secondaryButtonText = "View";
        private bool _showSecondaryButton = true;
        private bool _showDeleteButton = true;

        // Properties
        public ObservableCollection<FilterBrowserItemViewModel> CurrentPageFilters { get; } = new();
        
        public FilterBrowserItem? SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (SetProperty(ref _selectedFilter, value))
                {
                    OnPropertyChanged(nameof(HasSelectedFilter));
                    OnPropertyChanged(nameof(SelectedItemTriangleY));
                    UpdateCurrentPageSelection();
                }
            }
        }

        public bool HasSelectedFilter => SelectedFilter != null;

        public string MainButtonText
        {
            get => _mainButtonText;
            set => SetProperty(ref _mainButtonText, value);
        }

        public string SecondaryButtonText
        {
            get => _secondaryButtonText;
            set => SetProperty(ref _secondaryButtonText, value);
        }

        public bool ShowSecondaryButton
        {
            get => _showSecondaryButton;
            set => SetProperty(ref _showSecondaryButton, value);
        }

        public bool ShowDeleteButton
        {
            get => _showDeleteButton;
            set => SetProperty(ref _showDeleteButton, value);
        }

        public string PageIndicatorText => $"Page {_currentPage + 1}/{TotalPages}";
        public string StatusText => $"Total {_allFilters.Count} filters";
        
        public double SelectedItemTriangleY
        {
            get
            {
                if (SelectedFilter == null) return 0;
                var pageItems = GetCurrentPageItems();
                var index = pageItems.FindIndex(f => f.FilePath == SelectedFilter.FilePath);
                return index >= 0 ? (index * 44) + 20 : 0; // 44px per item + offset
            }
        }

        private int TotalPages => (int)Math.Ceiling((double)_allFilters.Count / ITEMS_PER_PAGE);

        // Commands
        public ICommand SelectFilterCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        
        // Commands that parent ViewModels will set
        public ICommand MainButtonCommand { get; set; } = null!;
        public ICommand SecondaryButtonCommand { get; set; } = null!;
        public ICommand DeleteCommand { get; set; } = null!;

        // Events
        public event EventHandler<FilterBrowserItem>? FilterSelected;

        public PaginatedFilterBrowserViewModel()
        {
            SelectFilterCommand = new RelayCommand<FilterBrowserItemViewModel>(OnFilterSelected);
            PreviousPageCommand = new RelayCommand(PreviousPage, () => _currentPage > 0);
            NextPageCommand = new RelayCommand(NextPage, () => _currentPage < TotalPages - 1);
            
            LoadFilters();
        }

        private async void OnFilterSelected(FilterBrowserItemViewModel? filterViewModel)
        {
            if (filterViewModel?.FilterBrowserItem != null)
            {
                // Handle CREATE NEW FILTER specially
                if (filterViewModel.FilterBrowserItem.IsCreateNew)
                {
                    try
                    {
                        var tempPath = await CreateTempFilter();
                        var tempFilter = LoadFilterBrowserItem(tempPath);
                        if (tempFilter != null)
                        {
                            SelectedFilter = tempFilter;
                            FilterSelected?.Invoke(this, tempFilter);
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError("PaginatedFilterBrowserViewModel", $"Failed to create temp filter: {ex.Message}");
                    }
                }
                else
                {
                    SelectedFilter = filterViewModel.FilterBrowserItem;
                    FilterSelected?.Invoke(this, filterViewModel.FilterBrowserItem);
                }
            }
        }
        
        private async System.Threading.Tasks.Task<string> CreateTempFilter()
        {
            var baseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? System.AppDomain.CurrentDomain.BaseDirectory;
            var filtersDir = System.IO.Path.Combine(baseDir, "JsonItemFilters");
            System.IO.Directory.CreateDirectory(filtersDir);
            
            var tempPath = System.IO.Path.Combine(filtersDir, "_UNSAVED_CREATION.json");
            
            // Create basic empty filter structure
            var emptyFilter = new Motely.Filters.MotelyJsonConfig
            {
                Name = "New Filter",
                Description = "Created with Filter Designer",
                Author = ServiceHelper.GetService<Services.UserProfileService>()?.GetAuthorName() ?? "Unknown",
                DateCreated = System.DateTime.UtcNow,
                Must = new System.Collections.Generic.List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>(),
                Should = new System.Collections.Generic.List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>(),
                MustNot = new System.Collections.Generic.List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>()
            };
            
            var json = System.Text.Json.JsonSerializer.Serialize(emptyFilter, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(tempPath, json);
            
            return tempPath;
        }

        private void PreviousPage()
        {
            if (_currentPage > 0)
            {
                _currentPage--;
                UpdateCurrentPage();
            }
        }

        private void NextPage()
        {
            if (_currentPage < TotalPages - 1)
            {
                _currentPage++;
                UpdateCurrentPage();
            }
        }

        private void LoadFilters()
        {
            try
            {
                _allFilters.Clear();
                
                // Add CREATE NEW FILTER as first item (special blue item)
                _allFilters.Add(new FilterBrowserItem
                {
                    Name = "CREATE NEW FILTER",
                    Description = "Start with a blank filter",
                    Author = "System",
                    DateCreated = DateTime.Now,
                    FilePath = "__CREATE_NEW__",
                    MustCount = 0,
                    ShouldCount = 0,
                    MustNotCount = 0,
                    IsCreateNew = true
                });
                
                var filtersDir = Path.Combine(Directory.GetCurrentDirectory(), "JsonItemFilters");
                if (!Directory.Exists(filtersDir))
                {
                    Directory.CreateDirectory(filtersDir);
                    UpdateCurrentPage();
                    return;
                }

                var filterFiles = Directory.GetFiles(filtersDir, "*.json")
                    .Where(f => Path.GetFileName(f) != "_UNSAVED_CREATION.json") // Skip temp files
                    .OrderByDescending(File.GetLastWriteTime)
                    .ToList();

                foreach (var filePath in filterFiles)
                {
                    var filterItem = LoadFilterBrowserItem(filePath);
                    if (filterItem != null)
                    {
                        _allFilters.Add(filterItem);
                    }
                }

                UpdateCurrentPage();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("PaginatedFilterBrowserViewModel", $"Error loading filters: {ex.Message}");
            }
        }

        private FilterBrowserItem? LoadFilterBrowserItem(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(content))
                    return null;

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                var config = JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(content, options);
                if (config == null || string.IsNullOrEmpty(config.Name))
                    return null;

                return new FilterBrowserItem
                {
                    Name = config.Name,
                    Description = config.Description ?? "",
                    Author = config.Author ?? "Unknown",
                    DateCreated = config.DateCreated ?? File.GetLastWriteTime(filePath),
                    FilePath = filePath,
                    MustCount = config.Must?.Count ?? 0,
                    ShouldCount = config.Should?.Count ?? 0,
                    MustNotCount = config.MustNot?.Count ?? 0
                };
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("PaginatedFilterBrowserViewModel", $"Error parsing filter {filePath}: {ex.Message}");
                return null;
            }
        }

        private List<FilterBrowserItem> GetCurrentPageItems()
        {
            var startIndex = _currentPage * ITEMS_PER_PAGE;
            return _allFilters.Skip(startIndex).Take(ITEMS_PER_PAGE).ToList();
        }

        private void UpdateCurrentPage()
        {
            CurrentPageFilters.Clear();
            
            var pageItems = GetCurrentPageItems();
            for (int i = 0; i < pageItems.Count; i++)
            {
                var filter = pageItems[i];
                string displayText;
                
                if (filter.IsCreateNew)
                {
                    displayText = filter.Name; // No number for CREATE NEW
                }
                else
                {
                    // Calculate actual number (accounting for CREATE NEW being item 0)
                    var actualFilterIndex = _allFilters.FindIndex(f => f.FilePath == filter.FilePath);
                    displayText = $"{actualFilterIndex}  {filter.Name}";
                }
                
                var itemViewModel = new FilterBrowserItemViewModel
                {
                    FilterBrowserItem = filter,
                    DisplayText = displayText,
                    IsSelected = SelectedFilter?.FilePath == filter.FilePath
                };
                CurrentPageFilters.Add(itemViewModel);
            }

            OnPropertyChanged(nameof(PageIndicatorText));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(SelectedItemTriangleY));
            
            // Update command states
            ((RelayCommand)PreviousPageCommand).NotifyCanExecuteChanged();
            ((RelayCommand)NextPageCommand).NotifyCanExecuteChanged();
        }

        private void UpdateCurrentPageSelection()
        {
            foreach (var item in CurrentPageFilters)
            {
                item.IsSelected = SelectedFilter?.FilePath == item.FilterBrowserItem.FilePath;
            }
        }

        public void RefreshFilters()
        {
            LoadFilters();
        }
    }

    // Data models
    public class FilterBrowserItem
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Author { get; set; } = "";
        public DateTime DateCreated { get; set; }
        public string FilePath { get; set; } = "";
        public int MustCount { get; set; }
        public int ShouldCount { get; set; }
        public int MustNotCount { get; set; }
        public bool IsCreateNew { get; set; } = false;

        public string AuthorText => $"by {Author}";
        public string DateText => DateCreated.ToString("MMM dd, yyyy");
        public string StatsText => IsCreateNew ? "Start with a blank filter" : $"Must: {MustCount}, Should: {ShouldCount}, Must Not: {MustNotCount}";
    }

    public class FilterBrowserItemViewModel : BaseViewModel
    {
        private bool _isSelected;
        
        public FilterBrowserItem FilterBrowserItem { get; set; } = null!;
        public string DisplayText { get; set; } = "";
        
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        
        public string ItemClasses => FilterBrowserItem.IsCreateNew ? "filter-list-item create-new-item" : "filter-list-item";
    }
}