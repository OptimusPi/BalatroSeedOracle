using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using Avalonia.Platform;

namespace BalatroSeedOracle.ViewModels
{
    public class CreditsModalViewModel
    {
        public ObservableCollection<Credit> Credits { get; }

        public CreditsModalViewModel()
        {
            // credits.json is included as an <AvaloniaResource Include="Assets\\**" /> in the csproj,
            // so load it through the Avalonia asset loader, NOT raw File IO (which breaks after publish).
            try
            {
                var uri = new Uri("avares://BalatroSeedOracle/Assets/credits.json");
                if (AssetLoader.Exists(uri))
                {
                    using var stream = AssetLoader.Open(uri);
                    using var reader = new StreamReader(stream);
                    var json = reader.ReadToEnd();
                    var items = JsonSerializer.Deserialize<Credit[]>(json) ?? Array.Empty<Credit>();
                    Credits = new ObservableCollection<Credit>(items);
                }
                else
                {
                    Credits = new ObservableCollection<Credit>();
                }
            }
            catch
            {
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
