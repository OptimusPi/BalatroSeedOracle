using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Loads and caches Balatro sprite images (jokers, tarots, vouchers, tags, bosses, decks,
/// editions, seals, stickers, booster packs) from the embedded asset atlases.
/// TIER0-STUB: replace in Tier 1. Every lookup returns null (blank image) until the real
/// atlas loading (SkiaSharp + avares:// assets) is restored; the API surface matches all
/// current call sites so UI code compiles unchanged.
/// </summary>
public class SpriteService
{
    public static SpriteService Instance { get; } = new();

    public Task PreloadAllSpritesAsync(CancellationToken? cancellationToken = null) =>
        Task.CompletedTask;

    public IImage? GetItemImage(string itemType) => null;

    public IImage? GetItemImage(string itemName, string? typeHint) => null;

    public IImage? GetJokerImage(string jokerName, string? edition = null) => null;

    public IImage? GetJokerSoulImage(string jokerName) => null;

    public IImage? GetJokerWithStickerImage(string stickerName) => null;

    public IImage? GetTarotImage(string tarotName) => null;

    public IImage? GetSpectralImage(string spectralName) => null;

    public IImage? GetPlanetCardImage(string planetName) => null;

    public IImage? GetVoucherImage(string voucherName) => null;

    public IImage? GetTagImage(string tagName) => null;

    public IImage? GetBossImage(string bossName) => null;

    public IImage? GetBoosterImage(string packName) => null;

    public IImage? GetDeckImage(string deckName) => null;

    public IImage? GetDeckImage(string deckName, int width, int height) => null;

    public IImage? GetStakeImage(string stakeName) => null;

    public IImage? GetDeckWithStakeSticker(string deckName, string? stakeName = null) => null;

    public IImage? GetPlayingCardImage(
        string suit,
        string rank,
        string? enhancement = null,
        string? seal = null,
        string? edition = null
    ) => null;

    public IImage? GetEditionImage(string editionName) => null;

    public IImage? GetSealImage(string sealName) => null;

    public IImage? GetEnhancementImage(string enhancementName) => null;

    public IImage? GetStickerImage(string stickerName) => null;

    public IImage? GetSoulGemImage() => null;

    public IImage? GetMysteryJokerFaceImage() => null;
}
