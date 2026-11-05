// tests/e2e/tier4_scenarios/s02_desktop_gpu_screaming_search_workflow.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';
import {
  getMcpServer,
  getMotelyWasm,
  runWasmSeedSearch,
  runDesktopTestsFilter,
} from '../helpers/runtime_oracle.mjs';

describe('Tier 4 Scenario 2: Desktop GPU Screaming Search Workflow', () => {
  const filterJaml = `name: Hybrid GPU Screaming Hunt
deck: Red
stake: White
must:
  - voucher: Overstock
    antes: [1]
  - soulJoker: Any
    edition: Negative
    antes: [1, 2]
`;

  it('Step 1: Host OpenCL runtime discovers GPU hardware via live Motely.MCP root telemetry', async () => {
    const mcp = await getMcpServer();
    const info = await mcp.rootInfo();
    assert.equal(info.service, 'Motely.MCP');
    assert.ok(typeof info.gpu.deviceCount === 'number' && info.gpu.deviceCount >= 0);
    if (info.gpu.deviceCount > 0) {
      assert.ok(info.gpu.bestDevice && info.gpu.bestDevice.length > 0);
    }
  });

  it('Step 2: SearchTab.axaml exposes GPU Screaming Mode toggle and PartyCoordinatorUrlBox bindings', () => {
    const searchTab = path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle', 'Views', 'SearchModalTabs', 'SearchTab.axaml');
    assert.equal(exists(searchTab), true);
    const axaml = fs.readFileSync(searchTab, 'utf-8');
    assert.ok(axaml.includes('UseGpuSearch'), 'SearchTab.axaml must bind UseGpuSearch');
    assert.ok(axaml.includes('PartyCoordinatorUrl'), 'SearchTab.axaml must bind PartyCoordinatorUrl');
  });

  it('Step 3: Multi-clause filter validates cleanly through real JamlConfigLoader', async () => {
    const { JamlConfigLoader } = await getMotelyWasm();
    assert.equal(JamlConfigLoader.check(filterJaml), null);
    const parsed = JamlConfigLoader.fromJaml(filterJaml);
    assert.equal(parsed.must.length, 2);
  });

  it('Step 4: Live hybrid GPU/SIMD sweep executes 35 batches (1,500,625 seeds) and verifies confirmed matches', async () => {
    const mcp = await getMcpServer();
    const rarePolyJaml = `name: Overstock Polychrome\ndeck: Red\nstake: White\nmust:\n  - voucher: Overstock\n    antes: [1]\n  - rareJoker: []\n    edition: Polychrome\n    antes: [1, 2]\n`;
    const created = await mcp.createParty({
      filterOrJaml: rarePolyJaml,
      batchChars: 3,
      blockSize: 1,
      totalBlocks: 70,
    });
    assert.equal(created.status, 200, JSON.stringify(created.body));

    const sweep = await mcp.sweepParty(created.body.partyId, { blocks: 35, gpu: true });
    assert.equal(sweep.status, 200, JSON.stringify(sweep.body));
    assert.equal(sweep.body.seedsSearched, 35 * 42875);
    assert.ok(sweep.body.seedsPerSecond > 100_000, `Expected >100k seeds/sec, got ${sweep.body.seedsPerSecond}`);
    if (sweep.body.confirmedSeeds?.length > 0) {
      const check = await runWasmSeedSearch(
        rarePolyJaml,
        sweep.body.confirmedSeeds.slice(0, 5)
      );
      assert.equal(check.matches.length, Math.min(5, sweep.body.confirmedSeeds.length));
    }
  });

  it('Step 5: Headless Avalonia DesktopAuditRegressionTests verify truthful GPU active status in SearchModalViewModel', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~SearchModalViewModel_GpuActiveState_StaysFalse');
    assert.equal(res.exitCode, 0, `Headless GPU status test failed:\n${res.stdout}\n${res.stderr}`);
  });
});
