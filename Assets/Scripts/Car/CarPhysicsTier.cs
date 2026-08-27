using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(KenneyCarController))]
public class CarPhysicsTier : MonoBehaviour
{
    public CarTier tier = CarTier.Commuter;
    public bool applyOnAwake = true;

    [Tooltip("Optional override applied after the tier preset.")]
    public CarPhysicsSettings customOverride;

    public bool useCustomOverride;

    private KenneyCarController controller;

    private void Awake()
    {
        controller = GetComponent<KenneyCarController>();
        if (applyOnAwake)
            ApplyTier();
    }

    public void ApplyTier()
    {
        if (controller == null)
            controller = GetComponent<KenneyCarController>();
        if (controller == null)
            return;

        CarPhysicsSettings settings = CarPhysicsSettings.GetPreset(tier);
        if (useCustomOverride && customOverride != null)
            MergeOverride(settings, customOverride);

        controller.ApplySettings(settings);
    }

    public void SetTier(CarTier newTier, bool applyNow = true)
    {
        tier = newTier;
        if (applyNow)
            ApplyTier();
    }

    private static void MergeOverride(CarPhysicsSettings target, CarPhysicsSettings source)
    {
        if (source.motorPower > 0f) target.motorPower = source.motorPower;
        if (source.maxSpeed > 0f) target.maxSpeed = source.maxSpeed;
        if (source.brakeForce > 0f) target.brakeForce = source.brakeForce;
        if (source.handbrakeForce > 0f) target.handbrakeForce = source.handbrakeForce;
        if (source.coastBrake > 0f) target.coastBrake = source.coastBrake;
        if (source.maxSteerAngle > 0f) target.maxSteerAngle = source.maxSteerAngle;
        if (source.minSteerAngle > 0f) target.minSteerAngle = source.minSteerAngle;
        if (source.steerRampIn > 0f) target.steerRampIn = source.steerRampIn;
        if (source.steerRampOut > 0f) target.steerRampOut = source.steerRampOut;
        if (source.steerHighSpeedRate > 0f) target.steerHighSpeedRate = source.steerHighSpeedRate;
        if (source.steerSpeedFalloff > 0f) target.steerSpeedFalloff = source.steerSpeedFalloff;
        if (source.keyboardSteerScale > 0f) target.keyboardSteerScale = source.keyboardSteerScale;
        if (source.frontGrip > 0f) target.frontGrip = source.frontGrip;
        if (source.rearGrip > 0f) target.rearGrip = source.rearGrip;
        if (source.handbrakeRearGrip > 0f) target.handbrakeRearGrip = source.handbrakeRearGrip;
        if (source.mass > 0f) target.mass = source.mass;
        if (source.downforce > 0f) target.downforce = source.downforce;
        if (source.handbrakeYaw > 0f) target.handbrakeYaw = source.handbrakeYaw;
        if (source.driftAlignStrength > 0f) target.driftAlignStrength = source.driftAlignStrength;
        if (source.maxYawRate > 0f) target.maxYawRate = source.maxYawRate;
        if (source.driftAngleThreshold > 0f) target.driftAngleThreshold = source.driftAngleThreshold;
        if (source.skidSlipReference > 0f) target.skidSlipReference = source.skidSlipReference;
        if (source.impactStopSeconds > 0f) target.impactStopSeconds = source.impactStopSeconds;
        if (source.impactBrakeForce > 0f) target.impactBrakeForce = source.impactBrakeForce;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        ApplyTier();
    }
#endif
}
