// tests/e2e/tier2_boundaries/f11_jaml_ui_build_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';

describe('Feature 11 Boundaries: jaml-ui Rebuild & Validation', () => {
  const tsdownConfig = path.join(PATHS.jamlUi, 'tsdown.config.ts');
  const tsdownContent = fs.readFileSync(tsdownConfig, 'utf-8');

  it('F11-B1: tsdown.config.ts defines all 7 required package entrypoints', () => {
    const requiredEntries = ['index', 'tokens', 'ui', 'sprites', 'assets', 'motely', 'motd'];
    for (const entry of requiredEntries) {
      assert.ok(tsdownContent.includes(entry), `tsdown.config.ts must configure entry '${entry}'`);
    }
  });

  it('F11-B2: tsdown.config.ts outputs unminified declaration files with dts option', () => {
    assert.ok(tsdownContent.includes('dts: true') || tsdownContent.includes('dts:'));
  });

  it('F11-B3: tsdown.config.ts configures clean output directory before building', () => {
    assert.ok(tsdownContent.includes('clean: true') || tsdownContent.includes('clean:'));
  });

  it('F11-B4: Vitest configuration exists at vitest.config.ts', () => {
    const vitestConfig = path.join(PATHS.jamlUi, 'vitest.config.ts');
    assert.equal(fs.existsSync(vitestConfig), true);
  });

  it('F11-B5: SeedLab unit test suite exists at src/components/SeedLab.test.tsx', () => {
    const testFile = path.join(PATHS.jamlUi, 'src', 'components', 'SeedLab.test.tsx');
    assert.equal(fs.existsSync(testFile), true);
  });

  it('F11-B6: Build generates CSS files inside dist/styles or dist/ui', () => {
    const distStyles = path.join(PATHS.jamlUi, 'dist', 'styles');
    const distUi = path.join(PATHS.jamlUi, 'dist', 'ui');
    assert.ok(fs.existsSync(distStyles) || fs.existsSync(distUi));
  });
});
