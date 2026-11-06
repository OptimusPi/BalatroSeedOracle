// tests/e2e/helpers/parquet_oracle.mjs
// Parquet, CSV, and text lake validation oracle
import fs from 'node:fs';

export function validateParquetFile(filePath) {
  if (!fs.existsSync(filePath)) {
    return { valid: false, reason: `File does not exist: ${filePath}` };
  }

  const stat = fs.statSync(filePath);
  if (stat.size < 12) {
    return { valid: false, reason: `File too small to be valid Parquet: ${stat.size} bytes` };
  }

  const fd = fs.openSync(filePath, 'r');
  const headerBuf = Buffer.alloc(4);
  fs.readSync(fd, headerBuf, 0, 4, 0);

  const footerBuf = Buffer.alloc(4);
  fs.readSync(fd, footerBuf, 0, 4, stat.size - 4);
  fs.closeSync(fd);

  const headerMagic = headerBuf.toString('ascii');
  const footerMagic = footerBuf.toString('ascii');

  if (headerMagic !== 'PAR1') {
    return { valid: false, reason: `Invalid Parquet magic header: '${headerMagic}', expected 'PAR1'` };
  }
  if (footerMagic !== 'PAR1') {
    return { valid: false, reason: `Invalid Parquet magic footer: '${footerMagic}', expected 'PAR1'` };
  }

  return { valid: true, sizeBytes: stat.size };
}

export function validateSeedListFile(filePath, minSeeds = 1) {
  if (!fs.existsSync(filePath)) {
    return { valid: false, reason: `Seed list file not found: ${filePath}` };
  }

  const content = fs.readFileSync(filePath, 'utf-8');
  const lines = content.split(/\r?\n/).map(l => l.trim()).filter(l => l.length > 0);

  if (lines.length < minSeeds) {
    return { valid: false, reason: `Expected at least ${minSeeds} seeds, found ${lines.length}` };
  }

  // Balatro seeds are 1 to 8 alphanumeric characters
  const seedRegex = /^[0-9A-Z]{1,8}$/;
  const invalid = lines.filter(s => !seedRegex.test(s));
  if (invalid.length > 0) {
    return { valid: false, reason: `Found invalid seeds (e.g. '${invalid[0]}')`, invalidCount: invalid.length };
  }

  return { valid: true, seedCount: lines.length, seeds: lines };
}

export function validateCsvFile(filePath, expectedHeader) {
  if (!fs.existsSync(filePath)) {
    return { valid: false, reason: `CSV file not found: ${filePath}` };
  }

  const content = fs.readFileSync(filePath, 'utf-8');
  const lines = content.split(/\r?\n/).map(l => l.trim()).filter(l => l.length > 0);

  if (lines.length === 0) {
    return { valid: false, reason: `CSV file is empty: ${filePath}` };
  }

  const firstLine = lines[0];
  const hasHeader = expectedHeader && firstLine.toLowerCase() === expectedHeader.toLowerCase();
  const isHeaderlessData = /^[0-9A-Z]{7,8},\d+$/i.test(firstLine);

  if (expectedHeader && !hasHeader && !isHeaderlessData) {
    return { valid: false, reason: `Invalid header '${firstLine}', expected '${expectedHeader}' or direct seed data` };
  }

  const dataRows = hasHeader ? lines.slice(1) : lines;
  return { valid: true, rowCount: dataRows.length, hasHeader: !!hasHeader, lines: dataRows };
}
