using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Oracle.Constants;
using Oracle.Helpers;

namespace Oracle.Services;

public class SpriteService
{
    private static SpriteService _instance = null!;
    public static SpriteService Instance => _instance ??= new SpriteService();

    private Dictionary<string, SpritePosition> jokerPositions = null!;
    private Dictionary<string, SpritePosition> tagPositions = null!;
    private Dictionary<string, SpritePosition> tarotPositions = null!;
    private Dictionary<string, SpritePosition> spectralPositions = null!;
    private Dictionary<string, SpritePosition> voucherPositions = null!;
    private Dictionary<string, SpritePosition> uiAssetPositions = null!;
    private Dictionary<string, SpritePosition> deckPositions = null!;
    private Dictionary<string, SpritePosition> enhancementPositions = null!;
    private Dictionary<string, SpritePosition> sealPositions = null!;
    private Dictionary<string, SpritePosition> specialPositions = null!;
    private Dictionary<string, Dictionary<string, SpritePosition>> playingCardPositions = null!;
    private Dictionary<string, SpritePosition> bossPositions = null!;
    private Dictionary<string, SpritePosition> blindPositions = null!;
    private Dictionary<string, SpritePosition> stickerPositions = null!;
    private Bitmap? jokerSheet;
    private Bitmap? tagSheet;
    private Bitmap? tarotSheet;
    private Bitmap? spectralSheet;
    private Bitmap? voucherSheet;
    private Bitmap? uiAssetsSheet;
    private Bitmap? enhancersSheet;
    private Bitmap? playingCardsSheet;
    private Bitmap? bossSheet;
    private Bitmap? stickersSheet;

    private SpriteService()
    {
        LoadAssets();
    }

