#include "lib/ouija.cl"
#include "lib/ouija_config.cl" 
#include "lib/ouija_result.cl" 

void ouija_filter(instance *inst, __constant MotelyJsonConfig *config, __global OuijaResult *result);

__kernel void ouija_list_search(__global char8 *seed_list, // Array of seeds from file
                                long num_seeds_for_this_dispatch, // Total seeds this kernel dispatch should handle
                                __constant MotelyJsonConfig *config,
                                __global OuijaResult *results,
                                long batch_seed_offset) { // Offset into the seed list
    size_t current_global_id = get_global_id(0);
    size_t total_global_size = get_global_size(0);
    
    // Safety check: ensure we don't go out of bounds
    if (current_global_id >= num_seeds_for_this_dispatch) {
        return;
    }
    
    if (num_seeds_for_this_dispatch <= 0) {
        return; // Exit early if there are no seeds to process
    }
    
    // Get seed directly from the list
    size_t seed_index = current_global_id;
    size_t list_index = batch_seed_offset + seed_index;
    seed _seed = s_new_c8(seed_list[list_index]);

    // Process the seed
    if (seed_index < num_seeds_for_this_dispatch) {
        instance inst = i_new(_seed);
        ouija_filter(&inst, config, &results[seed_index]);
    }
    
    // Sync at end of batch to ensure all work-items complete before returning
    barrier(CLK_GLOBAL_MEM_FENCE);
}
