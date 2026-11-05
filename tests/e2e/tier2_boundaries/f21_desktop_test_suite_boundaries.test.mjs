// tests/e2e/tier2_boundaries/f21_desktop_test_suite_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';
import {
  getDesktopTestSuiteSummary,
  runDesktopTestsFilter,
} from '../helpers/runtime_oracle.mjs';

describe('Feature 21 Boundaries: Desktop Test Suite Verification', () => {
  it('F21-B1: BalatroSeedOracle.Tests uses shared HeadlessAvaloniaFixture collection without leaking test hosts', () => {
    const testsDir = path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle.Tests');
    const csproj = fs.readFileSync(path.join(testsDir, 'BalatroSeedOracle.Tests.csproj'), 'utf-8');
    assert.ok(csproj.includes('Avalonia.Headless'));
    const fixtureFiles = fs.readdirSync(testsDir, { recursive: true }).filter(f => String(f).includes('Headless'));
    assert.ok(fixtureFiles.length >= 1, 'Expected HeadlessAvaloniaFixture in BalatroSeedOracle.Tests');
  });

  it('F21-B2: MusicServiceTests execute cleanly in headless environment without hardware audio device crashes', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~MusicServiceTests');
    assert.equal(res.exitCode, 0, `Headless MusicServiceTests failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('F21-B3: Centrally pinned package versions in Directory.Packages.props prohibit floating wildcard versions', () => {
    const propsPath = path.join(PATHS.BalatroSeedOracle, 'Directory.Packages.props');
    const content = fs.readFileSync(propsPath, 'utf-8');
    assert.ok(!content.includes('Version="*"'), 'Floating package versions are prohibited');
  });

  it('F21-B4: All 157 shipped YamlFilters validate with 0 errors in ShippedYamlFilters_AllValidateCleanly test', () => {
    const res = runDesktopTestsFilter('FullyQualifiedName~ShippedYamlFilters_AllValidateCleanly');
    assert.equal(res.exitCode, 0, `Shipped YamlFilters validation test failed:\n${res.stdout}\n${res.stderr}`);
  });

  it('F21-B5: BalatroSeedOracle.slnx includes both BalatroSeedOracle.Tests and Motely.Tests projects', () => {
    const slnx = fs.readFileSync(path.join(PATHS.BalatroSeedOracle, 'BalatroSeedOracle.slnx'), 'utf-8');
    assert.ok(slnx.includes('BalatroSeedOracle.Tests.csproj'));
    assert.ok(slnx.includes('Motely.Tests.csproj'));
  });

  it('F21-B6: Actual Release desktop test run reports 0 skipped tests and 0 failed tests', () => {
    const summary = getDesktopTestSuiteSummary();
    assert.equal(summary.exitCode, 0);
    assert.equal(summary.failed, 0, 'Zero failed tests permitted in Release suite');
    assert.equal(summary.skipped, 0, 'Zero skipped tests permitted in Release suite');
  });
});
