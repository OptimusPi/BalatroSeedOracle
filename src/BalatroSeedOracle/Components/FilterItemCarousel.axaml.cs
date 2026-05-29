using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// Horizontal carousel for FilterItems - replaces complex fanned card layout.
    /// Reuses FilterItemCard component for clean, maintainable code.
    /// </summary>
    public partial class FilterItemCarousel : UserControl
    {
        private ItemsControl? _carouselItems;

        public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
            AvaloniaProperty.Register<FilterItemCarousel, IEnumerable?>(nameof(ItemsSource));

        public IEnumerable? ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public FilterItemCarousel()
        {
            InitializeComponent();

            // CarouselItems (x:Name) is wired by the generated InitializeComponent(); bind ItemsSource through it.
            _carouselItems = CarouselItems;
            if (_carouselItems != null)
            {
                _carouselItems.Bind(
                    ItemsControl.ItemsSourceProperty,
                    this.GetObservable(ItemsSourceProperty)
                );
            }
        }
    }
}
