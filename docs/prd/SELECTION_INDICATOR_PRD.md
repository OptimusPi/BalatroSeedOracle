# PRD: Refactor Filter Category Navigation to Use Intrinsic Selection State

## Executive Summary
Refactor the FilterCategoryNav component to use the existing `SelectableCategoryButton` control, eliminating the external arrow positioning anti-pattern that has consumed ~23 hours of development time. The selection indicator (bouncing red arrow) should be an intrinsic visual state of each button, not an external element requiring C# positioning logic.

## Problem Statement
The current implementation violates fundamental UI architecture principles:
- **External arrow management**: A Canvas with a Polygon arrow is positioned externally via C# code
- **MVVM violation**: Visual state (selection indicator) is managed in code-behind or ViewModels
- **Complexity overhead**: Simple button selection requires complex positioning calculations
- **Time waste**: ~23 hours spent on what should be a trivial visual state change

## Solution Architecture

### Use Existing SelectableCategoryButton
The project already has the correct implementation at `src/Controls/Navigation/SelectableCategoryButton.cs`:
- Has `IsSelected` property
- Arrow is part of the button template
- Selection state triggers `:selected` pseudoclass
- Bouncing animation is pure XAML

### Required Changes

#### 1. Update FilterCategoryNav.axaml
Replace current implementation with SelectableCategoryButton usage:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:nav="using:BalatroSeedOracle.Controls.Navigation"
             xmlns:vm="using:BalatroSeedOracle.ViewModels.FilterTabs"
             x:Class="BalatroSeedOracle.Components.Shared.FilterCategoryNav"
             x:DataType="vm:FilterTabViewModelBase"
             x:CompileBindings="True">

    <Grid RowDefinitions="Auto,8,Auto,8,*">
        <!-- Filter Name Display -->
        <Border Grid.Row="0"
                Background="{StaticResource DarkBackground}"
                BorderThickness="0"
                CornerRadius="12"
                Padding="8,6"
                MaxWidth="160">
            <TextBlock Text="{Binding FilterName, FallbackValue='New Filter'}"
                       FontFamily="{StaticResource BalatroFont}"
                       FontSize="13"
                       Foreground="{StaticResource Gold}"
                       HorizontalAlignment="Center"
                       TextTrimming="CharacterEllipsis"/>
        </Border>

        <!-- Search Box -->
        <TextBox Name="SearchBox"
                 Grid.Row="2"
                 Watermark="Search items..."
                 Text="{Binding SearchFilter, Mode=TwoWay}"
                 Background="{StaticResource DarkBackground}"
                 BorderThickness="0"
                 CornerRadius="8"
                 Foreground="{StaticResource White}"
                 Padding="8,6"
                 VerticalContentAlignment="Center"
                 HorizontalAlignment="Stretch"
                 MaxWidth="160"
                 FontSize="12"/>

        <!-- Category buttons with INTRINSIC selection state -->
        <StackPanel Grid.Row="4" Spacing="4" DragDrop.AllowDrop="True">
            <nav:SelectableCategoryButton 
                Content="Favorites"
                Classes="special"
                Category="Favorites"
                IsSelected="{Binding IsFavoritesSelected}"
                Command="{Binding SelectCategoryCommand}"
                CommandParameter="Favorites"/>
                
            <nav:SelectableCategoryButton 
                Content="Joker"
                Category="Joker"
                IsSelected="{Binding IsJokerSelected}"
                Command="{Binding SelectCategoryCommand}"
                CommandParameter="Joker"/>
                
            <nav:SelectableCategoryButton 
                Content="Consumable"
                Category="Consumable"
                IsSelected="{Binding IsConsumableSelected}"
                Command="{Binding SelectCategoryCommand}"
                CommandParameter="Consumable"/>
                
            <nav:SelectableCategoryButton 
                Content="Skip Tags"
                Category="SkipTag"
                IsSelected="{Binding IsSkipTagSelected}"
                Command="{Binding SelectCategoryCommand}"
                CommandParameter="SkipTag"/>
                
            <nav:SelectableCategoryButton 
                Content="Boss Blind"
                Category="Boss"
                IsSelected="{Binding IsBossSelected}"
                Command="{Binding SelectCategoryCommand}"
                CommandParameter="Boss"/>
                
            <nav:SelectableCategoryButton 
                Content="Voucher"
                Category="Voucher"
                IsSelected="{Binding IsVoucherSelected}"
                Command="{Binding SelectCategoryCommand}"
                CommandParameter="Voucher"/>
                
            <nav:SelectableCategoryButton 
                Content="Standard Card"
                Category="StandardCard"
                IsSelected="{Binding IsStandardCardSelected}"
                Command="{Binding SelectCategoryCommand}"
                CommandParameter="StandardCard"/>
        </StackPanel>

        <!-- Favorites drop overlay (keep as-is) -->
        <Border Grid.Row="4"
                Name="FavoritesDropOverlay"
                ... />
    </Grid>
