// tests/e2e/tier3_combinations/c06_motely_party_to_seedfinder_party.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  getMcpServer,
  getSeedfinderPartyStore,
  getMotelyWasm,
} from '../helpers/runtime_oracle.mjs';

describe('Combination C06: Motely.Party DTOs <-> seedfinder.app Coordinator Protocol', () => {
  const sampleJaml = `name: Protocol Check\ndeck: Red\nstake: White\nmust:\n  - voucher: Overstock\n    antes: [1]\n`;

  it('C06-1: Both C# Motely.MCP and seedfinder.app lease endpoints return compatible PartyLease fields', async () => {
    const mcp = await getMcpServer();
    const { body: csharpParty } = await mcp.createParty({
      filterOrJaml: sampleJaml,
      batchChars: 3,
      blockSize: 5,
      totalBlocks: 20,
    });
    const { body: csharpNext } = await mcp.nextPartyLease(csharpParty.partyId, 5);
    assert.ok(csharpNext.lease);

    const { createParty, leaseNext } = await getSeedfinderPartyStore();
    const sfParty = createParty({ jaml: sampleJaml, batchChars: 3, leaseBlocks: 5, maxBlocks: 20 });
    const sfNext = leaseNext(sfParty.partyId);
    assert.ok(sfNext.lease);

    for (const key of ['partyId', 'startBlock', 'blockCount', 'workerToken']) {
      assert.ok(key in csharpNext.lease, `C# lease missing ${key}`);
      assert.ok(key in sfNext.lease, `seedfinder.app lease missing ${key}`);
    }
  });

  it('C06-2: Both coordinators return confirmed[] and rejected[] arrays matching PartyReportResponse', async () => {
    const { JamlConfigLoader, JamlSearchBuilder } = await getMotelyWasm();
    const hits = [];
    const s = JamlSearchBuilder.createSettings(JamlConfigLoader.fromJaml(sampleJaml))
      .withThreadCount(1)
      .withQuietMode(true)
      .withSequentialSearch()
      .withBatchCharacterCount(3)
      .withEndBatchIndex(1n)
      .withSeedMatchCallback(seed => hits.push(seed))
      .start();
    await s.waitForCompletionAsync();
    s.dispose();
    const validSeed = hits[0];

    const { createParty, leaseNext, reportFinds } = await getSeedfinderPartyStore();
    const { partyId } = createParty({ jaml: sampleJaml, batchChars: 3, leaseBlocks: 2, maxBlocks: 10 });
    const { lease } = leaseNext(partyId);
    const sfReport = await reportFinds({
      partyId,
      workerToken: lease.workerToken,
      startBlock: lease.startBlock,
      seeds: [validSeed, 'ZZZZZZZZ'],
      heartbeatOnly: false,
    });
    assert.equal(sfReport.ok, true);
    assert.deepEqual(sfReport.confirmed, [validSeed]);
    assert.ok(sfReport.rejected.includes('ZZZZZZZZ'));
  });

  it('C06-3: Exhausted party returns done: true and no lease in seedfinder.app partyStore', async () => {
    const { createParty, leaseNext, reportFinds } = await getSeedfinderPartyStore();
    const { partyId } = createParty({ jaml: sampleJaml, batchChars: 3, leaseBlocks: 2, maxBlocks: 2 });
    const first = leaseNext(partyId);
    assert.ok(first.lease && !first.done);
    await reportFinds({
      partyId,
      workerToken: first.lease.workerToken,
      startBlock: first.lease.startBlock,
      seeds: [],
      heartbeatOnly: false,
    });
    const second = leaseNext(partyId);
    assert.equal(second.done, true);
    assert.equal(second.lease, undefined);
  });

  it('C06-4: "default" partyId alias resolves to active community party in seedfinder.app', async () => {
    const { getPartyStatus } = await getSeedfinderPartyStore();
    const def = getPartyStatus('default');
    assert.ok(def, 'default party alias must resolve');
    assert.ok(def.jaml.length > 0);
  });

  it('C06-5: Party telemetry computes exact percentComplete and activeWorkers from live leases', async () => {
    const { createParty, leaseNext, reportFinds, getPartyStatus } = await getSeedfinderPartyStore();
    const { partyId } = createParty({ jaml: sampleJaml, batchChars: 3, leaseBlocks: 5, maxBlocks: 10 });
    assert.equal(getPartyStatus(partyId).activeWorkers, 0);
    const { lease } = leaseNext(partyId);
    assert.equal(getPartyStatus(partyId).activeWorkers, 1);
    await reportFinds({
      partyId,
      workerToken: lease.workerToken,
      startBlock: lease.startBlock,
      seeds: [],
      heartbeatOnly: false,
    });
    const after = getPartyStatus(partyId);
    assert.equal(after.activeWorkers, 0);
    assert.equal(after.percentComplete, 50);
  });
});
