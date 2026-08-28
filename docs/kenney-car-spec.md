# Kenney Car — Physics Spec

**Controller:** `KenneyCarController`  
**Tier presets:** `CarPhysicsTier` + `CarPhysicsSettings`  
**Prefab:** `Assets/Prefabs/Cars/DrivableCar.prefab`  
**Layout:** RWD (rear motor)  
**Speed unit:** m/s (`18` ≈ **65 km/h**)

---

## Architecture

| Script | Role |
|---|---|
| `KenneyCarController` | WheelCollider physics, input, skid state |
| `CarPhysicsTier` | Selects Starter / Commuter / Sport / Super preset |
| `CarPhysicsSettings` | Serializable tuning data + static presets |
| `CarSkidEffects` | Skid marks and tire smoke from controller state |
| `CarImpactStop` | Wall-hit stun |

Driving assist hooks on the controller (`assistGripMultiplier`, `assistSteerMultiplier`) are reserved for a future `CarDrivingAssist` script.

---

## Car tiers

Set **Tier** on `CarPhysicsTier`. Presets apply on play.

| Tier | Feel | Max speed | Motor | Grip bias |
|---|---|---|---|---|
| **Starter** | Forgiving, slow | 14 m/s | 1100 | High grip, easy turns |
| **Commuter** | Default city car | 18 m/s | 1400 | Balanced |
| **Sport** | Faster, slides easier | 22 m/s | 1900 | Looser rear |
| **Super** | Fastest, drift-friendly | 26 m/s | 2400 | Loose rear, strong HB |

After picking a tier, tweak fields under **Physics** on `KenneyCarController` in the inspector.

---

## Physics model (simple)

- **Motor:** `power × (1 - speedRatio²)` soft cap — no fragile torque curve
- **Steer:** Lerp from max to min angle by speed; one response rate
- **Grip:** Fixed friction curve shape; only **frontGrip / rearGrip** multipliers change at runtime
- **Handbrake:** Drops rear grip + yaw push; predictable slides
- **Skid:** `SkidIntensity = sidewaysSlip / skidSlipReference`
- **Stability:** Downforce + gentle drift alignment (not a full sim)

---

## Tune order

1. Tier on `CarPhysicsTier`
2. `maxSpeed` + `motorPower`
3. `maxSteerAngle` / `minSteerAngle` / `steerResponse`
4. `frontGrip` / `rearGrip` / `handbrakeRearGrip`
5. Brakes
6. `handbrakeYaw` + `driftAlignStrength` for drift feel

One cluster per test lap.

---

## Wiring (do not tune)

| Field | Role |
|---|---|
| Wheel Colliders FL/FR/RL/RR | Physics wheels |
| Wheel Meshes FL/FR/RL/RR | Visuals follow collider pose |

Wrong refs = spinning in air, no steer, or wheels floating.
