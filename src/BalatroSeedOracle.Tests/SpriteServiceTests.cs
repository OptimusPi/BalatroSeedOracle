using Avalonia;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using Motely.Enums;
using Xunit;

namespace BalatroSeedOracle.Tests;

/// <summary>Minimal Avalonia app for the headless session: real Skia, no windows, no App.axaml.</summary>
public class SpriteTestApp : Application
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder
            .Configure<SpriteTestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

/// <summary>
/// The one headless Avalonia session for the whole test run. Avalonia allows a single
/// application per process, so every class that needs it joins this collection (one fixture
/// instance, and its tests never run in parallel with each other's).
/// </summary>
[CollectionDefinition(Name)]
public sealed class HeadlessAvaloniaCollection : ICollectionFixture<HeadlessAvaloniaFixture>
{
    public const string Name = "Headless Avalonia";
}

/// <summary>One headless Avalonia session (AssetLoader + Skia bitmaps) shared by the sprite and designer tests.</summary>
public sealed class HeadlessAvaloniaFixture : IDisposable
{
    private readonly HeadlessUnitTestSession _session = HeadlessUnitTestSession.StartNew(typeof(SpriteTestApp));

    /// <summary>Runs <paramref name="action"/> on the Avalonia UI thread.</summary>
    public T Run<T>(Func<T> action) => _session.Dispatch(action, CancellationToken.None).GetAwaiter().GetResult();

    public void Dispose() => _session.Dispose();
}

/// <summary>
/// Every engine enum value (the names the analyzer and filters pass) and every picker entry
/// (BalatroData display names) must resolve to a real, in-bounds sprite.
/// </summary>
[Collection(HeadlessAvaloniaCollection.Name)]
public class SpriteServiceTests(HeadlessAvaloniaFixture avalonia)
{
    private static SpriteService Sprites => SpriteService.Instance;

    public static TheoryData<string, string> EngineNames()
    {
        var data = new TheoryData<string, string>();
        void Add<T>(string category)
            where T : struct, Enum
        {
            foreach (var name in Enum.GetNames<T>())
                data.Add(category, name);
        }
        Add<MotelyJoker>("joker");
        Add<MotelyTag>("tag");
        Add<MotelyBossBlind>("boss");
        Add<MotelyVoucher>("voucher");
        Add<MotelyTarotCard>("tarot");
        Add<MotelyPlanetCard>("planet");
        Add<MotelySpectralCard>("spectral");
        Add<MotelyBoosterPack>("booster");
        Add<MotelyBoosterPackType>("booster");
        Add<MotelyDeck>("deck");
        Add<MotelyStake>("stake");
        Add<MotelyStake>("stakesticker");
        return data;
    }

    [Theory]
    [MemberData(nameof(EngineNames))]
    public void EveryEngineEnumValueHasASprite(string category, string name)
    {
        var image = avalonia.Run(() => Resolve(category, name));
        AssertRealImage(image, $"{category} '{name}'");
    }

    /// <summary>Picker entries: enum-name keys and display-name values (e.g. "Oops! All 6s").</summary>
    public static TheoryData<string, string> PickerNames()
    {
        var data = new TheoryData<string, string>();
        void Add(string category, Dictionary<string, string> items)
        {
            foreach (var (key, display) in items)
            {
                data.Add(category, key);
                data.Add(category, display);
            }
        }
        Add("joker", BalatroData.Jokers);
        Add("tarot", BalatroData.TarotCards);
        Add("spectral", BalatroData.SpectralCards);
        Add("planet", BalatroData.PlanetCards);
        Add("voucher", BalatroData.Vouchers);
        Add("tag", BalatroData.Tags);
        Add("boss", BalatroData.BossBlinds);
        return data;
    }

    [Theory]
    [MemberData(nameof(PickerNames))]
    public void EveryPickerEntryHasASprite(string category, string name)
    {
        var image = avalonia.Run(() => Resolve(category, name));
        AssertRealImage(image, $"{category} '{name}'");
    }

