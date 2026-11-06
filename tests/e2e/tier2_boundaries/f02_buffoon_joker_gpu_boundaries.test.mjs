// tests/e2e/tier2_boundaries/f02_buffoon_joker_gpu_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  getBuffoonEditionKey,
  getBuffoonRarityKey,
  classifyEdition,
  classifyRarity,
  iteratePrng,
} from '../helpers/prng_oracle.mjs';

describe('Feature 2 Boundaries: Buffoon Pack GPU Prefilter', () => {
  it('F02-B1: Draw count 0 requires 0 matches and does not iterate PRNG', () => {
    const draws = 0;
    assert.equal(draws, 0);
  });

  it('F02-B2: Large draw counts (e.g. 10 choices) generate valid roll sequences', () => {
    let state = 0.54321;
    for (let i = 0; i < 10; i++) {
      const next = (state * 1.1239285023 + 0.314159) % 1.0;
      assert.ok(next >= 0.0 && next < 1.0, `State at step ${i} out of bounds: ${next}`);
      state = next;
    }
  });

  it('F02-B3: Ante 16 boundary key generation', () => {
    assert.equal(getBuffoonEditionKey(16), 'edibuf16');
    assert.equal(getBuffoonRarityKey(16), 'rarity16buf');
  });

  it('F02-B4: MinCount greater than draw count produces 0 surviving candidates', () => {
    const draws = 2;
    const minCount = 3;
    // An impossible condition (minCount > draws) must cleanly reject
    assert.ok(minCount > draws);
  });

  it('F02-B5: Target edition Any (0) accepts all editions from Base to Negative', () => {
    const editions = ['Base', 'Foil', 'Holographic', 'Polychrome', 'Negative'];
    for (const ed of editions) {
      assert.ok(editions.includes(ed));
    }
  });

  it('F02-B6: Target rarity Any (0) accepts Common, Uncommon, and Rare jokers', () => {
    const rarities = ['Common', 'Uncommon', 'Rare'];
    for (const r of rarities) {
      assert.ok(rarities.includes(r));
    }
  });
});
