#include <ctype.h>
#include <string.h>
#ifndef __HOST_ITEMS_H_
#define __HOST_ITEMS_H_

// Contains every kind of thing you could search for!

typedef enum Item {
    RETRY = 0,

    //Jokers
    J_BEGIN = 1,

    J_C_BEGIN = 2,
    Joker = 3,
    Greedy_Joker = 4,
    Lusty_Joker = 5,
    Wrathful_Joker = 6,
    Gluttonous_Joker = 7,
    Jolly_Joker = 8,
    Zany_Joker = 9,
    Mad_Joker = 10,
    Crazy_Joker = 11,
    Droll_Joker = 12,
    Sly_Joker = 13,
    Wily_Joker = 14,
    Clever_Joker = 15,
    Devious_Joker = 16,
    Crafty_Joker = 17,
    Half_Joker = 18,
    Credit_Card = 19,
    Banner = 20,
    Mystic_Summit = 21,
    _8_Ball = 22,
    Misprint = 23,
    Raised_Fist = 24,
    Chaos_the_Clown = 25,
    Scary_Face = 26,
    Abstract_Joker = 27,
    Delayed_Gratification = 28,
    Gros_Michel = 29,
    Even_Steven = 30,
    Odd_Todd = 31,
    Scholar = 32,
    Business_Card = 33,
    Supernova = 34,
    Ride_the_Bus = 35,
    Egg = 36,
    Runner = 37,
    Ice_Cream = 38,
    Splash = 39,
    Blue_Joker = 40,
    Faceless_Joker = 41,
    Green_Joker = 42,
    Superposition = 43,
    To_Do_List = 44,
    Cavendish = 45,
    Red_Card = 46,
    Square_Joker = 47,
    Riff_raff = 48,
    Photograph = 49,
    Reserved_Parking = 50,
    Mail_In_Rebate = 51,
    Hallucination = 52,
    Fortune_Teller = 53,
    Juggler = 54,
    Drunkard = 55,
    Golden_Joker = 56,
    Popcorn = 57,
    Walkie_Talkie = 58,
    Smiley_Face = 59,
    Golden_Ticket = 60,
    Swashbuckler = 61,
    Hanging_Chad = 62,
    Shoot_the_Moon = 63,
    J_C_END = 64,

    J_U_BEGIN = 65,
    Joker_Stencil = 66,
    Four_Fingers = 67,
    Mime = 68,
    Ceremonial_Dagger = 69,
    Marble_Joker = 70,
    Loyalty_Card = 71,
    Dusk = 72,
    Fibonacci = 73,
    Steel_Joker = 74,
    Hack = 75,
    Pareidolia = 76,
    Space_Joker = 77,
    Burglar = 78,
    Blackboard = 79,
    Sixth_Sense = 80,
    Constellation = 81,
    Hiker = 82,
    Card_Sharp = 83,
    Madness = 84,
    Seance = 85,
    Shortcut = 86,
    Hologram = 87,
    Cloud_9 = 88,
    Rocket = 89,
    Midas_Mask = 90,
    Luchador = 91,
    Gift_Card = 92,
    Turtle_Bean = 93,
    Erosion = 94,
    To_the_Moon = 95,
    Stone_Joker = 96,
    Lucky_Cat = 97,
    Bull = 98,
    Diet_Cola = 99,
    Trading_Card = 100,
    Flash_Card = 101,
    Spare_Trousers = 102,
    Ramen = 103,
    Seltzer = 104,
    Castle = 105,
    Mr_Bones = 106,
    Acrobat = 107,
    Sock_and_Buskin = 108,
    Troubadour = 109,
    Certificate = 110,
    Smeared_Joker = 111,
    Throwback = 112,
    Rough_Gem = 113,
    Bloodstone = 114,
    Arrowhead = 115,
    Onyx_Agate = 116,
    Glass_Joker = 117,
    Showman = 118,
    Flower_Pot = 119,
    Merry_Andy = 120,
    Oops_All_6s = 121,
    The_Idol = 122,
    Seeing_Double = 123,
    Matador = 124,
    Stuntman = 125,
    Satellite = 126,
    Cartomancer = 127,
    Astronomer = 128,
    Bootstraps = 129,
    J_U_END = 130,

    J_R_BEGIN = 131,
    DNA = 132,
    Vampire = 133,
    Vagabond = 134,
    Baron = 135,
    Obelisk = 136,
    Baseball_Card = 137,
    Ancient_Joker = 138,
    Campfire = 139,
    Blueprint = 140,
    Wee_Joker = 141,
    Hit_the_Road = 142,
    The_Duo = 143,
    The_Trio = 144,
    The_Family = 145,
    The_Order = 146,
    The_Tribe = 147,
    Invisible_Joker = 148,
    Brainstorm = 149,
    Drivers_License = 150,
    Burnt_Joker = 151,
    J_R_END = 152,

    J_L_BEGIN = 153,
    Canio = 154,
    Triboulet = 155,
    Yorick = 156,
    Chicot = 157,
    Perkeo = 158,
    J_L_END = 159,

    J_END = 160,

    // Vouchers
    V_BEGIN = 161,
    Overstock = 162,
    Overstock_Plus = 163,
    Clearance_Sale = 164,
    Liquidation = 165,
    Hone = 166,
    Glow_Up = 167,
    Reroll_Surplus = 168,
    Reroll_Glut = 169,
    Crystal_Ball = 170,
    Omen_Globe = 171,
    Telescope = 172,
    Observatory = 173,
    Grabber = 174,
    Nacho_Tong = 175,
    Wasteful = 176,
    Recyclomancy = 177,
    Tarot_Merchant = 178,
    Tarot_Tycoon = 179,
    Planet_Merchant = 180,
    Planet_Tycoon = 181,
    Seed_Money = 182,
    Money_Tree = 183,
    Blank = 184,
    Antimatter = 185,
    Magic_Trick = 186,
    Illusion = 187,
    Hieroglyph = 188,
    Petroglyph = 189,
    Directors_Cut = 190,
    Retcon = 191,
    Paint_Brush = 192,
    Palette = 193,
    V_END = 194,

    // Tarots
    T_BEGIN = 195,
    The_Fool = 196,
    The_Magician = 197,
    The_High_Priestess = 198,
    The_Empress = 199,
    The_Emperor = 200,
    The_Hierophant = 201,
    The_Lovers = 202,
    The_Chariot = 203,
    Justice = 204,
    The_Hermit = 205,
    The_Wheel_of_Fortune = 206,
    Strength = 207,
    The_Hanged_Man = 208,
    Death = 209,
    Temperance = 210,
    The_Devil = 211,
    The_Tower = 212,
    The_Star = 213,
    The_Moon = 214,
    The_Sun = 215,
    Judgement = 216,
    The_World = 217,
    T_END = 218,

    // Planets
    P_BEGIN = 219,
    Mercury = 220,
    Venus = 221,
    Earth = 222,
    Mars = 223,
    Jupiter = 224,
    Saturn = 225,
    Uranus = 226,
    Neptune = 227,
    Pluto = 228,
    Planet_X = 229,
    Ceres = 230,
    Eris = 231,
    P_END = 232,

    // Hands
    H_BEGIN = 233,
    Pair = 234,
    Three_of_a_Kind = 235,
    Full_House = 236,
    Four_of_a_Kind = 237,
    Flush = 238,
    Straight = 239,
    Two_Pair = 240,
    Straight_Flush = 241,
    High_Card = 242,
    Five_of_a_Kind = 243,
    Flush_House = 244,
    Flush_Five = 245,
    H_END = 246,

    // Spectrals
    S_BEGIN = 247,
    Familiar = 248,
    Grim = 249,
    Incantation = 250,
    Talisman = 251,
    Aura = 252,
    Wraith = 253,
    Sigil = 254,
    Ouija = 255,
    Ectoplasm = 256,
    Immolate = 257,
    Ankh = 258,
    Deja_Vu = 259,
    Hex = 260,
    Trance = 261,
    Medium = 262,
    Cryptid = 263,
    The_Soul = 264,
    Black_Hole = 265,
    S_END = 266,

    // Enhancements
    ENHANCEMENT_BEGIN = 267,
    No_Enhancement = 268,
    Bonus_Card = 269,
    Mult_Card = 270,
    Wild_Card = 271,
    Glass_Card = 272,
    Steel_Card = 273,
    Stone_Card = 274,
    Gold_Card = 275,
    Lucky_Card = 276,
    ENHANCEMENT_END = 277,

    // Seals
    SEAL_BEGIN = 278,
    No_Seal = 279,
    Gold_Seal = 280,
    Red_Seal = 281,
    Blue_Seal = 282,
    Purple_Seal = 283,
    SEAL_END = 284,

    // Editions
    E_BEGIN = 285,
    No_Edition = 286,
    Foil = 287,
    Holographic = 288,
    Polychrome = 289,
    Negative = 290,
    E_END = 291,

    // Booster Packs
    PACK_BEGIN = 292,
    Arcana_Pack = 293,
    Jumbo_Arcana_Pack = 294,
    Mega_Arcana_Pack = 295,
    Celestial_Pack = 296,
    Jumbo_Celestial_Pack = 297,
    Mega_Celestial_Pack = 298,
    Standard_Pack = 299,
    Jumbo_Standard_Pack = 300,
    Mega_Standard_Pack = 301,
    Buffoon_Pack = 302,
    Jumbo_Buffoon_Pack = 303,
    Mega_Buffoon_Pack = 304,
    Spectral_Pack = 305,
    Jumbo_Spectral_Pack = 306,
    Mega_Spectral_Pack = 307,
    PACK_END = 308,

    // Tags
    TAG_BEGIN = 309,
    Uncommon_Tag = 310,
    Rare_Tag = 311,
    Negative_Tag = 312,
    Foil_Tag = 313,
    Holographic_Tag = 314,
    Polychrome_Tag = 315,
    Investment_Tag = 316,
    Voucher_Tag = 317,
    Boss_Tag = 318,
    Standard_Tag = 319,
    Charm_Tag = 320,
    Meteor_Tag = 321,
    Buffoon_Tag = 322,
    Handy_Tag = 323,
    Garbage_Tag = 324,
    Ethereal_Tag = 325,
    Coupon_Tag = 326,
    Double_Tag = 327,
    Juggle_Tag = 328,
    D6_Tag = 329,
    Top_up_Tag = 330,
    Speed_Tag = 331,
    Orbital_Tag = 332,
    Economy_Tag = 333,
    TAG_END = 334,

    // Blinds
    B_BEGIN = 335,
    Small_Blind = 336,
    Big_Blind = 337,
    The_Hook = 338,
    The_Ox = 339,
    The_House = 340,
    The_Wall = 341,
    The_Wheel = 342,
    The_Arm = 343,
    The_Club = 344,
    The_Fish = 345,
    The_Psychic = 346,
    The_Goad = 347,
    The_Water = 348,
    The_Window = 349,
    The_Manacle = 350,
    The_Eye = 351,
    The_Mouth = 352,
    The_Plant = 353,
    The_Serpent = 354,
    The_Pillar = 355,
    The_Needle = 356,
    The_Head = 357,
    The_Tooth = 358,
    The_Flint = 359,
    The_Mark = 360,
    B_F_BEGIN = 361,
    Amber_Acorn = 362,
    Verdant_Leaf = 363,
    Violet_Vessel = 364,
    Crimson_Heart = 365,
    Cerulean_Bell = 366,
    B_F_END = 367,
    B_END = 368,

    // Suits
    SUIT_BEGIN = 369,
    Hearts = 370,
    Clubs = 371,
    Diamonds = 372,
    Spades = 373,
    SUIT_END = 374,

    // Ranks
    RANK_BEGIN = 375,
    _2 = 376,
    _3 = 377,
    _4 = 378,
    _5 = 379,
    _6 = 380,
    _7 = 381,
    _8 = 382,
    _9 = 383,
    _10 = 384,
    Jack = 385,
    Queen = 386,
    King = 387,
    Ace = 388,
    RANK_END = 389,

    // Cards
    C_BEGIN = 390,
    C_2 = 391,
    C_3 = 392,
    C_4 = 393,
    C_5 = 394,
    C_6 = 395,
    C_7 = 396,
    C_8 = 397,
    C_9 = 398,
    C_A = 399,
    C_J = 400,
    C_K = 401,
    C_Q = 402,
    C_T = 403,
    D_2 = 404,
    D_3 = 405,
    D_4 = 406,
    D_5 = 407,
    D_6 = 408,
    D_7 = 409,
    D_8 = 410,
    D_9 = 411,
    D_A = 412,
    D_J = 413,
    D_K = 414,
    D_Q = 415,
    D_T = 416,
    H_2 = 417,
    H_3 = 418,
    H_4 = 419,
    H_5 = 420,
    H_6 = 421,
    H_7 = 422,
    H_8 = 423,
    H_9 = 424,
    H_A = 425,
    H_J = 426,
    H_K = 427,
    H_Q = 428,
    H_T = 429,
    S_2 = 430,
    S_3 = 431,
    S_4 = 432,
    S_5 = 433,
    S_6 = 434,
    S_7 = 435,
    S_8 = 436,
    S_9 = 437,
    S_A = 438,
    S_J = 439,
    S_K = 440,
    S_Q = 441,
    S_T = 442,
    C_END = 443,

    // Decks
    D_BEGIN = 444,
    Red_Deck = 445,
    Blue_Deck = 446,
    Yellow_Deck = 447,
    Green_Deck = 448,
    Black_Deck = 449,
    Magic_Deck = 450,
    Nebula_Deck = 451,
    Ghost_Deck = 452,
    Abandoned_Deck = 453,
    Checkered_Deck = 454,
    Zodiac_Deck = 455,
    Painted_Deck = 456,
    Anaglyph_Deck = 457,
    Plasma_Deck = 458,
    Erratic_Deck = 459,
    Challenge_Deck = 460,
    D_END = 461,

    // Challenges
    CHAL_BEGIN = 462,
    The_Omelette = 463,
    _15_Minute_City = 464,
    Rich_get_Richer = 465,
    On_a_Knifes_Edge = 466,
    X_ray_Vision = 467,
    Mad_World = 468,
    Luxury_Tax = 469,
    Non_Perishable = 470,
    Medusa = 471,
    Double_or_Nothing = 472,
    Typecast = 473,
    Inflation = 474,
    Bram_Poker = 475,
    Fragile = 476,
    Monolith = 477,
    Blast_Off = 478,
    Five_Card_Draw = 479,
    Golden_Needle = 480,
    Cruelty = 481,
    Jokerless = 482,
    CHAL_END = 483,

    //Stakes
    STAKE_BEGIN = 484,
    White_Stake = 485,
    Red_Stake = 486,
    Green_Stake = 487,
    Black_Stake = 488,
    Blue_Stake = 489,
    Purple_Stake = 490,
    Orange_Stake = 491,
    Gold_Stake = 492,
    STAKE_END = 493,

    ITEMS_END = 494
} item;


