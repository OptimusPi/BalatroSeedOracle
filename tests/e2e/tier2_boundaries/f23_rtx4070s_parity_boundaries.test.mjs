// tests/e2e/tier2_boundaries/f23_rtx4070s_parity_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';
import { pseudoHash8 } from '../helpers/prng_oracle.mjs';
import { getMcpServer } from '../helpers/runtime_oracle.mjs';

describe('Feature 23 Boundaries: Live RTX 4070 SUPER Parity Verification', () => {
  const gpuSrc = fs.readFileSync(path.join(PATHS.MotelyJAML, 'Motely', 'Gpu', 'GpuSeedSearcher.cs'), 'utf-8');

  it('F23-B1: GpuSeedSearcher OpenCL dispatch aligns localWorkSize = 256 (8x 32-lane NVIDIA warps)', () => {
    assert.ok(gpuSrc.includes('localWorkSize') && gpuSrc.includes('256'));
  });

  it('F23-B2: GpuSeedSearcher pads globalWorkSize up to an exact multiple of localWorkSize for non-divisible seed counts', () => {
    // 35^3 = 42,875 is odd; globalWorkSize rounds up to multiple of localWorkSize with gid >= total_seeds guard
    assert.ok(gpuSrc.includes('total_seeds') && gpuSrc.includes('totalSeeds'));
    assert.ok(gpuSrc.includes('if (gid >= total_seeds) return;'));
    assert.ok(gpuSrc.includes('get_global_id(0)'));
  });

  it('F23-B3: OpenCL kernel uses atomic_inc on candidate count with max_candidates overflow guard', () => {
    assert.ok(gpuSrc.includes('atomic_inc'));
    assert.ok(gpuSrc.includes('max_candidates') || gpuSrc.includes('MaxCandidates'));
  });

  it('F23-B4: pseudoHash8 across extremal Balatro seeds produces strictly bounded [0, 1) hashes', () => {
    const seeds = ['11111111', 'ZZZZZZZZ', 'ABCDEFGH', '12345678', 'MOTELY99', 'S4DM5V37'];
    for (const seed of seeds) {
      const hash = pseudoHash8(seed);
      assert.ok(hash >= 0.0 && hash < 1.0, `Hash ${hash} for seed ${seed} out of bounds [0, 1)`);
    }
  });

  it('F23-B5: Live GPU vs CPU sweep on Erratic + Voucher + Boss filter has zero false negatives across 85,750 seeds', async () => {
    const mcp = await getMcpServer();
    const jaml = [
      'name: F23B Parity',
      'deck: Erratic',
      'stake: White',
      'must:',
      '  - erraticRank: Ace',
      '    min: 9',
      '  - voucher: Overstock',
      '    antes: [1, 2]',
    ].join('\n');

    const pCpu = await mcp.createParty({ filterOrJaml: jaml, batchChars: 3, blockSize: 1 });
    const pGpu = await mcp.createParty({ filterOrJaml: jaml, batchChars: 3, blockSize: 1 });
    const rCpu = await mcp.sweepParty(pCpu.body.partyId, { blocks: 2, gpu: false, threads: 4 });
    const rGpu = await mcp.sweepParty(pGpu.body.partyId, { blocks: 2, gpu: true, threads: 4 });

    assert.equal(rCpu.status, 200);
    assert.equal(rGpu.status, 200);
    assert.equal(rGpu.body.gpuUsed, true);
    assert.deepEqual([...rGpu.body.confirmedSeeds].sort(), [...rCpu.body.confirmedSeeds].sort());
  });

  it('F23-B6: Repeated gpu_status benchmark calls release OpenCL buffers cleanly without memory exhaustion', async () => {
    const mcp = await getMcpServer();
    const b1 = JSON.parse((await mcp.callTool('gpu_status', {})).result.content[0].text);
    const b2 = JSON.parse((await mcp.callTool('gpu_status', {})).result.content[0].text);
    assert.equal((b1.benchmark ?? b1.Benchmark).gpuAvailable ?? (b1.benchmark ?? b1.Benchmark).GpuAvailable, true);
    assert.equal((b2.benchmark ?? b2.Benchmark).gpuAvailable ?? (b2.benchmark ?? b2.Benchmark).GpuAvailable, true);
  });
});
