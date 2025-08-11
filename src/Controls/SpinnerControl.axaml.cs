using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace BalatroSeedOracle.Controls
{
    public partial class SpinnerControl : UserControl
    {
        public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<
            SpinnerControl,
            string
        >(nameof(Label), "");

        public static readonly StyledProperty<int> ValueProperty = AvaloniaProperty.Register<
            SpinnerControl,
            int
        >(nameof(Value), 1);

        public static readonly StyledProperty<int> MinimumProperty = AvaloniaProperty.Register<
            SpinnerControl,
            int
        >(nameof(Minimum), 0);

        public static readonly StyledProperty<int> MaximumProperty = AvaloniaProperty.Register<
            SpinnerControl,
            int
        >(nameof(Maximum), 999);

        public static readonly StyledProperty<int> IncrementProperty = AvaloniaProperty.Register<
            SpinnerControl,
            int
        >(nameof(Increment), 1);

        public static readonly StyledProperty<string> SpinnerTypeProperty =
            AvaloniaProperty.Register<SpinnerControl, string>(nameof(SpinnerType), "default");

        public static readonly StyledProperty<string> ShadowDirectionProperty =
            AvaloniaProperty.Register<SpinnerControl, string>(
                nameof(ShadowDirection),
                "south-west"
            );
            
        public static readonly StyledProperty<string[]?> DisplayValuesProperty =
            AvaloniaProperty.Register<SpinnerControl, string[]?>(
                nameof(DisplayValues),
                null
            );
            
        public static readonly StyledProperty<bool> AllowAutoProperty =
            AvaloniaProperty.Register<SpinnerControl, bool>(
                nameof(AllowAuto),
                false
            );

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

        public string SpinnerType
        {
            get => GetValue(SpinnerTypeProperty);
            set => SetValue(SpinnerTypeProperty, value);
        }

        public string ShadowDirection
        {
            get => GetValue(ShadowDirectionProperty);
            set => SetValue(ShadowDirectionProperty, value);
        }
        
        public string[]? DisplayValues
        {
            get => GetValue(DisplayValuesProperty);
            set => SetValue(DisplayValuesProperty, value);
        }
        
        public bool AllowAuto
        {
            get => GetValue(AllowAutoProperty);
            set => SetValue(AllowAutoProperty, value);
        }

        public event EventHandler<int>? ValueChanged;


        public SpinnerControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            // Update the initial display value
            var valueText = this.FindControl<TextBlock>("ValueText");
            if (valueText != null)
            {
                valueText.Text = GetDisplayValue();
            }

            // Hide label if empty
            var labelText = this.FindControl<TextBlock>("LabelText");
            if (labelText != null)
            {
                labelText.IsVisible = !string.IsNullOrWhiteSpace(Label);
            }

            // Update button states
            var decrementButton = this.FindControl<Button>("DecrementButton");
            var incrementButton = this.FindControl<Button>("IncrementButton");

            if (decrementButton != null)
                decrementButton.IsEnabled = Value > Minimum;

            if (incrementButton != null)
                incrementButton.IsEnabled = Value < Maximum;
        }

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }

        private string GetDisplayValue()
        {
            // Check if we should show "Auto" for special values
            if (AllowAuto && Value <= 0)
            {
                return "Auto";
            }
            
            // Use provided DisplayValues if available
            if (DisplayValues != null && DisplayValues.Length > 0)
            {
                var index = Math.Max(0, Math.Min(Value, DisplayValues.Length - 1));
                return DisplayValues[index];
            }
            
            // Default to showing the numeric value
            return Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (
                change.Property == ValueProperty
                || change.Property == LabelProperty
                || change.Property == SpinnerTypeProperty
                || change.Property == DisplayValuesProperty
                || change.Property == AllowAutoProperty
            )
            {
                // Update display text
                var valueText = this.FindControl<TextBlock>("ValueText");
                if (valueText != null)
                {
                    valueText.Text = GetDisplayValue();
                }

                // Update label visibility if label changed
                if (change.Property == LabelProperty)
                {
                    var labelText = this.FindControl<TextBlock>("LabelText");
                    if (labelText != null)
                    {
                        labelText.IsVisible = !string.IsNullOrWhiteSpace(Label);
                    }
                }

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
        }

        private void OnDecrementClick(object? sender, RoutedEventArgs e)
        {
            Value = Math.Max(Minimum, Value - Increment);
        }

        private void OnIncrementClick(object? sender, RoutedEventArgs e)
        {
            Value = Math.Min(Maximum, Value + Increment);
        }

        /// <summary>
        /// Sets a custom display text for the current value
        /// </summary>
        public void SetDisplayText(string text)
        {
            var valueText = this.FindControl<TextBlock>("ValueText");
            if (valueText != null)
            {
                valueText.Text = text;
            }
        }
    }
}
