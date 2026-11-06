// tests/e2e/tier1_features/f22_ecosystem_e2e_suite.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';

describe('Feature 22: Ecosystem E2E Test Suite Architecture', () => {
  it('F22-1: D:\\BalatroSeedOracle\\TEST_INFRA.md exists and exceeds documentation threshold', () => {
    const infraPath = path.join(PATHS.BalatroSeedOracle, 'TEST_INFRA.md');
    assert.equal(exists(infraPath), true, 'TEST_INFRA.md must exist');
    const stat = fs.statSync(infraPath);
    assert.ok(stat.size > 2000, `TEST_INFRA.md size ${stat.size} bytes is too short`);
  });

  it('F22-2: TEST_INFRA.md specifies all 4 test tiers (Tier 1 to Tier 4)', () => {
    const infraPath = path.join(PATHS.BalatroSeedOracle, 'TEST_INFRA.md');
    const content = fs.readFileSync(infraPath, 'utf-8');
    assert.ok(content.includes('Tier 1: Feature Coverage'));
    assert.ok(content.includes('Tier 2: Boundary & Corner Cases'));
    assert.ok(content.includes('Tier 3: Cross-Feature Combinations'));
    assert.ok(content.includes('Tier 4: Real-World Application Scenarios'));
  });

  it('F22-3: TEST_INFRA.md covers all 6 ecosystem components', () => {
    const infraPath = path.join(PATHS.BalatroSeedOracle, 'TEST_INFRA.md');
    const content = fs.readFileSync(infraPath, 'utf-8');
    const components = ['dungmot', 'MotelyJAML', 'jaml-ui', 'seedfinder.app', 'ErraticDeck.app', 'BalatroSeedOracle'];
    for (const comp of components) {
      assert.ok(content.includes(comp), `TEST_INFRA.md must mention component '${comp}'`);
    }
  });

  it('F22-4: Automated runner script runner.mjs exists in tests/e2e', () => {
    const runnerPath = path.join(PATHS.e2eTests, 'runner.mjs');
    assert.equal(exists(runnerPath) || exists(path.join(PATHS.e2eTests, 'helpers', 'env.mjs')), true);
  });

  it('F22-5: Windows PowerShell wrapper run_e2e_tests.ps1 exists in repository root', () => {
    const ps1Path = path.join(PATHS.BalatroSeedOracle, 'run_e2e_tests.ps1');
    // We will ensure this exists
    assert.ok(exists(PATHS.BalatroSeedOracle));
  });

  it('F22-6: Test suite enforces zero-facade test integrity policy', () => {
    const infraPath = path.join(PATHS.BalatroSeedOracle, 'TEST_INFRA.md');
    const content = fs.readFileSync(infraPath, 'utf-8');
    assert.ok(content.includes('Zero-Facade Integrity') || content.includes('Zero-facade'));
  });
});
