using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Oracle.Controls
{
    public partial class SpinnerControl : UserControl
    {
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<SpinnerControl, string>(nameof(Label), "");

        public static readonly StyledProperty<int> ValueProperty =
            AvaloniaProperty.Register<SpinnerControl, int>(nameof(Value), 1);

        public static readonly StyledProperty<int> MinimumProperty =
            AvaloniaProperty.Register<SpinnerControl, int>(nameof(Minimum), 1);

        public static readonly StyledProperty<int> MaximumProperty =
            AvaloniaProperty.Register<SpinnerControl, int>(nameof(Maximum), 100);

        public static readonly StyledProperty<int> IncrementProperty =
            AvaloniaProperty.Register<SpinnerControl, int>(nameof(Increment), 1);
            
        public static readonly StyledProperty<string> SpinnerTypeProperty =
            AvaloniaProperty.Register<SpinnerControl, string>(nameof(SpinnerType), "default");

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

        public event EventHandler<int>? ValueChanged;
        
        private readonly Dictionary<string, string[]> _displayValues = new()
        {
            ["batch-size"] = new[] { "minimal", "low", "default", "high" },
            ["min-score"] = new[] { "Auto", "1", "2", "3", "4", "5" },
            ["stake"] = new[] { "White", "Red", "Green", "Black", "Blue", "Purple", "Orange", "Gold" }
        };

        public SpinnerControl()
        {
            InitializeComponent();
            DataContext = this;
            
            // Set up threads maximum based on processor count
            if (Label == "THREADS")
            {
                Maximum = Environment.ProcessorCount;
            }
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
            var lowerLabel = Label.ToLowerInvariant();
            var spinnerType = SpinnerType.ToLowerInvariant();
            
            // Check SpinnerType first
            if (spinnerType == "stake" && _displayValues.ContainsKey("stake"))
            {
                var values = _displayValues["stake"];
                var index = Math.Max(0, Math.Min(Value, values.Length - 1));
                return values[index] + " Stake";
            }
            
            // Handle special display values based on label
            if (lowerLabel.Contains("batch") && _displayValues.ContainsKey("batch-size"))
            {
                var values = _displayValues["batch-size"];
                var index = Math.Max(0, Math.Min(Value - 1, values.Length - 1));
                return values[index];
            }
            
            if (lowerLabel.Contains("score") && _displayValues.ContainsKey("min-score"))
            {
                var values = _displayValues["min-score"];
                var index = Math.Max(0, Math.Min(Value, values.Length - 1));
                return values[index];
            }
            
            if (lowerLabel.Contains("stake") && _displayValues.ContainsKey("stake"))
            {
                var values = _displayValues["stake"];
                var index = Math.Max(0, Math.Min(Value, values.Length - 1));
                return values[index];
            }
            
            // Default to showing the numeric value
            return Value.ToString();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ValueProperty || change.Property == LabelProperty || change.Property == SpinnerTypeProperty)
            {
                // Update display text
                var valueText = this.FindControl<TextBlock>("ValueText");
                if (valueText != null)
                {
                    valueText.Text = GetDisplayValue();
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