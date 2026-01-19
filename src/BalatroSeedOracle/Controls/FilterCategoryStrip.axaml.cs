using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace BalatroSeedOracle.Controls
{
    public partial class FilterCategoryStrip : UserControl
    {
        public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<
            FilterCategoryStrip,
            string
        >(nameof(Label), "CATEGORY");

        public static readonly StyledProperty<IBrush> LabelForegroundProperty =
            AvaloniaProperty.Register<FilterCategoryStrip, IBrush>(nameof(LabelForeground));

        public static readonly StyledProperty<IEnumerable> ItemsProperty =
            AvaloniaProperty.Register<FilterCategoryStrip, IEnumerable>(nameof(Items));

        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public IBrush LabelForeground
        {
            get => GetValue(LabelForegroundProperty);
            set => SetValue(LabelForegroundProperty, value);
        }

        public IEnumerable Items
        {
            get => GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public FilterCategoryStrip()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }
    }
}
