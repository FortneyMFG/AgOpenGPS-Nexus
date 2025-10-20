# Vehicle Configuration Guide

This guide covers vehicle setup and configuration in Nexus. For hardware-specific setup, see the [Deployment Guides](../../deployment/INDEX.md).

## Vehicle Profiles

### Basic Configuration
```json
{
  "vehicle": {
    "name": "John Deere 6R",
    "type": "tractor",
    "wheelbase": 2.8,
    "turnRadius": 4.5,
    "antennaHeight": 3.2,
    "antennaOffset": {
      "x": 0,
      "y": 1.5,
      "z": 0
    }
  }
}
```

### Implement Settings
Following [ADR-008: Equipment Hierarchy](../../ADR/ADR-008-equipment-hierarchy.md):

```json
{
  "implement": {
    "type": "planter",
    "width": 12.0,
    "sections": [
      {
        "id": 1,
        "width": 3.0,
        "offset": -4.5
      },
      {
        "id": 2,
        "width": 3.0,
        "offset": -1.5
      },
      {
        "id": 3,
        "width": 3.0,
        "offset": 1.5
      },
      {
        "id": 4,
        "width": 3.0,
        "offset": 4.5
      }
    ]
  }
}
```

## Guidance Configuration

### AutoSteer Settings
As defined in [ADR-033: Guidance & AutoSteer](../../ADR/ADR-033-guidance-planner-autosteer.md):

| Parameter | Range | Description |
|-----------|-------|-------------|
| P Gain | 0.1 - 5.0 | Position error gain |
| I Gain | 0.0 - 1.0 | Integral error gain |
| D Gain | 0.0 - 2.0 | Rate error gain |
| Lookahead | 1.0 - 5.0 | Preview distance (m) |

### Example Configuration
```json
{
  "guidance": {
    "controller": {
      "p_gain": 2.5,
      "i_gain": 0.1,
      "d_gain": 0.5,
      "lookahead": 2.8
    },
    "limits": {
      "max_steer_angle": 35.0,
      "max_steer_rate": 25.0,
      "min_speed": 0.5,
      "max_speed": 20.0
    }
  }
}
```

## Section Control

### Basic Setup
1. **Measure Implement**
   - Total width
   - Section widths
   - Section offsets
   - Overlap settings

2. **Configure Sections**
   - Number of sections
   - Individual control
   - Coverage settings

### Advanced Settings
```json
{
  "sections": {
    "overlap": 0.15,
    "lookAhead": 2.0,
    "coverage": {
      "minimum": 0.98,
      "target": 1.0,
      "maximum": 1.02
    }
  }
}
```

## Rate Control

### Product Settings
```json
{
  "products": [
    {
      "id": "seed-corn",
      "name": "Corn Seed",
      "units": "seeds/ha",
      "ranges": {
        "min": 60000,
        "max": 100000,
        "default": 80000
      }
    }
  ]
}
```

### Calibration Process
1. **Zero Calibration**
   - Empty system
   - Set zero point
   - Verify sensors

2. **Flow Calibration**
   - Run test quantity
   - Measure output
   - Calculate factor

3. **Verification**
   - Test multiple rates
   - Verify accuracy
   - Document results

## Machine Types

### Supported Configurations
1. **Tractors**
   - Standard tractor
   - Articulated
   - Track systems

2. **Self-Propelled**
   - Sprayers
   - Spreaders
   - Harvesters

3. **Implements**
   - Planters
   - Sprayers
   - Fertilizer units

## Safety Features

### Operational Limits
- Maximum speed
- Turn rate limits
- Boundary keeping
- Obstacle avoidance

### Emergency Controls
- Manual override
- Emergency stop
- Fault handling
- Safety interlocks

## Related Documentation

- [Equipment Requirements](../../SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md)
- [Control Requirements](../../SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md)
- [Calibration Guide](../maintenance/calibration.md)
- [Operation Manual](../operation/INDEX.md)
