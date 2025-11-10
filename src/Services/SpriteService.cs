using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BalatroSeedOracle.Constants;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Services
{
    public class SpriteService
    {
        private static SpriteService _instance = null!;
        public static SpriteService Instance => _instance ??= new SpriteService();

        private bool _spritesPreloaded = false;
        private readonly Dictionary<string, IImage> _preloadedSprites = new();

        /// <summary>
        /// Normalizes a sprite name for consistent lookup: trims whitespace, removes spaces and underscores, converts to lowercase.
        /// This ensures "Red Deck", "red_deck", "RedDeck", and "reddeck" all map to the same key.
        /// </summary>
        private static string NormalizeSpriteName(string name)
        {
            return name.Trim()
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("_", string.Empty, StringComparison.Ordinal)
                .ToLowerInvariant();
        }

        private Dictionary<string, SpritePosition> jokerPositions = null!;
        private Dictionary<string, SpritePosition> tagPositions = null!;
        private Dictionary<string, SpritePosition> tarotPositions = null!;
        private Dictionary<string, SpritePosition> spectralPositions = null!;
        private Dictionary<string, SpritePosition> planetPositions = null!;
        private Dictionary<string, SpritePosition> voucherPositions = null!;

        private Dictionary<string, SpritePosition> deckPositions = null!;
        private Dictionary<string, SpritePosition> stakePositions = null!;
        private Dictionary<string, SpritePosition> enhancementPositions = null!;
        private Dictionary<string, SpritePosition> sealPositions = null!;
        private Dictionary<string, SpritePosition> specialPositions = null!;
        private Dictionary<string, Dictionary<string, SpritePosition>> playingCardPositions = null!;
        private Dictionary<string, SpritePosition> bossPositions = null!;
        private Dictionary<string, SpritePosition> blindPositions = null!;
        private Dictionary<string, SpritePosition> stickerPositions = null!;
        private Dictionary<string, SpritePosition> boosterPositions = null!;
        private Bitmap? jokerSheet;
        private Bitmap? tagSheet;
        private Bitmap? tarotSheet;
        private Bitmap? spectralSheet;
        private Bitmap? voucherSheet;

        private Bitmap? deckSheet;
        private Bitmap? stakeSheet;
        private Bitmap? enhancersSheet;
        private Bitmap? playingCardsSheet;
        private Bitmap? bossSheet;
        private Bitmap? stickersSheet;
        private Bitmap? boosterSheet;

        private SpriteService()
        {
            LoadAssets();
        }

        public async Task PreloadAllSpritesAsync(
            IProgress<(string category, int current, int total)>? progress = null
        )
        {
            if (_spritesPreloaded)
            {
                DebugLogger.Log("SpriteService", "Sprites already preloaded, skipping...");
                return;
            }

            DebugLogger.LogImportant("SpriteService", "Starting sprite pre-load...");
            var startTime = DateTime.Now;

            try
            {
                await Task.Run(() =>
                {
                    PreloadJokers(progress);
                    PreloadTags(progress);
                    PreloadTarots(progress);
                    PreloadSpectrals(progress);
                    PreloadPlanets(progress);
                    PreloadVouchers(progress);
                    PreloadDecks(progress);
                    PreloadStakes(progress);
                    PreloadBosses(progress);
                    PreloadBlinds(progress);
                    PreloadStickers(progress);
                    PreloadBoosters(progress);
                    PreloadEditions(progress);
                    PreloadEnhancements(progress);
                    PreloadSeals(progress);
                });

                _spritesPreloaded = true;
                var elapsed = DateTime.Now - startTime;
                DebugLogger.LogImportant(
                    "SpriteService",
                    $"Sprite pre-load complete in {elapsed.TotalSeconds:F2} seconds! Loaded {_preloadedSprites.Count} sprites."
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SpriteService",
                    $"Error during sprite pre-load: {ex.Message}"
                );
            }
        }

        private void PreloadJokers(IProgress<(string category, int current, int total)>? progress)
        {
            if (jokerPositions == null || jokerSheet == null)
                return;

            var jokers = jokerPositions.Keys.ToList();
            for (int i = 0; i < jokers.Count; i++)
            {
                try
                {
                    var key = $"joker_{jokers[i]}";
                    if (!_preloadedSprites.ContainsKey(key))
                    {
                        var image = GetJokerImage(jokers[i]);
                        if (image != null)
                        {
                            _preloadedSprites[key] = image;
                        }
                    }
                    progress?.Report(("Jokers", i + 1, jokers.Count));
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "SpriteService",
                        $"Failed to preload joker {jokers[i]}: {ex.Message}"
                    );
                }
            }
        }

        private void PreloadTags(IProgress<(string category, int current, int total)>? progress)
        {
            if (tagPositions == null || tagSheet == null)
                return;

            var tags = tagPositions.Keys.ToList();
            for (int i = 0; i < tags.Count; i++)
            {
                try
                {
                    var key = $"tag_{tags[i]}";
                    if (!_preloadedSprites.ContainsKey(key))
                    {
                        var image = GetTagImage(tags[i]);
                        if (image != null)
                        {
                            _preloadedSprites[key] = image;
                        }
                    }
                    progress?.Report(("Tags", i + 1, tags.Count));
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "SpriteService",
                        $"Failed to preload tag {tags[i]}: {ex.Message}"
                    );
                }
            }
        }

        private void PreloadTarots(IProgress<(string category, int current, int total)>? progress)
        {
            if (tarotPositions == null || tarotSheet == null)
                return;

            var tarots = tarotPositions.Keys.ToList();
            for (int i = 0; i < tarots.Count; i++)
            {
                try
                {
                    var key = $"tarot_{tarots[i]}";
                    if (!_preloadedSprites.ContainsKey(key))
                    {
                        var image = GetTarotImage(tarots[i]);
                        if (image != null)
                        {
                            _preloadedSprites[key] = image;
                        }
                    }
                    progress?.Report(("Tarots", i + 1, tarots.Count));
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "SpriteService",
                        $"Failed to preload tarot {tarots[i]}: {ex.Message}"
                    );
                }
            }
        }

        private void PreloadSpectrals(
            IProgress<(string category, int current, int total)>? progress
        )
        {
            if (spectralPositions == null || spectralSheet == null)
                return;

            var spectrals = spectralPositions.Keys.ToList();
            for (int i = 0; i < spectrals.Count; i++)
            {
                try
                {
                    var key = $"spectral_{spectrals[i]}";
                    if (!_preloadedSprites.ContainsKey(key))
                    {
                        var image = GetSpectralImage(spectrals[i]);
                        if (image != null)
                        {
                            _preloadedSprites[key] = image;
                        }
                    }
                    progress?.Report(("Spectrals", i + 1, spectrals.Count));
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "SpriteService",
                        $"Failed to preload spectral {spectrals[i]}: {ex.Message}"
                    );
                }
            }
        }

        private void PreloadPlanets(IProgress<(string category, int current, int total)>? progress)
        {
            if (planetPositions == null || tarotSheet == null)
                return;

            var planets = planetPositions.Keys.ToList();
            for (int i = 0; i < planets.Count; i++)
            {
                try
                {
                    var key = $"planet_{planets[i]}";
                    if (!_preloadedSprites.ContainsKey(key))
                    {
                        var image = GetPlanetCardImage(planets[i]);
                        if (image != null)
                        {
                            _preloadedSprites[key] = image;
                        }
                    }
                    progress?.Report(("Planets", i + 1, planets.Count));
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "SpriteService",
                        $"Failed to preload planet {planets[i]}: {ex.Message}"
                    );
                }
            }
        }

        private void PreloadVouchers(IProgress<(string category, int current, int total)>? progress)
        {
            if (voucherPositions == null || voucherSheet == null)
                return;

            var vouchers = voucherPositions.Keys.ToList();
            for (int i = 0; i < vouchers.Count; i++)
            {
                try
                {
                    var key = $"voucher_{vouchers[i]}";
                    if (!_preloadedSprites.ContainsKey(key))
                    {
                        var image = GetVoucherImage(vouchers[i]);
                        if (image != null)
                        {
                            _preloadedSprites[key] = image;
                        }
                    }
                    progress?.Report(("Vouchers", i + 1, vouchers.Count));
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "SpriteService",
                        $"Failed to preload voucher {vouchers[i]}: {ex.Message}"
                    );
                }
            }
        }

        private void PreloadDecks(IProgress<(string category, int current, int total)>? progress)
        {
            if (deckPositions == null || deckSheet == null)
                return;

            var decks = deckPositions.Keys.ToList();
            for (int i = 0; i < decks.Count; i++)
            {
                try
                {
                    var key = $"deck_{decks[i]}";
                    if (!_preloadedSprites.ContainsKey(key))
                    {
                        var image = GetDeckImage(decks[i]);
                        if (image != null)
                        {
                            _preloadedSprites[key] = image;
                        }
                    }
                    progress?.Report(("Decks", i + 1, decks.Count));
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "SpriteService",
                        $"Failed to preload deck {decks[i]}: {ex.Message}"
                    );
                }
            }
        }

        private void PreloadStakes(IProgress<(string category, int current, int total)>? progress)
        {
            if (stakePositions == null || stakeSheet == null)
                return;

            var stakes = stakePositions.Keys.ToList();
            for (int i = 0; i < stakes.Count; i++)
            {
                try
                {
                    var key = $"stake_{stakes[i]}";
                    if (!_preloadedSprites.ContainsKey(key))
                    {
                        var image = GetStakeImage(stakes[i]);
                        if (image != null)
                        {
                            _preloadedSprites[key] = image;
                        }
                    }
                    progress?.Report(("Stakes", i + 1, stakes.Count));
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "SpriteService",
                        $"Failed to preload stake {stakes[i]}: {ex.Message}"
                    );
                }
            }
        }

        private void PreloadBosses(IProgress<(string category, int current, int total)>? progress)
        {
            if (bossPositions == null || bossSheet == null)
                return;

            var bosses = bossPositions.Keys.ToList();
            for (int i = 0; i < bosses.Count; i++)
            {
                try
                {
                    var key = $"boss_{bosses[i]}";
                    if (!_preloadedSprites.ContainsKey(key))
                    {
                        var image = GetBossImage(bosses[i]);
                        if (image != null)
                        {
                            _preloadedSprites[key] = image;
                        }
                    }
                    progress?.Report(("Bosses", i + 1, bosses.Count));
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "SpriteService",
                        $"Failed to preload boss {bosses[i]}: {ex.Message}"
                    );
                }
            }
        }

        private void PreloadBlinds(IProgress<(string category, int current, int total)>? progress)
        {
            if (blindPositions == null || bossSheet == null)
                return;

            var blinds = blindPositions.Keys.ToList();
            for (int i = 0; i < blinds.Count; i++)
            {
                try
                {
                    var key = $"blind_{blinds[i]}";
                    if (!_preloadedSprites.ContainsKey(key))
                    {
                        var image = GetBlindImage(blinds[i]);
                        if (image != null)
                        {
                            _preloadedSprites[key] = image;
                        }
                    }
                    progress?.Report(("Blinds", i + 1, blinds.Count));
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "SpriteService",
                        $"Failed to preload blind {blinds[i]}: {ex.Message}"
                    );
                }
            }
        }

        private void PreloadStickers(IProgress<(string category, int current, int total)>? progress)
        {
            if (stickerPositions == null || stickersSheet == null)
                return;

            var stickers = stickerPositions.Keys.ToList();
            for (int i = 0; i < stickers.Count; i++)
            {
                try
                {
                    var key = $"sticker_{stickers[i]}";
                    if (!_preloadedSprites.ContainsKey(key))
                    {
                        var image = GetStickerImage(stickers[i]);
                        if (image != null)
                        {
                            _preloadedSprites[key] = image;
                        }
                    }
                    progress?.Report(("Stickers", i + 1, stickers.Count));
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "SpriteService",
                        $"Failed to preload sticker {stickers[i]}: {ex.Message}"
                    );
                }
            }
        }

        private void PreloadBoosters(IProgress<(string category, int current, int total)>? progress)
        {
            if (boosterPositions == null || boosterSheet == null)
                return;

            var boosters = boosterPositions.Keys.ToList();
            for (int i = 0; i < boosters.Count; i++)
            {
                try
                {
                    var key = $"booster_{boosters[i]}";
                    if (!_preloadedSprites.ContainsKey(key))
                    {
                        var image = GetBoosterImage(boosters[i]);
                        if (image != null)
                        {
                            _preloadedSprites[key] = image;
                        }
                    }
                    progress?.Report(("Boosters", i + 1, boosters.Count));
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "SpriteService",
                        $"Failed to preload booster {boosters[i]}: {ex.Message}"
                    );
                }
            }
        }

        private void PreloadEditions(IProgress<(string category, int current, int total)>? progress)
        {
            var editions = new[] { "none", "foil", "holographic", "polychrome", "negative" };
            for (int i = 0; i < editions.Length; i++)
            {
                try
                {
                    var key = $"edition_{editions[i]}";
                    if (!_preloadedSprites.ContainsKey(key))
                    {
                        var image = GetEditionImage(editions[i]);
                        if (image != null)
                        {
                            _preloadedSprites[key] = image;
                        }
                    }
                    progress?.Report(("Editions", i + 1, editions.Length));
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "SpriteService",
                        $"Failed to preload edition {editions[i]}: {ex.Message}"
                    );
                }
            }
        }

        private void PreloadEnhancements(
            IProgress<(string category, int current, int total)>? progress
        )
        {
            if (enhancementPositions == null || enhancersSheet == null)
                return;

            var enhancements = enhancementPositions.Keys.ToList();
            for (int i = 0; i < enhancements.Count; i++)
            {
                try
                {
                    var key = $"enhancement_{enhancements[i]}";
                    if (!_preloadedSprites.ContainsKey(key))
                    {
                        var image = GetEnhancementImage(enhancements[i]);
                        if (image != null)
                        {
                            _preloadedSprites[key] = image;
                        }
                    }
                    progress?.Report(("Enhancements", i + 1, enhancements.Count));
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "SpriteService",
                        $"Failed to preload enhancement {enhancements[i]}: {ex.Message}"
                    );
                }
            }
        }

        private void PreloadSeals(IProgress<(string category, int current, int total)>? progress)
        {
            if (sealPositions == null || enhancersSheet == null)
                return;

            var seals = sealPositions.Keys.ToList();
            for (int i = 0; i < seals.Count; i++)
            {
                try
                {
                    var key = $"seal_{seals[i]}";
                    if (!_preloadedSprites.ContainsKey(key))
                    {
                        var image = GetSealImage(seals[i]);
                        if (image != null)
                        {
                            _preloadedSprites[key] = image;
                        }
                    }
                    progress?.Report(("Seals", i + 1, seals.Count));
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "SpriteService",
                        $"Failed to preload seal {seals[i]}: {ex.Message}"
                    );
                }
            }
        }

        private void LoadAssets()
        {
            DebugLogger.Log("SpriteService", "Loading sprite assets...");
            try
            {
                // Load joker positions from json
                jokerPositions = LoadSpritePositions(
                    "avares://BalatroSeedOracle/Assets/Jokers/jokers.json"
                );

                // Load tag positions from json
                tagPositions = LoadSpritePositions(
                    "avares://BalatroSeedOracle/Assets/Tags/tags.json"
                );

                // Load tarot positions from json
                tarotPositions = LoadSpritePositions(
                    "avares://BalatroSeedOracle/Assets/Tarots/tarots.json"
                );

                // Load spectral positions from json
                // Load spectral positions from json (they're in the tarots sprite sheet)
                spectralPositions = LoadSpritePositions(
                    "avares://BalatroSeedOracle/Assets/Tarots/spectrals.json"
                );

                // Load planet positions from json (they're also in the tarots sprite sheet)
                // NOTE: Planet X, Ceres, and Eris positions are defined at (0,6), (1,6), (2,6)
                // but the Tarots.png sprite sheet needs to be expanded from 710x570 to 710x665 pixels
                // and have sprites added for these three planets at row 6.
                planetPositions = LoadSpritePositions(
                    "avares://BalatroSeedOracle/Assets/Tarots/planets.json"
                );

                // Load voucher positions from json
                voucherPositions = LoadSpritePositions(
                    "avares://BalatroSeedOracle/Assets/Vouchers/vouchers.json"
                );

                // Load stake positions from json
                stakePositions = LoadSpritePositions(
                    "avares://BalatroSeedOracle/Assets/Decks/stakes.json"
                );

                // Load booster pack positions from json
                boosterPositions = LoadSpritePositions(
                    "avares://BalatroSeedOracle/Assets/Other/Boosters.json"
                );

                // Load deck, enhancement, and seal positions from enhancers metadata
                var enhancersMetadata = LoadEnhancersMetadata(
                    "avares://BalatroSeedOracle/Assets/Decks/enhancers_metadata.json"
                );
                if (enhancersMetadata != null)
                {
                    deckPositions = enhancersMetadata.Decks;
                    enhancementPositions = enhancersMetadata.Enhancements;
                    sealPositions = enhancersMetadata.Seals;
                    specialPositions = enhancersMetadata.Special;
                }

                // Load playing card positions
                playingCardPositions = LoadPlayingCardMetadata(
                    "avares://BalatroSeedOracle/Assets/Decks/playing_cards_metadata.json"
                );

                // Load boss blind positions
                var bossMetadata = LoadBossMetadata(
                    "avares://BalatroSeedOracle/Assets/Bosses/blinds_metadata.json"
                );
                if (bossMetadata != null)
                {
                    blindPositions = bossMetadata.Blinds;
                    bossPositions = new Dictionary<string, SpritePosition>();
                    // Merge all boss types into one dictionary
                    foreach (var kvp in bossMetadata.Bosses)
                    {
                        bossPositions[kvp.Key.ToLowerInvariant()] = kvp.Value;
                    }

                    foreach (var kvp in bossMetadata.FinisherBosses)
                    {
                        bossPositions[kvp.Key.ToLowerInvariant()] = kvp.Value;
                    }

                    foreach (var kvp in bossMetadata.Special)
                    {
                        bossPositions[kvp.Key.ToLowerInvariant()] = kvp.Value;
                    }
                }

                // Load sticker positions
                var stickerMetadata = LoadStickersMetadata(
                    "avares://BalatroSeedOracle/Assets/Jokers/stickers_metadata.json"
                );
                if (stickerMetadata != null)
                {
                    stickerPositions = new Dictionary<string, SpritePosition>();
                    // Merge all sticker types into one dictionary
                    foreach (var kvp in stickerMetadata.JokerStickers)
                    {
                        stickerPositions[kvp.Key.ToLowerInvariant()] = kvp.Value;
                    }

                    foreach (var kvp in stickerMetadata.StakeStickers)
                    {
                        // Store stake stickers with lowercase keys
                        var lower = kvp.Key.ToLowerInvariant();
                        stickerPositions[lower] = kvp.Value;
                    }
                }

                // Load spritesheets
                jokerSheet = LoadBitmap("avares://BalatroSeedOracle/Assets/Jokers/Jokers.png");
                tagSheet = LoadBitmap("avares://BalatroSeedOracle/Assets/Tags/tags.png");
                tarotSheet = LoadBitmap("avares://BalatroSeedOracle/Assets/Tarots/Tarots.png");
                voucherSheet = LoadBitmap(
                    "avares://BalatroSeedOracle/Assets/Vouchers/Vouchers.png"
                );
                spectralSheet = tarotSheet;

                stakeSheet = LoadBitmap(
                    "avares://BalatroSeedOracle/Assets/Decks/balatro-stake-chips.png"
                );
                enhancersSheet = LoadBitmap(
                    "avares://BalatroSeedOracle/Assets/Decks/Enhancers.png"
                );
                deckSheet = enhancersSheet;
                playingCardsSheet = LoadBitmap(
                    "avares://BalatroSeedOracle/Assets/Decks/8BitDeck.png"
                );
                bossSheet = LoadBitmap("avares://BalatroSeedOracle/Assets/Bosses/BlindChips.png");
                stickersSheet = LoadBitmap("avares://BalatroSeedOracle/Assets/Jokers/stickers.png");
                boosterSheet = LoadBitmap("avares://BalatroSeedOracle/Assets/Other/boosters.png");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SpriteService", $"Error loading sprite assets: {ex.Message}");
                // Initialize empty dictionaries to prevent null reference errors
                jokerPositions ??= new Dictionary<string, SpritePosition>();
                tagPositions ??= new Dictionary<string, SpritePosition>();
                tarotPositions ??= new Dictionary<string, SpritePosition>();
                spectralPositions ??= new Dictionary<string, SpritePosition>();
                planetPositions ??= new Dictionary<string, SpritePosition>();
                voucherPositions ??= new Dictionary<string, SpritePosition>();

                deckPositions ??= new Dictionary<string, SpritePosition>();
                enhancementPositions ??= new Dictionary<string, SpritePosition>();
                sealPositions ??= new Dictionary<string, SpritePosition>();
                specialPositions ??= new Dictionary<string, SpritePosition>();
                playingCardPositions ??=
                    new Dictionary<string, Dictionary<string, SpritePosition>>();
                bossPositions ??= new Dictionary<string, SpritePosition>();
                blindPositions ??= new Dictionary<string, SpritePosition>();
                stickerPositions ??= new Dictionary<string, SpritePosition>();
                boosterPositions ??= new Dictionary<string, SpritePosition>();

                // Bitmaps will remain null if loading failed
            }
        }

        private static Dictionary<string, SpritePosition> LoadSpritePositions(string jsonUri)
        {
            try
            {
                using var stream = TryOpenAssetStream(jsonUri);
                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();

                var positions = new Dictionary<string, SpritePosition>();

                // If this is a simple array of positions, use the original deserializer
                var trimmed = json.TrimStart();
                if (trimmed.StartsWith("["))
                {
                    var positionsList = JsonSerializer.Deserialize<List<SpritePosition>>(json);
                    foreach (var pos in positionsList ?? new List<SpritePosition>())
                    {
                        if (pos?.Name != null)
                        {
                            positions[pos.Name.ToLowerInvariant()] = pos;
                        }
                    }
                    return positions;
                }

                // Otherwise, support metadata-style JSON with a "sprites" object
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (!root.TryGetProperty("sprites", out var spritesObj))
                {
                    // No sprites object; return empty
                    return positions;
                }

                // Determine the category from the file name so we pick the correct branch
                var fileName = Path.GetFileName(jsonUri).ToLowerInvariant();
                var categories = new List<string>();
                if (fileName.Contains("decks"))
                    categories.Add("decks");
                else if (fileName.Contains("stakes"))
                    categories.Add("stakes");
                else
                {
                    // Fallback: include all categories present
                    foreach (var prop in spritesObj.EnumerateObject())
                    {
                        categories.Add(prop.Name);
                    }
                }

                foreach (var cat in categories)
                {
                    if (
                        !spritesObj.TryGetProperty(cat, out var dictObj)
                        || dictObj.ValueKind != JsonValueKind.Object
                    )
                        continue;

                    foreach (var kv in dictObj.EnumerateObject())
                    {
                        var keyOriginal = kv.Name;
                        var val = kv.Value;
                        if (
                            val.ValueKind == JsonValueKind.Object
                            && val.TryGetProperty("x", out var xEl)
                            && val.TryGetProperty("y", out var yEl)
                        )
                        {
                            var normalizedKey = NormalizeSpriteName(keyOriginal);

                            var pos = new SpritePosition
                            {
                                Name = normalizedKey,
                                Pos = new Pos { X = xEl.GetInt32(), Y = yEl.GetInt32() },
                            };

                            positions[normalizedKey] = pos;

                            // For stakes, add a simplified alias without "stake" (e.g., "white" -> "whitestake")
                            if (cat.Equals("stakes", StringComparison.OrdinalIgnoreCase))
                            {
                                var simple = normalizedKey.Replace(
                                    "stake",
                                    string.Empty,
                                    StringComparison.Ordinal
                                );
                                if (!string.IsNullOrEmpty(simple) && !positions.ContainsKey(simple))
                                {
                                    positions[simple] = pos;
                                }
                            }
                        }
                    }
                }

                return positions;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SpriteService",
                    $"Error loading sprite positions from {jsonUri}: {ex.Message}"
                );
                return new Dictionary<string, SpritePosition>();
            }
        }

        private static Bitmap? LoadBitmap(string bitmapUri)
        {
            try
            {
                using var stream = TryOpenAssetStream(bitmapUri);
                return new Bitmap(stream);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SpriteService",
                    $"Error loading bitmap from {bitmapUri}: {ex.Message}"
                );
                return null;
            }
        }

        // Try to open an asset as a stream. First try the Avalonia AssetLoader (avares://).
        // If that fails, attempt to locate the file on disk relative to known folders (current dir and parent dirs)
        // This helps during development when resources may not be embedded/copied.
        private static Stream TryOpenAssetStream(string avaresUri)
        {
            try
            {
                var uri = new Uri(avaresUri);
                return AssetLoader.Open(uri);
            }
            catch (Exception ex)
            {
                DebugLogger.Log(
                    "SpriteService",
                    $"AssetLoader failed for '{avaresUri}': {ex.Message}. Trying filesystem fallback..."
                );
            }

            // Fallback: attempt to find the asset under a local Assets folder by stripping the avares scheme
            // Expected format: avares://Assembly/Assets/Path/to/file
            try
            {
                var parts = avaresUri.Split(
                    new[] { "avares://" },
                    StringSplitOptions.RemoveEmptyEntries
                );
                string relativePath = parts.Length > 0 ? parts[0] : avaresUri;

                // If the relativePath contains a leading assembly name (e.g. BalatroSeedOracle/Assets/...), remove it
                var idx = relativePath.IndexOf('/');
                if (idx >= 0)
                {
                    relativePath = relativePath
                        .Substring(idx + 1)
                        .Replace('/', Path.DirectorySeparatorChar);
                }

                // Try current directory and up to 5 parent directories to find Assets
                string? baseDir = AppContext.BaseDirectory;
                for (int depth = 0; depth < 6; depth++)
                {
                    if (baseDir == null)
                        break;

                    // Direct path
                    var candidate = Path.Combine(baseDir, relativePath);
                    if (File.Exists(candidate))
                    {
                        return File.OpenRead(candidate);
                    }

                    // Repository-style path: base/src/<relative>
                    var srcCandidate = Path.Combine(baseDir, "src", relativePath);
                    if (File.Exists(srcCandidate))
                    {
                        return File.OpenRead(srcCandidate);
                    }

                    // Walk up one directory
                    baseDir = Path.GetDirectoryName(baseDir);
                }

                // Also try the workspace relative path (relative to current working directory)
                var cwdCandidate = Path.Combine(Environment.CurrentDirectory, relativePath);
                if (File.Exists(cwdCandidate))
                {
                    return File.OpenRead(cwdCandidate);
                }

                // And CWD/src path
                var cwdSrcCandidate = Path.Combine(
                    Environment.CurrentDirectory,
                    "src",
                    relativePath
                );
                if (File.Exists(cwdSrcCandidate))
                {
                    return File.OpenRead(cwdSrcCandidate);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SpriteService",
                    $"Filesystem fallback failed for '{avaresUri}': {ex.Message}"
                );
            }

            throw new FileNotFoundException($"Asset not found: {avaresUri}");
        }

        private static IImage GetSpriteImage(
            string name_in,
            Dictionary<string, SpritePosition> positions,
            Bitmap? spriteSheet,
            int spriteWidth,
            int spriteHeight,
            string category
        )
        {
            // FAIL LOUDLY: If basic preconditions aren't met, the app is fundamentally broken
            System.Diagnostics.Debug.Assert(
                !string.IsNullOrEmpty(name_in),
                $"[SPRITE SERVICE] name_in is null or empty for category '{category}'"
            );
            System.Diagnostics.Debug.Assert(
                positions != null,
                $"[SPRITE SERVICE] positions dictionary is null for category '{category}'"
            );
            System.Diagnostics.Debug.Assert(
                spriteSheet != null,
                $"[SPRITE SERVICE] spriteSheet is null for category '{category}' - assets failed to load!"
            );

            if (string.IsNullOrEmpty(name_in))
                throw new ArgumentException(
                    $"Sprite name cannot be null or empty (category: {category})",
                    nameof(name_in)
                );

            if (positions == null)
                throw new InvalidOperationException(
                    $"Sprite positions dictionary is null for category '{category}' - SpriteService failed to initialize properly!"
                );

            if (spriteSheet == null)
                throw new InvalidOperationException(
                    $"Sprite sheet is null for category '{category}' - Assets failed to load! Check that all sprite assets are present."
                );

            // Normalize the sprite name for consistent lookup
            string name = NormalizeSpriteName(name_in);

            // Try the normalized name
            if (positions.TryGetValue(name, out var pos))
            {
                int x = pos.Pos.X * spriteWidth;
                int y = pos.Pos.Y * spriteHeight;
                var cropped = new CroppedBitmap(
                    spriteSheet,
                    new PixelRect(x, y, spriteWidth, spriteHeight)
                );
                return cropped;
            }
            else
            {
                // Stake-specific fallback: if caller passed just the color, try appending "stake"
                if (string.Equals(category, "stake", StringComparison.OrdinalIgnoreCase))
                {
                    var stakeKey = name + "stake";
                    if (positions.TryGetValue(stakeKey, out var stakePos))
                    {
                        int x = stakePos.Pos.X * spriteWidth;
                        int y = stakePos.Pos.Y * spriteHeight;
                        return new CroppedBitmap(
                            spriteSheet,
                            new PixelRect(x, y, spriteWidth, spriteHeight)
                        );
                    }
                }

                // FAIL LOUDLY: Missing sprite data means the JSON metadata is incomplete or sprite name is wrong
                var availableKeys = string.Join(
                    ", ",
                    positions
                        .Keys.Where(k =>
                            k.Contains(
                                name.Substring(0, Math.Min(3, name.Length)),
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        .Take(5)
                );
                throw new KeyNotFoundException(
                    $"Sprite '{name_in}' (normalized: '{name}') not found in {category} positions. Similar keys: {availableKeys}"
                );
            }
        }

        public IImage? GetJokerImage(
            string name,
            int spriteWidth = UIConstants.JokerSpriteWidth,
            int spriteHeight = UIConstants.JokerSpriteHeight
        )
        {
            ArgumentNullException.ThrowIfNull(name);

            // Translate Wildcard_ prefix to the actual sprite name
            if (name.StartsWith("Wildcard_", StringComparison.OrdinalIgnoreCase))
            {
                name = "anyjoker";
            }

            return GetSpriteImage(
                name,
                jokerPositions,
                jokerSheet,
                spriteWidth,
                spriteHeight,
                "joker"
            );
        }

        // Get joker image with stickers applied
        public IImage? GetJokerImageWithStickers(
            string name,
            List<string>? stickers,
            int spriteWidth = UIConstants.JokerSpriteWidth,
            int spriteHeight = UIConstants.JokerSpriteHeight
        )
        {
            // Get the base joker image
            var jokerImage = GetJokerImage(name, spriteWidth, spriteHeight);
            if (jokerImage == null)
                return null;

            // If no stickers, return base image
            if (stickers == null || stickers.Count == 0)
                return jokerImage;

            // Legendary jokers (soul jokers) cannot have stickers
            var legendaryJokers = new[] { "perkeo", "canio", "chicot", "triboulet", "yorick" };
            if (legendaryJokers.Contains(name.ToLowerInvariant()))
                return jokerImage;

            // Create a composite with stickers
            var renderTarget = new RenderTargetBitmap(
                new PixelSize(spriteWidth, spriteHeight),
                new Vector(96, 96)
            );
            using (var context = renderTarget.CreateDrawingContext())
            {
                // Draw the base joker
                context.DrawImage(jokerImage, new Rect(0, 0, spriteWidth, spriteHeight));

                // Apply stickers (stickers are 142x190, scale to fit joker size)
                foreach (var sticker in stickers)
                {
                    var stickerImage = GetStickerImage(sticker.ToLowerInvariant());
                    if (stickerImage != null)
                    {
                        // Scale sticker from 142x190 to joker size (71x95)
                        context.DrawImage(
                            stickerImage,
                            new Rect(0, 0, 142, 190),
                            new Rect(0, 0, spriteWidth, spriteHeight)
                        );
                    }
                }
            }

            return renderTarget;
        }

        public IImage? GetJokerSoulImage(
            string name_in,
            int spriteWidth = UIConstants.JokerSpriteWidth,
            int spriteHeight = UIConstants.JokerSpriteHeight
        )
        {
            ArgumentNullException.ThrowIfNull(name_in);
            var name = NormalizeSpriteName(name_in);

            // For legendary jokers, the soul is one row below (y+1)
            if (
                jokerPositions == null
                || jokerSheet == null
                || !jokerPositions.TryGetValue(name, out var basePos)
            )
            {
                return null;
            }

            // Create a new position one row below
            var soulPos = new SpritePosition
            {
                Name = name + "_soul",
                Pos = new Pos { X = basePos.Pos.X, Y = basePos.Pos.Y + 1 },
            };

            int x = soulPos.Pos.X * spriteWidth;
            int y = soulPos.Pos.Y * spriteHeight;

            if (
                x >= 0
                && y >= 0
                && x + spriteWidth <= jokerSheet.PixelSize.Width
                && y + spriteHeight <= jokerSheet.PixelSize.Height
            )
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                    "GetJokerSoulImage",
                    $"🎴 SUCCESS - Creating soul image at ({x}, {y}) for {name}"
                );
                return new CroppedBitmap(
                    jokerSheet,
                    new PixelRect(x, y, spriteWidth, spriteHeight)
                );
            }

            BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                "GetJokerSoulImage",
                $"🎴 FAILED - Invalid coordinates ({x}, {y}) for {name}"
            );
            return null;
        }

        /// <summary>
        /// Gets the Soul Gem overlay sprite from Enhancers.png for "The Soul" spectral card
        /// </summary>
        public IImage? GetSoulGemImage(
            int spriteWidth = UIConstants.SpectralSpriteWidth,
            int spriteHeight = UIConstants.SpectralSpriteHeight
        )
        {
            return GetSpriteImage(
                "TheSoulGem",
                specialPositions,
                enhancersSheet,
                spriteWidth,
                spriteHeight,
                "special"
            );
        }

        /// <summary>
        /// Gets the Mystery Joker Face overlay sprite from Enhancers.png for wildcard jokers
        /// </summary>
        public IImage? GetMysteryJokerFaceImage(
            int spriteWidth = UIConstants.JokerSpriteWidth,
            int spriteHeight = UIConstants.JokerSpriteHeight
        )
        {
            return GetSpriteImage(
                "MysteryJokerFace",
                specialPositions,
                enhancersSheet,
                spriteWidth,
                spriteHeight,
                "special"
            );
        }

        public IImage? GetTagImage(
            string name,
            int spriteWidth = UIConstants.TagSpriteWidth,
            int spriteHeight = UIConstants.TagSpriteHeight
        )
        {
            ArgumentNullException.ThrowIfNull(name);
            return GetSpriteImage(name, tagPositions, tagSheet, spriteWidth, spriteHeight, "tag");
        }

        public IImage? GetTarotImage(
            string name,
            int spriteWidth = UIConstants.TarotSpriteWidth,
            int spriteHeight = UIConstants.TarotSpriteHeight
        )
        {
            ArgumentNullException.ThrowIfNull(name);

            // Translate Wildcard_ prefix to the actual sprite name
            if (name.StartsWith("Wildcard_", StringComparison.OrdinalIgnoreCase))
            {
                name = "anytarot";
            }

            return GetSpriteImage(
                name,
                tarotPositions,
                tarotSheet,
                spriteWidth,
                spriteHeight,
                "tarot"
            );
        }

        public IImage? GetSpectralImage(
            string name,
            int spriteWidth = UIConstants.SpectralSpriteWidth,
            int spriteHeight = UIConstants.SpectralSpriteHeight
        )
        {
            ArgumentNullException.ThrowIfNull(name);

            // Translate Wildcard_ prefix to the actual sprite name
            if (name.StartsWith("Wildcard_", StringComparison.OrdinalIgnoreCase))
            {
                name = "anyspectral";
            }

            return GetSpriteImage(
                name,
                spectralPositions,
                spectralSheet,
                spriteWidth,
                spriteHeight,
                "spectral"
            );
        }

        public IImage? GetPlanetCardImage(
            string name,
            int spriteWidth = UIConstants.TarotSpriteWidth,
            int spriteHeight = UIConstants.TarotSpriteHeight
        )
        {
            ArgumentNullException.ThrowIfNull(name);

            // Translate Wildcard_ prefix to the actual sprite name
            if (name.StartsWith("Wildcard_", StringComparison.OrdinalIgnoreCase))
            {
                name = "anyplanet";
            }

            // Planet cards are in the tarots sprite sheet
            return GetSpriteImage(
                name,
                planetPositions,
                tarotSheet,
                spriteWidth,
                spriteHeight,
                "planet"
            );
        }

        public IImage? GetVoucherImage(
            string name,
            int spriteWidth = UIConstants.VoucherSpriteWidth,
            int spriteHeight = UIConstants.VoucherSpriteHeight
        )
        {
            ArgumentNullException.ThrowIfNull(name);
            return GetSpriteImage(
                name,
                voucherPositions,
                voucherSheet,
                spriteWidth,
                spriteHeight,
                "voucher"
            );
        }

        /// <summary>
        /// Get an image for any type of item - automatically determines the type
        /// </summary>
        public IImage? GetItemImage(string name, string? type = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            // Handle wildcard values (Any, *, etc.) - return a transparent placeholder
            if (
                name.Equals("Any", StringComparison.OrdinalIgnoreCase)
                || name == "*"
                || name == "."
                || name.Equals("None", StringComparison.OrdinalIgnoreCase)
            )
            {
                // Return a 1x1 transparent bitmap as placeholder
                var bitmap = new Avalonia.Media.Imaging.WriteableBitmap(
                    new Avalonia.PixelSize(1, 1),
                    new Avalonia.Vector(96, 96),
                    Avalonia.Platform.PixelFormat.Bgra8888,
                    Avalonia.Platform.AlphaFormat.Premul
                );
                return bitmap;
            }

            // Strip unique key suffix (#1, #2, etc.) from duplicate items
            var hashIndex = name.LastIndexOf('#');
            if (hashIndex > 0)
            {
                name = name.Substring(0, hashIndex);
            }

            // If type is specified, use it directly
            if (!string.IsNullOrEmpty(type))
            {
                return type.ToLowerInvariant() switch
                {
                    "joker" or "jokers" => GetJokerImage(name),
                    "tag" or "tags" => GetTagImage(name),
                    "tarot" or "tarots" => GetTarotImage(name),
                    "spectral" or "spectrals" => GetSpectralImage(name),
                    "planet" or "planets" => GetPlanetCardImage(name),
                    "voucher" or "vouchers" => GetVoucherImage(name),
                    "boss" or "bosses" => GetBossImage(name),
                    "blind" or "blinds" => GetBlindImage(name),
                    "sticker" or "stickers" => GetStickerImage(name),
                    "booster" or "boosters" or "pack" or "packs" => GetBoosterImage(name),
                    _ => null,
                };
            }

            // Normalize the name for lookup
            var normalizedName = NormalizeSpriteName(name);

            // Check jokers first (most common)
            if (jokerPositions.ContainsKey(normalizedName))
            {
                return GetJokerImage(name);
            }

            // Check tags
            if (tagPositions.ContainsKey(normalizedName))
            {
                return GetTagImage(name);
            }

            // Check tarots
            if (tarotPositions.ContainsKey(normalizedName))
            {
                return GetTarotImage(name);
            }

            // Check spectrals
            if (spectralPositions.ContainsKey(normalizedName))
            {
                return GetSpectralImage(name);
            }

            // Check planets
            if (planetPositions.ContainsKey(normalizedName))
            {
                return GetPlanetCardImage(name);
            }

            // Check vouchers
            if (voucherPositions.ContainsKey(normalizedName))
            {
                return GetVoucherImage(name);
            }

            return null;
        }

        private Bitmap? editionsSheet;

        public IImage? GetEditionImage(string edition)
        {
            try
            {
                // Lazy load the editions sheet
                if (editionsSheet == null)
                {
                    var editionsUri = "avares://BalatroSeedOracle/Assets/Jokers/Editions.png";
                    editionsSheet = LoadBitmap(editionsUri);
                    if (editionsSheet != null)
                    {
                        // DebugLogger.Log("SpriteService", "Loaded editions sprite sheet");
                    }
                    else
                    {
                        DebugLogger.LogError(
                            "SpriteService",
                            $"Failed to load editions sprite sheet"
                        );
                        return null;
                    }
                }

                // Each edition is 71x94 pixels (355 width / 5 editions = 71)
                int spriteWidth = 71;
                int spriteHeight = 94;

                // Map edition names to positions (0-4)
                int position = edition.ToLowerInvariant() switch
                {
                    "none" or "normal" => 0,
                    "foil" => 1,
                    "holographic" or "holo" => 2,
                    "polychrome" or "poly" => 3,
                    "negative" or "neg" => 4, // Position 4 is Negative (not debuffed/red X)
                    _ => 0,
                };

                int x = position * spriteWidth;

                if (
                    x + spriteWidth <= editionsSheet.PixelSize.Width
                    && spriteHeight <= editionsSheet.PixelSize.Height
                )
                {
                    return new CroppedBitmap(
                        editionsSheet,
                        new PixelRect(x, 0, spriteWidth, spriteHeight)
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SpriteService", $"Failed to get edition image: {ex.Message}");
            }

            return null;
        }

        // New methods for deck, enhancement, and seal sprites
        public IImage? GetDeckImage(string name, int spriteWidth = 142, int spriteHeight = 190)
        {
            var normalized = NormalizeSpriteName(name);
            if (!deckPositions.TryGetValue(normalized, out var pos))
            {
                DebugLogger.LogError(
                    "SpriteService",
                    $"'{name}' NOT found in deckPositions! Available: {string.Join(", ", deckPositions.Keys)}"
                );
            }

            var result = GetSpriteImage(
                name,
                deckPositions,
                deckSheet,
                spriteWidth,
                spriteHeight,
                "deck"
            );
            if (result == null)
            {
                DebugLogger.LogError(
                    "SpriteService",
                    $"GetDeckImage returned NULL for '{name}'! deckPositions={deckPositions != null}, deckSheet={deckSheet != null}"
                );
            }
            return result;
        }

        public IImage? GetStakeImage(string name, int spriteWidth = 48, int spriteHeight = 48)
        {
            return GetSpriteImage(
                name,
                stakePositions,
                stakeSheet,
                spriteWidth,
                spriteHeight,
                "stake"
            );
        }

        // Create a composite image with deck and stake sticker
        public IImage? GetDeckWithStakeSticker(string deckName, string stakeName)
        {
            ArgumentNullException.ThrowIfNull(deckName);
            ArgumentNullException.ThrowIfNull(stakeName);

            // Get the base deck image
            var deckImage = GetDeckImage(deckName);
            if (deckImage == null)
            {
                return null;
            }

            // Get the stake STICKER image (not the UI chip - stickers overlay on cards!)
            // Normalize to sticker key (e.g., "white" -> "whitestake")
            var normalized = stakeName
                .ToLowerInvariant()
                .Replace("stake", string.Empty, StringComparison.Ordinal)
                .Trim();
            var stickerKey = string.IsNullOrEmpty(normalized) ? "whitestake" : normalized + "stake";
            var stickerImage = GetStickerImage(stickerKey);

            // If sticker not found, just return the base deck image
            if (stickerImage == null)
            {
                return deckImage;
            }

            // Create a render target to composite the images
            var pixelSize = new PixelSize(142, 190); // Full card size (stickers are 142x190!)
            var renderTarget = new RenderTargetBitmap(pixelSize);

            using (var context = renderTarget.CreateDrawingContext())
            {
                // Draw the deck image
                context.DrawImage(deckImage, new Rect(0, 0, 142, 190));

                // Draw the stake sticker on top (same size as deck)
                context.DrawImage(stickerImage, new Rect(0, 0, 142, 190));
            }

            return renderTarget;
        }

        public IImage? GetEnhancementImage(
            string name,
            int spriteWidth = 142,
            int spriteHeight = 190
        )
        {
            return GetSpriteImage(
                name,
                enhancementPositions,
                enhancersSheet,
                spriteWidth,
                spriteHeight,
                "enhancement"
            );
        }

        public IImage? GetSealImage(string name, int spriteWidth = 142, int spriteHeight = 190)
        {
            return GetSpriteImage(
                name,
                sealPositions,
                enhancersSheet,
                spriteWidth,
                spriteHeight,
                "seal"
            );
        }

        public IImage? GetSpecialImage(string name, int spriteWidth = 142, int spriteHeight = 190)
        {
            // Special images like Soul gem, mystery icons, etc.
            return GetSpriteImage(
                name,
                specialPositions,
                enhancersSheet,
                spriteWidth,
                spriteHeight,
                "special"
            );
        }

        /// <summary>
        /// Composites two images together (overlay on top of base)
        /// </summary>
        private IImage? CompositeImages(
            IImage baseImage,
            IImage overlayImage,
            int width,
            int height
        )
        {
            try
            {
                // Create a new render target bitmap
                var renderTarget = new Avalonia.Media.Imaging.RenderTargetBitmap(
                    new Avalonia.PixelSize(width, height),
                    new Avalonia.Vector(96, 96)
                );

                using (var ctx = renderTarget.CreateDrawingContext())
                {
                    // Draw base image - accepts IImage (includes Bitmap and CroppedBitmap)
                    ctx.DrawImage(baseImage, new Avalonia.Rect(0, 0, width, height));

                    // Draw overlay image on top - accepts IImage
                    ctx.DrawImage(overlayImage, new Avalonia.Rect(0, 0, width, height));
                }

                return renderTarget;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SpriteService", $"Failed to composite images: {ex.Message}");
                return baseImage;
            }
        }

        // Get a composite playing card image (enhancement + card pattern)
        public IImage GetPlayingCardImage(
            string suit,
            string rank,
            string? enhancement = null,
            string? seal = null,
            string? edition = null
        )
        {
            // Special case: Stone enhancement has no rank/suit pattern
            if (enhancement == "Stone")
            {
                var stoneImage = GetEnhancementImage("Stone");
                if (stoneImage == null)
                    throw new InvalidOperationException("Stone enhancement image not found!");
                return stoneImage;
            }

            // Start with base card or enhancement
            IImage baseCard;
            if (!string.IsNullOrEmpty(enhancement))
            {
                var enhancementImage = GetEnhancementImage(enhancement);
                if (enhancementImage == null)
                    throw new InvalidOperationException(
                        $"Enhancement '{enhancement}' image not found!"
                    );
                baseCard = enhancementImage;
            }
            else
            {
                // Use blank card as base (StandardCard_Base in metadata)
                var blankCard = GetSpecialImage("StandardCard_Base");
                if (blankCard == null)
                    throw new InvalidOperationException(
                        "StandardCard_Base image not found in special sprites!"
                    );
                baseCard = blankCard;
            }

            // Get the card pattern overlay (will throw if not found - which is what we want!)
            var cardPattern = GetPlayingCardPattern(suit, rank);

            // Composite the images together (base + pattern overlay)
            // Playing cards are 142x190 pixels
            var result = CompositeImages(baseCard, cardPattern, 142, 190);
            if (result == null)
                throw new InvalidOperationException(
                    $"Failed to composite playing card {rank} of {suit}!"
                );

            return result;
        }

        // Get boss blind image (first frame of animation, similar size to tags)
        public IImage? GetBossImage(string name, int frameIndex = 0)
        {
            ArgumentNullException.ThrowIfNull(name);

            // Strip unique key suffix (#1, #2, etc.) if present
            var hashIndex = name.LastIndexOf('#');
            if (hashIndex > 0)
            {
                name = name.Substring(0, hashIndex);
            }

            // SmallBlind and BigBlind are not actual bosses - they're tags
            var normalizedName = NormalizeSpriteName(name);
            if (normalizedName == "smallblind" || normalizedName == "bigblind")
            {
                return GetTagImage(name); // Redirect to tag images
            }

            if (bossPositions == null || bossSheet == null)
            {
                DebugLogger.LogError(
                    "SpriteService",
                    $"Boss positions or sheet not loaded for: {name}"
                );
                return null;
            }
            if (!bossPositions.TryGetValue(normalizedName, out var position))
            {
                DebugLogger.LogError(
                    "SpriteService",
                    $"Boss position not found for: {name} (normalized: {normalizedName})"
                );
                return null;
            }

            // Boss blind sprites are 68x68 (1428px / 21 cols = 68px per sprite)
            int spriteWidth = 68;
            int spriteHeight = 68;

            // Use the specified frame (0-20)
            // All bosses start at column 0, so just use frameIndex for x position
            int x = frameIndex * spriteWidth;
            int y = position.Pos.Y * spriteHeight;

            return new CroppedBitmap(bossSheet, new PixelRect(x, y, spriteWidth, spriteHeight));
        }

        // Get sticker image (Eternal, Perishable, Rental, or stake stickers)
        public IImage? GetStickerImage(string stickerType)
        {
            ArgumentNullException.ThrowIfNull(stickerType);
            if (stickerPositions == null)
            {
                DebugLogger.LogError("SpriteService", "stickerPositions is null!");
            }
            return GetSpriteImage(
                stickerType,
                stickerPositions!,
                stickersSheet,
                142,
                190,
                "sticker"
            );
        }

        /// <summary>
        /// Gets a composite image of a Joker with a sticker overlay
        /// </summary>
        public IImage? GetJokerWithStickerImage(string stickerType)
        {
            ArgumentNullException.ThrowIfNull(stickerType);

            // Get base joker image
            var baseJoker = GetItemImage("Joker", "Joker");
            if (baseJoker == null)
            {
                DebugLogger.LogError(
                    "SpriteService",
                    "Failed to get base Joker image for sticker composite"
                );
                return null;
            }

            // Get sticker overlay
            var stickerOverlay = GetStickerImage(stickerType);
            if (stickerOverlay == null)
            {
                DebugLogger.LogError(
                    "SpriteService",
                    $"Failed to get sticker image for '{stickerType}'"
                );
                return baseJoker; // Return just the joker if sticker not found
            }

            // Composite them together (142x190 pixels - standard joker size)
            return CompositeImages(baseJoker, stickerOverlay, 142, 190);
        }

        /// <summary>
        /// Gets a composite image of a Joker with an edition overlay
        /// </summary>
        public IImage? GetJokerWithEditionImage(string edition)
        {
            ArgumentNullException.ThrowIfNull(edition);

            // Get base joker image
            var baseJoker = GetItemImage("Joker", "Joker");
            if (baseJoker == null)
            {
                DebugLogger.LogError(
                    "SpriteService",
                    "Failed to get base Joker image for edition composite"
                );
                return null;
            }

            // Special case for "None" - just return the base joker
            if (edition.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                return baseJoker;
            }

            // Get edition overlay
            var editionOverlay = GetEditionImage(edition);
            if (editionOverlay == null)
            {
                DebugLogger.LogError(
                    "SpriteService",
                    $"Failed to get edition overlay for: {edition}"
                );
                return baseJoker; // Return base joker if no overlay available
            }

            // Composite them together (142x190 pixels - standard joker size)
            return CompositeImages(baseJoker, editionOverlay, 142, 190);
        }

        // Get stake chip image from the smaller stake chips sprite sheet (29x29 pixels each)

        public IImage? GetBoosterImage(string packType)
        {
            ArgumentNullException.ThrowIfNull(packType);
            // Booster pack sprites are 142x285 (width of 568/4, height of 1710/6)
            return GetSpriteImage(packType, boosterPositions, boosterSheet, 142, 285, "booster");
        }

        // Get blind chip image (small/big blind indicators)
        public IImage? GetBlindImage(string blindType, int frameIndex = 0)
        {
            ArgumentNullException.ThrowIfNull(blindType);
            if (blindPositions == null || bossSheet == null)
            {
                return null;
            }

            var normalizedName = NormalizeSpriteName(blindType);
            if (!blindPositions.TryGetValue(normalizedName, out var position))
            {
                return null;
            }

            // Same dimensions as boss sprites - 68x68 pixels
            int spriteWidth = 68;
            int spriteHeight = 68;

            // Blinds also start at column 0, so just use frameIndex for x position
            int x = frameIndex * spriteWidth;
            int y = position.Pos.Y * spriteHeight;

            return new CroppedBitmap(bossSheet, new PixelRect(x, y, spriteWidth, spriteHeight));
        }

        // Get just the playing card pattern (suit/rank)
        private IImage GetPlayingCardPattern(string suit, string rank)
        {
            if (playingCardPositions == null)
                throw new InvalidOperationException(
                    "Playing card positions not loaded! LoadSprites() was not called or failed."
                );

            if (playingCardsSheet == null)
                throw new InvalidOperationException(
                    "Playing cards sprite sheet not loaded! 8BitDeck.png missing or failed to load."
                );

            if (!playingCardPositions.TryGetValue(suit, out var suitCards))
            {
                throw new ArgumentException(
                    $"Suit '{suit}' not found. Available: {string.Join(", ", playingCardPositions.Keys)}",
                    nameof(suit)
                );
            }

            if (!suitCards.TryGetValue(rank, out var position))
            {
                throw new ArgumentException(
                    $"Rank '{rank}' not found for suit '{suit}'. Available: {string.Join(", ", suitCards.Keys)}",
                    nameof(rank)
                );
            }

            // Calculate sprite dimensions (1846x760 with 13x4 grid)
            int spriteWidth = 142; // 1846 / 13
            int spriteHeight = 190; // 760 / 4

            int x = position.Pos.X * spriteWidth;
            int y = position.Pos.Y * spriteHeight;

            DebugLogger.Log(
                "SpriteService",
                $"Loading pattern {rank} of {suit} from position ({position.Pos.X}, {position.Pos.Y}) -> pixel ({x}, {y})"
            );

            return new CroppedBitmap(
                playingCardsSheet,
                new PixelRect(x, y, spriteWidth, spriteHeight)
            );
        }

        // Helper method to load stickers metadata
        private static StickersMetadata? LoadStickersMetadata(string jsonUri)
        {
            try
            {
                var uri = new Uri(jsonUri);
                using var stream = AssetLoader.Open(uri);
                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();

                var metadata = JsonSerializer.Deserialize<StickersMetadataJson>(json);
                if (metadata == null)
                {
                    return null;
                }

                var result = new StickersMetadata
                {
                    JokerStickers = ConvertToSpritePositions(metadata.Sprites?.JokerStickers),
                    StakeStickers = ConvertToSpritePositions(metadata.Sprites?.StakeStickers),
                };

                return result;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SpriteService",
                    $"Error loading stickers metadata: {ex.Message}"
                );
                return null;
            }
        }

        // Helper method to load boss metadata
        private static BossMetadata? LoadBossMetadata(string jsonUri)
        {
            try
            {
                var uri = new Uri(jsonUri);
                using var stream = AssetLoader.Open(uri);
                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();

                var metadata = JsonSerializer.Deserialize<BossMetadataJson>(json);
                if (metadata == null)
                {
                    return null;
                }

                var result = new BossMetadata
                {
                    Blinds = ConvertToSpritePositions(metadata.Sprites?.Blinds),
                    Bosses = ConvertToSpritePositions(metadata.Sprites?.Bosses),
                    FinisherBosses = ConvertToSpritePositions(metadata.Sprites?.FinisherBosses),
                    Special = ConvertToSpritePositions(metadata.Sprites?.Special),
                };

                return result;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SpriteService", $"Error loading boss metadata: {ex.Message}");
                return null;
            }
        }

        // Helper method to load playing card metadata
        private static Dictionary<
            string,
            Dictionary<string, SpritePosition>
        > LoadPlayingCardMetadata(string jsonUri)
        {
            try
            {
                var uri = new Uri(jsonUri);
                using var stream = AssetLoader.Open(uri);
                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();

                var metadata = JsonSerializer.Deserialize<PlayingCardMetadataJson>(json);
                if (metadata?.Sprites == null)
                {
                    return new();
                }

                var result = new Dictionary<string, Dictionary<string, SpritePosition>>();

                foreach (var suitKvp in metadata.Sprites)
                {
                    var suitPositions = new Dictionary<string, SpritePosition>();
                    foreach (var rankKvp in suitKvp.Value)
                    {
                        suitPositions[rankKvp.Key] = new SpritePosition
                        {
                            Name = $"{rankKvp.Key} of {suitKvp.Key}",
                            Pos = new Pos { X = rankKvp.Value.X, Y = rankKvp.Value.Y },
                        };
                    }
                    result[suitKvp.Key] = suitPositions;
                }

                return result;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SpriteService",
                    $"Error loading playing card metadata: {ex.Message}"
                );
                return new();
            }
        }

        // Helper method to load enhancers metadata with custom structure
        private static EnhancersMetadata? LoadEnhancersMetadata(string jsonUri)
        {
            try
            {
                // Use robust asset loading with filesystem fallback to handle non-embedded resources during development
                using var stream = TryOpenAssetStream(jsonUri);
                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();

                var metadata = JsonSerializer.Deserialize<EnhancersMetadataJson>(json);
                if (metadata == null)
                {
                    return null;
                }

                // Convert the JSON structure to SpritePosition dictionaries
                var result = new EnhancersMetadata
                {
                    Decks = ConvertToSpritePositions(metadata.Sprites?.Decks),
                    Enhancements = ConvertToSpritePositions(metadata.Sprites?.Enhancements),
                    Seals = ConvertToSpritePositions(metadata.Sprites?.Seals),
                    Special = ConvertToSpritePositions(metadata.Sprites?.Special),
                };

                return result;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SpriteService",
                    $"Error loading enhancers metadata: {ex.Message}"
                );
                return null;
            }
        }

        private static Dictionary<string, SpritePosition> ConvertToSpritePositions(
            Dictionary<string, EnhancerSprite>? sprites
        )
        {
            var result = new Dictionary<string, SpritePosition>();
            if (sprites == null)
            {
                return result;
            }

            foreach (var kvp in sprites)
            {
                var normalizedKey = NormalizeSpriteName(kvp.Key);

                result[normalizedKey] = new SpritePosition
                {
                    Name = kvp.Key,
                    Pos = new Pos { X = kvp.Value.X, Y = kvp.Value.Y },
                };
            }

            return result;
        }

        // Internal classes for metadata deserialization
        private sealed class EnhancersMetadata
        {
            public Dictionary<string, SpritePosition> Decks { get; set; } = new();
            public Dictionary<string, SpritePosition> Enhancements { get; set; } = new();
            public Dictionary<string, SpritePosition> Seals { get; set; } = new();
            public Dictionary<string, SpritePosition> Special { get; set; } = new();
        }

        private sealed class EnhancersMetadataJson
        {
            [JsonPropertyName("sprites")]
            public EnhancersSprites? Sprites { get; set; }
        }

        private sealed record EnhancersSprites(
            [property: JsonPropertyName("decks")] Dictionary<string, EnhancerSprite>? Decks,
            [property: JsonPropertyName("enhancements")]
                Dictionary<string, EnhancerSprite>? Enhancements,
            [property: JsonPropertyName("seals")] Dictionary<string, EnhancerSprite>? Seals,
            [property: JsonPropertyName("special")] Dictionary<string, EnhancerSprite>? Special
        );

        private sealed record EnhancerSprite(
            [property: JsonPropertyName("x")] int X,
            [property: JsonPropertyName("y")] int Y,
            [property: JsonPropertyName("description")] string? Description
        );

        private sealed class PlayingCardMetadataJson
        {
            [JsonPropertyName("sprites")]
            public Dictionary<string, Dictionary<string, CardPosition>>? Sprites { get; set; }
        }

        private sealed class CardPosition
        {
            [JsonPropertyName("x")]
            public int X { get; set; }

            [JsonPropertyName("y")]
            public int Y { get; set; }
        }

        private sealed class BossMetadata
        {
            public Dictionary<string, SpritePosition> Blinds { get; set; } = new();
            public Dictionary<string, SpritePosition> Bosses { get; set; } = new();
            public Dictionary<string, SpritePosition> FinisherBosses { get; set; } = new();
            public Dictionary<string, SpritePosition> Special { get; set; } = new();
        }

        private sealed class BossMetadataJson
        {
            [JsonPropertyName("sprites")]
            public BossSprites? Sprites { get; set; }
        }

        private sealed class BossSprites
        {
            [JsonPropertyName("blinds")]
            public Dictionary<string, EnhancerSprite>? Blinds { get; set; }

            [JsonPropertyName("bosses")]
            public Dictionary<string, EnhancerSprite>? Bosses { get; set; }

            [JsonPropertyName("finisherBosses")]
            public Dictionary<string, EnhancerSprite>? FinisherBosses { get; set; }

            [JsonPropertyName("special")]
            public Dictionary<string, EnhancerSprite>? Special { get; set; }
        }

        private sealed class StickersMetadata
        {
            public Dictionary<string, SpritePosition> JokerStickers { get; set; } = new();
            public Dictionary<string, SpritePosition> StakeStickers { get; set; } = new();
        }

        private sealed class StickersMetadataJson
        {
            [JsonPropertyName("sprites")]
            public StickersSprites? Sprites { get; set; }
        }

        private sealed class StickersSprites
        {
            [JsonPropertyName("jokerStickers")]
            public Dictionary<string, EnhancerSprite>? JokerStickers { get; set; }

            [JsonPropertyName("stakeStickers")]
            public Dictionary<string, EnhancerSprite>? StakeStickers { get; set; }
        }
    }

    public class SpritePosition
    {
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("pos")]
        public required Pos Pos { get; set; }
    }

    public class Pos
    {
        [JsonPropertyName("x")]
        public required int X { get; set; }

        [JsonPropertyName("y")]
        public required int Y { get; set; }
    }
}
