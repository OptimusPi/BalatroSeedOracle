#include "lib/ouija.cl"
#include "lib/ouija_config.cl" 
#include "lib/ouija_result.cl" 

void ouija_filter(instance *inst, __constant MotelyJsonConfig *config, __global OuijaResult *result);

__kernel void ouija_search(char8 starting_seed_char8, // Renamed to avoid conflict with seed type
                           long num_seeds_for_this_dispatch, // Total seeds this kernel dispatch should handle
                           __constant MotelyJsonConfig *config,
                           __global OuijaResult *results,
                           long batch_seed_offset) { // Changed from pointer to value
    size_t current_global_id = get_global_id(0); // Original OpenCL type: size_t
    size_t total_global_size = get_global_size(0); // Original OpenCL type: size_t
    
    // Safety check: ensure we don't go out of bounds
    if (current_global_id >= num_seeds_for_this_dispatch) {
        return;
    }
    
    if (num_seeds_for_this_dispatch <= 0) {
        return; // Exit early if there are no seeds to process
    }
    seed _seed = s_new_c8(starting_seed_char8); // Initialize once from the global starting seed string
    
    // Calculate the absolute offset for this specific work-item
    size_t seed_index = current_global_id;
    size_t total_offset = batch_seed_offset + seed_index;
    
    // Skip to the correct seed position
    s_skip(&_seed, total_offset);

    // Process only one seed per work-item (simplified approach)
    if (seed_index < num_seeds_for_this_dispatch) {
        instance inst = i_new(_seed);
        ouija_filter(&inst, config, &results[seed_index]); // Process the instance
    }
    // Sync at end of batch to ensure all work-items complete before returning
    barrier(CLK_GLOBAL_MEM_FENCE); // Ensure all work-items in the work-group complete before returning
}