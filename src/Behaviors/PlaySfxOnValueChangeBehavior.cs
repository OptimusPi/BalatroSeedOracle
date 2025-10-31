using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Behaviors
{
    public class PlaySfxOnValueChangeBehavior : Behavior<Slider>
    {
        public static readonly StyledProperty<string> SoundProperty = AvaloniaProperty.Register<
            PlaySfxOnValueChangeBehavior,
            string
        >(nameof(Sound), "whoosh");

        public static readonly StyledProperty<double> ThresholdProperty = AvaloniaProperty.Register<
            PlaySfxOnValueChangeBehavior,
            double
        >(nameof(Threshold), 1.0);

        public static readonly StyledProperty<int> MinIntervalMsProperty =
            AvaloniaProperty.Register<PlaySfxOnValueChangeBehavior, int>(
                nameof(MinIntervalMs),
                150
            );

        private DateTime _lastPlayTime = DateTime.MinValue;
        private double _lastValue;
        private bool _initialized;

        public string Sound
        {
            get => GetValue(SoundProperty);
            set => SetValue(SoundProperty, value);
        }

        public double Threshold
        {
            get => GetValue(ThresholdProperty);
            set => SetValue(ThresholdProperty, value);
        }

        public int MinIntervalMs
        {
            get => GetValue(MinIntervalMsProperty);
            set => SetValue(MinIntervalMsProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject is Slider slider)
            {
                _lastValue = slider.Value;
                _initialized = true;
                slider.PropertyChanged += OnSliderPropertyChanged;
                slider.PointerReleased += OnPointerReleased;
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject is Slider slider)
            {
                slider.PropertyChanged -= OnSliderPropertyChanged;
                slider.PointerReleased -= OnPointerReleased;
            }
            base.OnDetaching();
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            TryPlay();
        }

        private void OnSliderPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Slider.ValueProperty && _initialized)
            {
                if (e.NewValue is double newValue)
                {
                    var delta = Math.Abs(newValue - _lastValue);
                    _lastValue = newValue;

                    if (delta >= Threshold)
                    {
                        TryPlay();
                    }
                }
            }
        }

        private void TryPlay()
        {
            var now = DateTime.UtcNow;
            if ((now - _lastPlayTime).TotalMilliseconds < MinIntervalMs)
            {
                return;
            }

            _lastPlayTime = now;

            // Default to whoosh if specific sound isn't exposed
            // Sound effects disabled - NAudio removed for cross-platform compatibility
            // if (string.Equals(Sound, "whoosh", StringComparison.OrdinalIgnoreCase))
            // {
            //     SoundEffectService.Instance.PlayWhoosh();
            // }
            // else
            // {
            //     SoundEffectService.Instance.PlayButtonClick();
            // }
        }
    }
}
