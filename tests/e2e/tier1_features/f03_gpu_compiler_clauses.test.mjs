// tests/e2e/tier1_features/f03_gpu_compiler_clauses.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';
import { EDITION_CODES, RARITY_CODES } from '../helpers/prng_oracle.mjs';
import { getMcpServer, runMotelyTestsFilter } from '../helpers/runtime_oracle.mjs';

describe('Feature 3: GPU Compiler Clause Emission (GpuFilterProgram)', () => {
  const gpuSearcherPath = path.join(PATHS.MotelyJAML, 'Motely', 'Gpu', 'GpuSeedSearcher.cs');
  const gpuSource = fs.readFileSync(gpuSearcherPath, 'utf-8');

  it('F03-1: GpuFilterProgram declares all 9 GPU opcodes (OpVoucher=1 .. OpBuffoonJoker=9) in GpuSeedSearcher.cs', () => {
    const expected = [
      ['OpVoucher', 1],
      ['OpTag', 2],
      ['OpBoss', 3],
      ['OpSoulEdition', 4],
      ['OpSoulPack', 5],
      ['OpErraticRank', 6],
      ['OpErraticSuit', 7],
      ['OpShopJoker', 8],
      ['OpBuffoonJoker', 9],
    ];
    for (const [name, code] of expected) {
      const m = gpuSource.match(new RegExp(`public\\s+const\\s+int\\s+${name}\\s*=\\s*(\\d+)\\s*;`));
      assert.ok(m, `Missing ${name} in GpuFilterProgram`);
      assert.equal(Number(m[1]), code, `${name} must equal ${code}`);
    }
  });

  it('F03-2: GpuFilterProgram.Empty initializes InstructionGroupCount = 0 and HasSelectiveOps = false', () => {
    assert.ok(gpuSource.includes('public static GpuFilterProgram Empty { get; } = new([0], 0);'));
    assert.ok(gpuSource.includes('public bool HasSelectiveOps => InstructionGroupCount > 0;'));
  });

  it('F03-3: Edition codes in GpuSeedSearcher.cs match OpenCL kernel constants (Any=0..Negative=4)', () => {
    assert.equal(EDITION_CODES.Any, 0);
    assert.equal(EDITION_CODES.Foil, 1);
    assert.equal(EDITION_CODES.Holographic, 2);
    assert.equal(EDITION_CODES.Polychrome, 3);
    assert.equal(EDITION_CODES.Negative, 4);
    assert.ok(gpuSource.includes('MotelyItemEdition.Negative'));
    assert.ok(gpuSource.includes('MotelyItemEdition.Polychrome'));
  });

  it('F03-4: Rarity codes match OpenCL kernel constants (Any=0, Common=1, Uncommon=2, Rare=3)', () => {
    assert.equal(RARITY_CODES.Any, 0);
    assert.equal(RARITY_CODES.Common, 1);
    assert.equal(RARITY_CODES.Uncommon, 2);
    assert.equal(RARITY_CODES.Rare, 3);
  });

  it('F03-5: Live Motely.MCP GPU sweep uses GpuUsed=true when selective opcodes are present and GpuUsed=false when empty', async () => {
    const mcp = await getMcpServer();
    const selective = await mcp.createParty({
      filterOrJaml: 'name: Selective\ndeck: Red\nstake: White\nmust:\n  - voucher: Overstock\n    antes: [1]\n',
      batchChars: 3,
      blockSize: 1,
    });
    const selSweep = await mcp.sweepParty(selective.body.partyId, { blocks: 1, gpu: true });
    assert.equal(selSweep.status, 200);

    const nonSelective = await mcp.createParty({
      filterOrJaml: 'name: NonSelective\ndeck: Red\nstake: White\nshould:\n  - voucher: Overstock\n    antes: [1]\n    score: 1\n',
      batchChars: 3,
      blockSize: 1,
    });
    const nonSelSweep = await mcp.sweepParty(nonSelective.body.partyId, { blocks: 1, gpu: true });
    assert.equal(nonSelSweep.status, 200);
    assert.equal(nonSelSweep.body.gpuUsed, false, 'Should-only filter has 0 must GPU opcodes so GpuUsed must be false');
  });

  it('F03-6: Live GpuSearchTests mixed must + mustNot clause compilation passes with zero false negatives', () => {
    const res = runMotelyTestsFilter('FullyQualifiedName~Gpu_mixed_special_spectral_and_mustNot_voucher_roll1_have_zero_false_negatives');
    assert.equal(res.exitCode, 0, `GpuSearchTests mixed clause test failed:\n${res.stdout}\n${res.stderr}`);
  });
});
