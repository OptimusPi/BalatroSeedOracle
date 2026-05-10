using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Theme;
using Microsoft.Extensions.DependencyInjection;

namespace BalatroSeedOracle.UI;

public class FilterSelectionComponent : ComponentBase
{
    private readonly IServiceProvider _services;
    private readonly Action _onClose;

    // MVU state
    public string? SelectedFilterName { get; set; }
    public List<string> FilterNames { get; set; } = new();
    public bool IsLoading { get; set; } = true;

    public FilterSelectionComponent(IServiceProvider services, Action onClose)
    {
        _services = services;
        _onClose = onClose;
        _ = LoadFiltersAsync();
    }

    private async System.Threading.Tasks.Task LoadFiltersAsync()
    {
        try
        {
            var filterService = _services.GetService<IFilterService>();
            if (filterService != null)
            {
                var filters = await filterService.GetFilterFilesAsync();
                FilterNames = filters.Select(f => System.IO.Path.GetFileNameWithoutExtension(f)).ToList();
            }
        }
        catch (Exception ex)
        {
            Helpers.DebugLogger.LogError("FilterSelection", $"Failed to load filters: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    protected override object Build() =>
        new Border()
            .Background(BalatroBrushes.SemiTransparentBlack)
            .Child(
                new Border()
                    .Background(BalatroBrushes.ModalBackground)
                    .CornerRadius(12)
                    .Margin(60)
                    .MaxWidth(700)
                    .MaxHeight(600)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Child(
                        new Grid()
                            .Rows("Auto, *, Auto")
                            .Children(
                                // Header
                                new Border()
                                    .Row(0)
                                    .Padding(16, 12)
                                    .Child(
                                        new Grid()
                                            .Cols("*, Auto")
                                            .Children(
                                                new TextBlock()
                                                    .Text("SELECT FILTER")
                                                    .FontFamily(BalatroFonts.Primary)
                                                    .FontSize(BalatroFonts.SizeTitle)
                                                    .Foreground(BalatroBrushes.GoldText),
                                                new Button()
                                                    .Col(1)
                                                    .Content("X")
                                                    .FontFamily(BalatroFonts.Primary)
                                                    .Background(BalatroBrushes.RedHover)
                                                    .Foreground(BalatroBrushes.White)
                                                    .OnClick((_, _) => _onClose())
                                            )
                                    ),

                                // Filter list
                                new Border()
                                    .Row(1)
                                    .Background(BalatroBrushes.ModalInnerPanel)
                                    .Padding(8)
                                    .Child(
                                        IsLoading
                                            ? (Control)new TextBlock()
                                                .Text("Loading filters...")
                                                .FontFamily(BalatroFonts.Primary)
                                                .Foreground(BalatroBrushes.LightGrey)
                                                .HorizontalAlignment(HorizontalAlignment.Center)
                                                .VerticalAlignment(VerticalAlignment.Center)
                                            : new ScrollViewer()
                                                .Child(
                                                    new StackPanel()
                                                        .Spacing(4)
                                                        .Children(
                                                            FilterNames.Select(FilterRow).ToArray()
                                                        )
                                                )
                                    ),

                                // Footer
                                new Border()
                                    .Row(2)
                                    .Padding(16, 12)
                                    .Child(
                                        new StackPanel()
                                            .Orientation(Orientation.Horizontal)
                                            .HorizontalAlignment(HorizontalAlignment.Right)
                                            .Spacing(8)
                                            .Children(
                                                new Button()
                                                    .Content("SELECT")
                                                    .FontFamily(BalatroFonts.Primary)
                                                    .Background(BalatroBrushes.Green)
                                                    .Foreground(BalatroBrushes.White)
                                                    .IsEnabled(SelectedFilterName != null)
                                                    .OnClick((_, _) => OnSelect()),
                                                new Button()
                                                    .Content("CANCEL")
                                                    .FontFamily(BalatroFonts.Primary)
                                                    .Background(BalatroBrushes.Red)
                                                    .Foreground(BalatroBrushes.White)
                                                    .OnClick((_, _) => _onClose())
                                            )
                                    )
                            )
                    )
            );

    private Button FilterRow(string filterName) =>
        new Button()
            .Content(filterName)
            .FontFamily(BalatroFonts.Primary)
            .FontSize(BalatroFonts.SizeNormal)
            .Foreground(BalatroBrushes.White)
            .Background(filterName == SelectedFilterName ? BalatroBrushes.Blue : BalatroBrushes.DarkBackground)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .HorizontalContentAlignment(HorizontalAlignment.Left)
            .OnClick((_, _) =>
            {
                SelectedFilterName = filterName;
                StateHasChanged();
            });

    private void OnSelect()
    {
        if (SelectedFilterName != null)
            _onClose();
    }
}