item parse_item(const char* name) {
    // Nothing - RETRY marker for 0 value
    if (strcmp(name, "RETRY") == 0) return RETRY;

    // Jokers
    if (strcmp(name, "J_BEGIN") == 0) return J_BEGIN;

    // Uncommon Jokers
    if (strcmp(name, "J_C_BEGIN") == 0) return J_C_BEGIN;
    if (strcmp(name, "Joker") == 0) return Joker;
    if (strcmp(name, "Greedy_Joker") == 0) return Greedy_Joker;
    if (strcmp(name, "Lusty_Joker") == 0) return Lusty_Joker;
    if (strcmp(name, "Wrathful_Joker") == 0) return Wrathful_Joker;
    if (strcmp(name, "Gluttonous_Joker") == 0) return Gluttonous_Joker;
    if (strcmp(name, "Jolly_Joker") == 0) return Jolly_Joker;
    if (strcmp(name, "Zany_Joker") == 0) return Zany_Joker;
    if (strcmp(name, "Mad_Joker") == 0) return Mad_Joker;
    if (strcmp(name, "Crazy_Joker") == 0) return Crazy_Joker;
    if (strcmp(name, "Droll_Joker") == 0) return Droll_Joker;
    if (strcmp(name, "Sly_Joker") == 0) return Sly_Joker;
    if (strcmp(name, "Wily_Joker") == 0) return Wily_Joker;
    if (strcmp(name, "Clever_Joker") == 0) return Clever_Joker;
    if (strcmp(name, "Devious_Joker") == 0) return Devious_Joker;
    if (strcmp(name, "Crafty_Joker") == 0) return Crafty_Joker;
    if (strcmp(name, "Half_Joker") == 0) return Half_Joker;
    if (strcmp(name, "Credit_Card") == 0) return Credit_Card;
    if (strcmp(name, "Banner") == 0) return Banner;
    if (strcmp(name, "Mystic_Summit") == 0) return Mystic_Summit;
    if (strcmp(name, "_8_Ball") == 0) return _8_Ball;
    if (strcmp(name, "Misprint") == 0) return Misprint;
    if (strcmp(name, "Raised_Fist") == 0) return Raised_Fist;
    if (strcmp(name, "Chaos_the_Clown") == 0) return Chaos_the_Clown;
    if (strcmp(name, "Scary_Face") == 0) return Scary_Face;
    if (strcmp(name, "Abstract_Joker") == 0) return Abstract_Joker;
    if (strcmp(name, "Delayed_Gratification") == 0) return Delayed_Gratification;
    if (strcmp(name, "Gros_Michel") == 0) return Gros_Michel;
    if (strcmp(name, "Even_Steven") == 0) return Even_Steven;
    if (strcmp(name, "Odd_Todd") == 0) return Odd_Todd;
    if (strcmp(name, "Scholar") == 0) return Scholar;
    if (strcmp(name, "Business_Card") == 0) return Business_Card;
    if (strcmp(name, "Supernova") == 0) return Supernova;
    if (strcmp(name, "Ride_the_Bus") == 0) return Ride_the_Bus;
    if (strcmp(name, "Egg") == 0) return Egg;
    if (strcmp(name, "Runner") == 0) return Runner;
    if (strcmp(name, "Ice_Cream") == 0) return Ice_Cream;
    if (strcmp(name, "Splash") == 0) return Splash;
    if (strcmp(name, "Blue_Joker") == 0) return Blue_Joker;
    if (strcmp(name, "Faceless_Joker") == 0) return Faceless_Joker;
    if (strcmp(name, "Green_Joker") == 0) return Green_Joker;
    if (strcmp(name, "Superposition") == 0) return Superposition;
    if (strcmp(name, "To_Do_List") == 0) return To_Do_List;
    if (strcmp(name, "Cavendish") == 0) return Cavendish;
    if (strcmp(name, "Red_Card") == 0) return Red_Card;
    if (strcmp(name, "Square_Joker") == 0) return Square_Joker;
    if (strcmp(name, "Riff_raff") == 0) return Riff_raff;
    if (strcmp(name, "Photograph") == 0) return Photograph;
    if (strcmp(name, "Reserved_Parking") == 0) return Reserved_Parking;
    if (strcmp(name, "Mail_In_Rebate") == 0) return Mail_In_Rebate;
    if (strcmp(name, "Hallucination") == 0) return Hallucination;
    if (strcmp(name, "Fortune_Teller") == 0) return Fortune_Teller;
    if (strcmp(name, "Juggler") == 0) return Juggler;
    if (strcmp(name, "Drunkard") == 0) return Drunkard;
    if (strcmp(name, "Golden_Joker") == 0) return Golden_Joker;
    if (strcmp(name, "Popcorn") == 0) return Popcorn;
    if (strcmp(name, "Walkie_Talkie") == 0) return Walkie_Talkie;
    if (strcmp(name, "Smiley_Face") == 0) return Smiley_Face;
    if (strcmp(name, "Golden_Ticket") == 0) return Golden_Ticket;
    if (strcmp(name, "Swashbuckler") == 0) return Swashbuckler;
    if (strcmp(name, "Hanging_Chad") == 0) return Hanging_Chad;
    if (strcmp(name, "Shoot_the_Moon") == 0) return Shoot_the_Moon;
    if (strcmp(name, "J_C_END") == 0) return J_C_END;

    // Uncommon jokers
    if (strcmp(name, "J_U_BEGIN") == 0) return J_U_BEGIN;
    if (strcmp(name, "Joker_Stencil") == 0) return Joker_Stencil;
    if (strcmp(name, "Four_Fingers") == 0) return Four_Fingers;
    if (strcmp(name, "Mime") == 0) return Mime;
    if (strcmp(name, "Ceremonial_Dagger") == 0) return Ceremonial_Dagger;
    if (strcmp(name, "Marble_Joker") == 0) return Marble_Joker;
    if (strcmp(name, "Loyalty_Card") == 0) return Loyalty_Card;
    if (strcmp(name, "Dusk") == 0) return Dusk;
    if (strcmp(name, "Fibonacci") == 0) return Fibonacci;
    if (strcmp(name, "Steel_Joker") == 0) return Steel_Joker;
    if (strcmp(name, "Hack") == 0) return Hack;
    if (strcmp(name, "Pareidolia") == 0) return Pareidolia;
    if (strcmp(name, "Space_Joker") == 0) return Space_Joker;
    if (strcmp(name, "Burglar") == 0) return Burglar;
    if (strcmp(name, "Blackboard") == 0) return Blackboard;
    if (strcmp(name, "Sixth_Sense") == 0) return Sixth_Sense;
    if (strcmp(name, "Constellation") == 0) return Constellation;
    if (strcmp(name, "Hiker") == 0) return Hiker;
    if (strcmp(name, "Card_Sharp") == 0) return Card_Sharp;
    if (strcmp(name, "Madness") == 0) return Madness;
    if (strcmp(name, "Seance") == 0) return Seance;
    if (strcmp(name, "Shortcut") == 0) return Shortcut;
    if (strcmp(name, "Hologram") == 0) return Hologram;
    if (strcmp(name, "Cloud_9") == 0) return Cloud_9;
    if (strcmp(name, "Rocket") == 0) return Rocket;
    if (strcmp(name, "Midas_Mask") == 0) return Midas_Mask;
    if (strcmp(name, "Luchador") == 0) return Luchador;
    if (strcmp(name, "Gift_Card") == 0) return Gift_Card;
    if (strcmp(name, "Turtle_Bean") == 0) return Turtle_Bean;
    if (strcmp(name, "Erosion") == 0) return Erosion;
    if (strcmp(name, "To_the_Moon") == 0) return To_the_Moon;
    if (strcmp(name, "Stone_Joker") == 0) return Stone_Joker;
    if (strcmp(name, "Lucky_Cat") == 0) return Lucky_Cat;
    if (strcmp(name, "Bull") == 0) return Bull;
    if (strcmp(name, "Diet_Cola") == 0) return Diet_Cola;
    if (strcmp(name, "Trading_Card") == 0) return Trading_Card;
    if (strcmp(name, "Flash_Card") == 0) return Flash_Card;
    if (strcmp(name, "Spare_Trousers") == 0) return Spare_Trousers;
    if (strcmp(name, "Ramen") == 0) return Ramen;
    if (strcmp(name, "Seltzer") == 0) return Seltzer;
    if (strcmp(name, "Castle") == 0) return Castle;
    if (strcmp(name, "Mr_Bones") == 0) return Mr_Bones;
    if (strcmp(name, "Acrobat") == 0) return Acrobat;
    if (strcmp(name, "Sock_and_Buskin") == 0) return Sock_and_Buskin;
    if (strcmp(name, "Troubadour") == 0) return Troubadour;
    if (strcmp(name, "Certificate") == 0) return Certificate;
    if (strcmp(name, "Smeared_Joker") == 0) return Smeared_Joker;
    if (strcmp(name, "Throwback") == 0) return Throwback;
    if (strcmp(name, "Rough_Gem") == 0) return Rough_Gem;
    if (strcmp(name, "Bloodstone") == 0) return Bloodstone;
    if (strcmp(name, "Arrowhead") == 0) return Arrowhead;
    if (strcmp(name, "Onyx_Agate") == 0) return Onyx_Agate;
    if (strcmp(name, "Glass_Joker") == 0) return Glass_Joker;
    if (strcmp(name, "Showman") == 0) return Showman;
    if (strcmp(name, "Flower_Pot") == 0) return Flower_Pot;
    if (strcmp(name, "Merry_Andy") == 0) return Merry_Andy;
    if (strcmp(name, "Oops_All_6s") == 0) return Oops_All_6s;
    if (strcmp(name, "The_Idol") == 0) return The_Idol;
    if (strcmp(name, "Seeing_Double") == 0) return Seeing_Double;
    if (strcmp(name, "Matador") == 0) return Matador;
    if (strcmp(name, "Stuntman") == 0) return Stuntman;
    if (strcmp(name, "Satellite") == 0) return Satellite;
    if (strcmp(name, "Cartomancer") == 0) return Cartomancer;
    if (strcmp(name, "Astronomer") == 0) return Astronomer;
    if (strcmp(name, "Bootstraps") == 0) return Bootstraps;
    if (strcmp(name, "J_U_END") == 0) return J_U_END;

    // Rare Jokers
    if (strcmp(name, "J_R_BEGIN") == 0) return J_R_BEGIN;
    if (strcmp(name, "DNA") == 0) return DNA;
    if (strcmp(name, "Vampire") == 0) return Vampire;
    if (strcmp(name, "Vagabond") == 0) return Vagabond;
    if (strcmp(name, "Baron") == 0) return Baron;
    if (strcmp(name, "Obelisk") == 0) return Obelisk;
    if (strcmp(name, "Baseball_Card") == 0) return Baseball_Card;
    if (strcmp(name, "Ancient_Joker") == 0) return Ancient_Joker;
    if (strcmp(name, "Campfire") == 0) return Campfire;
    if (strcmp(name, "Blueprint") == 0) return Blueprint;
    if (strcmp(name, "Wee_Joker") == 0) return Wee_Joker;
    if (strcmp(name, "Hit_the_Road") == 0) return Hit_the_Road;
    if (strcmp(name, "The_Duo") == 0) return The_Duo;
    if (strcmp(name, "The_Trio") == 0) return The_Trio;
    if (strcmp(name, "The_Family") == 0) return The_Family;
    if (strcmp(name, "The_Order") == 0) return The_Order;
    if (strcmp(name, "The_Tribe") == 0) return The_Tribe;
    if (strcmp(name, "Invisible_Joker") == 0) return Invisible_Joker;
    if (strcmp(name, "Brainstorm") == 0) return Brainstorm;
    if (strcmp(name, "Drivers_License") == 0) return Drivers_License;
    if (strcmp(name, "Burnt_Joker") == 0) return Burnt_Joker;
    if (strcmp(name, "J_R_END") == 0) return J_R_END;

    // Legendary Jokers
    if (strcmp(name, "J_L_BEGIN") == 0) return J_L_BEGIN;
    if (strcmp(name, "Canio") == 0) return Canio;
    if (strcmp(name, "Triboulet") == 0) return Triboulet;
    if (strcmp(name, "Yorick") == 0) return Yorick;
    if (strcmp(name, "Chicot") == 0) return Chicot;
    if (strcmp(name, "Perkeo") == 0) return Perkeo;
    if (strcmp(name, "J_L_END") == 0) return J_L_END;
    if (strcmp(name, "J_END") == 0) return J_END;

    // Vouchers
    if (strcmp(name, "V_BEGIN") == 0) return V_BEGIN;
    if (strcmp(name, "Overstock") == 0) return Overstock;
    if (strcmp(name, "Overstock_Plus") == 0) return Overstock_Plus;
    if (strcmp(name, "Clearance_Sale") == 0) return Clearance_Sale;
    if (strcmp(name, "Liquidation") == 0) return Liquidation;
    if (strcmp(name, "Hone") == 0) return Hone;
    if (strcmp(name, "Glow_Up") == 0) return Glow_Up;
    if (strcmp(name, "Reroll_Surplus") == 0) return Reroll_Surplus;
    if (strcmp(name, "Reroll_Glut") == 0) return Reroll_Glut;
    if (strcmp(name, "Crystal_Ball") == 0) return Crystal_Ball;
    if (strcmp(name, "Omen_Globe") == 0) return Omen_Globe;
    if (strcmp(name, "Telescope") == 0) return Telescope;
    if (strcmp(name, "Observatory") == 0) return Observatory;
    if (strcmp(name, "Grabber") == 0) return Grabber;
    if (strcmp(name, "Nacho_Tong") == 0) return Nacho_Tong;
    if (strcmp(name, "Wasteful") == 0) return Wasteful;
    if (strcmp(name, "Recyclomancy") == 0) return Recyclomancy;
    if (strcmp(name, "Tarot_Merchant") == 0) return Tarot_Merchant;
    if (strcmp(name, "Tarot_Tycoon") == 0) return Tarot_Tycoon;
    if (strcmp(name, "Planet_Merchant") == 0) return Planet_Merchant;
    if (strcmp(name, "Planet_Tycoon") == 0) return Planet_Tycoon;
    if (strcmp(name, "Seed_Money") == 0) return Seed_Money;
    if (strcmp(name, "Money_Tree") == 0) return Money_Tree;
    if (strcmp(name, "Blank") == 0) return Blank;
    if (strcmp(name, "Antimatter") == 0) return Antimatter;
    if (strcmp(name, "Magic_Trick") == 0) return Magic_Trick;
    if (strcmp(name, "Illusion") == 0) return Illusion;
    if (strcmp(name, "Hieroglyph") == 0) return Hieroglyph;
    if (strcmp(name, "Petroglyph") == 0) return Petroglyph;
    if (strcmp(name, "Directors_Cut") == 0) return Directors_Cut;
    if (strcmp(name, "Retcon") == 0) return Retcon;
    if (strcmp(name, "Paint_Brush") == 0) return Paint_Brush;
    if (strcmp(name, "Palette") == 0) return Palette;
    if (strcmp(name, "V_END") == 0) return V_END;

    // Tarots
    if (strcmp(name, "T_BEGIN") == 0) return T_BEGIN;
    if (strcmp(name, "The_Fool") == 0) return The_Fool;
    if (strcmp(name, "The_Magician") == 0) return The_Magician;
    if (strcmp(name, "The_High_Priestess") == 0) return The_High_Priestess;
    if (strcmp(name, "The_Empress") == 0) return The_Empress;
    if (strcmp(name, "The_Emperor") == 0) return The_Emperor;
    if (strcmp(name, "The_Hierophant") == 0) return The_Hierophant;
    if (strcmp(name, "The_Lovers") == 0) return The_Lovers;
    if (strcmp(name, "The_Chariot") == 0) return The_Chariot;
    if (strcmp(name, "Justice") == 0) return Justice;
    if (strcmp(name, "The_Hermit") == 0) return The_Hermit;
    if (strcmp(name, "The_Wheel_of_Fortune") == 0) return The_Wheel_of_Fortune;
    if (strcmp(name, "Strength") == 0) return Strength;
    if (strcmp(name, "The_Hanged_Man") == 0) return The_Hanged_Man;
    if (strcmp(name, "Death") == 0) return Death;
    if (strcmp(name, "Temperance") == 0) return Temperance;
    if (strcmp(name, "The_Devil") == 0) return The_Devil;
    if (strcmp(name, "The_Tower") == 0) return The_Tower;
    if (strcmp(name, "The_Star") == 0) return The_Star;
    if (strcmp(name, "The_Moon") == 0) return The_Moon;
    if (strcmp(name, "The_Sun") == 0) return The_Sun;
    if (strcmp(name, "Judgement") == 0) return Judgement;
    if (strcmp(name, "The_World") == 0) return The_World;
    if (strcmp(name, "T_END") == 0) return T_END;

    // Planets
    if (strcmp(name, "P_BEGIN") == 0) return P_BEGIN;
    if (strcmp(name, "Mercury") == 0) return Mercury;
    if (strcmp(name, "Venus") == 0) return Venus;
    if (strcmp(name, "Earth") == 0) return Earth;
    if (strcmp(name, "Mars") == 0) return Mars;
    if (strcmp(name, "Jupiter") == 0) return Jupiter;
    if (strcmp(name, "Saturn") == 0) return Saturn;
    if (strcmp(name, "Uranus") == 0) return Uranus;
    if (strcmp(name, "Neptune") == 0) return Neptune;
    if (strcmp(name, "Pluto") == 0) return Pluto;
    if (strcmp(name, "Planet_X") == 0) return Planet_X;
    if (strcmp(name, "Ceres") == 0) return Ceres;
    if (strcmp(name, "Eris") == 0) return Eris;
    if (strcmp(name, "P_END") == 0) return P_END;

    // Hands
    if (strcmp(name, "H_BEGIN") == 0) return H_BEGIN;
    if (strcmp(name, "Pair") == 0) return Pair;
    if (strcmp(name, "Three_of_a_Kind") == 0) return Three_of_a_Kind;
    if (strcmp(name, "Full_House") == 0) return Full_House;
    if (strcmp(name, "Four_of_a_Kind") == 0) return Four_of_a_Kind;
    if (strcmp(name, "Flush") == 0) return Flush;
    if (strcmp(name, "Straight") == 0) return Straight;
    if (strcmp(name, "Two_Pair") == 0) return Two_Pair;
    if (strcmp(name, "Straight_Flush") == 0) return Straight_Flush;
    if (strcmp(name, "High_Card") == 0) return High_Card;
    if (strcmp(name, "Five_of_a_Kind") == 0) return Five_of_a_Kind;
    if (strcmp(name, "Flush_House") == 0) return Flush_House;
    if (strcmp(name, "Flush_Five") == 0) return Flush_Five;
    if (strcmp(name, "H_END") == 0) return H_END;

    // Spectrals
    if (strcmp(name, "S_BEGIN") == 0) return S_BEGIN;
    if (strcmp(name, "Familiar") == 0) return Familiar;
    if (strcmp(name, "Grim") == 0) return Grim;
    if (strcmp(name, "Incantation") == 0) return Incantation;
    if (strcmp(name, "Talisman") == 0) return Talisman;
    if (strcmp(name, "Aura") == 0) return Aura;
    if (strcmp(name, "Wraith") == 0) return Wraith;
    if (strcmp(name, "Sigil") == 0) return Sigil;
    if (strcmp(name, "Ouija") == 0) return Ouija;
    if (strcmp(name, "Ectoplasm") == 0) return Ectoplasm;
    if (strcmp(name, "Immolate") == 0) return Immolate;
    if (strcmp(name, "Ankh") == 0) return Ankh;
    if (strcmp(name, "Deja_Vu") == 0) return Deja_Vu;
    if (strcmp(name, "Hex") == 0) return Hex;
    if (strcmp(name, "Trance") == 0) return Trance;
    if (strcmp(name, "Medium") == 0) return Medium;
    if (strcmp(name, "Cryptid") == 0) return Cryptid;
    if (strcmp(name, "The_Soul") == 0) return The_Soul;
    if (strcmp(name, "Black_Hole") == 0) return Black_Hole;
    if (strcmp(name, "S_END") == 0) return S_END;

    // Enhancements
    if (strcmp(name, "ENHANCEMENT_BEGIN") == 0) return ENHANCEMENT_BEGIN;
    if (strcmp(name, "No_Enhancement") == 0) return No_Enhancement;
    if (strcmp(name, "Bonus_Card") == 0) return Bonus_Card;
    if (strcmp(name, "Mult_Card") == 0) return Mult_Card;
    if (strcmp(name, "Wild_Card") == 0) return Wild_Card;
    if (strcmp(name, "Glass_Card") == 0) return Glass_Card;
    if (strcmp(name, "Steel_Card") == 0) return Steel_Card;
    if (strcmp(name, "Stone_Card") == 0) return Stone_Card;
    if (strcmp(name, "Gold_Card") == 0) return Gold_Card;
    if (strcmp(name, "Lucky_Card") == 0) return Lucky_Card;
    if (strcmp(name, "ENHANCEMENT_END") == 0) return ENHANCEMENT_END;

    // Seals
    if (strcmp(name, "SEAL_BEGIN") == 0) return SEAL_BEGIN;
    if (strcmp(name, "No_Seal") == 0) return No_Seal;
    if (strcmp(name, "Gold_Seal") == 0) return Gold_Seal;
    if (strcmp(name, "Red_Seal") == 0) return Red_Seal;
    if (strcmp(name, "Blue_Seal") == 0) return Blue_Seal;
    if (strcmp(name, "Purple_Seal") == 0) return Purple_Seal;
    if (strcmp(name, "SEAL_END") == 0) return SEAL_END;

    // Editions
    if (strcmp(name, "E_BEGIN") == 0) return E_BEGIN;
    if (strcmp(name, "No_Edition") == 0) return No_Edition;
    if (strcmp(name, "Foil") == 0) return Foil;
    if (strcmp(name, "Holographic") == 0) return Holographic;
    if (strcmp(name, "Polychrome") == 0) return Polychrome;
    if (strcmp(name, "Negative") == 0) return Negative;
    if (strcmp(name, "E_END") == 0) return E_END;

    // Booster Packs
    if (strcmp(name, "PACK_BEGIN") == 0) return PACK_BEGIN;
    if (strcmp(name, "Arcana_Pack") == 0) return Arcana_Pack;
    if (strcmp(name, "Jumbo_Arcana_Pack") == 0) return Jumbo_Arcana_Pack;
    if (strcmp(name, "Mega_Arcana_Pack") == 0) return Mega_Arcana_Pack;
    if (strcmp(name, "Celestial_Pack") == 0) return Celestial_Pack;
    if (strcmp(name, "Jumbo_Celestial_Pack") == 0) return Jumbo_Celestial_Pack;
    if (strcmp(name, "Mega_Celestial_Pack") == 0) return Mega_Celestial_Pack;
    if (strcmp(name, "Standard_Pack") == 0) return Standard_Pack;
    if (strcmp(name, "Jumbo_Standard_Pack") == 0) return Jumbo_Standard_Pack;
    if (strcmp(name, "Mega_Standard_Pack") == 0) return Mega_Standard_Pack;
    if (strcmp(name, "Buffoon_Pack") == 0) return Buffoon_Pack;
    if (strcmp(name, "Jumbo_Buffoon_Pack") == 0) return Jumbo_Buffoon_Pack;
    if (strcmp(name, "Mega_Buffoon_Pack") == 0) return Mega_Buffoon_Pack;
    if (strcmp(name, "Spectral_Pack") == 0) return Spectral_Pack;
    if (strcmp(name, "Jumbo_Spectral_Pack") == 0) return Jumbo_Spectral_Pack;
    if (strcmp(name, "Mega_Spectral_Pack") == 0) return Mega_Spectral_Pack;
    if (strcmp(name, "PACK_END") == 0) return PACK_END;

    // Tags
    if (strcmp(name, "TAG_BEGIN") == 0) return TAG_BEGIN;
    if (strcmp(name, "Uncommon_Tag") == 0) return Uncommon_Tag;
    if (strcmp(name, "Rare_Tag") == 0) return Rare_Tag;
    if (strcmp(name, "Negative_Tag") == 0) return Negative_Tag;
    if (strcmp(name, "Foil_Tag") == 0) return Foil_Tag;
    if (strcmp(name, "Holographic_Tag") == 0) return Holographic_Tag;
    if (strcmp(name, "Polychrome_Tag") == 0) return Polychrome_Tag;
    if (strcmp(name, "Investment_Tag") == 0) return Investment_Tag;
    if (strcmp(name, "Voucher_Tag") == 0) return Voucher_Tag;
    if (strcmp(name, "Boss_Tag") == 0) return Boss_Tag;
    if (strcmp(name, "Standard_Tag") == 0) return Standard_Tag;
    if (strcmp(name, "Charm_Tag") == 0) return Charm_Tag;
    if (strcmp(name, "Meteor_Tag") == 0) return Meteor_Tag;
    if (strcmp(name, "Buffoon_Tag") == 0) return Buffoon_Tag;
    if (strcmp(name, "Handy_Tag") == 0) return Handy_Tag;
    if (strcmp(name, "Garbage_Tag") == 0) return Garbage_Tag;
    if (strcmp(name, "Ethereal_Tag") == 0) return Ethereal_Tag;
    if (strcmp(name, "Coupon_Tag") == 0) return Coupon_Tag;
    if (strcmp(name, "Double_Tag") == 0) return Double_Tag;
    if (strcmp(name, "Juggle_Tag") == 0) return Juggle_Tag;
    if (strcmp(name, "D6_Tag") == 0) return D6_Tag;
    if (strcmp(name, "Top_up_Tag") == 0) return Top_up_Tag;
    if (strcmp(name, "Speed_Tag") == 0) return Speed_Tag;
    if (strcmp(name, "Orbital_Tag") == 0) return Orbital_Tag;
    if (strcmp(name, "Economy_Tag") == 0) return Economy_Tag;
    if (strcmp(name, "TAG_END") == 0) return TAG_END;

    // Blinds
    if (strcmp(name, "B_BEGIN") == 0) return B_BEGIN;
    if (strcmp(name, "Small_Blind") == 0) return Small_Blind;
    if (strcmp(name, "Big_Blind") == 0) return Big_Blind;
    if (strcmp(name, "The_Hook") == 0) return The_Hook;
    if (strcmp(name, "The_Ox") == 0) return The_Ox;
    if (strcmp(name, "The_House") == 0) return The_House;
    if (strcmp(name, "The_Wall") == 0) return The_Wall;
    if (strcmp(name, "The_Wheel") == 0) return The_Wheel;
    if (strcmp(name, "The_Arm") == 0) return The_Arm;
    if (strcmp(name, "The_Club") == 0) return The_Club;
    if (strcmp(name, "The_Fish") == 0) return The_Fish;
    if (strcmp(name, "The_Psychic") == 0) return The_Psychic;
    if (strcmp(name, "The_Goad") == 0) return The_Goad;
    if (strcmp(name, "The_Water") == 0) return The_Water;
    if (strcmp(name, "The_Window") == 0) return The_Window;
    if (strcmp(name, "The_Manacle") == 0) return The_Manacle;
    if (strcmp(name, "The_Eye") == 0) return The_Eye;
    if (strcmp(name, "The_Mouth") == 0) return The_Mouth;
    if (strcmp(name, "The_Plant") == 0) return The_Plant;
    if (strcmp(name, "The_Serpent") == 0) return The_Serpent;
    if (strcmp(name, "The_Pillar") == 0) return The_Pillar;
    if (strcmp(name, "The_Needle") == 0) return The_Needle;
    if (strcmp(name, "The_Head") == 0) return The_Head;
    if (strcmp(name, "The_Tooth") == 0) return The_Tooth;
    if (strcmp(name, "The_Flint") == 0) return The_Flint;
    if (strcmp(name, "The_Mark") == 0) return The_Mark;
    if (strcmp(name, "B_F_BEGIN") == 0) return B_F_BEGIN;
    if (strcmp(name, "Amber_Acorn") == 0) return Amber_Acorn;
    if (strcmp(name, "Verdant_Leaf") == 0) return Verdant_Leaf;
    if (strcmp(name, "Violet_Vessel") == 0) return Violet_Vessel;
    if (strcmp(name, "Crimson_Heart") == 0) return Crimson_Heart;
    if (strcmp(name, "Cerulean_Bell") == 0) return Cerulean_Bell;
    if (strcmp(name, "B_F_END") == 0) return B_F_END;
    if (strcmp(name, "B_END") == 0) return B_END;

    // Suits
    if (strcmp(name, "SUIT_BEGIN") == 0) return SUIT_BEGIN;
    if (strcmp(name, "Hearts") == 0) return Hearts;
    if (strcmp(name, "Clubs") == 0) return Clubs;
    if (strcmp(name, "Diamonds") == 0) return Diamonds;
    if (strcmp(name, "Spades") == 0) return Spades;
    if (strcmp(name, "SUIT_END") == 0) return SUIT_END;

    // Ranks
    if (strcmp(name, "RANK_BEGIN") == 0) return RANK_BEGIN;
    if (strcmp(name, "2") == 0) return _2;
    if (strcmp(name, "3") == 0) return _3;
    if (strcmp(name, "4") == 0) return _4;
    if (strcmp(name, "5") == 0) return _5;
    if (strcmp(name, "6") == 0) return _6;
    if (strcmp(name, "7") == 0) return _7;
    if (strcmp(name, "8") == 0) return _8;
    if (strcmp(name, "9") == 0) return _9;
    if (strcmp(name, "10") == 0) return _10;
    if (strcmp(name, "Jack") == 0) return Jack;
    if (strcmp(name, "Queen") == 0) return Queen;
    if (strcmp(name, "King") == 0) return King;
    if (strcmp(name, "Ace") == 0) return Ace;
    if (strcmp(name, "RANK_END") == 0) return RANK_END;

    // Cards
    if (strcmp(name, "C_BEGIN") == 0) return C_BEGIN;
    if (strcmp(name, "C_2") == 0) return C_2;
    if (strcmp(name, "C_3") == 0) return C_3;
    if (strcmp(name, "C_4") == 0) return C_4;
    if (strcmp(name, "C_5") == 0) return C_5;
    if (strcmp(name, "C_6") == 0) return C_6;
    if (strcmp(name, "C_7") == 0) return C_7;
    if (strcmp(name, "C_8") == 0) return C_8;
    if (strcmp(name, "C_9") == 0) return C_9;
    if (strcmp(name, "C_A") == 0) return C_A;
    if (strcmp(name, "C_J") == 0) return C_J;
    if (strcmp(name, "C_K") == 0) return C_K;
    if (strcmp(name, "C_Q") == 0) return C_Q;
    if (strcmp(name, "C_T") == 0) return C_T;
    if (strcmp(name, "D_2") == 0) return D_2;
    if (strcmp(name, "D_3") == 0) return D_3;
    if (strcmp(name, "D_4") == 0) return D_4;
    if (strcmp(name, "D_5") == 0) return D_5;
    if (strcmp(name, "D_6") == 0) return D_6;
    if (strcmp(name, "D_7") == 0) return D_7;
    if (strcmp(name, "D_8") == 0) return D_8;
    if (strcmp(name, "D_9") == 0) return D_9;
    if (strcmp(name, "D_A") == 0) return D_A;
    if (strcmp(name, "D_J") == 0) return D_J;
    if (strcmp(name, "D_K") == 0) return D_K;
    if (strcmp(name, "D_Q") == 0) return D_Q;
    if (strcmp(name, "D_T") == 0) return D_T;
    if (strcmp(name, "H_2") == 0) return H_2;
    if (strcmp(name, "H_3") == 0) return H_3;
    if (strcmp(name, "H_4") == 0) return H_4;
    if (strcmp(name, "H_5") == 0) return H_5;
    if (strcmp(name, "H_6") == 0) return H_6;
    if (strcmp(name, "H_7") == 0) return H_7;
    if (strcmp(name, "H_8") == 0) return H_8;
    if (strcmp(name, "H_9") == 0) return H_9;
    if (strcmp(name, "H_A") == 0) return H_A;
    if (strcmp(name, "H_J") == 0) return H_J;
    if (strcmp(name, "H_K") == 0) return H_K;
    if (strcmp(name, "H_Q") == 0) return H_Q;
    if (strcmp(name, "H_T") == 0) return H_T;
    if (strcmp(name, "S_2") == 0) return S_2;
    if (strcmp(name, "S_3") == 0) return S_3;
    if (strcmp(name, "S_4") == 0) return S_4;
    if (strcmp(name, "S_5") == 0) return S_5;
    if (strcmp(name, "S_6") == 0) return S_6;
    if (strcmp(name, "S_7") == 0) return S_7;
    if (strcmp(name, "S_8") == 0) return S_8;
    if (strcmp(name, "S_9") == 0) return S_9;
    if (strcmp(name, "S_A") == 0) return S_A;
    if (strcmp(name, "S_J") == 0) return S_J;
    if (strcmp(name, "S_K") == 0) return S_K;
    if (strcmp(name, "S_Q") == 0) return S_Q;
    if (strcmp(name, "S_T") == 0) return S_T;
    if (strcmp(name, "C_END") == 0) return C_END;

    // Decks
    if (strcmp(name, "D_BEGIN") == 0) return D_BEGIN;
    if (strcmp(name, "Red_Deck") == 0) return Red_Deck;
    if (strcmp(name, "Blue_Deck") == 0) return Blue_Deck;
    if (strcmp(name, "Yellow_Deck") == 0) return Yellow_Deck;
    if (strcmp(name, "Green_Deck") == 0) return Green_Deck;
    if (strcmp(name, "Black_Deck") == 0) return Black_Deck;
    if (strcmp(name, "Magic_Deck") == 0) return Magic_Deck;
    if (strcmp(name, "Nebula_Deck") == 0) return Nebula_Deck;
    if (strcmp(name, "Ghost_Deck") == 0) return Ghost_Deck;
    if (strcmp(name, "Abandoned_Deck") == 0) return Abandoned_Deck;
    if (strcmp(name, "Checkered_Deck") == 0) return Checkered_Deck;
    if (strcmp(name, "Zodiac_Deck") == 0) return Zodiac_Deck;
    if (strcmp(name, "Painted_Deck") == 0) return Painted_Deck;
    if (strcmp(name, "Anaglyph_Deck") == 0) return Anaglyph_Deck;
    if (strcmp(name, "Plasma_Deck") == 0) return Plasma_Deck;
    if (strcmp(name, "Erratic_Deck") == 0) return Erratic_Deck;
    if (strcmp(name, "Challenge_Deck") == 0) return Challenge_Deck;
    if (strcmp(name, "D_END") == 0) return D_END;

    // Challenges
    if (strcmp(name, "CHAL_BEGIN") == 0) return CHAL_BEGIN;
    if (strcmp(name, "The_Omelette") == 0) return The_Omelette;
    if (strcmp(name, "_15_Minute_City") == 0) return _15_Minute_City;
    if (strcmp(name, "Rich_get_Richer") == 0) return Rich_get_Richer;
    if (strcmp(name, "On_a_Knifes_Edge") == 0) return On_a_Knifes_Edge;
    if (strcmp(name, "X_ray_Vision") == 0) return X_ray_Vision;
    if (strcmp(name, "Mad_World") == 0) return Mad_World;
    if (strcmp(name, "Luxury_Tax") == 0) return Luxury_Tax;
    if (strcmp(name, "Non_Perishable") == 0) return Non_Perishable;
    if (strcmp(name, "Medusa") == 0) return Medusa;
    if (strcmp(name, "Double_or_Nothing") == 0) return Double_or_Nothing;
    if (strcmp(name, "Typecast") == 0) return Typecast;
    if (strcmp(name, "Inflation") == 0) return Inflation;
    if (strcmp(name, "Bram_Poker") == 0) return Bram_Poker;
    if (strcmp(name, "Fragile") == 0) return Fragile;
    if (strcmp(name, "Monolith") == 0) return Monolith;
    if (strcmp(name, "Blast_Off") == 0) return Blast_Off;
    if (strcmp(name, "Five_Card_Draw") == 0) return Five_Card_Draw;
    if (strcmp(name, "Golden_Needle") == 0) return Golden_Needle;
    if (strcmp(name, "Cruelty") == 0) return Cruelty;
    if (strcmp(name, "Jokerless") == 0) return Jokerless;
    if (strcmp(name, "CHAL_END") == 0) return CHAL_END;

    //Stakes
    if (strcmp(name, "STAKE_BEGIN") == 0) return STAKE_BEGIN;
    if (strcmp(name, "White_Stake") == 0) return White_Stake;
    if (strcmp(name, "Red_Stake") == 0) return Red_Stake;
    if (strcmp(name, "Green_Stake") == 0) return Green_Stake;
    if (strcmp(name, "Black_Stake") == 0) return Black_Stake;
    if (strcmp(name, "Blue_Stake") == 0) return Blue_Stake;
    if (strcmp(name, "Purple_Stake") == 0) return Purple_Stake;
    if (strcmp(name, "Orange_Stake") == 0) return Orange_Stake;
    if (strcmp(name, "Gold_Stake") == 0) return Gold_Stake;
    if (strcmp(name, "STAKE_END") == 0) return STAKE_END;

    if (strcmp(name, "ITEMS_END") == 0) return ITEMS_END;

    return RETRY;
}
void print_item_host(item i) {
    switch(i) {
        case RETRY: printf("RETRY"); break;
        case J_BEGIN: printf("J BEGIN"); break;
        case J_C_BEGIN: printf("J C BEGIN"); break;
        case Joker: printf("Joker"); break;
        case Greedy_Joker: printf("Greedy Joker"); break;
        case Lusty_Joker: printf("Lusty Joker"); break;
        case Wrathful_Joker: printf("Wrathful Joker"); break;
        case Gluttonous_Joker: printf("Gluttonous Joker"); break;
        case Jolly_Joker: printf("Jolly Joker"); break;
        case Zany_Joker: printf("Zany Joker"); break;
        case Mad_Joker: printf("Mad Joker"); break;
        case Crazy_Joker: printf("Crazy Joker"); break;
        case Droll_Joker: printf("Droll Joker"); break;
        case Sly_Joker: printf("Sly Joker"); break;
        case Wily_Joker: printf("Wily Joker"); break;
        case Clever_Joker: printf("Clever Joker"); break;
        case Devious_Joker: printf("Devious Joker"); break;
        case Crafty_Joker: printf("Crafty Joker"); break;
        case Half_Joker: printf("Half Joker"); break;
        case Credit_Card: printf("Credit Card"); break;
        case Banner: printf("Banner"); break;
        case Mystic_Summit: printf("Mystic Summit"); break;
        case _8_Ball: printf("8 Ball"); break;
        case Misprint: printf("Misprint"); break;
        case Raised_Fist: printf("Raised Fist"); break;
        case Chaos_the_Clown: printf("Chaos the Clown"); break;
        case Scary_Face: printf("Scary Face"); break;
        case Abstract_Joker: printf("Abstract Joker"); break;
        case Delayed_Gratification: printf("Delayed Gratification"); break;
        case Gros_Michel: printf("Gros Michel"); break;
        case Even_Steven: printf("Even Steven"); break;
        case Odd_Todd: printf("Odd Todd"); break;
        case Scholar: printf("Scholar"); break;
        case Business_Card: printf("Business Card"); break;
        case Supernova: printf("Supernova"); break;
        case Ride_the_Bus: printf("Ride the Bus"); break;
        case Egg: printf("Egg"); break;
        case Runner: printf("Runner"); break;
        case Ice_Cream: printf("Ice Cream"); break;
        case Splash: printf("Splash"); break;
        case Blue_Joker: printf("Blue Joker"); break;
        case Faceless_Joker: printf("Faceless Joker"); break;
        case Green_Joker: printf("Green Joker"); break;
        case Superposition: printf("Superposition"); break;
        case To_Do_List: printf("To Do List"); break;
        case Cavendish: printf("Cavendish"); break;
        case Red_Card: printf("Red Card"); break;
        case Square_Joker: printf("Square Joker"); break;
        case Riff_raff: printf("Riff-raff"); break;
        case Photograph: printf("Photograph"); break;
        case Mail_In_Rebate: printf("Mail-In Rebate"); break;
        case Hallucination: printf("Hallucination"); break;
        case Fortune_Teller: printf("Fortune Teller"); break;
        case Juggler: printf("Juggler"); break;
        case Drunkard: printf("Drunkard"); break;
        case Golden_Joker: printf("Golden Joker"); break;
        case Popcorn: printf("Popcorn"); break;
        case Walkie_Talkie: printf("Walkie Talkie"); break;
        case Smiley_Face: printf("Smiley Face"); break;
        case Golden_Ticket: printf("Golden Ticket"); break;
        case Swashbuckler: printf("Swashbuckler"); break;
        case Hanging_Chad: printf("Hanging Chad"); break;
        case Shoot_the_Moon: printf("Shoot the Moon"); break;
        case J_C_END: printf("J C END"); break;
        case J_U_BEGIN: printf("J U BEGIN"); break;
        case Joker_Stencil: printf("Joker Stencil"); break;
        case Four_Fingers: printf("Four Fingers"); break;
        case Mime: printf("Mime"); break;
        case Ceremonial_Dagger: printf("Ceremonial Dagger"); break;
        case Marble_Joker: printf("Marble Joker"); break;
        case Loyalty_Card: printf("Loyalty Card"); break;
        case Dusk: printf("Dusk"); break;
        case Fibonacci: printf("Fibonacci"); break;
        case Steel_Joker: printf("Steel Joker"); break;
        case Hack: printf("Hack"); break;
        case Pareidolia: printf("Pareidolia"); break;
        case Space_Joker: printf("Space Joker"); break;
        case Burglar: printf("Burglar"); break;
        case Blackboard: printf("Blackboard"); break;
        case Constellation: printf("Constellation"); break;
        case Hiker: printf("Hiker"); break;
        case Card_Sharp: printf("Card Sharp"); break;
        case Madness: printf("Madness"); break;
        case Vampire: printf("Vampire"); break;
        case Shortcut: printf("Shortcut"); break;
        case Hologram: printf("Hologram"); break;
        case Vagabond: printf("Vagabond"); break;
        case Cloud_9: printf("Cloud 9"); break;
        case Rocket: printf("Rocket"); break;
        case Midas_Mask: printf("Midas Mask"); break;
        case Luchador: printf("Luchador"); break;
        case Gift_Card: printf("Gift Card"); break;
        case Turtle_Bean: printf("Turtle Bean"); break;
        case Erosion: printf("Erosion"); break;
        case Reserved_Parking: printf("Reserved Parking"); break;
        case To_the_Moon: printf("To the Moon"); break;
        case Stone_Joker: printf("Stone Joker"); break;
        case Lucky_Cat: printf("Lucky Cat"); break;
        case Bull: printf("Bull"); break;
        case Diet_Cola: printf("Diet Cola"); break;
        case Trading_Card: printf("Trading Card"); break;
        case Flash_Card: printf("Flash Card"); break;
        case Spare_Trousers: printf("Spare Trousers"); break;
        case Ramen: printf("Ramen"); break;
        case Seltzer: printf("Seltzer"); break;
        case Castle: printf("Castle"); break;
        case Mr_Bones: printf("Mr. Bones"); break;
        case Acrobat: printf("Acrobat"); break;
        case Sock_and_Buskin: printf("Sock and Buskin"); break;
        case Troubadour: printf("Troubadour"); break;
        case Certificate: printf("Certificate"); break;
        case Smeared_Joker: printf("Smeared Joker"); break;
        case Throwback: printf("Throwback"); break;
        case Rough_Gem: printf("Rough Gem"); break;
        case Bloodstone: printf("Bloodstone"); break;
        case Arrowhead: printf("Arrowhead"); break;
        case Onyx_Agate: printf("Onyx Agate"); break;
        case Glass_Joker: printf("Glass Joker"); break;
        case Showman: printf("Showman"); break;
        case Flower_Pot: printf("Flower Pot"); break;
        case Merry_Andy: printf("Merry Andy"); break;
        case Oops_All_6s: printf("Oops! All 6s"); break;
        case The_Idol: printf("The Idol"); break;
        case Seeing_Double: printf("Seeing Double"); break;
        case Matador: printf("Matador"); break;
        case Stuntman: printf("Stuntman"); break;
        case Satellite: printf("Satellite"); break;
        case Cartomancer: printf("Cartomancer"); break;
        case Astronomer: printf("Astronomer"); break;
        case Burnt_Joker: printf("Burnt Joker"); break;
        case Bootstraps: printf("Bootstraps"); break;
        case J_U_END: printf("J U END"); break;
        case J_R_BEGIN: printf("J R BEGIN"); break;
        case DNA: printf("DNA"); break;
        case Sixth_Sense: printf("Sixth Sense"); break;
        case Seance: printf("Seance"); break;
        case Baron: printf("Baron"); break;
        case Obelisk: printf("Obelisk"); break;
        case Baseball_Card: printf("Baseball Card"); break;
        case Ancient_Joker: printf("Ancient Joker"); break;
        case Campfire: printf("Campfire"); break;
        case Blueprint: printf("Blueprint"); break;
        case Wee_Joker: printf("Wee Joker"); break;
        case Hit_the_Road: printf("Hit the Road"); break;
        case The_Duo: printf("The Duo"); break;
        case The_Trio: printf("The Trio"); break;
        case The_Family: printf("The Family"); break;
        case The_Order: printf("The Order"); break;
        case The_Tribe: printf("The Tribe"); break;
        case Invisible_Joker: printf("Invisible Joker"); break;
        case Brainstorm: printf("Brainstorm"); break;
        case Drivers_License: printf("Driver's License"); break;
        case J_R_END: printf("J R END"); break;
        case J_L_BEGIN: printf("J L BEGIN"); break;
        case Canio: printf("Canio"); break;
        case Triboulet: printf("Triboulet"); break;
        case Yorick: printf("Yorick"); break;
        case Chicot: printf("Chicot"); break;
        case Perkeo: printf("Perkeo"); break;
        case J_L_END: printf("J L END"); break;
        case J_END: printf("J END"); break;
        case V_BEGIN: printf("V BEGIN"); break;
        case Overstock: printf("Overstock"); break;
        case Overstock_Plus: printf("Overstock Plus"); break;
        case Clearance_Sale: printf("Clearance Sale"); break;
        case Liquidation: printf("Liquidation"); break;
        case Hone: printf("Hone"); break;
        case Glow_Up: printf("Glow Up"); break;
        case Reroll_Surplus: printf("Reroll Surplus"); break;
        case Reroll_Glut: printf("Reroll Glut"); break;
        case Crystal_Ball: printf("Crystal Ball"); break;
        case Omen_Globe: printf("Omen Globe"); break;
        case Telescope: printf("Telescope"); break;
        case Observatory: printf("Observatory"); break;
        case Grabber: printf("Grabber"); break;
        case Nacho_Tong: printf("Nacho Tong"); break;
        case Wasteful: printf("Wasteful"); break;
        case Recyclomancy: printf("Recyclomancy"); break;
        case Tarot_Merchant: printf("Tarot Merchant"); break;
        case Tarot_Tycoon: printf("Tarot Tycoon"); break;
        case Planet_Merchant: printf("Planet Merchant"); break;
        case Planet_Tycoon: printf("Planet Tycoon"); break;
        case Seed_Money: printf("Seed Money"); break;
        case Money_Tree: printf("Money Tree"); break;
        case Blank: printf("Blank"); break;
        case Antimatter: printf("Antimatter"); break;
        case Magic_Trick: printf("Magic Trick"); break;
        case Illusion: printf("Illusion"); break;
        case Hieroglyph: printf("Hieroglyph"); break;
        case Petroglyph: printf("Petroglyph"); break;
        case Directors_Cut: printf("Director's Cut"); break;
        case Retcon: printf("Retcon"); break;
        case Paint_Brush: printf("Paint Brush"); break;
        case Palette: printf("Palette"); break;
        case V_END: printf("V END"); break;
        case T_BEGIN: printf("T BEGIN"); break;
        case The_Fool: printf("The Fool"); break;
        case The_Magician: printf("The Magician"); break;
        case The_High_Priestess: printf("The High Priestess"); break;
        case The_Empress: printf("The Empress"); break;
        case The_Emperor: printf("The Emperor"); break;
        case The_Hierophant: printf("The Hierophant"); break;
        case The_Lovers: printf("The Lovers"); break;
        case The_Chariot: printf("The Chariot"); break;
        case Justice: printf("Justice"); break;
        case The_Hermit: printf("The Hermit"); break;
        case The_Wheel_of_Fortune: printf("The Wheel of Fortune"); break;
        case Strength: printf("Strength"); break;
        case The_Hanged_Man: printf("The Hanged Man"); break;
        case Death: printf("Death"); break;
        case Temperance: printf("Temperance"); break;
        case The_Devil: printf("The Devil"); break;
        case The_Tower: printf("The Tower"); break;
        case The_Star: printf("The Star"); break;
        case The_Moon: printf("The Moon"); break;
        case The_Sun: printf("The Sun"); break;
        case Judgement: printf("Judgement"); break;
        case The_World: printf("The World"); break;
        case T_END: printf("T END"); break;
        case P_BEGIN: printf("P BEGIN"); break;
        case Mercury: printf("Mercury"); break;
        case Venus: printf("Venus"); break;
        case Earth: printf("Earth"); break;
        case Mars: printf("Mars"); break;
        case Jupiter: printf("Jupiter"); break;
        case Saturn: printf("Saturn"); break;
        case Uranus: printf("Uranus"); break;
        case Neptune: printf("Neptune"); break;
        case Pluto: printf("Pluto"); break;
        case Planet_X: printf("Planet X"); break;
        case Ceres: printf("Ceres"); break;
        case Eris: printf("Eris"); break;
        case P_END: printf("P END"); break;
        case H_BEGIN: printf("H BEGIN"); break;
        case Pair: printf("Pair"); break;
        case Three_of_a_Kind: printf("Three of a Kind"); break;
        case Full_House: printf("Full House"); break;
        case Four_of_a_Kind: printf("Four of a Kind"); break;
        case Flush: printf("Flush"); break;
        case Straight: printf("Straight"); break;
        case Two_Pair: printf("Two Pair"); break;
        case Straight_Flush: printf("Straight Flush"); break;
        case High_Card: printf("High Card"); break;
        case Five_of_a_Kind: printf("Five of a Kind"); break;
        case Flush_House: printf("Flush House"); break;
        case Flush_Five: printf("Flush Five"); break;
        case H_END: printf("H END"); break;
        case S_BEGIN: printf("S BEGIN"); break;
        case Familiar: printf("Familiar"); break;
        case Grim: printf("Grim"); break;
        case Incantation: printf("Incantation"); break;
        case Talisman: printf("Talisman"); break;
        case Aura: printf("Aura"); break;
        case Wraith: printf("Wraith"); break;
        case Sigil: printf("Sigil"); break;
        case Ouija: printf("Ouija"); break;
        case Ectoplasm: printf("Ectoplasm"); break;
        case Immolate: printf("Immolate"); break;
        case Ankh: printf("Ankh"); break;
        case Deja_Vu: printf("Deja Vu"); break;
        case Hex: printf("Hex"); break;
        case Trance: printf("Trance"); break;
        case Medium: printf("Medium"); break;
        case Cryptid: printf("Cryptid"); break;
        case The_Soul: printf("The Soul"); break;
        case Black_Hole: printf("Black Hole"); break;
        case S_END: printf("S END"); break;
        case ENHANCEMENT_BEGIN: printf("ENHANCEMENT BEGIN"); break;
        case No_Enhancement: printf("No Enhancement"); break;
        case Bonus_Card: printf("Bonus"); break;
        case Mult_Card: printf("Mult"); break;
        case Wild_Card: printf("Wild"); break;
        case Glass_Card: printf("Glass"); break;
        case Steel_Card: printf("Steel"); break;
        case Stone_Card: printf("Stone"); break;
        case Gold_Card: printf("Gold"); break;
        case Lucky_Card: printf("Lucky"); break;
        case ENHANCEMENT_END: printf("ENHANCEMENT END"); break;
        case SEAL_BEGIN: printf("SEAL BEGIN"); break;
        case No_Seal: printf("No Seal"); break;
        case Gold_Seal: printf("Gold Seal"); break;
        case Red_Seal: printf("Red Seal"); break;
        case Blue_Seal: printf("Blue Seal"); break;
        case Purple_Seal: printf("Purple Seal"); break;
        case SEAL_END: printf("SEAL END"); break;
        case E_BEGIN: printf("E BEGIN"); break;
        case No_Edition: printf("No Edition"); break;
        case Foil: printf("Foil"); break;
        case Holographic: printf("Holographic"); break;
        case Polychrome: printf("Polychrome"); break;
        case Negative: printf("Negative"); break;
        case E_END: printf("E END"); break;
        case PACK_BEGIN: printf("PACK BEGIN"); break;
        case Arcana_Pack: printf("Arcana Pack"); break;
        case Jumbo_Arcana_Pack: printf("Jumbo Arcana Pack"); break;
        case Mega_Arcana_Pack: printf("Mega Arcana Pack"); break;
        case Celestial_Pack: printf("Celestial Pack"); break;
        case Jumbo_Celestial_Pack: printf("Jumbo Celestial Pack"); break;
        case Mega_Celestial_Pack: printf("Mega Celestial Pack"); break;
        case Standard_Pack: printf("Standard Pack"); break;
        case Jumbo_Standard_Pack: printf("Jumbo Standard Pack"); break;
        case Mega_Standard_Pack: printf("Mega Standard Pack"); break;
        case Buffoon_Pack: printf("Buffoon Pack"); break;
        case Jumbo_Buffoon_Pack: printf("Jumbo Buffoon Pack"); break;
        case Mega_Buffoon_Pack: printf("Mega Buffoon Pack"); break;
        case Spectral_Pack: printf("Spectral Pack"); break;
        case Jumbo_Spectral_Pack: printf("Jumbo Spectral Pack"); break;
        case Mega_Spectral_Pack: printf("Mega Spectral Pack"); break;
        case PACK_END: printf("PACK END"); break;
        case TAG_BEGIN: printf("TAG BEGIN"); break;
        case Uncommon_Tag: printf("Uncommon Tag"); break;
        case Rare_Tag: printf("Rare Tag"); break;
        case Negative_Tag: printf("Negative Tag"); break;
        case Foil_Tag: printf("Foil Tag"); break;
        case Holographic_Tag: printf("Holographic Tag"); break;
        case Polychrome_Tag: printf("Polychrome Tag"); break;
        case Investment_Tag: printf("Investment Tag"); break;
        case Voucher_Tag: printf("Voucher Tag"); break;
        case Boss_Tag: printf("Boss Tag"); break;
        case Standard_Tag: printf("Standard Tag"); break;
        case Charm_Tag: printf("Charm Tag"); break;
        case Meteor_Tag: printf("Meteor Tag"); break;
        case Buffoon_Tag: printf("Buffoon Tag"); break;
        case Handy_Tag: printf("Handy Tag"); break;
        case Garbage_Tag: printf("Garbage Tag"); break;
        case Ethereal_Tag: printf("Ethereal Tag"); break;
        case Coupon_Tag: printf("Coupon Tag"); break;
        case Double_Tag: printf("Double Tag"); break;
        case Juggle_Tag: printf("Juggle Tag"); break;
        case D6_Tag: printf("D6 Tag"); break;
        case Top_up_Tag: printf("Top-up Tag"); break;
        case Speed_Tag: printf("Speed Tag"); break;
        case Orbital_Tag: printf("Orbital Tag"); break;
        case Economy_Tag: printf("Economy Tag"); break;
        case TAG_END: printf("TAG END"); break;
        case B_BEGIN: printf("B BEGIN"); break;
        case Small_Blind: printf("Small Blind"); break;
        case Big_Blind: printf("Big Blind"); break;
        case The_Hook: printf("The Hook"); break;
        case The_Ox: printf("The Ox"); break;
        case The_House: printf("The House"); break;
        case The_Wall: printf("The Wall"); break;
        case The_Wheel: printf("The Wheel"); break;
        case The_Arm: printf("The Arm"); break;
        case The_Club: printf("The Club"); break;
        case The_Fish: printf("The Fish"); break;
        case The_Psychic: printf("The Psychic"); break;
        case The_Goad: printf("The Goad"); break;
        case The_Water: printf("The Water"); break;
        case The_Window: printf("The Window"); break;
        case The_Manacle: printf("The Manacle"); break;
        case The_Eye: printf("The Eye"); break;
        case The_Mouth: printf("The Mouth"); break;
        case The_Plant: printf("The Plant"); break;
        case The_Serpent: printf("The Serpent"); break;
        case The_Pillar: printf("The Pillar"); break;
        case The_Needle: printf("The Needle"); break;
        case The_Head: printf("The Head"); break;
        case The_Tooth: printf("The Tooth"); break;
        case The_Flint: printf("The Flint"); break;
        case The_Mark: printf("The Mark"); break;
        case Amber_Acorn: printf("Amber Acorn"); break;
        case Verdant_Leaf: printf("Verdant Leaf"); break;
        case Violet_Vessel: printf("Violet Vessel"); break;
        case Crimson_Heart: printf("Crimson Heart"); break;
        case Cerulean_Bell: printf("Cerulean Bell"); break;
        case B_END: printf("B END"); break;
        case SUIT_BEGIN: printf("SUIT BEGIN"); break;
        case Hearts: printf("Hearts"); break;
        case Clubs: printf("Clubs"); break;
        case Diamonds: printf("Diamonds"); break;
        case Spades: printf("Spades"); break;
        case SUIT_END: printf("SUIT END"); break;
        case RANK_BEGIN: printf("RANK BEGIN"); break;
        case _2: printf("2"); break;
        case _3: printf("3"); break;
        case _4: printf("4"); break;
        case _5: printf("5"); break;
        case _6: printf("6"); break;
        case _7: printf("7"); break;
        case _8: printf("8"); break;
        case _9: printf("9"); break;
        case _10: printf("10"); break;
        case Jack: printf("Jack"); break;
        case Queen: printf("Queen"); break;
        case King: printf("King"); break;
        case Ace: printf("Ace"); break;
        case RANK_END: printf("RANK END"); break;
        case C_BEGIN: printf("C BEGIN"); break;
        case C_2: printf("C 2"); break;
        case C_3: printf("C 3"); break;
        case C_4: printf("C 4"); break;
        case C_5: printf("C 5"); break;
        case C_6: printf("C 6"); break;
        case C_7: printf("C 7"); break;
        case C_8: printf("C 8"); break;
        case C_9: printf("C 9"); break;
        case C_A: printf("C A"); break;
        case C_J: printf("C J"); break;
        case C_K: printf("C K"); break;
        case C_Q: printf("C Q"); break;
        case C_T: printf("C T"); break;
        case D_2: printf("D 2"); break;
        case D_3: printf("D 3"); break;
        case D_4: printf("D 4"); break;
        case D_5: printf("D 5"); break;
        case D_6: printf("D 6"); break;
        case D_7: printf("D 7"); break;
        case D_8: printf("D 8"); break;
        case D_9: printf("D 9"); break;
        case D_A: printf("D A"); break;
        case D_J: printf("D J"); break;
        case D_K: printf("D K"); break;
        case D_Q: printf("D Q"); break;
        case D_T: printf("D T"); break;
        case H_2: printf("H 2"); break;
        case H_3: printf("H 3"); break;
        case H_4: printf("H 4"); break;
        case H_5: printf("H 5"); break;
        case H_6: printf("H 6"); break;
        case H_7: printf("H 7"); break;
        case H_8: printf("H 8"); break;
        case H_9: printf("H 9"); break;
        case H_A: printf("H A"); break;
        case H_J: printf("H J"); break;
        case H_K: printf("H K"); break;
        case H_Q: printf("H Q"); break;
        case H_T: printf("H T"); break;
        case S_2: printf("S 2"); break;
        case S_3: printf("S 3"); break;
        case S_4: printf("S 4"); break;
        case S_5: printf("S 5"); break;
        case S_6: printf("S 6"); break;
        case S_7: printf("S 7"); break;
        case S_8: printf("S 8"); break;
        case S_9: printf("S 9"); break;
        case S_A: printf("S A"); break;
        case S_J: printf("S J"); break;
        case S_K: printf("S K"); break;
        case S_Q: printf("S Q"); break;
        case S_T: printf("S T"); break;
        case C_END: printf("C END"); break;
        case D_BEGIN: printf("D BEGIN"); break;
        case Red_Deck: printf("Red Deck"); break;
        case Blue_Deck: printf("Blue Deck"); break;
        case Yellow_Deck: printf("Yellow Deck"); break;
        case Green_Deck: printf("Green Deck"); break;
        case Black_Deck: printf("Black Deck"); break;
        case Magic_Deck: printf("Magic Deck"); break;
        case Nebula_Deck: printf("Nebula Deck"); break;
        case Ghost_Deck: printf("Ghost Deck"); break;
        case Abandoned_Deck: printf("Abandoned Deck"); break;
        case Checkered_Deck: printf("Checkered Deck"); break;
        case Zodiac_Deck: printf("Zodiac Deck"); break;
        case Painted_Deck: printf("Painted Deck"); break;
        case Anaglyph_Deck: printf("Anaglyph Deck"); break;
        case Plasma_Deck: printf("Plasma Deck"); break;
        case Erratic_Deck: printf("Erratic Deck"); break;
        case Challenge_Deck: printf("Challenge Deck"); break;
        case D_END: printf("D END"); break;
        case CHAL_BEGIN: printf("CHAL BEGIN"); break;
        case The_Omelette: printf("The Omelette"); break;
        case _15_Minute_City: printf("15 Minute City"); break;
        case Rich_get_Richer: printf("Rich get Richer"); break;
        case On_a_Knifes_Edge: printf("On a Knife's Edge"); break;
        case X_ray_Vision: printf("X-ray Vision"); break;
        case Mad_World: printf("Mad World"); break;
        case Luxury_Tax: printf("Luxury Tax"); break;
        case Non_Perishable: printf("Non-Perishable"); break;
        case Medusa: printf("Medusa"); break;
        case Double_or_Nothing: printf("Double or Nothing"); break;
        case Typecast: printf("Typecast"); break;
        case Inflation: printf("Inflation"); break;
        case Bram_Poker: printf("Bram Poker"); break;
        case Fragile: printf("Fragile"); break;
        case Monolith: printf("Monolith"); break;
        case Blast_Off: printf("Blast Off"); break;
        case Five_Card_Draw: printf("Five-Card Draw"); break;
        case Golden_Needle: printf("Golden Needle"); break;
        case Cruelty: printf("Cruelty"); break;
        case Jokerless: printf("Jokerless"); break;
        case CHAL_END: printf("CHAL END"); break;
        case White_Stake: printf("White Stake"); break;
        case Green_Stake: printf("Green Stake"); break;
        case Black_Stake: printf("Black Stake"); break;
        case Red_Stake: printf("Red Stake"); break;
        case Blue_Stake: printf("Blue Stake"); break;
        case Purple_Stake: printf("Purple Stake"); break;
        case Orange_Stake: printf("Orange Stake"); break;
        case Gold_Stake: printf("Gold Stake"); break;
        default: printf("RETRY"); break;
    }
}
#endif // ITEMS_H