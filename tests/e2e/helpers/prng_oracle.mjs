// tests/e2e/helpers/prng_oracle.mjs
// Authoritative independent reference oracle for Balatro PRNG, Lua LFSR, and stream keys

export const EDITION_THRESHOLDS = {
  Negative: 0.997,
  Polychrome: 0.994,
  Holographic: 0.980,
  Foil: 0.960,
};

export const RARITY_THRESHOLDS = {
  Rare: 0.95,
  Uncommon: 0.70,
  Common: 0.0,
};

export const EDITION_CODES = {
  Any: 0,
  Foil: 1,
  Holographic: 2,
  Polychrome: 3,
  Negative: 4,
};

export const RARITY_CODES = {
  Any: 0,
  Common: 1,
  Uncommon: 2,
  Rare: 3,
};

/**
 * Calculates Balatro's pseudohash over an arbitrary ASCII string
 * Formula: fmod(1.0 + (sum(char * 1.1239285023) * 3.141592653589793116), 1.0)
 */
export function pseudoHash(str) {
  let num = 0.0;
  for (let i = str.length - 1; i >= 0; i--) {
    num = (num + (str.charCodeAt(i) * 1.1239285023) % 1.0) % 1.0;
  }
  let hash = (num * 3.141592653589793116) % 1.0;
  return hash < 0 ? hash + 1.0 : hash;
}

/**
 * Optimized pseudohash for 8-character seed strings
 */
export function pseudoHash8(seed) {
  if (seed.length !== 8) return pseudoHash(seed);
  let num = 0.0;
  for (let i = 7; i >= 0; i--) {
    num = (num + (seed.charCodeAt(i) * 1.1239285023) % 1.0) % 1.0;
  }
  let hash = (num * 3.141592653589793116) % 1.0;
  return hash < 0 ? hash + 1.0 : hash;
}

/**
 * Generates stream keys exactly matching balatro_streams.cuh and dungmot
 */
export function getShopEditionKey(ante) {
  return `edisho${ante}`;
}

export function getShopRarityKey(ante) {
  return `rarity${ante}sho`;
}

export function getBuffoonEditionKey(ante) {
  return `edibuf${ante}`;
}

export function getBuffoonRarityKey(ante) {
  return `rarity${ante}buf`;
}

/**
 * Evaluates edition roll
 */
export function classifyEdition(roll) {
  if (roll > EDITION_THRESHOLDS.Negative) return 'Negative';
  if (roll > EDITION_THRESHOLDS.Polychrome) return 'Polychrome';
  if (roll > EDITION_THRESHOLDS.Holographic) return 'Holographic';
  if (roll > EDITION_THRESHOLDS.Foil) return 'Foil';
  return 'Base';
}

/**
 * Evaluates rarity roll
 */
export function classifyRarity(roll) {
  if (roll > RARITY_THRESHOLDS.Rare) return 'Rare';
  if (roll > RARITY_THRESHOLDS.Uncommon) return 'Uncommon';
  return 'Common';
}

/**
 * PRNG state iterator matching LuaJIT / balatro_streams.cuh
 */
export function iteratePrng(state) {
  const PI = 3.141592653589793116;
  let next = (state * 1e13 + PI) % 1.0;
  return next < 0 ? next + 1.0 : next;
}
