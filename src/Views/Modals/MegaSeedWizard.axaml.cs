using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using BalatroSeedOracle.Components;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Views.Modals;

public partial class MegaSeedWizard : UserControl
{
    public enum WizardStep
    {
        Select = 0,
        Build = 1,
        Configure = 2,
        Search = 3,
        Results = 4
    }
    
    private WizardStep _currentStep = WizardStep.Select;
    private readonly Button[] _tabButtons;
    private readonly Border[] _panels;
    
    // Components
    private ChallengesStyleFilterGrid? _filterPicker;
    private Button? _selectTab;
    private Button? _buildTab;
    private Button? _configureTab;
    private Button? _searchTab;
    private Button? _resultsTab;
    private Border? _selectPanel;
    private Border? _buildPanel;
    private Border? _configurePanel;
    private Border? _searchPanel;
    private Border? _resultsPanel;
    private Button? _backButton;
    private Button? _nextButton;
    private Button? _cancelButton;
    private TextBlock? _statusText;
    
    // State
    private string? _selectedFilterPath;
    private bool _isNewFilter = false;
    private bool _isSearching = false;
    
    public event EventHandler? Cancelled;
    public event EventHandler? Completed;
    
    public MegaSeedWizard()
    {
        InitializeComponent();
        
        _tabButtons = new Button[5];
        _panels = new Border[5];
        
        // Set up keyboard handling
        this.Focusable = true;
        this.KeyDown += OnKeyDown;
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
        // Find all controls
        _filterPicker = this.FindControl<ChallengesStyleFilterGrid>("FilterPicker");
        
        _selectTab = this.FindControl<Button>("SelectTab");
        _buildTab = this.FindControl<Button>("BuildTab");
        _configureTab = this.FindControl<Button>("ConfigureTab");
        _searchTab = this.FindControl<Button>("SearchTab");
        _resultsTab = this.FindControl<Button>("ResultsTab");
        
        _selectPanel = this.FindControl<Border>("SelectPanel");
        _buildPanel = this.FindControl<Border>("BuildPanel");
        _configurePanel = this.FindControl<Border>("ConfigurePanel");
        _searchPanel = this.FindControl<Border>("SearchPanel");
        _resultsPanel = this.FindControl<Border>("ResultsPanel");
        
        _backButton = this.FindControl<Button>("BackButton");
        _nextButton = this.FindControl<Button>("NextButton");
        _cancelButton = this.FindControl<Button>("CancelButton");
        _statusText = this.FindControl<TextBlock>("StatusText");
        
        if (_selectTab != null) _tabButtons[0] = _selectTab;
        if (_buildTab != null) _tabButtons[1] = _buildTab;
        if (_configureTab != null) _tabButtons[2] = _configureTab;
        if (_searchTab != null) _tabButtons[3] = _searchTab;
        if (_resultsTab != null) _tabButtons[4] = _resultsTab;
        
        if (_selectPanel != null) _panels[0] = _selectPanel;
        if (_buildPanel != null) _panels[1] = _buildPanel;
        if (_configurePanel != null) _panels[2] = _configurePanel;
        if (_searchPanel != null) _panels[3] = _searchPanel;
        if (_resultsPanel != null) _panels[4] = _resultsPanel;
        
        // Hook up filter picker events
        if (_filterPicker != null)
        {
            _filterPicker.FilterSelected += OnFilterSelected;
        }
        
        Loaded += OnLoaded;
    }
    
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        UpdateUI();
        
