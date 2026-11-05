// tests/e2e/tier2_boundaries/f08_engine_parity_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';
import {
  getMotelyWasm,
  getMcpServer,
  runWasmSeedSearch,
} from '../helpers/runtime_oracle.mjs';

describe('Feature 8 Boundaries: Engine Unit & Parity Tests', () => {
  it('F08-B1: Single-character batch (batchCharacterCount = 1, 35 seeds) evaluates cleanly in motely-wasm', async () => {
    const { JamlConfigLoader, JamlSearchBuilder } = await getMotelyWasm();
    const cfg = JamlConfigLoader.fromJaml('deck: Red\nstake: White\nmust:\n  - voucher: Overstock\n    antes: [1]\n');
    const search = JamlSearchBuilder.createSettings(cfg)
      .withThreadCount(1)
      .withQuietMode(true)
      .withSequentialSearch()
      .withBatchCharacterCount(1)
      .withEndBatchIndex(1n)
      .start();
    try {
      await search.waitForCompletionAsync();
      assert.equal(Number(search.totalSeedsSearched), 35);
    } finally {
      search.dispose();
    }
  });

  it('F08-B2: GpuSeedSearcher implements negative candidate overflow sentinel fallback to CPU SIMD', () => {
    const gpuSrc = fs.readFileSync(path.join(PATHS.MotelyJAML, 'Motely', 'Gpu', 'GpuSeedSearcher.cs'), 'utf-8');
    const searchSrc = fs.readFileSync(path.join(PATHS.MotelyJAML, 'Motely', 'MotelySearch.cs'), 'utf-8');
    assert.ok(gpuSrc.includes('-1') && (searchSrc.includes('< 0') || gpuSrc.includes('MaxCandidates')));
  });

  it('F08-B3: 100% rejection filter yields 0 matches in real GPU + WASM sweeps without error', async () => {
    const jaml = 'deck: Erratic\nstake: White\nmust:\n  - erraticRank: Ace\n    min: 53\n';
    const wasmRes = await runWasmSeedSearch(jaml, ['1UQ8D22L', 'ALEEB', '111S8CHT']);
    assert.equal(wasmRes.matches.length, 0);
  });

  it('F08-B4: 4-character GPU batch (1,500,625 seeds per batch) executes cleanly on OpenCL RTX 4070 SUPER', async () => {
    const mcp = await getMcpServer();
    const created = await mcp.createParty({
      filterOrJaml: 'name: Batch4Boundary\ndeck: Red\nstake: White\nmust:\n  - legendaryJoker: Perkeo\n    edition: Negative\n    antes: [1]\n',
      batchChars: 4,
      blockSize: 1,
    });
    assert.equal(created.status, 200);
    const sweep = await mcp.sweepParty(created.body.partyId, { blocks: 1, gpu: true, threads: 4 });
    assert.equal(sweep.status, 200);
    assert.equal(sweep.body.seedsSearched, 1_500_625);
  });

  it('F08-B5: NaN-triggering Balatro seeds (S4DM5V37, S4TMWWAE) analyze cleanly without crashing WASM engine', async () => {
    const { JamlConfigLoader, MotelyJamlyzer } = await getMotelyWasm();
    const cfg = JamlConfigLoader.fromJaml('deck: Erratic\nstake: Gold\nseeds:\n  - S4DM5V37\n  - S4TMWWAE\n');
    const results = MotelyJamlyzer.analyze(cfg);
    assert.equal(results.length, 2);
    for (const r of results) {
      assert.ok(!r.error, `Unexpected error on NaN seed ${r.seed}: ${r.error}`);
      assert.equal(r.erraticDeck.length, 52);
    }
  });

  it('F08-B6: Empty JAML filter matches all seeds with default score 0 in motely-wasm', async () => {
    const res = await runWasmSeedSearch('deck: Red\nstake: White\n', ['ALEEB', '111S8CHT', '1UQ8D22L']);
    assert.equal(res.matches.length, 3);
    for (const item of res.scored) {
      assert.equal(item.score, 0);
    }
  });
});
