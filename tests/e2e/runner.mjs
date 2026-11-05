#!/usr/bin/env node
// tests/e2e/runner.mjs
// Standalone automated test runner for Balatro Seed Oracle E2E test suite
import path from 'node:path';
import fs from 'node:fs';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import { PATHS } from './helpers/env.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const e2eDir = __dirname;

const VALID_TIERS = new Set(['1', '2', '3', '4']);
const VALID_COMPONENTS = new Set([
  'dungmot',
  'motely',
  'motelyjaml',
  'jaml-ui',
  'seedfinder',
  'seedfinder.app',
  'erraticdeck',
  'erraticdeck.app',
  'desktop',
  'balatroseedoracle',
]);

function printUsage(stream = console.log) {
  stream(`
Balatro Seed Oracle — E2E Test Suite Runner

Usage:
  node tests/e2e/runner.mjs [options]

Options:
  -t, --tier <1|2|3|4>        Run tests from specific tier only
  -f, --feature <1-23>        Run tests covering specific feature index (e.g. 1)
  -c, --component <name>      Filter tests by component name (dungmot, motely, jaml-ui, seedfinder, erraticdeck, desktop)
  -v, --verbose               Enable verbose runner output
  -h, --help                  Show this usage help message

Examples:
  node tests/e2e/runner.mjs                     # Run all tiers 1-4
  node tests/e2e/runner.mjs --tier 1            # Run Tier 1 Feature Coverage
  node tests/e2e/runner.mjs --feature 1         # Run Feature 1 (Shop Joker GPU)
  node tests/e2e/runner.mjs --component jaml-ui # Run all tests touching jaml-ui
`);
}

function parseArgs() {
  const args = process.argv.slice(2);
  const options = {
    tier: null,
    feature: null,
    component: null,
    verbose: false,
    help: false,
    error: null,
  };

  for (let i = 0; i < args.length; i++) {
    const arg = args[i];
    if (arg === '--help' || arg === '-h') {
      options.help = true;
    } else if (arg === '--verbose' || arg === '-v') {
      options.verbose = true;
    } else if (arg === '--tier' || arg === '-t') {
      if (i + 1 >= args.length) {
        options.error = 'Missing value for --tier (expected 1, 2, 3, or 4).';
        return options;
      }
      const val = args[++i];
      if (!VALID_TIERS.has(val)) {
        options.error = `Invalid --tier '${val}'. Expected one of: 1, 2, 3, 4.`;
        return options;
      }
      options.tier = val;
    } else if (arg === '--feature' || arg === '-f') {
      if (i + 1 >= args.length) {
        options.error = 'Missing value for --feature (expected 1..23).';
        return options;
      }
      const rawFeat = args[++i];
      const num = Number.parseInt(rawFeat, 10);
      if (!Number.isInteger(num) || num < 1 || num > 23) {
        options.error = `Invalid --feature '${rawFeat}'. Expected integer 1..23.`;
        return options;
      }
      options.feature = num;
    } else if (arg === '--component' || arg === '-c') {
      if (i + 1 >= args.length) {
        options.error = 'Missing value for --component.';
        return options;
      }
      const rawComp = args[++i];
      const comp = rawComp.toLowerCase();
      if (!VALID_COMPONENTS.has(comp)) {
        options.error = `Invalid --component '${rawComp}'. Expected one of: ${[...VALID_COMPONENTS].join(', ')}.`;
        return options;
      }
      options.component = comp;
    } else {
      options.error = `Unknown CLI flag '${arg}'.`;
      return options;
    }
  }

  return options;
}

function collectTestFiles(options) {
  const tiers = [
    { name: 'Tier 1: Feature Coverage', dir: path.join(e2eDir, 'tier1_features'), tierNum: '1' },
    { name: 'Tier 2: Boundary & Corner Cases', dir: path.join(e2eDir, 'tier2_boundaries'), tierNum: '2' },
    { name: 'Tier 3: Cross-Feature Combinations', dir: path.join(e2eDir, 'tier3_combinations'), tierNum: '3' },
    { name: 'Tier 4: Real-World Scenarios', dir: path.join(e2eDir, 'tier4_scenarios'), tierNum: '4' },
  ];

  const selectedFiles = [];

  for (const tier of tiers) {
    if (options.tier && options.tier !== tier.tierNum) continue;
    if (!fs.existsSync(tier.dir)) continue;

    const files = fs.readdirSync(tier.dir)
      .filter(f => f.endsWith('.test.mjs'))
      .sort()
      .map(f => path.join(tier.dir, f));

    for (const file of files) {
      const baseName = path.basename(file);

      if (options.feature !== null) {
        const featPrefix = `f${options.feature.toString().padStart(2, '0')}`;
        if (!baseName.startsWith(featPrefix)) continue;
      }

      if (options.component) {
        const comp = options.component.toLowerCase();
        const content = fs.readFileSync(file, 'utf-8').toLowerCase();
        if (!baseName.toLowerCase().includes(comp) && !content.includes(comp)) continue;
      }

      selectedFiles.push({ file, tierName: tier.name, tierNum: tier.tierNum });
    }
  }

  return selectedFiles;
}

async function main() {
  const options = parseArgs();

  if (options.error) {
    console.error(`Error: ${options.error}`);
    printUsage(console.error);
    process.exit(1);
  }

  if (options.help) {
    printUsage();
    process.exit(0);
  }

  console.log('='.repeat(78));
  console.log('  Balatro Seed Oracle — Independent Opaque-Box E2E Test Suite');
  console.log('='.repeat(78));

  const items = collectTestFiles(options);
  if (items.length === 0) {
    console.log('[!] No test files matched the specified filters.');
    process.exit(0);
  }

  console.log(`Found ${items.length} test files to execute.`);
  console.log('-'.repeat(78));

  const startTime = Date.now();
  const filePaths = items.map(i => i.file);

  const testProc = spawnSync(
    process.execPath,
    ['--no-warnings', '--test', '--test-concurrency=1', ...filePaths],
    {
      cwd: PATHS.BalatroSeedOracle,
      encoding: 'utf-8',
      stdio: options.verbose ? 'inherit' : 'pipe',
      maxBuffer: 32 * 1024 * 1024,
    }
  );

  const durationSec = ((Date.now() - startTime) / 1000).toFixed(2);

  if (!options.verbose && testProc.stdout) {
    console.log(testProc.stdout);
  }
  if (testProc.stderr && testProc.stderr.trim().length > 0) {
    console.error(testProc.stderr);
  }

  console.log('='.repeat(78));
  console.log('  Test Execution Summary');
  console.log('='.repeat(78));
  console.log(`Duration:    ${durationSec}s`);
  console.log(`Exit Code:   ${testProc.status}`);

  if (testProc.status === 0) {
    console.log(`Status:      ALL TESTS PASSED [100% SUCCESS]`);
    process.exit(0);
  } else {
    console.log(`Status:      FAILED (Exit code ${testProc.status})`);
    process.exit(testProc.status || 1);
  }
}

main().catch(err => {
  console.error('Fatal error during test run:', err);
  process.exit(1);
});