        // Focus the control to enable keyboard shortcuts
        this.Focus();
    }
    
    private void OnFilterSelected(object? sender, string? filterPath)
    {
        _selectedFilterPath = filterPath;
        _isNewFilter = filterPath == null;
        
        // Enable next step
        if (_nextButton != null)
        {
            _nextButton.IsEnabled = true;
            
            // If creating new filter, next goes to Build
            // If existing filter selected, can skip to Configure
            if (_isNewFilter)
            {
                _nextButton.Content = "BUILD FILTER →";
            }
            else
            {
                _nextButton.Content = "CONFIGURE →";
            }
        }
    }
    
    private async void OnNextClick(object? sender, RoutedEventArgs e)
    {
        switch (_currentStep)
        {
            case WizardStep.Select:
                if (_isNewFilter)
                {
                    await TransitionTo(WizardStep.Build);
                }
                else
                {
                    // Skip build step for existing filters
                    await TransitionTo(WizardStep.Configure);
                }
                break;
                
            case WizardStep.Build:
                await TransitionTo(WizardStep.Configure);
                break;
                
            case WizardStep.Configure:
                await TransitionTo(WizardStep.Search);
                StartSearch();
                break;
                
            case WizardStep.Search:
                if (!_isSearching)
                {
                    await TransitionTo(WizardStep.Results);
                }
                break;
                
            case WizardStep.Results:
                Completed?.Invoke(this, EventArgs.Empty);
                break;
        }
    }
    
    private async void OnBackClick(object? sender, RoutedEventArgs e)
    {
        switch (_currentStep)
        {
            case WizardStep.Build:
                await TransitionTo(WizardStep.Select);
                break;
                
            case WizardStep.Configure:
                if (_isNewFilter)
                {
                    await TransitionTo(WizardStep.Build);
                }
                else
                {
                    await TransitionTo(WizardStep.Select);
                }
                break;
                
            case WizardStep.Search:
                if (_isSearching)
                {
                    StopSearch();
                }
                await TransitionTo(WizardStep.Configure);
                break;
                
            case WizardStep.Results:
                await TransitionTo(WizardStep.Configure);
                break;
        }
    }
    
    private async Task TransitionTo(WizardStep newStep)
    {
        if (newStep == _currentStep) return;
        
        var oldPanel = _panels[(int)_currentStep];
        var newPanel = _panels[(int)newStep];
        var oldTab = _tabButtons[(int)_currentStep];
        var newTab = _tabButtons[(int)newStep];
        
        // Animate out old panel
        if (oldPanel != null)
        {
            var slideOut = new TranslateTransform();
            oldPanel.RenderTransform = slideOut;
            
            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(200),
                Children =
                {
                    new KeyFrame
                    {
                        Setters = { new Setter(TranslateTransform.XProperty, 0.0) },
                        Cue = new Cue(0)
                    },
                    new KeyFrame
                    {
                        Setters = { new Setter(TranslateTransform.XProperty, newStep > _currentStep ? -50.0 : 50.0) },
                        Cue = new Cue(1)
                    }
                }
            };
            
            await animation.RunAsync(slideOut);
            oldPanel.IsVisible = false;
            oldPanel.Opacity = 0;
        }
        
        // Update tabs
        if (oldTab != null)
        {
            oldTab.Classes.Remove("active");
            oldTab.Classes.Add("completed");
        }
        
        if (newTab != null)
        {
            newTab.Classes.Remove("completed");
            newTab.Classes.Add("active");
            newTab.IsEnabled = true;
        }
        
        // Enable future tabs if going forward
        if (newStep > _currentStep)
        {
            for (int i = (int)_currentStep + 1; i <= (int)newStep; i++)
            {
                if (_tabButtons[i] != null)
                {
                    _tabButtons[i].IsEnabled = true;
                }
            }
        }
        
        // Animate in new panel
        if (newPanel != null)
        {
            newPanel.IsVisible = true;
            var slideIn = new TranslateTransform();
            newPanel.RenderTransform = slideIn;
            
            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(300),
                Easing = new CubicEaseOut(),
                Children =
                {
                    new KeyFrame
                    {
                        Setters = 
                        { 
                            new Setter(TranslateTransform.XProperty, newStep > _currentStep ? 50.0 : -50.0),
                            new Setter(Border.OpacityProperty, 0.0)
                        },
                        Cue = new Cue(0)
                    },
                    new KeyFrame
                    {
                        Setters = 
                        { 
                            new Setter(TranslateTransform.XProperty, 0.0),
                            new Setter(Border.OpacityProperty, 1.0)
                        },
                        Cue = new Cue(1)
                    }
                }
            };
            
            await animation.RunAsync(slideIn);
            newPanel.Opacity = 1;
        }
        
        _currentStep = newStep;
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        // Update status text
        if (_statusText != null)
        {
            _statusText.Text = $"Step {(int)_currentStep + 1} of 5";
        }
        
        // Update back button visibility
        if (_backButton != null)
        {
            _backButton.IsVisible = _currentStep != WizardStep.Select;
        }
        
        // Update next button text
        if (_nextButton != null)
        {
            switch (_currentStep)
            {
                case WizardStep.Select:
                    _nextButton.Content = "NEXT →";
                    _nextButton.IsEnabled = _selectedFilterPath != null || _isNewFilter;
                    break;
                    
                case WizardStep.Build:
                    _nextButton.Content = "CONFIGURE →";
                    break;
                    
                case WizardStep.Configure:
                    _nextButton.Content = "🔍 START SEARCH";
                    _nextButton.Classes.Remove("wizard-action");
                    _nextButton.Classes.Add("wizard-action");
                    _nextButton.Background = Application.Current!.FindResource("Green") as IBrush;
                    break;
                    
                case WizardStep.Search:
                    if (_isSearching)
                    {
                        _nextButton.Content = "⏸ PAUSE";
                        _nextButton.Background = Application.Current!.FindResource("Orange") as IBrush;
                    }
                    else
                    {
                        _nextButton.Content = "VIEW RESULTS →";
                        _nextButton.Background = Application.Current!.FindResource("Blue") as IBrush;
                    }
                    break;
                    
                case WizardStep.Results:
                    _nextButton.Content = "✓ DONE";
                    _nextButton.Background = Application.Current!.FindResource("Green") as IBrush;
                    break;
            }
        }
    }
    
    private void StartSearch()
    {
        _isSearching = true;
        UpdateUI();
        
        // TODO: Actually start the search process
        DebugLogger.Log("Starting search with filter: " + _selectedFilterPath);
    }
    
    private void StopSearch()
    {
        _isSearching = false;
        UpdateUI();
        
        // TODO: Actually stop the search process
        DebugLogger.Log("Stopping search");
    }
    
    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (_isSearching)
        {
            StopSearch();
        }
        
        Cancelled?.Invoke(this, EventArgs.Empty);
    }
    
    // Tab click handlers
    private async void OnSelectTabClick(object? sender, RoutedEventArgs e)
    {
        if (_currentStep != WizardStep.Select)
            await TransitionTo(WizardStep.Select);
    }
    
    private async void OnBuildTabClick(object? sender, RoutedEventArgs e)
    {
        if (_currentStep != WizardStep.Build && _buildTab?.IsEnabled == true)
            await TransitionTo(WizardStep.Build);
    }
    
    private async void OnConfigureTabClick(object? sender, RoutedEventArgs e)
    {
        if (_currentStep != WizardStep.Configure && _configureTab?.IsEnabled == true)
            await TransitionTo(WizardStep.Configure);
    }
    
    private async void OnSearchTabClick(object? sender, RoutedEventArgs e)
    {
        if (_currentStep != WizardStep.Search && _searchTab?.IsEnabled == true)
            await TransitionTo(WizardStep.Search);
    }
    
    private async void OnResultsTabClick(object? sender, RoutedEventArgs e)
    {
        if (_currentStep != WizardStep.Results && _resultsTab?.IsEnabled == true)
            await TransitionTo(WizardStep.Results);
    }
    
    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Don't process if a text input has focus
        if (e.Source is TextBox || e.Source is NumericUpDown)
            return;
        
        switch (e.Key)
        {
            // Enter - proceed to next step
            case Key.Enter:
                if (_nextButton?.IsEnabled == true)
                {
                    OnNextClick(null, null!);
                    e.Handled = true;
                }
                break;
            
            // Escape - cancel or go back
            case Key.Escape:
                if (_currentStep == WizardStep.Select)
                {
                    OnCancelClick(null, null!);
                }
                else if (_backButton?.IsVisible == true)
                {
                    OnBackClick(null, null!);
                }
                e.Handled = true;
                break;
            
            // Backspace - go back
            case Key.Back:
                if (_backButton?.IsVisible == true)
                {
                    OnBackClick(null, null!);
                    e.Handled = true;
                }
                break;
            
            // Tab - navigate forward through steps
            case Key.Tab:
                if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    // Forward tab - go to next enabled step
                    await NavigateToNextStep();
                    e.Handled = true;
                }
                else
                {
                    // Shift+Tab - go to previous step
                    await NavigateToPreviousStep();
                    e.Handled = true;
                }
                break;
            
            // Number keys 1-5 - jump to specific step
            case Key.D1:
            case Key.NumPad1:
                if (_selectTab?.IsEnabled == true)
                    await TransitionTo(WizardStep.Select);
                e.Handled = true;
                break;
            
            case Key.D2:
            case Key.NumPad2:
                if (_buildTab?.IsEnabled == true)
                    await TransitionTo(WizardStep.Build);
                e.Handled = true;
                break;
            
            case Key.D3:
            case Key.NumPad3:
                if (_configureTab?.IsEnabled == true)
                    await TransitionTo(WizardStep.Configure);
                e.Handled = true;
                break;
            
            case Key.D4:
            case Key.NumPad4:
                if (_searchTab?.IsEnabled == true)
                    await TransitionTo(WizardStep.Search);
                e.Handled = true;
                break;
            
            case Key.D5:
            case Key.NumPad5:
                if (_resultsTab?.IsEnabled == true)
                    await TransitionTo(WizardStep.Results);
                e.Handled = true;
                break;
            
            // Arrow keys for navigation
            case Key.Left:
                if (_backButton?.IsVisible == true)
                {
                    OnBackClick(null, null!);
                    e.Handled = true;
                }
                break;
            
            case Key.Right:
                if (_nextButton?.IsEnabled == true)
                {
                    OnNextClick(null, null!);
                    e.Handled = true;
                }
                break;
            
            // Space - trigger primary action (same as Enter)
            case Key.Space:
                if (_currentStep == WizardStep.Configure && _nextButton?.IsEnabled == true)
                {
                    // Start search
                    OnNextClick(null, null!);
                    e.Handled = true;
                }
                else if (_currentStep == WizardStep.Search && _isSearching)
                {
                    // Pause search (toggle)
                    _isSearching = false;
                    UpdateUI();
                    e.Handled = true;
                }
                break;
            
            // F5 - refresh filter list (on Select step)
            case Key.F5:
                if (_currentStep == WizardStep.Select && _filterPicker != null)
                {
                    await _filterPicker.RefreshFilters();
                    e.Handled = true;
                }
                break;
        }
    }
    
    private async Task NavigateToNextStep()
    {
        var nextStep = _currentStep;
        
        // Find next enabled step
        for (int i = (int)_currentStep + 1; i < 5; i++)
        {
            if (_tabButtons[i]?.IsEnabled == true)
            {
                nextStep = (WizardStep)i;
                break;
            }
        }
        
        if (nextStep != _currentStep)
        {
            await TransitionTo(nextStep);
        }
    }
    
    private async Task NavigateToPreviousStep()
    {
        var prevStep = _currentStep;
        
        // Find previous step (all previous steps should be enabled)
        if ((int)_currentStep > 0)
        {
            prevStep = (WizardStep)((int)_currentStep - 1);
            await TransitionTo(prevStep);
        }
    }
}