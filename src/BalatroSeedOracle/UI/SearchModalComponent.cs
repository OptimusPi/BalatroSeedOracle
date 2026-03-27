using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Theme;
using Microsoft.Extensions.DependencyInjection;

namespace BalatroSeedOracle.UI;

public class SearchModalComponent : ComponentBase
{
    private readonly IServiceProvider _services;
    private readonly Action _onClose;

    // MVU state
    public string ActiveTab { get; set; } = "settings";
    public bool IsSearching { get; set; }
    public string FilterName { get; set; } = "(no filter selected)";
    public int ResultCount { get; set; }
    public string ConsoleOutput { get; set; } = "";
    public long SeedsSearched { get; set; }

    public SearchModalComponent(IServiceProvider services, Action onClose)
    {
        _services = services;
        _onClose = onClose;
    }

    protected override object Build() =>
        // Full-screen overlay
        new Border()
            .Background(BalatroBrushes.SemiTransparentBlack)
            .Child(
                // Modal container
                new Border()
                    .Background(BalatroBrushes.ModalBackground)
                    .CornerRadius(12)
                    .Margin(40)
                    .MaxWidth(900)
                    .MaxHeight(700)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Child(
                        new Grid()
                            .Rows("Auto, Auto, *, Auto")
                            .Children(
                                // Header
                                ModalHeader(),
                                // Tab bar
                                TabBar(),
                                // Tab content
                                TabContent(),
                                // Footer
                                ModalFooter()
                            )
                    )
            );

    private Border ModalHeader() =>
        new Border()
            .Row(0)
            .Background(BalatroBrushes.ModalBackground)
            .Padding(16, 12)
            .Child(
                new Grid()
                    .Cols("*, Auto")
                    .Children(
                        new TextBlock()
                            .Text("SEARCH SEEDS")
                            .FontFamily(BalatroFonts.Primary)
                            .FontSize(BalatroFonts.SizeTitle)
                            .Foreground(BalatroBrushes.GoldText)
                            .VerticalAlignment(VerticalAlignment.Center),
                        new Button()
                            .Col(1)
                            .Content("X")
                            .FontFamily(BalatroFonts.Primary)
                            .Background(BalatroBrushes.RedHover)
                            .Foreground(BalatroBrushes.White)
                            .OnClick((_, _) => _onClose())
                    )
            );

    private Border TabBar() =>
        new Border()
            .Row(1)
            .Background(BalatroBrushes.MediumGrey)
            .Padding(8, 4)
            .Child(
                new StackPanel()
                    .Orientation(Orientation.Horizontal)
                    .Spacing(4)
                    .Children(
                        TabButton("SETTINGS", "settings"),
                        TabButton("SEARCH", "search"),
                        TabButton("RESULTS", "results")
                    )
            );

    private Button TabButton(string label, string tab)
    {
        var isActive = ActiveTab == tab;
        return new Button()
            .Content(label)
            .FontFamily(BalatroFonts.Primary)
            .FontSize(BalatroFonts.SizeNormal)
            .Background(isActive ? BalatroBrushes.Blue : BalatroBrushes.DarkBackground)
            .Foreground(isActive ? BalatroBrushes.White : BalatroBrushes.LightGrey)
            .MinWidth(100)
            .OnClick((_, _) =>
            {
                ActiveTab = tab;
                StateHasChanged();
            });
    }

    private Control TabContent() => ActiveTab switch
    {
        "settings" => SettingsTab(),
        "search" => SearchTab(),
        "results" => ResultsTab(),
        _ => SettingsTab()
    };

