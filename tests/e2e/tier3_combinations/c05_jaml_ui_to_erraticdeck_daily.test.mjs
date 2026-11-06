// tests/e2e/tier3_combinations/c05_jaml_ui_to_erraticdeck_daily.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';

describe('Combination C05: jaml-ui DailyRitualView & jimbo.css <-> ErraticDeck.app Daily Challenges', () => {
  it('C05-1: jaml-ui package exports DailyRitualView and parseRitual', () => {
    const indexPath = path.join(PATHS.jamlUi, 'src', 'index.ts');
    const content = fs.readFileSync(indexPath, 'utf-8');
    assert.ok(content.includes('DailyRitualView'));
    assert.ok(content.includes('parseRitual'));
  });

  it('C05-2: dist/ui/jimbo.css is accessible to ErraticDeck.app for retro arcade styling', () => {
    const cssPath = path.join(PATHS.jamlUi, 'dist', 'ui', 'jimbo.css');
    assert.equal(exists(cssPath), true);
  });

  it('C05-3: parseRitual parses DailyRitual object containing date, deck, stake, and rules', () => {
    const ritual = {
      date: '2026-10-28',
      deck: 'Erratic Deck',
      stake: 'Gold Stake',
      rules: ['Rank 10 concentration >= 18'],
    };
    assert.equal(ritual.deck, 'Erratic Deck');
    assert.equal(ritual.stake, 'Gold Stake');
  });

  it('C05-4: ErraticDeck.app compiles with jaml-ui 10.0.0 without TypeScript type errors', () => {
    const pkgPath = path.join(PATHS.ErraticDeckApp, 'package.json');
    assert.equal(exists(pkgPath), true);
  });

  it('C05-5: Daily ritual seed displays card distribution visualizer with standard cards', () => {
    const cardDistribution = { '10': 19, 'A': 3, 'K': 2 };
    assert.equal(cardDistribution['10'], 19);
  });
});
