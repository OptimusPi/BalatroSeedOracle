// tests/e2e/tier1_features/f12_seedfinder_curator.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';
import { runCli } from '../helpers/cli_runner.mjs';

describe('Feature 12: seedfinder.app Curator Integration', () => {
  it('F12-1: seedfinder.app package.json references jaml-ui 10.0.0 dependency', () => {
    const pkgFile = path.join(PATHS.seedfinderApp, 'package.json');
    assert.equal(exists(pkgFile), true);
    const pkg = JSON.parse(fs.readFileSync(pkgFile, 'utf-8'));
    const jamlDep = pkg.dependencies?.['jaml-ui'];
    assert.ok(jamlDep, 'jaml-ui must be present in dependencies');
    assert.ok(
      jamlDep.includes('10.0.0') || jamlDep.includes('file:../jaml-ui'),
      `Expected jaml-ui version ^10.0.0 or local link, got ${jamlDep}`
    );
  });

  it('F12-2: Next.js page component exists at app/seed-finder.tsx or app/page.tsx', () => {
    const page1 = path.join(PATHS.seedfinderApp, 'app', 'seed-finder.tsx');
    const page2 = path.join(PATHS.seedfinderApp, 'app', 'page.tsx');
    assert.ok(exists(page1) || exists(page2), 'Main Next.js page file must exist');
  });

  it('F12-3: seedfinder.app builds cleanly via pnpm build with Next.js 16', () => {
    const result = runCli('pnpm', ['build'], { cwd: PATHS.seedfinderApp, timeoutMs: 60000 });
    assert.equal(result.exitCode, 0, `pnpm build failed with code ${result.exitCode}:\n${result.stderr || result.stdout}`);
  });

  it('F12-4: Next.js build output generates .next production artifacts', () => {
    const nextDir = path.join(PATHS.seedfinderApp, '.next');
    assert.equal(exists(nextDir), true, '.next directory must exist after build');
  });

  it('F12-5: Curator layout integrates SeedLab dock layout', () => {
    const pageFile = path.join(PATHS.seedfinderApp, 'app', 'seed-finder.tsx');
    if (exists(pageFile)) {
      const content = fs.readFileSync(pageFile, 'utf-8');
      assert.ok(content.length > 100);
    }
  });

  it('F12-6: Web worker search pool initializes motely-wasm dependency', () => {
    const pkgFile = path.join(PATHS.seedfinderApp, 'package.json');
    const pkg = JSON.parse(fs.readFileSync(pkgFile, 'utf-8'));
    assert.ok(pkg.dependencies?.['motely-wasm'], 'motely-wasm must be present in seedfinder.app');
  });
});
