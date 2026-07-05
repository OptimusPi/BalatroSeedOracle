using System.Collections.Generic;
using System.Text.Json.Serialization;
using Motely.Enums;
using YamlDotNet.Serialization;

namespace Motely.Filters.Jaml;

/// <summary>
/// Top-level JAML document: the loader fills this from YAML; <see cref="JamlSerializer"/> and the TUI emit the same shape. Keys are camelCase (JAML convention).
/// </summary>
public sealed class JamlRootDocument
{
    [YamlMember(Alias = "id")]
    public string? Id { get; set; }

    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    [YamlMember(Alias = "author")]
    public string? Author { get; set; }

    [YamlMember(Alias = "dateCreated")]
    public string? DateCreated { get; set; }

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "deck")]
    public string? Deck { get; set; }

    [YamlMember(Alias = "stake")]
    public string? Stake { get; set; }

    [YamlMember(Alias = "defaults")]
    public JamlDefaults? Defaults { get; set; }

    [YamlMember(Alias = "must")]
    public List<JamlClauseUnion>? Must { get; set; }

    [YamlMember(Alias = "should")]
    public List<JamlClauseUnion>? Should { get; set; }

    [YamlMember(Alias = "mustNot")]
    public List<JamlClauseUnion>? MustNot { get; set; }

    [YamlMember(Alias = "seeds")]
    public List<string>? Seeds { get; set; }
}

public sealed class JamlDefaults
{
    [YamlMember(Alias = "antes")]
    public int[]? Antes { get; set; }

    [YamlMember(Alias = "boosterPacks")]
    public int[]? BoosterPacks { get; set; }

    [YamlMember(Alias = "shopItems")]
    public int[]? ShopItems { get; set; }

    [YamlMember(Alias = "score")]
    public int? Score { get; set; }
}

public sealed class JamlClauseUnion
{
    [YamlMember(Alias = "joker")]
    public EnumOrAny<MotelyJoker>? Joker { get; set; }

    [YamlMember(Alias = "jokers")]
    public List<MotelyJoker>? Jokers { get; set; }

    [YamlMember(Alias = "commonJoker")]
    public EnumOrAny<MotelyJokerCommon>? CommonJoker { get; set; }

    [YamlMember(Alias = "commonJokers")]
    public List<MotelyJokerCommon>? CommonJokers { get; set; }

    [YamlMember(Alias = "uncommonJoker")]
    public EnumOrAny<MotelyJokerUncommon>? UncommonJoker { get; set; }

    [YamlMember(Alias = "uncommonJokers")]
    public List<MotelyJokerUncommon>? UncommonJokers { get; set; }

    [YamlMember(Alias = "rareJoker")]
    public EnumOrAny<MotelyJokerRare>? RareJoker { get; set; }

    [YamlMember(Alias = "rareJokers")]
    public List<MotelyJokerRare>? RareJokers { get; set; }

    [YamlMember(Alias = "legendaryJoker")]
    public EnumOrAny<MotelyJokerLegendary>? LegendaryJoker { get; set; }

    [YamlMember(Alias = "legendaryJokers")]
    public List<MotelyJokerLegendary>? LegendaryJokers { get; set; }

    [YamlMember(Alias = "voucher")]
    public MotelyVoucher? Voucher { get; set; }

    [YamlMember(Alias = "vouchers")]
    public List<MotelyVoucher>? Vouchers { get; set; }

    [YamlMember(Alias = "tarotCard")]
    public MotelyTarotCard? TarotCard { get; set; }

    [YamlMember(Alias = "tarotCards")]
    public List<MotelyTarotCard>? TarotCards { get; set; }

    [YamlMember(Alias = "spectralCard")]
    public MotelySpectralCard? SpectralCard { get; set; }

    [YamlMember(Alias = "spectralCards")]
    public List<MotelySpectralCard>? SpectralCards { get; set; }

    [YamlMember(Alias = "planetCard")]
    public MotelyPlanetCard? PlanetCard { get; set; }

    [YamlMember(Alias = "boss")]
    public MotelyBossBlind? Boss { get; set; }

    [YamlMember(Alias = "tag")]
    public MotelyTag? Tag { get; set; }

    [YamlMember(Alias = "tags")]
    public List<MotelyTag>? Tags { get; set; }

    [YamlMember(Alias = "smallBlindTag")]
    public MotelyTag? SmallBlindTag { get; set; }

    [YamlMember(Alias = "smallBlindTags")]
    public List<MotelyTag>? SmallBlindTags { get; set; }

    [YamlMember(Alias = "bigBlindTag")]
    public MotelyTag? BigBlindTag { get; set; }

    [YamlMember(Alias = "bigBlindTags")]
    public List<MotelyTag>? BigBlindTags { get; set; }

