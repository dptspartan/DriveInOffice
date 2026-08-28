using System;
using UnityEngine;

public enum CarTier
{
    Starter = 0,
    Commuter = 1,
    Sport = 2,
    Super = 3
}

public enum CarDriveType
{
    RWD = 0,
    FWD = 1,
    AWD = 2
}

[Serializable]
public class CarPhysicsSettings
{
    [Header("Engine")]
    [Tooltip("Drive motor torque (split across driven wheels).")]
    public float motorPower = 1650f;

    [Tooltip("Soft top speed in m/s. 18 ≈ 65 km/h.")]
    public float maxSpeed = 18f;

    [Range(0.2f, 0.8f)]
    public float reversePower = 0.45f;

    [Tooltip("Which axle(s) receive motor torque.")]
    public CarDriveType driveType = CarDriveType.RWD;

    [Header("Brakes")]
    public float brakeForce = 2400f;
    public float handbrakeForce = 3400f;
    public float coastBrake = 900f;

    [Header("Steering")]
    [Tooltip("Steer angle at low speed.")]
    public float maxSteerAngle = 18f;

    [Tooltip("Steer angle at top speed.")]
    public float minSteerAngle = 8f;

    [Tooltip("How fast wheels turn toward input. Lower = less twitchy.")]
    public float steerRampIn = 2.8f;

    [Tooltip("How fast wheels return to center when input is released.")]
    public float steerRampOut = 4.5f;

    [Tooltip("How fast wheels cross through center when input flips (A→D). Higher = snappier counter-steer.")]
    public float steerCounterRamp = 9f;

    [Tooltip("Extra yaw torque when counter-steering (flipping A↔D). Makes the body follow the turn.")]
    public float counterSteerYaw = 220f;

    [Tooltip("Steer ramp multiplier at top speed. Lower = slower turns when fast.")]
    [Range(0.2f, 1f)]
    public float steerHighSpeedRate = 0.52f;

    [Tooltip("How early max steer angle drops off as speed rises. Higher = tighter at moderate speed.")]
    [Range(0.8f, 2.5f)]
    public float steerSpeedFalloff = 1.25f;

    [Tooltip("Keyboard A/D is digital; this scales how much of full lock you get.")]
    [Range(0.5f, 1f)]
    public float keyboardSteerScale = 0.88f;

    [Header("Grip")]
    [Tooltip("Front tire sideways grip. Higher = more understeer.")]
    [Range(0.5f, 1.5f)]
    public float frontGrip = 1.08f;

    [Tooltip("Rear tire sideways grip.")]
    [Range(0.5f, 1.5f)]
    public float rearGrip = 1.05f;

    [Tooltip("Longitudinal (drive/brake) grip. Keep near 1.0–1.2; high values lock and wobble.")]
    [Range(0.8f, 2.5f)]
    public float forwardGrip = 1.12f;

    [Tooltip("Rear grip while handbraking. Lower = easier slides.")]
    [Range(0.15f, 1f)]
    public float handbrakeRearGrip = 0.4f;

    [Header("Body")]
    public float mass = 1200f;
    public Vector3 centerOfMass = new Vector3(0f, 0.1f, 0.05f);
    public float downforce = 18f;
    public float rollStability = 2800f;
    public float pitchStability = 2200f;

    [Header("Drift")]
    public float handbrakeYaw = 260f;
    public float driftAlignStrength = 12f;
    public float maxYawRate = 2f;
    public float driftAngleThreshold = 10f;

    [Header("Skid Detection")]
    [Tooltip("Sideways slip value that maps to full skid intensity.")]
    public float skidSlipReference = 0.55f;

    [Header("Impact")]
    public float impactStopSeconds = 1.2f;
    public float impactBrakeForce = 7500f;

    public CarPhysicsSettings Clone()
    {
        return (CarPhysicsSettings)MemberwiseClone();
    }

