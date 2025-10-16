package shm

import (
	"encoding/binary"
	"sort"
	"testing"
	"time"

	"golang.org/x/sys/unix"
)

func TestRingPushUpdatesHeadTail(t *testing.T) {
	ring, err := Open("/test_ring", "/test_event")
	if err != nil {
		t.Fatalf("open ring: %v", err)
	}
	t.Cleanup(func() { _ = ring.Close() })

	target := Target{Sequence: 1, MonotonicUS: NowMicro(), AngleDeg: 12.5}
	if err := ring.Push(target); err != nil {
		t.Fatalf("push: %v", err)
	}

	if head := ring.HeadIndex(); head != 1 {
		t.Fatalf("head index = %d, want 1", head)
	}
	if tail := ring.TailIndex(); tail != 0 {
		t.Fatalf("tail index = %d, want 0", tail)
	}

	slot := ring.Slot(1)
	if slot.Sequence != target.Sequence || slot.AngleDeg != target.AngleDeg {
		t.Fatalf("slot mismatch got %+v want %+v", slot, target)
	}
}

func TestRingSignalsEventFD(t *testing.T) {
	ring, err := Open("/test_ring2", "/test_event2")
	if err != nil {
		t.Fatalf("open ring: %v", err)
	}
	t.Cleanup(func() { _ = ring.Close() })

	if err := ring.Push(Target{Sequence: 7, MonotonicUS: NowMicro()}); err != nil {
		t.Fatalf("push: %v", err)
	}

	buf := make([]byte, 8)
	if _, err := unix.Read(ring.EventFD(), buf); err != nil {
		t.Fatalf("eventfd read: %v", err)
	}
	if binary.LittleEndian.Uint64(buf) == 0 {
		t.Fatalf("eventfd counter not incremented")
	}
}

func TestRingLatencyBelow2ms(t *testing.T) {
	ring, err := Open("/test_ring_latency", "/test_event_latency")
	if err != nil {
		t.Fatalf("open ring: %v", err)
	}
	t.Cleanup(func() { _ = ring.Close() })

	const samples = 100
	latencies := make([]time.Duration, 0, samples)
	for i := 0; i < samples; i++ {
		start := time.Now()
		if err := ring.Push(Target{Sequence: uint32(i + 1), MonotonicUS: NowMicro()}); err != nil {
			t.Fatalf("push: %v", err)
		}
		buf := make([]byte, 8)
		if _, err := unix.Read(ring.EventFD(), buf); err != nil {
			t.Fatalf("eventfd read: %v", err)
		}
		latencies = append(latencies, time.Since(start))
	}
	median := percentile(latencies, 50)
	if median > 2*time.Millisecond {
		t.Fatalf("median latency %s exceeds 2ms", median)
	}
}

func percentile(values []time.Duration, p int) time.Duration {
	copyVals := append([]time.Duration(nil), values...)
	sort.Slice(copyVals, func(i, j int) bool { return copyVals[i] < copyVals[j] })
	idx := (len(copyVals) * p) / 100
	if idx >= len(copyVals) {
		idx = len(copyVals) - 1
	}
	return copyVals[idx]
}
