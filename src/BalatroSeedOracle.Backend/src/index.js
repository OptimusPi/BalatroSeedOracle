export default {
  async fetch(request, env, ctx) {
    const url = new URL(request.url);
    const path = url.pathname;

    // CORS Headers
    const corsHeaders = {
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
      'Access-Control-Allow-Headers': 'Content-Type',
      // Required for SharedArrayBuffer compatibility in clients
      'Cross-Origin-Opener-Policy': 'same-origin',
      'Cross-Origin-Embedder-Policy': 'require-corp'
    };

    if (request.method === 'OPTIONS') {
      return new Response(null, { headers: corsHeaders });
    }

    // 1. Game Configs (JAML Cartridges from R2)
    // Route: /games/:gameId.jaml
    if (request.method === 'GET' && path.startsWith('/games/')) {
      const gameId = path.split('/')[2]; // e.g., "weejoker_season1.jaml"
      
      if (!gameId) {
        return new Response('Game ID required', { status: 400, headers: corsHeaders });
      }

      // Fetch from R2
      const object = await env.SEED_ASSETS.get(`games/${gameId}`);
      if (object === null) {
        return new Response('Game config not found', { status: 404, headers: corsHeaders });
      }

      const headers = new Headers(corsHeaders);
      object.writeHttpMetadata(headers);
      headers.set('etag', object.httpEtag);
      headers.set('Content-Type', 'text/yaml'); // or application/x-yaml

      return new Response(object.body, { headers });
    }

    // 2. Scores API (D1)
    // Route: /scores
    if (path.startsWith('/scores')) {
      // GET /scores?seed=ABC12345
      if (request.method === 'GET') {
        const seed = url.searchParams.get('seed');
        if (!seed) {
           return new Response('Seed required', { status: 400, headers: corsHeaders });
        }

        // Fetch top 10 scores for this seed
        const query = `
          SELECT player_name, score_display, score_value, ante 
          FROM scores 
          WHERE seed = ? 
          ORDER BY score_value DESC 
          LIMIT 10
        `;
        
        try {
          const { results } = await env.DB.prepare(query).bind(seed).all();
          return Response.json(results, { headers: corsHeaders });
        } catch (e) {
          return new Response(e.message, { status: 500, headers: corsHeaders });
        }
      }

      // POST /scores (Submit Score)
      if (request.method === 'POST') {
        try {
          const body = await request.json();
          const { ritual_id, seed, player_name, score_display, score_value, ante } = body;

          // Basic Validation
          if (!ritual_id || !seed || !player_name || !score_value) {
             return new Response('Missing fields', { status: 400, headers: corsHeaders });
          }

          // Insert or Replace
          const query = `
            INSERT OR REPLACE INTO scores (ritual_id, seed, player_name, score_display, score_value, ante, submitted_at)
            VALUES (?, ?, ?, ?, ?, ?, datetime('now'))
          `;

          await env.DB.prepare(query)
            .bind(ritual_id, seed, player_name, score_display, score_value, ante || 0)
            .run();

          return new Response('Score submitted', { status: 201, headers: corsHeaders });
        } catch (e) {
          return new Response(e.message, { status: 500, headers: corsHeaders });
        }
      }
    }

    return new Response('Not Found', { status: 404, headers: corsHeaders });
  },
};
