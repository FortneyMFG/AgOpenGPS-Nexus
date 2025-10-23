# Bridge Topology with Pumpkin Pi Fastpath

Pumpkin Pi introduces a CM5-local control loop while keeping AgIO Bridge available for legacy or external modules. The
[Pumpkin Pi ADR](../development/SRS/sections/5X_Hardware_IO_Device_Layer/54-ADR-00XX%20-%2000XX%20CM5%20SHM%20Fastpath%20+%20HAL%20Plugin%20%28Pumpkin%20Pi%29.md)
captures the formal coexistence requirements.

```mermaid
flowchart TD
  subgraph CM5["CM5 (Host + Controller)"]
    Core[gRPC Bus / NAV / UI]
    subgraph Pumpkin["Pumpkin Pi (HAL + SHM)"]
      SHM[(SHM Fastpath)]
      STEER[steer-ctrl]
    end
    subgraph AgIO["AgIO Bridge (external transports)"]
      LINK[AOG-Link v1]
      UDP[UDP]
      SERIAL[Serial]
      CAN[CAN-FD]
      MQTT[MQTT Adapter]
      V0[v0 Bridge]
      LINK --> UDP & SERIAL & CAN & MQTT & V0
    end
    Core --> |SetSteerTarget (gRPC)| -->Pumpkin
    Pumpkin --> SHM
    STEER -->|HAL| Hardware
    Pumpkin <--> MQTT
  end
```

Key behaviors:
- SHM fastpath handles NAV ⇄ steer-ctrl setpoints under 5 ms p95.
- AgIO Bridge still mirrors MQTT retained topics for UI and observers.
- External transports continue to route through AgIO to preserve arbitration and compatibility.