    [Theory]
    [InlineData("TheEye", "boss")]
    [InlineData("The Eye", "boss")]
    [InlineData("the_eye", "boss")]
    [InlineData("Eye", "boss")]
    [InlineData("SmallBlind", "boss")]
    [InlineData("Blueprint", "Joker")]
    [InlineData("Oops! All 6s", "Joker")]
    [InlineData("Mr. Bones", "Joker")]
    [InlineData("Séance", "Joker")]
    [InlineData("Wildcard_Joker", "Joker")]
    [InlineData("Perkeo#2", "SoulJoker")]
    [InlineData("D6 Tag", "SmallBlindTag")]
    [InlineData("Negative", "Tag")]
    [InlineData("Overstock+", "Voucher")]
    [InlineData("Wheel of Fortune", "Tarot")]
    [InlineData("The Soul", "Spectral")]
    [InlineData("Planet X", "Planet")]
    [InlineData("Mega Buffoon Pack", "Booster")]
    [InlineData("Red Deck", "Deck")]
    [InlineData("Gold Stake", "Stake")]
    [InlineData("Ace of Spades", "StandardCard")]
    [InlineData("Joker", "joker")]
    [InlineData("Any", "Joker")]
    [InlineData("*", "Tag")]
    // (type, name) order, as JamlEditorTabViewModel passes it
    [InlineData("Joker", "Blueprint")]
    [InlineData("Tag", "Charm Tag")]
    // A synced rareJoker/uncommonJoker/commonJoker clause's builder type, in (type, name) order:
    // these came back null, so Sync to Visual showed Blueprint/Baron/Mime with no card art.
    [InlineData("RareJoker", "Blueprint")]
    [InlineData("UncommonJoker", "Mime")]
    [InlineData("CommonJoker", "Joker")]
    public void TolerantNamesResolve(string name, string typeHint)
    {
        var image = avalonia.Run(() => Sprites.GetItemImage(name, typeHint));
        AssertRealImage(image, $"GetItemImage('{name}', '{typeHint}')");
    }

    [Fact]
    public void NameWithoutTypeHintSearchesEveryCategory()
    {
        foreach (var name in new[] { "Blueprint", "TheFool", "Mercury", "Ankh", "Hone", "CouponTag", "TheOx", "Erratic", "Glass" })
            AssertRealImage(avalonia.Run(() => Sprites.GetItemImage(name)), name);
    }

    [Fact]
    public void AnalyzerUsesTarotLookupForPlanetsAndSpectrals()
    {
        // AnalyzeModalViewModel.GetItemSprite routes planets and spectrals through GetTarotImage.
        AssertRealImage(avalonia.Run(() => Sprites.GetTarotImage("Pluto")), "Pluto via tarot");
        AssertRealImage(avalonia.Run(() => Sprites.GetTarotImage("BlackHole")), "BlackHole via tarot");
    }

    [Fact]
    public void EveryPlayingCardEnhancementSealAndEditionResolves()
    {
        string[] suits = ["Hearts", "Clubs", "Diamonds", "Spades"];
        string[] ranks = ["2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King", "Ace"];
        foreach (var suit in suits)
            foreach (var rank in ranks)
                AssertRealImage(avalonia.Run(() => Sprites.GetPlayingCardImage(suit, rank)), $"{rank} of {suit}");

        foreach (var e in new[] { "Bonus", "Mult", "Wild", "Glass", "Steel", "Stone", "Gold", "Lucky" })
        {
            AssertRealImage(avalonia.Run(() => Sprites.GetEnhancementImage(e)), $"enhancement {e}");
            AssertRealImage(avalonia.Run(() => Sprites.GetPlayingCardImage("Hearts", "K", e, "Red", "Foil")), $"K of Hearts {e}");
        }
        foreach (var s in new[] { "Gold", "Red", "Blue", "Purple", "Gold Seal" })
            AssertRealImage(avalonia.Run(() => Sprites.GetSealImage(s)), $"seal {s}");
        foreach (var ed in new[] { "None", "Foil", "Holographic", "Polychrome", "debuffed" })
            AssertRealImage(avalonia.Run(() => Sprites.GetEditionImage(ed)), $"edition {ed}");
        foreach (var st in new[] { "eternal", "perishable", "rental", "WhiteStake", "GoldStake" })
        {
            AssertRealImage(avalonia.Run(() => Sprites.GetStickerImage(st)), $"sticker {st}");
            AssertRealImage(avalonia.Run(() => Sprites.GetJokerWithStickerImage(st)), $"joker with sticker {st}");
        }
    }

