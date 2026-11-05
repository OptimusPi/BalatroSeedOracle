// tests/e2e/tier2_boundaries/f14_seedfinder_party_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { getSeedfinderPartyStore } from '../helpers/runtime_oracle.mjs';

describe('Feature 14 Boundaries: seedfinder.app Community Party Store', () => {
  const jaml = 'name: F14B\ndeck: Red\nstake: White\nmust:\n  - voucher: Overstock\n    antes: [1]\n';

  it('F14-B1: leaseNext on unknown partyId returns { done: true, reason: "Party not found" }', async () => {
    const store = await getSeedfinderPartyStore();
    const res = store.leaseNext('party_does_not_exist_999');
    assert.equal(res.done, true);
    assert.ok(res.reason && res.reason.toLowerCase().includes('not found'));
  });

  it('F14-B2: Completed/exhausted party (100% blocks done) returns { done: true } to subsequent workers', async () => {
    const store = await getSeedfinderPartyStore();
    const created = store.createParty({ jaml, batchChars: 7, leaseBlocks: 2, maxBlocks: 2 });
    const { lease } = store.leaseNext(created.partyId);
    await store.reportFinds({
      partyId: created.partyId,
      workerToken: lease.workerToken,
      startBlock: lease.startBlock,
      seeds: [],
    });

    const afterDone = store.leaseNext(created.partyId);
    assert.equal(afterDone.done, true);
    assert.equal(afterDone.lease, undefined);
  });

  it('F14-B3: Worker reporting seeds with stale/invalid workerToken is rejected cleanly', async () => {
    const store = await getSeedfinderPartyStore();
    const created = store.createParty({ jaml, batchChars: 7, leaseBlocks: 2, maxBlocks: 10 });
    store.leaseNext(created.partyId);
    const rep = await store.reportFinds({
      partyId: created.partyId,
      workerToken: 'stale_token_xyz',
      startBlock: 0,
      seeds: ['11111111'],
    });
    assert.equal(rep.ok, false);
    assert.ok(rep.error && rep.error.toLowerCase().includes('invalid'));
  });

  it('F14-B4: Re-submitting an already-completed lease token is rejected on replay', async () => {
    const store = await getSeedfinderPartyStore();
    const created = store.createParty({ jaml, batchChars: 7, leaseBlocks: 1, maxBlocks: 10 });
    const { lease } = store.leaseNext(created.partyId);
    const first = await store.reportFinds({
      partyId: created.partyId,
      workerToken: lease.workerToken,
      startBlock: lease.startBlock,
      seeds: [],
    });
    assert.equal(first.ok, true);

    const replay = await store.reportFinds({
      partyId: created.partyId,
      workerToken: lease.workerToken,
      startBlock: lease.startBlock,
      seeds: [],
    });
    assert.equal(replay.ok, false);
  });

  it('F14-B5: Concurrent leaseNext calls from multiple workers allocate strictly disjoint block ranges', async () => {
    const store = await getSeedfinderPartyStore();
    const created = store.createParty({ jaml, batchChars: 6, leaseBlocks: 50, maxBlocks: 500 });
    const l1 = store.leaseNext(created.partyId).lease;
    const l2 = store.leaseNext(created.partyId).lease;
    const l3 = store.leaseNext(created.partyId).lease;
    assert.equal(l1.startBlock, 0);
    assert.equal(l2.startBlock, 50);
    assert.equal(l3.startBlock, 100);
    assert.ok(l1.startBlock + l1.blockCount <= l2.startBlock);
    assert.ok(l2.startBlock + l2.blockCount <= l3.startBlock);
  });

  it('F14-B6: Fully leased party with active workers returns { done: true, reason: "fully leased" } until blocks finish or expire', async () => {
    const store = await getSeedfinderPartyStore();
    const created = store.createParty({ jaml, batchChars: 7, leaseBlocks: 4, maxBlocks: 4 });
    const first = store.leaseNext(created.partyId);
    assert.ok(first.lease);
    const second = store.leaseNext(created.partyId);
    assert.equal(second.done, true);
    assert.equal(second.reason, 'fully leased');
  });
});
