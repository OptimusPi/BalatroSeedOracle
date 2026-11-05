// tests/e2e/helpers/parquet_oracle.mjs
// Parquet binary footer/schema, CSV, and text lake validation oracle
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
  let headerMagic = '';
  let footerMagic = '';
  let footerLength = 0;
  try {
    const headerBuf = Buffer.alloc(4);
    fs.readSync(fd, headerBuf, 0, 4, 0);
    headerMagic = headerBuf.toString('ascii');

    const tailBuf = Buffer.alloc(8);
    fs.readSync(fd, tailBuf, 0, 8, stat.size - 8);
    footerLength = tailBuf.readUInt32LE(0);
    footerMagic = tailBuf.subarray(4, 8).toString('ascii');
  } finally {
    fs.closeSync(fd);
  }

  if (headerMagic !== 'PAR1') {
    return { valid: false, reason: `Invalid Parquet magic header: '${headerMagic}', expected 'PAR1'` };
  }
  if (footerMagic !== 'PAR1') {
    return { valid: false, reason: `Invalid Parquet magic footer: '${footerMagic}', expected 'PAR1'` };
  }
  if (footerLength <= 0 || footerLength + 12 > stat.size) {
    return { valid: false, reason: `Invalid Parquet Thrift footer length: ${footerLength}` };
  }

  return { valid: true, sizeBytes: stat.size, footerLength };
}

/**
 * Reads ONLY the trailing Thrift FileMetaData footer from an Apache Parquet file
 * (constant O(footerLength) byte range read — safe on multi-GB files) and decodes
 * the Thrift Compact Protocol SchemaElement list (`version`, `schema`, `num_rows`).
 */
export function readParquetMetadata(filePath) {
  const check = validateParquetFile(filePath);
  if (!check.valid) {
    throw new Error(check.reason);
  }

  const fd = fs.openSync(filePath, 'r');
  const footerBuf = Buffer.alloc(check.footerLength);
  try {
    fs.readSync(fd, footerBuf, 0, check.footerLength, check.sizeBytes - 8 - check.footerLength);
  } finally {
    fs.closeSync(fd);
  }

  // Minimal Thrift Compact Protocol reader for FileMetaData:
  // field 1: i32 version
  // field 2: list<SchemaElement> schema (SchemaElement field 4 is binary/string `name`)
  // field 3: i64 num_rows
  let pos = 0;
  const readVarint = () => {
    let result = 0n;
    let shift = 0n;
    while (pos < footerBuf.length) {
      const b = footerBuf[pos++];
      result |= BigInt(b & 0x7f) << shift;
      if ((b & 0x80) === 0) break;
      shift += 7n;
    }
    return result;
  };
  const readZigZag = () => {
    const n = readVarint();
    return (n >> 1n) ^ -(n & 1n);
  };
  const skipType = (type) => {
    switch (type) {
      case 1:
      case 2:
        return; // boolean true/false encoded in field header
      case 3:
        pos += 1;
        return;
      case 4:
      case 5:
      case 6:
        readVarint();
        return;
      case 7:
        pos += 8;
        return;
      case 8: {
        const len = Number(readVarint());
        pos += len;
        return;
      }
      case 9:
      case 10: {
        const hdr = footerBuf[pos++];
        let size = hdr >> 4;
        const elemType = hdr & 0x0f;
        if (size === 15) size = Number(readVarint());
        for (let i = 0; i < size; i++) skipType(elemType);
        return;
      }
      case 11: {
        const size = Number(readVarint());
        if (size === 0) return;
        const kv = footerBuf[pos++];
        const kType = kv >> 4;
        const vType = kv & 0x0f;
        for (let i = 0; i < size; i++) {
          skipType(kType);
          skipType(vType);
        }
        return;
      }
      case 12: {
        while (pos < footerBuf.length) {
          const fh = footerBuf[pos++];
          if (fh === 0) break;
          const delta = fh >> 4;
          const ft = fh & 0x0f;
          if (delta === 0) readZigZag();
          skipType(ft);
        }
        return;
      }
      default:
        throw new Error(`Unsupported Thrift compact type ${type} at offset ${pos}`);
    }
  };

  let version = 0;
  let numRows = 0n;
  const schemaElements = [];
  let prevFieldId = 0;

  while (pos < footerBuf.length) {
    const fh = footerBuf[pos++];
    if (fh === 0) break;
    const delta = fh >> 4;
    const fieldType = fh & 0x0f;
    const fieldId = delta === 0 ? Number(readZigZag()) : prevFieldId + delta;
    prevFieldId = fieldId;

    if (fieldId === 1 && fieldType === 5) {
      version = Number(readZigZag());
    } else if (fieldId === 2 && fieldType === 9) {
      const hdr = footerBuf[pos++];
      let count = hdr >> 4;
      if (count === 15) count = Number(readVarint());
      for (let i = 0; i < count; i++) {
        let elemPrevField = 0;
        let name = '';
        let numChildren = 0;
        while (pos < footerBuf.length) {
          const efh = footerBuf[pos++];
          if (efh === 0) break;
          const edelta = efh >> 4;
          const eft = efh & 0x0f;
          const efid = edelta === 0 ? Number(readZigZag()) : elemPrevField + edelta;
          elemPrevField = efid;
          if (efid === 4 && eft === 8) {
            const len = Number(readVarint());
            name = footerBuf.subarray(pos, pos + len).toString('utf-8');
            pos += len;
          } else if (efid === 5 && eft === 5) {
            numChildren = Number(readZigZag());
          } else {
            skipType(eft);
          }
        }
        schemaElements.push({ name, numChildren });
      }
    } else if (fieldId === 3 && fieldType === 6) {
      numRows = readZigZag();
    } else {
      skipType(fieldType);
    }
  }

  // First schema element is the root struct (e.g. duckdb_schema); leaf columns follow.
  const columns = schemaElements.slice(1).filter(e => e.numChildren === 0).map(e => e.name);
  return {
    version,
    rootName: schemaElements[0]?.name ?? '',
    columns,
    numRows: Number(numRows),
    sizeBytes: check.sizeBytes,
  };
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

  // Balatro seeds use MotelyGlobals.SeedDigits: 1-9 and A-Z (never 0)
  const seedRegex = /^[1-9A-Z]{1,8}$/;
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
  const isHeaderlessData = /^[1-9A-Z]{1,8},\d+$/i.test(firstLine);

  if (expectedHeader && !hasHeader && !isHeaderlessData) {
    return { valid: false, reason: `Invalid header '${firstLine}', expected '${expectedHeader}' or direct seed data` };
  }

  const dataRows = hasHeader ? lines.slice(1) : lines;
  return { valid: true, rowCount: dataRows.length, hasHeader: !!hasHeader, lines: dataRows };
}
