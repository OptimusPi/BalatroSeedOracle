using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
using BalatroSeedOracle.Helpers;
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
        private const int BindingUpdateDelayMs = 50;

        private bool _isAnimating;
        private int _lastTriggerValue = -1;
        private CancellationTokenSource? _animationCts;

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

            // Ensure the Image has a ScaleTransform for the flip animation
            if (AssociatedObject.RenderTransform == null)
            {
                AssociatedObject.RenderTransform = new ScaleTransform();
                AssociatedObject.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            }

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
        /// Manually trigger the flip animation using proper Avalonia animations
        /// </summary>
        public async Task TriggerFlipAsync()
        {
            if (AssociatedObject == null || _isAnimating)
            {
                DebugLogger.Log("CardFlip", $"Skipping flip - AssociatedObject={AssociatedObject != null}, _isAnimating={_isAnimating}");
                return;
            }

            // Cancel any in-progress animation
            _animationCts?.Cancel();
            _animationCts = new CancellationTokenSource();
            var cancellationToken = _animationCts.Token;

            _isAnimating = true;
            DebugLogger.Log("CardFlip", $"Starting flip animation for deck: {DeckName}");

            try
            {
                // Wait for stagger delay (if specified)
                if (StaggerDelay > 0)
                {
                    await Task.Delay(StaggerDelay, cancellationToken);
                }

                // Give the binding a moment to apply the latest sprite before we cache it.
                await Task.Delay(BindingUpdateDelayMs, cancellationToken);

                var originalSource = AssociatedObject.Source;
                DebugLogger.Log("CardFlip", $"Cached original source: {originalSource != null}");

                // Get deck back sprite at DISPLAY size (64x85) to match actual card container size
                // This prevents cropping that occurs when a 71x95 sprite is forced into 64x85 container
                var deckBackSprite = SpriteService.Instance.GetDeckImage(DeckName, 64, 85);
                DebugLogger.Log("CardFlip", $"Got deck back sprite: {deckBackSprite != null}");

                if (deckBackSprite == null || originalSource == null)
                {
                    // Fallback: skip animation if sprites not available
                    DebugLogger.LogError("CardFlip", $"Skipping animation - deckBack={deckBackSprite != null}, original={originalSource != null}");
                    _isAnimating = false;
                    return;
                }

                // Find ALL overlay images (Edition, Stickers, SoulFace) and cache their visibility state
                var overlayImages = new List<Image>();
                var overlayVisibility = new Dictionary<Image, bool>();
                if (AssociatedObject.Parent is Grid parentGrid)
                {
                    foreach (var child in parentGrid.Children)
                    {
                        if (child is Image img && img != AssociatedObject)
                        {
                            overlayImages.Add(img);
                            overlayVisibility[img] = img.IsVisible; // Cache original visibility
                        }
                    }
                }

                // Keep reference to soul face for compatibility
                Image? soulFaceImage = overlayImages.FirstOrDefault();

                // Create the flip animation
                // We'll use ScaleTransform.ScaleX to create a horizontal flip effect
                var transform = AssociatedObject.RenderTransform as ScaleTransform;
                if (transform == null)
                {
                    DebugLogger.LogError("CardFlip", "RenderTransform is not a ScaleTransform!");
                    _isAnimating = false;
                    return;
                }

                // Animation parameters
                var flipDuration = TimeSpan.FromMilliseconds(150);
                var easing = new CubicEaseInOut();

                // Phase 1: Flip from front to edge (ScaleX: 1 -> 0)
                var flipToEdgeAnimation = new Avalonia.Animation.Animation
                {
                    Duration = flipDuration,
                    Easing = easing,
                    Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0.0),
                            Setters =
                            {
                                new Setter(ScaleTransform.ScaleXProperty, 1.0)
                            }
                        },
                        new KeyFrame
                        {
                            Cue = new Cue(1.0),
                            Setters =
                            {
                                new Setter(ScaleTransform.ScaleXProperty, 0.0)
                            }
                        }
                    }
                };

                // Phase 2: Flip from edge to back (ScaleX: 0 -> 1)
                var flipFromEdgeAnimation = new Avalonia.Animation.Animation
                {
                    Duration = flipDuration,
                    Easing = easing,
                    Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0.0),
                            Setters =
                            {
                                new Setter(ScaleTransform.ScaleXProperty, 0.0)
                            }
                        },
                        new KeyFrame
                        {
                            Cue = new Cue(1.0),
                            Setters =
                            {
                                new Setter(ScaleTransform.ScaleXProperty, 1.0)
                            }
                        }
                    }
                };

                // Execute Phase 1: Flip to edge while showing original sprite
                DebugLogger.Log("CardFlip", "Phase 1: Flipping to edge");
                await RunAnimationAsync(transform, flipToEdgeAnimation, cancellationToken);

                // At the midpoint (card is edge-on), swap to deck back and hide ALL overlays
                DebugLogger.Log("CardFlip", "Midpoint: Swapping to deck back sprite");
                AssociatedObject.Source = deckBackSprite;
                foreach (var overlay in overlayImages)
                {
                    overlay.IsVisible = false;
                }

                // Execute Phase 2: Flip from edge to show deck back
                DebugLogger.Log("CardFlip", "Phase 2: Revealing deck back");
                await RunAnimationAsync(transform, flipFromEdgeAnimation, cancellationToken);

                // Brief pause to show deck back
                await Task.Delay(100, cancellationToken);

                // Execute Phase 3: Flip to edge again
                DebugLogger.Log("CardFlip", "Phase 3: Flipping to edge");
                await RunAnimationAsync(transform, flipToEdgeAnimation, cancellationToken);

                // At the midpoint, swap back to joker sprite and restore overlays to ORIGINAL visibility
                DebugLogger.Log("CardFlip", "Midpoint: Swapping to joker sprite");
                AssociatedObject.Source = originalSource;
                foreach (var overlay in overlayImages)
                {
                    overlay.IsVisible = overlayVisibility[overlay]; // Restore cached visibility state
                }

                // Execute Phase 4: Flip from edge to show joker with new enhancement
                DebugLogger.Log("CardFlip", "Phase 4: Revealing enhanced joker");
                await RunAnimationAsync(transform, flipFromEdgeAnimation, cancellationToken);

                DebugLogger.Log("CardFlip", "Animation completed successfully");
            }
            catch (OperationCanceledException)
            {
                DebugLogger.Log("CardFlip", "Animation was cancelled");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("CardFlip", $"Animation failed: {ex.Message}");
            }
            finally
            {
                _isAnimating = false;
            }
        }

        /// <summary>
        /// Helper method to run an Avalonia animation on a transform with cancellation support.
        /// In Avalonia 11.x, we need to use the RunAsync extension method on the Animatable object (transform).
        /// </summary>
        private async Task RunAnimationAsync(
            ScaleTransform transform,
            Avalonia.Animation.Animation animation,
            CancellationToken cancellationToken
        )
        {
            // Create a TaskCompletionSource to handle cancellation
            var tcs = new TaskCompletionSource<bool>();

            // Register cancellation callback
            using var cancellationRegistration = cancellationToken.Register(() =>
            {
                tcs.TrySetCanceled();
            });

            // Run the animation - in Avalonia 11.x, RunAsync returns a Task
            var animationTask = animation.RunAsync(transform, CancellationToken.None);

            // Wait for either the animation to complete or cancellation
            var completedTask = await Task.WhenAny(animationTask, tcs.Task);

            if (completedTask == tcs.Task)
            {
                // Cancellation was requested
                cancellationToken.ThrowIfCancellationRequested();
            }

            // Animation completed successfully
            await animationTask;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            // Cancel any in-progress animation and dispose of the cancellation token
            _animationCts?.Cancel();
            _animationCts?.Dispose();
            _animationCts = null;
        }
    }
}
