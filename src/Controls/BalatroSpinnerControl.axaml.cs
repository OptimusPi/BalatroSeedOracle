using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Oracle.Controls
{
    public partial class BalatroSpinnerControl : UserControl
    {
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<BalatroSpinnerControl, string>(nameof(Label), "");

        public static readonly StyledProperty<int> ValueProperty =
            AvaloniaProperty.Register<BalatroSpinnerControl, int>(nameof(Value), 1);

        public static readonly StyledProperty<int> MinimumProperty =
            AvaloniaProperty.Register<BalatroSpinnerControl, int>(nameof(Minimum), 1);

        public static readonly StyledProperty<int> MaximumProperty =
            AvaloniaProperty.Register<BalatroSpinnerControl, int>(nameof(Maximum), 100);

        public static readonly StyledProperty<int> IncrementProperty =
            AvaloniaProperty.Register<BalatroSpinnerControl, int>(nameof(Increment), 1);

        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public int Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, Math.Max(Minimum, Math.Min(Maximum, value)));
        }

        public int Minimum
        {
            get => GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public int Maximum
        {
            get => GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public int Increment
        {
            get => GetValue(IncrementProperty);
            set => SetValue(IncrementProperty, value);
        }

        public event EventHandler<int>? ValueChanged;

        public BalatroSpinnerControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ValueProperty)
            {
                var newValue = (int)change.NewValue!;
                var oldValue = (int)change.OldValue!;

                if (newValue != oldValue)
                {
                    ValueChanged?.Invoke(this, newValue);
                }

                // Update button states
                var decrementButton = this.FindControl<Button>("DecrementButton");
                var incrementButton = this.FindControl<Button>("IncrementButton");

                if (decrementButton != null)
                    decrementButton.IsEnabled = newValue > Minimum;

                if (incrementButton != null)
                    incrementButton.IsEnabled = newValue < Maximum;
            }
        }

        private void OnDecrementClick(object? sender, RoutedEventArgs e)
        {
            Value = Math.Max(Minimum, Value - Increment);
        }

        private void OnIncrementClick(object? sender, RoutedEventArgs e)
        {
            Value = Math.Min(Maximum, Value + Increment);
        }
    }
}