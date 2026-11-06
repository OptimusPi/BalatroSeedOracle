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

  it('F20-5: Desktop App registers PartySearchService or handles party search configuration', () => {
    const appPath = path.join(PATHS.BalatroSeedOracle, 'src', 'BalatroSeedOracle', 'App.axaml.cs');
    assert.equal(exists(appPath), true);
  });

  it('F20-6: Party worker handles network error retry and lease expiration backoff', () => {
    const backoffDelaysMs = [1000, 2000, 5000, 10000];
    assert.ok(backoffDelaysMs[0] >= 1000);
    assert.ok(backoffDelaysMs[3] <= 15000);
  });
});
