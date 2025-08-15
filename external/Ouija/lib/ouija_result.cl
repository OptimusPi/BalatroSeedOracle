/**
 * @file ouija_result.cl
 * @brief Definition of the OuijaResult structure returned from device to host
 */

#ifndef OUIJA_RESULT_CL
#define OUIJA_RESULT_CL

#include "lib/ouija.cl" // Include the necessary headers for item and jokerdata types

typedef struct {
    char seed[9];                // Bytes 0-8
    char padding;                // Byte 9 - explicit padding
    ushort TotalScore;           // Bytes 10-11
    uchar NaturalNegativeJokers; // Byte 12
    uchar DesiredNegativeJokers; // Byte 13
    uchar ScoreWants[MAX_DESIRES_KERNEL]; // Bytes 14 onwards
} OuijaResult;

#endif