    private void LoadAssets()
    {
        try
        {
            // Load joker positions from json
            jokerPositions = LoadSpritePositions("avares://Oracle/Assets/Jokers/jokers.json");

            // Load tag positions from json
            tagPositions = LoadSpritePositions("avares://Oracle/Assets/Tags/tags.json");

            // Load tarot positions from json
            tarotPositions = LoadSpritePositions("avares://Oracle/Assets/Tarots/tarots.json");

            // Load spectral positions from json
            // Load spectral positions from json (they're in the tarots sprite sheet)
            spectralPositions = LoadSpritePositions("avares://Oracle/Assets/Tarots/spectrals.json");

            // Load voucher positions from json
            voucherPositions = LoadSpritePositions("avares://Oracle/Assets/Vouchers/vouchers.json");

            // Load UI asset positions from json
            uiAssetPositions = LoadSpritePositions("avares://Oracle/Assets/Other/ui_assets.json");
            
            // Load deck, enhancement, and seal positions from enhancers metadata
            var enhancersMetadata = LoadEnhancersMetadata("avares://Oracle/Assets/Decks/enhancers_metadata.json");
            if (enhancersMetadata != null)
            {
                deckPositions = enhancersMetadata.decks;
                enhancementPositions = enhancersMetadata.enhancements;
                sealPositions = enhancersMetadata.seals;
                specialPositions = enhancersMetadata.special;
            }
            
            // Load playing card positions
            playingCardPositions = LoadPlayingCardMetadata("avares://Oracle/Assets/Decks/playing_cards_metadata.json");
            
            // Load boss blind positions
            var bossMetadata = LoadBossMetadata("avares://Oracle/Assets/Bosses/blinds_metadata.json");
            if (bossMetadata != null)
            {
                blindPositions = bossMetadata.blinds;
                bossPositions = new Dictionary<string, SpritePosition>();
                // Merge all boss types into one dictionary
                foreach (var kvp in bossMetadata.bosses)
                    bossPositions[kvp.Key.ToLower()] = kvp.Value;
                foreach (var kvp in bossMetadata.finisherBosses)
                    bossPositions[kvp.Key.ToLower()] = kvp.Value;
                foreach (var kvp in bossMetadata.special)
                    bossPositions[kvp.Key.ToLower()] = kvp.Value;
            }
            
            // Load sticker positions
            var stickerMetadata = LoadStickersMetadata("avares://Oracle/Assets/Jokers/stickers_metadata.json");
            if (stickerMetadata != null)
            {
                stickerPositions = new Dictionary<string, SpritePosition>();
                // Merge all sticker types into one dictionary
                foreach (var kvp in stickerMetadata.jokerStickers)
                    stickerPositions[kvp.Key.ToLower()] = kvp.Value;
                foreach (var kvp in stickerMetadata.stakeStickers)
                    stickerPositions[kvp.Key.ToLower()] = kvp.Value;
            }

            // Load spritesheets
            jokerSheet = LoadBitmap("avares://Oracle/Assets/Jokers/Jokers.png");
            tagSheet = LoadBitmap("avares://Oracle/Assets/Tags/tags.png");
            tarotSheet = LoadBitmap("avares://Oracle/Assets/Tarots/Tarots.png");
            voucherSheet = LoadBitmap("avares://Oracle/Assets/Vouchers/Vouchers.png");
            spectralSheet = tarotSheet;
            uiAssetsSheet = LoadBitmap("avares://Oracle/Assets/Other/ui_assets.png");
            enhancersSheet = LoadBitmap("avares://Oracle/Assets/Decks/Enhancers.png");
            playingCardsSheet = LoadBitmap("avares://Oracle/Assets/Decks/8BitDeck.png");
            bossSheet = LoadBitmap("avares://Oracle/Assets/Bosses/BlindChips.png");
            stickersSheet = LoadBitmap("avares://Oracle/Assets/Jokers/stickers.png");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SpriteService", $"Error loading sprite assets: {ex.Message}");
            // Initialize empty dictionaries to prevent null reference errors
            jokerPositions ??= new Dictionary<string, SpritePosition>();
            tagPositions ??= new Dictionary<string, SpritePosition>();
            tarotPositions ??= new Dictionary<string, SpritePosition>();
            spectralPositions ??= new Dictionary<string, SpritePosition>();
            voucherPositions ??= new Dictionary<string, SpritePosition>();
            uiAssetPositions ??= new Dictionary<string, SpritePosition>();
            deckPositions ??= new Dictionary<string, SpritePosition>();
            enhancementPositions ??= new Dictionary<string, SpritePosition>();
            sealPositions ??= new Dictionary<string, SpritePosition>();
            specialPositions ??= new Dictionary<string, SpritePosition>();
            playingCardPositions ??= new Dictionary<string, Dictionary<string, SpritePosition>>();
            bossPositions ??= new Dictionary<string, SpritePosition>();
            blindPositions ??= new Dictionary<string, SpritePosition>();
            stickerPositions ??= new Dictionary<string, SpritePosition>();
            
            // Bitmaps will remain null if loading failed
        }
    }

    private Dictionary<string, SpritePosition> LoadSpritePositions(string jsonUri)
    {
        try
        {
            var uri = new Uri(jsonUri);
            using var stream = AssetLoader.Open(uri);
            using var reader = new StreamReader(stream);
            string json = reader.ReadToEnd();
            
            var positionsList = JsonSerializer.Deserialize<List<SpritePosition>>(json);
            var positions = new Dictionary<string, SpritePosition>();
            
            foreach (var pos in positionsList ?? new List<SpritePosition>())
            {
                if (pos?.Name != null)
                {
                    positions[pos.Name.ToLower()] = pos;
                }
            }
            
            return positions;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SpriteService", $"Error loading sprite positions from {jsonUri}: {ex.Message}");
            return new Dictionary<string, SpritePosition>();
        }
    }

    private Bitmap? LoadBitmap(string bitmapUri)
    {
        try
        {
            var uri = new Uri(bitmapUri);
            return new Bitmap(AssetLoader.Open(uri));
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SpriteService", $"Error loading bitmap from {bitmapUri}: {ex.Message}");
            return null;
        }
    }

