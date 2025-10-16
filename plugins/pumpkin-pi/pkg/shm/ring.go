package shm

import (
	"encoding/binary"
	"fmt"
	"math"
	"os"
	"sync"
	"time"

	"golang.org/x/sys/unix"
)

const (
	ringSlots  = 8
	slotSize   = 24
	ringSize   = ringSlots*slotSize + 8 // slots + head/tail
	headOffset = ringSlots * slotSize
	tailOffset = headOffset + 4
)

// Target mirrors the aog_steer_target_t layout defined in aoglink_shm.h.
type Target struct {
	Sequence    uint32
	MonotonicUS uint64
	AngleDeg    float32
	RateDegS    float32
	Curvature   float32
}

// Ring publishes steer targets through POSIX shared memory with an eventfd notifier.
type Ring struct {
	mu        sync.Mutex
	fd        int
	eventFD   int
	data      []byte
	name      string
	eventName string
}

// Open initialises (or creates) the shared memory ring and eventfd pair.
func Open(name string, eventName string) (*Ring, error) {
        if name == "" {
                name = "/aoglink_steer"
        }
        if name[0] != '/' {
                name = "/" + name
        }
        if eventName == "" {
                eventName = "/pumpkin-pi-steer"
        }
        if eventName[0] != '/' {
                eventName = "/" + eventName
        }
        shmPath := "/dev/shm" + name
        file, err := os.OpenFile(shmPath, os.O_CREATE|os.O_RDWR, 0o600)
        if err != nil {
                return nil, fmt.Errorf("shm open %s: %w", name, err)
        }
        fd := int(file.Fd())
        if err := unix.Ftruncate(fd, ringSize); err != nil {
                file.Close()
                return nil, fmt.Errorf("shm truncate: %w", err)
        }
        data, err := unix.Mmap(fd, 0, ringSize, unix.PROT_READ|unix.PROT_WRITE, unix.MAP_SHARED)
        if err != nil {
                file.Close()
                return nil, fmt.Errorf("mmap: %w", err)
        }
	evtfd, err := unix.Eventfd(0, unix.EFD_NONBLOCK|unix.EFD_CLOEXEC)
	if err != nil {
		unix.Munmap(data)
		unix.Close(fd)
		return nil, fmt.Errorf("eventfd: %w", err)
	}
	if err := ensureEventFDLink(eventName, evtfd); err != nil {
		unix.Close(evtfd)
		unix.Munmap(data)
		unix.Close(fd)
		return nil, err
	}
	return &Ring{fd: fd, data: data, eventFD: evtfd, name: name, eventName: eventName}, nil
}

func ensureEventFDLink(name string, fd int) error {
	path := "/dev/shm" + name
	_ = os.Remove(path)
	target := fmt.Sprintf("/proc/%d/fd/%d", os.Getpid(), fd)
	if err := os.Symlink(target, path); err != nil {
		// If symlinks are not permitted (e.g. readonly /dev/shm) ignore the failure.
		return nil
	}
	return nil
}

// Close releases all resources tied to the ring.
func (r *Ring) Close() error {
	r.mu.Lock()
	defer r.mu.Unlock()
	if r.data != nil {
		_ = unix.Munmap(r.data)
		r.data = nil
	}
	if r.fd != 0 {
		_ = unix.Close(r.fd)
		r.fd = 0
	}
	if r.eventFD != 0 {
		_ = unix.Close(r.eventFD)
		r.eventFD = 0
	}
	if r.eventName != "" {
		_ = os.Remove("/dev/shm" + r.eventName)
	}
	return nil
}

// Push writes the provided target into the ring and emits an eventfd wakeup.
func (r *Ring) Push(t Target) error {
	r.mu.Lock()
	defer r.mu.Unlock()
	if r.data == nil {
		return fmt.Errorf("ring closed")
	}
	head := binary.LittleEndian.Uint32(r.data[headOffset : headOffset+4])
	tail := binary.LittleEndian.Uint32(r.data[tailOffset : tailOffset+4])
	next := (head + 1) % ringSlots
	if next == tail {
		tail = (tail + 1) % ringSlots
	}
	writeSlot(r.data[int(next)*slotSize:], t)
	binary.LittleEndian.PutUint32(r.data[headOffset:headOffset+4], next)
	binary.LittleEndian.PutUint32(r.data[tailOffset:tailOffset+4], tail)
	buf := make([]byte, 8)
	binary.LittleEndian.PutUint64(buf, 1)
	if _, err := unix.Write(r.eventFD, buf); err != nil {
		return fmt.Errorf("eventfd write: %w", err)
	}
	return nil
}

func writeSlot(slot []byte, t Target) {
	binary.LittleEndian.PutUint32(slot[0:4], t.Sequence)
	binary.LittleEndian.PutUint64(slot[4:12], t.MonotonicUS)
	binary.LittleEndian.PutUint32(slot[12:16], math.Float32bits(t.AngleDeg))
	binary.LittleEndian.PutUint32(slot[16:20], math.Float32bits(t.RateDegS))
	binary.LittleEndian.PutUint32(slot[20:24], math.Float32bits(t.Curvature))
}

// EventFD exposes the eventfd descriptor for integration tests.
func (r *Ring) EventFD() int {
	return r.eventFD
}

// HeadIndex returns the current head pointer (for testing).
func (r *Ring) HeadIndex() uint32 {
	r.mu.Lock()
	defer r.mu.Unlock()
	return binary.LittleEndian.Uint32(r.data[headOffset : headOffset+4])
}

// TailIndex returns the current tail pointer (for testing).
func (r *Ring) TailIndex() uint32 {
	r.mu.Lock()
	defer r.mu.Unlock()
	return binary.LittleEndian.Uint32(r.data[tailOffset : tailOffset+4])
}

// Slot decodes the slot at the supplied index (for testing).
func (r *Ring) Slot(idx uint32) Target {
	r.mu.Lock()
	defer r.mu.Unlock()
	base := int(idx) * slotSize
	return Target{
		Sequence:    binary.LittleEndian.Uint32(r.data[base : base+4]),
		MonotonicUS: binary.LittleEndian.Uint64(r.data[base+4 : base+12]),
		AngleDeg:    math.Float32frombits(binary.LittleEndian.Uint32(r.data[base+12 : base+16])),
		RateDegS:    math.Float32frombits(binary.LittleEndian.Uint32(r.data[base+16 : base+20])),
		Curvature:   math.Float32frombits(binary.LittleEndian.Uint32(r.data[base+20 : base+24])),
	}
}

// NowMicro returns the current monotonic time in microseconds.
func NowMicro() uint64 {
	return uint64(time.Now().UnixNano() / 1_000)
}
