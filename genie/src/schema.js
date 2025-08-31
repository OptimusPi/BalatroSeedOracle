// MotelyJson Filter Schema for Balatro Seed Oracle
// This is the REAL format used by the actual filter system

export const MOTELY_JSON_SCHEMA = {
  // Required fields
  name: "string",
  description: "string", 
  author: "string",
  deck: "string", // red, blue, yellow, green, black, magic, nebula, ghost, abandoned, checkered, zodiac, painted, anaglyph, plasma, erratic
  stake: "string", // white, red, green, black, orange, gold, purple, blue
  
  // Filter arrays
  must: "array",    // Required items - ALL must be present
  should: "array",  // Scoring items - gives points when found
  mustNot: "array", // Excluded items - rejects if ANY are found
};

export const VALID_ITEM_TYPES = [
  "joker",
  "souljoker", 
  "voucher",
  "tarotcard",
  "planetcard", 
  "spectralcard",
  "playingcard",
  "smallblindtag",
  "bigblindtag",
  "boss"
];

export const VALID_DECKS = [
  "red", "blue", "yellow", "green", "black", "magic", 
  "nebula", "ghost", "abandoned", "checkered", "zodiac", 
  "painted", "anaglyph", "plasma", "erratic"
];

export const VALID_STAKES = [
  "white", "red", "green", "black", "orange", "gold", "purple", "blue"
];

// Common joker names (for AI context)
export const COMMON_JOKERS = [
  "Blueprint", "Brainstorm", "Showman", "Perkeo", "InvisibleJoker",
  "TurtleBean", "Burglar", "Hiker", "Splash", "Seltzer", 
  "Baron", "SteelJoker", "Mime", "IceCream", "Obelisk",
  "Cavendish", "GrosMichel", "Baseball", "Bull", "Rocket",
  "Egg", "Driver", "Cartomancer", "Astronomer", "Burnt",
  "Bootstraps", "Bloodstone", "LuckyCard", "Vagabond", "Riff-raff"
];

// Common vouchers
export const COMMON_VOUCHERS = [
  "Overstock", "OverstockPlus", "Clearance", "ClearanceSale",
  "Hone", "Glow", "Reroll", "RerollSurplus", "Telescope", "Observatory",
  "Grabber", "Nacho", "PaintBrush", "Palette", "Blank", "Antimatter",
  "Magic", "Illusion", "Hieroglyph", "Petroglyph", "DirectorsCut", "Retcon"
];

// Common tags
export const COMMON_TAGS = [
  "NegativeTag", "PolychromeTag", "HolographicTag", "FoilTag",
  "UncommonTag", "RareTag", "MegaTag", "JumboTag", "CouponTag",
  "FreeTag", "D6Tag", "TopUpTag", "SpeedTag", "OrbitalTag", "MeteorTag"
];

export const GAMING_CONTEXT = `
You are helping players find Balatro seeds with specific items. Balatro is a poker roguelike where:
- Jokers provide scoring effects
- Vouchers are shop upgrades
- Tags appear between rounds
- Spectral/Tarot/Planet cards are consumables
- Decks change starting conditions
- Stakes increase difficulty

Common requests:
- "Perkeo seed" = soul joker Perkeo for copying consumables
- "Negative jokers" = jokers with negative edition for +1 slot
- "Blueprint/Brainstorm" = copy jokers
- "Observatory" = voucher that gives planets
- "Negative tags" = tags that give negative jokers
`;

