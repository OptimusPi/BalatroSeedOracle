using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Motely.Filters;

internal static class BsoDraftToJaml
{
    public static bool TryToJamlConfig(
        MotelyJsonConfig draft,
        out JamlConfig? config,
        out string? error)
    {
        config = null;
        try
        {
            var yaml = ToJamlYaml(draft);
            return Motely.Filters.JamlConfigLoader.TryLoad(yaml, out config, out error);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static string ToJamlYaml(MotelyJsonConfig draft)
    {
        var sb = new StringBuilder();
        var sw = new StringWriter(sb);
        var emitter = new Emitter(sw);
        emitter.Emit(new StreamStart());
        emitter.Emit(new DocumentStart());
        emitter.Emit(new MappingStart(null, null, true, MappingStyle.Block));

        WriteScalar(emitter, "id", string.IsNullOrWhiteSpace(draft.Name) ? "filter" : draft.Name!);
        if (!string.IsNullOrWhiteSpace(draft.Name))
            WriteScalar(emitter, "name", draft.Name!);
        if (!string.IsNullOrWhiteSpace(draft.Author))
            WriteScalar(emitter, "author", draft.Author!);
        if (!string.IsNullOrWhiteSpace(draft.Description))
            WriteScalar(emitter, "description", draft.Description!);
        if (!string.IsNullOrWhiteSpace(draft.Deck))
            WriteScalar(emitter, "deck", draft.Deck!);
        if (!string.IsNullOrWhiteSpace(draft.Stake))
            WriteScalar(emitter, "stake", draft.Stake!);

        WriteClauseSection(emitter, "must", draft.Must);
        WriteClauseSection(emitter, "should", draft.Should);
        WriteClauseSection(emitter, "mustNot", draft.MustNot);

        emitter.Emit(new MappingEnd());
        emitter.Emit(new DocumentEnd(true));
        emitter.Emit(new StreamEnd());
        return sb.ToString();
    }

    private static void WriteClauseSection(
        IEmitter emitter,
        string name,
        List<MotelyJsonConfig.MotelyJsonFilterClause>? clauses)
    {
        if (clauses == null || clauses.Count == 0)
            return;
        emitter.Emit(new Scalar(name));
        emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Block));
        foreach (var c in clauses)
            WriteClause(emitter, c);
        emitter.Emit(new SequenceEnd());
    }

    private static void WriteClause(IEmitter emitter, MotelyJsonConfig.MotelyJsonFilterClause c)
    {
        emitter.Emit(new MappingStart(null, null, true, MappingStyle.Block));

        var (key, value) = ResolveDiscriminator(c);
        if (c.Clauses != null && c.Clauses.Count > 0 && (c.Type?.Equals("and", StringComparison.OrdinalIgnoreCase) == true
            || c.Type?.Equals("or", StringComparison.OrdinalIgnoreCase) == true))
        {
            var logicKey = c.Type!.ToLowerInvariant();
            emitter.Emit(new Scalar(logicKey));
            emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Block));
            foreach (var nested in c.Clauses)
                WriteClause(emitter, nested);
            emitter.Emit(new SequenceEnd());
        }
        else if (!string.IsNullOrEmpty(key))
        {
            WriteScalar(emitter, key, value ?? "Any");
        }

        if (c.Antes != null && c.Antes.Length > 0)
            WriteIntArray(emitter, "antes", c.Antes);
        if (c.Min.HasValue)
            WriteScalar(emitter, "min", c.Min.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (c.Score != 1)
            WriteScalar(emitter, "score", c.Score.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (!string.IsNullOrEmpty(c.Label))
            WriteScalar(emitter, "label", c.Label!);
        if (!string.IsNullOrEmpty(c.Edition))
            WriteScalar(emitter, "edition", c.Edition!);
        if (c.Stickers != null && c.Stickers.Length > 0)
            WriteStringArray(emitter, "stickers", c.Stickers);
        if (!string.IsNullOrEmpty(c.Seal))
            WriteScalar(emitter, "seal", c.Seal!);
        if (!string.IsNullOrEmpty(c.Enhancement))
            WriteScalar(emitter, "enhancement", c.Enhancement!);
        if (!string.IsNullOrEmpty(c.Rank))
            WriteScalar(emitter, "rank", c.Rank!);
        if (!string.IsNullOrEmpty(c.Suit))
            WriteScalar(emitter, "suit", c.Suit!);
        if (c.Rolls != null && c.Rolls.Length > 0)
            WriteIntArray(emitter, "rolls", c.Rolls);

        var shopSlots = c.Sources?.ShopSlots ?? c.ShopSlots;
        var packSlots = c.Sources?.PackSlots ?? c.PackSlots;
        if (shopSlots != null && shopSlots.Length > 0)
            WriteIntArray(emitter, "shopItems", shopSlots);
        if (packSlots != null && packSlots.Length > 0)
            WriteIntArray(emitter, "boosterPacks", packSlots);

        emitter.Emit(new MappingEnd());
    }

    private static (string key, string? value) ResolveDiscriminator(
        MotelyJsonConfig.MotelyJsonFilterClause c)
    {
        var type = (c.Type ?? "").Trim().ToLowerInvariant();
        var v = c.Value;
        return type switch
        {
            "joker" => ("joker", v),
            "souljoker" or "legendaryjoker" => ("legendaryJoker", v),
            "commonjoker" => ("commonJoker", v),
            "uncommonjoker" => ("uncommonJoker", v),
            "rarejoker" => ("rareJoker", v),
            "voucher" => ("voucher", v),
            "tarot" or "tarotcard" => ("tarotCard", v),
            "spectral" or "spectralcard" => ("spectralCard", v),
            "planet" or "planetcard" => ("planetCard", v),
            "boss" => ("boss", v),
            "tag" => ("tag", v),
            "smallblindtag" => ("smallBlindTag", v),
            "bigblindtag" => ("bigBlindTag", v),
            "standardcard" or "playingcard" => ("standardCard", v),
            "erraticrank" => ("erraticRank", v),
            "erraticsuit" => ("erraticSuit", v),
            "erraticcard" => ("erraticCard", v),
            "startingdraw" => ("startingDraw", v),
            "event" => ("event", v),
            _ => (type, v),
        };
    }

    private static void WriteScalar(IEmitter emitter, string key, string value)
    {
        emitter.Emit(new Scalar(key));
        emitter.Emit(new Scalar(value));
    }

    private static void WriteIntArray(IEmitter emitter, string key, int[] arr)
    {
        emitter.Emit(new Scalar(key));
        emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Flow));
        foreach (var n in arr)
            emitter.Emit(new Scalar(n.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        emitter.Emit(new SequenceEnd());
    }

    private static void WriteStringArray(IEmitter emitter, string key, string[] arr)
    {
        emitter.Emit(new Scalar(key));
        emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Flow));
        foreach (var s in arr)
            emitter.Emit(new Scalar(s));
        emitter.Emit(new SequenceEnd());
    }
}
