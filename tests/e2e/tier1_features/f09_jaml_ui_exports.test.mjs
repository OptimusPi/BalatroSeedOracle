// tests/e2e/tier1_features/f09_jaml_ui_exports.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';

describe('Feature 9: jaml-ui Package Exports (package.json)', () => {
  const pkgPath = path.join(PATHS.jamlUi, 'package.json');
  const pkg = JSON.parse(fs.readFileSync(pkgPath, 'utf-8'));
  const exportsMap = pkg.exports || {};

  it('F09-1: Root "." export maps types and default entries', () => {
    assert.ok(exportsMap['.'], 'Root "." export must exist in package.json');
    assert.equal(exportsMap['.'].types, './dist/index.d.ts');
    assert.equal(exportsMap['.'].default, './dist/index.js');
  });

  it('F09-2: Subpath "./ui" export exists and maps to dist/ui', () => {
    assert.ok(exportsMap['./ui'], 'Subpath "./ui" export must exist');
    assert.equal(exportsMap['./ui'].types, './dist/ui.d.ts');
    assert.equal(exportsMap['./ui'].default, './dist/ui.js');
  });

  it('F09-3: Subpath "./sprites" export exists and maps to dist/sprites', () => {
    assert.ok(exportsMap['./sprites'], 'Subpath "./sprites" export must exist');
    assert.equal(exportsMap['./sprites'].types, './dist/sprites.d.ts');
    assert.equal(exportsMap['./sprites'].default, './dist/sprites.js');
  });

  it('F09-4: Subpath "./assets" export exists and maps to dist/assets', () => {
    assert.ok(exportsMap['./assets'], 'Subpath "./assets" export must exist');
    assert.equal(exportsMap['./assets'].types, './dist/assets.d.ts');
    assert.equal(exportsMap['./assets'].default, './dist/assets.js');
  });

  it('F09-5: Subpath "./motely" export exists and maps to dist/motely', () => {
    assert.ok(exportsMap['./motely'], 'Subpath "./motely" export must exist');
    assert.equal(exportsMap['./motely'].types, './dist/motely.d.ts');
    assert.equal(exportsMap['./motely'].default, './dist/motely.js');
  });

  it('F09-6: Subpaths "./motd" and "./jimbo.css" exist in exports map', () => {
    assert.ok(exportsMap['./motd'], 'Subpath "./motd" export must exist');
    assert.ok(exportsMap['./jimbo.css'], 'Subpath "./jimbo.css" export must exist');
    assert.equal(exportsMap['./jimbo.css'], './dist/ui/jimbo.css');
  });
});
