package config

import (
	"fmt"
	"time"

	"gopkg.in/yaml.v3"
)

// Config captures all runtime settings for the Pumpkin Pi service.
type Config struct {
	NodeID    string          `yaml:"node_id"`
	Roles     []string        `yaml:"roles"`
	Fastpath  FastpathConfig  `yaml:"fastpath"`
	HAL       HALConfig       `yaml:"hal"`
	MQTT      MQTTConfig      `yaml:"mqtt"`
	GRPC      GRPCConfig      `yaml:"grpc"`
	Authority AuthorityConfig `yaml:"authority"`
}

// FastpathConfig controls the shared-memory transport between NAV and steer-ctrl.
type FastpathConfig struct {
	ShmName       string `yaml:"shm_name"`
	SetpointTTLMs int    `yaml:"setpoint_ttl_ms"`
	EventFDName   string `yaml:"eventfd_name"`
}

// Duration converts the TTL to a time.Duration.
func (f FastpathConfig) Duration() time.Duration {
	if f.SetpointTTLMs <= 0 {
		return 250 * time.Millisecond
	}
	return time.Duration(f.SetpointTTLMs) * time.Millisecond
}

// HALConfig selects which hardware backend Pumpkin Pi should operate.
type HALConfig struct {
	Backend string     `yaml:"backend"`
	PWM     PWMConfig  `yaml:"pwm"`
	GPIO    GPIOConfig `yaml:"gpio"`
	CAN     CANConfig  `yaml:"can"`
}

// PWMConfig wires Linux PWM character devices.
type PWMConfig struct {
	Chip      int    `yaml:"chip"`
	Channel   int    `yaml:"channel"`
	PeriodNS  int    `yaml:"period_ns"`
	SysfsRoot string `yaml:"sysfs_root"`
	Simulate  bool   `yaml:"simulate"`
}

// GPIOConfig describes a libgpiod driven output.
type GPIOConfig struct {
	Chip       string `yaml:"chip"`
	Line       int    `yaml:"line"`
	ActiveHigh bool   `yaml:"active_high"`
	Simulate   bool   `yaml:"simulate"`
}

// CANConfig configures a SocketCAN command publisher.
type CANConfig struct {
	Interface     string `yaml:"interface"`
	ArbitrationID uint32 `yaml:"arbitration_id"`
	Simulate      bool   `yaml:"simulate"`
}

// MQTTConfig controls local MQTT loopback mirroring.
type MQTTConfig struct {
	Broker        string `yaml:"broker"`
	ClientID      string `yaml:"client_id"`
	PublishStatus bool   `yaml:"publish_status"`
	Username      string `yaml:"username"`
	Password      string `yaml:"password"`
}

// GRPCConfig controls the gRPC server endpoint.
type GRPCConfig struct {
	Listen string `yaml:"listen"`
}

// AuthorityConfig describes the MQTT authority token workflow.
type AuthorityConfig struct {
	Topic        string        `yaml:"topic"`
	DefaultOwner string        `yaml:"default_owner"`
	HoldInterval time.Duration `yaml:"hold_interval"`
}

// ApplyDefaults populates sane defaults for unset values.
func (c *Config) ApplyDefaults() {
	if c.Fastpath.ShmName == "" {
		c.Fastpath.ShmName = "/aoglink_steer"
	}
	if c.Fastpath.EventFDName == "" {
		c.Fastpath.EventFDName = "/pumpkin-pi-steer"
	}
	if c.Fastpath.SetpointTTLMs == 0 {
		c.Fastpath.SetpointTTLMs = 250
	}
	if c.GRPC.Listen == "" {
		c.GRPC.Listen = ":44111"
	}
	if c.MQTT.ClientID == "" {
		c.MQTT.ClientID = "pumpkin-pi"
	}
	if c.Authority.Topic == "" {
		c.Authority.Topic = "aog/v1/ctrl/authority/steer"
	}
	if c.Authority.DefaultOwner == "" {
		c.Authority.DefaultOwner = "cm5"
	}
	if c.Authority.HoldInterval == 0 {
		c.Authority.HoldInterval = 150 * time.Millisecond
	}
}

// Validate returns an error if the configuration is inconsistent.
func (c Config) Validate() error {
	if c.NodeID == "" {
		return fmt.Errorf("node_id must be specified")
	}
	if c.Fastpath.ShmName == "" {
		return fmt.Errorf("fastpath.shm_name must be specified")
	}
	if c.HAL.Backend == "" {
		return fmt.Errorf("hal.backend must be specified")
	}
	switch c.HAL.Backend {
	case "pwm", "gpio", "can":
	default:
		return fmt.Errorf("unsupported hal backend %q", c.HAL.Backend)
	}
	if c.MQTT.Broker == "" {
		return fmt.Errorf("mqtt.broker must be specified")
	}
	return nil
}

// Parse decodes the YAML payload into Config.
func Parse(raw []byte) (Config, error) {
	var cfg Config
	if err := yaml.Unmarshal(raw, &cfg); err != nil {
		return Config{}, err
	}
	cfg.ApplyDefaults()
	return cfg, cfg.Validate()
}
