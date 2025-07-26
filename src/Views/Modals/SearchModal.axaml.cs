using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Oracle.Helpers;
using Oracle.Models;
using Oracle.Services;

namespace Oracle.Views.Modals;

public partial class SearchModal : UserControl
{
    private MotelySearchService _searchService;
    private readonly ObservableCollection<SearchResultViewModel> _results = new();
    private string? _configPath;
    private bool _isRunning;
    

    public SearchModal()
    {
        InitializeComponent();
        
        // Get service from DI using helper
        _searchService = ServiceHelper.GetService<MotelySearchService>() ?? new MotelySearchService();
    }
    
    // Methods to set state from widget
    public void SetSearchService(MotelySearchService service)
    {
        _searchService = service;
    }
    
    public void SetConfigPath(string? configPath)
    {
        _configPath = configPath;
        var configPathTextBox = this.FindControl<TextBlock>("ConfigPathTextBox");
        if (configPathTextBox != null && !string.IsNullOrEmpty(configPath))
        {
            configPathTextBox.Text = Path.GetFileName(configPath);
        }
    }
    
    public void SetResults(List<SearchResult> results)
    {
        _results.Clear();
        int index = 1;
        foreach (var result in results)
        {
            _results.Add(new SearchResultViewModel
            {
                Index = index++,
                Seed = result.Seed,
                Score = result.Score,
                Details = result.Details ?? ""
            });
        }
        
        var resultsGrid = this.FindControl<DataGrid>("ResultsGrid");
        var noResultsText = this.FindControl<TextBlock>("NoResultsText");
        var summaryText = this.FindControl<TextBlock>("SummaryText");
        var exportButton = this.FindControl<Button>("ExportButton");
        
        if (resultsGrid != null)
        {
            resultsGrid.ItemsSource = _results;
            resultsGrid.IsVisible = _results.Count > 0;
        }
        
        if (noResultsText != null)
        {
            noResultsText.IsVisible = _results.Count == 0;
        }
        
        if (summaryText != null)
        {
            summaryText.Text = $"{_results.Count} seeds found";
        }
        
        if (exportButton != null)
        {
            exportButton.IsEnabled = _results.Count > 0;
        }
    }
    
    public void SetSearchState(bool isRunning, int foundCount)
    {
        _isRunning = isRunning;
        
        var statusText = this.FindControl<TextBlock>("StatusText");
        if (statusText != null)
        {
            statusText.Text = isRunning ? $"Searching... Found {foundCount} seeds" : $"Found {foundCount} seeds";
        }
    }

    private void InitializeComponent()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
    }
    
    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        _results.Clear();
        SetResults(new List<SearchResult>());
    }
    
    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        if (_results.Count == 0) return;
        
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var storageProvider = topLevel?.StorageProvider;
            
            if (storageProvider == null) return;

            var saveOptions = new FilePickerSaveOptions
            {
                Title = "Export Search Results",
                DefaultExtension = "csv",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("CSV Files") 
                    { 
                        Patterns = new[] { "*.csv" }
                    }
                },
                SuggestedFileName = $"search-results-{DateTime.Now:yyyyMMdd-HHmmss}.csv"
            };

            var file = await storageProvider.SaveFilePickerAsync(saveOptions);
            if (file != null)
            {
                // Export results to CSV
                var csv = "Seed,Score,Details\n";
                foreach (var result in _results)
                {
                    csv += $"{result.Seed},{result.Score},\"{result.Details}\"\n";
                }
                
                await File.WriteAllTextAsync(file.Path.LocalPath, csv);
                
                var statusText = this.FindControl<TextBlock>("StatusText");
                if (statusText != null)
                {
                    statusText.Text = $"Exported {_results.Count} results to {file.Name}";
                }
            }
        }
        catch (Exception ex)
        {
            Oracle.Helpers.DebugLogger.Log("SearchModal", $"Error exporting results: {ex.Message}");
        }
    }
}

public class SearchResultViewModel : INotifyPropertyChanged
{
    private int _index;
    private string _seed = "";
    private int _score;
    private string _details = "";
    
    public int Index
    {
        get => _index;
        set
        {
            _index = value;
            OnPropertyChanged();
        }
    }
    
    public string Seed 
    { 
        get => _seed; 
        set
        {
            _seed = value;
            OnPropertyChanged();
        }
    }
    
    public int Score 
    { 
        get => _score; 
        set
        {
            _score = value;
            OnPropertyChanged();
        }
    }
    
    public string Details 
    { 
        get => _details; 
        set
        {
            _details = value;
            OnPropertyChanged();
        }
    }
    
    // Copy command
    private ICommand? _copyCommand;
    public ICommand CopyCommand => _copyCommand ??= new RelayCommand<string>(CopySeed);
    
    // View command
    private ICommand? _viewCommand;
    public ICommand ViewCommand => _viewCommand ??= new RelayCommand<SearchResultViewModel>(ViewDetails);
    
    private async void CopySeed(string? seed)
    {
        if (string.IsNullOrEmpty(seed)) return;
        
        try
        {
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var clipboard = desktop.MainWindow?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(seed);
                    Oracle.Helpers.DebugLogger.Log("SearchModal", $"Copied seed {seed} to clipboard");
                }
            }
        }
        catch (Exception ex)
        {
            Oracle.Helpers.DebugLogger.Log("SearchModal", $"Error copying to clipboard: {ex.Message}");
        }
    }
    
    private void ViewDetails(SearchResultViewModel? result)
    {
        if (result == null) return;
        
        // Show seed details in a simple message box for now
        var detailsText = $"Seed: {result.Seed}\nScore: {result.Score:N0}\nDetails: {result.Details}";
        
        // Create a simple detail dialog
        var detailWindow = new Window
        {
            Title = $"Seed Details - {result.Seed}",
            Width = 400,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new ScrollViewer
            {
                Content = new TextBlock
                {
                    Text = detailsText,
                    Margin = new Thickness(10),
                    TextWrapping = TextWrapping.Wrap
                }
            }
        };
        
        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var parentWindow = desktop.MainWindow;
            if (parentWindow != null)
            {
                detailWindow.ShowDialog(parentWindow);
            }
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Predicate<T?>? _canExecute;
    
    public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }
    
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }
    
    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke((T?)parameter) ?? true;
    }
    
    public void Execute(object? parameter)
    {
        _execute((T?)parameter);
    }
}