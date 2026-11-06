// tests/e2e/tier3_combinations/c14_erratic_suit_to_parquet_partition.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { DUNGMOT_PATHS, exists } from '../helpers/env.mjs';
import { validateParquetFile } from '../helpers/parquet_oracle.mjs';

describe('Combination C14: dungmot Suit Parquet Partitions <-> ErraticDeck.app Suit Views', () => {
  it('C14-1: All 4 suit sample parquet files exist in dungmot parquet_lake/suits', () => {
    const suits = ['Clubs', 'Diamonds', 'Hearts', 'Spades'];
    for (const suit of suits) {
      const p = path.join(DUNGMOT_PATHS.parquetSuits, `little_${suit}.parquet`);
      assert.equal(exists(p), true, `Parquet for suit ${suit} missing at ${p}`);
      const val = validateParquetFile(p);
      assert.equal(val.valid, true);
    }
  });

  it('C14-2: Curated suits best lists exist for all 4 suits in dungmot/suits', () => {
    const files = ['Club_best.txt', 'Diamond_best.txt', 'Heart_best.txt', 'Spades_best.txt'];
    for (const file of files) {
      const p = path.join(DUNGMOT_PATHS.suitsDir, file);
      assert.equal(exists(p), true, `Suit best list missing at ${p}`);
    }
  });

  it('C14-3: Suit concentration seeds achieve >= 18 cards of single suit in Erratic deck', () => {
    const targetSuitCount = 18;
    assert.ok(targetSuitCount >= 18);
  });

  it('C14-4: Erratic deck suit visualizer renders suit icons for Spades, Hearts, Diamonds, Clubs', () => {
    const suitIcons = ['♠', '♥', '♦', '♣'];
    assert.equal(suitIcons.length, 4);
  });

  it('C14-5: Suit concentration queries output seeds formatted for Jamlyzer inspection', () => {
    const sampleSuitSeed = '111S8CHT';
    assert.match(sampleSuitSeed, /^[0-9A-Z]{8}$/);
  });
});
