// tests/e2e/tier2_boundaries/f03_gpu_compiler_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';
import {
  getMcpServer,
  getMotelyWasm,
} from '../helpers/runtime_oracle.mjs';

describe('Feature 3 Boundaries: GPU Compiler Clause Emission', () => {
  const gpuSrc = fs.readFileSync(path.join(PATHS.MotelyJAML, 'Motely', 'Gpu', 'GpuSeedSearcher.cs'), 'utf-8');

  it('F03-B1: Empty JAML config (no must/mustNot clauses) runs sweep with gpuUsed = false', async () => {
    const mcp = await getMcpServer();
    const created = await mcp.createParty({
      filterOrJaml: 'name: EmptyFilter\ndeck: Red\nstake: White\n',
      batchChars: 2,
      blockSize: 1,
    });
    assert.equal(created.status, 200);
    const sweep = await mcp.sweepParty(created.body.partyId, { blocks: 1, gpu: true, threads: 2 });
    assert.equal(sweep.status, 200);
    assert.equal(sweep.body.gpuUsed, false, 'Empty filter must have HasSelectiveOps=false -> gpuUsed=false');
    assert.equal(sweep.body.seedsSearched, 35 * 35);
  });

  it('F03-B2: Non-GPU-accelerated clause (e.g. tarotCard) bypasses GPU prefilter cleanly and evaluates on CPU', async () => {
    const mcp = await getMcpServer();
    const created = await mcp.createParty({
      filterOrJaml: 'name: TarotOnly\ndeck: Red\nstake: White\nmust:\n  - tarotCard: TheHermit\n    antes: [1]\n',
      batchChars: 2,
      blockSize: 1,
    });
    assert.equal(created.status, 200);
    const sweep = await mcp.sweepParty(created.body.partyId, { blocks: 1, gpu: true, threads: 2 });
    assert.equal(sweep.status, 200);
    assert.equal(sweep.body.gpuUsed, false);
    assert.ok(sweep.body.confirmed >= 1);
  });

  it('F03-B3: Empty antes: [] list is rejected or produces zero GPU opcodes without crashing compiler', async () => {
    const { JamlConfigLoader } = await getMotelyWasm();
    const checkErr = JamlConfigLoader.check('deck: Red\nstake: White\nmust:\n  - voucher: Overstock\n    antes: []\n');
    // Either validator flags empty antes or compiler handles it without NativeAOT crash
    if (checkErr === null) {
      const cfg = JamlConfigLoader.fromJaml('deck: Red\nstake: White\nmust:\n  - voucher: Overstock\n    antes: []\n');
      assert.ok(cfg);
    } else {
      assert.ok(checkErr.length > 0);
    }
  });

  it('F03-B4: min: 0 clause passes all 1,225 seeds in a 2-char batch', async () => {
    const mcp = await getMcpServer();
    const created = await mcp.createParty({
      filterOrJaml: 'name: MinZero\ndeck: Red\nstake: White\nmust:\n  - voucher: Overstock\n    min: 0\n    antes: [1]\n',
      batchChars: 2,
      blockSize: 1,
    });
    assert.equal(created.status, 200);
    const sweep = await mcp.sweepParty(created.body.partyId, { blocks: 1, gpu: true, threads: 2 });
    assert.equal(sweep.status, 200);
    assert.equal(sweep.body.matched, 35 * 35);
    assert.equal(sweep.body.confirmed, 1000);
  });

  it('F03-B5: Impossible min: 100 ErraticRank clause safely filters out 100% of seeds on GPU with 0 survivors', async () => {
    const mcp = await getMcpServer();
    const created = await mcp.createParty({
      filterOrJaml: 'name: ImpossibleErratic\ndeck: Erratic\nstake: White\nmust:\n  - erraticRank: Ace\n    min: 100\n',
      batchChars: 3,
      blockSize: 1,
    });
    assert.equal(created.status, 200);
    const sweep = await mcp.sweepParty(created.body.partyId, { blocks: 1, gpu: true, threads: 2 });
    assert.equal(sweep.status, 200);
    assert.equal(sweep.body.confirmed, 0);
  });

  it('F03-B6: Invalid edition name in JAML is caught by JamlConfigLoader.check before bytecode compilation', async () => {
    const { JamlConfigLoader } = await getMotelyWasm();
    const err = JamlConfigLoader.check('deck: Red\nstake: White\nmust:\n  - rareJoker: []\n    edition: FakeEdition99\n    antes: [1]\n');
    assert.ok(err && err.includes('FakeEdition99'), `Expected JAML validation error on FakeEdition99, got: ${err}`);
    assert.ok(gpuSrc.includes('Compile(JamlConfig? config)'));
  });
});
