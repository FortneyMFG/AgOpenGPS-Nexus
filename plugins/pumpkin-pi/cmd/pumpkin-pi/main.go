package main

import (
	"context"
	"flag"
	"log"
	"net"
	"os"
	"os/signal"
	"strings"
	"syscall"
	"time"

	"google.golang.org/grpc"

	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/config"
	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/grpcapi"
	pumpkinpipb "github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/grpcapi/pb"
	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/hal"
	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/mqtt"
	"github.com/AgOpenGPS/AgOpenGPS-Nexus/plugins/pumpkin-pi/pkg/shm"
	"golang.org/x/sys/unix"
)

func main() {
	cfgPath := flag.String("config", "/etc/aog/pumpkin.yaml", "Path to configuration file")
	overrideListen := flag.String("listen", "", "Override gRPC listen address")
	flag.Parse()

	raw, err := os.ReadFile(*cfgPath)
	if err != nil {
		log.Fatalf("read config: %v", err)
	}
	cfg, err := config.Parse(raw)
	if err != nil {
		log.Fatalf("parse config: %v", err)
	}
	if *overrideListen != "" {
		cfg.GRPC.Listen = *overrideListen
	}

	if err := unix.Mlockall(unix.MCL_CURRENT | unix.MCL_FUTURE); err != nil {
		log.Printf("warning: mlockall failed: %v", err)
	}

	ring, err := shm.Open(cfg.Fastpath.ShmName, cfg.Fastpath.EventFDName)
	if err != nil {
		log.Fatalf("open shm: %v", err)
	}
	defer ring.Close()

	halBackend, err := hal.New(cfg.HAL)
	if err != nil {
		log.Fatalf("init hal: %v", err)
	}
	defer halBackend.Close()

	publisher, err := mqtt.NewPublisher(cfg.MQTT)
	if err != nil {
		log.Fatalf("mqtt connect: %v", err)
	}
	defer publisher.Close()

	server := grpcapi.NewServer(ring, halBackend, publisher, cfg.Fastpath, cfg.Authority)

	listen, err := listenAddress(cfg.GRPC.Listen)
	if err != nil {
		log.Fatalf("listen: %v", err)
	}
	grpcServer := grpc.NewServer()
	pumpkinpipb.RegisterPumpkinPiServiceServer(grpcServer, server)

	ctx, stop := signal.NotifyContext(context.Background(), syscall.SIGINT, syscall.SIGTERM)
	defer stop()

	server.StartAuthorityMonitor(ctx)

	go func() {
		ticker := time.NewTicker(5 * time.Second)
		defer ticker.Stop()
		for {
			select {
			case <-ctx.Done():
				return
			case <-ticker.C:
				_ = publisher.PublishHealth("ok")
			}
		}
	}()

	go func() {
		<-ctx.Done()
		log.Printf("shutdown requested")
		grpcServer.GracefulStop()
	}()

	log.Printf("Pumpkin Pi listening on %s", cfg.GRPC.Listen)
	if err := grpcServer.Serve(listen); err != nil {
		log.Fatalf("serve: %v", err)
	}
}

func listenAddress(addr string) (net.Listener, error) {
	if strings.HasPrefix(addr, "unix://") {
		path := strings.TrimPrefix(addr, "unix://")
		_ = os.Remove(path)
		return net.Listen("unix", path)
	}
	if strings.HasPrefix(addr, "unix:") {
		path := strings.TrimPrefix(addr, "unix:")
		_ = os.Remove(path)
		return net.Listen("unix", path)
	}
	if strings.HasPrefix(addr, "/") {
		_ = os.Remove(addr)
		return net.Listen("unix", addr)
	}
	if addr == "" {
		addr = ":44111"
	}
	return net.Listen("tcp", addr)
}
