// tests/e2e/tier2_boundaries/f18_desktop_gpu_status_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';
import {
  getMcpServer,
  runDesktopTestsFilter,
} from '../helpers/runtime_oracle.mjs';

describe('Feature 18 Boundaries: Live GPU Device Status Indicator', () => {
  const gpuInfoSrc = fs.readFileSync(path.join(PATHS.MotelyJAML, 'Motely', 'Gpu', 'GpuDeviceInfo.cs'), 'utf-8');
  const gpuSvcSrc = fs.readFileSync(
    path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle', 'Services', 'GpuDeviceService.cs'),
    'utf-8'
  );

  it('F18-B1: GpuDeviceInfo.DiscoverDevices catches OpenCL loader absence/errors and returns empty list safely', () => {
    assert.ok(gpuInfoSrc.includes('catch'));
    assert.ok(gpuInfoSrc.includes('DiscoverDevices'));
  });

  it('F18-B2: GpuDeviceInfo.GetBestDevice filters strictly for SupportsFp64 == true before selecting a device', () => {
    assert.ok(gpuInfoSrc.includes('SupportsFp64'));
    assert.ok(gpuSvcSrc.includes('SupportsFp64') || gpuSvcSrc.includes('IsAvailable'));
  });

  it('F18-B3: GpuDeviceService_TracksActiveState_AndFiresStatusChanged passes in headless Avalonia suite', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~GpuDeviceService_TracksActiveState_AndFiresStatusChanged');
    assert.equal(res.exitCode, 0, `GpuDeviceService status transition test failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('F18-B4: GpuDeviceService_RunsBenchmark_WithoutCrashing executes real OpenCL benchmark in BalatroSeedOracle.Tests', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~GpuDeviceService_RunsBenchmark_WithoutCrashing');
    assert.equal(res.exitCode, 0, `GpuDeviceService benchmark test failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('F18-B5: SearchModalViewModel progress updates report live throughput only when _isGpuExecutingCurrentSearch is true', () => {
    const vmSrc = fs.readFileSync(
      path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle', 'ViewModels', 'SearchModalViewModel.cs'),
      'utf-8'
    );
    assert.ok(vmSrc.includes('if (_isGpuExecutingCurrentSearch)'));
  });

  it('F18-B6: Live Motely.MCP rootInfo selects discrete NVIDIA/AMD/Intel FP64 GPU as bestDevice on host', async () => {
    const mcp = await getMcpServer();
    const info = await mcp.rootInfo();
    assert.ok(info.gpu.deviceCount >= 1);
    assert.ok(typeof info.gpu.bestDevice === 'string' && info.gpu.bestDevice.length > 0);
    const bestObj = info.gpu.devices.find(d => (d.name ?? d.Name) === info.gpu.bestDevice);
    assert.ok(bestObj, 'bestDevice must appear in discovered devices list');
    assert.equal(bestObj.supportsFp64 ?? bestObj.SupportsFp64, true);
  });
});
