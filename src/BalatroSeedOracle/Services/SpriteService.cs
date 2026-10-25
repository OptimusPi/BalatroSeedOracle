using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Json;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Loads and caches Balatro sprite images (jokers, tarots, planets, spectrals, vouchers, tags,
/// boss blinds, booster packs, decks, stakes, editions, seals, enhancements, stickers and
/// playing cards) from the atlases under <c>Assets/</c> (embedded as AvaloniaResource, read via
/// <c>avares://</c>).
///
/// Lookups are tolerant: names are compared case-insensitively with spaces, underscores,
/// punctuation and a trailing <c>#n</c> uniqueness suffix ignored, so engine enum names
/// ("TheEye", "OopsAll6s"), display names ("The Eye", "Oops! All 6s") and asset keys
/// ("theeye") all resolve. Each category also tries the obvious variants (with/without a
/// leading "The", a trailing "Joker"/"Tag"/"Pack"/"Deck"/"Stake"/"Seal").
///
/// Atlases load lazily and synchronously on first use (a few small PNGs), so a binding that
/// fires before <see cref="PreloadAllSpritesAsync"/> finishes still gets its image. Crops and
/// composites are cached, so the same name always returns the same <see cref="IImage"/>.
/// A name that matches nothing returns null and is logged once.
/// </summary>
public class SpriteService
{
    public static SpriteService Instance { get; } = new();

    private const string AssetRoot = "avares://BalatroSeedOracle/Assets/";

    /// <summary>Full-size card canvas used by the 2x atlases (Enhancers, 8BitDeck, stickers).</summary>
    private static readonly PixelSize CardCanvas = new(142, 190);

    /// <summary>Legendary jokers whose floating face sits one row below the card in Jokers.png.</summary>
    private static readonly HashSet<string> LegendaryJokers = new(StringComparer.Ordinal)
    {
        "canio",
        "triboulet",
        "yorick",
        "chicot",
        "perkeo",
    };

    private static readonly string[] Suits = ["hearts", "clubs", "diamonds", "spades"];

    private readonly Lock _loadLock = new();
    private volatile Catalog? _catalog;
    private readonly ConcurrentDictionary<string, IImage> _cache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _reportedMisses = new(StringComparer.Ordinal);
    private IImage? _transparentPixel;

    /// <summary>
    /// Loads every atlas and its metadata off the UI thread so the first lookup doesn't pay for
    /// the PNG decodes. Safe to call more than once and safe to skip (lookups load lazily).
    /// </summary>
    public Task PreloadAllSpritesAsync(CancellationToken? cancellationToken = null) =>
        Task.Run(() => EnsureLoaded(), cancellationToken ?? CancellationToken.None);

    // ------------------------------------------------------------------ generic item lookup

    /// <summary>Image for any item, detecting its category from the name.</summary>
    public IImage? GetItemImage(string itemType) => GetItemImage(itemType, null);

    /// <summary>
    /// Image for any item. <paramref name="typeHint"/> ("Joker", "Tag", "Voucher", "Tarot",
    /// "Planet", "Spectral", "Boss", "Booster", "Deck", "Stake", "Enhancement", "Seal",
    /// "Sticker", "Edition", "StandardCard", ...) picks the atlas; without one, every category
    /// is searched. Arguments passed as (type, name) are detected and swapped.
    /// </summary>
    public IImage? GetItemImage(string itemName, string? typeHint)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return null;

        var type = typeHint is null ? string.Empty : Normalize(typeHint);
        if (type.Length > 0 && !IsTypeWord(type) && IsTypeWord(Normalize(itemName)))
        {
            (itemName, typeHint) = (typeHint!, itemName);
            type = Normalize(typeHint);
        }

        if (IsNone(itemName))
            return TransparentPixel();

        // "Any" / "*" with a category goes to that category's wildcard sprite (anyjoker, anytag...).
        switch (type)
        {
            case "joker" or "jokers" or "souljoker" or "legendaryjoker" or "wildcardjoker":
                return GetJokerImage(itemName);
            case "tag" or "tags" or "smallblindtag" or "bigblindtag" or "skiptag":
                return GetTagImage(itemName);
            case "voucher" or "vouchers":
                return GetVoucherImage(itemName);
            case "tarot" or "tarots" or "tarotcard":
                return GetTarotImage(itemName);
            case "planet" or "planets" or "planetcard":
                return GetPlanetCardImage(itemName);
            case "spectral" or "spectrals" or "spectralcard":
                return GetSpectralImage(itemName);
            case "boss" or "bosses" or "bossblind" or "blind" or "blinds":
                return GetBossImage(itemName);
            case "booster" or "boosters" or "boosterpack" or "pack" or "packs":
                return GetBoosterImage(itemName);
            case "deck" or "decks":
                return GetDeckImage(itemName);
            case "stake" or "stakes":
                return GetStakeImage(itemName);
            case "enhancement" or "enhancements":
                return GetEnhancementImage(itemName);
            case "seal" or "seals":
                return GetSealImage(itemName);
            case "sticker" or "stickers":
                return GetStickerImage(itemName);
            case "edition" or "editions":
                return GetEditionImage(itemName);
            case "standardcard" or "playingcard" or "card":
                return TryParsePlayingCard(itemName, out var suit, out var rank)
                    ? GetPlayingCardImage(suit, rank)
                    : GetPlayingCardImage("Spades", "Ace");
        }

        // No (usable) hint. "Any" / "*" alone names no category: blank placeholder.
        if (IsPlaceholder(itemName))
            return TransparentPixel();

        // Search every category, quietly, then report one miss.
        var c = EnsureLoaded();
        var n = Normalize(itemName);
        var found =
            ResolveJoker(c, n, negative: false)
            ?? ResolveConsumable(c, n, c.Tarots)
            ?? Crop(c.Vouchers, VoucherKeys(n))
            ?? Crop(c.Tags, TagKeys(n))
            ?? ResolveBoss(c, n)
            ?? Crop(c.Boosters, BoosterKeys(n))
            ?? Crop(c.Decks, DeckKeys(n))
            ?? Crop(c.Stakes, StakeKeys(n))
            ?? Crop(c.Enhancements, EnhancementKeys(n));
        if (found is null && TryParsePlayingCard(itemName, out var s2, out var r2))
            found = GetPlayingCardImage(s2, r2);
        return found ?? Miss("item", itemName);
    }

    // ------------------------------------------------------------------ jokers

    /// <summary>
    /// Joker card. <paramref name="edition"/>: "Negative" uses the pre-inverted negative atlas;
    /// "Foil" / "Holographic" / "Polychrome" layer the edition overlay on top. Legendary jokers
    /// (and Hologram) come with their floating face composited on.
    /// </summary>
    public IImage? GetJokerImage(string jokerName, string? edition = null)
    {
        if (string.IsNullOrWhiteSpace(jokerName))
            return null;
        var c = EnsureLoaded();
        var n = Normalize(jokerName);
        var ed = edition is null ? string.Empty : Normalize(edition);
        var negative = ed is "negative";

        var card = ResolveJoker(c, n, negative);
        if (card is null)
            return Miss("joker", jokerName);

        var overlayCell = EditionCell(ed);
        if (overlayCell <= 0 || negative)
            return card;
        var overlay = Crop(c.Editions, overlayCell, 0);
        if (overlay is null)
            return card;
        var size = PixelSizeOf(card);
        return Compose($"joker+ed:{n}:{ed}", size, card, overlay);
    }

    /// <summary>
    /// The floating face of a legendary joker (or Hologram). Null for jokers that have none,
    /// which is expected: callers ask for every joker.
    /// </summary>
    public IImage? GetJokerSoulImage(string jokerName)
    {
        if (string.IsNullOrWhiteSpace(jokerName))
            return null;
        var c = EnsureLoaded();
        return SoulFace(c.Jokers, Normalize(jokerName));
    }

    /// <summary>The base Joker with an Eternal / Perishable / Rental (or stake) sticker on it.</summary>
    public IImage? GetJokerWithStickerImage(string stickerName)
    {
        var joker = GetJokerImage("Joker");
        var sticker = GetStickerImage(stickerName);
        if (joker is null || sticker is null)
            return joker;
        return Compose($"joker+sticker:{Normalize(stickerName)}", CardCanvas, joker, sticker);
    }

    // ------------------------------------------------------------------ consumables

    /// <summary>Tarot card. Planet and spectral names also resolve (same atlas).</summary>
    public IImage? GetTarotImage(string tarotName) => Consumable("tarot", tarotName, c => c.Tarots);

    /// <summary>Spectral card ("TheSoul" → The Soul). Tarot/planet names also resolve.</summary>
    public IImage? GetSpectralImage(string spectralName) =>
        Consumable("spectral", spectralName, c => c.Spectrals);

    /// <summary>Planet card. Tarot/spectral names also resolve.</summary>
    public IImage? GetPlanetCardImage(string planetName) =>
        Consumable("planet", planetName, c => c.Planets);

    // ------------------------------------------------------------------ vouchers, tags, bosses, packs

    public IImage? GetVoucherImage(string voucherName)
    {
        if (string.IsNullOrWhiteSpace(voucherName))
            return null;
        var c = EnsureLoaded();
        return Crop(c.Vouchers, VoucherKeys(Normalize(voucherName))) ?? Miss("voucher", voucherName);
    }

    public IImage? GetTagImage(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            return null;
        var c = EnsureLoaded();
        return Crop(c.Tags, TagKeys(Normalize(tagName))) ?? Miss("tag", tagName);
    }

    /// <summary>Boss blind chip (first animation frame). "SmallBlind"/"BigBlind" give the blind chips.</summary>
    public IImage? GetBossImage(string bossName)
    {
        if (string.IsNullOrWhiteSpace(bossName))
            return null;
        var c = EnsureLoaded();
        return ResolveBoss(c, Normalize(bossName)) ?? Miss("boss", bossName);
    }

    /// <summary>Booster pack: pack type ("Arcana"), pack ("JumboBuffoon") or display name ("Mega Arcana Pack").</summary>
    public IImage? GetBoosterImage(string packName)
    {
        if (string.IsNullOrWhiteSpace(packName))
            return null;
        var c = EnsureLoaded();
        return Crop(c.Boosters, BoosterKeys(Normalize(packName))) ?? Miss("booster", packName);
    }

    // ------------------------------------------------------------------ decks and stakes

    /// <summary>Deck back ("Red", "Red Deck", "Erratic"). Blank name → Red.</summary>
    public IImage? GetDeckImage(string deckName)
    {
        var c = EnsureLoaded();
        var n = string.IsNullOrWhiteSpace(deckName) ? "red" : Normalize(deckName);
        return Crop(c.Decks, DeckKeys(n)) ?? Miss("deck", deckName);
    }

    /// <summary>
    /// Deck back. <paramref name="width"/>/<paramref name="height"/> are the caller's display
    /// size; the image is always the full 142x190 card and the Image control scales it.
    /// </summary>
    public IImage? GetDeckImage(string deckName, int width, int height) => GetDeckImage(deckName);

    /// <summary>Stake chip ("White", "White Stake", "WhiteStake").</summary>
    public IImage? GetStakeImage(string stakeName)
    {
        var c = EnsureLoaded();
        var n = string.IsNullOrWhiteSpace(stakeName) ? "white" : Normalize(stakeName);
        return Crop(c.Stakes, StakeKeys(n)) ?? Miss("stake", stakeName);
    }

    /// <summary>Deck back with the stake's sticker on it (blank stake → White).</summary>
    public IImage? GetDeckWithStakeSticker(string deckName, string? stakeName = null)
    {
        var deck = GetDeckImage(deckName);
        if (deck is null)
            return null;
        var stake = string.IsNullOrWhiteSpace(stakeName) ? "white" : Normalize(stakeName);
        var sticker = GetStickerImage(stake);
        if (sticker is null)
            return deck;
        var deckKey = string.IsNullOrWhiteSpace(deckName) ? "red" : Normalize(deckName);
        return Compose($"deck+stake:{deckKey}:{stake}", CardCanvas, deck, sticker);
    }

    // ------------------------------------------------------------------ playing cards

    /// <summary>
    /// A playing card: enhancement (or the blank base) + rank/suit face, then the edition
    /// overlay and the seal. Stone cards have no face. Accepts "Hearts"/"H", "10"/"Ten"/"T",
    /// "King"/"K", etc. Negative has no effect: playing cards can't be Negative in Balatro.
    /// </summary>
    public IImage? GetPlayingCardImage(
        string suit,
        string rank,
        string? enhancement = null,
        string? seal = null,
        string? edition = null
    )
    {
        var c = EnsureLoaded();
        var enh = IsNone(enhancement) ? string.Empty : Normalize(enhancement!);
        var sl = IsNone(seal) ? string.Empty : Normalize(seal!);
        var ed = IsNone(edition) ? string.Empty : Normalize(edition!);
        var isStone = enh is "stone" or "stonecard";

        IImage? face = null;
        string faceKey = "stone";
        if (!isStone)
        {
            var s = CanonicalSuit(suit);
            var r = CanonicalRank(rank);
            if (s is null || r is null)
                return Miss("playing card", $"{rank} of {suit}");
            faceKey = $"{s}|{r}";
            face = Crop(c.PlayingCards, faceKey);
            if (face is null)
                return Miss("playing card", $"{rank} of {suit}");
        }

        var baseCard = enh.Length > 0
            ? Crop(c.Enhancements, EnhancementKeys(enh)) ?? Miss("enhancement", enhancement!)
            : Crop(c.Special, "standardcardbase");
        if (baseCard is null)
            return face;

        var layers = new List<IImage> { baseCard };
        if (face is not null)
            layers.Add(face);
        var edCell = EditionCell(ed);
        if (edCell > 0 && Crop(c.Editions, edCell, 0) is { } overlay)
            layers.Add(overlay);
        if (sl.Length > 0)
        {
            if (Crop(c.Seals, SealKeys(sl)) is { } sealImage)
                layers.Add(sealImage);
            else
                Miss("seal", seal!);
        }

        if (layers.Count == 1)
            return baseCard;
        return Compose($"card:{faceKey}:{enh}:{ed}:{sl}", CardCanvas, [.. layers]);
    }

    // ------------------------------------------------------------------ overlays

    /// <summary>
    /// Edition overlay from Editions.png: "None", "Foil", "Holographic", "Polychrome",
    /// "Debuffed" (the red X). Returns null for "Negative": in Balatro negative is a shader,
    /// not a sprite, so there is no overlay for it (use <see cref="GetJokerImage"/> with
    /// edition "Negative" for the inverted joker art).
    /// </summary>
    public IImage? GetEditionImage(string editionName)
    {
        if (string.IsNullOrWhiteSpace(editionName))
            return null;
        var n = Normalize(editionName);
        if (n is "negative")
            return null;
        var cell = EditionCell(n);
        if (cell < 0)
            return Miss("edition", editionName);
        return Crop(EnsureLoaded().Editions, cell, 0);
    }

    /// <summary>Seal overlay ("Gold", "Red Seal", "Purple", "Blue").</summary>
    public IImage? GetSealImage(string sealName)
    {
        if (string.IsNullOrWhiteSpace(sealName))
            return null;
        return Crop(EnsureLoaded().Seals, SealKeys(Normalize(sealName))) ?? Miss("seal", sealName);
    }

    /// <summary>Enhanced card base ("Bonus", "Mult", "Wild", "Glass", "Steel", "Stone", "Gold", "Lucky").</summary>
    public IImage? GetEnhancementImage(string enhancementName)
    {
        if (string.IsNullOrWhiteSpace(enhancementName))
            return null;
        return Crop(EnsureLoaded().Enhancements, EnhancementKeys(Normalize(enhancementName)))
            ?? Miss("enhancement", enhancementName);
    }

    /// <summary>Card sticker overlay: "Eternal", "Perishable", "Rental", or a stake ("WhiteStake", "Gold").</summary>
    public IImage? GetStickerImage(string stickerName)
    {
        if (string.IsNullOrWhiteSpace(stickerName))
            return null;
        return Crop(EnsureLoaded().Stickers, StickerKeys(Normalize(stickerName)))
            ?? Miss("sticker", stickerName);
    }

    /// <summary>The gem that floats over The Soul spectral card.</summary>
    public IImage? GetSoulGemImage() => Crop(EnsureLoaded().Special, "thesoulgem");

    /// <summary>The "?" joker silhouette used for wildcard jokers.</summary>
    public IImage? GetMysteryJokerFaceImage() => Crop(EnsureLoaded().Special, "mysteryjokerface");

    // ================================================================== resolution helpers

    private IImage? Consumable(string category, string name, Func<Catalog, Atlas?> preferred)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;
        var c = EnsureLoaded();
        return ResolveConsumable(c, Normalize(name), preferred(c)) ?? Miss(category, name);
    }

    /// <summary>Tarots, planets and spectrals share one sheet; try the preferred set first.</summary>
    private IImage? ResolveConsumable(Catalog c, string n, Atlas? preferred)
    {
        if (IsAny(n))
            return Crop(preferred, "any" + preferred?.Category);

        foreach (var atlas in new[] { preferred, c.Tarots, c.Planets, c.Spectrals }.Distinct())
        {
            var hit = Crop(atlas, ConsumableKeys(n));
            if (hit is not null)
                return hit;
        }
        return null;
    }

    private static IEnumerable<string> ConsumableKeys(string n)
    {
        yield return n;
        if (n.StartsWith("the", StringComparison.Ordinal) && n.Length > 3)
            yield return n[3..]; // TheSoul -> soul
        else
            yield return "the" + n; // Wheel of Fortune -> thewheeloffortune
        if (n is "judgment")
            yield return "judgement";
        if (n.EndsWith("card", StringComparison.Ordinal) && n.Length > 4)
            yield return n[..^4];
    }

    private IImage? ResolveJoker(Catalog c, string n, bool negative)
    {
        var atlas = negative ? c.JokersNegative : c.Jokers;
        if (atlas is null)
            return null;
        if (IsAny(n))
            n = "anyjoker";

        var key = JokerKeys(n).FirstOrDefault(atlas.Cells.ContainsKey);
        if (key is null)
            return null;

        var card = Crop(atlas, key);
        if (card is null)
            return null;
        var face = SoulFace(atlas, key);
        if (face is null)
            return card;
        return Compose($"joker+soul:{atlas.Name}:{key}", PixelSizeOf(card), card, face);
    }

    private static IEnumerable<string> JokerKeys(string n)
    {
        yield return n;
        if (n.EndsWith("joker", StringComparison.Ordinal) && n.Length > 5)
            yield return n[..^5];
        else
            yield return n + "joker";
        if (n.StartsWith("the", StringComparison.Ordinal))
            yield return n[3..];
        else
            yield return "the" + n; // Idol -> theidol
        switch (n)
        {
            case "8ball":
                yield return "eightball";
                break;
            case "caino":
                yield return "canio";
                break;
            case "oopsallsixes":
                yield return "oopsall6s";
                break;
            case "cloudnine":
                yield return "cloud9";
                break;
        }
    }

    /// <summary>Soul face cell: one row below a legendary joker; Hologram's face is at (2,9).</summary>
    private IImage? SoulFace(Atlas? atlas, string n)
    {
        if (atlas is null)
            return null;
        var key = JokerKeys(n).FirstOrDefault(atlas.Cells.ContainsKey);
        if (key is null)
            return null;
        if (key == "hologram")
            return Crop(atlas, 2, 9);
        if (!LegendaryJokers.Contains(key))
            return null;
        var (x, y) = atlas.Cells[key];
        return Crop(atlas, x, y + 1);
    }

    private IImage? ResolveBoss(Catalog c, string n)
    {
        if (n is "smallblind" or "bigblind")
            return Crop(c.Blinds, n);
        if (IsAny(n))
            n = "mysteryboss";
        return Crop(c.Bosses, BossKeys(n)) ?? Crop(c.Blinds, n);
    }

    private static IEnumerable<string> BossKeys(string n)
    {
        yield return n;
        if (n.EndsWith("blind", StringComparison.Ordinal) && n.Length > 5)
            n = n[..^5]; // "The Eye Blind" / "TheEyeBlind"
        if (n.StartsWith("boss", StringComparison.Ordinal) && n.Length > 4)
            n = n[4..];
        yield return n;
        yield return n.StartsWith("the", StringComparison.Ordinal) ? n[3..] : "the" + n;
    }

    private static IEnumerable<string> TagKeys(string n)
    {
        if (IsAny(n))
        {
            yield return "anytag";
            yield break;
        }
        yield return n;
        yield return n.EndsWith("tag", StringComparison.Ordinal) ? n[..^3] : n + "tag";
        if (n is "topuptag" or "topup")
            yield return "topuptag";
    }

    private static IEnumerable<string> VoucherKeys(string n)
    {
        yield return n;
        if (n.EndsWith("voucher", StringComparison.Ordinal) && n.Length > 7)
            yield return n[..^7];
        if (n is "paintbrush" or "brush")
            yield return "paintbrush";
    }

    private static IEnumerable<string> BoosterKeys(string n)
    {
        yield return n;
        yield return n.EndsWith("pack", StringComparison.Ordinal) ? n[..^4] : n + "pack";
        if (n.StartsWith("normal", StringComparison.Ordinal))
            yield return n[6..];
    }

    private static IEnumerable<string> DeckKeys(string n)
    {
        yield return n;
        if (n.EndsWith("deck", StringComparison.Ordinal) && n.Length > 4)
            yield return n[..^4];
    }

    private static IEnumerable<string> StakeKeys(string n)
    {
        yield return n;
        yield return n.EndsWith("stake", StringComparison.Ordinal) ? n[..^5] : n + "stake";
    }

    private static IEnumerable<string> StickerKeys(string n)
    {
        yield return n;
        if (n.EndsWith("sticker", StringComparison.Ordinal) && n.Length > 7)
            n = n[..^7];
        yield return n;
        yield return n.EndsWith("stake", StringComparison.Ordinal) ? n[..^5] : n + "stake";
    }

    private static IEnumerable<string> EnhancementKeys(string n)
    {
        yield return n;
        foreach (var suffix in new[] { "card", "enhancement" })
            if (n.EndsWith(suffix, StringComparison.Ordinal) && n.Length > suffix.Length)
                yield return n[..^suffix.Length];
    }

    private static IEnumerable<string> SealKeys(string n)
    {
        yield return n;
        if (n.EndsWith("seal", StringComparison.Ordinal) && n.Length > 4)
            yield return n[..^4];
    }

    /// <summary>Column in Editions.png; -1 when the name isn't an edition.</summary>
    private static int EditionCell(string n) =>
        n switch
        {
            "" or "none" or "normal" or "base" => 0,
            "foil" => 1,
            "holographic" or "holo" => 2,
            "polychrome" or "poly" => 3,
            "debuffed" or "debuff" => 4,
            _ => -1,
        };

    private static string? CanonicalSuit(string suit)
    {
        var n = Normalize(suit ?? string.Empty);
        return n switch
        {
            "h" or "heart" or "hearts" => "hearts",
            "c" or "club" or "clubs" => "clubs",
            "d" or "diamond" or "diamonds" => "diamonds",
            "s" or "spade" or "spades" => "spades",
            _ => null,
        };
    }

    private static string? CanonicalRank(string rank)
    {
        var n = Normalize(rank ?? string.Empty);
        return n switch
        {
            "2" or "two" => "2",
            "3" or "three" => "3",
            "4" or "four" => "4",
            "5" or "five" => "5",
            "6" or "six" => "6",
            "7" or "seven" => "7",
            "8" or "eight" => "8",
            "9" or "nine" => "9",
            "10" or "t" or "ten" => "10",
            "j" or "jack" => "jack",
            "q" or "queen" => "queen",
            "k" or "king" => "king",
            "a" or "ace" => "ace",
            _ => null,
        };
    }

    /// <summary>"Ace of Spades", "AceOfSpades", "10 of Hearts" → suit + rank.</summary>
    private static bool TryParsePlayingCard(string name, out string suit, out string rank)
    {
        suit = rank = string.Empty;
        var n = Normalize(name);
        foreach (var s in Suits)
        {
            var at = n.LastIndexOf("of" + s, StringComparison.Ordinal);
            if (at <= 0 || at + 2 + s.Length != n.Length)
                continue;
            var r = CanonicalRank(n[..at]);
            if (r is null)
                return false;
            (suit, rank) = (s, r);
            return true;
        }
        return false;
    }

    private static bool IsTypeWord(string n) =>
        n
            is "joker"
                or "jokers"
                or "souljoker"
                or "legendaryjoker"
                or "wildcardjoker"
                or "tag"
                or "tags"
                or "smallblindtag"
                or "bigblindtag"
                or "skiptag"
                or "voucher"
                or "vouchers"
                or "tarot"
                or "tarots"
                or "tarotcard"
                or "planet"
                or "planets"
                or "planetcard"
                or "spectral"
                or "spectrals"
                or "spectralcard"
                or "boss"
                or "bosses"
                or "bossblind"
                or "blind"
                or "blinds"
                or "booster"
                or "boosters"
                or "boosterpack"
                or "pack"
                or "packs"
                or "deck"
                or "decks"
                or "stake"
                or "stakes"
                or "enhancement"
                or "enhancements"
                or "seal"
                or "seals"
                or "sticker"
                or "stickers"
                or "edition"
                or "editions"
                or "standardcard"
                or "playingcard"
                or "card";

    /// <summary>"Any", "*", "Wildcard_..." — the category's wildcard sprite.</summary>
    private static bool IsAny(string n) =>
        n.Length == 0 || n is "any" || n.StartsWith("wildcard", StringComparison.Ordinal);

    private static bool IsPlaceholder(string name) =>
        name.Trim() is "*" or "." || Normalize(name) is "any" or "none";

    private static bool IsNone(string? s) => string.IsNullOrWhiteSpace(s) || Normalize(s) is "none";

    /// <summary>
    /// Lookup key: lower-case letters and digits only ("Oops! All 6s" → "oopsall6s",
    /// "Mr. Bones" → "mrbones"), "+" spelled "plus", accents dropped, "#n" suffix removed.
    /// </summary>
    internal static string Normalize(string name)
    {
        var hash = name.LastIndexOf('#');
        if (hash > 0)
            name = name[..hash];
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (ch == '+')
            {
                sb.Append("plus");
                continue;
            }
            if (!char.IsLetterOrDigit(ch))
                continue;
            var lower = char.ToLowerInvariant(ch);
            sb.Append(
                lower switch
                {
                    'é' or 'è' or 'ê' or 'ë' => 'e',
                    'á' or 'à' or 'â' or 'ä' => 'a',
                    'í' or 'ì' or 'î' or 'ï' => 'i',
                    'ó' or 'ò' or 'ô' or 'ö' => 'o',
                    'ú' or 'ù' or 'û' or 'ü' => 'u',
                    _ => lower,
                }
            );
        }
        return sb.ToString();
    }

    // ================================================================== cropping and composing

    private IImage? Crop(Atlas? atlas, IEnumerable<string> keys)
    {
        if (atlas is null)
            return null;
        foreach (var key in keys)
            if (atlas.Cells.TryGetValue(key, out var cell))
                return Crop(atlas, cell.X, cell.Y);
        return null;
    }

    private IImage? Crop(Atlas? atlas, string key) =>
        atlas is not null && atlas.Cells.TryGetValue(key, out var cell)
            ? Crop(atlas, cell.X, cell.Y)
            : null;

    private IImage? Crop(Atlas? atlas, int x, int y)
    {
        if (atlas is null)
            return null;
        var rect = new PixelRect(x * atlas.CellWidth, y * atlas.CellHeight, atlas.CellWidth, atlas.CellHeight);
        var bounds = new PixelRect(atlas.Sheet.PixelSize);
        if (!bounds.Contains(rect))
        {
            DebugLogger.LogError("SpriteService", $"Cell ({x},{y}) is outside {atlas.Name} ({bounds.Width}x{bounds.Height})");
            return null;
        }
        return _cache.GetOrAdd($"crop:{atlas.Name}:{x}:{y}", _ => Cut(atlas.Sheet, rect));
    }

    /// <summary>
    /// Copies one cell out of a sheet into its own bitmap. A <see cref="CroppedBitmap"/> would
    /// avoid the copy, but it is an AvaloniaObject owned by the thread that created it, and
    /// these images are cached and shared; a plain bitmap works from any thread.
    /// </summary>
    private static IImage Cut(Bitmap sheet, PixelRect rect)
    {
        try
        {
            var target = new RenderTargetBitmap(rect.Size, new Vector(96, 96));
            using (var ctx = target.CreateDrawingContext())
            using (ctx.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None }))
            {
                ctx.DrawImage(sheet, rect.ToRect(1), new Rect(0, 0, rect.Width, rect.Height));
            }
            return target;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SpriteService", $"Cutting {rect} failed, using a CroppedBitmap: {ex.Message}");
            return new CroppedBitmap(sheet, rect);
        }
    }

    /// <summary>
    /// Draws <paramref name="layers"/> bottom-to-top, each stretched to <paramref name="size"/>
    /// with nearest-neighbour sampling (pixel art), into one cached bitmap.
    /// </summary>
    private IImage Compose(string key, PixelSize size, params IImage[] layers)
    {
        if (_cache.TryGetValue(key, out var cached))
            return cached;
        try
        {
            var target = new RenderTargetBitmap(size, new Vector(96, 96));
            using (var ctx = target.CreateDrawingContext())
            using (ctx.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None }))
            {
                var dest = new Rect(0, 0, size.Width, size.Height);
                foreach (var layer in layers)
                    ctx.DrawImage(layer, dest);
            }
            return _cache.GetOrAdd(key, target);
        }
        catch (Exception ex)
        {
            // Composition needs the render platform; fall back to the bottom layer.
            DebugLogger.LogError("SpriteService", $"Compositing '{key}' failed: {ex.Message}");
            return layers[0];
        }
    }

    private static PixelSize PixelSizeOf(IImage image) =>
        image switch
        {
            CroppedBitmap cropped => cropped.SourceRect.Size,
            Bitmap bitmap => bitmap.PixelSize,
            _ => new PixelSize((int)image.Size.Width, (int)image.Size.Height),
        };

    private IImage? Miss(string category, string name)
    {
        if (_reportedMisses.TryAdd($"{category}:{name}", 0))
            DebugLogger.LogWarning("SpriteService", $"No {category} sprite for '{name}'");
        return null;
    }

    private IImage TransparentPixel() =>
        _transparentPixel ??= new WriteableBitmap(
            new PixelSize(1, 1),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul
        );

    // ================================================================== loading

    private Catalog EnsureLoaded()
    {
        var catalog = _catalog;
        if (catalog is not null)
            return catalog;
        lock (_loadLock)
        {
            return _catalog ??= Catalog.Load();
        }
    }

    /// <summary>
    /// One atlas: a sheet cut into an even grid, plus name → cell. The cell size comes from
    /// the PNG's pixel size divided by the grid, not from the metadata's spriteWidth fields
    /// (several of those describe a different export of the sheet).
    /// </summary>
    private sealed class Atlas(string name, string category, Bitmap sheet, int columns, int rows, Dictionary<string, (int X, int Y)> cells)
    {
        public string Name { get; } = name;

        /// <summary>Key suffix for the category's "any" sprite (anyjoker, anytarot, ...).</summary>
        public string Category { get; } = category;

        public Bitmap Sheet { get; } = sheet;

        public int CellWidth { get; } = sheet.PixelSize.Width / columns;

        public int CellHeight { get; } = sheet.PixelSize.Height / rows;

        public Dictionary<string, (int X, int Y)> Cells { get; } = cells;
    }

    private sealed class Catalog
    {
        public Atlas? Jokers,
            JokersNegative,
            Tags,
            Tarots,
            Planets,
            Spectrals,
            Vouchers,
            Boosters,
            Blinds,
            Bosses,
            Decks,
            Enhancements,
            Seals,
            Special,
            Stakes,
            Stickers,
            Editions,
            PlayingCards;

        public static Catalog Load()
        {
            var c = new Catalog();
            var started = DateTime.UtcNow;

            var jokers = LoadSheet("Jokers/Jokers.png");
            var jokersNegative = LoadSheet("Jokers/jokers_negative.png");
            var tarots = LoadSheet("Tarots/Tarots.png");
            var enhancers = LoadSheet("Decks/Enhancers.png");
            var blinds = LoadSheet("Bosses/BlindChips.png");

            c.Jokers = ListAtlas("jokers", "joker", jokers, 10, 16, "Jokers/jokers.json");
            c.JokersNegative = ListAtlas("jokers_negative", "joker", jokersNegative, 10, 16, "Jokers/jokers_negative.json");
            c.Tags = ListAtlas("tags", "tag", LoadSheet("Tags/tags.png"), 6, 5, "Tags/tags.json");
            c.Tarots = ListAtlas("tarots", "tarot", tarots, 10, 6, "Tarots/tarots.json");
            c.Planets = ListAtlas("planets", "planet", tarots, 10, 6, "Tarots/planets.json");
            c.Spectrals = ListAtlas("spectrals", "spectral", tarots, 10, 6, "Tarots/spectrals.json");
            c.Vouchers = ListAtlas("vouchers", "voucher", LoadSheet("Vouchers/Vouchers.png"), 9, 4, "Vouchers/vouchers.json");
            c.Boosters = ListAtlas("boosters", "booster", LoadSheet("Boosters/boosters.png"), 4, 9, "Boosters/Boosters.json");

            var blindMeta = ReadGridMetadata("Bosses/blinds_metadata.json");
            c.Blinds = GridAtlas("blinds", "blind", blinds, 21, 31, blindMeta, "blinds");
            c.Bosses = GridAtlas("bosses", "boss", blinds, 21, 31, blindMeta, "bosses", "finisherBosses", "special");

            var enhancerMeta = ReadGridMetadata("Decks/enhancers_metadata.json");
            c.Decks = GridAtlas("decks", "deck", enhancers, 7, 5, enhancerMeta, "decks");
            c.Enhancements = GridAtlas("enhancements", "enhancement", enhancers, 7, 5, enhancerMeta, "enhancements");
            c.Seals = GridAtlas("seals", "seal", enhancers, 7, 5, enhancerMeta, "seals");
            c.Special = GridAtlas("special", "special", enhancers, 7, 5, enhancerMeta, "special");

            c.Stakes = GridAtlas("stakes", "stake", LoadSheet("Decks/balatro-stake-chips.png"), 5, 2, ReadGridMetadata("Decks/stakes.json"), "stakes");
            c.Stickers = GridAtlas("stickers", "sticker", LoadSheet("Jokers/stickers.png"), 5, 3, ReadGridMetadata("Jokers/stickers_metadata.json"), "jokerStickers", "stakeStickers");

            // Editions.png is one row: none, foil, holographic, polychrome, debuffed.
            var editions = LoadSheet("Jokers/Editions.png");
            if (editions is not null)
                c.Editions = new Atlas("editions", "edition", editions, 5, 1, new());

            // standard_cards_metadata.json: sprites.{Suit}.{Rank} = {x,y}; keyed "suit|rank".
            var cards = LoadSheet("Decks/8BitDeck.png");
            var cardMeta = ReadGridMetadata("Decks/standard_cards_metadata.json");
            if (cards is not null && cardMeta is not null)
            {
                var cells = new Dictionary<string, (int, int)>(StringComparer.Ordinal);
                foreach (var suit in cardMeta)
                    foreach (var rank in suit.Value)
                        cells[$"{suit.Key}|{rank.Key}"] = rank.Value;
                c.PlayingCards = new Atlas("playingcards", "card", cards, 13, 4, cells);
            }

            DebugLogger.Log("SpriteService", $"Sprite atlases loaded in {(DateTime.UtcNow - started).TotalMilliseconds:F0} ms");
            return c;
        }

        private static Bitmap? LoadSheet(string path)
        {
            try
            {
                using var stream = AssetLoader.Open(new Uri(AssetRoot + path));
                return new Bitmap(stream);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SpriteService", $"Failed to load sprite sheet '{path}': {ex.Message}");
                return null;
            }
        }

        /// <summary>List-style metadata: [{ "name": ..., "pos": { "x": .., "y": .. } }].</summary>
        private static Atlas? ListAtlas(string name, string category, Bitmap? sheet, int columns, int rows, string jsonPath)
        {
            if (sheet is null)
                return null;
            var cells = new Dictionary<string, (int, int)>(StringComparer.Ordinal);
            try
            {
                using var stream = AssetLoader.Open(new Uri(AssetRoot + jsonPath));
                var entries = JsonSerializer.Deserialize(stream, BsoJsonSerializerContext.Default.ListSpritePosition);
                foreach (var entry in entries ?? [])
                    if (entry.Name is { } n && entry.Pos is { } p)
                        cells[Normalize(n)] = (p.X, p.Y);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SpriteService", $"Failed to read sprite metadata '{jsonPath}': {ex.Message}");
            }
            return new Atlas(name, category, sheet, columns, rows, cells);
        }

        /// <summary>Grid-style metadata: picks the named groups out of <c>sprites</c>.</summary>
        private static Atlas? GridAtlas(
            string name,
            string category,
            Bitmap? sheet,
            int columns,
            int rows,
            Dictionary<string, Dictionary<string, (int X, int Y)>>? meta,
            params string[] groups
        )
        {
            if (sheet is null)
                return null;
            var cells = new Dictionary<string, (int, int)>(StringComparer.Ordinal);
            if (meta is not null)
                foreach (var group in groups)
                    if (meta.TryGetValue(group, out var entries))
                        foreach (var entry in entries)
                            cells.TryAdd(Normalize(entry.Key), entry.Value);
            return new Atlas(name, category, sheet, columns, rows, cells);
        }

        /// <summary>
        /// Reads <c>{"sprites": {group: {name: {"x":..,"y":..}}}}</c> into group → name → cell.
        /// Group and name keys are kept as written (normalized by the caller); playing-card
        /// names are lower-cased here so "Hearts"/"Jack" match the canonical suit/rank keys.
        /// </summary>
        private static Dictionary<string, Dictionary<string, (int X, int Y)>>? ReadGridMetadata(string jsonPath)
        {
            try
            {
                using var stream = AssetLoader.Open(new Uri(AssetRoot + jsonPath));
                using var doc = JsonDocument.Parse(stream);
                if (!doc.RootElement.TryGetProperty("sprites", out var sprites))
                    return null;
                var result = new Dictionary<string, Dictionary<string, (int, int)>>(StringComparer.OrdinalIgnoreCase);
                foreach (var group in sprites.EnumerateObject())
                {
                    if (group.Value.ValueKind != JsonValueKind.Object)
                        continue;
                    var entries = new Dictionary<string, (int, int)>(StringComparer.Ordinal);
                    foreach (var entry in group.Value.EnumerateObject())
                    {
                        if (
                            entry.Value.ValueKind == JsonValueKind.Object
                            && entry.Value.TryGetProperty("x", out var x)
                            && entry.Value.TryGetProperty("y", out var y)
                        )
                            entries[entry.Name.ToLowerInvariant()] = (x.GetInt32(), y.GetInt32());
                    }
                    result[group.Name.ToLowerInvariant()] = entries;
                }
                return result;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SpriteService", $"Failed to read sprite metadata '{jsonPath}': {ex.Message}");
                return null;
            }
        }
    }
}
