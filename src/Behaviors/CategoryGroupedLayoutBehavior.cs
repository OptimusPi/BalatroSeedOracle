using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;
using BalatroSeedOracle.Extensions;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Behaviors
{
    /// <summary>
    /// Arranges items in a drop zone based on their category:
    /// - Jokers: Fanned out horizontally on the left
    /// - Tags: Small, lined up along bottom left
    /// - Boss Blinds: Small, lined up along bottom right (after tags)
    /// - Vouchers: Fanned out on the right side
    /// - Consumables: Normal spread in the center
    /// </summary>
    public class CategoryGroupedLayoutBehavior : Behavior<ItemsControl>
    {
        // Joker fanned layout
        private const double JokerCardWidth = 57;
        private const double JokerCardSpacing = 45; // More horizontal spread
        private const double JokerMaxRotation = 8;

        // Voucher fanned layout
        private const double VoucherCardWidth = 57;
        private const double VoucherCardSpacing = 40;
        private const double VoucherMaxRotation = 6;

        // Consumable normal layout
        private const double ConsumableSpacing = 8;

        // Tag/Boss small layout
        private const double TagBossSize = 34;
        private const double TagBossSpacing = 4;

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

            ArrangeItems();
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(ArrangeItems);
        }

        private void ArrangeItems()
        {
            if (AssociatedObject?.ItemsSource is not IEnumerable items)
            {
                System.Diagnostics.Debug.WriteLine("CategoryGroupedLayoutBehavior: No ItemsSource");
                return;
            }

            var itemList = new List<FilterItem>();
            foreach (var item in items)
            {
                if (item is FilterItem filterItem)
                    itemList.Add(filterItem);
            }

            if (itemList.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    "CategoryGroupedLayoutBehavior: No items to arrange"
                );
                return;
            }

            System.Diagnostics.Debug.WriteLine(
                $"CategoryGroupedLayoutBehavior: Arranging {itemList.Count} items"
            );

            // Group items by category
            var jokers = itemList.Where(i => i.Category == "Jokers").ToList();
            var tags = itemList.Where(i => i.Category == "Tags").ToList();
            var bosses = itemList.Where(i => i.Category == "Bosses").ToList();
            var vouchers = itemList.Where(i => i.Category == "Vouchers").ToList();
            var consumables = itemList
                .Where(i =>
                    i.Category != "Jokers"
                    && i.Category != "Tags"
                    && i.Category != "Bosses"
                    && i.Category != "Vouchers"
                )
                .ToList();

            System.Diagnostics.Debug.WriteLine(
                $"CategoryGroupedLayoutBehavior: Grouped - Jokers:{jokers.Count}, Tags:{tags.Count}, Bosses:{bosses.Count}, Vouchers:{vouchers.Count}, Consumables:{consumables.Count}"
            );

            // Wait for items to be realized - use a slightly longer delay to ensure rendering
            Avalonia.Threading.Dispatcher.UIThread.Post(
                () =>
                {
                    var containers = AssociatedObject.GetRealizedContainers().ToList();

                    System.Diagnostics.Debug.WriteLine(
                        $"CategoryGroupedLayoutBehavior: Found {containers.Count} realized containers for {itemList.Count} items"
                    );

                    if (containers.Count == 0)
                    {
                        // Items not yet realized, try again with a delay
                        System.Diagnostics.Debug.WriteLine(
                            "CategoryGroupedLayoutBehavior: No containers found, retrying..."
                        );
                        Avalonia.Threading.DispatcherTimer.RunOnce(
                            () => ArrangeItems(),
                            TimeSpan.FromMilliseconds(100)
                        );
                        return;
                    }

                    double currentX = 0;
                    int containerIndex = 0;

                    // Layout jokers (fanned, left side)
                    currentX = LayoutJokers(jokers, containers, ref containerIndex, currentX);

                    // Layout consumables (normal spacing, center)
                    currentX = LayoutConsumables(
                        consumables,
                        containers,
                        ref containerIndex,
                        currentX
                    );

                    // Layout vouchers (fanned, right side)
                    currentX = LayoutVouchers(vouchers, containers, ref containerIndex, currentX);

                    // Layout tags (small, bottom left)
                    LayoutTags(tags, containers, ref containerIndex);

                    // Layout bosses (small, bottom right after tags)
                    LayoutBosses(bosses, containers, ref containerIndex, tags.Count);

                    System.Diagnostics.Debug.WriteLine(
                        $"CategoryGroupedLayoutBehavior: Layout complete. Positioned {containerIndex} containers"
                    );
                },
                Avalonia.Threading.DispatcherPriority.Loaded
            );
        }

        private double LayoutJokers(
            List<FilterItem> jokers,
            List<Control> containers,
            ref int containerIndex,
            double startX
        )
        {
            if (jokers.Count == 0)
                return startX;

            for (
                int i = 0;
                i < jokers.Count && containerIndex < containers.Count;
                i++, containerIndex++
            )
            {
                var control = containers[containerIndex];
                var centerIndex = (jokers.Count - 1) / 2.0;
                var offsetFromCenter = i - centerIndex;

                // X position with horizontal spread
                var x = startX + (i * JokerCardSpacing);

                // Y position: slight arc
                var normalizedOffset = Math.Abs(offsetFromCenter / Math.Max(centerIndex, 1));
                var y = normalizedOffset * normalizedOffset * 12;

                // Rotation: fan out from center
                var rotation = (offsetFromCenter / Math.Max(centerIndex, 1)) * JokerMaxRotation;

                Canvas.SetLeft(control, x);
                Canvas.SetTop(control, y);

                control.RenderTransform = new RotateTransform(rotation);
                control.RenderTransformOrigin = new RelativePoint(0.5, 1.0, RelativeUnit.Relative);
            }

            return startX + (jokers.Count * JokerCardSpacing) + 16;
        }

        private double LayoutConsumables(
            List<FilterItem> consumables,
            List<Control> containers,
            ref int containerIndex,
            double startX
        )
        {
            if (consumables.Count == 0)
                return startX;

            for (
                int i = 0;
                i < consumables.Count && containerIndex < containers.Count;
                i++, containerIndex++
            )
            {
                var control = containers[containerIndex];

                var x = startX + (i * (JokerCardWidth + ConsumableSpacing));
                var y = 0;

                Canvas.SetLeft(control, x);
                Canvas.SetTop(control, y);

                control.RenderTransform = null;
            }

            return startX + (consumables.Count * (JokerCardWidth + ConsumableSpacing)) + 16;
        }

        private double LayoutVouchers(
            List<FilterItem> vouchers,
            List<Control> containers,
            ref int containerIndex,
            double startX
        )
        {
            if (vouchers.Count == 0)
                return startX;

            for (
                int i = 0;
                i < vouchers.Count && containerIndex < containers.Count;
                i++, containerIndex++
            )
            {
                var control = containers[containerIndex];
                var centerIndex = (vouchers.Count - 1) / 2.0;
                var offsetFromCenter = i - centerIndex;

                var x = startX + (i * VoucherCardSpacing);
                var normalizedOffset = Math.Abs(offsetFromCenter / Math.Max(centerIndex, 1));
                var y = normalizedOffset * normalizedOffset * 10;
                var rotation = (offsetFromCenter / Math.Max(centerIndex, 1)) * VoucherMaxRotation;

                Canvas.SetLeft(control, x);
                Canvas.SetTop(control, y);

                control.RenderTransform = new RotateTransform(rotation);
                control.RenderTransformOrigin = new RelativePoint(0.5, 1.0, RelativeUnit.Relative);
            }

            return startX + (vouchers.Count * VoucherCardSpacing);
        }

        private void LayoutTags(
            List<FilterItem> tags,
            List<Control> containers,
            ref int containerIndex
        )
        {
            if (tags.Count == 0)
                return;

            for (
                int i = 0;
                i < tags.Count && containerIndex < containers.Count;
                i++, containerIndex++
            )
            {
                var control = containers[containerIndex];

                var x = 8 + (i * (TagBossSize + TagBossSpacing));
                var y = 120; // Bottom of drop zone

                Canvas.SetLeft(control, x);
                Canvas.SetTop(control, y);

                control.RenderTransform = null;
                control.Width = TagBossSize;
                control.Height = TagBossSize;
            }
        }

        private void LayoutBosses(
            List<FilterItem> bosses,
            List<Control> containers,
            ref int containerIndex,
            int tagCount
        )
        {
            if (bosses.Count == 0)
                return;

            var startX = 8 + (tagCount * (TagBossSize + TagBossSpacing)) + 12; // Start after tags with gap

            for (
                int i = 0;
                i < bosses.Count && containerIndex < containers.Count;
                i++, containerIndex++
            )
            {
                var control = containers[containerIndex];

                var x = startX + (i * (TagBossSize + TagBossSpacing));
                var y = 120; // Bottom of drop zone

                Canvas.SetLeft(control, x);
                Canvas.SetTop(control, y);

                control.RenderTransform = null;
                control.Width = TagBossSize;
                control.Height = TagBossSize;
            }
        }
    }
}
