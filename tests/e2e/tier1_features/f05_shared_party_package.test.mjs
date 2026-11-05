// tests/e2e/tier1_features/f05_shared_party_package.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';
import { getMcpServer, runMotelyTestsFilter } from '../helpers/runtime_oracle.mjs';

describe('Feature 5: Shared Motely.Party Package', () => {
  const jaml = 'name: F05 Party\ndeck: Red\nstake: White\nmust:\n  - voucher: Overstock\n    antes: [1]\n';

  it('F05-1: Live /api/party/next returns PartyLease with partyId, startBlock, blockCount, workerToken, expiresAt', async () => {
    const mcp = await getMcpServer();
    const created = await mcp.createParty({ filterOrJaml: jaml, batchChars: 3, blockSize: 5, totalBlocks: 20 });
    assert.equal(created.status, 200);

    const nextRes = await mcp.nextPartyLease(created.body.partyId, 5);
    assert.equal(nextRes.status, 200);
    const lease = nextRes.body.lease;
    assert.equal(lease.partyId, created.body.partyId);
    assert.equal(lease.startBlock, 0);
    assert.equal(lease.blockCount, 5);
    assert.ok(typeof lease.workerToken === 'string' && lease.workerToken.length > 0);
    assert.ok(!Number.isNaN(Date.parse(lease.expiresAt)));
  });

  it('F05-2: Live /api/party/report confirms real matching seeds and rejects non-matching seeds', async () => {
    const mcp = await getMcpServer();
    const helper = await mcp.createParty({ filterOrJaml: jaml, batchChars: 3, blockSize: 1 });
    const helperSweep = await mcp.sweepParty(helper.body.partyId, { blocks: 1, gpu: false, threads: 2 });
    const realSeed = helperSweep.body.confirmedSeeds[0];
    assert.ok(realSeed);

    const created = await mcp.createParty({ filterOrJaml: jaml, batchChars: 3, blockSize: 1, totalBlocks: 10 });
    const nextRes = await mcp.nextPartyLease(created.body.partyId, 1);
    const rep = await mcp.reportParty({
      partyId: created.body.partyId,
      workerToken: nextRes.body.lease.workerToken,
      startBlock: nextRes.body.lease.startBlock,
      seeds: [realSeed, 'ZZZZZZZZ'],
      heartbeatOnly: false,
    });
    assert.equal(rep.status, 200);
    assert.equal(rep.body.ok, true);
    assert.ok(rep.body.confirmed.includes(realSeed));
    assert.ok(rep.body.rejected.includes('ZZZZZZZZ'));
  });

  it('F05-3: Live /api/party/report supports heartbeatOnly: true without completing the leased block', async () => {
    const mcp = await getMcpServer();
    const created = await mcp.createParty({ filterOrJaml: jaml, batchChars: 3, blockSize: 2, totalBlocks: 10 });
    const nextRes = await mcp.nextPartyLease(created.body.partyId, 2);
    const hb = await mcp.reportParty({
      partyId: created.body.partyId,
      workerToken: nextRes.body.lease.workerToken,
      startBlock: nextRes.body.lease.startBlock,
      seeds: [],
      heartbeatOnly: true,
    });
    assert.equal(hb.status, 200);
    assert.equal(hb.body.ok, true);

    const status = await mcp.getPartyStatus(created.body.partyId);
    assert.equal(status.body.completedBlocks, 0, 'Heartbeat must keep lease active without marking blocks completed');
    assert.equal(status.body.activeLeases, 1);
  });

  it('F05-4: Sequential /api/party/next calls allocate contiguous non-overlapping block ranges', async () => {
    const mcp = await getMcpServer();
    const created = await mcp.createParty({ filterOrJaml: jaml, batchChars: 3, blockSize: 4, totalBlocks: 20 });
    const l1 = (await mcp.nextPartyLease(created.body.partyId, 4)).body.lease;
    const l2 = (await mcp.nextPartyLease(created.body.partyId, 4)).body.lease;
    assert.equal(l1.startBlock, 0);
    assert.equal(l1.blockCount, 4);
    assert.equal(l2.startBlock, 4);
    assert.equal(l2.blockCount, 4);
  });

  it('F05-5: PartyWorker.cs and PartyTests pass real C# unit test suite in Motely.Tests', () => {
    const workerFile = path.join(PATHS.MotelyJAML, 'Motely', 'Party', 'PartyWorker.cs');
    assert.equal(fs.existsSync(workerFile), true);
    const res = runMotelyTestsFilter('FullyQualifiedName~Party');
    assert.equal(res.exitCode, 0, `Motely.Party unit tests failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('F05-6: Live PartyCoordinator status endpoint computes accurate percentComplete and completedBlocks', async () => {
    const mcp = await getMcpServer();
    const created = await mcp.createParty({ filterOrJaml: jaml, batchChars: 3, blockSize: 1, totalBlocks: 4 });
    await mcp.sweepParty(created.body.partyId, { blocks: 2, gpu: false, threads: 2 });
    const status = await mcp.getPartyStatus(created.body.partyId);
    assert.equal(status.status, 200);
    assert.equal(status.body.completedBlocks, 2);
    assert.equal(status.body.totalBlocks, 4);
    assert.equal(status.body.percentComplete, 50.0);
  });
});