    [YamlMember(Alias = "standardCard")]
    public StandardCardValue? StandardCard { get; set; }

    [YamlMember(Alias = "standardCards")]
    public List<StandardCardValue>? StandardCards { get; set; }

    [YamlMember(Alias = "erraticRank")]
    public string? ErraticRank { get; set; }

    [YamlMember(Alias = "erraticSuit")]
    public string? ErraticSuit { get; set; }

    [YamlMember(Alias = "erraticCard")]
    public string? ErraticCard { get; set; }

    [YamlMember(Alias = "startingDraw")]
    public string? StartingDraw { get; set; }

    [YamlMember(Alias = "event")]
    public MotelyEventType? Event { get; set; }

    [YamlMember(Alias = "luckyMoney")]
    public int[]? LuckyMoney { get; set; }

    [YamlMember(Alias = "luckyMult")]
    public int[]? LuckyMult { get; set; }

    [YamlMember(Alias = "misprintMult")]
    public int[]? MisprintMult { get; set; }

    [YamlMember(Alias = "wheelOfFortune")]
    public int[]? WheelOfFortune { get; set; }

    [YamlMember(Alias = "cavendishExtinct")]
    public int[]? CavendishExtinct { get; set; }

    [YamlMember(Alias = "grosMichelExtinct")]
    public int[]? GrosMichelExtinct { get; set; }

    [YamlMember(Alias = "spaceLevelup")]
    public int[]? SpaceLevelup { get; set; }

    [YamlMember(Alias = "businessPayout")]
    public int[]? BusinessPayout { get; set; }

    [YamlMember(Alias = "bloodstoneTrigger")]
    public int[]? BloodstoneTrigger { get; set; }

    [YamlMember(Alias = "parkingPayout")]
    public int[]? ParkingPayout { get; set; }

    [YamlMember(Alias = "glassDestroy")]
    public int[]? GlassDestroy { get; set; }

    [YamlMember(Alias = "wheelStaysFlipped")]
    public int[]? WheelStaysFlipped { get; set; }

    // Common clause properties
    [YamlMember(Alias = "antes")]
    public int[]? Antes { get; set; }

    [YamlMember(Alias = "score")]
    public int? Score { get; set; }

    [YamlMember(Alias = "min")]
    public int? Min { get; set; }

    [YamlMember(Alias = "max")]
    public int? Max { get; set; }

    [YamlMember(Alias = "label")]
    public string? Label { get; set; }

    [YamlMember(Alias = "edition")]
    public MotelyItemEdition? Edition { get; set; }

    [YamlMember(Alias = "stickers")]
    public MotelyJokerSticker[]? Stickers { get; set; }

    [YamlMember(Alias = "seal")]
    public MotelyItemSeal? Seal { get; set; }

    [YamlMember(Alias = "enhancement")]
    public MotelyItemEnhancement? Enhancement { get; set; }

    [YamlMember(Alias = "rank")]
    public string? Rank { get; set; }

    [YamlMember(Alias = "suit")]
    public string? Suit { get; set; }

    [YamlMember(Alias = "rolls")]
    public int[]? Rolls { get; set; }

    /// <summary>
    /// Extra soul-stream edition reads per ante for the legendary edition vector prefilter (see
    /// <see cref="LegendaryJokerClause.SoulEditionRolls"/>).
    /// </summary>
    [YamlMember(Alias = "soulEditionRolls")]
    public int? SoulEditionRolls { get; set; }

    /// <summary>
    /// Match The Soul tarot/spectral card in packs only (no legendary joker roll). See
    /// <see cref="LegendaryJokerClause.SoulCardOnly"/>.
    /// </summary>
    [YamlMember(Alias = "soulCardOnly")]
    public bool? SoulCardOnly { get; set; }

    // Compound clauses (YAML keys are lowercase; matches jaml.schema / hand-written JAML)
    [YamlMember(Alias = "and")]
    public List<JamlClauseUnion>? And { get; set; }

    [YamlMember(Alias = "or")]
    public List<JamlClauseUnion>? Or { get; set; }

    [YamlMember(Alias = "clauses")]
    public List<JamlClauseUnion>? Clauses { get; set; }

    [YamlMember(Alias = "mode")]
    public string? Mode { get; set; }

    [YamlMember(Alias = "judgement")]
    public int[]? Judgement { get; set; }

    [YamlMember(Alias = "wraith")]
    public int[]? Wraith { get; set; }

    [YamlMember(Alias = "rareTag")]
    public int[]? RareTag { get; set; }

    [YamlMember(Alias = "uncommonTag")]
    public int[]? UncommonTag { get; set; }

    // Flat source shortcuts (top-level on clause)
    [YamlMember(Alias = "shopItems")]
    public int[]? ShopItems { get; set; }

    [YamlMember(Alias = "boosterPacks")]
    public int[]? BoosterPacks { get; set; }

