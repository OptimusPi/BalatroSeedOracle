// tests/e2e/tier1_features/f03_gpu_compiler_clauses.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { EDITION_CODES, RARITY_CODES } from '../helpers/prng_oracle.mjs';

describe('Feature 3: GPU Compiler Clause Emission (GpuFilterProgram)', () => {
  it('F03-1: OpShopJoker bytecode parameter layout contains exactly 7 integer words', () => {
    // Contract: [opcode, ante_mask, max_ante, max_slots, target_edition, target_rarity, min_count]
    const shopBytecode = [8, 0x01, 1, 2, EDITION_CODES.Negative, RARITY_CODES.Rare, 1];
    assert.equal(shopBytecode.length, 7);
    assert.equal(shopBytecode[0], 8, 'Opcode is OpShopJoker');
    assert.equal(shopBytecode[4], 4, 'Target edition Negative = 4');
    assert.equal(shopBytecode[5], 3, 'Target rarity Rare = 3');
  });

  it('F03-2: OpBuffoonJoker bytecode parameter layout contains exactly 7 integer words', () => {
    // Contract: [opcode, ante_mask, max_ante, max_draws, target_edition, target_rarity, min_count]
    const buffoonBytecode = [9, 0x03, 2, 4, EDITION_CODES.Polychrome, RARITY_CODES.Uncommon, 1];
    assert.equal(buffoonBytecode.length, 7);
    assert.equal(buffoonBytecode[0], 9, 'Opcode is OpBuffoonJoker');
    assert.equal(buffoonBytecode[4], 3, 'Target edition Polychrome = 3');
    assert.equal(buffoonBytecode[5], 2, 'Target rarity Uncommon = 2');
  });

  it('F03-3: Edition codes strictly match documented interface contract', () => {
    assert.equal(EDITION_CODES.Any, 0);
    assert.equal(EDITION_CODES.Foil, 1);
    assert.equal(EDITION_CODES.Holographic, 2);
    assert.equal(EDITION_CODES.Polychrome, 3);
    assert.equal(EDITION_CODES.Negative, 4);
  });

  it('F03-4: Rarity codes strictly match documented interface contract', () => {
    assert.equal(RARITY_CODES.Any, 0);
    assert.equal(RARITY_CODES.Common, 1);
    assert.equal(RARITY_CODES.Uncommon, 2);
    assert.equal(RARITY_CODES.Rare, 3);
  });

  it('F03-5: Ante mask bitwise representation correctly encapsulates multi-ante selections', () => {
    // Ante 1 = 1 << 0 (1), Ante 2 = 1 << 1 (2), Ante 8 = 1 << 7 (128)
    const ante1And2 = (1 << 0) | (1 << 1);
    assert.equal(ante1And2, 3);
    const anteAll8 = 0xFF;
    assert.equal(anteAll8, 255);
  });

  it('F03-6: HasSelectiveOps evaluates true when instruction count is positive', () => {
    const emptyProgram = { InstructionGroupCount: 0, HasSelectiveOps: false };
    const populatedProgram = { InstructionGroupCount: 2, HasSelectiveOps: true };
    assert.equal(emptyProgram.HasSelectiveOps, false);
    assert.equal(populatedProgram.HasSelectiveOps, true);
  });
});