    private IImage? GetSpriteImage(string name_in, Dictionary<string, SpritePosition> positions, Bitmap? spriteSheet, int spriteWidth, int spriteHeight, string category)
    {
        if (string.IsNullOrEmpty(name_in) || positions == null || spriteSheet == null)
            return null;

        // Simple approach: just convert to lowercase and remove all spaces
        string name = name_in.Trim().Replace(" ", "").ToLower();

        // Try the normalized name
        if (positions!.TryGetValue(name, out var pos))
        {
            int x = pos.Pos.X * spriteWidth;
            int y = pos.Pos.Y * spriteHeight;
            return new CroppedBitmap(spriteSheet, new PixelRect(x, y, spriteWidth, spriteHeight));
        }
        else
        {
            DebugLogger.Log("SpriteService", $"INFO: Could not find sprite for {category} '{name_in}' (tried: '{name}')");
            return null;
        }
    }

    public IImage? GetJokerImage(string name, int spriteWidth = UIConstants.JokerSpriteWidth, int spriteHeight = UIConstants.JokerSpriteHeight)
    {
        // Special handling for "any" soul joker
        if (name.ToLower() == "any")
        {
            // Return a special "any" icon or the first legendary joker as a placeholder
            // For now, use Perkeo as the representative for "any" soul joker
            return GetSpriteImage("perkeo", jokerPositions, jokerSheet, spriteWidth, spriteHeight, "joker");
        }
        return GetSpriteImage(name, jokerPositions, jokerSheet, spriteWidth, spriteHeight, "joker");
    }
    
    public IImage? GetJokerSoulImage(string name_in, int spriteWidth = UIConstants.JokerSpriteWidth, int spriteHeight = UIConstants.JokerSpriteHeight)
    {
        var name = name_in.Trim().Replace(" ", "").Replace("_", "").ToLower();
        Oracle.Helpers.DebugLogger.LogImportant("GetJokerSoulImage", $"🎴 SOUL IMAGE REQUEST - Input: '{name_in}', Normalized: '{name}'");
        
        // For legendary jokers, the soul is one row below (y+1)
        if (jokerPositions == null || jokerSheet == null || !jokerPositions.TryGetValue(name, out var basePos))
        {
            Oracle.Helpers.DebugLogger.LogImportant("GetJokerSoulImage", $"🎴 Failed to find position for: {name}. Available positions: {string.Join(", ", jokerPositions?.Keys.Where(k => k.Contains(name.Substring(0, Math.Min(3, name.Length)))).Take(5) ?? new List<string>())}");
            return null;
        }
            
        // Create a new position one row below
        var soulPos = new SpritePosition 
        { 
            Name = name + "_soul", 
            Pos = new Pos { X = basePos.Pos.X, Y = basePos.Pos.Y + 1 } 
        };
        
        int x = soulPos.Pos.X * spriteWidth;
        int y = soulPos.Pos.Y * spriteHeight;
        
        // Validate coordinates
        Oracle.Helpers.DebugLogger.LogImportant("GetJokerSoulImage", $"🎴 Sheet dimensions: {jokerSheet.PixelSize.Width}x{jokerSheet.PixelSize.Height}");
        Oracle.Helpers.DebugLogger.LogImportant("GetJokerSoulImage", $"🎴 Trying to crop at ({x}, {y}) with size {spriteWidth}x{spriteHeight}");
        Oracle.Helpers.DebugLogger.LogImportant("GetJokerSoulImage", $"🎴 Bottom-right corner would be at ({x + spriteWidth}, {y + spriteHeight})");
        
        if (x >= 0 && y >= 0 && x + spriteWidth <= jokerSheet.PixelSize.Width && y + spriteHeight <= jokerSheet.PixelSize.Height)
        {
            Oracle.Helpers.DebugLogger.LogImportant("GetJokerSoulImage", $"🎴 SUCCESS - Creating soul image at ({x}, {y}) for {name}");
            return new CroppedBitmap(jokerSheet, new PixelRect(x, y, spriteWidth, spriteHeight));
        }
        
        Oracle.Helpers.DebugLogger.LogImportant("GetJokerSoulImage", $"🎴 FAILED - Invalid coordinates ({x}, {y}) for {name}");
        return null;
    }

