using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using BalatroSeedOracle.Constants;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Json;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Controls
{
    public partial class JokerSetDisplay : UserControl
    {
        private TextBlock? _setNameText;
        private TextBlock? _setDescriptionText;
        private Canvas? _jokerCanvas;
        private ItemsControl? _tagsControl;

        public static readonly StyledProperty<JokerSet?> JokerSetProperty =
            AvaloniaProperty.Register<JokerSetDisplay, JokerSet?>(
                nameof(JokerSet)
            );

        public static readonly RoutedEvent<RoutedEventArgs> ClickEvent = RoutedEvent.Register<
            JokerSetDisplay,
            RoutedEventArgs
        >(nameof(Click), RoutingStrategies.Bubble);

        public JokerSet? JokerSet
        {
            get => GetValue(JokerSetProperty);
            set => SetValue(JokerSetProperty, value);
        }

        public event EventHandler<RoutedEventArgs>? Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        public JokerSetDisplay()
        {
            InitializeComponent();

            _setNameText = this.FindControl<TextBlock>("SetNameText");
            _setDescriptionText = this.FindControl<TextBlock>("SetDescriptionText");
            _jokerCanvas = this.FindControl<Canvas>("JokerCanvas");
            _tagsControl = this.FindControl<ItemsControl>("TagsControl");

            this.PointerPressed += OnPointerPressed;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (
                change.Property == JokerSetProperty
                && change.NewValue is JokerSet set
            )
            {
                LoadJokerSet(set);
            }
        }

        private void LoadJokerSet(JokerSet set)
        {
            if (_setNameText != null)
                _setNameText.Text = set.Name;

            if (_setDescriptionText != null)
                _setDescriptionText.Text = set.Description;

            if (_tagsControl != null)
                _tagsControl.ItemsSource = set.Tags;

            if (_jokerCanvas != null)
            {
                _jokerCanvas.Children.Clear();
                DisplayOverlappingJokers(set.Items);
            }
        }

        private void DisplayOverlappingJokers(List<string> items)
        {
            if (_jokerCanvas == null || items.Count == 0)
                return;

            var spriteService = SpriteService.Instance;
            const double cardWidth = UIConstants.JokerSpriteWidth;
            const double cardHeight = UIConstants.JokerSpriteHeight;
            const double overlap = 40; // Pixels of overlap

            // Calculate starting position to center the cards
            double totalWidth = cardWidth + (items.Count - 1) * (cardWidth - overlap);
            double startX = (_jokerCanvas.Width - totalWidth) / 2;
            double startY = (_jokerCanvas.Height - cardHeight) / 2;

            for (int i = 0; i < items.Count && i < 5; i++) // Max 5 cards to fit
            {
                var itemName = items[i];
                IImage? image = null;

                // Try to get image from different sprite types
                image = spriteService.GetJokerImage(itemName);
                if (image == null)
                    image = spriteService.GetTarotImage(itemName);
                if (image == null)
                    image = spriteService.GetSpectralImage(itemName);
                if (image == null)
                    image = spriteService.GetVoucherImage(itemName);

                if (image != null)
                {
                    var imageControl = new Image
                    {
                        Source = image,
                        Width = cardWidth,
                        Height = cardHeight,
                        Stretch = Stretch.Uniform,
                        RenderTransform = new TranslateTransform(
                            startX + i * (cardWidth - overlap),
                            startY
                        ),
                    };

                    // Add slight rotation for a "hand of cards" effect
                    var rotation = (i - items.Count / 2.0) * 3; // -6 to +6 degrees
                    imageControl.RenderTransform = new TransformGroup
                    {
                        Children =
                        {
                            new TranslateTransform(startX + i * (cardWidth - overlap), startY),
                            new RotateTransform(rotation, cardWidth / 2, cardHeight / 2),
                        },
                    };

                    // Add a subtle shadow with a border instead
                    var shadowBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(50, 0, 0, 0)),
                        Width = cardWidth,
                        Height = cardHeight,
                        CornerRadius = new CornerRadius(4),
                        RenderTransform = new TransformGroup
                        {
                            Children =
                            {
                                new TranslateTransform(
                                    startX + i * (cardWidth - overlap) + 2,
                                    startY + 2
                                ),
                                new RotateTransform(rotation, cardWidth / 2, cardHeight / 2),
                            },
                        },
                    };
                    _jokerCanvas.Children.Add(shadowBorder);

                    _jokerCanvas.Children.Add(imageControl);
                }
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var args = new RoutedEventArgs(ClickEvent);
            RaiseEvent(args);
        }
    }
}
