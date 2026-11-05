// tests/e2e/tier3_combinations/c09_gpu_screaming_to_desktop_viewmodel.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';
import {
  getMcpServer,
  runDesktopTestsFilter,
  runWasmSeedSearch,
} from '../helpers/runtime_oracle.mjs';

describe('Combination C09: MotelyJAML GpuSeedSearcher <-> BalatroSeedOracle Screaming Mode', () => {
  const vmPath = path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle', 'ViewModels', 'SearchModalViewModel.cs');

  it('C09-1: SearchModalViewModel connects WithGpuSearch and GpuFilterProgram.Compile(LoadedConfig).HasSelectiveOps', () => {
    assert.equal(exists(vmPath), true);
    const content = fs.readFileSync(vmPath, 'utf-8');
    assert.ok(content.includes('CreateSettings'));
    assert.ok(content.includes('WithGpuSearch'));
    assert.ok(content.includes('GpuFilterProgram.Compile(LoadedConfig).HasSelectiveOps'));
  });

  it('C09-2: Desktop app passes LoadedConfig to search builder via WithSourceConfig', () => {
    const content = fs.readFileSync(vmPath, 'utf-8');
    assert.ok(content.includes('WithSourceConfig(LoadedConfig)'));
    assert.ok(content.includes('_isGpuExecutingCurrentSearch'));
  });

  it('C09-3: Live Motely.MCP GPU + CPU hybrid sweep executes on real selective JAML filter and matches WASM CPU oracle', async () => {
    const mcp = await getMcpServer();
    const jaml = [
      'name: C09 Screaming Sweep',
      'deck: Red',
      'stake: White',
      'must:',
      '  - voucher: Overstock',
      '    antes: [1]',
      'should:',
      '  - soulJoker: Perkeo',
      '    antes: [1, 2]',
      '    score: 50',
    ].join('\n');

    const created = await mcp.createParty({ filterOrJaml: jaml, name: 'C09-Screaming', batchChars: 3, blockSize: 1 });
    assert.equal(created.status, 200, JSON.stringify(created.body));
    const sweep = await mcp.sweepParty(created.body.partyId, { blocks: 2, gpu: true, threads: 2 });
    assert.equal(sweep.status, 200, JSON.stringify(sweep.body));
    assert.equal(sweep.body.seedsSearched, 2 * 42875);

    if (sweep.body.confirmedSeeds.length > 0) {
      const sample = sweep.body.confirmedSeeds.slice(0, 5);
      const wasmRes = await runWasmSeedSearch(jaml, sample);
      assert.equal(wasmRes.matches.length, sample.length, 'Every GPU screaming match must pass CPU/WASM full filter');
    }
  });

  it('C09-4: GpuDeviceService active state reflects truthful GPU execution via DesktopAuditRegressionTests', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~SearchModalViewModel_GpuActiveState_StaysFalse');
    assert.equal(res.exitCode, 0, `Desktop GPU active-state regression test failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('C09-5: Headless SearchModalViewModel WordList search populates console & UI state cleanly', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~SearchModalViewModel_WordListMode_PopulatesAesthetics');
    assert.equal(res.exitCode, 0, `Desktop SearchModalViewModel live search test failed:\n${res.stdout}\n${res.stderr}`);
  });
});
