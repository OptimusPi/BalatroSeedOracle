// Cloudflare Worker for Balatro Seed Oracle Genie
import { handleWorkerRequest } from './src/genie.js';

export default {
  async fetch(request, env, ctx) {
    // Handle CORS preflight
    if (request.method === 'OPTIONS') {
      return new Response(null, {
        headers: {
          'Access-Control-Allow-Origin': '*',
          'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
          'Access-Control-Allow-Headers': 'Content-Type',
        },
      });
    }

    const url = new URL(request.url);
    
    // Serve HTML interface
    if (url.pathname === '/' && request.method === 'GET') {
      const html = await env.ASSETS.get('index.html');
      return new Response(html, {
        headers: { 'Content-Type': 'text/html' }
      });
    }
    
    // Serve JS modules
    if (url.pathname.startsWith('/src/') && request.method === 'GET') {
      const file = await env.ASSETS.get(url.pathname.slice(1));
      return new Response(file, {
        headers: { 'Content-Type': 'application/javascript' }
      });
    }
    
    // API endpoint
    if (url.pathname === '/api/generate' && request.method === 'POST') {
      return handleWorkerRequest(request, env);
    }
    
    return new Response('Not Found', { status: 404 });
  },
};