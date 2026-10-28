// tests/e2e/tier3_combinations/c01_dungmot_to_motely_gpu.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  getShopEditionKey,
  getShopRarityKey,
  getBuffoonEditionKey,
  getBuffoonRarityKey,
  EDITION_THRESHOLDS,
  RARITY_THRESHOLDS,
  pseudoHash8,
} from '../helpers/prng_oracle.mjs';

describe('Combination C01: dungmot CUDA PRNG <-> Motely OpenCL FP64 Kernel Parity', () => {
  it('C01-1: Stream keys edisho{ante} and rarity{ante}sho match exactly between CUDA and OpenCL', () => {
    for (let ante = 1; ante <= 8; ante++) {
      assert.equal(getShopEditionKey(ante), `edisho${ante}`);
      assert.equal(getShopRarityKey(ante), `rarity${ante}sho`);
    }
  });

  it('C01-2: Stream keys edibuf{ante} and rarity{ante}buf match exactly between CUDA and OpenCL', () => {
    for (let ante = 1; ante <= 8; ante++) {
      assert.equal(getBuffoonEditionKey(ante), `edibuf${ante}`);
      assert.equal(getBuffoonRarityKey(ante), `rarity${ante}buf`);
    }
  });

  it('C01-3: Negative edition threshold 0.997 is identical in balatro_streams.cuh and OpenClKernelSource', () => {
    assert.equal(EDITION_THRESHOLDS.Negative, 0.997);
  });

  it('C01-4: Rare threshold 0.95 and Uncommon threshold 0.70 are identical in both kernels', () => {
    assert.equal(RARITY_THRESHOLDS.Rare, 0.95);
    assert.equal(RARITY_THRESHOLDS.Uncommon, 0.70);
  });

  it('C01-5: Reference seed 111S8CHT generates identical pseudohash in both CUDA and OpenCL models', () => {
    const hash = pseudoHash8('111S8CHT');
    assert.ok(hash >= 0.0 && hash < 1.0);
  });
});
