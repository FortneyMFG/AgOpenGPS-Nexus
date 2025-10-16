package hal

import (
	"fmt"
	"sync"
	"time"

	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/config"
	gpiocdev "github.com/warthog618/go-gpiocdev"
)

type gpioBackend struct {
	baseStatus
	cfg       config.GPIOConfig
	line      *gpiocdev.Line
	mu        sync.Mutex
	simulated bool
	state     bool
}

func newGPIOBackend(cfg config.GPIOConfig) (HAL, error) {
	backend := &gpioBackend{cfg: cfg, simulated: cfg.Simulate}
	if cfg.Chip == "" {
		backend.cfg.Chip = "gpiochip0"
	}
	if cfg.Simulate {
		return backend, nil
	}
	line, err := gpiocdev.RequestLine(backend.cfg.Chip, backend.cfg.Line, gpiocdev.AsOutput(0))
	if err != nil {
		return nil, fmt.Errorf("gpiocdev request line: %w", err)
	}
	backend.line = line
	return backend, nil
}

func (g *gpioBackend) Apply(cmd Command) (Status, error) {
	g.mu.Lock()
	defer g.mu.Unlock()

	level := 0
	if cmd.Enable {
		level = 1
	}
	if !g.cfg.ActiveHigh {
		if level == 1 {
			level = 0
		} else {
			level = 1
		}
	}

	if g.simulated {
		g.state = level == 1
	} else if g.line != nil {
		if err := g.line.SetValue(level); err != nil {
			return Status{}, fmt.Errorf("gpiocdev set value: %w", err)
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
	return g.update(status), nil
}

func (g *gpioBackend) Close() error {
	g.mu.Lock()
	defer g.mu.Unlock()
	if g.line != nil {
		g.line.Close()
		g.line = nil
	}
	return nil
}

func (g *gpioBackend) State() bool {
	g.mu.Lock()
	defer g.mu.Unlock()
	return g.state
}
