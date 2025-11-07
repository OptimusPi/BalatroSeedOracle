using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace BalatroSeedOracle.Controls.Navigation
{
    /// <summary>
    /// A button that has an intrinsic IsSelected property and shows a selection indicator.
    /// This is the RIGHT WAY - selection state is part of the control, not external positioning!
    /// </summary>
    public class SelectableCategoryButton : Button
    {
        /// <summary>
        /// Defines the IsSelected property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSelectedProperty =
            AvaloniaProperty.Register<SelectableCategoryButton, bool>(nameof(IsSelected));

        /// <summary>
        /// Defines the Category property to identify which category this button represents.
        /// </summary>
        public static readonly StyledProperty<string> CategoryProperty =
            AvaloniaProperty.Register<SelectableCategoryButton, string>(nameof(Category), string.Empty);

        /// <summary>
        /// Gets or sets whether this button is selected.
        /// </summary>
        public bool IsSelected
        {
            get => GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        /// <summary>
        /// Gets or sets the category this button represents.
        /// </summary>
        public string Category
        {
            get => GetValue(CategoryProperty);
            set => SetValue(CategoryProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            
            if (change.Property == IsSelectedProperty)
            {
                // Update pseudoclass when selection changes
                PseudoClasses.Set(":selected", IsSelected);
            }
        }
    }
}
