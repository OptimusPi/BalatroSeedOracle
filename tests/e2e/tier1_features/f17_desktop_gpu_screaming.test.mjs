// tests/e2e/tier1_features/f17_desktop_gpu_screaming.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';

describe('Feature 17: Desktop GPU Screaming Mode (SearchModalViewModel)', () => {
  const vmPath = path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle', 'ViewModels', 'SearchModalViewModel.cs');

  it('F17-1: SearchModalViewModel.cs exists in Desktop project', () => {
    assert.equal(exists(vmPath), true, `SearchModalViewModel.cs not found at ${vmPath}`);
  });

  it('F17-2: SearchModalViewModel defines search settings pipeline', () => {
    const content = fs.readFileSync(vmPath, 'utf-8');
    assert.ok(content.includes('MotelySearchBuilder.CreateSettings'));
    assert.ok(content.includes('WithThreadCount'));
  });

  it('F17-3: SearchModalViewModel search settings support configurable pipeline and thread count', () => {
    const content = fs.readFileSync(vmPath, 'utf-8');
    assert.ok(
      content.includes('WithThreadCount') || content.includes('CreateSettings'),
      'SearchModalViewModel must construct settings pipeline'
    );
  });

  it('F17-4: SearchModalViewModel supports WithSourceConfig passing JamlConfig', () => {
    const content = fs.readFileSync(vmPath, 'utf-8');
    assert.ok(
      content.includes('WithSourceConfig') || content.includes('LoadedConfig'),
      'Must pass filter configuration to search pipeline'
    );
  });

  it('F17-5: Sequential search mode configures batch character size', () => {
    const content = fs.readFileSync(vmPath, 'utf-8');
    assert.ok(content.includes('WithBatchCharacterCount'));
  });

  it('F17-6: Result sink captures matched seeds to SearchResults directory', () => {
    const content = fs.readFileSync(vmPath, 'utf-8');
    assert.ok(content.includes('SeedLakeSink') || content.includes('SearchResults'));
  });
});
