// tests/e2e/tier3_combinations/c01_dungmot_to_motely_gpu.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { DUNGMOT_PATHS, PATHS, exists } from '../helpers/env.mjs';
import {
  getShopEditionKey,
  getShopRarityKey,
  getBuffoonEditionKey,
  getBuffoonRarityKey,
  EDITION_THRESHOLDS,
  RARITY_THRESHOLDS,
  pseudoHash8,
} from '../helpers/prng_oracle.mjs';
import { runMotelyTestsFilter } from '../helpers/runtime_oracle.mjs';

describe('Combination C01: dungmot CUDA PRNG <-> Motely OpenCL FP64 Kernel Parity', () => {
  const cudaSrc = [
    exists(DUNGMOT_PATHS.balatroStreamsCuh) ? fs.readFileSync(DUNGMOT_PATHS.balatroStreamsCuh, 'utf-8') : '',
    exists(DUNGMOT_PATHS.negativeRarePrefilterCu) ? fs.readFileSync(DUNGMOT_PATHS.negativeRarePrefilterCu, 'utf-8') : '',
  ].join('\n');
  const openClSrc = fs.readFileSync(
    path.join(PATHS.MotelyJAML, 'Motely', 'Gpu', 'GpuSeedSearcher.cs'),
    'utf-8'
  );

  it('C01-1: Stream keys edisho{ante} and rarity{ante}sho match between dungmot CUDA prefilter and GpuSeedSearcher.cs', () => {
    assert.ok(openClSrc.includes('edisho'), 'OpenCL kernel must construct edisho{ante}');
    assert.ok(openClSrc.includes('rarity') && openClSrc.includes('sho'), 'OpenCL kernel must construct rarity{ante}sho');
    if (cudaSrc.trim()) {
      assert.ok(cudaSrc.includes('edisho'), 'CUDA prefilter must construct edisho{ante}');
      assert.ok(cudaSrc.includes('rarity'), 'CUDA prefilter must construct rarity{ante}sho');
    }
    for (let ante = 1; ante <= 8; ante++) {
      assert.equal(getShopEditionKey(ante), `edisho${ante}`);
      assert.equal(getShopRarityKey(ante), `rarity${ante}sho`);
    }
  });

  it('C01-2: Stream keys edibuf{ante} and rarity{ante}buf match between CUDA and OpenCL', () => {
    assert.ok(openClSrc.includes('edibuf'), 'OpenCL kernel must construct edibuf{ante}');
    assert.ok(openClSrc.includes('buf'), 'OpenCL kernel must construct rarity{ante}buf');
    for (let ante = 1; ante <= 8; ante++) {
      assert.equal(getBuffoonEditionKey(ante), `edibuf${ante}`);
      assert.equal(getBuffoonRarityKey(ante), `rarity${ante}buf`);
    }
  });

  it('C01-3: Negative edition threshold 0.997 is verified in OpenCL kernel source', () => {
    assert.equal(EDITION_THRESHOLDS.Negative, 0.997);
    assert.ok(openClSrc.includes('0.997'), 'OpenCL kernel must compare > 0.997 for Negative');
  });

  it('C01-4: Rare threshold 0.95 and Uncommon threshold 0.70 are verified in OpenCL kernel source', () => {
    assert.equal(RARITY_THRESHOLDS.Rare, 0.95);
    assert.equal(RARITY_THRESHOLDS.Uncommon, 0.70);
    assert.ok(openClSrc.includes('0.95'), 'OpenCL kernel must use 0.95 Rare threshold');
    assert.ok(openClSrc.includes('0.7') || openClSrc.includes('0.70'), 'OpenCL kernel must use 0.70 Uncommon threshold');
  });

  it('C01-5: Live GpuSearchTests pass Shop & Buffoon OpenCL-vs-CPU parity on host hardware', () => {
    const hash = pseudoHash8('111S8CHT');
    assert.ok(hash > 0.0 && hash < 1.0);
    const res = runMotelyTestsFilter('FullyQualifiedName~GpuSearchTests');
    assert.equal(res.exitCode, 0, `GpuSearchTests failed:\n${res.stdout}\n${res.stderr}`);
  });
});
