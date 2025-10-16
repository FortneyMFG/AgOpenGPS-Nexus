package mqtt

import (
	"testing"
	"time"

	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/config"
	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/hal"
	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/shm"
)

func TestPublisherDisabled(t *testing.T) {
	pub, err := NewPublisher(config.MQTTConfig{Broker: "localhost:1883", PublishStatus: false})
	if err != nil {
		t.Fatalf("NewPublisher: %v", err)
	}
	defer pub.Close()
	if err := pub.PublishSteerTarget(shm.Target{Sequence: 1}); err != nil {
		t.Fatalf("publish target: %v", err)
	}
	if err := pub.PublishSteerStatus(hal.Status{Sequence: 1}); err != nil {
		t.Fatalf("publish status: %v", err)
	}
	if err := pub.PublishHealth("ok"); err != nil {
		t.Fatalf("publish health: %v", err)
	}
	if err := pub.PublishAuthority("aog/v1/ctrl/authority/steer", "cm5", time.Now()); err != nil {
		t.Fatalf("publish authority: %v", err)
	}
}
