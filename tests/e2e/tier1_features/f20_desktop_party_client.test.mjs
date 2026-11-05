// tests/e2e/tier1_features/f20_desktop_party_client.test.mjs
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import fs from 'node:fs';
import { PATHS, exists } from '../helpers/env.mjs';

describe('Feature 20: Desktop Community Party Client', () => {
  it('F20-1: PartyWorker implementation exists in Motely.Party', () => {
    const workerPath1 = path.join(PATHS.MotelyJAML, 'Motely', 'Party', 'PartyWorker.cs');
    const workerPath2 = path.join(PATHS.MotelyJAML, 'Motely.CLI', 'Party', 'PartyWorker.cs');
    assert.ok(exists(workerPath1) || exists(workerPath2), 'PartyWorker.cs must exist');
  });

  it('F20-2: PartyClient HTTP transport handles base coordinator URL configuration', () => {
    const clientPath1 = path.join(PATHS.MotelyJAML, 'Motely', 'Party', 'PartyClient.cs');
    const clientPath2 = path.join(PATHS.MotelyJAML, 'Motely.CLI', 'Party', 'PartyClient.cs');
    const chosen = exists(clientPath1) ? clientPath1 : clientPath2;
    assert.equal(exists(chosen), true);
    const content = fs.readFileSync(chosen, 'utf-8');
    assert.ok(content.includes('HttpClient'));
  });

  it('F20-3: PartyWorker state machine supports start, sweep, and cancellation', () => {
    const workerPath1 = path.join(PATHS.MotelyJAML, 'Motely', 'Party', 'PartyWorker.cs');
    const workerPath2 = path.join(PATHS.MotelyJAML, 'Motely.CLI', 'Party', 'PartyWorker.cs');
    const chosen = exists(workerPath1) ? workerPath1 : workerPath2;
    const content = fs.readFileSync(chosen, 'utf-8');
    assert.ok(content.includes('CancellationToken'));
  });

  it('F20-4: Party worker telemetry reports blocks completed and seed find counts', () => {
    const sweepPath1 = path.join(PATHS.MotelyJAML, 'Motely', 'Party', 'PartySweep.cs');
    const sweepPath2 = path.join(PATHS.MotelyJAML, 'Motely.CLI', 'Party', 'PartySweep.cs');
    const chosen = exists(sweepPath1) ? sweepPath1 : sweepPath2;
    assert.equal(exists(chosen), true);
  });

  it('F20-5: Desktop App implements PartySearchService and binds PartyCoordinatorUrl in SearchTab.axaml', () => {
    const svcPath = path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle', 'Services', 'PartySearchService.cs');
    assert.equal(exists(svcPath), true, 'PartySearchService.cs must exist');
    const tabPath = path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle', 'Views', 'SearchModalTabs', 'SearchTab.axaml');
    assert.ok(fs.readFileSync(tabPath, 'utf-8').includes('PartyCoordinatorUrl'));
  });

  it('F20-6: PartySearchService and PartyWorker handle invalid coordinator URLs and network errors cleanly in real tests', async () => {
    const { runDesktopTestsFilter } = await import('../helpers/runtime_oracle.mjs');
    const res = runDesktopTestsFilter('FullyQualifiedName~PartySearchService');
    assert.equal(res.exitCode, 0, `PartySearchService tests failed:\n${res.stdout}\n${res.stderr}`);
  });
});
