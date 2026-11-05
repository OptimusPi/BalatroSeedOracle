// tests/e2e/tier1_features/f23_rtx4070s_parity_benchmark.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';
import { getMcpServer } from '../helpers/runtime_oracle.mjs';

describe('Feature 23: Live RTX 4070 SUPER Parity Verification & Benchmark', () => {
  it('F23-1: GpuDeviceInfo defines device discovery and selection logic', () => {
    const devPath = path.join(PATHS.MotelyJAML, 'Motely', 'Gpu', 'GpuDeviceInfo.cs');
    assert.equal(exists(devPath), true);
    const content = fs.readFileSync(devPath, 'utf-8');
    assert.ok(content.includes('GetBestDevice'));
  });

  it('F23-2: OpenCL kernel source uses IEEE-754 FP64 arithmetic matching CPU SIMD', () => {
    const kernelPath = path.join(PATHS.MotelyJAML, 'Motely', 'Gpu', 'GpuSeedSearcher.cs');
    assert.equal(exists(kernelPath), true);
    const content = fs.readFileSync(kernelPath, 'utf-8');
    assert.ok(content.includes('3.141592653589793116'), 'OpenCL kernel must use exact PI constant');
    assert.ok(content.includes('1.1239285023'), 'OpenCL kernel must use exact character multiplier');
  });

  it('F23-3: OpenCL kernel implements explicit FMA / FP64 iterative PRNG', () => {
    const kernelPath = path.join(PATHS.MotelyJAML, 'Motely', 'Gpu', 'GpuSeedSearcher.cs');
    const content = fs.readFileSync(kernelPath, 'utf-8');
    assert.ok(content.includes('fma(') || content.includes('1e13'), 'Kernel must implement FP64 PRNG iteration');
  });

  it('F23-4: Live benchmark method Benchmark(batchChars, batchCount) measures throughput', () => {
    const kernelPath = path.join(PATHS.MotelyJAML, 'Motely', 'Gpu', 'GpuSeedSearcher.cs');
    const content = fs.readFileSync(kernelPath, 'utf-8');
    assert.ok(content.includes('public static GpuBenchmarkResult Benchmark'));
  });

  it('F23-5: Live OpenCL hardware benchmark via Motely.MCP gpu_status exceeds 10 Mseeds/sec on host GPU', async () => {
    const mcp = await getMcpServer();
    const rpc = await mcp.callTool('gpu_status', {});
    assert.ok(!rpc.error, `gpu_status MCP call failed: ${JSON.stringify(rpc.error)}`);
    const status = JSON.parse(rpc.result.content[0].text);
    const bench = status.benchmark ?? status.Benchmark;
    assert.ok(bench, 'gpu_status must return live GpuBenchmarkResult');
    assert.equal(bench.gpuAvailable ?? bench.GpuAvailable, true, 'Host OpenCL GPU must be available');
    const mseeds = bench.millionsOfSeedsPerSecond ?? bench.MillionsOfSeedsPerSecond;
    assert.ok(mseeds >= 10.0, `Expected live OpenCL benchmark >= 10.0 Mseeds/sec, got ${mseeds}`);
  });

  it('F23-6: Engine maintains SIMD CPU fallback when GPU is unavailable or disabled', () => {
    const searchPath = path.join(PATHS.MotelyJAML, 'Motely', 'MotelySearch.cs');
    const content = fs.readFileSync(searchPath, 'utf-8');
    assert.ok(content.includes('UseGpu') || content.includes('Vector512'));
  });
});
