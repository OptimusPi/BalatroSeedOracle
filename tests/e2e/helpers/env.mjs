// tests/e2e/helpers/env.mjs
// Central environment configuration and component path resolver
import path from 'node:path';
import fs from 'node:fs';
import { execSync } from 'node:child_process';

export const ECOSYSTEM_ROOT = 'D:\\';

export const PATHS = {
  dungmot: path.resolve(ECOSYSTEM_ROOT, 'dungmot'),
  MotelyJAML: path.resolve(ECOSYSTEM_ROOT, 'BalatroSeedOracle', 'src', 'MotelyJAML'),
  MotelyJAML_standalone: path.resolve(ECOSYSTEM_ROOT, 'MotelyJAML'),
  jamlUi: path.resolve(ECOSYSTEM_ROOT, 'jaml-ui'),
  seedfinderApp: path.resolve(ECOSYSTEM_ROOT, 'seedfinder.app'),
  ErraticDeckApp: path.resolve(ECOSYSTEM_ROOT, 'ErraticDeck.app'),
  BalatroSeedOracle: path.resolve(ECOSYSTEM_ROOT, 'BalatroSeedOracle'),
  e2eTests: path.resolve(ECOSYSTEM_ROOT, 'BalatroSeedOracle', 'tests', 'e2e'),
};

export const DUNGMOT_PATHS = {
  root: PATHS.dungmot,
  parquetLake: path.join(PATHS.dungmot, 'parquet_lake'),
  parquetRanks: path.join(PATHS.dungmot, 'parquet_lake', 'ranks'),
  parquetSuits: path.join(PATHS.dungmot, 'parquet_lake', 'suits'),
  ranksDir: path.join(PATHS.dungmot, 'ranks'),
  suitsDir: path.join(PATHS.dungmot, 'suits'),
  uncommonCsv: path.join(PATHS.dungmot, 'uncommonss.csv'),
  negraresCsv: path.join(PATHS.dungmot, 'negrares.csv'),
  tripleNegativeCsv: path.join(PATHS.dungmot, 'triple-negative.csv'),
  balatroStreamsCuh: path.join(PATHS.dungmot, 'balatro_streams.cuh'),
  negativeRarePrefilterCu: path.join(PATHS.dungmot, 'negative_rare_prefilter.cu'),
};

export function checkTools() {
  const tools = {
    dotnet: false,
    dotnetVersion: '',
    node: false,
    nodeVersion: '',
    pnpm: false,
    pnpmVersion: '',
  };

  try {
    tools.dotnetVersion = execSync('dotnet --version', { encoding: 'utf-8' }).trim();
    tools.dotnet = true;
  } catch {}

  try {
    tools.nodeVersion = process.version;
    tools.node = true;
  } catch {}

  try {
    tools.pnpmVersion = execSync('pnpm --version', { encoding: 'utf-8' }).trim();
    tools.pnpm = true;
  } catch {}

  return tools;
}

export function exists(filePath) {
  return fs.existsSync(filePath);
}
