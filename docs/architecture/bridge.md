# Bridge Topology with Pumpkin Pi Fastpath

Pumpkin Pi introduces a CM5-local control loop while keeping AgIO Bridge available for legacy or external modules.

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
    Core --> SHM
    STEER -->|HAL| Hardware
    Pumpkin <--> MQTT
  end
```

Key behaviors:
- SHM fastpath handles NAV ⇄ steer-ctrl setpoints under 5 ms p95.
- AgIO Bridge still mirrors MQTT retained topics for UI and observers.
- External transports continue to route through AgIO to preserve arbitration and compatibility.
