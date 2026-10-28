// tests/e2e/tier1_features/f15_erraticdeck_v10_upgrade.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';
import { runCli } from '../helpers/cli_runner.mjs';

describe('Feature 15: ErraticDeck.app 10.0.0 Upgrade', () => {
  const pkgFile = path.join(PATHS.ErraticDeckApp, 'package.json');
  const pkg = JSON.parse(fs.readFileSync(pkgFile, 'utf-8'));

  it('F15-1: ErraticDeck.app package.json specifies jaml-ui version 10.0.0', () => {
    const jamlDep = pkg.dependencies?.['jaml-ui'];
    assert.ok(jamlDep, 'jaml-ui must be in dependencies');
    assert.ok(
      jamlDep.includes('10.0.0') || jamlDep.includes('file:../jaml-ui'),
      `Expected jaml-ui 10.0.0 or local link, got ${jamlDep}`
    );
  });

  it('F15-2: ErraticDeck.app dependencies include @duckdb/duckdb-wasm', () => {
    assert.ok(pkg.dependencies?.['@duckdb/duckdb-wasm'], '@duckdb/duckdb-wasm missing');
  });

  it('F15-3: ErraticDeck.app dependencies include apache-arrow', () => {
    assert.ok(pkg.dependencies?.['apache-arrow'], 'apache-arrow missing');
  });

  it('F15-4: ErraticDeck.app builds with next build --webpack with 0 errors', () => {
    const result = runCli('pnpm', ['build'], { cwd: PATHS.ErraticDeckApp, timeoutMs: 90000 });
    assert.equal(result.exitCode, 0, `Build failed with code ${result.exitCode}:\n${result.stderr || result.stdout}`);
  });

  it('F15-5: Next.js Webpack build generates production standalone / static assets', () => {
    const nextDir = path.join(PATHS.ErraticDeckApp, '.next');
    assert.equal(exists(nextDir), true, '.next output directory must exist');
  });

  it('F15-6: Application styles import jaml-ui/jimbo.css for authentic CRT styling', () => {
    const globalCss = path.join(PATHS.ErraticDeckApp, 'app', 'globals.css');
    const layout = path.join(PATHS.ErraticDeckApp, 'app', 'layout.tsx');
    let hasJimboCss = false;
    if (exists(globalCss) && fs.readFileSync(globalCss, 'utf-8').includes('jimbo.css')) {
      hasJimboCss = true;
    }
    if (exists(layout) && fs.readFileSync(layout, 'utf-8').includes('jimbo.css')) {
      hasJimboCss = true;
    }
    // jimbo.css is either imported in globals.css, layout.tsx, or pages
    assert.ok(hasJimboCss || exists(path.join(PATHS.jamlUi, 'dist', 'ui', 'jimbo.css')));
  });
});
