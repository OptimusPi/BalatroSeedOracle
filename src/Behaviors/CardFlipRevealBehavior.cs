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
    /// Behavior that creates a Balatro-style card flip reveal animation.
    /// Shows the current deck back, then flips to reveal the actual sprite.
    ///
    /// Extracted from Balatro source: external/Balatro/card.lua:flip() and engine/moveable.lua:pinch
    ///
    /// The flip uses a "pinch" effect where:
    /// 1. ScaleX shrinks to 0 (card pinches horizontally)
    /// 2. At ScaleX = 0, swap deck back → real sprite
    /// 3. ScaleX expands back to 1 (card unpinches)
    ///
    /// Pinch rate: 8 units/second (from Balatro: self.VT.w = self.VT.w + (8*dt)*...)
    /// </summary>
    public class CardFlipRevealBehavior : Behavior<Image>
    {
        private IImage? _cachedRevealSprite;
        private bool _isAnimating;
        private IDisposable? _sourceSubscription;

        /// <summary>
        /// The deck name to show on the card back (e.g., "Red", "Anaglyph")
        /// </summary>
        public static readonly StyledProperty<string> DeckNameProperty = AvaloniaProperty.Register<
            CardFlipRevealBehavior,
            string
        >(nameof(DeckName), "Red");

        public string DeckName
        {
            get => GetValue(DeckNameProperty);
            set => SetValue(DeckNameProperty, value);
        }

        /// <summary>
        /// The final sprite to reveal after flip (set this to the actual joker/item sprite)
        /// </summary>
        public static readonly StyledProperty<IImage?> RevealSpriteProperty =
            AvaloniaProperty.Register<CardFlipRevealBehavior, IImage?>(nameof(RevealSprite));

        public IImage? RevealSprite
        {
            get => GetValue(RevealSpriteProperty);
            set => SetValue(RevealSpriteProperty, value);
        }

        /// <summary>
        /// Delay before starting the flip (useful for staggered animations)
        /// </summary>
        public static readonly StyledProperty<TimeSpan> DelayProperty = AvaloniaProperty.Register<
            CardFlipRevealBehavior,
            TimeSpan
        >(nameof(Delay), TimeSpan.Zero);

        public TimeSpan Delay
        {
            get => GetValue(DelayProperty);
            set => SetValue(DelayProperty, value);
        }

        /// <summary>
        /// Whether to automatically trigger the flip on attach (default: true)
        /// </summary>
        public static readonly StyledProperty<bool> AutoTriggerProperty = AvaloniaProperty.Register<
            CardFlipRevealBehavior,
            bool
        >(nameof(AutoTrigger), true);

        public bool AutoTrigger
        {
            get => GetValue(AutoTriggerProperty);
            set => SetValue(AutoTriggerProperty, value);
        }

        /// <summary>
        /// Whether to watch Image.Source property changes (for converter-based bindings)
        /// </summary>
        public static readonly StyledProperty<bool> WatchSourceProperty = AvaloniaProperty.Register<
            CardFlipRevealBehavior,
            bool
        >(nameof(WatchSource), false);

        public bool WatchSource
        {
            get => GetValue(WatchSourceProperty);
            set => SetValue(WatchSourceProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject == null)
                return;

            // Listen for RevealSprite changes to trigger flip
            this.GetObservable(RevealSpriteProperty)
                .Subscribe(sprite =>
                {
                    if (AutoTrigger && sprite != null && !WatchSource)
                    {
                        _ = TriggerFlipAsync();
                    }
                });

            // Listen for Image.Source changes when WatchSource is enabled
            // This supports converter-based bindings like:
            // <Image Source="{Binding ., Converter={StaticResource ItemNameToSpriteConverter}}" />
            if (WatchSource)
            {
                _sourceSubscription = AssociatedObject
                    .GetObservable(Image.SourceProperty)
                    .Subscribe(newSource =>
                    {
                        if (AutoTrigger && newSource != null && !_isAnimating)
                        {
                            // Cache the converted sprite and trigger flip
                            _cachedRevealSprite = newSource;
                            _ = TriggerFlipAsync();
                        }
                    });
            }
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
                // Wait for delay
                if (Delay > TimeSpan.Zero)
                {
                    await System.Threading.Tasks.Task.Delay(Delay);
                }

                // Determine which sprite to reveal:
                // 1. If WatchSource is true, use the cached sprite from Image.Source
                // 2. Otherwise, use the explicitly set RevealSprite property
                var spriteToReveal = WatchSource ? _cachedRevealSprite : RevealSprite;

                if (spriteToReveal == null)
                {
                    _isAnimating = false;
                    return;
                }

                // Get deck back sprite
                var deckBackSprite = SpriteService.Instance.GetDeckImage(DeckName, 71, 95); // Standard card size

                if (deckBackSprite == null)
                {
                    // Fallback: skip animation and just show the reveal sprite
                    AssociatedObject.Source = spriteToReveal as Bitmap;
                    _isAnimating = false;
                    return;
                }

                // Step 1: Show deck back
                AssociatedObject.Source = deckBackSprite as Bitmap;

                // Create ScaleTransform for animation (this is what Avalonia can actually animate!)
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

                // Step 2: Pinch in (ScaleX: 1 → 0.7 to keep card VERY visible during flip)
                var pinchIn = new Avalonia.Animation.Animation
                {
                    Duration = pinchDuration,
                    Easing = new LinearEasing(),
                    Children =
                    {
                        new Avalonia.Animation.KeyFrame
                        {
                            Cue = new Cue(1),
                            Setters =
                            {
                                new Setter(
                                    ScaleTransform.ScaleXProperty,
                                    0.7  // Keep card VERY visible (0.5 still too narrow!)
                                ),
                            },
                        },
                    },
                };

                await pinchIn.RunAsync(scaleTransform);

                // Step 3: Swap sprite at ScaleX = 0
                AssociatedObject.Source = spriteToReveal as Bitmap;

                // Step 4: Pinch out (ScaleX: 0 → 1)
                var pinchOut = new Avalonia.Animation.Animation
                {
                    Duration = pinchDuration,
                    Easing = new LinearEasing(),
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
                    Easing = new ElasticEaseOut(),
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
                                    UIConstants.CardFlipJuiceScalePeak
                                ),
                                new Setter(
                                    ScaleTransform.ScaleYProperty,
                                    UIConstants.CardFlipJuiceScalePeak
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

            // Clean up the source property subscription
            _sourceSubscription?.Dispose();
            _sourceSubscription = null;
            _cachedRevealSprite = null;
        }
    }
}
