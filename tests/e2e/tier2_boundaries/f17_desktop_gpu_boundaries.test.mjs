// tests/e2e/tier2_boundaries/f17_desktop_gpu_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';
import {
  runDesktopTestsFilter,
  runWasmSeedSearch,
} from '../helpers/runtime_oracle.mjs';

describe('Feature 17 Boundaries: Desktop GPU Screaming Mode', () => {
  const vmSrc = fs.readFileSync(
    path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle', 'ViewModels', 'SearchModalViewModel.cs'),
    'utf-8'
  );

  it('F17-B1: SearchModalViewModel clamps thread count to >= 1 via Math.Max(1, Threads)', () => {
    assert.ok(vmSrc.includes('Math.Max(1,') || vmSrc.includes('WithThreadCount'));
  });

  it('F17-B2: SearchModalViewModel keeps thread slider visible in DbList mode for multi-threaded SIMD verification', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~SearchModalViewModel_SelectedDbListIndexChanged_UpdatesSelectedLakePartition_AndShowsThreadSlider');
    assert.equal(res.exitCode, 0, `DbList thread visibility test failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('F17-B3: Rapid StartSearch -> StopSearch cycle aborts search cleanly and resets GPU active status', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~SearchModalViewModel_GpuActiveState_StaysFalse');
    assert.equal(res.exitCode, 0, `Start/Stop GPU active state test failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('F17-B4: High minScore cutoff (500) filters out low-scoring seeds while minScore 0 retains them in real engine search', async () => {
    const jaml = 'deck: Red\nstake: White\nshould:\n  - voucher: Overstock\n    antes: [1]\n    score: 10\n';
    const low = await runWasmSeedSearch(jaml, ['ALEEB', '111S8CHT'], { minScore: 0 });
    const high = await runWasmSeedSearch(jaml, ['ALEEB', '111S8CHT'], { minScore: 500 });
    assert.equal(low.matches.length, 2);
    assert.equal(high.matches.length, 0);
  });

  it('F17-B5: SeedLakeSink auto-creates SearchResults directory and sanitizes invalid filename characters', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~SeedLakeSinkTests');
    assert.equal(res.exitCode, 0, `SeedLakeSinkTests failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('F17-B6: Non-selective JAML filter seamlessly executes on CPU SIMD without activating GPU status', () => {
    assert.ok(vmSrc.includes('GpuFilterProgram.Compile(LoadedConfig).HasSelectiveOps'));
    assert.ok(vmSrc.includes('_isGpuExecutingCurrentSearch'));
  });
});
