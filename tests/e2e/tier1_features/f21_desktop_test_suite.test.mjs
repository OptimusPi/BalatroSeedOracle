// tests/e2e/tier1_features/f21_desktop_test_suite.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';
import { runCli } from '../helpers/cli_runner.mjs';
import {
  getDesktopTestSuiteSummary,
  runDesktopTestsFilter,
} from '../helpers/runtime_oracle.mjs';

describe('Feature 21: Desktop Test Suite Verification (BalatroSeedOracle.slnx)', () => {
  const slnxPath = path.join(PATHS.BalatroSeedOracle, 'BalatroSeedOracle.slnx');

  it('F21-1: BalatroSeedOracle.slnx builds cleanly in Release with zero compiler errors and includes Motely.Tests', () => {
    const slnxContent = fs.readFileSync(slnxPath, 'utf-8');
    assert.ok(slnxContent.includes('Motely.Tests.csproj'), 'BalatroSeedOracle.slnx must include Motely.Tests.csproj');

    let result = runCli('dotnet', ['build', slnxPath, '-c', 'Release', '-p:UseSharedCompilation=false', '--nologo'], {
      cwd: PATHS.BalatroSeedOracle,
      timeoutMs: 90000,
    });
    if (result.exitCode !== 0 && `${result.stdout}\n${result.stderr}`.includes('used by another process')) {
      result = runCli('dotnet', ['build', slnxPath, '-c', 'Release', '-p:UseSharedCompilation=false', '--nologo'], {
        cwd: PATHS.BalatroSeedOracle,
        timeoutMs: 90000,
      });
    }
    assert.equal(result.exitCode, 0, `BalatroSeedOracle.slnx Release build failed:\n${result.stderr || result.stdout}`);
  });

  it('F21-2: BalatroSeedOracle.Tests executes PersistenceTests with 0 failures', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~PersistenceTests');
    assert.equal(res.exitCode, 0, `PersistenceTests failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('F21-3: BalatroSeedOracle.Tests executes JamlEditorTests with 0 failures', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~JamlEditorTests');
    assert.equal(res.exitCode, 0, `JamlEditorTests failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('F21-4: BalatroSeedOracle.Tests executes MusicServiceTests with 0 failures', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~MusicServiceTests');
    assert.equal(res.exitCode, 0, `MusicServiceTests failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('F21-5: Directory.Packages.props centrally pins Avalonia 12.0.3 and DuckDB.NET.Data.Full', () => {
    const propsFile = path.join(PATHS.BalatroSeedOracle, 'Directory.Packages.props');
    const content = fs.readFileSync(propsFile, 'utf-8');
    assert.ok(content.includes('12.0.3'), 'Must pin Avalonia 12.0.3');
    assert.ok(content.includes('DuckDB.NET.Data.Full'), 'Must pin DuckDB.NET');
  });

  it('F21-6: Full BalatroSeedOracle.Tests execution reports 0 failed, 0 skipped, and >= 2,900 passed tests', () => {
    const summary = getDesktopTestSuiteSummary();
    assert.equal(summary.exitCode, 0, `Full desktop test suite exited ${summary.exitCode}:\n${summary.output.slice(-1500)}`);
    assert.equal(summary.failed, 0, `Expected 0 failed tests, got ${summary.failed}`);
    assert.equal(summary.skipped, 0, `Expected 0 skipped tests, got ${summary.skipped}`);
    assert.ok(summary.passed >= 2900, `Expected >= 2,900 passing desktop tests, got ${summary.passed}`);
  });
});
