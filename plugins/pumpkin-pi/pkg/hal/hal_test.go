package hal

import (
	"testing"
	"time"

	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/config"
)

func TestPWMBackendSimulate(t *testing.T) {
	backend, err := newPWMBackend(config.PWMConfig{PeriodNS: 1_000_000, Simulate: true})
	if err != nil {
		t.Fatalf("newPWMBackend: %v", err)
	}
	defer backend.Close()
	cmd := Command{Sequence: 3, AngleDeg: 7.5, Effort: 0.6, Enable: true, Timestamp: time.Now()}
	status, err := backend.Apply(cmd)
	if err != nil {
		t.Fatalf("apply: %v", err)
	}
	if status.AppliedEffort < 0.59 || status.AppliedEffort > 0.61 {
		t.Fatalf("unexpected effort %.2f", status.AppliedEffort)
	}
	pwm := backend.(*pwmBackend)
	if got, want := pwm.LastDuty(), 600000; got != want {
		t.Fatalf("duty=%d want %d", got, want)
	}
}

func TestGPIOBackendSimulate(t *testing.T) {
	backend, err := newGPIOBackend(config.GPIOConfig{Chip: "gpiochip0", Line: 1, ActiveHigh: true, Simulate: true})
	if err != nil {
		t.Fatalf("newGPIOBackend: %v", err)
	}
	defer backend.Close()
	cmd := Command{Sequence: 1, Enable: true}
	if _, err := backend.Apply(cmd); err != nil {
		t.Fatalf("apply: %v", err)
	}
	gpio := backend.(*gpioBackend)
	if !gpio.State() {
		t.Fatalf("gpio state not high")
	}
	cmd.Enable = false
	if _, err := backend.Apply(cmd); err != nil {
		t.Fatalf("apply disable: %v", err)
	}
	if gpio.State() {
		t.Fatalf("gpio state not low")
	}
}

func TestCANBackendSimulate(t *testing.T) {
	backend, err := newCANBackend(config.CANConfig{Interface: "can0", ArbitrationID: 0x120, Simulate: true})
	if err != nil {
		t.Fatalf("newCANBackend: %v", err)
	}
	defer backend.Close()
	cmd := Command{Sequence: 42, AngleDeg: -3.25, Effort: 0.4, Enable: true}
	if _, err := backend.Apply(cmd); err != nil {
		t.Fatalf("apply: %v", err)
	}
    frame := backend.(*canBackend).LastFrame()
    if frame.ID != 0x120 {
        t.Fatalf("frame id %x want 0x120", frame.ID)
    }
    if frame.Length != 6 {
        t.Fatalf("frame length %d", frame.Length)
    }
    seq := uint16(frame.Data[0])<<8 | uint16(frame.Data[1])
    if seq != 42 {
        t.Fatalf("sequence=%d", seq)
    }
	angle := int16(uint16(frame.Data[2])<<8 | uint16(frame.Data[3]))
	if angle != -325 {
		t.Fatalf("angle=%d", angle)
	}
	if frame.Data[4] == 0 {
		t.Fatalf("effort not encoded")
	}
}
