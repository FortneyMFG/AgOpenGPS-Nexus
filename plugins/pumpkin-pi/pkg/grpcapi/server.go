package grpcapi

import (
	"context"
	"fmt"
	"sync"
	"time"

	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/config"
	pumpkinpipb "github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/grpcapi/pb"
	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/hal"
	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/mqtt"
	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/shm"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/status"
	"google.golang.org/protobuf/types/known/emptypb"
)

// Server exposes the Pumpkin Pi gRPC API and bridges fast-path events to HAL + MQTT.
type Server struct {
	pumpkinpipb.UnimplementedPumpkinPiServiceServer

	ring *shm.Ring
	hal  hal.HAL
	mqtt *mqtt.Publisher
	ttl  time.Duration

	mu               sync.Mutex
	subscribers      map[int]chan *pumpkinpipb.SteerStatus
	nextSubID        int
	lastStatus       *pumpkinpipb.SteerStatus
	authorityOwner   string
	authorityUpdated time.Time
	authorityHold    time.Duration
	authorityDefault string
	authorityTopic   string
}

// NewServer builds a Server using the supplied dependencies.
func NewServer(r *shm.Ring, h hal.HAL, pub *mqtt.Publisher, fastpath config.FastpathConfig, authority config.AuthorityConfig) *Server {
	s := &Server{
		ring:             r,
		hal:              h,
		mqtt:             pub,
		ttl:              fastpath.Duration(),
		subscribers:      make(map[int]chan *pumpkinpipb.SteerStatus),
		authorityOwner:   authority.DefaultOwner,
		authorityDefault: authority.DefaultOwner,
		authorityHold:    authority.HoldInterval,
		authorityTopic:   authority.Topic,
		authorityUpdated: time.Now(),
	}
	if s.authorityHold == 0 {
		s.authorityHold = 150 * time.Millisecond
	}
	if s.mqtt != nil {
		_ = s.mqtt.PublishHealth("ready")
		_ = s.mqtt.PublishAuthority(s.authorityTopic, s.authorityOwner, time.Now().Add(s.authorityHold))
	}
	return s
}

// SetSteerTarget writes the latest target to shared memory, applies it through HAL, and mirrors telemetry.
func (s *Server) SetSteerTarget(ctx context.Context, req *pumpkinpipb.SetSteerTargetRequest) (*emptypb.Empty, error) {
	if req == nil {
		return nil, status.Error(codes.InvalidArgument, "request required")
	}
	if s.isExpired(req.MonotonicTimeUs) {
		// Expired commands drive neutral output.
		status := hal.Status{Sequence: req.Sequence, Timestamp: time.Now()}
		if s.hal != nil {
			if halStatus, err := s.hal.Apply(hal.Command{Sequence: req.Sequence, AngleDeg: 0, Effort: 0, Enable: false, Timestamp: time.Now()}); err == nil {
				status = halStatus
			}
		}
		s.publishStatus(status, req.MonotonicTimeUs)
		return &emptypb.Empty{}, nil
	}

	target := shm.Target{
		Sequence:    req.Sequence,
		MonotonicUS: req.MonotonicTimeUs,
		AngleDeg:    float32(req.TargetWheelAngleDeg),
		RateDegS:    0,
		Curvature:   0,
	}
	if s.ring != nil {
		if err := s.ring.Push(target); err != nil {
			return nil, status.Errorf(codes.Internal, "shm push: %v", err)
		}
	}
	if s.mqtt != nil {
		_ = s.mqtt.PublishSteerTarget(target)
	}

	var halStatus hal.Status
	var err error
	if s.hal != nil {
		halStatus, err = s.hal.Apply(hal.Command{
			Sequence:  req.Sequence,
			AngleDeg:  req.TargetWheelAngleDeg,
			Effort:    req.ControllerOutput,
			Enable:    req.Enable,
			Timestamp: time.Now(),
		})
		if err != nil {
			return nil, status.Errorf(codes.Internal, "hal apply: %v", err)
		}
	} else {
		halStatus = hal.Status{Sequence: req.Sequence, AppliedEffort: req.ControllerOutput, MeasuredAngle: req.TargetWheelAngleDeg, Engaged: req.Enable, Timestamp: time.Now()}
	}
	s.publishStatus(halStatus, req.MonotonicTimeUs)
	s.ensureAuthorityFresh()
	return &emptypb.Empty{}, nil
}

