# Hardware Integration Patterns

This document outlines the hardware integration patterns used in Nexus, mapping directly to the requirements specified in the [OS Support Requirements](../SRS/sections/1X/11_OS_Support.md) and implemented through [ADR-006: AOG-Link MCU Communications](../ADR/ADR-006-aog-link-mcu-communications.md).

## Integration Models

### 1. CM5/Pi5 Direct Integration
As defined in [ADR-001](../ADR/ADR-001-dotnet8-runtime.md), the CM5/Pi5 deployment model enables:
- Direct GPIO control for steering and sections
- Native serial/CAN communication
- Hardware-accelerated display support
- Single-board solution for complete system

### 2. Legacy AIO Compatibility
Maintains compatibility with existing hardware through:
- USB serial connections
- Ethernet/UDP communication
- Legacy PGN protocol support
See [ADR-002: gRPC Contracts](../ADR/ADR-002-grpc-contracts.md) for protocol details.

### 3. Hybrid Deployments
Supports mixed hardware configurations:
- Remote section controllers
- Distributed sensor networks
- ISOBUS integration
- MQTT/MQTT-SN connectivity

## Hardware Abstraction

```mermaid
graph TD
    Core[Core Services] --> AgIO[AgIO Layer]
    AgIO --> Legacy[Legacy Protocol]
    AgIO --> AOGLink[AOG-Link V1]
    AgIO --> Direct[Direct Hardware]
    Legacy --> AIO[AIO Hardware]
    AOGLink --> Modern[Modern MCUs]
    Direct --> GPIO[GPIO/Hardware]
```

## Protocol Support

| Protocol | Transport | Use Case | Documentation |
|----------|-----------|-----------|---------------|
| PGN V0 | UDP | Legacy AIO | [Legacy Protocol](../reference/legacy-protocol.md) |
| AOG-Link V1 | Serial/CAN | Modern MCUs | [AOG-Link Spec](../reference/aog-link-spec.md) |
| MQTT-SN | Network | Remote Devices | [MQTT Integration](../reference/mqtt-integration.md) |

## Configuration Examples

### CM5 Direct Control
```json
{
  "hardware": {
    "type": "cm5-direct",
    "interfaces": {
      "steering": "gpio",
      "sections": "gpio",
      "sensors": "i2c"
    }
  }
}
```

### Legacy AIO
```json
{
  "hardware": {
    "type": "aio-legacy",
    "connection": {
      "type": "serial",
      "port": "COM3",
      "baud": 38400
    }
  }
}
```

## Safety Considerations

1. **Fail-Safe Defaults**
   - All outputs default to safe state
   - Watchdog timers on critical systems
   - Automatic disengagement on errors

2. **Connection Management**
   - Heartbeat monitoring
   - Connection loss handling
   - Graceful degradation

3. **Physical Safety**
   - Emergency stop integration
   - Mechanical overrides
   - Operator presence detection

## Performance Requirements

As specified in [Threading, Scheduling & Timing requirements](../SRS/sections/2X/23_Threading_Scheduling_Timing.md):

- Maximum latency: 100ms
- Minimum update rate: 10Hz
- Reliability: 99.99% uptime

## Related Documentation

- [OS Support Requirements](../SRS/sections/1X/11_OS_Support.md)
- [Communications Requirements](../SRS/sections/4X/42_Transports.md)
- [ADR-006: AOG-Link Protocol](../ADR/ADR-006-aog-link-mcu-communications.md)
- [Deployment Guide](../deployment/INDEX.md)
- [Hardware Compatibility List](../deployment/hardware/compatibility.md)