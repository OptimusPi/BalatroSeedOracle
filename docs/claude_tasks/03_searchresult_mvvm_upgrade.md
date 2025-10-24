# Task: Modernize `SearchResultViewModel` to CommunityToolkit MVVM

## Problem
`src/ViewModels/SearchResultViewModel.cs` still implements `INotifyPropertyChanged` by hand and wires commands manually. The rest of the project now depends on CommunityToolkit.Mvvm source generators, so this class is an outlier and risks diverging behavior.

## Goals
- Convert the class to `public partial class SearchResultViewModel : ObservableObject`.
- Replace manual properties with `[ObservableProperty]` backing fields.
- Use `[RelayCommand]` to expose the existing `CopyCommand` and `ViewCommand` behaviors (names must stay identical for bindings).
- Keep `ScoreFormatted` and `ScoreTooltip` updating when `Score` changes.

## Non-goals
- Changing how search results are displayed.
- Altering command behavior (still copy to clipboard and log view requests).

## Acceptance tests
1. `dotnet build src/BalatroSeedOracle.csproj` passes with no new warnings.
2. The Search results grid still shows formatted scores and tooltips.
3. Clicking the Copy button copies the seed to the clipboard in the running app.
