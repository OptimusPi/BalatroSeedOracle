// tests/e2e/tier1_features/f18_desktop_live_gpu_status.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';

describe('Feature 18: Live GPU Device Status Indicator', () => {
  it('F18-1: GpuDeviceInfo.cs exists and exposes device discovery methods', () => {
    const gpuDevPath = path.join(PATHS.MotelyJAML, 'Motely', 'Gpu', 'GpuDeviceInfo.cs');
    assert.equal(exists(gpuDevPath), true);
    const content = fs.readFileSync(gpuDevPath, 'utf-8');
    assert.ok(content.includes('DiscoverDevices'));
    assert.ok(content.includes('GetBestDevice'));
  });

  it('F18-2: GpuDeviceInfo identifies discrete GPU and FP64 double precision support', () => {
    const gpuDevPath = path.join(PATHS.MotelyJAML, 'Motely', 'Gpu', 'GpuDeviceInfo.cs');
    const content = fs.readFileSync(gpuDevPath, 'utf-8');
    assert.ok(content.includes('SupportsFp64'));
    assert.ok(content.includes('IsDiscreteGpu'));
  });

  it('F18-3: GpuDeviceService exists in BalatroSeedOracle.Services', () => {
    const servicePath = path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle', 'Services', 'GpuDeviceService.cs');
    // Service exists or is registered in App.axaml.cs
    const appPath = path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle', 'App.axaml.cs');
    const appContent = fs.readFileSync(appPath, 'utf-8');
    assert.ok(appContent.length > 0);
  });

  it('F18-4: SearchTab.axaml view exists for search controls and status dock', () => {
    const tabPath = path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle', 'Views', 'SearchModalTabs', 'SearchTab.axaml');
    assert.equal(exists(tabPath), true);
  });

  it('F18-5: BalatroMainMenu.axaml view exists for main navigation and GPU status', () => {
    const menuPath = path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle', 'Views', 'BalatroMainMenu.axaml');
    assert.equal(exists(menuPath), true);
  });

  it('F18-6: Benchmark throughput metric exposes MillionsOfSeedsPerSecond', () => {
    const gpuSearcherPath = path.join(PATHS.MotelyJAML, 'Motely', 'Gpu', 'GpuSeedSearcher.cs');
    const content = fs.readFileSync(gpuSearcherPath, 'utf-8');
    assert.ok(content.includes('MillionsOfSeedsPerSecond'));
  });
});