    public IImage? GetTagImage(string name, int spriteWidth = UIConstants.TagSpriteWidth, int spriteHeight = UIConstants.TagSpriteHeight)
    {
        return GetSpriteImage(name, tagPositions, tagSheet, spriteWidth, spriteHeight, "tag");
    }

    public IImage? GetTarotImage(string name, int spriteWidth = UIConstants.TarotSpriteWidth, int spriteHeight = UIConstants.TarotSpriteHeight)
    {
        return GetSpriteImage(name, tarotPositions, tarotSheet, spriteWidth, spriteHeight, "tarot");
    }

    public IImage? GetSpectralImage(string name, int spriteWidth = UIConstants.SpectralSpriteWidth, int spriteHeight = UIConstants.SpectralSpriteHeight)
    {
        return GetSpriteImage(name, spectralPositions, spectralSheet, spriteWidth, spriteHeight, "spectral");
    }

    public IImage? GetVoucherImage(string name, int spriteWidth = UIConstants.VoucherSpriteWidth, int spriteHeight = UIConstants.VoucherSpriteHeight)
    {
        return GetSpriteImage(name, voucherPositions, voucherSheet, spriteWidth, spriteHeight, "voucher");
    }

    /// <summary>
    /// Get an image for any type of item - automatically determines the type
    /// </summary>
    public IImage? GetItemImage(string name, string? type = null)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        // If type is specified, use it directly
        if (!string.IsNullOrEmpty(type))
        {
            return type.ToLower() switch
            {
                "joker" or "jokers" => GetJokerImage(name),
                "tag" or "tags" => GetTagImage(name),
                "tarot" or "tarots" => GetTarotImage(name),
                "spectral" or "spectrals" => GetSpectralImage(name),
                "voucher" or "vouchers" => GetVoucherImage(name),
                "boss" or "bosses" => GetBossImage(name),
                "blind" or "blinds" => GetBlindImage(name),
                "sticker" or "stickers" => GetStickerImage(name),
                _ => null
            };
        }

        // Normalize the name for lookup
        var normalizedName = name.Trim().Replace(" ", "").Replace("_", "").ToLower();
        
        // Check jokers first (most common)
        if (jokerPositions.ContainsKey(normalizedName))
            return GetJokerImage(name);

        // Check tags
        if (tagPositions.ContainsKey(normalizedName))
            return GetTagImage(name);

        // Check tarots
        if (tarotPositions.ContainsKey(normalizedName))
            return GetTarotImage(name);

        // Check spectrals
        if (spectralPositions.ContainsKey(normalizedName))
            return GetSpectralImage(name);

        // Check vouchers
        if (voucherPositions.ContainsKey(normalizedName))
            return GetVoucherImage(name);

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
                var editionsUri = "avares://Oracle/Assets/Jokers/Editions.png";
                editionsSheet = LoadBitmap(editionsUri);
                if (editionsSheet != null)
                {
                    DebugLogger.Log("SpriteService", "Loaded editions sprite sheet");
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
            int position = edition.ToLower() switch
            {
                "none" or "normal" => 0,
                "foil" => 1,
                "holographic" or "holo" => 2,
                "polychrome" or "poly" => 3,
                "negative" or "neg" => 4,  // Position 4 is Negative (not debuffed/red X)
                _ => 0
            };
            
            int x = position * spriteWidth;
            
            if (x + spriteWidth <= editionsSheet.PixelSize.Width && spriteHeight <= editionsSheet.PixelSize.Height)
            {
                return new CroppedBitmap(editionsSheet, new PixelRect(x, 0, spriteWidth, spriteHeight));
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SpriteService", $"Failed to get edition image: {ex.Message}");
        }
        
        return null;
    }
    
