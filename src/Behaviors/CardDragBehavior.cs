using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using BalatroSeedOracle.Constants;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Behaviors
{
    /// <summary>
    /// Balatro-style card drag animation with tilt, hover, and juice effects
    /// Implements the exact mechanics from card.lua and moveable.lua
    /// </summary>
    public class CardDragBehavior : Behavior<Control>
    {
        private DispatcherTimer? _animationTimer;
        private DateTime _startTime;
        private double _cardId;
        private Point? _lastPointerPosition;
        private Point? _pointerPressedPosition;
        private DateTime? _juiceStartTime;
        private bool _isHovering;
        private bool _isDragging;
        private TransformGroup? _transformGroup;
        private TranslateTransform? _translateTransform;
        private ScaleTransform? _scaleTransform;
        private RotateTransform? _rotateTransform;
        private SkewTransform? _skewTransform;
        private Control? _visualChild; // The actual visual content that gets transformed
        private Control? _hitboxElement; // The hitbox element that receives pointer events

        /// <summary>
        /// Enable/disable all animations
        /// </summary>
        public static readonly StyledProperty<bool> IsEnabledProperty = AvaloniaProperty.Register<
            CardDragBehavior,
            bool
        >(nameof(IsEnabled), true);

        public bool IsEnabled
        {
            get => GetValue(IsEnabledProperty);
            set => SetValue(IsEnabledProperty, value);
        }

        /// <summary>
        /// Juice intensity on grab (0.4 = Balatro default)
        /// </summary>
        public static readonly StyledProperty<double> JuiceAmountProperty =
            AvaloniaProperty.Register<CardDragBehavior, double>(
                nameof(JuiceAmount),
                UIConstants.CardJuiceScaleFactor
            );

        public double JuiceAmount
        {
            get => GetValue(JuiceAmountProperty);
            set => SetValue(JuiceAmountProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject == null)
                return;

            // Generate unique card ID for timing variation
            _cardId = new Random().NextDouble() * 100;
            _startTime = DateTime.Now;

            // PROPER SOLUTION: Find the first child control to apply transforms to
            // The parent (AssociatedObject) keeps its static hitbox for pointer events
            // Only the visual child gets transformed, preventing hitbox rotation issues
            if (
                AssociatedObject is Avalonia.Controls.Decorator decorator
                && decorator.Child is Control child
            )
            {
                _visualChild = child;
            }
            else if (AssociatedObject is Avalonia.Controls.Panel panel && panel.Children.Count > 0)
            {
                // Find the VISUAL child (IsHitTestVisible=False) to transform, NOT the hitbox
                foreach (var panelChild in panel.Children)
                {
                    if (panelChild is Control control && control.IsHitTestVisible == false)
                    {
                        _visualChild = control;
                        break;
                    }
                }
                // If no visual child found, use first child
                if (_visualChild == null)
                {
                    _visualChild = panel.Children[0] as Control;
                }
            }
            else
            {
                // Fallback: if no child found, transform the AssociatedObject itself (old behavior)
                _visualChild = AssociatedObject;
            }

            // Set up transform group with skew (for 3D perspective), rotation, translation, and scale
            _skewTransform = new SkewTransform(0, 0);
            _rotateTransform = new RotateTransform(0);
            _translateTransform = new TranslateTransform();
            _scaleTransform = new ScaleTransform(
                UIConstants.DefaultScaleFactor,
                UIConstants.DefaultScaleFactor
            );
            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(_skewTransform);
            _transformGroup.Children.Add(_rotateTransform);
            _transformGroup.Children.Add(_scaleTransform);
            _transformGroup.Children.Add(_translateTransform);

            // Apply transforms to the VISUAL CHILD, not the parent container
            _visualChild.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            _visualChild.RenderTransform = _transformGroup;

            // CRITICAL FIX: Attach pointer events to the HITBOX (IsHitTestVisible=True child), not the parent
            // This prevents the rotating visual child from affecting hit detection
            _hitboxElement = AssociatedObject;
            if (AssociatedObject is Avalonia.Controls.Panel panelForHitbox)
            {
                // Find the hitbox child (IsHitTestVisible=True, typically ZIndex=1)
                foreach (var panelChild in panelForHitbox.Children)
                {
                    if (panelChild is Control control && control.IsHitTestVisible == true)
                    {
                        _hitboxElement = control;
                        break;
                    }
                }
            }

            // Attach pointer events to the hitbox element (static, never rotates)
            _hitboxElement.PointerEntered += OnPointerEntered;
            _hitboxElement.PointerExited += OnPointerExited;
            _hitboxElement.PointerMoved += OnPointerMoved;
            _hitboxElement.PointerPressed += OnPointerPressed;
            _hitboxElement.PointerReleased += OnPointerReleased;

            // Create animation timer and start it immediately for ambient sway
            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(UIConstants.AnimationFrameRateMs), // 60 FPS
            };
            _animationTimer.Tick += OnAnimationTick;
            _animationTimer.Start(); // Always running for ambient idle animation
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (_hitboxElement != null)
            {
                _hitboxElement.PointerEntered -= OnPointerEntered;
                _hitboxElement.PointerExited -= OnPointerExited;
                _hitboxElement.PointerMoved -= OnPointerMoved;
                _hitboxElement.PointerPressed -= OnPointerPressed;
                _hitboxElement.PointerReleased -= OnPointerReleased;
            }

            _animationTimer?.Stop();
            _animationTimer = null;
        }

        private void OnPointerEntered(object? sender, PointerEventArgs e)
        {
            _isHovering = true;
            _lastPointerPosition = e.GetPosition(_hitboxElement);

            // Play Balatro card hover sound (paper1.ogg with random pitch)
            var sfxService = ServiceHelper.GetService<SoundEffectsService>();
            sfxService?.PlayCardHover();
        }

        private void OnPointerExited(object? sender, PointerEventArgs e)
        {
            _isHovering = false;
            _lastPointerPosition = null;
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_isHovering || _isDragging)
            {
                _lastPointerPosition = e.GetPosition(_hitboxElement);
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            _isDragging = true;
            _pointerPressedPosition = e.GetPosition(_hitboxElement);

            // Trigger juice animation (Balatro's bounce effect on pickup)
            _juiceStartTime = DateTime.Now;

            // Ensure animation timer is running when dragging
            if (_animationTimer != null && !_animationTimer.IsEnabled)
            {
                _animationTimer.Start();
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _isDragging = false;
            _pointerPressedPosition = null;
        }

        private void OnAnimationTick(object? sender, EventArgs e)
        {
            if (
                !IsEnabled
                || AssociatedObject == null
                || _translateTransform == null
                || _scaleTransform == null
                || _rotateTransform == null
                || _skewTransform == null
            )
                return;

            var elapsedSeconds = (DateTime.Now - _startTime).TotalSeconds;

            // Calculate lean/translation, rotation, and skew based on current mode
            double leanX = 0;
            double leanY = 0;
            double leanDistance = 0;
            double rotationAngle = 0;
            double skewX = 0;
            double skewY = 0;

            if (_isDragging && _lastPointerPosition.HasValue && _pointerPressedPosition.HasValue)
            {
                // DRAG MODE: Lean based on drag offset (like Balatro's focus state)
                var bounds = _hitboxElement?.Bounds ?? AssociatedObject.Bounds;
                var centerX = bounds.Width / 2;
                var centerY = bounds.Height / 2;

                // Calculate offset from center (hover_offset in Balatro)
                var offsetX = (_lastPointerPosition.Value.X - centerX) / bounds.Width;
                var offsetY = (_lastPointerPosition.Value.Y - centerY) / bounds.Height;

                // Drag delta from initial press
                var dx =
                    (_lastPointerPosition.Value.X - _pointerPressedPosition.Value.X) / bounds.Width;
                var dy =
                    (_lastPointerPosition.Value.Y - _pointerPressedPosition.Value.Y)
                    / bounds.Height;

                // Balatro formula: abs(hover_offset.y + hover_offset.x - 1 + dx + dy - 1) * 0.3
                leanDistance =
                    Math.Abs(offsetY + offsetX - 1 + dx + dy - 1)
                    * UIConstants.CardTiltFactorRadians
                    * 15.0; // Convert tilt factor to pixel distance

                // Lean towards drag direction
                leanX = offsetX * leanDistance;
                leanY = offsetY * leanDistance;
            }
            else if (_isHovering && _lastPointerPosition.HasValue)
            {
                // HOVER MODE: 3D perspective tilt toward cursor (like real Balatro!)
                var bounds = _hitboxElement?.Bounds ?? AssociatedObject.Bounds;
                var centerX = bounds.Width / 2;
                var centerY = bounds.Height / 2;

                var offsetX = (_lastPointerPosition.Value.X - centerX) / (bounds.Width / 2);
                var offsetY = (_lastPointerPosition.Value.Y - centerY) / (bounds.Height / 2);

                // 3D perspective tilt - simple and direct
                // Mouse LEFT/RIGHT (offsetX) → horizontal skew (tilt left/right)
                // Mouse UP/DOWN (offsetY) → vertical skew (tilt up/down)

                var skewStrength = 8; // Subtle 3D perspective effect

                // Horizontal skew from horizontal mouse movement
                skewX = offsetX * skewStrength;

                // Vertical skew from vertical mouse movement
                skewY = offsetY * skewStrength;
            }
            else
            {
                // AMBIENT MODE: Subtle breathing sway (like real Balatro!)
                // tilt_angle = G.TIMERS.REAL*(1.56 + (self.ID/1.14212)%1) + self.ID/1.35122
                var tiltAngle = elapsedSeconds * (1.56 + (_cardId / 1.14212) % 1) + _cardId / 1.35122;

                // Balatro's ambient tilt: self.ambient_tilt*(0.5+math.cos(tilt_angle))*tilt_factor
                var ambientTilt = UIConstants.CardAmbientTiltRadians; // 0.2
                var tiltFactor = 0.3;

                // Calculate rotation for breathing effect (using Balatro's exact formula)
                rotationAngle = ambientTilt * (0.5 + Math.Cos(tiltAngle)) * tiltFactor * 10;
            }

            // Apply 3D perspective (skew effect)
            _skewTransform.AngleX = skewX;
            _skewTransform.AngleY = skewY;

            // Apply rotation (tilt effect for ambient sway)
            _rotateTransform.Angle = rotationAngle;

            // Apply translation (lean effect) - ONLY when not dragging
            if (!_isDragging)
            {
                _translateTransform.X = leanX;
                _translateTransform.Y = leanY;
            }
            else
            {
                // During drag: origin card stays put (no transform)
                _translateTransform.X = 0;
                _translateTransform.Y = 0;
            }

            // Apply juice effect (bounce/wiggle on pickup) - ONLY when not dragging
            if (_juiceStartTime.HasValue && !_isDragging)
            {
                var juiceElapsed = (DateTime.Now - _juiceStartTime.Value).TotalSeconds;
                var juiceDuration = UIConstants.JuiceDurationSeconds;

                if (juiceElapsed < juiceDuration)
                {
                    // Calculate decay factor (cubic for scale, quadratic for translation wobble)
                    var progress = juiceElapsed / juiceDuration;
                    var decayScale = Math.Max(0, Math.Pow(1 - progress, 3));
                    var decayWobble = Math.Max(0, Math.Pow(1 - progress, 2));

                    // Scale oscillation: scale_amt * sin(FREQUENCY*t) * decay^3
                    var scaleJuice =
                        JuiceAmount
                        * Math.Sin(UIConstants.JuiceBounceFrequency * juiceElapsed)
                        * decayScale;
                    _scaleTransform.ScaleX = UIConstants.DefaultScaleFactor + scaleJuice;
                    _scaleTransform.ScaleY = UIConstants.DefaultScaleFactor + scaleJuice;

                    // Translation wobble: subtle X/Y oscillation instead of rotation
                    var wobbleX =
                        (JuiceAmount * UIConstants.CardJuiceRotationFactor * 10.0)
                        * Math.Sin(UIConstants.JuiceWobbleFrequency * juiceElapsed)
                        * decayWobble;
                    var wobbleY =
                        (JuiceAmount * UIConstants.CardJuiceRotationFactor * 5.0)
                        * Math.Cos(UIConstants.JuiceWobbleFrequency * juiceElapsed * 0.7)
                        * decayWobble;

                    // Add wobble to lean (compound the effects)
                    _translateTransform.X = leanX + wobbleX;
                    _translateTransform.Y = leanY + wobbleY;
                }
                else
                {
                    // Juice finished
                    _juiceStartTime = null;

                    // Apply hover scale if hovering (prevents seizure bug by creating buffer zone)
                    if (_isHovering)
                    {
                        _scaleTransform.ScaleX = 1.10;
                        _scaleTransform.ScaleY = 1.10;
                    }
                    else
                    {
                        _scaleTransform.ScaleX = 1.0;
                        _scaleTransform.ScaleY = 1.0;
                    }
                }
            }
            else
            {
                // No juice - apply hover scale if hovering (prevents seizure bug)
                if (_isHovering)
                {
                    _scaleTransform.ScaleX = 1.10;
                    _scaleTransform.ScaleY = 1.10;
                }
                else
                {
                    _scaleTransform.ScaleX = 1.0;
                    _scaleTransform.ScaleY = 1.0;
                }
            }
        }
    }
}
