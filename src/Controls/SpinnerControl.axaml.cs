using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
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
        >(nameof(Value), 0, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

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
            AvaloniaProperty.Register<SpinnerControl, string[]?>(nameof(DisplayValues), null);

        public static readonly StyledProperty<bool> AllowAutoProperty = AvaloniaProperty.Register<
            SpinnerControl,
            bool
        >(nameof(AllowAuto), false);

        public static readonly StyledProperty<bool> IsEditingProperty = AvaloniaProperty.Register<
            SpinnerControl,
            bool
        >(nameof(IsEditing), false);

        public static readonly StyledProperty<bool> ReadOnlyProperty = AvaloniaProperty.Register<
            SpinnerControl,
            bool
        >(nameof(ReadOnly), false);

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

        public bool IsEditing
        {
            get => GetValue(IsEditingProperty);
            set => SetValue(IsEditingProperty, value);
        }

        public bool ReadOnly
        {
            get => GetValue(ReadOnlyProperty);
            set => SetValue(ReadOnlyProperty, value);
        }

        public SpinnerControl()
        {
            InitializeComponent();
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

            // In circular stake mode, arrows should always be enabled if we have multiple values
            bool circularStake = IsCircularStakeSpinner();

            if (decrementButton != null)
                decrementButton.IsEnabled = circularStake
                    ? HasMultipleValuesForCircular()
                    : Value > Minimum;

            if (incrementButton != null)
                incrementButton.IsEnabled = circularStake
                    ? HasMultipleValuesForCircular()
                    : Value < Maximum;
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
                    bool circularStake = IsCircularStakeSpinner();

                    if (decrementButton != null)
                        decrementButton.IsEnabled = circularStake
                            ? HasMultipleValuesForCircular()
                            : newValue > Minimum;

                    if (incrementButton != null)
                        incrementButton.IsEnabled = circularStake
                            ? HasMultipleValuesForCircular()
                            : newValue < Maximum;
                }
            }
        }

        private void OnDecrementClick(object? sender, RoutedEventArgs e)
        {
            if (IsCircularStakeSpinner())
            {
                // Wrap to end when at minimum
                if (Value > Minimum)
                {
                    Value = Value - Increment;
                }
                else
                {
                    Value = Maximum;
                }
            }
            else
            {
                Value = Math.Max(Minimum, Value - Increment);
            }
        }

        private void OnIncrementClick(object? sender, RoutedEventArgs e)
        {
            if (IsCircularStakeSpinner())
            {
                // Wrap to start when at maximum
                if (Value < Maximum)
                {
                    Value = Value + Increment;
                }
                else
                {
                    Value = Minimum;
                }
            }
            else
            {
                Value = Math.Min(Maximum, Value + Increment);
            }
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

        private void OnValueButtonClick(object? sender, RoutedEventArgs e)
        {
            // Don't allow editing if ReadOnly is true (for enum spinners like stake)
            if (ReadOnly)
                return;

            IsEditing = true;

            // Focus and select text in the edit box
            var valueEdit = this.FindControl<TextBox>("ValueEdit");
            if (valueEdit != null)
            {
                valueEdit.Focus();
                valueEdit.SelectAll();
            }
        }

        private void OnValueEditLostFocus(object? sender, RoutedEventArgs e)
        {
            SaveEditValue();
        }

        private void OnValueEditKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SaveEditValue();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                IsEditing = false;
                e.Handled = true;
            }
        }

        private void SaveEditValue()
        {
            var valueEdit = this.FindControl<TextBox>("ValueEdit");
            if (valueEdit != null && IsEditing)
            {
                var inputText = valueEdit.Text?.Trim();

                if (!string.IsNullOrEmpty(inputText))
                {
                    // Try to parse as integer
                    if (int.TryParse(inputText, out int newValue))
                    {
                        // Validate and set the value
                        Value = Math.Max(Minimum, Math.Min(Maximum, newValue));
                    }
                    // Check for "Auto" if allowed
                    else if (
                        AllowAuto && inputText.Equals("Auto", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        Value = 0; // Auto value
                    }
                    // Try to find in display values if available
                    else if (DisplayValues != null)
                    {
                        var index = Array.FindIndex(
                            DisplayValues,
                            v => v.Equals(inputText, StringComparison.OrdinalIgnoreCase)
                        );
                        if (index >= 0)
                        {
                            Value = index;
                        }
                    }
                }

                IsEditing = false;
            }
        }

        // Helpers for detecting circular navigation mode
        private bool IsCircularStakeSpinner()
        {
            var type = SpinnerType ?? string.Empty;
            return string.Equals(type, "stake", StringComparison.OrdinalIgnoreCase);
        }

        private bool HasMultipleValuesForCircular()
        {
            // Prefer DisplayValues length if provided; otherwise use range check
            if (DisplayValues != null)
                return DisplayValues.Length > 1;
            return Maximum > Minimum;
        }
    }
}
