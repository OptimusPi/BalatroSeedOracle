// tests/e2e/tier3_combinations/c13_deck_stake_picker_to_motely_config.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import {
  getMotelyWasm,
  analyzeSeedWithWasm,
  runMotelyCli,
} from '../helpers/runtime_oracle.mjs';

describe('Combination C13: jaml-ui DeckStakePicker <-> Motely Engine Deck & Stake Simulation', () => {
  it('C13-1: All 15 standard Balatro decks exist in MotelyDeck WASM enum and parse in JamlConfigLoader', async () => {
    const { MotelyDeck, JamlConfigLoader } = await getMotelyWasm();
    const standardDecks = [
      'Red', 'Blue', 'Yellow', 'Green', 'Black',
      'Magic', 'Nebula', 'Ghost', 'Abandoned', 'Checkered',
      'Zodiac', 'Painted', 'Anaglyph', 'Plasma', 'Erratic',
    ];
    assert.equal(standardDecks.length, 15);
    for (const deck of standardDecks) {
      assert.notEqual(MotelyDeck[deck], undefined, `Missing MotelyDeck.${deck}`);
      const cfg = JamlConfigLoader.fromJaml(`deck: ${deck}\nstake: White\n`);
      assert.equal(cfg.deck, MotelyDeck[deck]);
    }
  });

  it('C13-2: All 8 Balatro stakes (including Black Stake) exist in MotelyStake WASM enum and parse in JamlConfigLoader', async () => {
    const { MotelyStake, JamlConfigLoader } = await getMotelyWasm();
    const allStakes = [
      'White', 'Red', 'Green', 'Black',
      'Blue', 'Purple', 'Orange', 'Gold',
    ];
    assert.equal(allStakes.length, 8);
    for (const stake of allStakes) {
      assert.notEqual(MotelyStake[stake], undefined, `Missing MotelyStake.${stake}`);
      const cfg = JamlConfigLoader.fromJaml(`deck: Red\nstake: ${stake}\n`);
      assert.equal(cfg.stake, MotelyStake[stake]);
    }
  });

  it('C13-3: Built Motely.CLI accepts --deck Erratic --stake Gold on real JAML search', () => {
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'bso-c13-'));
    const jamlPath = path.join(tmpDir, 'c13.jaml');
    try {
      fs.writeFileSync(
        jamlPath,
        'name: C13 DeckStake\nmust:\n  - erraticRank: Ten\n    min: 10\n',
        'utf-8'
      );
      const res = runMotelyCli(['--jaml', jamlPath, '--deck', 'Erratic', '--stake', 'Gold', '--seeds', '1UQ8D22L']);
      assert.equal(res.exitCode, 0, `Motely.CLI --deck/--stake run failed:\n${res.stdout}\n${res.stderr}`);
      assert.ok(`${res.stdout}\n${res.stderr}`.includes('1UQ8D22L'));
    } finally {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    }
  });

  it('C13-4: Gold Stake simulation activates Eternal/Perishable/Rental joker stickers that White Stake omits', async () => {
    const seeds = ['ALEEB', '111S8CHT', '1UQ8D22L', '113L2V3N'];
    let whiteStickers = 0;
    let goldStickers = 0;

    for (const seed of seeds) {
      const white = await analyzeSeedWithWasm(seed, { deck: 'Red', stake: 'White' });
      const gold = await analyzeSeedWithWasm(seed, { deck: 'Red', stake: 'Gold' });
      for (const ante of white.antes) {
        for (const s of ante.shopItems ?? []) {
          if (s.isEternal || s.isPerishable || s.isRental) whiteStickers++;
        }
      }
      for (const ante of gold.antes) {
        for (const s of ante.shopItems ?? []) {
          if (s.isEternal || s.isPerishable || s.isRental) goldStickers++;
        }
      }
    }

    assert.equal(whiteStickers, 0, 'White Stake must never roll Eternal/Perishable/Rental stickers');
    assert.ok(goldStickers > 0, 'Gold Stake must roll Eternal/Perishable/Rental stickers in shop items');
  });

  it('C13-5: Erratic Deck populates 52 randomized starting cards while Red Deck leaves erraticDeck null/empty', async () => {
    const red = await analyzeSeedWithWasm('1UQ8D22L', { deck: 'Red', stake: 'White' });
    const erratic = await analyzeSeedWithWasm('1UQ8D22L', { deck: 'Erratic', stake: 'White' });
    assert.equal(red.erraticDeck == null || red.erraticDeck.length === 0, true);
    assert.ok(Array.isArray(erratic.erraticDeck));
    assert.equal(erratic.erraticDeck.length, 52);
  });
});
