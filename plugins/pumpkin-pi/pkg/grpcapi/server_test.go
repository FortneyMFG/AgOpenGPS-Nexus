package grpcapi

import (
	"context"
	"testing"
	"time"

	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/config"
	pumpkinpipb "github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/grpcapi/pb"
	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/hal"
	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/mqtt"
	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/shm"
)

func TestServerSetSteerTarget(t *testing.T) {
	ring, err := shm.Open("/srv_ring", "/srv_evt")
	if err != nil {
		t.Fatalf("open ring: %v", err)
	}
	defer ring.Close()
	halBackend, err := hal.New(config.HALConfig{Backend: "pwm", PWM: config.PWMConfig{PeriodNS: 1_000_000, Simulate: true}})
	if err != nil {
		t.Fatalf("hal.New: %v", err)
	}
	defer halBackend.Close()
	pub, err := mqtt.NewPublisher(config.MQTTConfig{Broker: "localhost:1883", PublishStatus: false})
	if err != nil {
		t.Fatalf("mqtt: %v", err)
	}
	defer pub.Close()
	server := NewServer(ring, halBackend, pub, config.FastpathConfig{SetpointTTLMs: 250}, config.AuthorityConfig{Topic: "aog/v1/ctrl/authority/steer", DefaultOwner: "cm5", HoldInterval: 150 * time.Millisecond})
	req := &pumpkinpipb.SetSteerTargetRequest{Sequence: 1, TargetWheelAngleDeg: 4.2, ControllerOutput: 0.5, Enable: true, MonotonicTimeUs: shm.NowMicro()}
	if _, err := server.SetSteerTarget(context.Background(), req); err != nil {
		t.Fatalf("SetSteerTarget: %v", err)
	}
	if ring.HeadIndex() != 1 {
		t.Fatalf("head index = %d", ring.HeadIndex())
	}
	status := server.snapshotStatus()
	if status == nil || !status.Engaged {
		t.Fatalf("expected engaged status, got %+v", status)
	}
}

func TestServerSetSteerTargetTTL(t *testing.T) {
	ring, err := shm.Open("/srv_ring_ttl", "/srv_evt_ttl")
	if err != nil {
		t.Fatalf("open ring: %v", err)
	}
	defer ring.Close()
	halBackend, err := hal.New(config.HALConfig{Backend: "pwm", PWM: config.PWMConfig{PeriodNS: 1_000_000, Simulate: true}})
	if err != nil {
		t.Fatalf("hal.New: %v", err)
	}
	defer halBackend.Close()
	pub, err := mqtt.NewPublisher(config.MQTTConfig{Broker: "localhost:1883", PublishStatus: false})
	if err != nil {
		t.Fatalf("mqtt: %v", err)
	}
	defer pub.Close()
	server := NewServer(ring, halBackend, pub, config.FastpathConfig{SetpointTTLMs: 10}, config.AuthorityConfig{Topic: "aog/v1/ctrl/authority/steer", DefaultOwner: "cm5", HoldInterval: 150 * time.Millisecond})
	req := &pumpkinpipb.SetSteerTargetRequest{Sequence: 2, MonotonicTimeUs: 1}
	if _, err := server.SetSteerTarget(context.Background(), req); err != nil {
		t.Fatalf("SetSteerTarget: %v", err)
	}
	status := server.snapshotStatus()
	if status == nil {
		t.Fatalf("expected status")
	}
	if status.Engaged {
		t.Fatalf("expired command should not engage")
	}
}

func TestServerAuthorityClaim(t *testing.T) {
	ring, err := shm.Open("/srv_ring_auth", "/srv_evt_auth")
	if err != nil {
		t.Fatalf("open ring: %v", err)
	}
	defer ring.Close()
	halBackend, err := hal.New(config.HALConfig{Backend: "pwm", PWM: config.PWMConfig{PeriodNS: 1_000_000, Simulate: true}})
	if err != nil {
		t.Fatalf("hal.New: %v", err)
	}
	defer halBackend.Close()
	pub, err := mqtt.NewPublisher(config.MQTTConfig{Broker: "localhost:1883", PublishStatus: false})
	if err != nil {
		t.Fatalf("mqtt: %v", err)
	}
	defer pub.Close()
	server := NewServer(ring, halBackend, pub, config.FastpathConfig{SetpointTTLMs: 250}, config.AuthorityConfig{Topic: "aog/v1/ctrl/authority/steer", DefaultOwner: "cm5", HoldInterval: 10 * time.Millisecond})
	if _, err := server.ClaimAuthority(context.Background(), &pumpkinpipb.AuthorityClaim{Owner: "external"}); err != nil {
		t.Fatalf("ClaimAuthority: %v", err)
	}
	if owner := server.authorityOwner; owner != "external" {
		t.Fatalf("owner=%s", owner)
	}
	time.Sleep(20 * time.Millisecond)
	server.ensureAuthorityFresh()
	if owner := server.authorityOwner; owner != "cm5" {
		t.Fatalf("owner not reset: %s", owner)
	}
}
