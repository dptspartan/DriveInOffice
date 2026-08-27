using System;
using UnityEngine;

public enum CarTier
{
    Starter = 0,
    Commuter = 1,
    Sport = 2,
    Super = 3
}

[Serializable]
public class CarPhysicsSettings
{
    [Header("Engine")]
    [Tooltip("Rear-wheel drive motor torque.")]
    public float motorPower = 1400f;

    [Tooltip("Soft top speed in m/s. 18 ≈ 65 km/h.")]
    public float maxSpeed = 18f;

    [Range(0.2f, 0.8f)]
    public float reversePower = 0.45f;

    [Header("Brakes")]
    public float brakeForce = 2600f;
    public float handbrakeForce = 3400f;
    public float coastBrake = 900f;

    [Header("Steering")]
    [Tooltip("Steer angle at low speed.")]
    public float maxSteerAngle = 18f;

    [Tooltip("Steer angle at top speed.")]
    public float minSteerAngle = 8f;

    [Tooltip("How fast wheels turn toward input. Lower = less twitchy.")]
    public float steerRampIn = 1.8f;

    [Tooltip("How fast wheels return to center when input is released.")]
    public float steerRampOut = 2.6f;

    [Tooltip("Steer ramp multiplier at top speed. Lower = slower turns when fast.")]
    [Range(0.2f, 1f)]
    public float steerHighSpeedRate = 0.42f;

    [Tooltip("How early max steer angle drops off as speed rises. Higher = tighter at moderate speed.")]
    [Range(0.8f, 2.5f)]
    public float steerSpeedFalloff = 1.35f;

    [Tooltip("Keyboard A/D is digital; this scales how much of full lock you get.")]
    [Range(0.5f, 1f)]
    public float keyboardSteerScale = 0.82f;

    [Header("Grip")]
    [Tooltip("Front tire grip multiplier. Higher = more understeer.")]
    [Range(0.5f, 1.5f)]
    public float frontGrip = 1f;

    [Tooltip("Rear tire grip multiplier.")]
    [Range(0.5f, 1.5f)]
    public float rearGrip = 0.95f;

    [Tooltip("Rear grip while handbraking. Lower = easier slides.")]
    [Range(0.15f, 1f)]
    public float handbrakeRearGrip = 0.4f;

    [Header("Body")]
    public float mass = 1200f;
    public Vector3 centerOfMass = new Vector3(0f, 0.15f, 0.04f);
    public float downforce = 14f;

    [Header("Drift")]
    public float handbrakeYaw = 260f;
    public float driftAlignStrength = 18f;
    public float maxYawRate = 2.2f;
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
                    motorPower = 1100f,
                    maxSpeed = 14f,
                    reversePower = 0.4f,
                    brakeForce = 2800f,
                    handbrakeForce = 3200f,
                    coastBrake = 1100f,
                    maxSteerAngle = 20f,
                    minSteerAngle = 10f,
                    steerRampIn = 1.6f,
                    steerRampOut = 2.4f,
                    steerHighSpeedRate = 0.48f,
                    steerSpeedFalloff = 1.2f,
                    keyboardSteerScale = 0.88f,
                    frontGrip = 1.12f,
                    rearGrip = 1.08f,
                    handbrakeRearGrip = 0.5f,
                    mass = 1150f,
                    downforce = 16f,
                    handbrakeYaw = 220f,
                    driftAlignStrength = 24f,
                    maxYawRate = 1.8f,
                    driftAngleThreshold = 12f,
                    skidSlipReference = 0.6f
                };

            case CarTier.Sport:
                return new CarPhysicsSettings
                {
                    motorPower = 1900f,
                    maxSpeed = 22f,
                    reversePower = 0.45f,
                    brakeForce = 2500f,
                    handbrakeForce = 3600f,
                    coastBrake = 750f,
                    maxSteerAngle = 16f,
                    minSteerAngle = 7f,
                    steerRampIn = 2.2f,
                    steerRampOut = 3f,
                    steerHighSpeedRate = 0.38f,
                    steerSpeedFalloff = 1.5f,
                    keyboardSteerScale = 0.78f,
                    frontGrip = 0.98f,
                    rearGrip = 0.88f,
                    handbrakeRearGrip = 0.32f,
                    mass = 1180f,
                    downforce = 18f,
                    handbrakeYaw = 320f,
                    driftAlignStrength = 14f,
                    maxYawRate = 2.6f,
                    driftAngleThreshold = 8f,
                    skidSlipReference = 0.5f
                };

            case CarTier.Super:
                return new CarPhysicsSettings
                {
                    motorPower = 2400f,
                    maxSpeed = 26f,
                    reversePower = 0.5f,
                    brakeForce = 2400f,
                    handbrakeForce = 3800f,
                    coastBrake = 650f,
                    maxSteerAngle = 15f,
                    minSteerAngle = 6f,
                    steerRampIn = 2.4f,
                    steerRampOut = 3.2f,
                    steerHighSpeedRate = 0.35f,
                    steerSpeedFalloff = 1.6f,
                    keyboardSteerScale = 0.75f,
                    frontGrip = 0.92f,
                    rearGrip = 0.78f,
                    handbrakeRearGrip = 0.28f,
                    mass = 1100f,
                    downforce = 22f,
                    handbrakeYaw = 380f,
                    driftAlignStrength = 10f,
                    maxYawRate = 3f,
                    driftAngleThreshold = 7f,
                    skidSlipReference = 0.45f
                };

            default:
                return new CarPhysicsSettings
                {
                    motorPower = 1400f,
                    maxSpeed = 18f,
                    reversePower = 0.45f,
                    brakeForce = 2600f,
                    handbrakeForce = 3400f,
                    coastBrake = 900f,
                    maxSteerAngle = 18f,
                    minSteerAngle = 8f,
                    steerRampIn = 1.8f,
                    steerRampOut = 2.6f,
                    steerHighSpeedRate = 0.42f,
                    steerSpeedFalloff = 1.35f,
                    keyboardSteerScale = 0.82f,
                    frontGrip = 1f,
                    rearGrip = 0.95f,
                    handbrakeRearGrip = 0.4f,
                    mass = 1200f,
                    downforce = 14f,
                    handbrakeYaw = 260f,
                    driftAlignStrength = 18f,
                    maxYawRate = 2.2f,
                    driftAngleThreshold = 10f,
                    skidSlipReference = 0.55f
                };
        }
    }
}