    public IImage? GetUIAssetImage(string name, int spriteWidth = 18, int spriteHeight = 18)
    {
        return GetSpriteImage(name, uiAssetPositions, uiAssetsSheet, spriteWidth, spriteHeight, "ui_asset");
    }
    
    // New methods for deck, enhancement, and seal sprites
    public IImage? GetDeckImage(string name, int spriteWidth = 142, int spriteHeight = 190)
    {
        return GetSpriteImage(name, deckPositions, enhancersSheet, spriteWidth, spriteHeight, "deck");
    }
    
    public IImage? GetEnhancementImage(string name, int spriteWidth = 142, int spriteHeight = 190)
    {
        return GetSpriteImage(name, enhancementPositions, enhancersSheet, spriteWidth, spriteHeight, "enhancement");
    }
    
    public IImage? GetSealImage(string name, int spriteWidth = 142, int spriteHeight = 190)
    {
        return GetSpriteImage(name, sealPositions, enhancersSheet, spriteWidth, spriteHeight, "seal");
    }
    
    public IImage? GetSpecialImage(string name, int spriteWidth = 142, int spriteHeight = 190)
    {
        // Special images like Soul gem, mystery icons, etc.
        return GetSpriteImage(name, specialPositions, enhancersSheet, spriteWidth, spriteHeight, "special");
    }
    
    // Get a composite playing card image (enhancement + card pattern)
    public IImage? GetPlayingCardImage(string suit, string rank, string? enhancement = null, string? seal = null, string? edition = null)
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
            
            if (baseCard == null) return null;
            
            // Get the card pattern overlay
            var cardPattern = GetPlayingCardPattern(suit, rank);
            if (cardPattern == null) return baseCard; // Return just the base if no pattern found
            
