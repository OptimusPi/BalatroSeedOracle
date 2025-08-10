using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Oracle.Controls
{
    public partial class ModeToggleSwitch : UserControl
    {
        public static readonly StyledProperty<string> LeftTextProperty = AvaloniaProperty.Register<
            ModeToggleSwitch,
            string
        >(nameof(LeftText), "OFF");

        public static readonly StyledProperty<string> RightTextProperty = AvaloniaProperty.Register<
            ModeToggleSwitch,
            string
        >(nameof(RightText), "ON");

        public static readonly StyledProperty<bool> IsCheckedProperty = AvaloniaProperty.Register<
            ModeToggleSwitch,
            bool
        >(nameof(IsChecked), false);

        public string LeftText
        {
            get => GetValue(LeftTextProperty);
            set => SetValue(LeftTextProperty, value);
        }

        public string RightText
        {
            get => GetValue(RightTextProperty);
            set => SetValue(RightTextProperty, value);
        }

        public bool IsChecked
        {
            get => GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        public event EventHandler<bool>? IsCheckedChanged;

        public ModeToggleSwitch()
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

            if (change.Property == IsCheckedProperty)
            {
                UpdateVisualState();

                var newValue = (bool)change.NewValue!;
                var oldValue = (bool)change.OldValue!;

                if (newValue != oldValue)
                {
                    IsCheckedChanged?.Invoke(this, newValue);
                }
            }
        }

        private void UpdateVisualState()
        {
            var knob = this.FindControl<Border>("ToggleKnob");
            var leftLabel = this.FindControl<TextBlock>("LeftLabel");
            var rightLabel = this.FindControl<TextBlock>("RightLabel");

            if (knob != null)
            {
                knob.Classes.Set("on", IsChecked);
            }

            if (leftLabel != null && rightLabel != null)
            {
                leftLabel.Classes.Set("active", !IsChecked);
                rightLabel.Classes.Set("active", IsChecked);
            }
        }

        protected void OnToggleClick(object? sender, PointerPressedEventArgs e)
        {
            IsChecked = !IsChecked;
            e.Handled = true;
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            UpdateVisualState();
        }
    }
}
