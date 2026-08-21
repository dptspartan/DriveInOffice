# Kenney Car — Tune Spec

**Script:** `KenneyCarController`  
**Prefab:** `Assets/Prefabs/Cars/DrivableCar.prefab` (playable inspector values)  
**Layout:** RWD (motor on rear wheels only)  
**Mass:** 1200 kg  
**Speed unit:** m/s (`18` ≈ **65 km/h**)

| | Playable (prefab) |
|---|---|
| Class | City / keyboard |
| Vmax | 18 m/s |
| Motor | 1550 |
| Steer | 17° → 8° with speed falloff |
| Grip F/R | 2.4 / 2.35 |

A full tune-preset system is not in the game yet. Change numbers on the prefab.

---

## Wiring (do not tune)

| Field | Role |
|---|---|
| Wheel Colliders FL/FR/RL/RR | Physics wheels. Must match mesh positions. |
| Wheel Meshes FL/FR/RL/RR | Visuals follow collider pose. |

Wrong refs = spinning in air, no steer, or wheels floating.

---

## Engine

| Field | Prefab | Playable | Range | What it does |
|---|---|---|---|---|
| **Motor Force** | 2500 | 1550 | 800–6000 | Rear torque × throttle × torque curve. |
| **Max Speed** | 28 | 18 | 12–45 | Soft cap. Also scales steer, grip, skids (`speedFactor = speed / maxSpeed`). |
| **Torque Curve** | 1 → 0.22 | same | keys 0–1 | X = speedFactor, Y = torque multiplier. Empty = auto stock. |

```
motor = throttle × MotorForce × torqueCurve(speed / MaxSpeed)
```

Raise **Motor Force** = punchier pull. Raise **Max Speed** without force/curve = long, weak top end. Lower **Max Speed** = steering/grip enter “high speed” earlier.

| Feel | Curve (X=speed, Y=torque) |
|---|---|
| Economy | 0→1.0, 1→0.15 |
| Stock / playable | 0→1.0, 1→0.22 |
| Sports | 0→0.85, 0.5→1.0, 1→0.35 |
| Muscle | 0→1.2, 0.4→1.0, 1→0.2 |
| EV | 0→1.0, 1→0.7 |

S while rolling forward is a **foot brake**. Reverse is `Motor Force × 0.55` only when nearly stopped.

---

## Brakes

| Field | Prefab | Playable | What it does |
|---|---|---|---|
| **Foot Brake Force** | 2500 | 2800 | S / LT while rolling forward. Front + rear. |
| **Handbrake Force** | 4000 | 3500 | Space. Rear only. |
| **Engine Brake Force** | 1100 | 1400 | Coast. Scales up with speed (45%→100%). |

Handbrake also sets rear sideways grip to **Handbrake Rear Stiffness** and adds yaw via **Handbrake Yaw**.

| Symptom | Tweak |
|---|---|
| Won’t stop | ↑ Foot Brake Force |
| Nose dive | ↓ Foot Brake, ↑ Engine Brake |
| No e-brake slide | ↑ Handbrake Force, ↓ HB rear stiffness |
| Rolls forever off-throttle | ↑ Engine Brake Force |

---

## Steering

Angle falls with speed (not linear in playable tune):

```
blend = SmoothStep(0, 1, speedFactor ^ steerFalloff)
steer = input × Lerp(MaxSteerAngle, MinSteerAngle, blend)
rate  = Lerp(SteerSpeed, SteerSpeed × highSpeedSteerRate, speedFactor)
```

Keyboard A/D is digital 1.0. **Steer Speed** and **highSpeedSteerRate** stop the wheels from snapping to full lock.

| Field | Prefab | Playable | What it does |
|---|---|---|---|
| **Max Steer Angle** | 28 | 17 | Low-speed lock. |
| **Min Steer Angle** | 8 | 8 | High-speed lock. Not tiny — still turns, won’t spin. |
| **Steer Speed** | 6 | 2.4 | How fast wheels catch input. |
| **Steer Falloff** | (linear) | 1.25 | >1 = lock drops earlier in the speed band. |
| **High Speed Steer Rate** | 1 | 0.38 | Fraction of Steer Speed at Vmax. |

| Feel | Max | Min | Speed | Falloff |
|---|---|---|---|---|
| Truck | 32–36 | 12 | 4 | 1.2 |
| Playable city | 17 | 8 | 2.4 | 1.25 |
| Kart | 34–40 | 16 | 10 | 1.0 |
| GT | 22–26 | 8 | 7 | 1.6 |