            // TODO: Composite the images together
            // For now, just return the pattern (this needs proper image compositing)
            return cardPattern;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SpriteService", $"Error creating playing card image: {ex.Message}");
            return null;
        }
    }
    
    // Get boss blind image (first frame of animation, similar size to tags)
    public IImage? GetBossImage(string name, int frameIndex = 0)
    {
        if (bossPositions == null || bossSheet == null)
        {
            DebugLogger.LogError("SpriteService", $"Boss positions or sheet not loaded for: {name}");
            return null;
        }
        
        var normalizedName = name.Trim().Replace(" ", "").ToLower();
        if (!bossPositions.TryGetValue(normalizedName, out var position))
        {
            DebugLogger.LogError("SpriteService", $"Boss position not found for: {name} (normalized: {normalizedName})");
            DebugLogger.Log("SpriteService", $"Available boss names: {string.Join(", ", bossPositions.Keys)}");
            return null;
        }
        
        // Boss blind sprites are 34x34, with 21 frames per row
        int spriteWidth = 34;
        int spriteHeight = 34;
        
        // Use the specified frame (0-20)
        int x = (position.Pos.X + frameIndex) * spriteWidth;
        int y = position.Pos.Y * spriteHeight;
        
        return new CroppedBitmap(bossSheet, new PixelRect(x, y, spriteWidth, spriteHeight));
    }
    
    // Get sticker image (Eternal, Perishable, Rental, or stake stickers)
    public IImage? GetStickerImage(string stickerType)
    {
        return GetSpriteImage(stickerType, stickerPositions, stickersSheet, 142, 190, "sticker");
    }
    
    // Get blind chip image (small/big blind indicators)
    public IImage? GetBlindImage(string blindType, int frameIndex = 0)
    {
        if (blindPositions == null || bossSheet == null) return null;
        
        var normalizedName = blindType.Trim().Replace(" ", "").ToLower();
        if (!blindPositions.TryGetValue(normalizedName, out var position)) return null;
        
        // Same dimensions as boss sprites
        int spriteWidth = 34;
        int spriteHeight = 34;
        
        int x = (position.Pos.X + frameIndex) * spriteWidth;
        int y = position.Pos.Y * spriteHeight;
        
        return new CroppedBitmap(bossSheet, new PixelRect(x, y, spriteWidth, spriteHeight));
    }
    
    // Get just the playing card pattern (suit/rank)
    private IImage? GetPlayingCardPattern(string suit, string rank)
    {
        if (playingCardPositions == null || playingCardsSheet == null) return null;
        
        if (!playingCardPositions.TryGetValue(suit, out var suitCards)) return null;
        if (!suitCards.TryGetValue(rank, out var position)) return null;
        
        // Calculate sprite dimensions (1846x760 with 13x4 grid)
        int spriteWidth = 142;  // 1846 / 13
        int spriteHeight = 190; // 760 / 4
        
        int x = position.Pos.X * spriteWidth;
        int y = position.Pos.Y * spriteHeight;
        
        return new CroppedBitmap(playingCardsSheet, new PixelRect(x, y, spriteWidth, spriteHeight));
    }
    
    // Helper method to load stickers metadata
    private StickersMetadata? LoadStickersMetadata(string jsonUri)
    {
        try
        {
            var uri = new Uri(jsonUri);
            using var stream = AssetLoader.Open(uri);
            using var reader = new StreamReader(stream);
            string json = reader.ReadToEnd();
            
            var metadata = JsonSerializer.Deserialize<StickersMetadataJson>(json);
            if (metadata == null) return null;
            
            var result = new StickersMetadata
            {
                jokerStickers = ConvertToSpritePositions(metadata.sprites?.jokerStickers),
                stakeStickers = ConvertToSpritePositions(metadata.sprites?.stakeStickers)
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
    private BossMetadata? LoadBossMetadata(string jsonUri)
    {
        try
        {
            var uri = new Uri(jsonUri);
            using var stream = AssetLoader.Open(uri);
            using var reader = new StreamReader(stream);
            string json = reader.ReadToEnd();
            
            var metadata = JsonSerializer.Deserialize<BossMetadataJson>(json);
            if (metadata == null) return null;
            
            var result = new BossMetadata
            {
                blinds = ConvertToSpritePositions(metadata.sprites?.blinds),
                bosses = ConvertToSpritePositions(metadata.sprites?.bosses),
                finisherBosses = ConvertToSpritePositions(metadata.sprites?.finisherBosses),
                special = ConvertToSpritePositions(metadata.sprites?.special)
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
    private Dictionary<string, Dictionary<string, SpritePosition>> LoadPlayingCardMetadata(string jsonUri)
    {
        try
        {
            var uri = new Uri(jsonUri);
            using var stream = AssetLoader.Open(uri);
            using var reader = new StreamReader(stream);
            string json = reader.ReadToEnd();
            
            var metadata = JsonSerializer.Deserialize<PlayingCardMetadataJson>(json);
            if (metadata?.sprites == null) return new();
            
            var result = new Dictionary<string, Dictionary<string, SpritePosition>>();
            
            foreach (var suitKvp in metadata.sprites)
            {
                var suitPositions = new Dictionary<string, SpritePosition>();
                foreach (var rankKvp in suitKvp.Value)
                {
                    suitPositions[rankKvp.Key] = new SpritePosition
                    {
                        Name = $"{rankKvp.Key} of {suitKvp.Key}",
                        Pos = new Pos { X = rankKvp.Value.x, Y = rankKvp.Value.y }
                    };
                }
                result[suitKvp.Key] = suitPositions;
            }
            
            return result;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SpriteService", $"Error loading playing card metadata: {ex.Message}");
            return new();
        }
    }
    
    // Helper method to load enhancers metadata with custom structure
    private EnhancersMetadata? LoadEnhancersMetadata(string jsonUri)
    {
        try
        {
            var uri = new Uri(jsonUri);
            using var stream = AssetLoader.Open(uri);
            using var reader = new StreamReader(stream);
            string json = reader.ReadToEnd();
            
            var metadata = JsonSerializer.Deserialize<EnhancersMetadataJson>(json);
            if (metadata == null) return null;
            
            // Convert the JSON structure to SpritePosition dictionaries
            var result = new EnhancersMetadata
            {
                decks = ConvertToSpritePositions(metadata.sprites?.decks),
                enhancements = ConvertToSpritePositions(metadata.sprites?.enhancements),
                seals = ConvertToSpritePositions(metadata.sprites?.seals),
                special = ConvertToSpritePositions(metadata.sprites?.special)
            };
            
            return result;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SpriteService", $"Error loading enhancers metadata: {ex.Message}");
            return null;
        }
    }
    
    private Dictionary<string, SpritePosition> ConvertToSpritePositions(Dictionary<string, EnhancerSprite>? sprites)
    {
        var result = new Dictionary<string, SpritePosition>();
        if (sprites == null) return result;
        
        foreach (var kvp in sprites)
        {
            result[kvp.Key.ToLower()] = new SpritePosition
            {
                Name = kvp.Key,
                Pos = new Pos { X = kvp.Value.x, Y = kvp.Value.y }
            };
        }
        
        return result;
    }
    
    // Internal classes for metadata deserialization
    private class EnhancersMetadata
    {
        public Dictionary<string, SpritePosition> decks { get; set; } = new();
        public Dictionary<string, SpritePosition> enhancements { get; set; } = new();
        public Dictionary<string, SpritePosition> seals { get; set; } = new();
        public Dictionary<string, SpritePosition> special { get; set; } = new();
    }
    
    private class EnhancersMetadataJson
    {
        public EnhancersSprites? sprites { get; set; }
    }
    
    private class EnhancersSprites
    {
        public Dictionary<string, EnhancerSprite>? decks { get; set; }
        public Dictionary<string, EnhancerSprite>? enhancements { get; set; }
        public Dictionary<string, EnhancerSprite>? seals { get; set; }
        public Dictionary<string, EnhancerSprite>? special { get; set; }
    }
    
    private class EnhancerSprite
    {
        public int x { get; set; }
        public int y { get; set; }
        public string? description { get; set; }
    }
    
    private class PlayingCardMetadataJson
    {
        public Dictionary<string, Dictionary<string, CardPosition>>? sprites { get; set; }
    }
    
    private class CardPosition
    {
        public int x { get; set; }
        public int y { get; set; }
    }
    
    private class BossMetadata
    {
        public Dictionary<string, SpritePosition> blinds { get; set; } = new();
        public Dictionary<string, SpritePosition> bosses { get; set; } = new();
        public Dictionary<string, SpritePosition> finisherBosses { get; set; } = new();
        public Dictionary<string, SpritePosition> special { get; set; } = new();
    }
    
    private class BossMetadataJson
    {
        public BossSprites? sprites { get; set; }
    }
    
    private class BossSprites
    {
        public Dictionary<string, EnhancerSprite>? blinds { get; set; }
        public Dictionary<string, EnhancerSprite>? bosses { get; set; }
        public Dictionary<string, EnhancerSprite>? finisherBosses { get; set; }
        public Dictionary<string, EnhancerSprite>? special { get; set; }
    }
    
    private class StickersMetadata
    {
        public Dictionary<string, SpritePosition> jokerStickers { get; set; } = new();
        public Dictionary<string, SpritePosition> stakeStickers { get; set; } = new();
    }
    
    private class StickersMetadataJson
    {
        public StickersSprites? sprites { get; set; }
    }
    
    private class StickersSprites
    {
        public Dictionary<string, EnhancerSprite>? jokerStickers { get; set; }
        public Dictionary<string, EnhancerSprite>? stakeStickers { get; set; }
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