</UserControl>
```

#### 2. Update FilterTabViewModelBase
Add selection properties and command:

```csharp
public abstract class FilterTabViewModelBase : ViewModelBase
{
    private string _selectedCategory = "Favorites";
    
    // Selection properties for each category
    public bool IsFavoritesSelected => SelectedCategory == "Favorites";
    public bool IsJokerSelected => SelectedCategory == "Joker";
    public bool IsConsumableSelected => SelectedCategory == "Consumable";
    public bool IsSkipTagSelected => SelectedCategory == "SkipTag";
    public bool IsBossSelected => SelectedCategory == "Boss";
    public bool IsVoucherSelected => SelectedCategory == "Voucher";
    public bool IsStandardCardSelected => SelectedCategory == "StandardCard";
    
    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                // Notify all selection properties
                OnPropertyChanged(nameof(IsFavoritesSelected));
                OnPropertyChanged(nameof(IsJokerSelected));
                OnPropertyChanged(nameof(IsConsumableSelected));
                OnPropertyChanged(nameof(IsSkipTagSelected));
                OnPropertyChanged(nameof(IsBossSelected));
                OnPropertyChanged(nameof(IsVoucherSelected));
                OnPropertyChanged(nameof(IsStandardCardSelected));
                
                // Update the filtered items
                UpdateFilteredItems();
            }
        }
    }
    
    public ReactiveCommand<string, Unit> SelectCategoryCommand { get; }
    
    protected FilterTabViewModelBase()
    {
        SelectCategoryCommand = ReactiveCommand.Create<string>(category =>
        {
            SelectedCategory = category;
        });
    }
}
```

#### 3. Clean Up Code-Behind
Simplify FilterCategoryNav.axaml.cs:

```csharp
using Avalonia.Controls;

namespace BalatroSeedOracle.Components.Shared
{
    public partial class FilterCategoryNav : UserControl
    {
        public FilterCategoryNav()
        {
            InitializeComponent();
        }
        // No more arrow positioning logic needed!
    }
}
```

#### 4. Remove External Arrow Management
Delete all code related to:
- `Canvas.SetLeft()` / `Canvas.SetTop()` for arrow positioning
- `TriangleCanvas` and `CategoryArrow` elements
- Any arrow animation code in C#
- Position calculation logic

## Alternative: Quick Property Binding (if ViewModel refactor is too complex)

If adding individual selection properties is too complex, use a converter:

```csharp
public class CategorySelectionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString();
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if ((bool)value)
            return parameter?.ToString();
        return Binding.DoNothing;
    }
}
```

Then in XAML:
```xml
<nav:SelectableCategoryButton 
    Content="Joker"
    Category="Joker"
    IsSelected="{Binding SelectedCategory, 
                 Converter={StaticResource CategorySelectionConverter}, 
                 ConverterParameter=Joker}"
    Command="{Binding SelectCategoryCommand}"
    CommandParameter="Joker"/>
```

## Testing Requirements
1. Verify arrow appears on selected category
2. Confirm arrow animates (bounces horizontally)
3. Test category switching updates arrow position
4. Ensure no external positioning code remains
5. Verify drag-drop still works on categories

## Success Criteria
- Selection indicator is part of button template
- Zero C# code for arrow positioning
- Selection state managed through MVVM bindings
- Visual states handled in XAML only
- Code follows existing SelectableCategoryButton pattern

## Implementation Notes
- The SelectableCategoryButton already exists and works correctly
- Just need to USE it instead of reinventing the wheel
- This is a refactor, not new functionality
- Keep all existing features (drag-drop, favorites overlay, etc.)

## File Locations
- **Component to refactor**: `src/Components/Shared/FilterCategoryNav.axaml` and `.axaml.cs`
- **Existing button to use**: `src/Controls/Navigation/SelectableCategoryButton.cs` and `.axaml`
- **ViewModel base**: `src/ViewModels/FilterTabs/FilterTabViewModelBase.cs`

## Time Estimate
Should take 30-60 minutes to implement properly, saving future hours of frustration with external positioning logic.
