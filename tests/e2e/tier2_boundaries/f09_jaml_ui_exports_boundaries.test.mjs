// tests/e2e/tier2_boundaries/f09_jaml_ui_exports_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';

describe('Feature 9 Boundaries: jaml-ui Package Exports', () => {
  const pkgFile = path.join(PATHS.jamlUi, 'package.json');
  const pkg = JSON.parse(fs.readFileSync(pkgFile, 'utf-8'));
  const exportsMap = pkg.exports || {};

  it('F09-B1: Unexported internal paths (e.g. ./src/internal) not exposed in exports', () => {
    assert.equal('./src' in exportsMap, false);
    assert.equal('./internal' in exportsMap, false);
  });

  it('F09-B2: sideEffects array explicitly specifies CSS stylesheets to prevent tree-shaking purge', () => {
    assert.ok(Array.isArray(pkg.sideEffects));
    assert.ok(pkg.sideEffects.includes('**/*.css'));
  });

  it('F09-B3: All exported type files end with .d.ts', () => {
    for (const [key, val] of Object.entries(exportsMap)) {
      if (typeof val === 'object' && val.types) {
        assert.ok(val.types.endsWith('.d.ts'), `Export ${key} type ${val.types} must end with .d.ts`);
      }
    }
  });

  it('F09-B4: All exported implementation files end with .js or .css or match wildcard', () => {
    for (const [key, val] of Object.entries(exportsMap)) {
      if (typeof val === 'object' && val.default) {
        assert.ok(val.default.endsWith('.js'), `Export ${key} default ${val.default} must end with .js`);
      } else if (typeof val === 'string') {
        assert.ok(val.endsWith('.css') || val.endsWith('.json') || val.includes('*'));
      }
    }
  });

  it('F09-B5: package.json exports mapping includes ./package.json entry', () => {
    assert.equal(exportsMap['./package.json'], './package.json');
  });

  it('F09-B6: fonts.css stylesheet export exists in exports map', () => {
    assert.ok(exportsMap['./fonts.css']);
  });
});
