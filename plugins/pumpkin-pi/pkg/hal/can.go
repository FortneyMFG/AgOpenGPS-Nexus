package hal

import (
	"encoding/binary"
	"fmt"
	"math"
	"net"
	"sync"
	"time"

	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/config"
	"golang.org/x/sys/unix"
)

type canBackend struct {
	baseStatus
	cfg       config.CANConfig
	fd        int
	ifindex   int
	mu        sync.Mutex
	simulated bool
	lastFrame canFrame
}

type canFrame struct {
	ID      uint32
	Length  uint8
	padding [3]byte
	Data    [8]byte
}

func newCANBackend(cfg config.CANConfig) (HAL, error) {
	backend := &canBackend{cfg: cfg, simulated: cfg.Simulate}
	if backend.cfg.Interface == "" {
		backend.cfg.Interface = "can0"
	}
	if backend.cfg.ArbitrationID == 0 {
		backend.cfg.ArbitrationID = 0x180
	}
	if backend.simulated {
		return backend, nil
	}
	iface, err := net.InterfaceByName(backend.cfg.Interface)
	if err != nil {
		return nil, fmt.Errorf("can interface %s: %w", backend.cfg.Interface, err)
	}
	fd, err := unix.Socket(unix.AF_CAN, unix.SOCK_RAW, unix.CAN_RAW)
	if err != nil {
		return nil, fmt.Errorf("can socket: %w", err)
	}
	addr := &unix.SockaddrCAN{Ifindex: iface.Index}
	if err := unix.Bind(fd, addr); err != nil {
		unix.Close(fd)
		return nil, fmt.Errorf("can bind: %w", err)
	}
	backend.fd = fd
	backend.ifindex = iface.Index
	return backend, nil
}

func (c *canBackend) Apply(cmd Command) (Status, error) {
	c.mu.Lock()
	defer c.mu.Unlock()

	frame := encodeSteerCommand(c.cfg.ArbitrationID, cmd)
	if c.simulated {
		c.lastFrame = frame
	} else if c.fd != 0 {
		buf := marshalCANFrame(frame)
		addr := &unix.SockaddrCAN{Ifindex: c.ifindex}
		if err := unix.Sendto(c.fd, buf, 0, addr); err != nil {
			return Status{}, fmt.Errorf("can send: %w", err)
		}
	}
	ts := cmd.Timestamp
	if ts.IsZero() {
		ts = time.Now()
	}
	status := Status{
		Sequence:      cmd.Sequence,
		AppliedEffort: clampEffort(cmd.Effort),
		MeasuredAngle: cmd.AngleDeg,
		Engaged:       cmd.Enable,
		Timestamp:     ts,
	}
	return c.update(status), nil
}

func (c *canBackend) Close() error {
	c.mu.Lock()
	defer c.mu.Unlock()
	if c.fd != 0 {
		unix.Close(c.fd)
		c.fd = 0
	}
	return nil
}

func (c *canBackend) LastFrame() canFrame {
	c.mu.Lock()
	defer c.mu.Unlock()
	return c.lastFrame
}

func encodeSteerCommand(arbitrationID uint32, cmd Command) canFrame {
	var data [8]byte
	binary.BigEndian.PutUint16(data[0:2], uint16(cmd.Sequence))
	angle := int16(math.Round(cmd.AngleDeg * 100))
	binary.BigEndian.PutUint16(data[2:4], uint16(angle))
	effort := uint8(math.Round(clampEffort(cmd.Effort) * 255))
	data[4] = effort
	if cmd.Enable {
		data[5] = 1
	}
	return canFrame{ID: arbitrationID, Length: 6, Data: data}
}

func marshalCANFrame(frame canFrame) []byte {
	buf := make([]byte, 16)
	binary.LittleEndian.PutUint32(buf[0:4], frame.ID)
	buf[4] = frame.Length
	copy(buf[8:], frame.Data[:])
	return buf
}
