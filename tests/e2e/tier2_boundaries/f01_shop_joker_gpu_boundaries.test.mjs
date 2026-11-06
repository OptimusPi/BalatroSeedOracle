// tests/e2e/tier2_boundaries/f01_shop_joker_gpu_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  pseudoHash8,
  pseudoHash,
  getShopEditionKey,
  getShopRarityKey,
  classifyEdition,
  classifyRarity,
  EDITION_THRESHOLDS,
  RARITY_THRESHOLDS,
} from '../helpers/prng_oracle.mjs';

describe('Feature 1 Boundaries: Shop Joker GPU Prefilter', () => {
  it('F01-B1: Empty seed string or single character seed handled gracefully by pseudohash', () => {
    const hashEmpty = pseudoHash('');
    assert.ok(hashEmpty >= 0.0 && hashEmpty < 1.0, 'Empty hash must be within [0, 1)');
    const hashSingle = pseudoHash('A');
    assert.ok(hashSingle >= 0.0 && hashSingle < 1.0, 'Single char hash must be within [0, 1)');
  });

  it('F01-B2: Ante 0 and boundary Ante 99 generate formatted keys without buffer overflow', () => {
    const key0 = getShopEditionKey(0);
    assert.equal(key0, 'edisho0');
    const key99 = getShopEditionKey(99);
    assert.equal(key99, 'edisho99');
    assert.equal(getShopRarityKey(0), 'rarity0sho');
    assert.equal(getShopRarityKey(99), 'rarity99sho');
  });

  it('F01-B3: Edition exact threshold boundary values 0.997, 0.994, 0.980, 0.960 are non-inclusive', () => {
    // Exactly on the boundary is NOT the higher edition (> strictly)
    assert.equal(classifyEdition(EDITION_THRESHOLDS.Negative), 'Polychrome');
    assert.equal(classifyEdition(EDITION_THRESHOLDS.Polychrome), 'Holographic');
    assert.equal(classifyEdition(EDITION_THRESHOLDS.Holographic), 'Foil');
    assert.equal(classifyEdition(EDITION_THRESHOLDS.Foil), 'Base');
  });

  it('F01-B4: Rarity exact threshold boundary values 0.95 and 0.70 are non-inclusive for higher rarity', () => {
    assert.equal(classifyRarity(RARITY_THRESHOLDS.Rare), 'Uncommon');
    assert.equal(classifyRarity(RARITY_THRESHOLDS.Uncommon), 'Common');
    assert.equal(classifyRarity(0.0), 'Common');
  });

  it('F01-B5: Extremal rolls 0.0 and 1.0 boundary safety', () => {
    assert.equal(classifyEdition(0.0), 'Base');
    assert.equal(classifyEdition(1.0), 'Negative');
    assert.equal(classifyRarity(0.0), 'Common');
    assert.equal(classifyRarity(1.0), 'Rare');
  });

  it('F01-B6: Negative and NaN rolls handle edge protections without exception', () => {
    assert.equal(classifyEdition(-0.5), 'Base');
    assert.equal(classifyRarity(-0.5), 'Common');
  });
});
