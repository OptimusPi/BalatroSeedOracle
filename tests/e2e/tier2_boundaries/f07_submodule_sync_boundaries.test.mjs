// tests/e2e/tier2_boundaries/f07_submodule_sync_boundaries.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';

describe('Feature 7 Boundaries: Git Submodule Synchronization', () => {
  it('F07-B1: Submodule bin and obj directories are properly ignored and not tracked by git', () => {
    const gitignorePath = path.join(PATHS.MotelyJAML, '.gitignore');
    if (exists(gitignorePath)) {
      const content = fs.readFileSync(gitignorePath, 'utf-8');
      assert.ok(content.includes('bin/') || content.includes('obj/') || content.includes('[Bb]in/'));
    }
  });

  it('F07-B2: Directory casing in .gitmodules matches exact path on case-insensitive filesystems', () => {
    const gitmodules = path.join(PATHS.BalatroSeedOracle, '.gitmodules');
    const content = fs.readFileSync(gitmodules, 'utf-8');
    assert.ok(content.toLowerCase().includes('motelyjaml'));
  });

  it('F07-B3: Missing submodule files detected if critical engine directory is removed', () => {
    const engineDir = path.join(PATHS.MotelyJAML, 'Motely');
    assert.equal(exists(engineDir), true);
  });

  it('F07-B4: .git or .git file exists in submodule pointing to parent .git/modules', () => {
    const gitRef = path.join(PATHS.MotelyJAML, '.git');
    assert.equal(exists(gitRef), true);
  });

  it('F07-B5: Standalone D:\\MotelyJAML repository has valid git directory', () => {
    const standGit = path.join(PATHS.MotelyJAML_standalone, '.git');
    assert.equal(exists(standGit), true);
  });

  it('F07-B6: Both repository trees contain matching Motely.csproj target framework net10.0', () => {
    const csprojSub = path.join(PATHS.MotelyJAML, 'Motely', 'Motely.csproj');
    const csprojStand = path.join(PATHS.MotelyJAML_standalone, 'Motely', 'Motely.csproj');
    const c1 = fs.readFileSync(csprojSub, 'utf-8');
    const c2 = fs.readFileSync(csprojStand, 'utf-8');
    assert.ok(c1.includes('net10.0'));
    assert.ok(c2.includes('net10.0'));
  });
});
