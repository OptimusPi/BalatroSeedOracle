#ifndef __UTILS_H_
#define __UTILS_H_

#include <stddef.h>

void format_number(double num, char* buffer, size_t buffer_size) {
    if (num >= 1e9) {
        snprintf(buffer, buffer_size, "%.2fB", num / 1e9);
    } else if (num >= 1e6) {
        snprintf(buffer, buffer_size, "%.2fM", num / 1e6);
    } else if (num >= 1e3) {
        snprintf(buffer, buffer_size, "%.2fK", num / 1e3);
    } else {
        snprintf(buffer, buffer_size, "%.2f", num);
    }
}

#endif