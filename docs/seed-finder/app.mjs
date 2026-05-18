// Static seed-finder app. Loads motely-wasm from the relative ./motely-wasm/
// directory (populated by the build script that npm packs motely-wasm and
// extracts it). No file:// — everything served over HTTPS by GitHub Pages.

import bootsharp, { Motely } from "./motely-wasm/dist/index.mjs";

const $ = (id) => document.getElementById(id);
const bootStatus = $("boot-status");
const jamlInput = $("jaml");
const validateBtn = $("validate");
const searchBtn = $("search");
const stopBtn = $("stop");
const batchInput = $("batch");
const validateStatus = $("validate-status");
const progressBar = $("progress");
const progressText = $("progress-text");
const matchCount = $("match-count");
const resultsBody = document.querySelector("#results tbody");

const DEFAULT_JAML = `name: WeeMonday
deck: Erratic
stake: Black
must:
  - joker: WeeJoker
    antes: [1]
`;

jamlInput.value = DEFAULT_JAML;

let activeSearch = null;
let matches = 0;

function setBootState(msg, klass) {
  bootStatus.textContent = msg;
  bootStatus.className = "boot " + (klass ?? "");
}

function setValidateState(msg, klass) {
  validateStatus.textContent = msg;
  validateStatus.className = klass ?? "";
}

function resetResults() {
  matches = 0;
  matchCount.textContent = "0";
  resultsBody.innerHTML = "";
  progressBar.value = 0;
  progressText.textContent = "starting...";
}

function addResult(seed, score, tallies) {
  matches += 1;
  matchCount.textContent = String(matches);
  const tr = document.createElement("tr");
  tr.innerHTML =
    `<td>${seed}</td><td>${score}</td>` +
    `<td>${tallies?.join(", ") ?? ""}</td>`;
  resultsBody.prepend(tr);
}

try {
  await bootsharp.boot("./motely-wasm/bin");
  setBootState(`motely-wasm v${Motely.version()} ready`, "ok");
  searchBtn.disabled = false;
} catch (e) {
  setBootState("Boot failed: " + (e?.message ?? e), "err");
  console.error(e);
}

validateBtn.addEventListener("click", () => {
  const result = Motely.validateJaml(jamlInput.value);
  if (result === "valid") {
    setValidateState("valid", "ok");
  } else {
    setValidateState(result, "err");
  }
});

Motely.onScoredResult.subscribe((r) => {
  addResult(r.seed, r.score, r.tallyValues ?? r.tallies);
});

Motely.onSeedMatch.subscribe((seed) => {
  // Seed matches without score (Must-only searches) — score column shows -.
  addResult(seed, "-", null);
});

Motely.onProgress.subscribe((p) => {
  progressBar.value = p.percentComplete ?? 0;
  const sps = p.seedsPerMillisecond
    ? `${(p.seedsPerMillisecond * 1000).toFixed(0)} seeds/s`
    : "";
  progressText.textContent =
    `${(p.percentComplete ?? 0).toFixed(2)}% (${p.seedsSearched ?? 0} seeds) ${sps}`;
});

searchBtn.addEventListener("click", async () => {
  if (activeSearch) return;
  resetResults();

  const status = Motely.validateJaml(jamlInput.value);
  if (status !== "valid") {
    setValidateState(status, "err");
    return;
  }
  setValidateState("valid", "ok");

  try {
    activeSearch = Motely.createSearch(jamlInput.value, {
      batchCharCount: Number(batchInput.value),
    });
    searchBtn.disabled = true;
    stopBtn.disabled = false;
    activeSearch.start();
    await activeSearch.awaitCompletion();
    progressText.textContent = "complete";
  } catch (e) {
    progressText.textContent = "error: " + (e?.message ?? e);
    console.error(e);
  } finally {
    activeSearch?.dispose?.();
    activeSearch = null;
    searchBtn.disabled = false;
    stopBtn.disabled = true;
  }
});

stopBtn.addEventListener("click", () => {
  if (!activeSearch) return;
  activeSearch.cancel?.();
  progressText.textContent = "stopped";
});
