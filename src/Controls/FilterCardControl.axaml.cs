using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Oracle.Services;
using Oracle.Helpers;

namespace Oracle.Controls
{
    public partial class FilterCardControl : UserControl
    {
        private SpriteService? _spriteService;
        private IImage? _debuffedOverlay;
        
        public static readonly StyledProperty<List<string>> NeedsProperty =
            AvaloniaProperty.Register<FilterCardControl, List<string>>(nameof(Needs), new List<string>());
            
        public static readonly StyledProperty<List<string>> WantsProperty =
            AvaloniaProperty.Register<FilterCardControl, List<string>>(nameof(Wants), new List<string>());
            
        public static readonly StyledProperty<List<string>> MustNotsProperty =
            AvaloniaProperty.Register<FilterCardControl, List<string>>(nameof(MustNots), new List<string>());
        
        public List<string> Needs
        {
            get => GetValue(NeedsProperty);
            set => SetValue(NeedsProperty, value);
        }
        
        public List<string> Wants
        {
            get => GetValue(WantsProperty);
            set => SetValue(WantsProperty, value);
        }
        
        public List<string> MustNots
        {
            get => GetValue(MustNotsProperty);
            set => SetValue(MustNotsProperty, value);
        }
        
        public FilterCardControl()
        {
            InitializeComponent();
            _spriteService = ServiceHelper.GetService<SpriteService>();
            
            // Load debuffed overlay (rightmost edition)
            _debuffedOverlay = _spriteService?.GetEditionImage("Debuffed");
        }
        
        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }
        
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            
            if (change.Property == NeedsProperty || 
                change.Property == WantsProperty || 
                change.Property == MustNotsProperty)
            {
                UpdatePreview();
            }
        }
        
        private void UpdatePreview()
        {
            var needsPanel = this.FindControl<StackPanel>("NeedsPanel");
            var wantsPanel = this.FindControl<StackPanel>("WantsPanel");
            var mustNotsPanel = this.FindControl<StackPanel>("MustNotsPanel");
            var emptyText = this.FindControl<TextBlock>("EmptyText");
            
            var needsItems = this.FindControl<ItemsControl>("NeedsItems");
            var wantsItems = this.FindControl<ItemsControl>("WantsItems");
            var mustNotsItems = this.FindControl<ItemsControl>("MustNotsItems");
            
            // Clear existing items
            needsItems?.Items.Clear();
            wantsItems?.Items.Clear();
            mustNotsItems?.Items.Clear();
            
            // Add needs
            var hasItems = false;
            if (Needs?.Count > 0 && needsPanel != null && needsItems != null)
            {
                needsPanel.IsVisible = true;
                foreach (var item in Needs.Take(5)) // Limit to 5 items
                {
                    var image = CreateItemImage(item);
                    if (image != null)
                    {
                        needsItems.Items.Add(image);
                        hasItems = true;
                    }
                }
            }
            else if (needsPanel != null)
            {
                needsPanel.IsVisible = false;
            }
            
            // Add wants
            if (Wants?.Count > 0 && wantsPanel != null && wantsItems != null)
            {
                wantsPanel.IsVisible = true;
                foreach (var item in Wants.Take(5)) // Limit to 5 items
                {
                    var image = CreateItemImage(item);
                    if (image != null)
                    {
                        wantsItems.Items.Add(image);
                        hasItems = true;
                    }
                }
            }
            else if (wantsPanel != null)
            {
                wantsPanel.IsVisible = false;
            }
            
            // Add must nots with debuffed overlay
            if (MustNots?.Count > 0 && mustNotsPanel != null && mustNotsItems != null)
            {
                mustNotsPanel.IsVisible = true;
                foreach (var item in MustNots.Take(5)) // Limit to 5 items
                {
                    var panel = CreateDebuffedItem(item);
                    if (panel != null)
                    {
                        mustNotsItems.Items.Add(panel);
                        hasItems = true;
                    }
                }
            }
            else if (mustNotsPanel != null)
            {
                mustNotsPanel.IsVisible = false;
            }
            
            // Show/hide empty text
            if (emptyText != null)
            {
                emptyText.IsVisible = !hasItems;
            }
        }
        
        private Image? CreateItemImage(string itemName)
        {
            if (_spriteService == null) return null;
            
            // Try to get sprite - check jokers first, then other types
            IImage? sprite = null;
            
            // Convert underscores to match sprite names
            var spriteName = itemName.Replace("_", " ");
            
            // Try joker
            sprite = _spriteService.GetJokerImage(spriteName);
            
            // Try tarot
            if (sprite == null)
                sprite = _spriteService.GetTarotImage(spriteName);
                
            // Try planet (use GetItemImage with type)
            if (sprite == null)
                sprite = _spriteService.GetItemImage(spriteName, "Planet");
                
            // Try spectral
            if (sprite == null)
                sprite = _spriteService.GetSpectralImage(spriteName);
                
            // Try voucher
            if (sprite == null)
                sprite = _spriteService.GetVoucherImage(spriteName);
                
            // Try tag
            if (sprite == null)
                sprite = _spriteService.GetTagImage(spriteName);
            
            if (sprite != null)
            {
                return new Image 
                { 
                    Source = sprite,
                    Classes = { "mini-item" }
                };
            }
            
            return null;
        }
        
        private Panel? CreateDebuffedItem(string itemName)
        {
            var baseImage = CreateItemImage(itemName);
            if (baseImage == null) return null;
            
            var panel = new Panel
            {
                Classes = { "debuffed-item" }
            };
            
            // Add base image
            panel.Children.Add(baseImage);
            
            // Add debuffed overlay if available
            if (_debuffedOverlay != null)
            {
                var overlay = new Image
                {
                    Source = _debuffedOverlay,
                    Width = 24,
                    Height = 24,
                    Opacity = 0.8
                };
                panel.Children.Add(overlay);
            }
            
            return panel;
        }
    }
}