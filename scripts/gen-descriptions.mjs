#!/usr/bin/env node
// Regenerates src/BalatroSeedOracle/Models/BalatroData.Descriptions.g.cs (issue #12).
//
// Source text: the seedfinder.app Balatro corpus (corpus/knowledge/{jokers,consumables}.md,
// v1.0.1o-FULL, verified against game source per that corpus's README). Item identity:
// the Motely enums in the src/MotelyJAML submodule, which is what BSO's pickers use as
// SelectableItem.Name.
//
//   node scripts/gen-descriptions.mjs [corpusDir]
//
// corpusDir defaults to $BALATRO_CORPUS_DIR, then ../seedfinder.app/corpus/knowledge
// (sibling checkout). Exits non-zero if any enum item has no corpus line or any corpus
// block matches no enum item. Nothing is invented: an item without a corpus line is
// reported, not filled.

import { execFileSync } from "node:child_process";
import { existsSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const corpusDir = resolve(
  process.argv[2] ??
    process.env.BALATRO_CORPUS_DIR ??
    join(repoRoot, "..", "seedfinder.app", "corpus", "knowledge"),
);
const enumDir = join(repoRoot, "src", "MotelyJAML", "Motely", "Enums");
const outFile = join(repoRoot, "src", "BalatroSeedOracle", "Models", "BalatroData.Descriptions.g.cs");

for (const p of [corpusDir, enumDir]) {
  if (!existsSync(p)) {
    console.error(`missing: ${p}`);
    process.exit(2);
  }
}

// Same normalization as BalatroData.ItemNameComparer: strip diacritics, keep [a-z0-9].
const norm = (s) =>
  s.normalize("NFD").replace(/\p{M}/gu, "").toLowerCase().replace(/[^a-z0-9]/g, "");

// Corpus names that do not normalize to their Motely enum name.
const ALIASES = { "8ball": "EightBall" };

function enumMembers(file, enumName) {
  const src = readFileSync(join(enumDir, file), "utf8");
  const start = src.indexOf(`enum ${enumName}`);
  if (start < 0) throw new Error(`enum ${enumName} not found in ${file}`);
  const body = src
    .slice(src.indexOf("{", start) + 1, src.indexOf("}", start))
    .replace(/\/\/.*$/gm, "");
  return [...body.matchAll(/^\s*([A-Za-z_]\w*)\s*(?:=[^,\n]*)?,?\s*$/gm)].map((m) => m[1]);
}

const groups = {
  Joker: [
    ...enumMembers("MotelyJokers.cs", "MotelyJokerCommon"),
    ...enumMembers("MotelyJokers.cs", "MotelyJokerUncommon"),
    ...enumMembers("MotelyJokers.cs", "MotelyJokerRare"),
    ...enumMembers("MotelyJokers.cs", "MotelyJokerLegendary"),
  ],
  Tarot: enumMembers("MotelyTarotCard.cs", "MotelyTarotCard"),
  Spectral: enumMembers("MotelySpectralCard.cs", "MotelySpectralCard"),
  Planet: enumMembers("MotelyPlanetCard.cs", "MotelyPlanetCard"),
  Voucher: enumMembers("MotelyVoucher.cs", "MotelyVoucher"),
};

// Front-matter blocks: "---\n<yaml>\n---\n<body>" repeated; content before the first "---" is the file header.
function blocks(file) {
  const parts = readFileSync(join(corpusDir, file), "utf8").split(/^---\s*$/m).slice(1);
  const out = [];
  for (let i = 0; i + 1 < parts.length; i += 2) {
    const meta = {};
    for (const line of parts[i].split("\n")) {
      const m = line.match(/^(\w+):\s*(.*)$/);
      if (m) meta[m[1]] = m[2].replace(/^"(.*)"$/, "$1").trim();
    }
    const body = parts[i + 1]
      .split("\n")
      .map((l) => l.trim())
      .filter((l) => l && !l.startsWith("#"));
    out.push({ meta, body });
  }
  return out;
}

// First sentence, not splitting inside parentheses or on decimals like X1.5.
function firstSentence(text) {
  let depth = 0;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (c === "(") depth++;
    else if (c === ")") depth = Math.max(0, depth - 1);
    else if (c === "." && depth === 0 && (i + 1 === text.length || text[i + 1] === " ")) {
      return text.slice(0, i + 1);
    }
  }
  return text;
}

const capitalize = (s) => s.charAt(0).toUpperCase() + s.slice(1);

const entries = []; // { group, corpusName, text, source }

for (const { meta, body } of blocks("jokers.md")) {
  if (meta.type !== "joker") continue;
  const line = body.find((l) => l.startsWith("Effect:"));
  if (!line) continue;
  const text = line
    .slice("Effect:".length)
    .trim()
    .replace(/\s*\(Currently:[^)]*\)/g, "") // run-state readout, not effect text
    .replace(/;\s*\d+ remaining$/, "") // Loyalty Card countdown readout
    .replace(/\s*\[\d+\]/g, "") // Yorick's "23 [23]" remaining-count readout
    .replace(/,;\s*/g, ", ")
    .replace(/;\s*(?=\()/g, " ") // ";" before a parenthetical is the game's line break, not prose
    .trim();
  // Game-internal key as a second lookup name, only where it is a different word from the
  // enum name (j_caino, j_ring_master, j_selzer, j_gluttenous_joker). Truncated keys like
  // j_glass / j_stone are skipped: they would shadow unrelated items named Glass / Stone.
  const id = meta.id?.replace(/^j_/, "");
  const internal = id && !norm(meta.name).includes(norm(id)) ? id : undefined;
  entries.push({ group: "Joker", corpusName: meta.name, internal, text, source: `jokers.md:${meta.id}` });
}

