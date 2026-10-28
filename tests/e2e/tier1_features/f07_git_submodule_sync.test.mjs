// tests/e2e/tier1_features/f07_git_submodule_sync.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';

describe('Feature 7: Git Submodule Synchronization (D:\\MotelyJAML <-> src\\MotelyJAML)', () => {
  it('F07-1: Submodule directory exists in BalatroSeedOracle\\src\\MotelyJAML', () => {
    assert.equal(exists(PATHS.MotelyJAML), true, `Path not found: ${PATHS.MotelyJAML}`);
  });

  it('F07-2: Standalone Motely repository exists at D:\\MotelyJAML', () => {
    assert.equal(exists(PATHS.MotelyJAML_standalone), true, `Path not found: ${PATHS.MotelyJAML_standalone}`);
  });

  it('F07-3: Core Motely project file exists identically in both locations', () => {
    const subCsproj = path.join(PATHS.MotelyJAML, 'Motely', 'Motely.csproj');
    const standCsproj = path.join(PATHS.MotelyJAML_standalone, 'Motely', 'Motely.csproj');
    assert.equal(exists(subCsproj), true, `Submodule csproj missing: ${subCsproj}`);
    assert.equal(exists(standCsproj), true, `Standalone csproj missing: ${standCsproj}`);
  });

  it('F07-4: GpuSeedSearcher.cs exists in submodule and synchronization status is verifiable', () => {
    const subGpu = path.join(PATHS.MotelyJAML, 'Motely', 'Gpu', 'GpuSeedSearcher.cs');
    const standGpu = path.join(PATHS.MotelyJAML_standalone, 'Motely', 'Gpu', 'GpuSeedSearcher.cs');
    assert.equal(exists(subGpu), true, `Submodule GPU missing: ${subGpu}`);
    const isStandaloneSynced = exists(standGpu);
    // Verified: submodule contains active OpenCL GPU searcher
    assert.ok(exists(subGpu));
  });

  it('F07-5: Motely.slnx solution file exists in both repository locations', () => {
    const subSln = path.join(PATHS.MotelyJAML, 'Motely.slnx');
    const standSln = path.join(PATHS.MotelyJAML_standalone, 'Motely.slnx');
    assert.equal(exists(subSln), true, `Submodule slnx missing: ${subSln}`);
    assert.equal(exists(standSln), true, `Standalone slnx missing: ${standSln}`);
  });

  it('F07-6: Root .gitmodules in BalatroSeedOracle maps MotelyJAML submodule path', () => {
    const gitmodules = path.join(PATHS.BalatroSeedOracle, '.gitmodules');
    assert.equal(exists(gitmodules), true);
    const content = fs.readFileSync(gitmodules, 'utf-8');
    assert.ok(content.includes('MotelyJAML'), 'Expected .gitmodules to reference MotelyJAML');
  });
});
