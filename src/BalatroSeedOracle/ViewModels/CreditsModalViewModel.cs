using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using Avalonia.Platform;
using BalatroSeedOracle.Helpers;

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

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var items =
                        JsonSerializer.Deserialize<Credit[]>(json, options)
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

    public class Credit
    {
        public string? name { get; set; }
        public string? role { get; set; }
        public string? note { get; set; }
        public string? link { get; set; }

        // Properties for binding (uppercase)
        public string? Name => name;
        public string? Role => role;
        public string? Note => note;
        public string? Link => link;
        public bool HasLink => !string.IsNullOrWhiteSpace(link);
    }
}
