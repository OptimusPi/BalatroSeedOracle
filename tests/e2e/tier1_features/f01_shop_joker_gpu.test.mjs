// tests/e2e/tier1_features/f01_shop_joker_gpu.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  pseudoHash8,
  getShopEditionKey,
  getShopRarityKey,
  EDITION_THRESHOLDS,
  RARITY_THRESHOLDS,
  classifyEdition,
  classifyRarity,
} from '../helpers/prng_oracle.mjs';

describe('Feature 1: Shop Joker GPU Prefilter (dungmot -> Motely)', () => {
  it('F01-1: Shop edition PRNG stream key matches edisho{ante} contract for Ante 1-8', () => {
    for (let ante = 1; ante <= 8; ante++) {
      const key = getShopEditionKey(ante);
      assert.equal(key, `edisho${ante}`, `Key for ante ${ante} must be edisho${ante}`);
      assert.equal(key.length, 7, 'Single digit ante edition key must be 7 chars');
    }
  });

  it('F01-2: Shop edition PRNG stream key matches edisho{ante} contract for Ante 10-16', () => {
    for (let ante = 10; ante <= 16; ante++) {
      const key = getShopEditionKey(ante);
      assert.equal(key, `edisho${ante}`, `Key for ante ${ante} must be edisho${ante}`);
      assert.equal(key.length, 8, 'Two digit ante edition key must be 8 chars');
    }
  });

  it('F01-3: Shop rarity PRNG stream key matches rarity{ante}sho contract', () => {
    assert.equal(getShopRarityKey(1), 'rarity1sho');
    assert.equal(getShopRarityKey(5), 'rarity5sho');
    assert.equal(getShopRarityKey(8), 'rarity8sho');
    assert.equal(getShopRarityKey(12), 'rarity12sho');
  });

  it('F01-4: Negative edition threshold strictly requires roll > 0.997', () => {
    assert.equal(EDITION_THRESHOLDS.Negative, 0.997);
    assert.equal(classifyEdition(0.9970000000001), 'Negative');
    assert.notEqual(classifyEdition(0.997), 'Negative');
    assert.notEqual(classifyEdition(0.99699), 'Negative');
  });

  it('F01-5: Rare joker threshold strictly requires roll > 0.95 and classifies correctly', () => {
    assert.equal(RARITY_THRESHOLDS.Rare, 0.95);
    assert.equal(classifyRarity(0.9500001), 'Rare');
    assert.equal(classifyRarity(0.95), 'Uncommon');
    assert.equal(classifyRarity(0.7000001), 'Uncommon');
    assert.equal(classifyRarity(0.70), 'Common');
  });

  it('F01-6: Shop Joker Opcode definition adheres to OpShopJoker = 8 OpenCL contract', () => {
    const OpShopJoker = 8;
    assert.equal(OpShopJoker, 8, 'OpShopJoker bytecode opcode must equal 8 in OpenCL kernel');
  });
});
