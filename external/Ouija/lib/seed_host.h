// C version of seed utilities for Ouija host
#ifndef SEED_HOST_H
#define SEED_HOST_H
#include <stdint.h>
#include <string.h>

#define NUM_CHARS 35
static const char SEEDCHARS[NUM_CHARS+1] = "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

typedef struct {
    uint8_t data[8];
    int len;
} seed_host;

static int s_char_num_host(char c) {
    if (c >= '1' && c <= '9') return c - '1';
    if (c >= 'A' && c <= 'Z') return c - 'A' + 9;
    return 0; // fallback
}

static void s_new_c8_host(const char* str_seed, seed_host* out) {
    int i;
    for (i = 0; i < 8; i++) {
        if (str_seed[i] == '\0') break;
        out->data[i] = s_char_num_host(str_seed[i]);
    }
    out->len = i;
}

static void s_to_c8_host(const seed_host* s, char* out) {
    for (int i = 0; i < s->len; i++) out[i] = SEEDCHARS[s->data[i]];
    for (int i = s->len; i < 8; i++) out[i] = '\0';
}

static void s_skip_host(seed_host* s, int64_t n) {
    int carry = 0;
    int i = s->len - 1;
    int j = 0;
    uint8_t data[8] = {0};
    int orig_len = s->len;
    while (n > 0 || carry || j < orig_len) {
        int sum = carry + (n % NUM_CHARS);
        sum += (i >= 0 ? s->data[i] : -1);
        data[j] = (uint8_t)(sum % NUM_CHARS);
        carry = (sum >= NUM_CHARS);
        n /= NUM_CHARS;
        i--;
        j++;
    }
    s->len = (orig_len <= j) ? j : orig_len;
    for (int x = 0; x < s->len; x++) {
        s->data[s->len - 1 - x] = data[x];
    }
}

#endif // SEED_HOST_H
