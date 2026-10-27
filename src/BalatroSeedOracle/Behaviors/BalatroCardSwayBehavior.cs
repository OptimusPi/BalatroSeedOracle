using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using BalatroSeedOracle.Constants;

namespace BalatroSeedOracle.Behaviors
{
    /// <summary>
    /// Balatro-style card sway animation - EXACTLY like the real game!
    /// Uses ambient tilt (breathing motion) based on cos/sin waves
    /// </summary>
    public class BalatroCardSwayBehavior : Behavior<Control>
    {
        private static readonly object _timerLock = new();
        private static readonly HashSet<BalatroCardSwayBehavior> _activeBehaviors = new();
        private static DispatcherTimer? _sharedTimer;

        private static void RegisterActive(BalatroCardSwayBehavior behavior)
        {
            lock (_timerLock)
            {
                _activeBehaviors.Add(behavior);
                if (_sharedTimer == null)
                {
                    _sharedTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(UIConstants.AnimationFrameRateMs),
                    };
                    _sharedTimer.Tick += OnSharedTimerTick;
                }
                if (!_sharedTimer.IsEnabled)
                {
                    _sharedTimer.Start();
                }
            }
        }

        private static void UnregisterActive(BalatroCardSwayBehavior behavior)
        {
            lock (_timerLock)
            {
                _activeBehaviors.Remove(behavior);
                if (_activeBehaviors.Count == 0 && _sharedTimer is { IsEnabled: true })
                {
                    _sharedTimer.Stop();
                }
            }
        }

        private static void OnSharedTimerTick(object? sender, EventArgs e)
        {
            BalatroCardSwayBehavior[] snapshot;
            lock (_timerLock)
            {
                if (_activeBehaviors.Count == 0)
                {
                    _sharedTimer?.Stop();
                    return;
                }
                snapshot = new BalatroCardSwayBehavior[_activeBehaviors.Count];
                _activeBehaviors.CopyTo(snapshot);
            }

            foreach (var behavior in snapshot)
            {
                behavior.OnAnimationTick();
            }
        }

        private DateTime _startTime;
        private double _cardId; // Unique ID for timing variation
        private bool _isHovering; // Track if mouse is over the card
        private RotateTransform? _rotateTransform;

        /// <summary>
        /// Ambient tilt strength (0.2 = Balatro default)
        /// </summary>
        public static readonly StyledProperty<double> AmbientTiltProperty =
            AvaloniaProperty.Register<BalatroCardSwayBehavior, double>(
                nameof(AmbientTilt),
                UIConstants.CardAmbientTiltMultiplier
            );

        public double AmbientTilt
        {
            get => GetValue(AmbientTiltProperty);
            set => SetValue(AmbientTiltProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject == null)
                return;

            // Generate unique card ID for timing variation (like Balatro)
            _cardId = Random.Shared.NextDouble() * 100;
            _startTime = DateTime.Now;

            // Setup or retrieve the shared TransformGroup to prevent stomping on other behaviors
            TransformGroup? group = AssociatedObject.RenderTransform as TransformGroup;
            if (group == null)
            {
                group = new TransformGroup();
                if (AssociatedObject.RenderTransform is Transform t)
                {
                    group.Children.Add(t);
                }
                AssociatedObject.RenderTransform = group;
                AssociatedObject.RenderTransformOrigin = new RelativePoint(
                    0.5,
                    0.5,
                    RelativeUnit.Relative
                );
            }

            _rotateTransform = new RotateTransform();
            group.Children.Add(_rotateTransform);

            // Listen for hover events to pause sway when hovering
            AssociatedObject.PointerEntered += OnPointerEntered;
            AssociatedObject.PointerExited += OnPointerExited;
            AssociatedObject.AttachedToVisualTree += OnVisualTreeAttached;
            AssociatedObject.DetachedFromVisualTree += OnVisualTreeDetached;

            if (TopLevel.GetTopLevel(AssociatedObject) != null)
            {
                RegisterActive(this);
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (AssociatedObject != null)
            {
                AssociatedObject.PointerEntered -= OnPointerEntered;
                AssociatedObject.PointerExited -= OnPointerExited;
                AssociatedObject.AttachedToVisualTree -= OnVisualTreeAttached;
                AssociatedObject.DetachedFromVisualTree -= OnVisualTreeDetached;

                if (AssociatedObject.RenderTransform is TransformGroup group && _rotateTransform != null)
                {
                    group.Children.Remove(_rotateTransform);
                    if (group.Children.Count == 0)
                    {
                        AssociatedObject.RenderTransform = null;
                    }
                }
            }

            UnregisterActive(this);
            _rotateTransform = null;
        }

        private void OnVisualTreeAttached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            RegisterActive(this);
        }

        private void OnVisualTreeDetached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            _isHovering = false;
            UnregisterActive(this);
        }

        private void OnPointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            _isHovering = true;
        }

        private void OnPointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            _isHovering = false;
        }

        private void OnAnimationTick()
        {
            if (AssociatedObject == null || !AssociatedObject.IsEffectivelyVisible || _rotateTransform == null)
                return;

            // STOP sway when hovering - let magnetic tilt take over!
            if (_isHovering)
                return;

            // AMBIENT MODE - Breathing motion when not hovering
            var elapsedSeconds = (DateTime.Now - _startTime).TotalSeconds;
            var tilt_angle = elapsedSeconds * (1.56 + (_cardId / 1.14212) % 1) + _cardId / 1.35122;
            var swayAngle = AmbientTilt * Math.Cos(tilt_angle) * 10; // 10 degrees max sway
            _rotateTransform.Angle = swayAngle;
        }
    }
}
