package mqtt

import (
	"encoding/json"
	"fmt"
	"strings"
	"sync"
	"time"

	gomqtt "github.com/eclipse/paho.mqtt.golang"

	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/config"
	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/hal"
	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/shm"
)

const (
	steerTargetTopic = "aog/v1/bus/nav/steer_target"
	steerStatusTopic = "aog/v1/bus/ctrl/steer_status"
	healthTopic      = "aog/v1/bus/pumpkin/health"
)

// Publisher mirrors Pumpkin Pi telemetry to the local MQTT broker for observability.
type Publisher struct {
	cfg      config.MQTTConfig
	client   gomqtt.Client
	disabled bool
	mu       sync.Mutex
}

// NewPublisher connects to the configured MQTT broker. When PublishStatus is false the
// publisher is created in disabled mode so tests can run without a broker.
func NewPublisher(cfg config.MQTTConfig) (*Publisher, error) {
	p := &Publisher{cfg: cfg}
	if !cfg.PublishStatus {
		p.disabled = true
		return p, nil
	}
	broker := cfg.Broker
	if !strings.Contains(broker, "://") {
		broker = "tcp://" + broker
	}
	opts := gomqtt.NewClientOptions().AddBroker(broker).SetClientID(cfg.ClientID)
	if cfg.Username != "" {
		opts.SetUsername(cfg.Username)
		opts.SetPassword(cfg.Password)
	}
	opts.SetOrderMatters(false)
	client := gomqtt.NewClient(opts)
	token := client.Connect()
	if ok := token.WaitTimeout(5 * time.Second); !ok {
		return nil, fmt.Errorf("mqtt connect timeout")
	}
	if err := token.Error(); err != nil {
		return nil, fmt.Errorf("mqtt connect: %w", err)
	}
	p.client = client
	return p, nil
}

// Close terminates the MQTT session.
func (p *Publisher) Close() {
	p.mu.Lock()
	defer p.mu.Unlock()
	if p.client != nil {
		p.client.Disconnect(250)
		p.client = nil
	}
}

// PublishSteerTarget mirrors the latest steer target to the loopback broker.
func (p *Publisher) PublishSteerTarget(target shm.Target) error {
	if p.disabled {
		return nil
	}
	payload := map[string]any{
		"seq":          target.Sequence,
		"angle_deg":    target.AngleDeg,
		"rate_deg_s":   target.RateDegS,
		"curvature":    target.Curvature,
		"monotonic_us": target.MonotonicUS,
	}
	return p.publish(steerTargetTopic, payload)
}

// PublishSteerStatus mirrors actuator status back to the broker.
func (p *Publisher) PublishSteerStatus(status hal.Status) error {
	if p.disabled {
		return nil
	}
	payload := map[string]any{
		"seq":            status.Sequence,
		"applied_effort": status.AppliedEffort,
		"measured_deg":   status.MeasuredAngle,
		"engaged":        status.Engaged,
		"timestamp":      status.Timestamp.UTC().Format(time.RFC3339Nano),
	}
	return p.publish(steerStatusTopic, payload)
}

// PublishHealth emits a simple health heartbeat.
func (p *Publisher) PublishHealth(state string) error {
	if p.disabled {
		return nil
	}
	payload := map[string]any{
		"state":     state,
		"timestamp": time.Now().UTC().Format(time.RFC3339Nano),
	}
	return p.publish(healthTopic, payload)
}

// PublishAuthority mirrors authority ownership updates on the configured topic.
func (p *Publisher) PublishAuthority(topic, owner string, expires time.Time) error {
	if p.disabled {
		return nil
	}
	payload := map[string]any{
		"owner":   owner,
		"expires": expires.UTC().Format(time.RFC3339Nano),
	}
	return p.publish(topic, payload)
}

func (p *Publisher) publish(topic string, payload map[string]any) error {
	if p.disabled {
		return nil
	}
	p.mu.Lock()
	client := p.client
	p.mu.Unlock()
	if client == nil {
		return fmt.Errorf("mqtt: client not connected")
	}
	buf, err := json.Marshal(payload)
	if err != nil {
		return fmt.Errorf("mqtt marshal: %w", err)
	}
	token := client.Publish(topic, 1, true, buf)
	if ok := token.WaitTimeout(2 * time.Second); !ok {
		return fmt.Errorf("mqtt publish timeout")
	}
	return token.Error()
}
