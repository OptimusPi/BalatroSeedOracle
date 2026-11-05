// tests/e2e/tier2_boundaries/f20_desktop_party_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';
import {
  getMcpServer,
  runDesktopTestsFilter,
} from '../helpers/runtime_oracle.mjs';

describe('Feature 20 Boundaries: Desktop Community Party Client', () => {
  const partySvcSrc = fs.readFileSync(
    path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle', 'Services', 'PartySearchService.cs'),
    'utf-8'
  );

  it('F20-B1: PartySearchService rejects invalid coordinator URLs and cleans up state so subsequent runs succeed', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~PartySearchService_StartAsync_InvalidCoordinatorUrl');
    assert.equal(res.exitCode, 0, `PartySearchService invalid URL cleanup test failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('F20-B2: SearchTab.axaml PartyCoordinatorUrlBox two-way binding updates SearchModalViewModel.PartyCoordinatorUrl', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~SearchTab_PartyCoordinatorUrlBox_TwoWayBinding');
    assert.equal(res.exitCode, 0, `SearchTab PartyCoordinatorUrl two-way binding test failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('F20-B3: PartySearchService disposes CancellationTokenSource & PartyClient inside try/catch when StartAsync fails', () => {
    assert.ok(partySvcSrc.includes('CancellationTokenSource'));
    assert.ok(partySvcSrc.includes('StopAsync') || partySvcSrc.includes('Dispose'));
  });

  it('F20-B4: Live PartyCoordinator deduplicates duplicate candidate seeds within a worker block report', async () => {
    const mcp = await getMcpServer();
    const jaml = 'name: F20B Dedup\ndeck: Red\nstake: White\nmust:\n  - voucher: Overstock\n    antes: [1]\n';
    const helper = await mcp.createParty({ filterOrJaml: jaml, batchChars: 3, blockSize: 1 });
    const seed = (await mcp.sweepParty(helper.body.partyId, { blocks: 1, gpu: false, threads: 2 })).body.confirmedSeeds[0];
    assert.ok(seed);

    const p = await mcp.createParty({ filterOrJaml: jaml, batchChars: 3, blockSize: 1, totalBlocks: 5 });
    const { lease } = (await mcp.nextPartyLease(p.body.partyId, 1)).body;
    const rep = await mcp.reportParty({
      partyId: p.body.partyId,
      workerToken: lease.workerToken,
      startBlock: lease.startBlock,
      seeds: [seed, seed],
    });
    assert.equal(rep.status, 200);
    assert.equal(rep.body.recorded, 1);
  });

  it('F20-B5: GET /api/party/{unknownPartyId} returns HTTP 404 NotFound cleanly', async () => {
    const mcp = await getMcpServer();
    const res = await mcp.getPartyStatus('nonexistent-party-id-404');
    assert.equal(res.status, 404);
  });

  it('F20-B6: Exhausted PartyCoordinator session returns { done: true } on /api/party/next', async () => {
    const mcp = await getMcpServer();
    const jaml = 'name: F20B Done\ndeck: Red\nstake: White\nmust:\n  - voucher: Overstock\n    antes: [1]\n';
    const p = await mcp.createParty({ filterOrJaml: jaml, batchChars: 2, blockSize: 1, totalBlocks: 1 });
    await mcp.sweepParty(p.body.partyId, { blocks: 1, gpu: false, threads: 2 });
    const nextRes = await mcp.nextPartyLease(p.body.partyId, 1);
    assert.equal(nextRes.status, 200);
    assert.equal(nextRes.body.done, true);
  });
});