    [YamlMember(Alias = "minShopItem")]
    public int? MinShopItem { get; set; }

    [YamlMember(Alias = "maxShopItem")]
    public int? MaxShopItem { get; set; }

    // Nested sources object
    [YamlMember(Alias = "sources")]
    public JamlSources? Sources { get; set; }
}

public struct StandardCardValue
{
    public string? StringValue;
    public StandardCardConfig? ObjectValue;
}

public sealed class StandardCardConfig
{
    [YamlMember(Alias = "rank")]
    public string? Rank { get; set; }

    [YamlMember(Alias = "suit")]
    public string? Suit { get; set; }

    [YamlMember(Alias = "seal")]
    public MotelyItemSeal? Seal { get; set; }

    [YamlMember(Alias = "enhancement")]
    public MotelyItemEnhancement? Enhancement { get; set; }

    [YamlMember(Alias = "edition")]
    public MotelyItemEdition? Edition { get; set; }

    [YamlMember(Alias = "sources")]
    public JamlSources? Sources { get; set; }
}

public sealed class JamlSources
{
    [YamlMember(Alias = "shopItems")]
    public int[]? ShopItems { get; set; }

    [YamlMember(Alias = "boosterPacks")]
    public int[]? BoosterPacks { get; set; }

    [YamlMember(Alias = "minShopItem")]
    public int? MinShopItem { get; set; }

    [YamlMember(Alias = "maxShopItem")]
    public int? MaxShopItem { get; set; }

    [YamlMember(Alias = "luck")]
    public int? Luck { get; set; }

    [YamlMember(Alias = "tags")]
    public bool Tags { get; set; }

    [YamlMember(Alias = "requireMega")]
    public bool RequireMega { get; set; }

    /// <summary>
    /// Booster scoring: apply Charm-tag rules (bonus Arcana pack on second weighted offer when none rolled Arcana).
    /// </summary>
    [YamlMember(Alias = "charmTag")]
    public bool CharmTag { get; set; }

    /// <summary>
    /// Booster scoring: apply Ethereal-tag rules (bonus Spectral pack when none rolled Spectral).
    /// </summary>
    [YamlMember(Alias = "etherealTag")]
    public bool EtherealTag { get; set; }

    [YamlMember(Alias = "judgement")]
    public int[]? Judgement { get; set; }

    [YamlMember(Alias = "rareTag")]
    public int[]? RareTag { get; set; }

    [YamlMember(Alias = "uncommonTag")]
    public int[]? UncommonTag { get; set; }

    [YamlMember(Alias = "wraith")]
    public int[]? Wraith { get; set; }

    [YamlMember(Alias = "soulCard")]
    public int[]? SoulCard { get; set; }

    /// <summary>
    /// Legendary / soul joker: pack indices where The Soul may appear in an <b>arcana</b> pack (tarot stream).
    /// If either this or spectralPacks is non-empty, matching uses split rules (see LegendaryJokerSourceConfig).
    /// </summary>
    [YamlMember(Alias = "arcanaPacks")]
    public int[]? ArcanaPacks { get; set; }

    /// <summary>
    /// Legendary / soul joker: pack indices where The Soul may appear in a <b>spectral</b> pack (spectral stream).
    /// </summary>
    [YamlMember(Alias = "spectralPacks")]
    public int[]? SpectralPacks { get; set; }

    [YamlMember(Alias = "riffRaff")]
    public int[]? RiffRaff { get; set; }

    [YamlMember(Alias = "purpleSealOrEightBall")]
    public int[]? PurpleSealOrEightBall { get; set; }

    [YamlMember(Alias = "emperor")]
    public int[]? Emperor { get; set; }

    [YamlMember(Alias = "sixthSense")]
    public int[]? SixthSense { get; set; }

    [YamlMember(Alias = "seance")]
    public int[]? Seance { get; set; }

    [YamlMember(Alias = "certificate")]
    public int[]? Certificate { get; set; }

    [YamlMember(Alias = "incantation")]
    public int[]? Incantation { get; set; }

    [YamlMember(Alias = "familiar")]
    public int[]? Familiar { get; set; }

    [YamlMember(Alias = "grim")]
    public int[]? Grim { get; set; }

    [YamlMember(Alias = "deckDraw")]
    public int[]? DeckDraw { get; set; }

    [YamlMember(Alias = "uncommonShopJokers")]
    public int[]? UncommonShopJokers { get; set; }

    [YamlMember(Alias = "rareShopJokers")]
    public int[]? RareShopJokers { get; set; }

    [YamlMember(Alias = "commonShopJokers")]
    public int[]? CommonShopJokers { get; set; }

    [YamlMember(Alias = "allShopJokers")]
    public int[]? AllShopJokers { get; set; }
}
