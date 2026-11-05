// tests/e2e/tier2_boundaries/f22_e2e_suite_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';
import { runCli } from '../helpers/cli_runner.mjs';

describe('Feature 22 Boundaries: Ecosystem E2E Test Suite', () => {
  const runnerPath = path.join(PATHS.e2eTests, 'runner.mjs');

  it('F22-B1: Invalid CLI flag (--non-existent-flag-99) causes runner.mjs to exit with code 1 and usage help', () => {
    const res = runCli('node', [runnerPath, '--non-existent-flag-99'], {
      cwd: PATHS.BalatroSeedOracle,
      timeoutMs: 10000,
    });
    assert.equal(res.exitCode, 1);
    const combined = `${res.stdout}\n${res.stderr}`;
    assert.ok(combined.includes('Usage:') || combined.includes('Unknown option'));
  });

  it('F22-B2: Out-of-range --tier 5 causes runner.mjs to exit with code 1 and descriptive error', () => {
    const res = runCli('node', [runnerPath, '--tier', '5'], {
      cwd: PATHS.BalatroSeedOracle,
      timeoutMs: 10000,
    });
    assert.equal(res.exitCode, 1);
    assert.ok(`${res.stdout}\n${res.stderr}`.includes('Invalid --tier'));
  });

  it('F22-B3: Unknown --component FakeProject causes runner.mjs to exit with code 1 and descriptive error', () => {
    const res = runCli('node', [runnerPath, '--component', 'FakeProject'], {
      cwd: PATHS.BalatroSeedOracle,
      timeoutMs: 10000,
    });
    assert.equal(res.exitCode, 1);
    assert.ok(`${res.stdout}\n${res.stderr}`.includes('Invalid --component'));
  });

  it('F22-B4: Out-of-range --feature 99 causes runner.mjs to exit with code 1', () => {
    const res = runCli('node', [runnerPath, '--feature', '99'], {
      cwd: PATHS.BalatroSeedOracle,
      timeoutMs: 10000,
    });
    assert.equal(res.exitCode, 1);
    assert.ok(`${res.stdout}\n${res.stderr}`.includes('Invalid --feature'));
  });

  it('F22-B5: runner.mjs --help exits with code 0 and prints CLI options', () => {
    const res = runCli('node', [runnerPath, '--help'], {
      cwd: PATHS.BalatroSeedOracle,
      timeoutMs: 10000,
    });
    assert.equal(res.exitCode, 0);
    assert.ok(res.stdout.includes('--tier'));
    assert.ok(res.stdout.includes('--component'));
  });

  it('F22-B6: Zero-facade audit: every .test.mjs file in tests/e2e imports real project or helper modules', () => {
    const dirs = ['tier1_features', 'tier2_boundaries', 'tier3_combinations', 'tier4_scenarios'];
    let checked = 0;
    for (const d of dirs) {
      const fullDir = path.join(PATHS.e2eTests, d);
      for (const f of fs.readdirSync(fullDir).filter(n => n.endsWith('.test.mjs'))) {
        const src = fs.readFileSync(path.join(fullDir, f), 'utf-8');
        const hasRealImport =
          src.includes('../helpers/') ||
          src.includes('node:fs') ||
          src.includes('node:child_process');
        assert.equal(hasRealImport, true, `Test file ${d}/${f} has zero helper/fs imports (facade suspect!)`);
        checked++;
      }
    }
    assert.equal(checked, 66, `Expected 66 E2E test files across 4 tiers, inspected ${checked}`);
  });
});
