using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Oracle.Services;

namespace Oracle.Controls
{
    public partial class ItemConfigPopup : UserControl
    {
        public event EventHandler<ItemConfigEventArgs>? ConfigApplied;
        public event EventHandler? DeleteRequested;
        public event EventHandler? Cancelled;
        
        private string _itemKey = "";
        private double _sliderWidth = 260;
        private int _minAnte = 1;
        private int _maxAnte = 8;
        private double _thumbOffset = 10; // margin offset for thumbs
        
        public ItemConfigPopup()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            // Initialize thumbs after loading
            Dispatcher.UIThread.Post(() => {
                UpdateThumbPositions();
                UpdateAnteRangeText();
                LoadEditionImages();
            });
        }
        
        private void LoadEditionImages()
        {
            var spriteService = SpriteService.Instance;
            
            // Load Normal edition image
            var normalImage = this.Find<Image>("EditionNormalImage");
            if (normalImage != null)
            {
                normalImage.Source = spriteService.GetEditionImage("normal");
            }
            
            // Load Foil edition image
            var foilImage = this.Find<Image>("EditionFoilImage");
            if (foilImage != null)
            {
                foilImage.Source = spriteService.GetEditionImage("foil");
            }
            
            // Load Holographic edition image
            var holoImage = this.Find<Image>("EditionHoloImage");
            if (holoImage != null)
            {
                holoImage.Source = spriteService.GetEditionImage("holographic");
            }
            
            // Load Polychrome edition image
            var polyImage = this.Find<Image>("EditionPolyImage");
            if (polyImage != null)
            {
                polyImage.Source = spriteService.GetEditionImage("polychrome");
            }
            
            // Load Negative edition image
            var negativeImage = this.Find<Image>("EditionNegativeImage");
            if (negativeImage != null)
            {
                negativeImage.Source = spriteService.GetEditionImage("negative");
            }
        }
        
        public void SetItem(string itemKey, string itemName, ItemConfig? existingConfig = null)
        {
            _itemKey = itemKey;
            
            var nameText = this.FindControl<TextBlock>("ItemNameText");
            if (nameText != null)
                nameText.Text = itemName;
            
            if (existingConfig != null)
            {
                // Load existing ante configuration for range slider
                if (existingConfig.SearchAntes != null && existingConfig.SearchAntes.Count > 0)
                {
                    _minAnte = existingConfig.SearchAntes.Min();
                    _maxAnte = existingConfig.SearchAntes.Max();
                    UpdateThumbPositions();
                    UpdateAnteRangeText();
                }
                else
                {
                    // Default to full range
                    _minAnte = 1;
                    _maxAnte = 8;
                    UpdateThumbPositions();
                    UpdateAnteRangeText();
                }
                
                // Set edition
                if (!string.IsNullOrEmpty(existingConfig.Edition))
                {
                    switch (existingConfig.Edition.ToLower())
                    {
                        case "foil":
                            SetRadioButton("EditionFoil");
                            break;
                        case "holographic":
                            SetRadioButton("EditionHolo");
                            break;
                        case "polychrome":
                            SetRadioButton("EditionPoly");
                            break;
                        case "negative":
                            SetRadioButton("EditionNegative");
                            break;
                        default:
                            SetRadioButton("EditionNormal");
                            break;
                    }
                }
                
                // Set sources
                if (existingConfig.Sources != null)
                {
                    SetCheckBox("SourceTags", existingConfig.Sources.Contains("tag"));
                    SetCheckBox("SourcePacks", existingConfig.Sources.Contains("booster"));
                    SetCheckBox("SourceShop", existingConfig.Sources.Contains("shop"));
                }
            }
        }
        
        private void SetRadioButton(string name)
        {
            var radio = this.FindControl<RadioButton>(name);
            if (radio != null)
                radio.IsChecked = true;
        }
        
        private void SetCheckBox(string name, bool isChecked)
        {
            var check = this.FindControl<CheckBox>(name);
            if (check != null)
                check.IsChecked = isChecked;
        }
        
        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            var config = new ItemConfig
            {
                ItemKey = _itemKey,
                SearchAntes = GetSelectedAntes(),
                Edition = GetSelectedEdition(),
                Sources = GetSelectedSources()
            };
            
