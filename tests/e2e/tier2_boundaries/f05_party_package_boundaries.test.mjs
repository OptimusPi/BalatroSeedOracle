// tests/e2e/tier2_boundaries/f05_party_package_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  getMcpServer,
  getSeedfinderPartyStore,
} from '../helpers/runtime_oracle.mjs';

describe('Feature 5 Boundaries: Shared Motely.Party Package', () => {
  const jaml = 'name: F05 Boundary\ndeck: Red\nstake: White\nmust:\n  - voucher: Overstock\n    antes: [1]\n';

  it('F05-B1: Forged or expired workerToken is rejected by live PartyCoordinator /api/party/report', async () => {
    const mcp = await getMcpServer();
    const created = await mcp.createParty({ filterOrJaml: jaml, batchChars: 3, blockSize: 1, totalBlocks: 5 });
    const rep = await mcp.reportParty({
      partyId: created.body.partyId,
      workerToken: 'expired-or-forged-token-000',
      startBlock: 0,
      seeds: [],
      heartbeatOnly: false,
    });
    assert.ok(rep.status >= 400 || rep.body.ok === false, 'Forged/expired token must be rejected');
  });

  it('F05-B2: Empty workerToken in /api/party/report request is rejected', async () => {
    const mcp = await getMcpServer();
    const created = await mcp.createParty({ filterOrJaml: jaml, batchChars: 3, blockSize: 1, totalBlocks: 5 });
    const rep = await mcp.reportParty({
      partyId: created.body.partyId,
      workerToken: '',
      startBlock: 0,
      seeds: [],
    });
    assert.ok(rep.status >= 400 || rep.body.ok === false, 'Empty workerToken must be rejected');
  });

  it('F05-B3: Missing FilterOrJaml in /api/party/create returns HTTP 400 BadRequest', async () => {
    const mcp = await getMcpServer();
    const res = await mcp.createParty({ filterOrJaml: '   ' });
    assert.equal(res.status, 400);
  });

  it('F05-B4: Negative or mismatched startBlock in /api/party/report is rejected', async () => {
    const mcp = await getMcpServer();
    const created = await mcp.createParty({ filterOrJaml: jaml, batchChars: 3, blockSize: 2, totalBlocks: 10 });
    const { lease } = (await mcp.nextPartyLease(created.body.partyId, 2)).body;
    const rep = await mcp.reportParty({
      partyId: created.body.partyId,
      workerToken: lease.workerToken,
      startBlock: -1,
      seeds: [],
    });
    assert.ok(rep.status >= 400 || rep.body.ok === false, 'Negative/mismatched startBlock must be rejected');
  });

  it('F05-B5: Reporting 0 seeds found for a completed block succeeds and advances completedBlocks', async () => {
    const mcp = await getMcpServer();
    const created = await mcp.createParty({ filterOrJaml: jaml, batchChars: 3, blockSize: 3, totalBlocks: 12 });
    const { lease } = (await mcp.nextPartyLease(created.body.partyId, 3)).body;
    const rep = await mcp.reportParty({
      partyId: created.body.partyId,
      workerToken: lease.workerToken,
      startBlock: lease.startBlock,
      seeds: [],
      heartbeatOnly: false,
    });
    assert.equal(rep.status, 200);
    assert.equal(rep.body.ok, true);
    const status = await mcp.getPartyStatus(created.body.partyId);
    assert.equal(status.body.completedBlocks, 3);
  });

  it('F05-B6: Reporting > 1,000 candidate seeds in a single payload is rejected by partyStore MAX_CANDIDATE_SEEDS cap', async () => {
    const store = await getSeedfinderPartyStore();
    const created = store.createParty({ jaml, batchChars: 4, leaseBlocks: 1, maxBlocks: 10 });
    const { lease } = store.leaseNext(created.partyId);
    const flood = Array.from({ length: 1005 }, () => '11111111');
    const rep = await store.reportFinds({
      partyId: created.partyId,
      workerToken: lease.workerToken,
      startBlock: lease.startBlock,
      seeds: flood,
    });
    assert.equal(rep.ok, false, 'Payloads exceeding 1,000 candidate seeds must be rejected');
  });
});