    private Border SettingsTab() =>
        new Border()
            .Row(2)
            .Background(BalatroBrushes.ModalInnerPanel)
            .Padding(16)
            .Child(
                new StackPanel()
                    .Spacing(12)
                    .Children(
                        Label("Filter"),
                        new Border()
                            .Background(BalatroBrushes.DarkBackground)
                            .CornerRadius(6)
                            .Padding(12, 8)
                            .Child(
                                new TextBlock()
                                    .Text(() => FilterName)
                                    .FontFamily(BalatroFonts.Primary)
                                    .FontSize(BalatroFonts.SizeNormal)
                                    .Foreground(BalatroBrushes.White)
                            ),
                        new Button()
                            .Content("SELECT FILTER")
                            .FontFamily(BalatroFonts.Primary)
                            .Background(BalatroBrushes.Blue)
                            .Foreground(BalatroBrushes.White),

                        Label("Search Range"),
                        new StackPanel()
                            .Orientation(Orientation.Horizontal)
                            .Spacing(8)
                            .Children(
                                new TextBox()
                                    .Watermark("Start seed")
                                    .Width(200)
                                    .FontFamily(BalatroFonts.Primary),
                                new TextBlock()
                                    .Text("to")
                                    .FontFamily(BalatroFonts.Primary)
                                    .Foreground(BalatroBrushes.LightGrey)
                                    .VerticalAlignment(VerticalAlignment.Center),
                                new TextBox()
                                    .Watermark("End seed")
                                    .Width(200)
                                    .FontFamily(BalatroFonts.Primary)
                            ),

                        Label("Thread Count"),
                        new TextBox()
                            .Text(Environment.ProcessorCount.ToString())
                            .Width(100)
                            .FontFamily(BalatroFonts.Primary)
                    )
            );

    private Border SearchTab() =>
        new Border()
            .Row(2)
            .Background(BalatroBrushes.ModalInnerPanel)
            .Padding(16)
            .Child(
                new StackPanel()
                    .Spacing(12)
                    .Children(
                        new TextBlock()
                            .Text(() => $"Seeds searched: {SeedsSearched:N0}")
                            .FontFamily(BalatroFonts.Primary)
                            .FontSize(BalatroFonts.SizeLarge)
                            .Foreground(BalatroBrushes.GoldText),
                        new TextBlock()
                            .Text(() => $"Results found: {ResultCount}")
                            .FontFamily(BalatroFonts.Primary)
                            .FontSize(BalatroFonts.SizeLarge)
                            .Foreground(BalatroBrushes.GreenText),

                        // Console output
                        new Border()
                            .Background(BalatroBrushes.DarkBackground)
                            .CornerRadius(6)
                            .Padding(8)
                            .MinHeight(200)
                            .Child(
                                new ScrollViewer()
                                    .Child(
                                        new TextBlock()
                                            .Text(() => ConsoleOutput)
                                            .FontFamily(BalatroFonts.Primary)
                                            .FontSize(BalatroFonts.SizeSmall)
                                            .Foreground(BalatroBrushes.GreenText)
                                            .TextWrapping(TextWrapping.Wrap)
                                    )
                            )
                    )
            );

    private Border ResultsTab() =>
        new Border()
            .Row(2)
            .Background(BalatroBrushes.ModalInnerPanel)
            .Padding(16)
            .Child(
                new StackPanel()
                    .Spacing(12)
                    .Children(
                        new TextBlock()
                            .Text(() => $"{ResultCount} results")
                            .FontFamily(BalatroFonts.Primary)
                            .FontSize(BalatroFonts.SizeLarge)
                            .Foreground(BalatroBrushes.GoldText),
                        new TextBlock()
                            .Text("Results will appear here as seeds are found.")
                            .FontFamily(BalatroFonts.Primary)
                            .FontSize(BalatroFonts.SizeNormal)
                            .Foreground(BalatroBrushes.LightGrey)
                    )
            );

    private Border ModalFooter() =>
        new Border()
            .Row(3)
            .Background(BalatroBrushes.ModalBackground)
            .Padding(16, 12)
            .Child(
                new StackPanel()
                    .Orientation(Orientation.Horizontal)
                    .HorizontalAlignment(HorizontalAlignment.Right)
                    .Spacing(8)
                    .Children(
                        new Button()
                            .Content(() => IsSearching ? "STOP" : "START SEARCH")
                            .FontFamily(BalatroFonts.Primary)
                            .Background(() => IsSearching ? BalatroBrushes.Red : BalatroBrushes.Green)
                            .Foreground(BalatroBrushes.White)
                            .MinWidth(160)
                            .OnClick((_, _) => OnToggleSearch())
                    )
            );

    private void OnToggleSearch()
    {
        IsSearching = !IsSearching;
        if (IsSearching)
            ActiveTab = "search";
        StateHasChanged();
    }

    private static TextBlock Label(string text) =>
        new TextBlock()
            .Text(text)
            .FontFamily(BalatroFonts.Primary)
            .FontSize(BalatroFonts.SizeNormal)
            .Foreground(BalatroBrushes.GoldText)
            .Margin(0, 8, 0, 4);
}
