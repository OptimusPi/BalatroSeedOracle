// tests/e2e/tier1_features/f01_shop_joker_gpu.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';
import {
  getShopEditionKey,
  getShopRarityKey,
  EDITION_THRESHOLDS,
  RARITY_THRESHOLDS,
  classifyEdition,
  classifyRarity,
} from '../helpers/prng_oracle.mjs';
import { runMotelyTestsFilter } from '../helpers/runtime_oracle.mjs';

describe('Feature 1: Shop Joker GPU Prefilter (dungmot -> Motely)', () => {
  const gpuSearcherPath = path.join(PATHS.MotelyJAML, 'Motely', 'Gpu', 'GpuSeedSearcher.cs');
  const gpuSource = fs.readFileSync(gpuSearcherPath, 'utf-8');

  it('F01-1: Shop edition PRNG stream key matches edisho{ante} contract in OpenCL & oracle for Ante 1-8', () => {
    assert.ok(gpuSource.includes('edisho'), 'OpenCL kernel must emit edisho{ante} stream key');
    for (let ante = 1; ante <= 8; ante++) {
      const key = getShopEditionKey(ante);
      assert.equal(key, `edisho${ante}`);
      assert.equal(key.length, 7);
    }
  });

  it('F01-2: Shop edition PRNG stream key matches edisho{ante} contract for Ante 10-16', () => {
    for (let ante = 10; ante <= 16; ante++) {
      const key = getShopEditionKey(ante);
      assert.equal(key, `edisho${ante}`);
      assert.equal(key.length, 8);
    }
  });

  it('F01-3: Shop rarity PRNG stream key matches rarity{ante}sho contract in OpenCL & oracle', () => {
    assert.ok(gpuSource.includes('rarity') && gpuSource.includes('sho'), 'OpenCL kernel must emit rarity{ante}sho key');
    assert.equal(getShopRarityKey(1), 'rarity1sho');
    assert.equal(getShopRarityKey(5), 'rarity5sho');
    assert.equal(getShopRarityKey(8), 'rarity8sho');
    assert.equal(getShopRarityKey(12), 'rarity12sho');
  });

  it('F01-4: Negative edition threshold strictly requires roll > 0.997 in OpenCL kernel and oracle', () => {
    assert.ok(gpuSource.includes('0.997'), 'OpenCL kernel must compare against 0.997 Negative threshold');
    assert.equal(EDITION_THRESHOLDS.Negative, 0.997);
    assert.equal(classifyEdition(0.9970000000001), 'Negative');
    assert.notEqual(classifyEdition(0.997), 'Negative');
  });

  it('F01-5: Rare joker threshold strictly requires roll > 0.95 and classifies correctly', () => {
    assert.ok(gpuSource.includes('0.95'), 'OpenCL kernel must compare against 0.95 Rare threshold');
    assert.equal(RARITY_THRESHOLDS.Rare, 0.95);
    assert.equal(classifyRarity(0.9500001), 'Rare');
    assert.equal(classifyRarity(0.95), 'Uncommon');
    assert.equal(classifyRarity(0.7000001), 'Uncommon');
    assert.equal(classifyRarity(0.70), 'Common');
  });

  it('F01-6: GpuFilterProgram defines OpShopJoker = 8 and passes live OpenCL vs CPU ShopJokerParity test', () => {
    const m = gpuSource.match(/public\s+const\s+int\s+OpShopJoker\s*=\s*(\d+)\s*;/);
    assert.ok(m, 'GpuFilterProgram must declare public const int OpShopJoker');
    assert.equal(Number(m[1]), 8, 'OpShopJoker bytecode opcode must equal 8');

    const res = runMotelyTestsFilter('FullyQualifiedName~Gpu_and_Cpu_sequential_sweeps_produce_bit_exact_identical_matches_and_scores');
    assert.equal(res.exitCode, 0, `GpuSearchTests ShopJokerParity failed:\n${res.stdout}\n${res.stderr}`);
  });
});
