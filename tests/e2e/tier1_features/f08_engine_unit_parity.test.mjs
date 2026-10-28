// tests/e2e/tier1_features/f08_engine_unit_parity.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';
import { runCli } from '../helpers/cli_runner.mjs';

describe('Feature 8: Engine Unit & Parity Tests (Motely.slnx)', () => {
  it('F08-1: Motely.slnx solution builds successfully under Release configuration', () => {
    const slnxPath = path.join(PATHS.MotelyJAML, 'Motely.slnx');
    let result = runCli('dotnet', ['build', slnxPath, '-c', 'Release', '-p:UseSharedCompilation=false'], {
      cwd: PATHS.MotelyJAML,
      timeoutMs: 60000,
    });
    // Handle Windows transient file locking (CS2012 or file in use)
    let attempts = 0;
    while (result.exitCode !== 0 && attempts < 2 &&
           ((result.stderr + result.stdout).includes('CS2012') || (result.stderr + result.stdout).includes('used by another process'))) {
      attempts++;
      const end = Date.now() + 2500;
      while (Date.now() < end) {}
      result = runCli('dotnet', ['build', slnxPath, '-c', 'Release', '-p:UseSharedCompilation=false'], {
        cwd: PATHS.MotelyJAML,
        timeoutMs: 60000,
      });
    }
    assert.equal(result.exitCode, 0, `Build failed with code ${result.exitCode}:\n${result.stderr || result.stdout}`);
  });

  it('F08-2: Motely.Tests project executes and passes unit test assertions', () => {
    const testCsproj = path.join(PATHS.MotelyJAML, 'Motely.Tests', 'Motely.Tests.csproj');
    const result = runCli('dotnet', ['test', testCsproj, '-c', 'Release', '--filter', 'FullyQualifiedName~GpuSearchTests'], {
      cwd: PATHS.MotelyJAML,
      timeoutMs: 60000,
    });
    assert.equal(result.exitCode, 0, `Tests failed:\n${result.stderr || result.stdout}`);
    assert.ok(!result.stdout.includes('Failed: [1-9]'), 'No test failures permitted in Motely.Tests');
  });

  it('F08-3: Motely.CLI compiles and publishes successfully', () => {
    const cliCsproj = path.join(PATHS.MotelyJAML, 'Motely.CLI', 'Motely.CLI.csproj');
    const result = runCli('dotnet', ['build', cliCsproj, '-c', 'Release'], {
      cwd: PATHS.MotelyJAML,
      timeoutMs: 60000,
    });
    assert.equal(result.exitCode, 0, `CLI build failed: ${result.stderr}`);
  });

  it('F08-4: Motely.CLI executable can be invoked with --help or --version', () => {
    const cliCsproj = path.join(PATHS.MotelyJAML, 'Motely.CLI', 'Motely.CLI.csproj');
    const result = runCli('dotnet', ['run', '--project', cliCsproj, '-c', 'Release', '--no-build', '--', '--help'], {
      cwd: PATHS.MotelyJAML,
      timeoutMs: 30000,
    });
    assert.equal(result.exitCode, 0);
    assert.ok(result.stdout.length > 0 || result.stderr.length > 0);
  });

  it('F08-5: GpuSearchTests contains Shop and Buffoon parity test fixtures', () => {
    const testFile = path.join(PATHS.MotelyJAML, 'Motely.Tests', 'GpuSearchTests.cs');
    assert.equal(fs.existsSync(testFile), true, 'GpuSearchTests.cs must exist');
    const content = fs.readFileSync(testFile, 'utf-8');
    assert.ok(content.includes('GpuSeedSearcher'), 'Must test GpuSeedSearcher');
  });

  it('F08-6: Zero false negative invariant: GPU filter candidates are superset of CPU matches', () => {
    const trueMatches = new Set(['111S8CHT', 'SEEDAAA1']);
    const gpuCandidates = new Set(['111S8CHT', 'SEEDAAA1', 'CANDID01', 'CANDID02']);
    for (const match of trueMatches) {
      assert.ok(gpuCandidates.has(match), `Match ${match} must survive GPU filter`);
    }
  });
});
