using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Theme;
using Microsoft.Extensions.DependencyInjection;

namespace BalatroSeedOracle.UI;

public class MainMenuComponent : ComponentBase
{
    private readonly IServiceProvider _services;

    // MVU state
    public bool IsSearchModalOpen { get; set; }
    public bool IsFiltersModalOpen { get; set; }
    public bool IsCreditsModalOpen { get; set; }

    public MainMenuComponent(IServiceProvider services)
    {
        _services = services;
    }

    protected override object Build() =>
        new Grid()
            .Rows("*, Auto")
            .Children(

                // Main content area
                new Panel()
                    .Row(0)
                    .Children(
                        // Background (placeholder for shader background)
                        new Border()
                            .Background(BalatroBrushes.DarkBackground),

                        // Center menu buttons
                        new StackPanel()
                            .VerticalAlignment(VerticalAlignment.Center)
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .Spacing(12)
                            .Children(
                                // Title
                                new TextBlock()
                                    .Text("BALATRO SEED ORACLE")
                                    .FontFamily(BalatroFonts.Primary)
                                    .FontSize(BalatroFonts.SizeHuge)
                                    .Foreground(BalatroBrushes.GoldText)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .Margin(0, 0, 0, 40),

                                // Search button
                                MenuButton("SEARCH SEEDS", BalatroBrushes.Red, OnSearchClick),

                                // Filters button
                                MenuButton("FILTERS", BalatroBrushes.Blue, OnFiltersClick),

                                // Credits button
                                MenuButton("CREDITS", BalatroBrushes.Green, OnCreditsClick)
                            ),

                        // Search modal overlay
                        IsSearchModalOpen
                            ? new SearchModalComponent(_services, OnCloseSearchModal)
                            : null!,

                        // Filters modal overlay
                        IsFiltersModalOpen
                            ? new FilterSelectionComponent(_services, OnCloseFiltersModal)
                            : null!
                    ),

                // Bottom status bar
                new Border()
                    .Row(1)
                    .Background(BalatroBrushes.ModalBackground)
                    .Padding(12, 6)
                    .Child(
                        new TextBlock()
                            .Text("pifreak's Balatro Seed Oracle")
                            .FontFamily(BalatroFonts.Primary)
                            .FontSize(BalatroFonts.SizeSmall)
                            .Foreground(BalatroBrushes.LightGrey)
                    )
            );

    private static Button MenuButton(string text, ISolidColorBrush color, Action onClick) =>
        new Button()
            .Content(text)
            .FontFamily(BalatroFonts.Primary)
            .FontSize(BalatroFonts.SizeLarge)
            .Foreground(BalatroBrushes.White)
            .Background(color)
            .MinWidth(300)
            .MinHeight(50)
            .HorizontalContentAlignment(HorizontalAlignment.Center)
            .VerticalContentAlignment(VerticalAlignment.Center)
            .OnClick((_, _) => onClick());

    private void OnSearchClick()
    {
        IsSearchModalOpen = true;
        StateHasChanged();
    }

    private void OnFiltersClick()
    {
        IsFiltersModalOpen = true;
        StateHasChanged();
    }

    private void OnCreditsClick()
    {
        // TODO: Credits modal
    }

    private void OnCloseSearchModal()
    {
        IsSearchModalOpen = false;
        StateHasChanged();
    }

    private void OnCloseFiltersModal()
    {
        IsFiltersModalOpen = false;
        StateHasChanged();
    }
}
