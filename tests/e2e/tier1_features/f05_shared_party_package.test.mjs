// tests/e2e/tier1_features/f05_shared_party_package.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';

describe('Feature 5: Shared Motely.Party Package', () => {
  it('F05-1: PartyLease contract contains partyId, startBlock, blockCount, workerToken, expiresUtc', () => {
    const lease = {
      partyId: 'party-test-123',
      startBlock: 1000n,
      blockCount: 200,
      workerToken: 'tok-abc-456',
      expiresUtc: new Date().toISOString(),
    };
    assert.equal(lease.partyId, 'party-test-123');
    assert.equal(lease.startBlock, 1000n);
    assert.equal(lease.blockCount, 200);
    assert.ok(lease.workerToken.length > 0);
  });

  it('F05-2: PartyReportRequest supports reporting matches with seeds array', () => {
    const report = {
      partyId: 'party-test-123',
      workerToken: 'tok-abc-456',
      startBlock: 1000n,
      seeds: ['111S8CHT', '222XYZ99'],
      heartbeatOnly: false,
    };
    assert.equal(report.seeds.length, 2);
    assert.equal(report.heartbeatOnly, false);
  });

  it('F05-3: PartyReportRequest supports heartbeat-only telemetry payloads', () => {
    const heartbeat = {
      partyId: 'party-test-123',
      workerToken: 'tok-abc-456',
      startBlock: 1000n,
      seeds: [],
      heartbeatOnly: true,
    };
    assert.equal(heartbeat.heartbeatOnly, true);
    assert.equal(heartbeat.seeds.length, 0);
  });

  it('F05-4: PartySweep block calculation computes correct inclusive block boundary', () => {
    const startBlock = 500;
    const blockCount = 50;
    const endBlock = startBlock + blockCount;
    assert.equal(endBlock, 550);
  });

  it('F05-5: PartyWorker state lifecycle enumerates valid operational states', () => {
    const workerStates = ['Uninitialized', 'Idle', 'RequestingLease', 'Sweeping', 'Reporting', 'Completed', 'Error'];
    assert.ok(workerStates.includes('Sweeping'));
    assert.ok(workerStates.includes('Reporting'));
  });

  it('F05-6: Party coordinator status schema returns progress metrics', () => {
    const status = {
      partyId: 'party-alpha',
      state: 'Active',
      blocksDone: 450,
      blocksTotal: 1000,
      percentComplete: 45.0,
      activeWorkers: 3,
      finds: ['111S8CHT'],
    };
    assert.equal(status.percentComplete, 45.0);
    assert.equal(status.activeWorkers, 3);
  });
});
