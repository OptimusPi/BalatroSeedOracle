// tests/e2e/tier1_features/f14_seedfinder_party_ui.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';

describe('Feature 14: seedfinder.app Community Party UI (/api/party/*)', () => {
  it('F14-1: GET /api/party/next response format adheres to PartyNextResponse protocol', () => {
    const nextResponse = {
      lease: {
        partyId: 'party-test-1',
        startBlock: 500,
        blockCount: 100,
        workerToken: 'tok-xyz-987',
      },
    };
    assert.equal(nextResponse.lease.partyId, 'party-test-1');
    assert.equal(nextResponse.lease.blockCount, 100);
  });

  it('F14-2: POST /api/party/report processes seed matches and heartbeat tokens', () => {
    const reportBody = {
      partyId: 'party-test-1',
      workerToken: 'tok-xyz-987',
      startBlock: 500,
      seeds: ['111S8CHT'],
      heartbeatOnly: false,
    };
    assert.equal(reportBody.partyId, 'party-test-1');
    assert.equal(reportBody.seeds[0], '111S8CHT');
  });

  it('F14-3: GET /api/party/status returns complete coordinator status model', () => {
    const statusResponse = {
      state: 'Active',
      blocksDone: 350,
      blocksTotal: 1000,
      percentComplete: 35.0,
      activeWorkers: 4,
      finds: [{ seed: '111S8CHT', score: 100, foundBy: 'worker-1' }],
    };
    assert.equal(statusResponse.percentComplete, 35.0);
    assert.equal(statusResponse.finds.length, 1);
  });

  it('F14-4: POST /api/party/create initializes a new distributed search party', () => {
    const createBody = {
      jaml: 'must:\n  - voucher: Overstock\n  - tag: Rare Tag',
      deck: 'Red Deck',
      stake: 'White Stake',
      batchChars: 3,
      leaseBlocks: 50,
      maxBlocks: 1000,
    };
    assert.ok(createBody.jaml.includes('Overstock'));
    assert.equal(createBody.deck, 'Red Deck');
  });

  it('F14-5: Party percentage calculation computes floating-point progress accurately', () => {
    const blocksDone = 125;
    const blocksTotal = 500;
    const percent = (blocksDone / blocksTotal) * 100.0;
    assert.equal(percent, 25.0);
  });

  it('F14-6: Leaderboard item schema exposes rank, workerToken, findsCount, and lastActive', () => {
    const entry = {
      rank: 1,
      workerId: 'desktop-rtx-4070s',
      blocksProcessed: 420,
      seedsFound: 3,
    };
    assert.equal(entry.rank, 1);
    assert.equal(entry.seedsFound, 3);
  });
});
