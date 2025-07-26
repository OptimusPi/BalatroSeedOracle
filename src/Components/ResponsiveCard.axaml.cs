using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Oracle.Models;
using Oracle.Services;
using Oracle.Helpers;
using System;
using System.Linq;
using Avalonia.Threading;

namespace Oracle.Components
{
    public partial class ResponsiveCard : UserControl
    {
        private Border _cardBorder;
        private Image _cardImage;
        private Image _soulImage;
        private TextBlock _cardName;
        private string _currentBreakpoint = "desktop";
        private System.Threading.Timer? _soulAnimationTimer;
        
        public static readonly StyledProperty<string> ItemNameProperty =
            AvaloniaProperty.Register<ResponsiveCard, string>(nameof(ItemName), "");
        
        public static readonly StyledProperty<string> CategoryProperty =
            AvaloniaProperty.Register<ResponsiveCard, string>(nameof(Category), "");
        
        public static readonly StyledProperty<IImage?> ImageSourceProperty =
            AvaloniaProperty.Register<ResponsiveCard, IImage?>(nameof(ImageSource));
        
        public static readonly StyledProperty<bool> IsSelectedNeedProperty =
            AvaloniaProperty.Register<ResponsiveCard, bool>(nameof(IsSelectedNeed));
        
        public static readonly StyledProperty<bool> IsSelectedWantProperty =
            AvaloniaProperty.Register<ResponsiveCard, bool>(nameof(IsSelectedWant));
        
        public string ItemName
        {
            get => GetValue(ItemNameProperty);
            set => SetValue(ItemNameProperty, value);
        }
        
        public string Category
        {
            get => GetValue(CategoryProperty);
            set => SetValue(CategoryProperty, value);
        }
        
        public IImage? ImageSource
        {
            get => GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }
        
        public bool IsSelectedNeed
        {
            get => GetValue(IsSelectedNeedProperty);
            set => SetValue(IsSelectedNeedProperty, value);
        }
        
        public bool IsSelectedWant
        {
            get => GetValue(IsSelectedWantProperty);
            set => SetValue(IsSelectedWantProperty, value);
        }
        
        // Events
        public event EventHandler<CardClickEventArgs>? CardClicked;
        public event EventHandler<CardDragEventArgs>? CardDragStarted;
        
        public ResponsiveCard()
        {
            InitializeComponent();
            
            _cardBorder = this.FindControl<Border>("CardBorder")!;
            _cardImage = this.FindControl<Image>("CardImage")!;
            _soulImage = this.FindControl<Image>("SoulImage")!;
            _cardName = this.FindControl<TextBlock>("CardName")!;
            
            // Property change handlers
            this.PropertyChanged += OnPropertyChanged;
            
            // Event handlers
            _cardBorder.PointerPressed += OnPointerPressed;
            _cardBorder.PointerMoved += OnPointerMoved;
            
            // Listen for parent size changes to update breakpoint
            this.AttachedToVisualTree += OnAttachedToVisualTree;
            this.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            StopSoulAnimation();
        }
        
        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (this.Parent is Control parent)
            {
                parent.SizeChanged += OnParentSizeChanged;
                UpdateBreakpoint(parent.Bounds.Width);
            }
        }
        
