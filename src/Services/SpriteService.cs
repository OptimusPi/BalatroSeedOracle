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
    private Bitmap? jokerSheet;
    private Bitmap? tagSheet;
    private Bitmap? tarotSheet;
    private Bitmap? spectralSheet;
    private Bitmap? voucherSheet;

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

            // Load spritesheets
            jokerSheet = LoadBitmap("avares://Oracle/Assets/Jokers/Jokers.png");
            tagSheet = LoadBitmap("avares://Oracle/Assets/Tags/tags.png");
            tarotSheet = LoadBitmap("avares://Oracle/Assets/Tarots/Tarots.png");
            voucherSheet = LoadBitmap("avares://Oracle/Assets/Vouchers/Vouchers.png");
            spectralSheet = tarotSheet;
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
    
    /// <summary>
    /// Get normalized variations of a name to handle mismatches between Motely enums and sprite JSON
    /// </summary>
    private List<string> GetNormalizedNames(string name)
    {
        var variations = new List<string>();
        
        // Known specific mappings
        var specificMappings = new Dictionary<string, string[]>
        {
            // Jokers
            { "OopsAll6s", new[] { "Oops_All_6s", "OopsAll6s" } },
            { "Cloud9", new[] { "Cloud_9", "Cloud9" } },
            { "EightBall", new[] { "_8Ball", "8Ball", "EightBall", "8ball" } },
            { "MrBones", new[] { "Mr_Bones", "MrBones" } },
            { "ToTheMoon", new[] { "To_the_Moon", "ToTheMoon" } },
            { "MidasMask", new[] { "Midas_Mask", "MidasMask" } },
            { "GiftCard", new[] { "Gift_Card", "GiftCard" } },
            { "TurtleBean", new[] { "Turtle_Bean", "TurtleBean" } },
            { "ReservedParking", new[] { "Reserved_Parking", "ReservedParking" } },
            { "MailInRebate", new[] { "Mail_In_Rebate", "MailInRebate" } },
            { "FortuneTeller", new[] { "Fortune_Teller", "FortuneTeller" } },
            { "LuckyCat", new[] { "Lucky_Cat", "LuckyCat" } },
            { "BaseballCard", new[] { "Baseball_Card", "BaseballCard" } },
            { "DietCola", new[] { "Diet_Cola", "DietCola" } },
            { "TradingCard", new[] { "Trading_Card", "TradingCard" } },
            { "FlashCard", new[] { "Flash_Card", "FlashCard" } },
            { "SparesTrousers", new[] { "Spare_Trousers", "SpareTrousers" } },
            { "AncientJoker", new[] { "Ancient_Joker", "AncientJoker" } },
            { "WalkieTalkie", new[] { "Walkie_Talkie", "WalkieTalkie" } },
            { "SmileyFace", new[] { "Smiley_Face", "SmileyFace" } },
            { "GoldenTicket", new[] { "Golden_Ticket", "GoldenTicket" } },
            { "SockandBuskin", new[] { "Sock_and_Buskin", "SockandBuskin" } },
            { "SmearedJoker", new[] { "Smeared_Joker", "SmearedJoker" } },
            { "HangingChad", new[] { "Hanging_Chad", "HangingChad" } },
            { "RoughGem", new[] { "Rough_Gem", "RoughGem" } },
            { "OnyxAgate", new[] { "Onyx_Agate", "OnyxAgate" } },
            { "GlassJoker", new[] { "Glass_Joker", "GlassJoker" } },
            { "FlowerPot", new[] { "Flower_Pot", "FlowerPot" } },
            { "WeeJoker", new[] { "Wee_Joker", "WeeJoker" } },
            { "MerryAndy", new[] { "Merry_Andy", "MerryAndy" } },
            { "TheIdol", new[] { "The_Idol", "TheIdol" } },
            { "SeeingDouble", new[] { "Seeing_Double", "SeeingDouble" } },
            { "HittheRoad", new[] { "Hit_the_Road", "HittheRoad" } },
            { "TheDuo", new[] { "The_Duo", "TheDuo" } },
            { "TheTrio", new[] { "The_Trio", "TheTrio" } },
            { "TheFamily", new[] { "The_Family", "TheFamily" } },
            { "TheOrder", new[] { "The_Order", "TheOrder" } },
            { "TheTribe", new[] { "The_Tribe", "TheTribe" } },
            { "InvisibleJoker", new[] { "Invisible_Joker", "InvisibleJoker" } },
            { "ShoottheMoon", new[] { "Shoot_the_Moon", "ShoottheMoon" } },
            { "DriversLicense", new[] { "Drivers_License", "DriversLicense" } },
            { "BurntJoker", new[] { "Burnt_Joker", "BurntJoker" } },
            
            // Spectrals
            { "DejaVu", new[] { "Deja_Vu", "DejaVu", "dejavu" } },
            { "Soul", new[] { "thesoul", "Soul", "The Soul", "The_Soul", "TheSoul" } },
            { "BlackHole", new[] { "blackhole", "Black Hole", "Black_Hole" } },
            
            // Tags - no underscores in JSON, all use PascalCase
            
            // Vouchers
            { "Grabber", new[] { "grabber", "Grabber" } },
            { "Palette", new[] { "palette", "Palette" } },
            { "Glut", new[] { "rerollglut", "RerollGlut", "Reroll_Glut" } },
            { "RerollGlut", new[] { "rerollglut", "RerollGlut" } },
            { "ClearanceSale", new[] { "Clearance_Sale", "ClearanceSale" } },
            { "CrystalBall", new[] { "Crystal_Ball", "CrystalBall" } },
            { "TarotMerchant", new[] { "Tarot_Merchant", "TarotMerchant" } },
            { "PlanetMerchant", new[] { "Planet_Merchant", "PlanetMerchant" } },
            { "SeedMoney", new[] { "Seed_Money", "SeedMoney" } },
            { "MagicTrick", new[] { "Magic_Trick", "MagicTrick" } },
            { "DirectorsCut", new[] { "Directors_Cut", "DirectorsCut" } },
            { "PaintBrush", new[] { "Paint_Brush", "PaintBrush" } },
            { "OverstockPlus", new[] { "Overstock_Plus", "OverstockPlus" } },
            { "GlowUp", new[] { "Glow_Up", "GlowUp" } },
            { "OmenGlobe", new[] { "Omen_Globe", "OmenGlobe" } },
            { "NachoTong", new[] { "Nacho_Tong", "NachoTong" } },
            { "TarotTycoon", new[] { "Tarot_Tycoon", "TarotTycoon", "tarotycoon" } },
            { "Tarot Tycoon", new[] { "tarotycoon", "Tarot Tycoon", "TarotTycoon" } },
            { "PlanetTycoon", new[] { "Planet_Tycoon", "PlanetTycoon" } },
            { "MoneyTree", new[] { "Money_Tree", "MoneyTree" } }
        };
        
        if (specificMappings.TryGetValue(name, out var mappings))
        {
            variations.AddRange(mappings);
        }
        
        // Generic transformations
        // Try with underscores between words (for multi-word names)
        var withUnderscores = System.Text.RegularExpressions.Regex.Replace(
            name, 
            @"(?<!^)(?=[A-Z])", 
            "_"
        );
        if (withUnderscores != name)
            variations.Add(withUnderscores);
        
        // Try prefixing numbers with underscore
        if (char.IsDigit(name[0]))
        {
            variations.Add("_" + name);
        }
        
        return variations;
    }

    /// <summary>
    /// Debug method to check what sprites are available
    /// </summary>
    public void DebugListSprites()
    {
        DebugLogger.Log("SpriteService", $"Loaded {jokerPositions.Count} jokers: {string.Join(", ", jokerPositions.Keys.Take(10))}...");
            DebugLogger.Log("SpriteService", $"Loaded {tagPositions.Count} tags: {string.Join(", ", tagPositions.Keys.Take(10))}...");
            DebugLogger.Log("SpriteService", $"Loaded {tarotPositions.Count} tarots: {string.Join(", ", tarotPositions.Keys.Take(10))}...");
            DebugLogger.Log("SpriteService", $"Loaded {spectralPositions.Count} spectrals: {string.Join(", ", spectralPositions.Keys.Take(10))}...");
            DebugLogger.Log("SpriteService", $"Loaded {voucherPositions.Count} vouchers: {string.Join(", ", voucherPositions.Keys.Take(10))}...");
    }

    /// <summary>
    /// Check if a sprite exists for the given name and type
    /// </summary>
    public bool SpriteExists(string name, string? type = null)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        if (!string.IsNullOrEmpty(type))
        {
            var positions = type.ToLower() switch
            {
                "joker" or "jokers" => jokerPositions,
                "tag" or "tags" => tagPositions,
                "tarot" or "tarots" => tarotPositions,
                "spectral" or "spectrals" => spectralPositions,
                "voucher" or "vouchers" => voucherPositions,
                _ => []
            };

            return positions.ContainsKey(name);
        }
        return false;
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