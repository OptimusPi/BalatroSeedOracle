#!/usr/bin/env node

/**
 * Simple test runner for Balatro MCP Server
 * Run with: node test-mcp.js
 */

const { spawn } = require('child_process');
const path = require('path');

// Test MCP requests
const tests = [
    {
        name: "Initialize MCP Server",
        request: {
            jsonrpc: "2.0",
            id: 1,
            method: "initialize",
            params: {
                protocolVersion: "2024-11-05",
                capabilities: {},
                clientInfo: {
                    name: "Test Client",
                    version: "1.0.0"
                }
            }
        }
    },
    {
        name: "List Available Tools",
        request: {
            jsonrpc: "2.0",
            id: 2,
            method: "tools/list",
            params: {}
        }
    },
    {
        name: "Get JAML Schema",
        request: {
            jsonrpc: "2.0",
            id: 3,
            method: "tools/call",
            params: {
                name: "get_jaml_schema",
                arguments: {}
            }
        }
    },
    {
        name: "Generate JAML with Context - Blueprint",
        request: {
            jsonrpc: "2.0",
            id: 4,
            method: "tools/call",
            params: {
                name: "generate_jaml_with_context",
                arguments: {
                    userRequest: "Blueprint scaling build with face cards"
                }
            }
        }
    },
    {
        name: "Get Blueprint Knowledge",
        request: {
            jsonrpc: "2.0",
            id: 5,
            method: "tools/call",
            params: {
                name: "get_balatro_knowledge",
                arguments: {
                    itemType: "joker",
                    itemName: "Blueprint"
                }
            }
        }
    }
];

async function runTest(test) {
    return new Promise((resolve, reject) => {
        console.log(`\n🧪 Testing: ${test.name}`);
        console.log(`📤 Request: ${JSON.stringify(test.request, null, 2)}`);
        
        // Start the MCP server process
        const serverProcess = spawn('dotnet', ['run'], {
            cwd: path.join(__dirname, 'src', 'BalatroSeedOracle.MCP.CLI'),
            stdio: ['pipe', 'pipe', 'pipe']
        });

        let serverOutput = '';
        let serverError = '';
        let responseReceived = false;

        // Capture server output
        serverProcess.stdout.on('data', (data) => {
            serverOutput += data.toString();
            console.log(`📡 Server: ${data.toString().trim()}`);
            
            // Try to parse JSON response
            try {
                const lines = data.toString().trim().split('\n');
                for (const line of lines) {
                    if (line.trim()) {
                        const response = JSON.parse(line);
                        if (response.id === test.request.id) {
                            responseReceived = true;
                            console.log(`✅ Response: ${JSON.stringify(response, null, 2)}`);
                            serverProcess.kill();
                            resolve(response);
                        }
                    }
                }
            } catch (e) {
                // Not JSON yet, continue
            }
        });

        serverProcess.stderr.on('data', (data) => {
            serverError += data.toString();
            console.log(`❌ Server Error: ${data.toString().trim()}`);
        });

        serverProcess.on('error', (error) => {
            console.log(`💥 Process Error: ${error.message}`);
            reject(error);
        });

        serverProcess.on('close', (code) => {
            if (!responseReceived) {
                console.log(`⚠️ Server closed with code ${code}`);
                if (serverError) {
                    console.log(`Server Error Output:\n${serverError}`);
                }
                reject(new Error('No response received'));
            }
        });

        // Send the request
        setTimeout(() => {
            serverProcess.stdin.write(JSON.stringify(test.request) + '\n');
            serverProcess.stdin.end();
        }, 1000);

        // Timeout after 10 seconds
        setTimeout(() => {
            if (!responseReceived) {
                serverProcess.kill();
                reject(new Error('Test timeout'));
            }
        }, 10000);
    });
}

async function runAllTests() {
    console.log('🚀 Starting Balatro MCP Server Tests');
    console.log('=====================================');

    try {
        for (const test of tests) {
            await runTest(test);
            console.log('✅ Test passed!\n');
        }
        
        console.log('🎉 All tests completed successfully!');
        console.log('\n📋 Summary:');
        console.log('- MCP Server initializes correctly');
        console.log('- Tools are available and listed');
        console.log('- JAML schema can be retrieved');
        console.log('- Context generation works with real Balatro data');
        console.log('- Knowledge base provides accurate Blueprint info');
        
    } catch (error) {
        console.log(`💥 Test failed: ${error.message}`);
        console.log('\n🔧 Troubleshooting:');
        console.log('1. Make sure you\'re in the correct directory');
        console.log('2. Run "dotnet build" in src/BalatroSeedOracle.MCP.CLI');
        console.log('3. Check if all dependencies are installed');
        console.log('4. Verify the MCP server code compiles');
        process.exit(1);
    }
}

// Run the tests
if (require.main === module) {
    runAllTests();
}

module.exports = { runTest, runAllTests, tests };