At speed, extra **turn limiter** scales steer down if the car is already sliding (`DriftAngle`), so full A/D at Vmax does not break the rear.

---

## Grip

Sideways stiffness is the tire knob. Fade above 50% Vmax (playable is milder than prefab):

| | Prefab at Vmax | Playable at Vmax |
|---|---|---|
| Front | 82% | 94% |
| Rear | 70% | 90% |
| Foot brake extra | harsh | mild |

| Field | Prefab | Playable | What it does |
|---|---|---|---|
| **Front Sideways Stiffness** | 2.2 | 2.4 | Front bite. High = understeer. |
| **Rear Sideways Stiffness** | 2.1 | 2.35 | Rear bite. Keep ≈ front ±0.15 for keyboard. |
| **Handbrake Rear Stiffness** | 0.55 | 0.65 | Rear grip on Space. |

| Bias | F / R |
|---|---|
| Safe understeer | 2.5 / 2.3 |
| Playable | 2.4 / 2.35 |
| Loose / drift | 2.0 / 1.5 |

---

## Assist

| Field | Prefab | Playable | What it does |
|---|---|---|---|
| **Downforce** | 12 | 16 | Downward force × `speed²`. |
| **Stability Yaw** | 1400 | 2100 | Damps spin when not handbraking. Stronger at speed. |
| **Handbrake Yaw** | 500 | 320 | Extra yaw from steer while Space held. |
| **Max Spin Rate** | 2.4 | 1.8 | Caps yaw during handbrake only. |

Hard throttle + full steer at speed also **cuts rear motor** slightly so RWD does not snap.

---

## Impact

Ground layer ignored. Hit > **5.5 m/s**, mostly horizontal normal.

| Field | Stock | What it does |
|---|---|---|
| **Impact Stop Seconds** | 1.35 | Stun: no throttle/steer, full brakes. |
| **Impact Brake Force** | 8000 | Brake torque while stunned. Velocity cut to 35%. |

`CarImpactStop`: `minImpactSpeed` 5.5, `maxGroundNormalY` 0.62, `cooldown` 0.45.

---

## Speed coupling

`speedFactor = clamp(speed / MaxSpeed)` drives torque, steer, grip, coast brake, skids. Changing **Max Speed** retunes the whole car.

---

## Presets

These are notes for a later tune system. The car in the game uses **Playable keyboard** on the prefab.

### Playable keyboard (current prefab)

| Motor 1550 | Vmax 18 | Steer 17/8/2.4 | Grip 2.4/2.35/0.65 |
| Assist 16 / 2100 / 320 / 1.8 | Brakes 2800 / 3500 / 1400 | Falloff 1.25 | High-speed steer rate 0.38 |

### Compact / taxi

| Motor 1600 | Vmax 16 | Steer 32/12/4 | Grip 2.45/2.4/0.8 |

### Sports GT

| Motor 2800 | Vmax 26 | Steer 24/9/5 | Grip 2.5/2.4/0.6 | Downforce 20 |

### Muscle / drift

| Motor 3200 | Vmax 24 | Steer 30/10/6 | Grip 2.0/1.5/0.35 | Stab 700 | HB yaw 800 |

### Truck / van

| Motor 2000 | Vmax 16 | Steer 34/12/3.5 | Grip 2.1/2.25/0.9 | Steer speed 3.5 |

---

## Tune order

1. Max Speed  
2. Motor Force + Torque Curve  
3. Steer Max / Min / Falloff  
4. Grip F/R  
5. Brakes  
6. Handbrake trio  
7. Downforce / Stability  
8. Impact  

One cluster per test lap.

---

## Fault matrix

| You feel | First knobs |
|---|---|
| Slow | Motor Force, curve low-speed Y |
| Too fast | Max Speed, then Motor Force |
| Understeer | ↓ Front stiffness or ↑ Max Steer |
| Spins on a full-speed turn | ↑ Rear grip, ↑ Stability, ↑ Steer Falloff, ↓ Motor |
| Twitchy keyboard | ↓ Steer Speed, ↓ High Speed Steer Rate |
| Dead steering at speed | ↑ Min Steer Angle |
| No drift | ↓ HB rear stiffness, ↑ HB Yaw |
| Bounce | Downforce; CoM y=0.18 in code |

---

## Skid FX

`CarSkidEffects` is visual/audio only (`slipThreshold`, `minSpeed`, `markWidth`, `smokeRate`).
