// DuckDB-WASM Interop for Avalonia Browser
// This module provides JavaScript interop for DuckDB-WASM to be called from .NET

import * as duckdb from './duckdb-wasm/duckdb.mjs';

let db = null;
let connections = new Map();
let appenders = new Map();
let nextConnectionId = 1;
let nextAppenderId = 1;

// Initialize DuckDB with EH build (exception handling)
window.DuckDB = {
    // Initialization
    initialize: async function() {
        if (db !== null) {
            return true;
        }

        try {
            const MANUAL_BUNDLES = {
                mvp: {
                    mainModule: './js/duckdb-wasm/duckdb-eh.wasm',
                    mainWorker: './js/duckdb-wasm/duckdb-browser-eh.worker.js'
                },
                eh: {
                    mainModule: './js/duckdb-wasm/duckdb-eh.wasm',
                    mainWorker: './js/duckdb-wasm/duckdb-browser-eh.worker.js'
                }
            };

            const bundle = await duckdb.selectBundle(MANUAL_BUNDLES);
            const worker = new Worker(bundle.mainWorker);
            const logger = new duckdb.ConsoleLogger();

            db = new duckdb.AsyncDuckDB(logger, worker);
            await db.instantiate(bundle.mainModule);

            // Open database with OPFS persistence for IndexedDB-like storage
            await db.open({
                path: ':memory:',
                query: { castBigIntToDouble: true }
            });

            console.log('DuckDB-WASM initialized successfully');
            return true;
        } catch (error) {
            console.error('Failed to initialize DuckDB-WASM:', error);
            throw error;
        }
    },

    // Check if initialized
    isInitialized: function() {
        return db !== null;
    },

    // Connection management
    openConnection: async function() {
        if (!db) throw new Error('DuckDB not initialized');

        const conn = await db.connect();
        const id = nextConnectionId++;
        connections.set(id, conn);
        return id;
    },

    closeConnection: async function(connId) {
        const conn = connections.get(connId);
        if (conn) {
            await conn.close();
            connections.delete(connId);
        }
    },

    // Execute SQL (no results)
    execute: async function(connId, sql) {
        const conn = connections.get(connId);
        if (!conn) throw new Error('Connection not found: ' + connId);

        await conn.query(sql);
    },

    // Query with results (returns JSON array)
    query: async function(connId, sql) {
        const conn = connections.get(connId);
        if (!conn) throw new Error('Connection not found: ' + connId);

        const result = await conn.query(sql);
        return JSON.stringify(result.toArray().map(row => row.toJSON()));
    },

    // Appender for bulk inserts
    createAppender: async function(connId, schema, table) {
        const conn = connections.get(connId);
        if (!conn) throw new Error('Connection not found: ' + connId);

        // DuckDB-WASM doesn't have native appender, use prepared statement approach
        const id = nextAppenderId++;
        appenders.set(id, {
            connId: connId,
            schema: schema,
            table: table,
            rows: []
        });
        return id;
    },

    appendRow: function(appenderId, valuesJson) {
        const appender = appenders.get(appenderId);
        if (!appender) throw new Error('Appender not found: ' + appenderId);

        // Parse JSON array from C# interop
        const values = typeof valuesJson === 'string' ? JSON.parse(valuesJson) : valuesJson;
        appender.rows.push(values);
    },

    flushAppender: async function(appenderId) {
        const appender = appenders.get(appenderId);
        if (!appender) throw new Error('Appender not found: ' + appenderId);

        if (appender.rows.length === 0) return;

        const conn = connections.get(appender.connId);
        if (!conn) throw new Error('Connection not found');

        // Build bulk INSERT statement
        const tableName = appender.schema ? `${appender.schema}.${appender.table}` : appender.table;
        const values = appender.rows.map(row =>
            '(' + row.map(v => typeof v === 'string' ? `'${v.replace(/'/g, "''")}'` : v).join(',') + ')'
        ).join(',');

        const sql = `INSERT INTO ${tableName} VALUES ${values}`;
        await conn.query(sql);

        appender.rows = [];
    },

    closeAppender: async function(appenderId) {
        const appender = appenders.get(appenderId);
        if (appender) {
            if (appender.rows.length > 0) {
                await this.flushAppender(appenderId);
            }
            appenders.delete(appenderId);
        }
    },

    // Utility: Export table to CSV string
    exportToCSV: async function(connId, tableName) {
        const conn = connections.get(connId);
        if (!conn) throw new Error('Connection not found: ' + connId);

        const result = await conn.query(`SELECT * FROM ${tableName}`);
        const rows = result.toArray();

        if (rows.length === 0) return '';

        const headers = Object.keys(rows[0].toJSON()).join(',');
        const data = rows.map(row => Object.values(row.toJSON()).join(',')).join('\n');

        return headers + '\n' + data;
    },

    // Get row count
    getRowCount: async function(connId, tableName) {
        const conn = connections.get(connId);
        if (!conn) throw new Error('Connection not found: ' + connId);

        const result = await conn.query(`SELECT COUNT(*) as cnt FROM ${tableName}`);
        const rows = result.toArray();
        return rows[0].toJSON().cnt;
    }
};

console.log('DuckDB interop module loaded');
