using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Components
{
    public partial class ResponsiveCard : UserControl, IDisposable
    {
        private Border _cardBorder;
        private Image _cardImage;
        private Image _soulImage;
        private Image _editionOverlay;
        private TextBlock _cardName;
        private Grid _imageContainer;
        private Border _legendaryBackground;

        // Drag tracking
        private Point? _dragStartPoint;
        private bool _isDragging;
        private const double MinimumDragDistance = 5;
        private string _currentBreakpoint = "desktop";
        private System.Threading.Timer? _soulAnimationTimer;

        public static readonly StyledProperty<string> ItemNameProperty = AvaloniaProperty.Register<
            ResponsiveCard,
            string
        >(nameof(ItemName), "");

        public static readonly StyledProperty<string> CategoryProperty = AvaloniaProperty.Register<
            ResponsiveCard,
            string
        >(nameof(Category), "");

        public static readonly StyledProperty<IImage?> ImageSourceProperty =
            AvaloniaProperty.Register<ResponsiveCard, IImage?>(nameof(ImageSource));

        public static readonly StyledProperty<bool> IsSelectedNeedProperty =
            AvaloniaProperty.Register<ResponsiveCard, bool>(nameof(IsSelectedNeed));

        public static readonly StyledProperty<bool> IsSelectedWantProperty =
            AvaloniaProperty.Register<ResponsiveCard, bool>(nameof(IsSelectedWant));

        public static readonly StyledProperty<bool> IsSelectedMustNotProperty =
            AvaloniaProperty.Register<ResponsiveCard, bool>(nameof(IsSelectedMustNot));

        public static readonly StyledProperty<string> EditionProperty = AvaloniaProperty.Register<
            ResponsiveCard,
            string
        >(nameof(Edition), "none");

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

        public bool IsSelectedMustNot
        {
            get => GetValue(IsSelectedMustNotProperty);
            set => SetValue(IsSelectedMustNotProperty, value);
        }

        public string Edition
        {
            get => GetValue(EditionProperty);
            set => SetValue(EditionProperty, value);
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
            _editionOverlay = this.FindControl<Image>("EditionOverlay")!;
            _cardName = this.FindControl<TextBlock>("CardName")!;
            _imageContainer = this.FindControl<Grid>("ImageContainer")!;
            _legendaryBackground = this.FindControl<Border>("LegendaryBackground")!;

            // Property change handlers
            this.PropertyChanged += OnPropertyChanged;

            // Event handlers
            _cardBorder.PointerPressed += OnPointerPressed;
            _cardBorder.PointerMoved += OnPointerMoved;
            _cardBorder.PointerReleased += OnPointerReleased;

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
                _ => "large-desktop",
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

        private void UpdateImageSize()
        {
            // Adjust image container size based on category
            switch (Category)
            {
                case "Tags":
                    // Tags at 50% size for better clarity
                    _imageContainer.Width = 27;
                    _imageContainer.Height = 27;
                    break;
                case "Bosses":
                    // Boss blinds are 34x34 sprites
                    _imageContainer.Width = 34;
                    _imageContainer.Height = 34;
                    break;
                default:
                    // Standard size for other items
                    _imageContainer.Width = 64;
                    _imageContainer.Height = 64;
                    break;
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
                UpdateImageSize();
                CheckAndLoadLegendarySoul();
            }
            else if (e.Property == CategoryProperty)
            {
                UpdateImageSize();
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
            else if (e.Property == IsSelectedMustNotProperty)
            {
                if (IsSelectedMustNot)
                {
                    _cardBorder.Classes.Add("selected-mustnot");
                    _cardBorder.Classes.Remove("selected-need");
                    _cardBorder.Classes.Remove("selected-want");
                }
                else
                {
                    _cardBorder.Classes.Remove("selected-mustnot");
                }
            }
            else if (e.Property == EditionProperty)
            {
                UpdateEditionOverlay();
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(this);

            // Store the drag start point for left button
            if (pointer.Properties.IsLeftButtonPressed)
            {
                _dragStartPoint = pointer.Position;
                _isDragging = false;
            }

            // Don't fire click event yet - wait to see if it's a drag
            // Right click should fire immediately though
            if (pointer.Properties.IsRightButtonPressed)
            {
                CardClicked?.Invoke(
                    this,
                    new CardClickEventArgs(ItemName, Category, CardClickType.RightClick)
                );
            }
        }

        private async void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(_cardBorder);

            if (
                currentPoint.Properties.IsLeftButtonPressed
                && _dragStartPoint.HasValue
                && !_isDragging
            )
            {
                // Calculate distance moved
                var distance = Math.Sqrt(
                    Math.Pow(currentPoint.Position.X - _dragStartPoint.Value.X, 2)
                        + Math.Pow(currentPoint.Position.Y - _dragStartPoint.Value.Y, 2)
                );

                // Only start drag if we've moved enough
                if (distance >= MinimumDragDistance)
                {
                    _isDragging = true;

                    var dragData = new DataObject();
                    dragData.Set("balatro-item", $"{Category}|{ItemName}");

                    CardDragStarted?.Invoke(
                        this,
                        new CardDragEventArgs(ItemName, Category, dragData)
                    );
                    DebugLogger.Log($"👋 Started dragging {ItemName} from {Category}");

                    try
                    {
                        await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Move);
                    }
                    finally
                    {
                        // IMPORTANT: Clean up drag visual state when drag ends
                        this.Classes.Remove("is-dragging");
                        this.RenderTransform = null; // Reset any transforms
                        this.Width = double.NaN; // Reset to auto width
                        this.Height = double.NaN; // Reset to auto height
                        _isDragging = false;
                        _dragStartPoint = null;
                    }
                }
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(this);

            // If we had a drag start point and didn't drag, it's a click
            if (
                _dragStartPoint.HasValue
                && !_isDragging
                && pointer.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased
            )
            {
                CardClicked?.Invoke(
                    this,
                    new CardClickEventArgs(ItemName, Category, CardClickType.LeftClick)
                );
            }

            // Clean up drag state
            _dragStartPoint = null;
            _isDragging = false;
        }

        private void CheckAndLoadLegendarySoul()
        {
            BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                "CheckAndLoadLegendarySoul",
                $"🎴 Checking legendary soul for: '{ItemName}' (Category: '{Category}')"
            );

            // Check if this is a legendary joker
            if (Category == "Jokers")
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                    "CheckAndLoadLegendarySoul",
                    $"🎴 LegendaryJokers contains: {string.Join(", ", BalatroData.LegendaryJokers)}"
                );
                BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                    "CheckAndLoadLegendarySoul",
                    $"🎴 ItemName.ToLowerInvariant(): '{ItemName.ToLowerInvariant()}'"
                );

                if (BalatroData.LegendaryJokers.Contains(ItemName.ToLowerInvariant()))
                {
                    BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                        "CheckAndLoadLegendarySoul",
                        $"🎴 '{ItemName}' IS a legendary joker!"
                    );

                    // Don't show gold background in item palette - it looks weird
                    _legendaryBackground.IsVisible = false;

                    // Check if this is one of the 5 with animated faces
                    var animatedLegendaryJokers = new[]
                    {
                        "canio",
                        "triboulet",
                        "yorick",
                        "chicot",
                        "perkeo",
                    };
                    if (animatedLegendaryJokers.Contains(ItemName.ToLowerInvariant()))
                    {
                        // Load the soul sprite (one row below in the sprite sheet)
                        var soulImage = SpriteService.Instance.GetJokerSoulImage(ItemName);
                        if (soulImage != null)
                        {
                            BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                                "CheckAndLoadLegendarySoul",
                                $"🎴 Soul image loaded successfully for '{ItemName}'"
                            );
                            _soulImage.Source = soulImage;
                            _soulImage.IsVisible = true;
                            StartSoulAnimation();
                        }
                        else
                        {
                            BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                                "CheckAndLoadLegendarySoul",
                                $"🎴 Failed to load soul image for '{ItemName}'"
                            );
                        }
                    }
                }
                else
                {
                    BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                        "CheckAndLoadLegendarySoul",
                        $"🎴 '{ItemName}' is NOT a legendary joker"
                    );
                    _legendaryBackground.IsVisible = false;
                    _soulImage.IsVisible = false;
                    StopSoulAnimation();
                }
            }
            else
            {
                _legendaryBackground.IsVisible = false;
                _soulImage.IsVisible = false;
                StopSoulAnimation();
            }
        }

        private void StartSoulAnimation()
        {
            StopSoulAnimation();

            var startTime = DateTime.Now;
            _soulAnimationTimer = new System.Threading.Timer(
                _ =>
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
                },
                null,
                0,
                15
            );
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
                // Special handling for wildcard jokers
                var wildcardNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "anyjoker", "Any Joker" },
                    { "anycommon", "Any Common" },
                    { "anyuncommon", "Any Uncommon" },
                    { "anyrare", "Any Rare" },
                    { "anylegendary", "Any Legendary" },
                };

                if (wildcardNames.TryGetValue(name, out var wildcardDisplay))
                {
                    return wildcardDisplay;
                }

                // Otherwise try to get display name from sprite mapping
                var displayName = BalatroData.GetDisplayNameFromSprite(name);
                BalatroSeedOracle.Helpers.DebugLogger.Log(
                    "ResponsiveCard",
                    $"FormatItemName: name='{name}', displayName='{displayName}', Category='{Category}'"
                );
                return displayName;
            }

            // Otherwise use the existing formatting
            return System.Text.RegularExpressions.Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");
        }

        private void UpdateEditionOverlay()
        {
            if (Category != "Jokers" || string.IsNullOrEmpty(Edition) || Edition == "none")
            {
                _editionOverlay.IsVisible = false;
                return;
            }

            var spriteService = SpriteService.Instance;
            var editionImage = spriteService.GetEditionImage(Edition.ToLowerInvariant());

            if (editionImage != null)
            {
                _editionOverlay.Source = editionImage;
                _editionOverlay.IsVisible = true;
            }
            else
            {
                _editionOverlay.IsVisible = false;
            }
        }

        public void Dispose()
        {
            _soulAnimationTimer?.Dispose();
            _soulAnimationTimer = null;
            GC.SuppressFinalize(this);
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
        RightClick,
    }
}