// StreamSteerStatus registers a stream subscriber for HAL status updates.
func (s *Server) StreamSteerStatus(_ *emptypb.Empty, stream pumpkinpipb.PumpkinPiService_StreamSteerStatusServer) error {
	ch := make(chan *pumpkinpipb.SteerStatus, 4)
	id := s.addSubscriber(ch)
	defer s.removeSubscriber(id)

	if status := s.snapshotStatus(); status != nil {
		if err := stream.Send(status); err != nil {
			return err
		}
	}
	for {
		select {
		case <-stream.Context().Done():
			return stream.Context().Err()
		case msg := <-ch:
			if err := stream.Send(msg); err != nil {
				return err
			}
		}
	}
}

// ClaimAuthority updates the active controller owner and mirrors to MQTT.
func (s *Server) ClaimAuthority(ctx context.Context, req *pumpkinpipb.AuthorityClaim) (*emptypb.Empty, error) {
	if req == nil || req.Owner == "" {
		return nil, status.Error(codes.InvalidArgument, "owner required")
	}
	s.mu.Lock()
	s.authorityOwner = req.Owner
	s.authorityUpdated = time.Now()
	s.mu.Unlock()
	if s.mqtt != nil {
		_ = s.mqtt.PublishAuthority(s.authorityTopic, req.Owner, time.Now().Add(s.authorityHold))
	}
	return &emptypb.Empty{}, nil
}

func (s *Server) addSubscriber(ch chan *pumpkinpipb.SteerStatus) int {
	s.mu.Lock()
	defer s.mu.Unlock()
	id := s.nextSubID
	s.nextSubID++
	s.subscribers[id] = ch
	return id
}

func (s *Server) removeSubscriber(id int) {
	s.mu.Lock()
	defer s.mu.Unlock()
	if ch, ok := s.subscribers[id]; ok {
		close(ch)
		delete(s.subscribers, id)
	}
}

func (s *Server) publishStatus(status hal.Status, monotonicUS uint64) {
	pbStatus := &pumpkinpipb.SteerStatus{
		Sequence:              status.Sequence,
		MeasuredWheelAngleDeg: status.MeasuredAngle,
		AppliedEffort:         status.AppliedEffort,
		Engaged:               status.Engaged,
		MonotonicTimeUs:       monotonicUS,
	}
	if status.Timestamp.UnixNano() > 0 {
		pbStatus.MonotonicTimeUs = uint64(status.Timestamp.UnixNano() / 1_000)
	}
	s.mu.Lock()
	s.lastStatus = pbStatus
	for _, ch := range s.subscribers {
		select {
		case ch <- pbStatus:
		default:
		}
	}
	s.mu.Unlock()
	if s.mqtt != nil {
		_ = s.mqtt.PublishSteerStatus(status)
	}
}

func (s *Server) snapshotStatus() *pumpkinpipb.SteerStatus {
	s.mu.Lock()
	defer s.mu.Unlock()
	if s.lastStatus == nil {
		return nil
	}
	clone := *s.lastStatus
	return &clone
}

func (s *Server) isExpired(monotonicUS uint64) bool {
	if monotonicUS == 0 || s.ttl <= 0 {
		return false
	}
	now := shm.NowMicro()
	if now <= monotonicUS {
		return false
	}
	age := time.Duration(now-monotonicUS) * time.Microsecond
	return age > s.ttl
}

func (s *Server) ensureAuthorityFresh() {
	s.mu.Lock()
	defer s.mu.Unlock()
	if time.Since(s.authorityUpdated) <= s.authorityHold {
		return
	}
	if s.authorityOwner == s.authorityDefault {
		return
	}
	s.authorityOwner = s.authorityDefault
	s.authorityUpdated = time.Now()
	if s.mqtt != nil {
		_ = s.mqtt.PublishAuthority(s.authorityTopic, s.authorityOwner, time.Now().Add(s.authorityHold))
	}
}

// ErrNotReady is returned when dependencies are missing.
var ErrNotReady = fmt.Errorf("pumpkin-pi: dependency not ready")
