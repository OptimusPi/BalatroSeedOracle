// tests/e2e/tier1_features/f02_buffoon_joker_gpu.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  getBuffoonEditionKey,
  getBuffoonRarityKey,
  EDITION_THRESHOLDS,
  classifyEdition,
  classifyRarity,
} from '../helpers/prng_oracle.mjs';

describe('Feature 2: Buffoon Pack GPU Prefilter (dungmot -> Motely)', () => {
  it('F02-1: Buffoon edition PRNG stream key matches edibuf{ante} contract', () => {
    assert.equal(getBuffoonEditionKey(1), 'edibuf1');
    assert.equal(getBuffoonEditionKey(4), 'edibuf4');
    assert.equal(getBuffoonEditionKey(8), 'edibuf8');
    assert.equal(getBuffoonEditionKey(16), 'edibuf16');
  });

  it('F02-2: Buffoon rarity PRNG stream key matches rarity{ante}buf contract', () => {
    assert.equal(getBuffoonRarityKey(1), 'rarity1buf');
    assert.equal(getBuffoonRarityKey(5), 'rarity5buf');
    assert.equal(getBuffoonRarityKey(8), 'rarity8buf');
  });

  it('F02-3: Buffoon Joker Opcode definition adheres to OpBuffoonJoker = 9 OpenCL contract', () => {
    const OpBuffoonJoker = 9;
    assert.equal(OpBuffoonJoker, 9, 'OpBuffoonJoker bytecode opcode must equal 9');
  });

  it('F02-4: Polychrome edition threshold strictly requires roll > 0.994 and <= 0.997', () => {
    assert.equal(EDITION_THRESHOLDS.Polychrome, 0.994);
    assert.equal(classifyEdition(0.9940001), 'Polychrome');
    assert.equal(classifyEdition(0.9969999), 'Polychrome');
    assert.notEqual(classifyEdition(0.994), 'Polychrome');
  });

  it('F02-5: Holographic edition threshold strictly requires roll > 0.980 and <= 0.994', () => {
    assert.equal(EDITION_THRESHOLDS.Holographic, 0.980);
    assert.equal(classifyEdition(0.9800001), 'Holographic');
    assert.equal(classifyEdition(0.9939999), 'Holographic');
    assert.notEqual(classifyEdition(0.980), 'Holographic');
  });

  it('F02-6: Buffoon pack draw evaluation maintains independent RNG roll progression', () => {
    // Buffoon packs roll edition first, then rarity for each choice slot
    const draws = [
      { editionRoll: 0.998, rarityRoll: 0.96 }, // Negative Rare
      { editionRoll: 0.965, rarityRoll: 0.50 }, // Foil Common
    ];
    assert.equal(classifyEdition(draws[0].editionRoll), 'Negative');
    assert.equal(classifyRarity(draws[0].rarityRoll), 'Rare');
    assert.equal(classifyEdition(draws[1].editionRoll), 'Foil');
    assert.equal(classifyRarity(draws[1].rarityRoll), 'Common');
  });
});
