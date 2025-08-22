/**
 * @file ouija_config.cl
 * @brief Definition of the MotelyJsonConfig structure shared between host and device
 */

#ifndef OUIJA_CONFIG_H
#define OUIJA_CONFIG_H

#include "lib/ouija.cl" // Include the necessary headers for item and jokerdata types

// Enhanced desire structure with per-item ante requirement and search parameters
typedef struct {
    item value;               // Item or Joker ID
    item jokeredition;        // Edition of the joker, or RETRY if not a joker
    int desireByAnte;         // Ante by which this item should be found
    int minMatches;           // Minimum matches required (for Needs only, default 1)
    int searchAntes[16];      // Array of antes to search (0-terminated, max 16 antes)
    int searchAnteCount;      // Number of antes in searchAntes array
} Desire;

typedef struct {
    int numNeeds;                      // Number of Needs
    int numWants;                      // Number of Wants
    Desire Needs[MAX_DESIRES_KERNEL];  // Array of Needs
    Desire Wants[MAX_DESIRES_KERNEL];  // Array of Wants
    int maxSearchAnte;                 // Maximum Ante to search through
    item deck;                         // Deck to use
    item stake;                        // Stake to use
    bool scoreNaturalNegatives;        // Score jokers that are naturally negative
    bool scoreDesiredNegatives;        // Score desired jokers that are naturally negative or from skip tag negative mechanic
    
    // Extended config parameters for flexible filters
    int startAnte;                     // Starting ante for search (default: 1)
    int jokersPerAnte;                 // Number of jokers to generate per ante (default: 4)
    int shopSkipCount;                 // Number of shop items to skip (default: 2)
    bool searchNegativeTags;           // Search for negative tags
    bool searchPacks;                  // Search booster packs
    bool countNegativeEditions;        // Count negative editions
    bool useDeepSearch;                // Use sliding window deep search
    int deepSearchWindow;              // Window size for deep search (default: 8)
    int deepSearchSlide;               // Slide step for deep search (default: 2)
    int deepSearchTotal;               // Total jokers for deep search (default: 16)
    int minimumErraticRankMatches;     // Minimum number of matching ranks required for Erratic deck (0 = no requirement)
    int minimumErraticSuitMatches;     // Minimum number of matching suits required for Erratic deck (0 = no requirement)
} MotelyJsonConfig;

#endif
