using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using Avalonia.Platform;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Json;

namespace BalatroSeedOracle.ViewModels
{
    public class CreditsModalViewModel
    {
        public ObservableCollection<Credit> Credits { get; }

        public CreditsModalViewModel()
        {
            // credits.json is included as an <AvaloniaResource Include="Assets\\**" /> in the csproj,
            // so load it through the Avalonia asset loader, NOT raw File IO (which breaks after publish).
            // This works cross-platform including browser!
            try
            {
                var uri = new Uri("avares://BalatroSeedOracle/Assets/credits.json");

                DebugLogger.Log("CreditsModalViewModel", $"Loading credits from: {uri}");

                if (AssetLoader.Exists(uri))
                {
                    DebugLogger.Log("CreditsModalViewModel", "Asset exists, loading...");
                    using var stream = AssetLoader.Open(uri);
                    using var reader = new StreamReader(stream);
                    var json = reader.ReadToEnd();

                    DebugLogger.Log("CreditsModalViewModel", $"JSON loaded, length: {json.Length}");

                    // Source-gen deserialization (AOT-safe; reflection JSON is disabled app-wide).
                    var items =
                        JsonSerializer.Deserialize(json, BsoJsonSerializerContext.Default.CreditArray)
                        ?? Array.Empty<Credit>();

                    DebugLogger.Log(
                        "CreditsModalViewModel",
                        $"Deserialized {items.Length} credits"
                    );

                    Credits = new ObservableCollection<Credit>(items);
                }
                else
                {
                    DebugLogger.LogError("CreditsModalViewModel", $"Asset does not exist: {uri}");
                    Credits = new ObservableCollection<Credit>();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "CreditsModalViewModel",
                    $"Failed to load credits: {ex.Message}\n{ex.StackTrace}"
                );
                Credits = new ObservableCollection<Credit>();
            }
        }
    }
}