            ConfigApplied?.Invoke(this, new ItemConfigEventArgs { Config = config });
        }
        
        private void OnDeleteClick(object? sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
        }
        
        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }
        
        private List<int>? GetSelectedAntes()
        {
            // Return the range of antes from min to max
            if (_minAnte == 1 && _maxAnte == 8)
            {
                // Full range means "any ante"
                return null;
            }
            
            var antes = new List<int>();
            for (int i = _minAnte; i <= _maxAnte; i++)
            {
                antes.Add(i);
            }
            return antes;
        }
        
        public string GetItem()
        {
            return _itemKey;
        }
        
        private void OnMinThumbDragDelta(object? sender, Avalonia.Input.VectorEventArgs e)
        {
            var minThumb = this.FindControl<Thumb>("MinThumb");
            var maxThumb = this.FindControl<Thumb>("MaxThumb");
            if (minThumb == null || maxThumb == null) return;
            
            // Calculate new position
            var currentMargin = minThumb.Margin;
            var newX = Math.Max(0, Math.Min(_sliderWidth, currentMargin.Left + e.Vector.X));
            
            // Don't allow min to go past max
            var maxX = maxThumb.Margin.Left;
            newX = Math.Min(newX, maxX);
            
            // Update position
            minThumb.Margin = new Thickness(newX, 0, 0, 0);
            
            // Update ante value
            _minAnte = PositionToAnte(newX);
            
            // Update UI
            UpdateActiveRange();
            UpdateAnteRangeText();
        }
        
        private void OnMaxThumbDragDelta(object? sender, Avalonia.Input.VectorEventArgs e)
        {
            var minThumb = this.FindControl<Thumb>("MinThumb");
            var maxThumb = this.FindControl<Thumb>("MaxThumb");
            if (minThumb == null || maxThumb == null) return;
            
            // Calculate new position
            var currentMargin = maxThumb.Margin;
            var newX = Math.Max(0, Math.Min(_sliderWidth, currentMargin.Left + e.Vector.X));
            
            // Don't allow max to go before min
            var minX = minThumb.Margin.Left;
            newX = Math.Max(newX, minX);
            
            // Update position
            maxThumb.Margin = new Thickness(newX, 0, 0, 0);
            
            // Update ante value
            _maxAnte = PositionToAnte(newX);
            
            // Update UI
            UpdateActiveRange();
            UpdateAnteRangeText();
        }
        
        private int PositionToAnte(double position)
        {
            // Convert pixel position to ante number (1-8)
            var ante = (int)Math.Round((position / _sliderWidth) * 7) + 1;
            return Math.Max(1, Math.Min(8, ante));
        }
        
        private double AnteToPosition(int ante)
        {
            // Convert ante number to pixel position
            return ((ante - 1) / 7.0) * _sliderWidth;
        }
        
        private void UpdateThumbPositions()
        {
            var minThumb = this.FindControl<Thumb>("MinThumb");
            var maxThumb = this.FindControl<Thumb>("MaxThumb");
            if (minThumb == null || maxThumb == null) return;
            
            minThumb.Margin = new Thickness(AnteToPosition(_minAnte), 0, 0, 0);
            maxThumb.Margin = new Thickness(AnteToPosition(_maxAnte), 0, 0, 0);
            
            UpdateActiveRange();
        }
        
        private void UpdateActiveRange()
        {
            var activeRange = this.FindControl<Border>("ActiveRange");
            var minThumb = this.FindControl<Thumb>("MinThumb");
            var maxThumb = this.FindControl<Thumb>("MaxThumb");
            if (activeRange == null || minThumb == null || maxThumb == null) return;
            
            var minX = minThumb.Margin.Left;
            var maxX = maxThumb.Margin.Left;
            
            activeRange.Margin = new Thickness(_thumbOffset + minX, 0, 0, 0);
            activeRange.Width = maxX - minX;
        }
        
        private void UpdateAnteRangeText()
        {
            var rangeText = this.FindControl<TextBlock>("AnteRangeText");
            var minText = this.FindControl<TextBlock>("MinAnteText");
            var maxText = this.FindControl<TextBlock>("MaxAnteText");
            
            if (rangeText != null)
            {
                if (_minAnte == 1 && _maxAnte == 8)
                {
                    rangeText.Text = "Any";
                }
                else if (_minAnte == _maxAnte)
                {
                    rangeText.Text = $"Ante {_minAnte}";
                }
                else
                {
                    rangeText.Text = $"{_minAnte}-{_maxAnte}";
                }
            }
            
            if (minText != null) minText.Text = _minAnte.ToString();
            if (maxText != null) maxText.Text = _maxAnte.ToString();
        }
        
        private string GetSelectedEdition()
        {
            if (this.FindControl<RadioButton>("EditionFoil")?.IsChecked == true) return "foil";
            if (this.FindControl<RadioButton>("EditionHolo")?.IsChecked == true) return "holographic";
            if (this.FindControl<RadioButton>("EditionPoly")?.IsChecked == true) return "polychrome";
            if (this.FindControl<RadioButton>("EditionNegative")?.IsChecked == true) return "negative";
            return "none";
        }
        
        private List<string> GetSelectedSources()
        {
            var sources = new List<string>();
            
            if (this.FindControl<CheckBox>("SourceTags")?.IsChecked == true) sources.Add("tag");
            if (this.FindControl<CheckBox>("SourcePacks")?.IsChecked == true) sources.Add("booster");
            if (this.FindControl<CheckBox>("SourceShop")?.IsChecked == true) sources.Add("shop");
            
            // Default to main sources if none selected
            if (sources.Count == 0)
            {
                sources.AddRange(new[] { "tag", "booster", "shop" });
            }
            
            return sources;
        }
    }
    
    public class ItemConfigEventArgs : EventArgs
    {
        public ItemConfig Config { get; set; } = new();
    }
    
    public class ItemConfig
    {
        public string ItemKey { get; set; } = "";
        public List<int>? SearchAntes { get; set; }
        public string Edition { get; set; } = "none";
        public List<string> Sources { get; set; } = new();
    }
}