    public static CarPhysicsSettings GetPreset(CarTier tier)
    {
        switch (tier)
        {
            case CarTier.Starter:
                return new CarPhysicsSettings
                {
                    motorPower = 1200f,
                    maxSpeed = 14f,
                    reversePower = 0.4f,
                    driveType = CarDriveType.RWD,
                    brakeForce = 2600f,
                    handbrakeForce = 3200f,
                    coastBrake = 1100f,
                    maxSteerAngle = 20f,
                    minSteerAngle = 10f,
                    steerRampIn = 2.6f,
                    steerRampOut = 4.2f,
                    steerCounterRamp = 8.5f,
                    counterSteerYaw = 180f,
                    steerHighSpeedRate = 0.55f,
                    steerSpeedFalloff = 1.15f,
                    keyboardSteerScale = 0.9f,
                    frontGrip = 1.18f,
                    rearGrip = 1.14f,
                    forwardGrip = 1.15f,
                    handbrakeRearGrip = 0.5f,
                    mass = 1150f,
                    centerOfMass = new Vector3(0f, 0.1f, 0.05f),
                    downforce = 20f,
                    rollStability = 3000f,
                    pitchStability = 2400f,
                    handbrakeYaw = 200f,
                    driftAlignStrength = 10f,
                    maxYawRate = 1.6f,
                    driftAngleThreshold = 12f,
                    skidSlipReference = 0.6f
                };

            case CarTier.Sport:
                return new CarPhysicsSettings
                {
                    motorPower = 2100f,
                    maxSpeed = 22f,
                    reversePower = 0.45f,
                    driveType = CarDriveType.RWD,
                    brakeForce = 2300f,
                    handbrakeForce = 3600f,
                    coastBrake = 800f,
                    maxSteerAngle = 17f,
                    minSteerAngle = 8f,
                    steerRampIn = 3f,
                    steerRampOut = 4.8f,
                    steerCounterRamp = 9.5f,
                    counterSteerYaw = 260f,
                    steerHighSpeedRate = 0.5f,
                    steerSpeedFalloff = 1.3f,
                    keyboardSteerScale = 0.86f,
                    frontGrip = 1.1f,
                    rearGrip = 1.04f,
                    forwardGrip = 1.1f,
                    handbrakeRearGrip = 0.34f,
                    mass = 1180f,
                    centerOfMass = new Vector3(0f, 0.095f, 0.05f),
                    downforce = 22f,
                    rollStability = 3000f,
                    pitchStability = 2400f,
                    handbrakeYaw = 280f,
                    driftAlignStrength = 11f,
                    maxYawRate = 2f,
                    driftAngleThreshold = 9f,
                    skidSlipReference = 0.52f
                };

            case CarTier.Super:
                return new CarPhysicsSettings
                {
                    motorPower = 2350f,
                    maxSpeed = 26f,
                    reversePower = 0.5f,
                    driveType = CarDriveType.RWD,
                    brakeForce = 2200f,
                    handbrakeForce = 3800f,
                    coastBrake = 700f,
                    maxSteerAngle = 15f,
                    minSteerAngle = 6f,
                    steerRampIn = 2.4f,
                    steerRampOut = 5.2f,
                    steerCounterRamp = 10f,
                    counterSteerYaw = 300f,
                    steerHighSpeedRate = 0.35f,
                    steerSpeedFalloff = 1.6f,
                    keyboardSteerScale = 0.75f,
                    frontGrip = 1.02f,
                    rearGrip = 0.92f,
                    forwardGrip = 1.08f,
                    handbrakeRearGrip = 0.28f,
                    mass = 1100f,
                    centerOfMass = new Vector3(0f, 0.09f, 0.04f),
                    downforce = 24f,
                    rollStability = 2800f,
                    pitchStability = 2200f,
                    handbrakeYaw = 360f,
                    driftAlignStrength = 9f,
                    maxYawRate = 2.6f,
                    driftAngleThreshold = 7f,
                    skidSlipReference = 0.45f
                };

            default: // Commuter — balanced arcade baseline
                return new CarPhysicsSettings
                {
                    motorPower = 1650f,
                    maxSpeed = 18f,
                    reversePower = 0.45f,
                    driveType = CarDriveType.RWD,
                    brakeForce = 2400f,
                    handbrakeForce = 3400f,
                    coastBrake = 900f,
                    maxSteerAngle = 18f,
                    minSteerAngle = 8f,
                    steerRampIn = 2.8f,
                    steerRampOut = 4.5f,
                    steerCounterRamp = 9f,
                    counterSteerYaw = 220f,
                    steerHighSpeedRate = 0.52f,
                    steerSpeedFalloff = 1.25f,
                    keyboardSteerScale = 0.88f,
                    frontGrip = 1.08f,
                    rearGrip = 1.05f,
                    forwardGrip = 1.12f,
                    handbrakeRearGrip = 0.4f,
                    mass = 1200f,
                    centerOfMass = new Vector3(0f, 0.1f, 0.05f),
                    downforce = 18f,
                    rollStability = 2800f,
                    pitchStability = 2200f,
                    handbrakeYaw = 240f,
                    driftAlignStrength = 12f,
                    maxYawRate = 2f,
                    driftAngleThreshold = 10f,
                    skidSlipReference = 0.55f
                };
        }
    }
}
