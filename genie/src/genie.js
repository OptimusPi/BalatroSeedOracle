// Balatro Seed Oracle Genie - Core Logic
// Works both as Cloudflare Worker AND embedded in Avalonia WebView

import { 
  MOTELY_JSON_SCHEMA, 
  GAMING_CONTEXT, 
  FEW_SHOT_EXAMPLES, 
  validateConfig 
} from './schema.js';

export class BalatroGenie {
  constructor(config = {}) {
    this.aiProvider = config.aiProvider || 'cloudflare'; // 'cloudflare' or 'local'
    this.apiEndpoint = config.apiEndpoint || null;
    this.modelName = config.modelName || '@cf/meta/llama-3-8b-instruct';
  }

  // Generate system prompt for the AI
  getSystemPrompt() {
    return `You are a Balatro Seed Oracle config generator. Convert natural language requests into valid MotelyJson filter configs.

${GAMING_CONTEXT}

IMPORTANT RULES:
1. Use lowercase for all type fields: "joker", "voucher", "tarotcard", etc.
2. Value fields should use PascalCase for joker names: "Blueprint", "Perkeo", "TurtleBean"
3. Always include antes array (usually [1,2,3,4,5,6,7,8])
4. Items in 'should' array MUST have a 'score' field
5. Return ONLY valid JSON, no explanations

Schema: ${JSON.stringify(MOTELY_JSON_SCHEMA, null, 2)}

Examples:
${FEW_SHOT_EXAMPLES.map(ex => 
  `User: "${ex.input}"
Config: ${JSON.stringify(ex.output, null, 2)}`
).join('\n\n')}`;
  }

  // Convert user prompt to filter config using AI
  async generateConfig(userPrompt, aiBinding = null) {
    try {
      let response;
      
      if (this.aiProvider === 'cloudflare' && aiBinding) {
        // Cloudflare Workers AI
        response = await this.callCloudflareAI(userPrompt, aiBinding);
      } else {
        // Fallback to a simple rule-based system for testing
        response = this.fallbackGenerator(userPrompt);
      }

      // Parse and validate the response
      let config;
      try {
        // Extract JSON from response (AI might add text around it)
        const jsonMatch = response.match(/\{[\s\S]*\}/);
        config = JSON.parse(jsonMatch ? jsonMatch[0] : response);
      } catch (e) {
        throw new Error(`Failed to parse AI response: ${e.message}`);
      }

      // Validate the config
      const validation = validateConfig(config);
      if (!validation.valid) {
        throw new Error(`Invalid config: ${validation.errors.join(', ')}`);
      }

      return {
        success: true,
        config,
        prompt: userPrompt
      };
    } catch (error) {
      return {
        success: false,
        error: error.message,
        prompt: userPrompt
      };
    }
  }

  // Call Cloudflare Workers AI
  async callCloudflareAI(userPrompt, ai) {
    const messages = [
      { role: 'system', content: this.getSystemPrompt() },
      { role: 'user', content: userPrompt }
    ];

    const response = await ai.run(this.modelName, {
      messages,
      temperature: 0.7,
      max_tokens: 1000
    });

    return response.response || response.text || JSON.stringify(response);
  }

  // Simple fallback generator for testing without AI
  fallbackGenerator(userPrompt) {
    const prompt = userPrompt.toLowerCase();
    
    // Default base config
    const config = {
      name: "Generated Filter",
      description: userPrompt,
      author: "Genie",
      deck: "red",
      stake: "white",
      must: [],
      should: [],
      mustNot: []
    };

    // Simple keyword matching
    if (prompt.includes('perkeo')) {
      config.name = "Perkeo Seed";
      config.deck = "ghost";
      config.must.push({
        type: "souljoker",
        value: "Perkeo",
        antes: [1,2,3,4,5,6,7,8]
      });
    }

    if (prompt.includes('blueprint')) {
      config.name = config.name === "Generated Filter" ? "Blueprint Build" : config.name + " Blueprint";
      config.must.push({
        type: "joker",
        value: "Blueprint",
        antes: [1,2,3,4,5,6,7,8]
      });
    }

    if (prompt.includes('negative')) {
      config.should.push({
        type: "smallblindtag",
        value: "NegativeTag",
        antes: [2,3,4,5,6,7,8],
        score: 5
      });
    }

    if (prompt.includes('money') || prompt.includes('gold') || prompt.includes('economy')) {
      config.deck = "green";
      config.should.push(
        { type: "joker", value: "Bull", antes: [1,2,3], score: 5 },
        { type: "joker", value: "Rocket", antes: [1,2,3], score: 5 },
        { type: "joker", value: "Egg", antes: [1,2,3], score: 3 }
      );
    }

    if (prompt.includes('observatory')) {
      config.must.push({
        type: "voucher",
        value: "Observatory",
        antes: [1,2,3,4,5,6,7,8]
      });
      config.should.push({
        type: "voucher",
        value: "Telescope",
        antes: [1,2,3,4,5,6,7],
        score: 5
      });
    }

    return JSON.stringify(config);
  }
}

// For Cloudflare Worker environment
export async function handleWorkerRequest(request, env) {
  const genie = new BalatroGenie({
    aiProvider: 'cloudflare',
    modelName: env.AI_MODEL || '@cf/meta/llama-3-8b-instruct'
  });

  try {
    const { prompt } = await request.json();
    if (!prompt) {
      return new Response(JSON.stringify({ error: 'No prompt provided' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    const result = await genie.generateConfig(prompt, env.AI);
    
    return new Response(JSON.stringify(result), {
      headers: { 
        'Content-Type': 'application/json',
        'Access-Control-Allow-Origin': '*'
      }
    });
  } catch (error) {
    return new Response(JSON.stringify({ 
      error: error.message,
      success: false 
    }), {
      status: 500,
      headers: { 'Content-Type': 'application/json' }
    });
  }
}

// For browser/WebView environment
if (typeof window !== 'undefined') {
  window.BalatroGenie = BalatroGenie;
}