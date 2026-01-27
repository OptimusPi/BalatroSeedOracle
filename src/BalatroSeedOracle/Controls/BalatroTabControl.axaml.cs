using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Styling;

namespace BalatroSeedOracle.Controls
{
    /// <summary>
    /// A reusable TabControl with a bouncing triangle indicator that automatically positions itself
    /// above the selected tab without using magic numbers or ViewModel calculations.
    ///
    /// Usage: Add TabItem children to the Items collection or use data binding.
    /// </summary>
    public class BalatroTabControl : TemplatedControl
    {
        private const double TRIANGLE_HALF_WIDTH = 6.0; // Half of the 12px wide triangle (Points="0,0 12,0 6,8")

        private TabControl? _innerTabControl;
        private Canvas? _triangleCanvas;
        private Polygon? _triangleIndicator;
        private IDisposable? _selectedIndexSubscription;

        static BalatroTabControl()
        {
            // Load the control's styles
            var uri = new Uri("avares://BalatroSeedOracle/Controls/BalatroTabControl.axaml");
            var styles = (Styles)Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(uri);
            if (Application.Current != null)
            {
                Application.Current.Styles.Add(styles);
            }
        }

        public BalatroTabControl()
        {
            // Initialize the Items collection so TabItems can be added directly
            Items = new AvaloniaList<object>();
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            // Unsubscribe from previous control if any
            _selectedIndexSubscription?.Dispose();
            _selectedIndexSubscription = null;

            // Get template parts
            _innerTabControl = e.NameScope.Find<TabControl>("PART_TabControl");
            _triangleCanvas = e.NameScope.Find<Canvas>("PART_TriangleCanvas");
            _triangleIndicator = e.NameScope.Find<Polygon>("PART_TriangleIndicator");

            if (_innerTabControl != null)
            {
                // Subscribe to SelectedIndex changes from the inner TabControl
                _innerTabControl.PropertyChanged += OnInnerTabControlPropertyChanged;

                // Also update on layout changes (handles window resizing, tab content changes, etc.)
                _innerTabControl.LayoutUpdated += OnInnerTabControlLayoutUpdated;

                // Initial update
                UpdateTrianglePosition();
            }
        }

        private void OnInnerTabControlPropertyChanged(
            object? sender,
            AvaloniaPropertyChangedEventArgs e
        )
        {
            if (e.Property == TabControl.SelectedIndexProperty)
            {
                UpdateTrianglePosition();
            }
        }

        private void OnInnerTabControlLayoutUpdated(object? sender, EventArgs e)
        {
            UpdateTrianglePosition();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            if (_innerTabControl != null)
            {
                _innerTabControl.PropertyChanged -= OnInnerTabControlPropertyChanged;
                _innerTabControl.LayoutUpdated -= OnInnerTabControlLayoutUpdated;
            }
        }

        private void UpdateTrianglePosition()
        {
            if (_innerTabControl == null || _triangleIndicator == null || _triangleCanvas == null)
                return;

            var selectedIndex = _innerTabControl.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= _innerTabControl.ItemCount)
            {
                _triangleIndicator.IsVisible = false;
                return;
            }

            // Find the selected TabItem visual
            var selectedContainer = _innerTabControl.ContainerFromIndex(selectedIndex) as Control;
            if (selectedContainer == null || !selectedContainer.IsArrangeValid)
            {
                _triangleIndicator.IsVisible = false;
                return;
            }

            // Get the position of the selected tab relative to the triangle canvas
            var tabBounds = selectedContainer.Bounds;
            var tabPosition = selectedContainer.TranslatePoint(new Point(0, 0), _triangleCanvas);

            if (tabPosition.HasValue)
            {
                // Center the triangle above the tab
                // Triangle is 16px wide, so subtract half width (8px) to center it
                var triangleCenterX =
                    tabPosition.Value.X + (tabBounds.Width / 2.0) - TRIANGLE_HALF_WIDTH;
                Canvas.SetLeft(_triangleIndicator, triangleCenterX);
                _triangleIndicator.IsVisible = true;
            }
            else
            {
                _triangleIndicator.IsVisible = false;
            }
        }

        // Items collection to hold TabItem children
        public static readonly StyledProperty<AvaloniaList<object>> ItemsProperty =
            AvaloniaProperty.Register<BalatroTabControl, AvaloniaList<object>>(nameof(Items));

        public AvaloniaList<object> Items
        {
            get => GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        // SelectedIndex property with TwoWay binding support
        public static readonly StyledProperty<int> SelectedIndexProperty =
            AvaloniaProperty.Register<BalatroTabControl, int>(
                nameof(SelectedIndex),
                defaultValue: 0,
                defaultBindingMode: Avalonia.Data.BindingMode.TwoWay
            );

        public int SelectedIndex
        {
            get => GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }
    }
}
