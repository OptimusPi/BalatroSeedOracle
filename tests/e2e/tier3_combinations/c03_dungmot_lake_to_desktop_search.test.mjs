// tests/e2e/tier3_combinations/c03_dungmot_lake_to_desktop_search.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { DUNGMOT_PATHS } from '../helpers/env.mjs';
import { validateSeedListFile, validateCsvFile } from '../helpers/parquet_oracle.mjs';
import {
  runDesktopTestsFilter,
  runWasmSeedSearch,
} from '../helpers/runtime_oracle.mjs';

describe('Combination C03: dungmot Curated Seed Lakes <-> BalatroSeedOracle Lake Streamer', () => {
  it('C03-1: Curated rank seeds list 10_best.txt provides valid 1-9A-Z seeds streamable into desktop search', () => {
    const listFile = path.join(DUNGMOT_PATHS.ranksDir, '10_best.txt');
    const result = validateSeedListFile(listFile, 10);
    assert.equal(result.valid, true, result.reason);
    assert.ok(result.seeds.length >= 10);
  });

  it('C03-2: Curated suit seeds list Spades_best.txt provides valid 1-9A-Z seeds streamable into desktop search', () => {
    const listFile = path.join(DUNGMOT_PATHS.suitsDir, 'Spades_best.txt');
    const result = validateSeedListFile(listFile, 10);
    assert.equal(result.valid, true, result.reason);
  });

  it('C03-3: Negative uncommon seeds CSV provides valid seeds streamable into desktop search', () => {
    const result = validateCsvFile(DUNGMOT_PATHS.uncommonCsv, 'SEED,COUNT');
    assert.equal(result.valid, true, result.reason);
    assert.ok(result.rowCount >= 100);
  });

  it('C03-4: Desktop SearchModalViewModel DbList mode streams selected lake partition in headless Avalonia test', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~SearchModalViewModel_SelectedDbListIndexChanged');
    assert.equal(res.exitCode, 0, `DbList partition test failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('C03-5: Streamed seeds from 10_best.txt score >= 18 Tens when evaluated by real Erratic Deck scoring pipeline', async () => {
    const listFile = path.join(DUNGMOT_PATHS.ranksDir, '10_best.txt');
    const { seeds } = validateSeedListFile(listFile, 5);
    const res = await runWasmSeedSearch(
      `name: Erratic Tens\ndeck: Erratic\nstake: White\nshould:\n  - erraticRank: Ten\n    score: 1\n`,
      seeds.slice(0, 5)
    );
    assert.equal(res.scored.length, 5);
    for (const item of res.scored) {
      assert.ok(item.score >= 18, `Expected ${item.seed} from 10_best.txt to score >= 18 Tens, got ${item.score}`);
    }
  });
});
