// tests/e2e/tier4_scenarios/s04_community_party_distributed_sweep_workflow.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  getMotelyWasm,
  getSeedfinderPartyStore,
} from '../helpers/runtime_oracle.mjs';

describe('Tier 4 Scenario 4: Distributed Community Party Search Workflow', () => {
  const partyJaml = `name: Community Overstock Hunt
deck: Red
stake: White
must:
  - voucher: Overstock
    antes: [1]
`;

  let partyId = '';
  let leased = null;
  let discoveredSeed = '';

  it('Step 1: Coordinator creates a real Community Party in seedfinder.app partyStore', async () => {
    const { createParty, getPartyStatus } = await getSeedfinderPartyStore();
    const created = createParty({
      jaml: partyJaml,
      deck: 'Red',
      stake: 'White',
      batchChars: 3,
      leaseBlocks: 2,
      maxBlocks: 20,
    });
    assert.ok(created.partyId.startsWith('party_'));
    assert.equal(created.totalBlocks, 20);
    partyId = created.partyId;

    const initial = getPartyStatus(partyId);
    assert.equal(initial.blocksDone, 0);
    assert.equal(initial.activeWorkers, 0);
    assert.equal(initial.finds.length, 0);
  });

  it('Step 2: Worker calls leaseNext(partyId) and acquires real lease for blocks [0, 2)', async () => {
    const { leaseNext, getPartyStatus } = await getSeedfinderPartyStore();
    const next = leaseNext(partyId);
    assert.ok(next.lease, 'Expected active lease');
    assert.equal(next.lease.startBlock, 0);
    assert.equal(next.lease.blockCount, 2);
    leased = next.lease;

    const status = getPartyStatus(partyId);
    assert.equal(status.activeWorkers, 1);
  });

  it('Step 3: Worker sweeps leased block 0 using real motely-wasm engine and finds matching seeds', async () => {
    const { JamlConfigLoader, JamlSearchBuilder } = await getMotelyWasm();
    const cfg = JamlConfigLoader.fromJaml(leased.jaml);
    const hits = [];
    const search = JamlSearchBuilder.createSettings(cfg)
      .withThreadCount(1)
      .withQuietMode(true)
      .withSequentialSearch()
      .withBatchCharacterCount(leased.batchChars)
      .withStartBatchIndex(BigInt(leased.startBlock))
      .withEndBatchIndex(BigInt(leased.startBlock + 1))
      .withSeedMatchCallback(s => hits.push(s))
      .start();
    try {
      await search.waitForCompletionAsync();
    } finally {
      search.dispose();
    }
    assert.ok(hits.length > 0, 'Block 0 must yield Overstock Ante 1 seeds');
    discoveredSeed = hits[0];
    assert.match(discoveredSeed, /^[1-9A-Z]{8}$/);
  });

  it('Step 4: Worker reports real match + forged out-of-range seed; coordinator confirms match and rejects forgery', async () => {
    const { reportFinds } = await getSeedfinderPartyStore();
    const res = await reportFinds({
      partyId,
      workerToken: leased.workerToken,
      startBlock: leased.startBlock,
      seeds: [discoveredSeed, 'ZZZZZZZZ'],
      heartbeatOnly: false,
    });
    assert.equal(res.ok, true);
    assert.deepEqual(res.confirmed, [discoveredSeed]);
    assert.ok(res.rejected.includes('ZZZZZZZZ'));
    assert.equal(res.recorded, 1);
  });

  it('Step 5: Party status updates blocksDone=2, percentComplete=10%, activeWorkers=0, finds=[discoveredSeed]', async () => {
    const { getPartyStatus } = await getSeedfinderPartyStore();
    const status = getPartyStatus(partyId);
    assert.equal(status.blocksDone, 2);
    assert.equal(status.blocksTotal, 20);
    assert.equal(status.percentComplete, 10);
    assert.equal(status.activeWorkers, 0);
    assert.deepEqual(status.finds, [discoveredSeed]);
  });
});
