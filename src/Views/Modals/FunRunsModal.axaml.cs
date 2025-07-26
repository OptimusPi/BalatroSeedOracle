using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Oracle.Controls;
using Oracle.Helpers;
using Oracle.Services;

namespace Oracle.Views.Modals
{
    public partial class FunRunsModal : UserControl
    {
        private ItemsControl? _curatedSeedsGrid;
        private ItemsControl? _communityRunsGrid;
        private TextBox? _selectedFilterPath;
        private TextBox? _funRunDescription;
        private TagEditor? _funRunTags;
        private Button? _addFunRunButton;
        
        private string? _selectedOuijaPath;
        
        public class CuratedSeed
        {
            public string Name { get; set; } = "";
            public string JokerName { get; set; } = "";
            public string Seed { get; set; } = "";
            public IImage? ImageSource { get; set; }
            public List<string> Tags { get; set; } = new List<string>();
        }
        
        public class CommunityFunRun
        {
            public string ConfigName { get; set; } = "";
            public string Description { get; set; } = "";
            public string OuijaPath { get; set; } = "";
            public string ConfigContent { get; set; } = "";
            public string Seed { get; set; } = "";
            public string Author { get; set; } = "";
            public List<string> Tags { get; set; } = new List<string>();
            public string ChallengeUrl => $"https://balatrogenie.app/challenge/{Seed}";
        }
        
        public FunRunsModal()
        {
            InitializeComponent();
            
            _curatedSeedsGrid = this.FindControl<ItemsControl>("CuratedSeedsGrid");
            _communityRunsGrid = this.FindControl<ItemsControl>("CommunityRunsGrid");
            _selectedFilterPath = this.FindControl<TextBox>("SelectedFilterPath");
            _funRunDescription = this.FindControl<TextBox>("FunRunDescription");
            _funRunTags = this.FindControl<TagEditor>("FunRunTags");
            _addFunRunButton = this.FindControl<Button>("AddFunRunButton");
            
            LoadCuratedSeeds();
            LoadCommunityRuns();
            
            // Enable Add button when both fields are filled
            if (_selectedFilterPath != null && _funRunDescription != null)
            {
                _funRunDescription.PropertyChanged += (s, e) =>
                {
                    if (e.Property.Name == "Text")
                        UpdateAddButtonState();
                };
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private void LoadCuratedSeeds()
        {
            if (_curatedSeedsGrid == null) return;
            
            var spriteService = SpriteService.Instance;
            var curatedSeeds = new List<CuratedSeed>();
            
            // Add Wee Joker Fun Run
            curatedSeeds.Add(new CuratedSeed
            {
                Name = "Wee Joker Fun Run",
                JokerName = "Wee Joker",
                Seed = "ALEEB123",
                ImageSource = spriteService.GetJokerImage("Wee Joker"),
                Tags = new List<string> { "#WeeJoker", "#EarlyGame", "#Chips" }
            });
            
            // Add 4 more curated fun runs
            curatedSeeds.Add(new CuratedSeed
            {
                Name = "Spectral Rush",
                JokerName = "Joker",
                Seed = "GHOST999",
                ImageSource = spriteService.GetJokerImage("Joker"),
                Tags = new List<string> { "#Spectral", "#Soul", "#Ankh", "#Wraith" }
            });
            
            curatedSeeds.Add(new CuratedSeed
            {
                Name = "Voucher Hunt",
                JokerName = "Jolly Joker",
                Seed = "VOUCH777",
                ImageSource = spriteService.GetJokerImage("Jolly Joker"),
                Tags = new List<string> { "#Voucher", "#Money", "#Rerolls" }
            });
            
            curatedSeeds.Add(new CuratedSeed
            {
                Name = "Tag Team",
                JokerName = "Zany Joker", 
                Seed = "TAGS4ALL",
                ImageSource = spriteService.GetJokerImage("Zany Joker"),
                Tags = new List<string> { "#Negative", "#Foil", "#Holographic", "#Polychrome" }
            });
            
            curatedSeeds.Add(new CuratedSeed
            {
                Name = "Boss Rush",
                JokerName = "Mad Joker",
                Seed = "BOSSFITE",
                ImageSource = spriteService.GetJokerImage("Mad Joker"),
                Tags = new List<string> { "#Boss", "#Ante8", "#LateGame", "#XMult" }
            });
            
            _curatedSeedsGrid.ItemsSource = curatedSeeds;
        }
        
        private void LoadCommunityRuns()
        {
            if (_communityRunsGrid == null) return;
            
            var communityRuns = new List<CommunityFunRun>();
            
            // TODO: Load actual community runs from a database or file
            // For now, add some placeholder examples
            communityRuns.Add(new CommunityFunRun
            {
                ConfigName = "Five Legendaries",
                Description = "WOW, FIVE LEGENDARIES by Ante 8!",
                OuijaPath = "five_legendaries.ouija.json",
                ConfigContent = "{}",
                Seed = "5LEGEND5",
                Author = "tacodiva",
                Tags = new List<string> { "#Legendary", "#Faceless", "#Brainstorm", "#Blueprint", "#LateGame" }
            });
            
            communityRuns.Add(new CommunityFunRun
            {
                ConfigName = "Voucher Madness",
                Description = "Find Blank Voucher + Antimatter",
                OuijaPath = "voucher_madness.ouija.json",
                ConfigContent = "{}",
                Seed = "ANTIMATT",
                Author = "pifreak",
                Tags = new List<string> { "#Voucher", "#Gold", "#HandSize", "#Ante8" }
            });
            
            communityRuns.Add(new CommunityFunRun
            {
                ConfigName = "Spectral Party",
                Description = "Soul + Ankh + Wraith combo",
                OuijaPath = "spectral_party.ouija.json",
                ConfigContent = "{}",
                Seed = "SPOOKY88",
                Author = "OptimusPi",
                Tags = new List<string> { "#Spectral", "#Soul", "#Ankh", "#Wraith", "#Cryptid" }
            });
            
            _communityRunsGrid.ItemsSource = communityRuns;
        }
        
        private async void OnCuratedSeedClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CuratedSeed seed)
            {
                DebugLogger.Log("FunRunsModal", $"Clicked curated seed: {seed.Name} ({seed.Seed})");
                
                // Generate challenge message
                var challengeUrl = $"https://balatrogenie.app/challenge/{seed.Seed}";
                var message = $"You have been challenged to a Balatro Fun Run curated by pifreak! " +
                             $"The seed is {seed.Seed}. " +
                             $"The theme is {seed.Name}. " +
                             $"Good luck, have fun!\n\n" +
                             $"Challenge link: {challengeUrl}";
                
                await ClipboardService.CopyToClipboardAsync(message);
            }
        }
        
