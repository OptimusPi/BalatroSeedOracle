using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        private void LoadAssets()
        {
            DebugLogger.Log("SpriteService", "Loading sprite assets...");
            try
            {
                // Load joker positions from json
                jokerPositions = LoadSpritePositions("avares://BalatroSeedOracle/Assets/Jokers/jokers.json");

                // Load tag positions from json
                tagPositions = LoadSpritePositions("avares://BalatroSeedOracle/Assets/Tags/tags.json");

                // Load tarot positions from json
                tarotPositions = LoadSpritePositions("avares://BalatroSeedOracle/Assets/Tarots/tarots.json");

                // Load spectral positions from json
                // Load spectral positions from json (they're in the tarots sprite sheet)
                spectralPositions = LoadSpritePositions("avares://BalatroSeedOracle/Assets/Tarots/spectrals.json");

                // Load planet positions from json (they're also in the tarots sprite sheet)
                planetPositions = LoadSpritePositions("avares://BalatroSeedOracle/Assets/Tarots/planets.json");

                // Load voucher positions from json
                voucherPositions = LoadSpritePositions("avares://BalatroSeedOracle/Assets/Vouchers/vouchers.json");

                

                // Load stake positions from json
                stakePositions = LoadSpritePositions("avares://BalatroSeedOracle/Assets/Decks/stakes.json");

                // Load booster pack positions from json
                boosterPositions = LoadSpritePositions("avares://BalatroSeedOracle/Assets/Other/Boosters.json");

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
                voucherSheet = LoadBitmap("avares://BalatroSeedOracle/Assets/Vouchers/Vouchers.png");
                spectralSheet = tarotSheet;
                
                stakeSheet = LoadBitmap("avares://BalatroSeedOracle/Assets/Decks/balatro-stake-chips.png");
                enhancersSheet = LoadBitmap("avares://BalatroSeedOracle/Assets/Decks/Enhancers.png");
                deckSheet = enhancersSheet;
                playingCardsSheet = LoadBitmap("avares://BalatroSeedOracle/Assets/Decks/8BitDeck.png");
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
                playingCardPositions ??= new Dictionary<string, Dictionary<string, SpritePosition>>();
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
                if (fileName.Contains("decks")) categories.Add("decks");
                else if (fileName.Contains("stakes")) categories.Add("stakes");
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
                    if (!spritesObj.TryGetProperty(cat, out var dictObj) || dictObj.ValueKind != JsonValueKind.Object)
                        continue;

                    foreach (var kv in dictObj.EnumerateObject())
                    {
                        var keyOriginal = kv.Name;
                        var val = kv.Value;
                        if (val.ValueKind == JsonValueKind.Object &&
                            val.TryGetProperty("x", out var xEl) &&
                            val.TryGetProperty("y", out var yEl))
                        {
                            var normalizedKey = keyOriginal
                                .Trim()
                                .Replace(" ", string.Empty, StringComparison.Ordinal)
                                .Replace("_", string.Empty, StringComparison.Ordinal)
                                .ToLowerInvariant();

                            var pos = new SpritePosition
                            {
                                Name = normalizedKey,
                                Pos = new Pos { X = xEl.GetInt32(), Y = yEl.GetInt32() },
                            };

                            positions[normalizedKey] = pos;

                            // For stakes, add a simplified alias without "stake" (e.g., "white" -> "whitestake")
                            if (cat.Equals("stakes", StringComparison.OrdinalIgnoreCase))
                            {
                                var simple = normalizedKey.Replace("stake", string.Empty, StringComparison.Ordinal);
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
            catch
            {
            }

            // Fallback: attempt to find the asset under a local Assets folder by stripping the avares scheme
            // Expected format: avares://Assembly/Assets/Path/to/file
            try
            {
                var parts = avaresUri.Split(new[] {"avares://"}, StringSplitOptions.RemoveEmptyEntries);
                string relativePath = parts.Length > 0 ? parts[0] : avaresUri;

                // If the relativePath contains a leading assembly name (e.g. BalatroSeedOracle/Assets/...), remove it
                var idx = relativePath.IndexOf('/');
                if (idx >= 0)
                {
                    relativePath = relativePath.Substring(idx + 1).Replace('/', Path.DirectorySeparatorChar);
                }

                // Try current directory and up to 5 parent directories to find Assets
                string? baseDir = AppContext.BaseDirectory;
                for (int depth = 0; depth < 6; depth++)
                {
                    if (baseDir == null) break;

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
                var cwdSrcCandidate = Path.Combine(Environment.CurrentDirectory, "src", relativePath);
                if (File.Exists(cwdSrcCandidate))
                {
                    return File.OpenRead(cwdSrcCandidate);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SpriteService", $"Filesystem fallback failed for '{avaresUri}': {ex.Message}");
            }

            throw new FileNotFoundException($"Asset not found: {avaresUri}");
        }

        private static IImage? GetSpriteImage(
            string name_in,
            Dictionary<string, SpritePosition> positions,
            Bitmap? spriteSheet,
            int spriteWidth,
            int spriteHeight,
            string category
        )
        {
            if (string.IsNullOrEmpty(name_in) || positions == null || spriteSheet == null)
            {
                return null;
            }

            // Simple approach: just convert to lowercase and remove all spaces
            string name = name_in
                .Trim()
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("_", string.Empty, StringComparison.Ordinal)
                .ToLowerInvariant();

            // Try the normalized name
            if (positions!.TryGetValue(name, out var pos))
            {
                int x = pos.Pos.X * spriteWidth;
                int y = pos.Pos.Y * spriteHeight;
                var cropped = new CroppedBitmap(spriteSheet, new PixelRect(x, y, spriteWidth, spriteHeight));
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
                        return new CroppedBitmap(spriteSheet, new PixelRect(x, y, spriteWidth, spriteHeight));
                    }
                }

                return null;
            }
        }

        public IImage? GetJokerImage(
            string name,
            int spriteWidth = UIConstants.JokerSpriteWidth,
            int spriteHeight = UIConstants.JokerSpriteHeight
        )
        {
            ArgumentNullException.ThrowIfNull(name);
            // Special handling for "any" soul joker
            if (name.Equals("any", StringComparison.OrdinalIgnoreCase))
            {
                // Return a special "any" icon or the first legendary joker as a placeholder
                // For now, use Perkeo as the representative for "any" soul joker
                return GetSpriteImage(
                    "perkeo",
                    jokerPositions,
                    jokerSheet,
                    spriteWidth,
                    spriteHeight,
                    "joker"
                );
            }
            return GetSpriteImage(name, jokerPositions, jokerSheet, spriteWidth, spriteHeight, "joker");
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
            var renderTarget = new RenderTargetBitmap(new PixelSize(spriteWidth, spriteHeight), new Vector(96, 96));
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
            var name = name_in
                .Trim()
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("_", string.Empty, StringComparison.Ordinal)
                .ToLowerInvariant();
            BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                "GetJokerSoulImage",
                $"🎴 SOUL IMAGE REQUEST - Input: '{name_in}', Normalized: '{name}'"
            );

            // For legendary jokers, the soul is one row below (y+1)
            if (
                jokerPositions == null
                || jokerSheet == null
                || !jokerPositions.TryGetValue(name, out var basePos)
            )
            {
                var prefix = name.Substring(0, Math.Min(3, name.Length));
                BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                    "GetJokerSoulImage",
                    $"🎴 Failed to find position for: {name}. Available positions: {string.Join(", ", jokerPositions?.Keys.Where(k => k.Contains(prefix, StringComparison.OrdinalIgnoreCase)).Take(5) ?? new List<string>())}"
                );
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

            // Validate coordinates
            BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                "GetJokerSoulImage",
                $"🎴 Sheet dimensions: {jokerSheet.PixelSize.Width}x{jokerSheet.PixelSize.Height}"
            );
            BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                "GetJokerSoulImage",
                $"🎴 Trying to crop at ({x}, {y}) with size {spriteWidth}x{spriteHeight}"
            );
            BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                "GetJokerSoulImage",
                $"🎴 Bottom-right corner would be at ({x + spriteWidth}, {y + spriteHeight})"
            );

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
                return new CroppedBitmap(jokerSheet, new PixelRect(x, y, spriteWidth, spriteHeight));
            }

            BalatroSeedOracle.Helpers.DebugLogger.LogImportant(
                "GetJokerSoulImage",
                $"🎴 FAILED - Invalid coordinates ({x}, {y}) for {name}"
            );
            return null;
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
            return GetSpriteImage(name, tarotPositions, tarotSheet, spriteWidth, spriteHeight, "tarot");
        }

        public IImage? GetSpectralImage(
            string name,
            int spriteWidth = UIConstants.SpectralSpriteWidth,
            int spriteHeight = UIConstants.SpectralSpriteHeight
        )
        {
            ArgumentNullException.ThrowIfNull(name);
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
            var normalizedName = name.Trim()
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("_", string.Empty, StringComparison.Ordinal)
                .ToLowerInvariant();

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
                        DebugLogger.LogError("SpriteService", $"Failed to load editions sprite sheet");
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
            var normalized = name.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
            if (!deckPositions.TryGetValue(normalized, out var pos))
            {
                DebugLogger.LogError("SpriteService", $"'{name}' NOT found in deckPositions! Available: {string.Join(", ", deckPositions.Keys)}");
            }

            var result = GetSpriteImage(name, deckPositions, deckSheet, spriteWidth, spriteHeight, "deck");
            if (result == null)
            {
                DebugLogger.LogError("SpriteService", $"GetDeckImage returned NULL for '{name}'! deckPositions={deckPositions != null}, deckSheet={deckSheet != null}");
            }
            return result;
        }

        public IImage? GetStakeImage(string name, int spriteWidth = 48, int spriteHeight = 48)
        {
            return GetSpriteImage(name, stakePositions, stakeSheet, spriteWidth, spriteHeight, "stake");
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

        public IImage? GetEnhancementImage(string name, int spriteWidth = 142, int spriteHeight = 190)
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

        // Get a composite playing card image (enhancement + card pattern)
        public IImage? GetPlayingCardImage(
            string suit,
            string rank,
            string? enhancement = null,
            string? seal = null,
            string? edition = null
        )
        {
            try
            {
                // Start with base card or enhancement
                IImage? baseCard = null;
                if (!string.IsNullOrEmpty(enhancement))
                {
                    baseCard = GetEnhancementImage(enhancement);
                }
                else
                {
                    // Use blank card as base
                    baseCard = GetSpecialImage("BlankCard");
                }

                if (baseCard == null)
                {
                    return null;
                }

                // Get the card pattern overlay
                var cardPattern = GetPlayingCardPattern(suit, rank);
                if (cardPattern == null)
                {
                    return baseCard; // Return just the base if no pattern found
                }

                // Composite the images together using Avalonia's image manipulation
                // Note: Full compositing implementation requires RenderTargetBitmap
                // For now, return the pattern overlay as the primary visual
                return cardPattern;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SpriteService",
                    $"Error creating playing card image: {ex.Message}"
                );
                return null;
            }
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
            var normalizedName = name.Trim()
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .ToLowerInvariant();
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
            return GetSpriteImage(stickerType, stickerPositions!, stickersSheet, 142, 190, "sticker");
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

            var normalizedName = blindType
                .Trim()
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .ToLowerInvariant();
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
        private IImage? GetPlayingCardPattern(string suit, string rank)
        {
            if (playingCardPositions == null || playingCardsSheet == null)
            {
                return null;
            }

            if (!playingCardPositions.TryGetValue(suit, out var suitCards))
            {
                return null;
            }

            if (!suitCards.TryGetValue(rank, out var position))
            {
                return null;
            }

            // Calculate sprite dimensions (1846x760 with 13x4 grid)
            int spriteWidth = 142; // 1846 / 13
            int spriteHeight = 190; // 760 / 4

            int x = position.Pos.X * spriteWidth;
            int y = position.Pos.Y * spriteHeight;

            return new CroppedBitmap(playingCardsSheet, new PixelRect(x, y, spriteWidth, spriteHeight));
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
                DebugLogger.LogError("SpriteService", $"Error loading stickers metadata: {ex.Message}");
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
        private static Dictionary<string, Dictionary<string, SpritePosition>> LoadPlayingCardMetadata(
            string jsonUri
        )
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
                var normalizedKey = kvp.Key
                    .Trim()
                    .Replace(" ", string.Empty, StringComparison.Ordinal)
                    .Replace("_", string.Empty, StringComparison.Ordinal)
                    .ToLowerInvariant();

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
            public int X { get; set; }
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