        private void OnParentSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            UpdateBreakpoint(e.NewSize.Width);
        }
        
        private void UpdateBreakpoint(double parentWidth)
        {
            string newBreakpoint = parentWidth switch
            {
                < 768 => "mobile",
                < 1024 => "tablet",
                < 1440 => "desktop",
                _ => "large-desktop"
            };
            
            if (newBreakpoint != _currentBreakpoint)
            {
                // Remove old breakpoint classes
                _cardBorder.Classes.Remove(_currentBreakpoint);
                _cardImage.Classes.Remove(_currentBreakpoint);
                _cardName.Classes.Remove(_currentBreakpoint);
                
                // Add new breakpoint classes
                _currentBreakpoint = newBreakpoint;
                _cardBorder.Classes.Add(_currentBreakpoint);
                _cardImage.Classes.Add(_currentBreakpoint);
                _cardName.Classes.Add(_currentBreakpoint);
            }
        }
        
        private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ItemNameProperty)
            {
                _cardName.Text = FormatItemName(ItemName);
                CheckAndLoadLegendarySoul();
            }
            else if (e.Property == ImageSourceProperty)
            {
                _cardImage.Source = ImageSource;
                CheckAndLoadLegendarySoul();
            }
            else if (e.Property == CategoryProperty)
            {
                CheckAndLoadLegendarySoul();
            }
            else if (e.Property == IsSelectedNeedProperty)
            {
                if (IsSelectedNeed)
                {
                    _cardBorder.Classes.Add("selected-need");
                    _cardBorder.Classes.Remove("selected-want");
                }
                else
                {
                    _cardBorder.Classes.Remove("selected-need");
                }
            }
            else if (e.Property == IsSelectedWantProperty)
            {
                if (IsSelectedWant)
                {
                    _cardBorder.Classes.Add("selected-want");
                    _cardBorder.Classes.Remove("selected-need");
                }
                else
                {
                    _cardBorder.Classes.Remove("selected-want");
                }
            }
        }
        
        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(this);
            var clickType = pointer.Properties.PointerUpdateKind switch
            {
                PointerUpdateKind.LeftButtonPressed => CardClickType.LeftClick,
                PointerUpdateKind.RightButtonPressed => CardClickType.RightClick,
                _ => CardClickType.LeftClick // Default to left click
            };
            
            CardClicked?.Invoke(this, new CardClickEventArgs(ItemName, Category, clickType));
        }
        
        private async void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (e.GetCurrentPoint(_cardBorder).Properties.IsLeftButtonPressed)
            {
                var dragData = new DataObject();
                dragData.Set("card-data", $"{Category}:{ItemName}");
                
                CardDragStarted?.Invoke(this, new CardDragEventArgs(ItemName, Category, dragData));
                DebugLogger.Log($"👋 Started dragging {ItemName} from {Category}");
                await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Move);
            }
        }
        
        private void CheckAndLoadLegendarySoul()
        {
            Oracle.Helpers.DebugLogger.LogImportant("CheckAndLoadLegendarySoul", $"🎴 Checking legendary soul for: '{ItemName}' (Category: '{Category}')");
            
            // Check if this is a legendary joker
            if (Category == "Jokers")
            {
                Oracle.Helpers.DebugLogger.LogImportant("CheckAndLoadLegendarySoul", $"🎴 LegendaryJokers contains: {string.Join(", ", BalatroData.LegendaryJokers)}");
                Oracle.Helpers.DebugLogger.LogImportant("CheckAndLoadLegendarySoul", $"🎴 ItemName.ToLower(): '{ItemName.ToLower()}'");
                
                if (BalatroData.LegendaryJokers.Contains(ItemName.ToLower()))
                {
                    Oracle.Helpers.DebugLogger.LogImportant("CheckAndLoadLegendarySoul", $"🎴 '{ItemName}' IS a legendary joker!");
                    // Load the soul sprite (one row below in the sprite sheet)
                    var soulImage = SpriteService.Instance.GetJokerSoulImage(ItemName);
                    if (soulImage != null)
                    {
                        Oracle.Helpers.DebugLogger.LogImportant("CheckAndLoadLegendarySoul", $"🎴 Soul image loaded successfully for '{ItemName}'");
                        _soulImage.Source = soulImage;
                        _soulImage.IsVisible = true;
                        StartSoulAnimation();
                    }
                    else
                    {
                        Oracle.Helpers.DebugLogger.LogImportant("CheckAndLoadLegendarySoul", $"🎴 Failed to load soul image for '{ItemName}'");
                    }
                }
                else
                {
                    Oracle.Helpers.DebugLogger.LogImportant("CheckAndLoadLegendarySoul", $"🎴 '{ItemName}' is NOT a legendary joker");
                    _soulImage.IsVisible = false;
                    StopSoulAnimation();
                }
            }
            else
            {
                _soulImage.IsVisible = false;
                StopSoulAnimation();
            }
        }
        
        private void StartSoulAnimation()
        {
            StopSoulAnimation();
            
            var startTime = DateTime.Now;
            _soulAnimationTimer = new System.Threading.Timer(_ =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_soulImage.IsVisible)
                    {
                        var elapsed = (DateTime.Now - startTime).TotalSeconds;
                        
                        // Floating scale animation
                        var scale = 1.0 + 0.07 + 0.02 * Math.Sin(1.8 * elapsed);
                        
                        // Floating rotation animation
                        var rotation = 5 * Math.Sin(1.219 * elapsed);
                        
                        if (_soulImage.RenderTransform is TransformGroup group)
                        {
                            if (group.Children[0] is ScaleTransform scaleTransform)
                            {
                                scaleTransform.ScaleX = scale;
                                scaleTransform.ScaleY = scale;
                            }
                            if (group.Children[1] is RotateTransform rotateTransform)
                            {
                                rotateTransform.Angle = rotation;
                            }
                        }
                    }
                });
            }, null, 0, 33); // ~30 FPS
        }
        
        private void StopSoulAnimation()
        {
            _soulAnimationTimer?.Dispose();
            _soulAnimationTimer = null;
        }

        private string FormatItemName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            // For jokers, check if we have a sprite name that needs display name lookup
            if (Category == "Jokers")
            {
                // First check if the sprite service has the image (meaning it's a lowercase sprite name)
                var spriteService = SpriteService.Instance;
                var jokerImg = spriteService.GetJokerImage(name);
                if (jokerImg is not null)
                {
                    // This is a sprite name, get the display name
                    return BalatroData.GetDisplayNameFromSprite(name);
                }
            }

            // Otherwise use the existing formatting
            return System.Text.RegularExpressions.Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");
        }
    }
    
    public class CardClickEventArgs : EventArgs
    {
        public string ItemName { get; }
        public string Category { get; }
        public CardClickType ClickType { get; }
        
        public CardClickEventArgs(string itemName, string category, CardClickType clickType)
        {
            ItemName = itemName;
            Category = category;
            ClickType = clickType;
        }
    }
    
    public class CardDragEventArgs : EventArgs
    {
        public string ItemName { get; }
        public string Category { get; }
        public DataObject DragData { get; }
        
        public CardDragEventArgs(string itemName, string category, DataObject dragData)
        {
            ItemName = itemName;
            Category = category;
            DragData = dragData;
        }
    }
    
    public enum CardClickType
    {
        LeftClick,
        RightClick
    }
}