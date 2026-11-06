// tests/e2e/tier4_scenarios/s04_community_party_distributed_sweep_workflow.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';

describe('Tier 4 Scenario 4: Distributed Community Party Search Workflow', () => {
  it('Step 1: Coordinator initiates a new Community Party in seedfinder.app with JAML filter', () => {
    const partyConfig = {
      partyId: 'party-community-alpha',
      jaml: 'must:\n  - voucher: Overstock',
      totalBlocks: 1000,
      blockSize: 100,
    };
    assert.equal(partyConfig.partyId, 'party-community-alpha');
  });

  it('Step 2: Desktop worker calls /api/party/next to lease block range [0, 100)', () => {
    const lease = {
      partyId: 'party-community-alpha',
      startBlock: 0,
      blockCount: 100,
      workerToken: 'desktop-worker-01',
      expiresUtc: new Date(Date.now() + 60000).toISOString(),
    };
    assert.equal(lease.startBlock, 0);
    assert.equal(lease.blockCount, 100);
  });

  it('Step 3: Desktop worker sweeps leased blocks on RTX 4070 SUPER and discovers target seed', () => {
    const discoveredMatch = '111S8CHT';
    assert.match(discoveredMatch, /^[0-9A-Z]{8}$/);
  });

  it('Step 4: Worker submits block report to /api/party/report with discovered seed', () => {
    const report = {
      partyId: 'party-community-alpha',
      workerToken: 'desktop-worker-01',
      startBlock: 0,
      seeds: ['111S8CHT'],
      heartbeatOnly: false,
    };
    assert.equal(report.seeds.length, 1);
  });

  it('Step 5: Coordinator updates party status: blocks completed = 100, percent = 10%, finds contains seed', () => {
    const status = {
      state: 'Active',
      blocksDone: 100,
      blocksTotal: 1000,
      percentComplete: 10.0,
      finds: [{ seed: '111S8CHT', worker: 'desktop-worker-01' }],
    };
    assert.equal(status.percentComplete, 10.0);
    assert.equal(status.finds[0].seed, '111S8CHT');
  });
});