export const FEW_SHOT_EXAMPLES = [
  {
    input: "I want a Perkeo seed with Observatory",
    output: {
      name: "Perkeo Observatory",
      description: "Perkeo soul joker with Observatory voucher for infinite planets",
      author: "Genie",
      deck: "ghost",
      stake: "white",
      must: [
        { type: "souljoker", value: "Perkeo", antes: [1,2,3,4,5,6,7,8] },
        { type: "voucher", value: "Observatory", antes: [1,2,3,4,5,6,7,8] }
      ],
      should: [
        { type: "voucher", value: "Telescope", antes: [1,2,3,4,5,6,7], score: 5 },
        { type: "spectralcard", value: "Cryptid", antes: [1,2,3,4,5,6,7,8], score: 3 }
      ],
      mustNot: []
    }
  },
  {
    input: "Blueprint and negative jokers",
    output: {
      name: "Negative Blueprint", 
      description: "Blueprint with negative edition jokers",
      author: "Genie",
      deck: "red",
      stake: "white",
      must: [
        { type: "joker", value: "Blueprint", antes: [1,2,3,4,5,6,7,8] }
      ],
      should: [
        { type: "joker", edition: "negative", value: "Blueprint", antes: [1,2,3,4,5,6,7,8], score: 10 },
        { type: "joker", edition: "negative", value: "Brainstorm", antes: [1,2,3,4,5,6,7,8], score: 10 },
        { type: "smallblindtag", value: "NegativeTag", antes: [2,3,4,5,6,7,8], score: 5 }
      ],
      mustNot: []
    }
  },
  {
    input: "Lots of money early",
    output: {
      name: "Money Rush",
      description: "Early money generation",
      author: "Genie",
      deck: "green",
      stake: "white", 
      must: [],
      should: [
        { type: "joker", value: "Bull", antes: [1,2], score: 5 },
        { type: "joker", value: "Rocket", antes: [1,2], score: 5 },
        { type: "joker", value: "Egg", antes: [1,2,3], score: 3 },
        { type: "joker", value: "Bootstraps", antes: [1,2,3], score: 3 },
        { type: "voucher", value: "SeedMoney", antes: [1], score: 5 },
        { type: "smallblindtag", value: "CouponTag", antes: [1,2,3], score: 2 }
      ],
      mustNot: []
    }
  }
];

export function validateConfig(config) {
  const errors = [];
  
  // Check required fields
  if (!config.name) errors.push("Missing 'name' field");
  if (!config.description) errors.push("Missing 'description' field");
  if (!config.deck) errors.push("Missing 'deck' field");
  if (!config.stake) errors.push("Missing 'stake' field");
  
  // Validate deck and stake
  if (config.deck && !VALID_DECKS.includes(config.deck.toLowerCase())) {
    errors.push(`Invalid deck: ${config.deck}`);
  }
  if (config.stake && !VALID_STAKES.includes(config.stake.toLowerCase())) {
    errors.push(`Invalid stake: ${config.stake}`);
  }
  
  // Check arrays exist
  if (!Array.isArray(config.must)) errors.push("'must' should be an array");
  if (!Array.isArray(config.should)) errors.push("'should' should be an array");
  if (!Array.isArray(config.mustNot)) errors.push("'mustNot' should be an array");
  
  // Validate items in arrays
  const validateItem = (item, arrayName) => {
    if (!item.type) {
      errors.push(`Item in '${arrayName}' missing 'type' field`);
    } else if (!VALID_ITEM_TYPES.includes(item.type.toLowerCase())) {
      errors.push(`Invalid type '${item.type}' in '${arrayName}'`);
    }
    
    if (!item.value) {
      errors.push(`Item in '${arrayName}' missing 'value' field`);
    }
    
    if (!item.antes || !Array.isArray(item.antes)) {
      errors.push(`Item in '${arrayName}' missing 'antes' array`);
    }
    
    if (arrayName === 'should' && typeof item.score !== 'number') {
      errors.push(`Item in 'should' missing 'score' field`);
    }
  };
  
  if (config.must) config.must.forEach(item => validateItem(item, 'must'));
  if (config.should) config.should.forEach(item => validateItem(item, 'should'));
  if (config.mustNot) config.mustNot.forEach(item => validateItem(item, 'mustNot'));
  
  return {
    valid: errors.length === 0,
    errors
  };
}