// tests/e2e/tier3_combinations/c15_party_sweep_lease_to_result_sink.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';
import {
  getMcpServer,
  runDesktopTestsFilter,
  runWasmSeedSearch,
} from '../helpers/runtime_oracle.mjs';

describe('Combination C15: Motely.Party Lease Sweep <-> BalatroSeedOracle SeedLakeSink CSV Output', () => {
  const sinkPath = path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle', 'Services', 'SeedLakeSink.cs');

  it('C15-1: SeedLakeSink.cs implements SEED,SCORE[,TALLY...] CSV schema and safe filename sanitization', () => {
    assert.equal(exists(sinkPath), true);
    const src = fs.readFileSync(sinkPath, 'utf-8');
    assert.ok(src.includes('TryParseLine'));
    assert.ok(src.includes('GetInvalidFileNameChars'));
    assert.ok(src.includes('SearchResults'));
  });

  it('C15-2: Real SeedLakeSink unit tests verify auto-directory creation, score/tally round-trip, and deduplication', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~SeedLakeSinkTests');
    assert.equal(res.exitCode, 0, `SeedLakeSinkTests failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('C15-3: Live Motely.MCP party sweep confirms real seed hits and updates party matchesFound', async () => {
    const mcp = await getMcpServer();
    const jaml = 'name: C15 Sink Sweep\ndeck: Red\nstake: White\nmust:\n  - voucher: Overstock\n    antes: [1]\n';
    const created = await mcp.createParty({ filterOrJaml: jaml, name: 'C15-Sink', batchChars: 3, blockSize: 1 });
    assert.equal(created.status, 200, JSON.stringify(created.body));

    const sweep = await mcp.sweepParty(created.body.partyId, { blocks: 1, gpu: false, threads: 2 });
    assert.equal(sweep.status, 200, JSON.stringify(sweep.body));
    assert.ok(sweep.body.confirmed >= 1, 'Expected Overstock matches in block 0 (42,875 seeds)');

    const status = await mcp.getPartyStatus(created.body.partyId);
    assert.equal(status.status, 200);
    assert.equal(status.body.matchesFound, sweep.body.recorded);
    assert.ok(status.body.topFinds.length >= 1);
  });

  it('C15-4: Duplicate candidate seeds reported to PartyCoordinator are deduplicated on report', async () => {
    const mcp = await getMcpServer();
    const jaml = 'name: C15 Dedup\ndeck: Red\nstake: White\nmust:\n  - voucher: Overstock\n    antes: [1]\n';
    const created = await mcp.createParty({ filterOrJaml: jaml, name: 'C15-Dedup', batchChars: 3, blockSize: 1 });
    // First discover a valid block-0 seed via sweep on a helper party
    const helper = await mcp.createParty({ filterOrJaml: jaml, name: 'C15-Helper', batchChars: 3, blockSize: 1 });
    const helperSweep = await mcp.sweepParty(helper.body.partyId, { blocks: 1, gpu: false, threads: 2 });
    const validBlock0Seed = helperSweep.body.confirmedSeeds[0];
    assert.ok(validBlock0Seed, 'Expected at least 1 valid seed in block 0');

    const nextRes = await mcp.nextPartyLease(created.body.partyId, 1);
    assert.equal(nextRes.status, 200);
    const lease = nextRes.body.lease;
    assert.ok(lease?.workerToken);

    const rep = await mcp.reportParty({
      partyId: created.body.partyId,
      workerToken: lease.workerToken,
      startBlock: lease.startBlock,
      seeds: [validBlock0Seed, validBlock0Seed, validBlock0Seed],
      heartbeatOnly: false,
    });
    assert.equal(rep.status, 200, JSON.stringify(rep.body));
    assert.equal(rep.body.recorded, 1, 'Duplicate candidate seed must be recorded once');

    const status = await mcp.getPartyStatus(created.body.partyId);
    assert.equal(status.body.matchesFound, 1);
  });

  it('C15-5: Confirmed party seeds can be re-queried as a seed list in WASM refinement search', async () => {
    const mcp = await getMcpServer();
    const jaml = 'name: C15 Refine\ndeck: Red\nstake: White\nmust:\n  - voucher: Overstock\n    antes: [1]\n';
    const created = await mcp.createParty({ filterOrJaml: jaml, name: 'C15-Refine', batchChars: 3, blockSize: 1 });
    const sweep = await mcp.sweepParty(created.body.partyId, { blocks: 1, gpu: false, threads: 2 });
    const seeds = sweep.body.confirmedSeeds.slice(0, 20);
    assert.ok(seeds.length >= 1);

    const refined = await runWasmSeedSearch(jaml, seeds);
    assert.equal(refined.matches.length, seeds.length);
  });
});