    [Fact]
    public void OverlaysAndCompositesResolve()
    {
        AssertRealImage(avalonia.Run(() => Sprites.GetSoulGemImage()), "soul gem");
        AssertRealImage(avalonia.Run(() => Sprites.GetMysteryJokerFaceImage()), "mystery joker face");
        AssertRealImage(avalonia.Run(() => Sprites.GetDeckWithStakeSticker("Erratic", "Gold")), "deck + stake");
        AssertRealImage(avalonia.Run(() => Sprites.GetDeckImage("Red", 71, 95)), "deck with size hint");
        foreach (var edition in new[] { "negative", "foil", "holographic", "polychrome" })
            AssertRealImage(avalonia.Run(() => Sprites.GetJokerImage("Blueprint", edition)), $"Blueprint {edition}");
        foreach (var legendary in new[] { "Canio", "Triboulet", "Yorick", "Chicot", "Perkeo", "Hologram" })
            AssertRealImage(avalonia.Run(() => Sprites.GetJokerSoulImage(legendary)), $"{legendary} soul");
    }

    [Theory]
    [InlineData("joker", "Blueprint", 71, 95)]
    [InlineData("tag", "CharmTag", 34, 34)]
    [InlineData("boss", "TheOx", 68, 68)]
    [InlineData("voucher", "Hone", 71, 95)]
    [InlineData("tarot", "TheFool", 71, 95)]
    [InlineData("booster", "MegaBuffoon", 142, 190)]
    [InlineData("deck", "Plasma", 142, 190)]
    [InlineData("stake", "Gold", 29, 29)]
    public void SpritesHaveTheAtlasCellSize(string category, string name, int width, int height)
    {
        var size = avalonia.Run(() => Assert.IsAssignableFrom<Bitmap>(Resolve(category, name)).PixelSize);
        Assert.Equal(new PixelSize(width, height), size);
    }

    [Fact]
    public async Task SpritesMadeOffTheUiThreadAreUsableAnywhere()
    {
        // Cached images are shared, so they must not be tied to the thread that made them.
        var image = await Task.Run(() => Sprites.GetVoucherImage("Palette"));
        AssertRealImage(image, "Palette (background thread)");
        Assert.Same(image, avalonia.Run(() => Sprites.GetVoucherImage("Palette")));
    }

    [Fact]
    public void SameNameReturnsTheCachedImage()
    {
        var (a, b) = avalonia.Run(() => (Sprites.GetJokerImage("Blueprint"), Sprites.GetJokerImage("blue_print")));
        Assert.Same(a, b);
    }

    [Fact]
    public void DocumentedNullsStayNull()
    {
        // Negative is a shader in Balatro, not a sprite: Editions.png has no cell for it.
        Assert.Null(avalonia.Run(() => Sprites.GetEditionImage("negative")));
        // Only legendary jokers (and Hologram) have a floating face.
        Assert.Null(avalonia.Run(() => Sprites.GetJokerSoulImage("Blueprint")));
        // Genuinely unknown names.
        Assert.Null(avalonia.Run(() => Sprites.GetJokerImage("NotAJoker")));
        Assert.Null(avalonia.Run(() => Sprites.GetTagImage("NotATag")));
        Assert.Null(avalonia.Run(() => Sprites.GetItemImage("Definitely Not A Sprite")));
    }

    private static IImage? Resolve(string category, string name) =>
        category switch
        {
            "joker" => Sprites.GetJokerImage(name),
            "tag" => Sprites.GetTagImage(name),
            "boss" => Sprites.GetBossImage(name),
            "voucher" => Sprites.GetVoucherImage(name),
            "tarot" => Sprites.GetTarotImage(name),
            "planet" => Sprites.GetPlanetCardImage(name),
            "spectral" => Sprites.GetSpectralImage(name),
            "booster" => Sprites.GetBoosterImage(name),
            "deck" => Sprites.GetDeckImage(name),
            "stake" => Sprites.GetStakeImage(name),
            "stakesticker" => Sprites.GetStickerImage(name + "Stake"),
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
        };

    /// <summary>Non-null and non-empty.</summary>
    private static void AssertRealImage(IImage? image, string what)
    {
        Assert.True(image is not null, $"{what} has no sprite");
        Assert.True(image!.Size.Width > 0 && image.Size.Height > 0, $"{what} is empty");
    }
}
