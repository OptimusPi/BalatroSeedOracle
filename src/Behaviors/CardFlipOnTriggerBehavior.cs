using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Xaml.Interactivity;
using BalatroSeedOracle.Constants;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Behaviors
{
    /// <summary>
    /// Behavior that triggers a flip animation when a property changes.
    /// Used to flip all cards in shelf when user changes edition/sticker/seal.
    ///
    /// This behavior watches a trigger property (like FlipAnimationTrigger counter)
    /// and automatically flips the card when the value changes, showing:
    /// 1. Current deck back
    /// 2. 3D flip rotation
    /// 3. Reveal with new edition/sticker applied
    /// </summary>
    public class CardFlipOnTriggerBehavior : Behavior<Image>
    {
        private bool _isAnimating;
        private int _lastTriggerValue = -1;

        /// <summary>
        /// The deck name to show on the card back (e.g., "Red", "Anaglyph")
        /// </summary>
        public static readonly StyledProperty<string> DeckNameProperty = AvaloniaProperty.Register<
            CardFlipOnTriggerBehavior,
            string
        >(nameof(DeckName), "Red");

        public string DeckName
        {
            get => GetValue(DeckNameProperty);
            set => SetValue(DeckNameProperty, value);
        }

        /// <summary>
        /// Trigger property - when this changes, the flip animation runs
        /// Typically bound to a counter in the ViewModel that increments on edition/sticker changes
        /// </summary>
        public static readonly StyledProperty<int> FlipTriggerProperty = AvaloniaProperty.Register<
            CardFlipOnTriggerBehavior,
            int
        >(nameof(FlipTrigger), 0);

        public int FlipTrigger
        {
            get => GetValue(FlipTriggerProperty);
            set => SetValue(FlipTriggerProperty, value);
        }

        /// <summary>
        /// Stagger delay for this card (milliseconds)
        /// Set different values for each card to create a wave effect
        /// </summary>
        public static readonly StyledProperty<int> StaggerDelayProperty = AvaloniaProperty.Register<
            CardFlipOnTriggerBehavior,
            int
        >(nameof(StaggerDelay), 0);

        public int StaggerDelay
        {
            get => GetValue(StaggerDelayProperty);
            set => SetValue(StaggerDelayProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject == null)
                return;

            // Watch for FlipTrigger changes
            this.GetObservable(FlipTriggerProperty)
                .Subscribe(triggerValue =>
                {
                    // Only flip if trigger value changed (not initial set)
                    if (_lastTriggerValue != -1 && triggerValue != _lastTriggerValue)
                    {
                        _ = TriggerFlipAsync();
                    }
                    _lastTriggerValue = triggerValue;
                });
        }

        /// <summary>
        /// Manually trigger the flip animation
        /// </summary>
        public async Task TriggerFlipAsync()
        {
            if (AssociatedObject == null || _isAnimating)
                return;

            _isAnimating = true;

            try
            {
                // Wait for stagger delay
                if (StaggerDelay > 0)
                {
                    await Task.Delay(StaggerDelay);
                }

                // CRITICAL: Wait for bindings to update before caching the source
                // When edition/seal/sticker changes, FlipTrigger++ happens immediately,
                // but the ItemImage binding takes a few milliseconds to update.
                // We need to cache the NEW source (with new enhancement), not the old one!
                await Task.Delay(50); // Small delay to let ItemImage binding update

                // Cache the NEW sprite source (now has updated enhancement/seal)
                var originalSource = AssociatedObject.Source;

                // Get deck back sprite
                var deckBackSprite = SpriteService.Instance.GetDeckImage(DeckName, 71, 95);

                if (deckBackSprite == null || originalSource == null)
                {
                    // Fallback: skip animation if sprites not available
                    _isAnimating = false;
                    return;
                }

                // Find the SoulFaceImage sibling (if it exists) to hide during flip
                Image? soulFaceImage = null;
                if (AssociatedObject.Parent is Grid parentGrid)
                {
                    foreach (var child in parentGrid.Children)
                    {
                        if (child is Image img && img != AssociatedObject && img.IsVisible)
                        {
                            soulFaceImage = img;
                            break;
                        }
                    }
                }

                // Step 1: Show deck back and hide soul face overlay
                AssociatedObject.Source = deckBackSprite as Bitmap;
                if (soulFaceImage != null)
                {
                    soulFaceImage.IsVisible = false;
                }

                // Create ScaleTransform for animation
                var scaleTransform = new ScaleTransform(
                    UIConstants.DefaultScaleFactor,
                    UIConstants.DefaultScaleFactor
                );
                AssociatedObject.RenderTransform = scaleTransform;
                AssociatedObject.RenderTransformOrigin = new RelativePoint(
                    0.5,
                    0.5,
                    RelativeUnit.Relative
                );

                var pinchDuration = TimeSpan.FromMilliseconds(UIConstants.QuickAnimationDurationMs);

                // Step 2: Pinch in (ScaleX: 1 → 0.3 to keep card visible during flip)
                var pinchIn = new Avalonia.Animation.Animation
                {
                    Duration = pinchDuration,
                    Easing = new CubicEaseIn(), // Smooth ease in
                    Children =
                    {
                        new Avalonia.Animation.KeyFrame
                        {
                            Cue = new Cue(1),
                            Setters =
                            {
                                new Setter(
                                    ScaleTransform.ScaleXProperty,
                                    0.3  // Keep card clearly visible during flip (was 0.15, too narrow)
                                ),
                            },
                        },
                    },
                };

                await pinchIn.RunAsync(scaleTransform);

                // Step 3: Swap sprite at ScaleX = 0 and restore soul face overlay
                AssociatedObject.Source = originalSource;
                if (soulFaceImage != null)
                {
                    soulFaceImage.IsVisible = true;
                }

                // Step 4: Pinch out (ScaleX: 0 → 1)
                var pinchOut = new Avalonia.Animation.Animation
                {
                    Duration = pinchDuration,
                    Easing = new CubicEaseOut(), // Smooth ease out
                    Children =
                    {
                        new Avalonia.Animation.KeyFrame
                        {
                            Cue = new Cue(1),
                            Setters =
                            {
                                new Setter(
                                    ScaleTransform.ScaleXProperty,
                                    UIConstants.DefaultScaleFactor
                                ),
                            },
                        },
                    },
                };

                await pinchOut.RunAsync(scaleTransform);

                // Step 5: Juice up! (both X and Y scale bounce)
                var juiceUp = new Avalonia.Animation.Animation
                {
                    Duration = TimeSpan.FromMilliseconds(UIConstants.MediumAnimationDurationMs),
                    Easing = new ElasticEaseOut(), // Bouncy feel
                    Children =
                    {
                        new Avalonia.Animation.KeyFrame
                        {
                            Cue = new Cue(0),
                            Setters =
                            {
                                new Setter(
                                    ScaleTransform.ScaleXProperty,
                                    UIConstants.DefaultScaleFactor
                                ),
                                new Setter(
                                    ScaleTransform.ScaleYProperty,
                                    UIConstants.DefaultScaleFactor
                                ),
                            },
                        },
                        new Avalonia.Animation.KeyFrame
                        {
                            Cue = new Cue(0.5),
                            Setters =
                            {
                                new Setter(
                                    ScaleTransform.ScaleXProperty,
                                    1.15
                                ),
                                new Setter(
                                    ScaleTransform.ScaleYProperty,
                                    1.15
                                ),
                            },
                        },
                        new Avalonia.Animation.KeyFrame
                        {
                            Cue = new Cue(1),
                            Setters =
                            {
                                new Setter(
                                    ScaleTransform.ScaleXProperty,
                                    UIConstants.DefaultScaleFactor
                                ),
                                new Setter(
                                    ScaleTransform.ScaleYProperty,
                                    UIConstants.DefaultScaleFactor
                                ),
                            },
                        },
                    },
                };

                await juiceUp.RunAsync(scaleTransform);

                // Reset render transform to allow other behaviors (sway, tilt) to work
                AssociatedObject.RenderTransform = null;
            }
            finally
            {
                // Ensure transform is reset even if animation is interrupted
                if (AssociatedObject != null && AssociatedObject.RenderTransform is ScaleTransform st)
                {
                    st.ScaleX = UIConstants.DefaultScaleFactor;
                    st.ScaleY = UIConstants.DefaultScaleFactor;
                }
                _isAnimating = false;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
        }
    }
}
