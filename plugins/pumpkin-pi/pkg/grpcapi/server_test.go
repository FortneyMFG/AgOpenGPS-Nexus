package grpcapi

import (
	"context"
	"fmt"
	"sync"
	"testing"
	"time"

	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/config"
	pumpkinpipb "github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/grpcapi/pb"
	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/hal"
	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/mqtt"
	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/shm"
	"google.golang.org/grpc/metadata"
	"google.golang.org/protobuf/types/known/emptypb"
)

func TestServerSetSteerTarget(t *testing.T) {
	ring, err := shm.Open("/srv_ring", "/srv_evt")
	if err != nil {
		t.Fatalf("open ring: %v", err)
	}
	defer ring.Close()
	initialHead := ring.HeadIndex()

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
	head := ring.HeadIndex()
	if head == initialHead {
		t.Fatalf("ring head did not advance")
	}
	slot := ring.Slot(head)
	if slot.Sequence != req.Sequence {
		t.Fatalf("ring slot sequence=%d want %d", slot.Sequence, req.Sequence)
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

func TestServerTTLExpiredDrivesNeutral(t *testing.T) {
	halBackend := newFakeHAL()
	pub := &fakePublisher{}
	server := NewServer(nil, halBackend, pub, config.FastpathConfig{SetpointTTLMs: 10}, config.AuthorityConfig{Topic: "aog/v1/ctrl/authority/steer", DefaultOwner: "cm5", HoldInterval: 150 * time.Millisecond})

	req := &pumpkinpipb.SetSteerTargetRequest{Sequence: 9, MonotonicTimeUs: 1}
	if _, err := server.SetSteerTarget(context.Background(), req); err != nil {
		t.Fatalf("SetSteerTarget: %v", err)
	}

	if halBackend.Calls() != 1 {
		t.Fatalf("expected hal apply, calls=%d", halBackend.Calls())
	}
	cmd := halBackend.LastCommand()
	if cmd.Sequence != req.Sequence {
		t.Fatalf("sequence mismatch got %d want %d", cmd.Sequence, req.Sequence)
	}
	if cmd.AngleDeg != 0 {
		t.Fatalf("angle deg=%f want 0", cmd.AngleDeg)
	}
	if cmd.Effort != 0 {
		t.Fatalf("effort=%f want 0", cmd.Effort)
	}
	if cmd.Enable {
		t.Fatalf("expired command should disable output")
	}

	status := server.snapshotStatus()
	if status == nil {
		t.Fatalf("expected status snapshot")
	}
	if status.Engaged {
		t.Fatalf("neutral status should not be engaged")
	}
}

func TestServerMQTTBrokerRestartResilience(t *testing.T) {
	ring, err := shm.Open("/srv_ring_broker", "/srv_evt_broker")
	if err != nil {
		t.Fatalf("open ring: %v", err)
	}
	defer ring.Close()

	halBackend := newFakeHAL()
	pub := &fakePublisher{failNextTarget: true}
	server := NewServer(ring, halBackend, pub, config.FastpathConfig{SetpointTTLMs: 250}, config.AuthorityConfig{Topic: "aog/v1/ctrl/authority/steer", DefaultOwner: "cm5", HoldInterval: 150 * time.Millisecond})

	initialHead := ring.HeadIndex()

	req1 := &pumpkinpipb.SetSteerTargetRequest{Sequence: 10, TargetWheelAngleDeg: 3.5, ControllerOutput: 0.4, Enable: true, MonotonicTimeUs: shm.NowMicro()}
	if _, err := server.SetSteerTarget(context.Background(), req1); err != nil {
		t.Fatalf("SetSteerTarget first: %v", err)
	}
	head1 := ring.HeadIndex()
	if head1 == initialHead {
		t.Fatalf("head did not advance on first publish")
	}
	slot1 := ring.Slot(head1)
	if slot1.Sequence != req1.Sequence {
		t.Fatalf("ring slot sequence=%d want %d", slot1.Sequence, req1.Sequence)
	}

	req2 := &pumpkinpipb.SetSteerTargetRequest{Sequence: 11, TargetWheelAngleDeg: 4.0, ControllerOutput: 0.6, Enable: true, MonotonicTimeUs: shm.NowMicro()}
	if _, err := server.SetSteerTarget(context.Background(), req2); err != nil {
		t.Fatalf("SetSteerTarget second: %v", err)
	}
	head2 := ring.HeadIndex()
	if head2 == head1 {
		t.Fatalf("head did not advance on second publish")
	}
	slot2 := ring.Slot(head2)
	if slot2.Sequence != req2.Sequence {
		t.Fatalf("ring slot sequence=%d want %d", slot2.Sequence, req2.Sequence)
	}

	targets := pub.Targets()
	if len(targets) != 1 {
		t.Fatalf("expected 1 mirrored target after recovery, got %d", len(targets))
	}
	if targets[0].Sequence != req2.Sequence {
		t.Fatalf("mirrored sequence=%d want %d", targets[0].Sequence, req2.Sequence)
	}

	statuses := pub.Statuses()
	if len(statuses) == 0 {
		t.Fatalf("expected status mirrors")
	}
	if statuses[len(statuses)-1].Sequence != req2.Sequence {
		t.Fatalf("status sequence=%d want %d", statuses[len(statuses)-1].Sequence, req2.Sequence)
	}
}

func TestServerExternalSubscriberNonBlocking(t *testing.T) {
	ring, err := shm.Open("/srv_ring_sub", "/srv_evt_sub")
	if err != nil {
		t.Fatalf("open ring: %v", err)
	}
	defer ring.Close()

	halBackend := newFakeHAL()
	pub := &fakePublisher{}
	server := NewServer(ring, halBackend, pub, config.FastpathConfig{SetpointTTLMs: 250}, config.AuthorityConfig{Topic: "aog/v1/ctrl/authority/steer", DefaultOwner: "cm5", HoldInterval: 150 * time.Millisecond})

	ch := make(chan *pumpkinpipb.SteerStatus, 1)
	ch <- &pumpkinpipb.SteerStatus{Sequence: 1}
	id := server.addSubscriber(ch)
	defer server.removeSubscriber(id)

	startHead := ring.HeadIndex()
	done := make(chan struct{})
	req := &pumpkinpipb.SetSteerTargetRequest{Sequence: 21, TargetWheelAngleDeg: 2.5, ControllerOutput: 0.55, Enable: true, MonotonicTimeUs: shm.NowMicro()}
	go func() {
		defer close(done)
		if _, err := server.SetSteerTarget(context.Background(), req); err != nil {
			t.Errorf("SetSteerTarget: %v", err)
		}
	}()

	select {
	case <-done:
	case <-time.After(100 * time.Millisecond):
		t.Fatal("SetSteerTarget blocked by subscriber backpressure")
	}

	head := ring.HeadIndex()
	if head == startHead {
		t.Fatalf("ring head did not advance")
	}
	slot := ring.Slot(head)
	if slot.Sequence != req.Sequence {
		t.Fatalf("ring slot sequence=%d want %d", slot.Sequence, req.Sequence)
	}

	status := server.snapshotStatus()
	if status == nil || status.Sequence != req.Sequence {
		t.Fatalf("snapshot mismatch %+v", status)
	}

	stream := newFakeStatusStream()
	errCh := make(chan error, 1)
	go func() {
		errCh <- server.StreamSteerStatus(&emptypb.Empty{}, stream)
	}()

	start := time.Now()
	if !stream.WaitForMessage(100 * time.Millisecond) {
		t.Fatal("stream join timed out")
	}
	if latency := time.Since(start); latency > 50*time.Millisecond {
		t.Fatalf("stream took too long: %s", latency)
	}

	stream.Cancel()

	select {
	case err := <-errCh:
		if err != context.Canceled {
			t.Fatalf("StreamSteerStatus err=%v", err)
		}
	case <-time.After(100 * time.Millisecond):
		t.Fatal("stream shutdown timed out")
	}

	msgs := stream.Messages()
	if len(msgs) == 0 {
		t.Fatalf("expected stream message")
	}
	if msgs[0].Sequence != req.Sequence {
		t.Fatalf("stream sequence=%d want %d", msgs[0].Sequence, req.Sequence)
	}
}

type fakePublisher struct {
	mu             sync.Mutex
	failNextTarget bool
	targets        []shm.Target
	statuses       []hal.Status
	health         []string
	authorities    []authorityRecord
}

type authorityRecord struct {
	topic   string
	owner   string
	expires time.Time
}

func (p *fakePublisher) PublishSteerTarget(target shm.Target) error {
	p.mu.Lock()
	defer p.mu.Unlock()
	if p.failNextTarget {
		p.failNextTarget = false
		return fmt.Errorf("broker unavailable")
	}
	p.targets = append(p.targets, target)
	return nil
}

func (p *fakePublisher) PublishSteerStatus(status hal.Status) error {
	p.mu.Lock()
	defer p.mu.Unlock()
	p.statuses = append(p.statuses, status)
	return nil
}

func (p *fakePublisher) PublishHealth(state string) error {
	p.mu.Lock()
	defer p.mu.Unlock()
	p.health = append(p.health, state)
	return nil
}

func (p *fakePublisher) PublishAuthority(topic, owner string, expires time.Time) error {
	p.mu.Lock()
	defer p.mu.Unlock()
	p.authorities = append(p.authorities, authorityRecord{topic: topic, owner: owner, expires: expires})
	return nil
}

func (p *fakePublisher) Targets() []shm.Target {
	p.mu.Lock()
	defer p.mu.Unlock()
	out := make([]shm.Target, len(p.targets))
	copy(out, p.targets)
	return out
}

func (p *fakePublisher) Statuses() []hal.Status {
	p.mu.Lock()
	defer p.mu.Unlock()
	out := make([]hal.Status, len(p.statuses))
	copy(out, p.statuses)
	return out
}

type fakeHAL struct {
	mu    sync.Mutex
	calls int
	last  hal.Command
}

func newFakeHAL() *fakeHAL {
	return &fakeHAL{}
}

func (f *fakeHAL) Apply(cmd hal.Command) (hal.Status, error) {
	f.mu.Lock()
	defer f.mu.Unlock()
	f.calls++
	f.last = cmd
	return hal.Status{Sequence: cmd.Sequence, AppliedEffort: cmd.Effort, MeasuredAngle: cmd.AngleDeg, Engaged: cmd.Enable, Timestamp: time.Now()}, nil
}

func (f *fakeHAL) Close() error { return nil }

func (f *fakeHAL) Calls() int {
	f.mu.Lock()
	defer f.mu.Unlock()
	return f.calls
}

func (f *fakeHAL) LastCommand() hal.Command {
	f.mu.Lock()
	defer f.mu.Unlock()
	return f.last
}

type fakeStatusStream struct {
	ctx    context.Context
	cancel context.CancelFunc
	mu     sync.Mutex
	msgs   []*pumpkinpipb.SteerStatus
	sent   chan struct{}
	once   sync.Once
}

func newFakeStatusStream() *fakeStatusStream {
	ctx, cancel := context.WithCancel(context.Background())
	return &fakeStatusStream{ctx: ctx, cancel: cancel, sent: make(chan struct{})}
}

func (f *fakeStatusStream) SetHeader(metadata.MD) error { return nil }

func (f *fakeStatusStream) SendHeader(metadata.MD) error { return nil }

func (f *fakeStatusStream) SetTrailer(metadata.MD) {}

func (f *fakeStatusStream) Context() context.Context { return f.ctx }

func (f *fakeStatusStream) Send(msg *pumpkinpipb.SteerStatus) error {
	f.mu.Lock()
	f.msgs = append(f.msgs, msg)
	f.mu.Unlock()
	f.once.Do(func() { close(f.sent) })
	return nil
}

func (f *fakeStatusStream) SendMsg(m interface{}) error { return nil }

func (f *fakeStatusStream) RecvMsg(m interface{}) error { return nil }

func (f *fakeStatusStream) Messages() []*pumpkinpipb.SteerStatus {
	f.mu.Lock()
	defer f.mu.Unlock()
	out := make([]*pumpkinpipb.SteerStatus, len(f.msgs))
	copy(out, f.msgs)
	return out
}

func (f *fakeStatusStream) WaitForMessage(timeout time.Duration) bool {
	select {
	case <-f.sent:
		return true
	case <-time.After(timeout):
		return false
	}
}

func (f *fakeStatusStream) Cancel() {
	f.cancel()
}