        private void OnCommunityRunClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CommunityFunRun run)
            {
                DebugLogger.Log("FunRunsModal", $"Clicked community run: {run.ConfigName}");
                // TODO: Load the ouija config and start searching
            }
        }
        
        private async void OnCopyChallengeClick(object? sender, RoutedEventArgs e)
        {
            // Stop event from bubbling to parent button
            e.Handled = true;
            
            if (sender is Button button && button.Tag is CommunityFunRun run)
            {
                DebugLogger.Log("FunRunsModal", $"Copying challenge for: {run.ConfigName}");
                
                // Generate challenge message
                var message = $"You have been challenged to a Balatro Fun Run curated by {run.Author}! " +
                             $"The seed is {run.Seed}. " +
                             $"The theme is {run.ConfigName}. " +
                             $"Good luck, have fun!\n\n" +
                             $"Challenge link: {run.ChallengeUrl}";
                
                await ClipboardService.CopyToClipboardAsync(message);
            }
        }
        
        private async void OnBrowseFilterClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            
            var options = new FilePickerOpenOptions
            {
                Title = "Select Ouija Config",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Ouija Config") { Patterns = new[] { "*.ouija.json" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            };
            
            var result = await topLevel.StorageProvider.OpenFilePickerAsync(options);
            if (result.Count > 0)
            {
                var file = result[0];
                _selectedOuijaPath = file.Path.LocalPath;
                
                if (_selectedFilterPath != null)
                {
                    _selectedFilterPath.Text = Path.GetFileName(_selectedOuijaPath);
                }
                
                UpdateAddButtonState();
            }
        }
        
        private async void OnAddCustomFunRun(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedOuijaPath) || 
                string.IsNullOrWhiteSpace(_funRunDescription?.Text))
                return;
            
            try
            {
                // Read the config content
                var configContent = await File.ReadAllTextAsync(_selectedOuijaPath);
                
                // Check if this config already exists
                if (_communityRunsGrid?.ItemsSource is List<CommunityFunRun> runs)
                {
                    var existingRun = runs.FirstOrDefault(r => 
                        r.ConfigContent == configContent || 
                        r.OuijaPath == Path.GetFileName(_selectedOuijaPath));
                    
                    if (existingRun != null)
                    {
                        DebugLogger.Log("FunRunsModal", "This config already exists in community runs!");
                        // TODO: Show error dialog
                        return;
                    }
                    
                    // Generate a random 8-character seed
                    var random = new Random();
                    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                    var seed = new string(Enumerable.Repeat(chars, 8)
                        .Select(s => s[random.Next(s.Length)]).ToArray());
                    
                    // Add the new fun run
                    var newRun = new CommunityFunRun
                    {
                        ConfigName = Path.GetFileNameWithoutExtension(_selectedOuijaPath).Replace(".ouija", ""),
                        Description = _funRunDescription.Text,
                        OuijaPath = Path.GetFileName(_selectedOuijaPath),
                        ConfigContent = configContent,
                        Seed = seed,
                        Author = "You", // TODO: Get from user settings/profile
                        Tags = _funRunTags?.Tags ?? new List<string>()
                    };
                    
                    runs.Add(newRun);
                    _communityRunsGrid.ItemsSource = null; // Force refresh
                    _communityRunsGrid.ItemsSource = runs;
                    
                    // Clear the form
                    _selectedFilterPath!.Text = "No filter selected...";
                    _funRunDescription.Text = "";
                    _selectedOuijaPath = null;
                    if (_funRunTags != null)
                        _funRunTags.Tags = new List<string>();
                    UpdateAddButtonState();
                    
                    DebugLogger.Log("FunRunsModal", $"Added new fun run: {newRun.ConfigName}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FunRunsModal", $"Error adding fun run: {ex.Message}");
            }
        }
        
        private void UpdateAddButtonState()
        {
            if (_addFunRunButton != null)
            {
                _addFunRunButton.IsEnabled = 
                    !string.IsNullOrWhiteSpace(_selectedOuijaPath) &&
                    !string.IsNullOrWhiteSpace(_funRunDescription?.Text);
            }
        }
    }
}