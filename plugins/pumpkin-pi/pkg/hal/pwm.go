package hal

import (
	"errors"
	"fmt"
	"os"
	"path/filepath"
	"strconv"
	"sync"
	"time"

	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/config"
)

type pwmBackend struct {
	baseStatus
	cfg        config.PWMConfig
	root       string
	dutyPath   string
	enablePath string
	mu         sync.Mutex
	simulated  bool
	lastDuty   int
}

func newPWMBackend(cfg config.PWMConfig) (HAL, error) {
	root := cfg.SysfsRoot
	if root == "" {
		root = "/sys/class/pwm"
	}
	b := &pwmBackend{cfg: cfg, root: root, simulated: cfg.Simulate}
	if cfg.PeriodNS <= 0 {
		b.cfg.PeriodNS = 2_000_000
	}
	if !b.simulated {
		if err := b.initialise(); err != nil {
			return nil, err
		}
	}
	return b, nil
}

func (p *pwmBackend) initialise() error {
	chipDir := filepath.Join(p.root, fmt.Sprintf("pwmchip%d", p.cfg.Chip))
	pwmDir := filepath.Join(chipDir, fmt.Sprintf("pwm%d", p.cfg.Channel))
	if _, err := os.Stat(pwmDir); errors.Is(err, os.ErrNotExist) {
		export := filepath.Join(chipDir, "export")
		if err := os.WriteFile(export, []byte(strconv.Itoa(p.cfg.Channel)), 0o644); err != nil {
			return fmt.Errorf("pwm export: %w", err)
		}
		deadline := time.Now().Add(500 * time.Millisecond)
		for {
			if _, err := os.Stat(pwmDir); err == nil {
				break
			}
			if time.Now().After(deadline) {
				return fmt.Errorf("pwm channel %d not available", p.cfg.Channel)
			}
			time.Sleep(10 * time.Millisecond)
		}
	}
	period := filepath.Join(pwmDir, "period")
	if err := os.WriteFile(period, []byte(strconv.Itoa(p.cfg.PeriodNS)), 0o644); err != nil {
		return fmt.Errorf("pwm period: %w", err)
	}
	p.dutyPath = filepath.Join(pwmDir, "duty_cycle")
	p.enablePath = filepath.Join(pwmDir, "enable")
	// Default to disabled until commands arrive.
	_ = os.WriteFile(p.enablePath, []byte("0"), 0o644)
	return nil
}

func (p *pwmBackend) Apply(cmd Command) (Status, error) {
	p.mu.Lock()
	defer p.mu.Unlock()

	effort := clampEffort(cmd.Effort)
	duty := int(float64(p.cfg.PeriodNS) * effort)
	if !cmd.Enable {
		duty = 0
	}

	if p.simulated {
		p.lastDuty = duty
	} else {
		if err := writeFile(p.dutyPath, strconv.Itoa(duty)); err != nil {
			return Status{}, fmt.Errorf("pwm duty_cycle: %w", err)
		}
		enable := "0"
		if cmd.Enable {
			enable = "1"
		}
		if err := writeFile(p.enablePath, enable); err != nil {
			return Status{}, fmt.Errorf("pwm enable: %w", err)
		}
	}

	ts := cmd.Timestamp
	if ts.IsZero() {
		ts = time.Now()
	}
	status := Status{
		Sequence:      cmd.Sequence,
		AppliedEffort: effort,
		MeasuredAngle: cmd.AngleDeg,
		Engaged:       cmd.Enable,
		Timestamp:     ts,
	}
	return p.update(status), nil
}

func (p *pwmBackend) Close() error {
	return nil
}

func writeFile(path, value string) error {
	if path == "" {
		return fmt.Errorf("path not initialised")
	}
	return os.WriteFile(path, []byte(value), 0o644)
}

// LastDuty returns the last simulated duty cycle for tests.
func (p *pwmBackend) LastDuty() int {
	p.mu.Lock()
	defer p.mu.Unlock()
	return p.lastDuty
}
