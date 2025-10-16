#pragma once
#include <stdint.h>

#define AOG_SHM_NAME "/aoglink_steer"
#define AOG_SHM_SLOTS 8

typedef struct {
  uint32_t seq;
  uint64_t monotonic_us;
  float angle_deg;
  float rate_deg_s;
  float curvature;
} aog_steer_target_t;

typedef struct {
  aog_steer_target_t slot[AOG_SHM_SLOTS];
  volatile uint32_t head;
  volatile uint32_t tail;
} aog_ring_t;
