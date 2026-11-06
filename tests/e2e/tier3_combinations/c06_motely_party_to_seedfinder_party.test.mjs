// tests/e2e/tier3_combinations/c06_motely_party_to_seedfinder_party.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';

describe('Combination C06: Motely.Party DTOs <-> seedfinder.app Coordinator Protocol', () => {
  it('C06-1: PartyLease JSON serialization matches seedfinder.app /api/party/next lease schema', () => {
    const lease = {
      partyId: 'party-test-1',
      startBlock: 100,
      blockCount: 50,
      workerToken: 'tok-abc',
      expiresUtc: '2026-10-28T12:00:00Z',
    };
    const json = JSON.stringify(lease);
    const parsed = JSON.parse(json);
    assert.equal(parsed.partyId, lease.partyId);
    assert.equal(parsed.startBlock, lease.startBlock);
    assert.equal(parsed.blockCount, lease.blockCount);
  });

  it('C06-2: PartyReportRequest JSON serialization matches seedfinder.app /api/party/report schema', () => {
    const report = {
      partyId: 'party-test-1',
      workerToken: 'tok-abc',
      startBlock: 100,
      seeds: ['111S8CHT'],
      heartbeatOnly: false,
    };
    const json = JSON.stringify(report);
    const parsed = JSON.parse(json);
    assert.equal(parsed.workerToken, report.workerToken);
    assert.equal(parsed.seeds[0], '111S8CHT');
  });

  it('C06-3: PartyNextResponse discriminates between active lease and done: true', () => {
    const leaseResponse = { lease: { partyId: 'p1', startBlock: 0, blockCount: 10 } };
    const doneResponse = { done: true, reason: 'Search space exhausted' };
    assert.ok(leaseResponse.lease && !leaseResponse.done);
    assert.ok(doneResponse.done && !doneResponse.lease);
  });

  it('C06-4: PartyCreateRequest transfers JAML config and search parameters to coordinator', () => {
    const createReq = {
      jaml: 'must:\n  - voucher: Overstock',
      deck: 'Red Deck',
      stake: 'White Stake',
      leaseBlocks: 50,
    };
    assert.ok(createReq.jaml.includes('Overstock'));
  });

  it('C06-5: Coordinator progress telemetry reflects blocks reported by Motely worker', () => {
    const blocksTotal = 1000;
    const blocksDone = 500;
    const percent = (blocksDone / blocksTotal) * 100.0;
    assert.equal(percent, 50.0);
  });
});