for (const { meta, body } of blocks("consumables.md")) {
  const group = { tarot: "Tarot", spectral: "Spectral", planet: "Planet" }[meta.type];
  if (group) {
    const first = body[0].replace(/^Buy \$\d+( \/ sell \$\d+)?\.\s*/, "");
    entries.push({ group, corpusName: meta.name, text: firstSentence(first), source: `consumables.md:${meta.id}` });
    continue;
  }
  if (meta.type === "voucher") {
    const line = body[0];
    // Anything after the unlock sentence is effect text shared by both tiers
    // (Clearance Sale / Liquidation: "Also discounts vouchers.").
    const m = line.match(/Base (.+?): (.+?) Upgraded (.+?): (.+?)(?: Upgrade unlock: [^.]*\.\s*(.*))?$/);
    if (!m) throw new Error(`unparsed voucher line (${meta.id}): ${line}`);
    const shared = m[5]?.trim() ? ` ${m[5].trim()}` : "";
    const baseText = capitalize(m[2].trim()) + shared;
    // The corpus states the upgraded tier relative to its base ("4x more often"), so the
    // upgrade's tooltip carries the base line after it.
    const upText = `${capitalize(m[4].trim())} Upgrades ${m[1]}: ${baseText}`;
    entries.push({ group: "Voucher", corpusName: m[1], text: baseText, source: `consumables.md:${meta.id}` });
    entries.push({ group: "Voucher", corpusName: m[3], text: upText, source: `consumables.md:${meta.id}` });
  }
}

const problems = [];
const resolved = new Map(); // enum name -> entry
for (const e of entries) {
  const key = norm(e.corpusName);
  const enumName = ALIASES[key] ?? groups[e.group].find((n) => norm(n) === key);
  if (!enumName) {
    problems.push(`corpus ${e.group} "${e.corpusName}" (${e.source}) matches no Motely enum member`);
    continue;
  }
  if (resolved.has(enumName)) {
    problems.push(`duplicate corpus text for ${e.group} ${enumName}`);
    continue;
  }
  if (!e.text) problems.push(`empty text for ${enumName} (${e.source})`);
  resolved.set(enumName, { ...e, enumName });
}

const missing = [];
for (const [group, names] of Object.entries(groups)) {
  for (const n of names) if (!resolved.has(n)) missing.push(`${group} ${n}`);
}

let corpusRev = "unknown";
try {
  corpusRev = execFileSync("git", ["-C", corpusDir, "log", "-1", "--format=%h", "--", "."], {
    encoding: "utf8",
  }).trim() || "unknown";
  // `git log` ignores uncommitted edits; mark the rev so the header never claims text from a
  // dirty tree came from that commit.
  const dirty = execFileSync("git", ["-C", corpusDir, "status", "--porcelain", "--", "."], {
    encoding: "utf8",
  }).trim();
  if (dirty && corpusRev !== "unknown") corpusRev += "-dirty";
} catch {}

const cs = (s) => JSON.stringify(s); // JSON string literal is a valid C# regular string literal here
const lines = [];
lines.push("// <auto-generated>");
lines.push("// Generated by scripts/gen-descriptions.mjs. Do not edit by hand; rerun the script.");
lines.push(`// Source: seedfinder.app corpus/knowledge (jokers.md, consumables.md) @ ${corpusRev}, Balatro v1.0.1o-FULL.`);
lines.push("// Keys: Motely enum member names (SelectableItem.Name) plus corpus display names where they differ.");
lines.push("// </auto-generated>");
lines.push("");
lines.push("namespace BalatroSeedOracle.Models");
lines.push("{");
lines.push("    public static partial class BalatroData");
lines.push("    {");
lines.push("        /// <summary>Corpus effect text for jokers, tarots, spectrals, planets and vouchers (#12).</summary>");
lines.push("        private static void InitializeCorpusDescriptions()");
lines.push("        {");
for (const [group, names] of Object.entries(groups)) {
  lines.push(`            // ${group}`);
  for (const n of names) {
    const e = resolved.get(n);
    if (!e) continue;
    const seen = new Set();
    for (const key of [n, e.corpusName, e.internal]) {
      if (!key || seen.has(norm(key))) continue;
      seen.add(norm(key));
      lines.push(`            Descriptions[${cs(key)}] = ${cs(e.text)};`);
    }
  }
}
lines.push("        }");
lines.push("    }");
lines.push("}");
lines.push("");
// CRLF: .gitattributes has `*.cs text eol=crlf`, so a checkout writes this file with CRLF.
// Emitting the same bytes keeps a regenerate in a fresh clone from dirtying `git status`.
writeFileSync(outFile, lines.join("\r\n"));

const counts = Object.fromEntries(
  Object.entries(groups).map(([g, names]) => [g, `${names.filter((n) => resolved.has(n)).length}/${names.length}`]),
);
console.log(`wrote ${outFile}`);
console.log(`corpus ${corpusDir} @ ${corpusRev}`);
console.log(JSON.stringify(counts));
if (missing.length) console.log(`MISSING (left empty, no corpus line):\n  ${missing.join("\n  ")}`);
if (problems.length) console.log(`PROBLEMS:\n  ${problems.join("\n  ")}`);
process.exit(missing.length || problems.length ? 1 : 0);
