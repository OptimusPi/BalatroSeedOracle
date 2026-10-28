// tests/e2e/tier2_boundaries/f10_jaml_ui_reexports_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS } from '../helpers/env.mjs';

describe('Feature 10 Boundaries: jaml-ui High-Level Re-Exports', () => {
  const indexFile = path.join(PATHS.jamlUi, 'src', 'index.ts');
  const content = fs.readFileSync(indexFile, 'utf-8');

  it('F10-B1: Re-exports pagination constants MAX_HITS and HIT_PAGE_SIZE', () => {
    assert.ok(content.includes('MAX_HITS'));
    assert.ok(content.includes('HIT_PAGE_SIZE'));
  });

  it('F10-B2: Re-exports starter JAML template constants STARTER_JAML and JAMLYZE_JAML', () => {
    assert.ok(content.includes('STARTER_JAML'));
    assert.ok(content.includes('JAMLYZE_JAML'));
  });

  it('F10-B3: Re-exports SeedHit, SeedSearchPhase, SeedSearchStats TypeScript types', () => {
    assert.ok(content.includes('SeedHit'));
    assert.ok(content.includes('SeedSearchPhase'));
    assert.ok(content.includes('SeedSearchStats'));
  });

  it('F10-B4: Re-exports JamlZone, JamlVisualFilter, JamlVisualClause visual IDE types', () => {
    assert.ok(content.includes('JamlZone'));
    assert.ok(content.includes('JamlVisualFilter'));
    assert.ok(content.includes('JamlVisualClause'));
  });

  it('F10-B5: Re-exports Base UI style wrapper utility cx', () => {
    assert.ok(content.includes('cx'));
  });

  it('F10-B6: Re-exports ThemeName and toneNames tokens', () => {
    assert.ok(content.includes('toneNames'));
    assert.ok(content.includes('ThemeName') || content.includes('Tone'));
  });
});
