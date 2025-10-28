using System;
using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;

namespace BalatroSeedOracle.Behaviors
{
    /// <summary>
    /// Arranges items in a "fanned hand" layout like Balatro joker displays
    /// Items overlap and rotate slightly to create a card-hand effect
    /// </summary>
    public class FannedHandBehavior : Behavior<ItemsControl>
    {
        private const double CardWidth = 36;
        private const double CardSpacing = 24; // Overlap amount (cards are 36px but spaced 24px apart)
        private const double MaxRotation = 8; // Max rotation in degrees for outer cards

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject != null)
            {
                AssociatedObject.Loaded += OnLoaded;
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.Loaded -= OnLoaded;
            }

            base.OnDetaching();
        }

        private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (AssociatedObject?.ItemsSource is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged += OnCollectionChanged;
            }

            ArrangeCards();
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Re-arrange when items change
            Avalonia.Threading.Dispatcher.UIThread.Post(ArrangeCards);
        }

        private void ArrangeCards()
        {
            if (AssociatedObject?.ItemsSource is System.Collections.IEnumerable items)
            {
                var itemList = new System.Collections.Generic.List<object>();
                foreach (var item in items)
                {
                    itemList.Add(item);
                }

                var count = itemList.Count;
                if (count == 0)
                    return;

                // Wait for items to be realized
                Avalonia.Threading.Dispatcher.UIThread.Post(
                    () =>
                    {
                        var panel =
                            AssociatedObject.GetValue(ItemsControl.ItemsPanelProperty)?.Build()
                            as Canvas;

                        if (panel == null)
                            return;

                        var index = 0;
                        foreach (var visual in AssociatedObject.GetRealizedContainers())
                        {
                            if (visual is Control control && index < count)
                            {
                                // Calculate position and rotation for fanned effect
                                var centerIndex = (count - 1) / 2.0;
                                var offsetFromCenter = index - centerIndex;

                                // X position: overlapping cards
                                var x = index * CardSpacing;

                                // Y position: slight arc (cards in middle slightly higher)
                                var normalizedOffset = Math.Abs(offsetFromCenter / centerIndex);
                                var y = normalizedOffset * normalizedOffset * 12; // Parabolic curve

                                // Rotation: fan out from center
                                var rotation = (offsetFromCenter / centerIndex) * MaxRotation;

                                // Apply transformsCanvas.SetLeft(control, x);
                                Canvas.SetTop(control, y);

                                control.RenderTransform = new RotateTransform(rotation);
                                control.RenderTransformOrigin = new RelativePoint(
                                    0.5,
                                    1.0,
                                    RelativeUnit.Relative
                                );

                                index++;
                            }
                        }
                    },
                    Avalonia.Threading.DispatcherPriority.Loaded
                );
            }
        }
    }
}
