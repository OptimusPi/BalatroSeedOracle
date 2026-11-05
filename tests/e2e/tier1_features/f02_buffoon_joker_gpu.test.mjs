// tests/e2e/tier1_features/f02_buffoon_joker_gpu.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';
import {
  getBuffoonEditionKey,
  getBuffoonRarityKey,
  EDITION_THRESHOLDS,
  classifyEdition,
} from '../helpers/prng_oracle.mjs';
import { analyzeSeedWithWasm, getMotelyWasm } from '../helpers/runtime_oracle.mjs';

describe('Feature 2: Buffoon Pack GPU Prefilter (dungmot -> Motely)', () => {
  const gpuSearcherPath = path.join(PATHS.MotelyJAML, 'Motely', 'Gpu', 'GpuSeedSearcher.cs');
  const gpuSource = fs.readFileSync(gpuSearcherPath, 'utf-8');

  it('F02-1: Buffoon edition PRNG stream key matches edibuf{ante} contract in OpenCL & oracle', () => {
    assert.ok(gpuSource.includes('edibuf'), 'OpenCL kernel must emit edibuf{ante} stream key');
    assert.equal(getBuffoonEditionKey(1), 'edibuf1');
    assert.equal(getBuffoonEditionKey(4), 'edibuf4');
    assert.equal(getBuffoonEditionKey(8), 'edibuf8');
    assert.equal(getBuffoonEditionKey(16), 'edibuf16');
  });

  it('F02-2: Buffoon rarity PRNG stream key matches rarity{ante}buf contract in OpenCL & oracle', () => {
    assert.ok(gpuSource.includes('buf'), 'OpenCL kernel must emit rarity{ante}buf stream key');
    assert.equal(getBuffoonRarityKey(1), 'rarity1buf');
    assert.equal(getBuffoonRarityKey(5), 'rarity5buf');
    assert.equal(getBuffoonRarityKey(8), 'rarity8buf');
  });

  it('F02-3: GpuFilterProgram defines OpBuffoonJoker = 9 in GpuSeedSearcher.cs', () => {
    const m = gpuSource.match(/public\s+const\s+int\s+OpBuffoonJoker\s*=\s*(\d+)\s*;/);
    assert.ok(m, 'GpuFilterProgram must declare public const int OpBuffoonJoker');
    assert.equal(Number(m[1]), 9, 'OpBuffoonJoker bytecode opcode must equal 9');
  });

  it('F02-4: Polychrome edition threshold strictly requires roll > 0.994 and <= 0.997 in OpenCL & oracle', () => {
    assert.ok(gpuSource.includes('0.994'), 'OpenCL kernel must include 0.994 Polychrome threshold');
    assert.equal(EDITION_THRESHOLDS.Polychrome, 0.994);
    assert.equal(classifyEdition(0.9940001), 'Polychrome');
    assert.equal(classifyEdition(0.9969999), 'Polychrome');
    assert.notEqual(classifyEdition(0.994), 'Polychrome');
  });

  it('F02-5: Holographic edition threshold strictly requires roll > 0.980 and <= 0.994 in OpenCL & oracle', () => {
    assert.ok(gpuSource.includes('0.98'), 'OpenCL kernel must include 0.980 Holographic threshold');
    assert.equal(EDITION_THRESHOLDS.Holographic, 0.980);
    assert.equal(classifyEdition(0.9800001), 'Holographic');
    assert.equal(classifyEdition(0.9939999), 'Holographic');
    assert.notEqual(classifyEdition(0.980), 'Holographic');
  });

  it('F02-6: Live WASM MotelyJamlyzer simulates Buffoon booster pack joker draws across Antes 1-8', async () => {
    const { MotelyBoosterPack } = await getMotelyWasm();
    const res = await analyzeSeedWithWasm('111S8CHT', { deck: 'Red', stake: 'White' });
    assert.ok(!res.error);
    let buffoonPacksFound = 0;
    let totalPacksWithItems = 0;
    for (const ante of res.antes) {
      for (const p of ante.packs ?? []) {
        if ((p.items?.length ?? 0) > 0) totalPacksWithItems++;
        const packName = MotelyBoosterPack[p.pack] ?? String(p.pack ?? '');
        if (packName.includes('Buffoon')) {
          buffoonPacksFound++;
        }
      }
    }
    assert.ok(totalPacksWithItems > 0, 'Expected real booster packs with items across Antes 1-8');
    assert.ok(buffoonPacksFound > 0, 'Expected at least one Buffoon pack across Antes 1-8');
  });
});
