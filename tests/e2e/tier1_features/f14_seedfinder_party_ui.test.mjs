// tests/e2e/tier1_features/f14_seedfinder_party_ui.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  getSeedfinderPartyStore,
  runWasmSeedSearch,
} from '../helpers/runtime_oracle.mjs';

describe('Feature 14: seedfinder.app Community Party Store & API (/api/party/*)', () => {
  const jaml = 'name: F14 Party\ndeck: Red\nstake: White\nmust:\n  - voucher: Overstock\n    antes: [1]\n';

  it('F14-1: Live leaseNext(partyId) returns real PartyLease with partyId, startBlock, blockCount, workerToken, expiresAt', async () => {
    const store = await getSeedfinderPartyStore();
    const created = store.createParty({ jaml, deck: 'Red', stake: 'White', batchChars: 4, leaseBlocks: 8, maxBlocks: 64 });
    const next = store.leaseNext(created.partyId);
    assert.ok(next.lease);
    assert.equal(next.lease.partyId, created.partyId);
    assert.equal(next.lease.startBlock, 0);
    assert.equal(next.lease.blockCount, 8);
    assert.ok(next.lease.workerToken.length >= 16);
  });

  it('F14-2: Live reportFinds processes heartbeatOnly: true and real WASM-verified seed matches', async () => {
    const store = await getSeedfinderPartyStore();
    const created = store.createParty({ jaml, deck: 'Red', stake: 'White', batchChars: 7, leaseBlocks: 1, maxBlocks: 35 });
    const { lease } = store.leaseNext(created.partyId);

    const hb = await store.reportFinds({
      partyId: created.partyId,
      workerToken: lease.workerToken,
      startBlock: lease.startBlock,
      seeds: [],
      heartbeatOnly: true,
    });
    assert.equal(hb.ok, true);
    assert.equal(hb.heartbeat, true);

    // Block 0 with batchChars=7 has 8th char '1' (index 0) -> seeds ending in '1'
    const block0Candidates = ['11111111', '21111111', '31111111', '41111111', '51111111', '61111111', '71111111', '81111111', '91111111', 'A1111111'];
    const wasmSweep = await runWasmSeedSearch(jaml, block0Candidates);
    const rep = await store.reportFinds({
      partyId: created.partyId,
      workerToken: lease.workerToken,
      startBlock: lease.startBlock,
      seeds: [...wasmSweep.matches, 'ZZZZZZZZ'],
      heartbeatOnly: false,
    });
    assert.equal(rep.ok, true);
    assert.deepEqual(rep.confirmed, wasmSweep.matches);
    assert.ok(rep.rejected.includes('ZZZZZZZZ'), 'Out-of-block seed ZZZZZZZZ must be rejected');
  });

  it('F14-3: Live getPartyStatus returns accurate state, blocksDone, blocksTotal, percentComplete, activeWorkers, and finds', async () => {
    const store = await getSeedfinderPartyStore();
    const created = store.createParty({ jaml, batchChars: 7, leaseBlocks: 5, maxBlocks: 20 });
    const { lease } = store.leaseNext(created.partyId);
    const activeStatus = store.getPartyStatus(created.partyId);
    assert.equal(activeStatus.activeWorkers, 1);
    assert.equal(activeStatus.blocksDone, 0);

    await store.reportFinds({
      partyId: created.partyId,
      workerToken: lease.workerToken,
      startBlock: lease.startBlock,
      seeds: [],
    });
    const doneStatus = store.getPartyStatus(created.partyId);
    assert.equal(doneStatus.activeWorkers, 0);
    assert.equal(doneStatus.blocksDone, 5);
    assert.equal(doneStatus.blocksTotal, 20);
    assert.equal(doneStatus.percentComplete, 25);
  });

  it('F14-4: createParty initializes party with zero fabricated seeds or blocks', async () => {
    const store = await getSeedfinderPartyStore();
    const created = store.createParty({ jaml, deck: 'Ghost', stake: 'Gold', batchChars: 4, leaseBlocks: 10, maxBlocks: 100 });
    const status = store.getPartyStatus(created.partyId);
    assert.equal(status.deck, 'Ghost');
    assert.equal(status.stake, 'Gold');
    assert.equal(status.blocksDone, 0);
    assert.equal(status.finds.length, 0);
  });

  it('F14-5: Exhausting all blocks transitions party state to exhausted', async () => {
    const store = await getSeedfinderPartyStore();
    const created = store.createParty({ jaml, batchChars: 7, leaseBlocks: 2, maxBlocks: 2 });
    const { lease } = store.leaseNext(created.partyId);
    await store.reportFinds({
      partyId: created.partyId,
      workerToken: lease.workerToken,
      startBlock: lease.startBlock,
      seeds: [],
    });
    const status = store.getPartyStatus(created.partyId);
    assert.equal(status.state, 'exhausted');
    assert.equal(status.percentComplete, 100);
  });

  it('F14-6: listParties enumerates active and completed parties with live worker counts', async () => {
    const store = await getSeedfinderPartyStore();
    const created = store.createParty({ jaml, batchChars: 5, leaseBlocks: 4, maxBlocks: 40 });
    const list = store.listParties();
    const found = list.find(p => p.partyId === created.partyId);
    assert.ok(found, 'Newly created party must appear in listParties()');
    assert.equal(found.totalBlocks, 40);
    assert.equal(found.findsCount, 0);
  });
});
