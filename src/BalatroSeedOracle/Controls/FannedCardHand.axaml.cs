using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using BalatroSeedOracle.Converters;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Controls
{
    /// <summary>
    /// Displays filter items in a fanned poker hand style.
    /// 1-4 cards: straight up and down, side by side
    /// 5+ cards: fan out like a poker hand (middle straight, outer cards lean outward)
    /// </summary>
    public partial class FannedCardHand : UserControl
    {
        private const double CardWidth = 71;
        private const double CardHeight = 95;
        private const double CardSpacing = 8; // Space between cards when not fanned
        private const double MaxFanAngle = 15; // Maximum rotation angle for outermost cards (degrees)
        private const double OverlapFactor = 0.55; // How much cards overlap when fanned (0.5 = 50% overlap)
        private const double VerticalArcHeight = 20; // How much the outer cards dip down

        public static readonly StyledProperty<IEnumerable?> ItemsProperty = AvaloniaProperty.Register<
            FannedCardHand,
            IEnumerable?
        >(nameof(Items));

        public IEnumerable? Items
        {
            get => GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public FannedCardHand()
        {
            InitializeComponent();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ItemsProperty)
            {
                // Unsubscribe from old collection
                if (change.OldValue is INotifyCollectionChanged oldCollection)
                {
                    oldCollection.CollectionChanged -= OnItemsCollectionChanged;
                }

                // Subscribe to new collection
                if (change.NewValue is INotifyCollectionChanged newCollection)
                {
                    newCollection.CollectionChanged += OnItemsCollectionChanged;
                }

                RenderCards();
            }
        }

        private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RenderCards();
        }

        private void RenderCards()
        {
            CardCanvas.Children.Clear();

            if (Items == null)
                return;

            var itemList = new List<object>();
            foreach (var item in Items)
            {
                if (item != null)
                    itemList.Add(item);
            }

            int count = itemList.Count;
            if (count == 0)
                return;

            // Get converters for sprites
            var spriteConverter = new ItemNameToSpriteConverter();
            var soulFaceConverter = new ItemNameToSoulFaceConverter();

            // Calculate layout
            bool useFan = count > 4;
            double totalWidth;

            if (useFan)
            {
                // Fanned layout with overlap
                double cardVisibleWidth = CardWidth * OverlapFactor;
                totalWidth = CardWidth + ((count - 1) * cardVisibleWidth);
            }
            else
            {
                // Side by side with spacing
                totalWidth = (count * CardWidth) + ((count - 1) * CardSpacing);
            }

            // Start from left edge with padding
            double startX = 10;

            for (int i = 0; i < count; i++)
            {
                var item = itemList[i];

                // Get sprite image using converter
                var sprite = spriteConverter.Convert(item, typeof(IImage), null!, null!) as IImage;
                var soulFace = soulFaceConverter.Convert(item, typeof(IImage), null!, null!) as IImage;

                // Create card container using Grid for layering
                var cardGrid = new Grid
                {
                    Width = CardWidth,
                    Height = CardHeight,
                    ClipToBounds = false,
                };

                // Base card image
                if (sprite != null)
                {
                    var cardImage = new Image
                    {
                        Source = sprite,
                        Width = CardWidth,
                        Height = CardHeight,
                        Stretch = Stretch.Uniform,
                    };
                    cardGrid.Children.Add(cardImage);
                }

                // Soul face overlay for legendary jokers
                if (soulFace != null)
                {
                    var soulImage = new Image
                    {
                        Source = soulFace,
                        Width = CardWidth,
                        Height = CardHeight,
                        Stretch = Stretch.Uniform,
                    };
                    cardGrid.Children.Add(soulImage);
                }

                // Calculate position and rotation
                double x,
                    y,
                    rotation;
                CalculateCardTransform(i, count, useFan, startX, out x, out y, out rotation);

                // Apply transforms
                var transformGroup = new TransformGroup();

                // Rotation around center-bottom of card (like holding cards)
                if (Math.Abs(rotation) > 0.01)
                {
                    transformGroup.Children.Add(new RotateTransform(rotation));
                }

                // Position
                transformGroup.Children.Add(new TranslateTransform(x, y + 10)); // +10 for top margin

                cardGrid.RenderTransform = transformGroup;
                cardGrid.RenderTransformOrigin = new RelativePoint(0.5, 0.85, RelativeUnit.Relative);

                // Z-Index: cards on the right should be on top
                cardGrid.ZIndex = i;

                CardCanvas.Children.Add(cardGrid);
            }

            // Resize canvas to fit content
            CardCanvas.Width = totalWidth + 30; // Add padding
            CardCanvas.Height = CardHeight + VerticalArcHeight + 25;
        }

        private void CalculateCardTransform(
            int index,
            int count,
            bool useFan,
            double startX,
            out double x,
            out double y,
            out double rotation
        )
        {
            if (!useFan)
            {
                // Simple side-by-side layout - no rotation
                x = startX + (index * (CardWidth + CardSpacing));
                y = 0;
                rotation = 0;
            }
            else
            {
                // Fanned layout
                double cardVisibleWidth = CardWidth * OverlapFactor;

                // X position with overlap
                x = startX + (index * cardVisibleWidth);

                // Calculate normalized position (-1 to 1, where 0 is center)
                double normalizedPos = (count > 1) ? (2.0 * index / (count - 1)) - 1.0 : 0;

                // Rotation: outer cards lean outward, center card is straight
                rotation = normalizedPos * MaxFanAngle;

                // Y position: create a slight arc (outer cards dip down)
                // Using parabola: y = a * x^2 where x is normalizedPos
                y = normalizedPos * normalizedPos * VerticalArcHeight;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            // Trigger render on measure to ensure proper layout
            RenderCards();
            return base.MeasureOverride(availableSize);
        }
    }
}
