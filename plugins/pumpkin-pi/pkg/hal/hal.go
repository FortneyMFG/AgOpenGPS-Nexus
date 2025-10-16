package hal

import (
	"errors"
	"fmt"
	"math"
	"sync"
	"time"

	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/config"
)

// Command represents the normalized actuator command forwarded from NAV.
type Command struct {
	Sequence  uint32
	AngleDeg  float64
	Effort    float64
	Enable    bool
	Timestamp time.Time
}

// Status reports the hardware-facing disposition of the last command.
type Status struct {
	Sequence      uint32
	AppliedEffort float64
	MeasuredAngle float64
	Engaged       bool
	Timestamp     time.Time
}

// HAL applies steering commands using the configured backend.
type HAL interface {
	Apply(Command) (Status, error)
	Close() error
}

var (
	// ErrUnsupportedOperation is returned when a backend cannot perform a requested action.
	ErrUnsupportedOperation = errors.New("hal: unsupported operation")
)

// New constructs the requested HAL backend.
func New(cfg config.HALConfig) (HAL, error) {
	switch cfg.Backend {
	case "pwm":
		return newPWMBackend(cfg.PWM)
	case "gpio":
		return newGPIOBackend(cfg.GPIO)
	case "can":
		return newCANBackend(cfg.CAN)
	default:
		return nil, fmt.Errorf("hal: unknown backend %q", cfg.Backend)
	}
}

// clampEffort bounds the command effort to [0,1].
func clampEffort(v float64) float64 {
	if math.IsNaN(v) {
		return 0
	}
	if v < 0 {
		return 0
	}
	if v > 1 {
		return 1
	}
	return v
}

// baseStatus implements shared bookkeeping.
type baseStatus struct {
	mu     sync.Mutex
	status Status
}

func (b *baseStatus) update(s Status) Status {
	b.mu.Lock()
	defer b.mu.Unlock()
	b.status = s
	return s
}

func (b *baseStatus) snapshot() Status {
	b.mu.Lock()
	defer b.mu.Unlock()
	return b.status
}
