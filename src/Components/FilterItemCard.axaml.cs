using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using BalatroSeedOracle.Behaviors;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// SELF-CONTAINED Balatro card component with ALL behaviors baked in.
    /// NO manual behavior attachment needed in parent - just set DataContext!
    ///
    /// Features:
    /// - MagneticTiltBehavior for hover effect
    /// - CardFlipOnTriggerBehavior for wave flip animation (attached via code-behind)
    /// - All visual overlays (Edition, Stickers, Soul Face)
    /// - Drag opacity feedback
    ///
    /// Usage: <components:FilterItemCard DataContext="{Binding}"/>
    /// </summary>
    public partial class FilterItemCard : UserControl
    {
        /// <summary>
        /// Trigger value for flip animation wave effect.
        /// Bind this to a counter in ViewModel that increments when Edition/Sticker changes.
        /// </summary>
        public static readonly StyledProperty<int> FlipTriggerProperty = AvaloniaProperty.Register<
            FilterItemCard,
            int
        >(nameof(FlipTrigger), 0);

        public int FlipTrigger
        {
            get => GetValue(FlipTriggerProperty);
            set => SetValue(FlipTriggerProperty, value);
        }

        /// <summary>
        /// Stagger delay in milliseconds for wave flip animation.
        /// Each card in shelf should have a unique delay (0, 50, 100, 150...).
        /// </summary>
        public static readonly StyledProperty<int> StaggerDelayProperty = AvaloniaProperty.Register<
            FilterItemCard,
            int
        >(nameof(StaggerDelay), 0);

        public int StaggerDelay
        {
            get => GetValue(StaggerDelayProperty);
            set => SetValue(StaggerDelayProperty, value);
        }

        /// <summary>
        /// Deck name for flip animation back sprite (e.g., "Red", "Anaglyph").
        /// Defaults to "Red" deck.
        /// </summary>
        public static readonly StyledProperty<string> DeckNameProperty = AvaloniaProperty.Register<
            FilterItemCard,
            string
        >(nameof(DeckName), "Red");

        public string DeckName
        {
            get => GetValue(DeckNameProperty);
            set => SetValue(DeckNameProperty, value);
        }

        public FilterItemCard()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            // Attach CardFlipOnTriggerBehavior to the base card image
            var baseImage = this.FindControl<Image>("BaseCardImage");
            if (baseImage != null)
            {
                var flipBehavior = new CardFlipOnTriggerBehavior();

                // Bind behavior properties to our StyledProperties
                flipBehavior.Bind(
                    CardFlipOnTriggerBehavior.FlipTriggerProperty,
                    this.GetObservable(FlipTriggerProperty)
                );

                flipBehavior.Bind(
                    CardFlipOnTriggerBehavior.StaggerDelayProperty,
                    this.GetObservable(StaggerDelayProperty)
                );

                flipBehavior.Bind(
                    CardFlipOnTriggerBehavior.DeckNameProperty,
                    this.GetObservable(DeckNameProperty)
                );

                // Attach behavior to the base image
                var behaviors = Interaction.GetBehaviors(baseImage);
                if (behaviors != null)
                {
                    behaviors.Add(flipBehavior);
                }
            }
        }
    }
}
