using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class KenneyCarController : MonoBehaviour
{
    private const float BaseSidewaysStiffness = 2.2f;

    [Header("Wheel Colliders")]
    public WheelCollider frontLeftCollider;
    public WheelCollider frontRightCollider;
    public WheelCollider rearLeftCollider;
    public WheelCollider rearRightCollider;

    [Header("Wheel Visual Meshes")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("Physics")]
    public CarPhysicsSettings physics = new CarPhysicsSettings();

    [Header("Driving Assist Hooks")]
    [Tooltip("Scaled by CarKeyboardDrivingAssist when keyboard assist is enabled.")]
    [Range(0.5f, 1.5f)]
    public float assistGripMultiplier = 1f;

    [Tooltip("Scaled by CarKeyboardDrivingAssist when keyboard assist is enabled.")]
    [Range(0.5f, 1.5f)]
    public float assistSteerMultiplier = 1f;

    public float Speed { get; private set; }
    public float ForwardSpeed { get; private set; }
    public float DriftAngle { get; private set; }
    public bool IsHandbraking { get; private set; }
    public bool IsFootBraking { get; private set; }
    public bool IsDrifting { get; private set; }
    public bool IsStunned { get; private set; }
    public float MaxSidewaysSlip { get; private set; }
    public float SkidIntensity { get; private set; }
    public float Throttle { get; private set; }

    /// <summary>When true, driving input is ignored (e.g. dev tuning modal open).</summary>
    public bool DevInputBlocked { get; set; }

    public float maxSpeed => physics.maxSpeed;

    private Rigidbody rb;
    private float stunUntil;
    private float moveInput;
    private float steerInputRaw;
    private float steerInput;
    private float steerTarget;
    private bool handbrake;
    private bool analogSteerInput;
    private bool isCounterSteering;
    private readonly float[] wheelSpin = new float[4];

    public void ApplySettings(CarPhysicsSettings settings)
    {
        if (settings == null)
            return;

        physics = settings.Clone();
        if (rb != null)
            ApplyRigidbodySettings();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        ApplyRigidbodySettings();
        SetupWheelFriction();
        RecenterPivotOnMesh(frontLeftMesh);
        RecenterPivotOnMesh(frontRightMesh);
        RecenterPivotOnMesh(rearLeftMesh);
        RecenterPivotOnMesh(rearRightMesh);
    }

    private void Update()
    {
        ReadInput();
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        IsStunned = Time.time < stunUntil;
        if (IsStunned)
        {
            moveInput = 0f;
            steerInputRaw = 0f;
            handbrake = true;
        }

        Vector3 velocity = rb.linearVelocity;
        Speed = velocity.magnitude;
        ForwardSpeed = Vector3.Dot(velocity, transform.forward);
        IsHandbraking = handbrake;

        UpdateDriftAngle(velocity);

        float speedRatio = physics.maxSpeed > 0.01f
            ? Mathf.Clamp01(Speed / physics.maxSpeed)
            : 0f;

        UpdateSteering(speedRatio);

        float motor = 0f;
        float footBrake = 0f;
        float coastBrake = 0f;

        if (moveInput > 0.01f)
        {
            float powerScale = 1f - speedRatio * speedRatio * 0.78f;
            powerScale = Mathf.Max(powerScale, 0.28f);
            float turnLoad = Mathf.Abs(steerInput) * speedRatio * 0.12f;
            // Soft launch ramp so torque doesn't overwhelm grip from a standstill.
            float launch = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / 4.5f));
            motor = moveInput * physics.motorPower * powerScale * (1f - turnLoad) * launch;
            motor *= TractionScale();
        }
        else if (moveInput < -0.01f)
        {
            if (ForwardSpeed > 1f)
            {
                footBrake = -moveInput * physics.brakeForce;
            }
            else
            {
                float launch = Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / 3.5f));
                motor = moveInput * physics.motorPower * physics.reversePower * launch;
                motor *= TractionScale();
            }
        }
        else if (Mathf.Abs(ForwardSpeed) > 0.5f)
        {
            coastBrake = physics.coastBrake * speedRatio;
        }

        IsFootBraking = footBrake > 0.01f;

        // Front-biased service brakes (arcade ABS-friendly). Equal lock was causing rear slip.
        float frontBrake = Mathf.Max(footBrake * 0.68f, coastBrake);
        float rearBrake = Mathf.Max(footBrake * 0.38f, coastBrake * 0.85f);
        if (handbrake)
            rearBrake = Mathf.Max(rearBrake, physics.handbrakeForce);

        if (IsStunned)
        {
            motor = 0f;
            frontBrake = physics.impactBrakeForce;
            rearBrake = physics.impactBrakeForce;
        }

        ApplyDriveTorque(motor);

        frontLeftCollider.brakeTorque = SoftAbsBrake(frontLeftCollider, frontBrake);
        frontRightCollider.brakeTorque = SoftAbsBrake(frontRightCollider, frontBrake);
        // Handbrake should lock rears for slides; ABS only on service braking.
        if (handbrake && !IsStunned)
        {
            rearLeftCollider.brakeTorque = rearBrake;
            rearRightCollider.brakeTorque = rearBrake;
        }
        else
        {
            rearLeftCollider.brakeTorque = SoftAbsBrake(rearLeftCollider, rearBrake);
            rearRightCollider.brakeTorque = SoftAbsBrake(rearRightCollider, rearBrake);
        }

        ApplyStandstillHold();

        float steerAngle = steerInput
            * GetSteerAngleLimit(speedRatio)
            * assistSteerMultiplier;
        // Softer steering in reverse keeps RWD from snapping left/right.
        if (ForwardSpeed < -0.25f)
            steerAngle *= 0.72f;

        frontLeftCollider.steerAngle = steerAngle;
        frontRightCollider.steerAngle = steerAngle;

        ApplyGrip();
        ApplyBodyForces(speedRatio, ForwardSpeed < -0.35f);
        ApplyCounterSteerAssist(speedRatio);
        ApplyRollStability();
        UpdateSkidState(speedRatio);

        UpdateWheelVisual(frontLeftCollider, frontLeftMesh, 0);
        UpdateWheelVisual(frontRightCollider, frontRightMesh, 1);
        UpdateWheelVisual(rearLeftCollider, rearLeftMesh, 2);
        UpdateWheelVisual(rearRightCollider, rearRightMesh, 3);
    }

    /// <summary>
    /// Holds the car still when idle and nearly stopped.
    /// Fixes slope/gravity creep without affecting launch, reverse, or rolling stops.
    /// </summary>
    private void ApplyStandstillHold()
    {
        // Any throttle/brake intent or airborne → leave normal physics alone.
        if (Mathf.Abs(moveInput) > 0.01f || IsStunned || !AnyWheelGrounded())
            return;

        Vector3 velocity = rb.linearVelocity;
        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
        float flatSpeed = flat.magnitude;

        // Still rolling to a stop — don't clamp yet (lets coast/brake finish naturally).
        if (flatSpeed > 0.55f)
            return;

        // Light parking brake so wheels don't freewheel under gravity.
        float holdBrake = Mathf.Max(1400f, physics.brakeForce * 0.4f);
        frontLeftCollider.brakeTorque = Mathf.Max(frontLeftCollider.brakeTorque, holdBrake);
        frontRightCollider.brakeTorque = Mathf.Max(frontRightCollider.brakeTorque, holdBrake);
        rearLeftCollider.brakeTorque = Mathf.Max(rearLeftCollider.brakeTorque, holdBrake);
        rearRightCollider.brakeTorque = Mathf.Max(rearRightCollider.brakeTorque, holdBrake);

        // Bleed residual horizontal/yaw creep. Keep vertical velocity so jumps/settling stay intact.
        float bleed = 3.5f * Time.fixedDeltaTime;
        velocity.x = Mathf.MoveTowards(velocity.x, 0f, bleed);
        velocity.z = Mathf.MoveTowards(velocity.z, 0f, bleed);
        rb.linearVelocity = velocity;

        Vector3 angular = rb.angularVelocity;
        angular.x = Mathf.MoveTowards(angular.x, 0f, bleed);
        angular.y = Mathf.MoveTowards(angular.y, 0f, bleed * 1.5f);
        angular.z = Mathf.MoveTowards(angular.z, 0f, bleed);
        rb.angularVelocity = angular;

        // Fully settle once basically stopped.
        if (flatSpeed < 0.08f && angular.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = new Vector3(0f, velocity.y, 0f);
            angular.x = 0f;
            angular.y = 0f;
            angular.z = 0f;
            rb.angularVelocity = angular;
        }
    }

    private void ApplyDriveTorque(float motor)
    {
        float frontMotor = 0f;
        float rearMotor = 0f;

        switch (physics.driveType)
        {
            case CarDriveType.FWD:
                frontMotor = motor;
                break;
            case CarDriveType.AWD:
                frontMotor = motor * 0.5f;
                rearMotor = motor * 0.5f;
                break;
            default:
                rearMotor = motor;
                break;
        }

        if (frontLeftCollider != null) frontLeftCollider.motorTorque = frontMotor;
        if (frontRightCollider != null) frontRightCollider.motorTorque = frontMotor;
        if (rearLeftCollider != null) rearLeftCollider.motorTorque = rearMotor;
        if (rearRightCollider != null) rearRightCollider.motorTorque = rearMotor;
    }

    private float GetSteerAngleLimit(float speedRatio)
    {
        float blend = Mathf.Pow(Mathf.Clamp01(speedRatio), physics.steerSpeedFalloff);
        return Mathf.Lerp(physics.maxSteerAngle, physics.minSteerAngle, blend);
    }

    private void UpdateSteering(float speedRatio)
    {
        float targetSteer = steerInputRaw;
        if (!analogSteerInput)
            targetSteer *= physics.keyboardSteerScale;

        steerTarget = targetSteer;

        bool releasing = Mathf.Abs(targetSteer) < 0.02f;
        bool countering = !releasing
            && Mathf.Abs(steerInput) > 0.04f
            && Mathf.Sign(targetSteer) != Mathf.Sign(steerInput);

        isCounterSteering = countering;

        float ramp;
        float rateScale;
        if (countering)
        {
            // Flip A↔D: rip through center much faster than normal ramp-in.
            ramp = physics.steerCounterRamp;
            // Keep counter-steer responsive even at speed.
            rateScale = Mathf.Lerp(1f, Mathf.Max(0.72f, physics.steerHighSpeedRate), speedRatio);
        }
        else if (releasing)
        {
            ramp = physics.steerRampOut;
            rateScale = Mathf.Lerp(1f, Mathf.Max(0.65f, physics.steerHighSpeedRate), speedRatio);
        }
        else
        {
            ramp = physics.steerRampIn;
            rateScale = Mathf.Lerp(1f, physics.steerHighSpeedRate, speedRatio);
        }

        float step = ramp * rateScale * Time.fixedDeltaTime;
        steerInput = Mathf.MoveTowards(steerInput, targetSteer, step);
    }

    /// <summary>
    /// Extra body yaw when the player flips steer (A→D). Wheels alone feel sluggish;
    /// a short torque makes the car rotate into the new direction.
    /// </summary>
    private void ApplyCounterSteerAssist(float speedRatio)
    {
        if (!isCounterSteering || rb == null || !AnyWheelGrounded())
            return;
        if (Speed < 1.5f || ForwardSpeed < -0.35f || handbrake)
            return;

        float flip = Mathf.Abs(steerTarget - steerInput);
        float strength = physics.counterSteerYaw
            * Mathf.Sign(steerTarget)
            * Mathf.Clamp01(Mathf.Abs(steerTarget))
            * Mathf.Lerp(0.55f, 1.1f, speedRatio)
            * Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(flip));

        rb.AddTorque(transform.up * strength);
    }

    public void StunFromImpact(float duration = -1f, float velocityRetention = 0.35f, float brakeScale = 1f)
    {
        float seconds = duration > 0f ? duration : physics.impactStopSeconds;
        stunUntil = Mathf.Max(stunUntil, Time.time + seconds);
        IsStunned = true;

        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (rb == null)
            return;

        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(v.x * velocityRetention, v.y, v.z * velocityRetention);
        rb.angularVelocity *= Mathf.Lerp(0.75f, 0.4f, brakeScale);
    }

    public void ApplyLightBump(float velocityRetention = 0.88f)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (rb == null)
            return;

        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(v.x * velocityRetention, v.y, v.z * velocityRetention);
        rb.angularVelocity *= 0.92f;
    }

    private void ApplyRigidbodySettings()
    {
        if (rb == null)
            return;

        rb.mass = physics.mass;
        rb.centerOfMass = physics.centerOfMass;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.angularDamping = 0.85f;
        rb.maxAngularVelocity = 7f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        StabilizeBarrierHit(collision);
    }

    private void StabilizeBarrierHit(Collision collision)
    {
        if (rb == null || collision == null || collision.contactCount == 0)
            return;

        Vector3 normal = Vector3.zero;
        for (int i = 0; i < collision.contactCount; i++)
            normal += collision.GetContact(i).normal;
        normal /= collision.contactCount;

        if (normal.y > 0.55f)
            return;

        Vector3 angular = rb.angularVelocity;
        angular -= transform.right * Vector3.Dot(angular, transform.right) * 0.9f;
        angular -= transform.forward * Vector3.Dot(angular, transform.forward) * 0.75f;
        rb.angularVelocity = angular;

        Vector3 velocity = rb.linearVelocity;
        if (velocity.y > 0.75f)
            velocity.y *= 0.35f;
        rb.linearVelocity = velocity;
    }

    private void ApplyRollStability()
    {
        if (!AnyWheelGrounded())
            return;

        Vector3 angular = rb.angularVelocity;
        float rollRate = Vector3.Dot(angular, transform.right);
        float pitchRate = Vector3.Dot(angular, transform.forward);
        rb.AddTorque(transform.right * (-rollRate * physics.rollStability));
        rb.AddTorque(transform.forward * (-pitchRate * physics.pitchStability));
    }

    private void UpdateDriftAngle(Vector3 velocity)
    {
        Vector3 flatVelocity = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;

        if (flatForward.sqrMagnitude > 0.001f && flatVelocity.sqrMagnitude > 0.25f)
            DriftAngle = Vector3.SignedAngle(flatForward.normalized, flatVelocity.normalized, Vector3.up);
        else
            DriftAngle = 0f;
    }

    private void ApplyGrip()
    {
        float gripScale = BaseSidewaysStiffness * assistGripMultiplier;
        float front = gripScale * physics.frontGrip;
        float rear = gripScale * (handbrake ? physics.handbrakeRearGrip : physics.rearGrip);

        // Foot brake: keep sideways bite so the car slows in line instead of swapping ends.
        if (IsFootBraking && !handbrake)
        {
            front *= 1.08f;
            rear *= 1.12f;
        }

        // Match revamp baseline stiffness (~1.6), scaled gently by forwardGrip.
        float forward = 1.6f * physics.forwardGrip;

        SetSidewaysStiffness(frontLeftCollider, front);
        SetSidewaysStiffness(frontRightCollider, front);
        SetSidewaysStiffness(rearLeftCollider, rear);
        SetSidewaysStiffness(rearRightCollider, rear);

        SetForwardStiffness(frontLeftCollider, forward);
        SetForwardStiffness(frontRightCollider, forward);
        SetForwardStiffness(rearLeftCollider, forward);
        SetForwardStiffness(rearRightCollider, forward);
    }

    /// <summary>
    /// Soft ABS: ease brake torque when a wheel is locking (high forward slip).
    /// </summary>
    private static float SoftAbsBrake(WheelCollider collider, float desiredBrake)
    {
        if (collider == null || desiredBrake < 1f)
            return desiredBrake;

        if (!collider.GetGroundHit(out WheelHit hit))
            return desiredBrake;

        float slip = Mathf.Abs(hit.forwardSlip);
        if (slip < 0.32f)
            return desiredBrake;

        float unlock = Mathf.InverseLerp(0.95f, 0.32f, slip);
        return desiredBrake * Mathf.Lerp(0.28f, 1f, unlock);
    }

    /// <summary>
    /// Cuts motor when drive wheels are spinning faster than the car is moving.
    /// </summary>
    private float TractionScale()
    {
        float avgRpm = 0f;
        int count = 0;
        switch (physics.driveType)
        {
            case CarDriveType.FWD:
                SampleRpm(frontLeftCollider, ref avgRpm, ref count);
                SampleRpm(frontRightCollider, ref avgRpm, ref count);
                break;
            case CarDriveType.AWD:
                SampleRpm(frontLeftCollider, ref avgRpm, ref count);
                SampleRpm(frontRightCollider, ref avgRpm, ref count);
                SampleRpm(rearLeftCollider, ref avgRpm, ref count);
                SampleRpm(rearRightCollider, ref avgRpm, ref count);
                break;
            default:
                SampleRpm(rearLeftCollider, ref avgRpm, ref count);
                SampleRpm(rearRightCollider, ref avgRpm, ref count);
                break;
        }

        if (count == 0)
            return 1f;

        avgRpm /= count;
        WheelCollider refWheel = physics.driveType == CarDriveType.FWD ? frontLeftCollider : rearLeftCollider;
        float radius = refWheel != null ? Mathf.Max(0.2f, refWheel.radius) : 0.35f;
        float expectedRpm = Mathf.Abs(ForwardSpeed) * 60f / (2f * Mathf.PI * radius);
        float overspin = avgRpm - expectedRpm;
        if (overspin < 90f)
            return 1f;

        float cut = Mathf.InverseLerp(90f, 420f, overspin);
        return Mathf.Lerp(1f, 0.45f, cut);
    }

    private static void SampleRpm(WheelCollider collider, ref float sum, ref int count)
    {
        if (collider == null)
            return;
        sum += Mathf.Abs(collider.rpm);
        count++;
    }

    private void ApplyBodyForces(float speedRatio, bool reversing)
    {
        rb.AddForce(-transform.up * physics.downforce * Speed * Speed);

        if (!AnyWheelGrounded())
            return;

        Vector3 angular = rb.angularVelocity;
        float yawRate = Vector3.Dot(angular, transform.up);

        if (handbrake && Speed > 3f)
        {
            float yawTorque = steerInput * physics.handbrakeYaw * (0.4f + speedRatio);
            rb.AddTorque(transform.up * yawTorque);
            ClampYawRate(yawRate, angular);
            return;
        }

        if (Speed < 2.5f)
            return;

        // Reverse: damp only. Any align torque fights the rear axle and jerks L/R.
        if (reversing)
        {
            float reverseDamp = -yawRate * 2200f * Mathf.Lerp(0.7f, 1.15f, speedRatio);
            rb.AddTorque(transform.up * reverseDamp);

            float reverseCap = Mathf.Min(1.15f, physics.maxYawRate * 0.45f);
            // Allow intentional reverse turns, but kill oscillation.
            if (Mathf.Abs(steerInput) < 0.08f && Mathf.Abs(yawRate) > reverseCap)
            {
                float clamped = Mathf.Clamp(yawRate, -reverseCap, reverseCap);
                rb.angularVelocity = angular - transform.up * (yawRate - clamped);
            }
            else if (Mathf.Abs(yawRate) > physics.maxYawRate * 0.85f)
            {
                float clamped = Mathf.Clamp(yawRate, -physics.maxYawRate * 0.85f, physics.maxYawRate * 0.85f);
                rb.angularVelocity = angular - transform.up * (yawRate - clamped);
            }
            return;
        }

        // Always damp yaw so small tire force noise cannot build into a wobble.
        float damp = -yawRate * 1550f * Mathf.Lerp(0.55f, 1.05f, speedRatio);

        float align = 0f;
        if (!IsFootBraking && !isCounterSteering)
        {
            // Deadzone: do not hunt toward velocity when nearly straight (was causing forward wobble).
            float absDrift = Mathf.Abs(DriftAngle);
            if (absDrift > 5f)
            {
                float scale = Mathf.InverseLerp(5f, 28f, absDrift);
                // Ease off while the player is steering so turns don't get yanked straight.
                float steerEase = Mathf.Lerp(1f, 0.25f, Mathf.Abs(steerInput));
                align = -DriftAngle * physics.driftAlignStrength * 0.32f
                    * (0.25f + speedRatio * 0.55f) * scale * steerEase;
            }
        }

        rb.AddTorque(transform.up * (align + damp));

        if (Mathf.Abs(yawRate) > physics.maxYawRate)
        {
            float clamped = Mathf.Clamp(yawRate, -physics.maxYawRate, physics.maxYawRate);
            rb.angularVelocity = angular - transform.up * (yawRate - clamped);
        }
    }

    private void ClampYawRate(float yawRate, Vector3 angular)
    {
        if (Mathf.Abs(yawRate) <= physics.maxYawRate)
            return;

        float clamped = Mathf.Clamp(yawRate, -physics.maxYawRate, physics.maxYawRate);
        rb.angularVelocity = angular - transform.up * (yawRate - clamped);
    }

    private void UpdateSkidState(float speedRatio)
    {
        MaxSidewaysSlip = 0f;
        MaxSidewaysSlip = Mathf.Max(MaxSidewaysSlip, ReadSlip(frontLeftCollider));
        MaxSidewaysSlip = Mathf.Max(MaxSidewaysSlip, ReadSlip(frontRightCollider));
        MaxSidewaysSlip = Mathf.Max(MaxSidewaysSlip, ReadSlip(rearLeftCollider));
        MaxSidewaysSlip = Mathf.Max(MaxSidewaysSlip, ReadSlip(rearRightCollider));

        float slipRef = Mathf.Max(0.15f, physics.skidSlipReference);
        SkidIntensity = Mathf.Clamp01(MaxSidewaysSlip / slipRef);

        if (handbrake && Speed > 4f)
            SkidIntensity = Mathf.Max(SkidIntensity, 0.45f + speedRatio * 0.35f);

        IsDrifting = (handbrake && Speed > 4f)
            || (Speed > 5f && SkidIntensity > 0.35f && Mathf.Abs(DriftAngle) > physics.driftAngleThreshold);
    }

    private void ReadInput()
    {
        moveInput = 0f;
        steerInputRaw = 0f;
        handbrake = false;
        analogSteerInput = false;

        if (DevInputBlocked)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                moveInput += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                moveInput -= 1f;

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                steerInputRaw += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                steerInputRaw -= 1f;

            handbrake = keyboard.spaceKey.isPressed;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stick = gamepad.leftStick.ReadValue();
            if (Mathf.Abs(stick.x) > 0.05f)
            {
                steerInputRaw = stick.x;
                analogSteerInput = true;
            }
            if (Mathf.Abs(stick.y) > 0.05f)
                moveInput = stick.y;

            float throttleTrigger = gamepad.rightTrigger.ReadValue();
            float brakeTrigger = gamepad.leftTrigger.ReadValue();
            if (throttleTrigger > 0.05f || brakeTrigger > 0.05f)
                moveInput = throttleTrigger - brakeTrigger;

            handbrake = handbrake || gamepad.buttonSouth.isPressed;
        }

        moveInput = Mathf.Clamp(moveInput, -1f, 1f);
        steerInputRaw = Mathf.Clamp(steerInputRaw, -1f, 1f);
        Throttle = moveInput;
    }

    private bool AnyWheelGrounded()
    {
        return frontLeftCollider.isGrounded
            || frontRightCollider.isGrounded
            || rearLeftCollider.isGrounded
            || rearRightCollider.isGrounded;
    }

    private static float ReadSlip(WheelCollider collider)
    {
        if (collider == null || !collider.GetGroundHit(out WheelHit hit))
            return 0f;

        return Mathf.Abs(hit.sidewaysSlip);
    }

    private static void SetupWheelFriction(WheelCollider collider)
    {
        if (collider == null)
            return;

        // Stable arcade curves (revamp baseline). Over-stiff forward was locking under brake → slip/wobble.
        WheelFrictionCurve forward = collider.forwardFriction;
        forward.extremumSlip = 0.35f;
        forward.extremumValue = 1.05f;
        forward.asymptoteSlip = 0.75f;
        forward.asymptoteValue = 0.7f;
        forward.stiffness = 1.6f;
        collider.forwardFriction = forward;

        WheelFrictionCurve sideways = collider.sidewaysFriction;
        sideways.extremumSlip = 0.22f;
        sideways.extremumValue = 1f;
        sideways.asymptoteSlip = 0.55f;
        sideways.asymptoteValue = 0.75f;
        sideways.stiffness = BaseSidewaysStiffness;
        collider.sidewaysFriction = sideways;
    }

    private void SetupWheelFriction()
    {
        SetupWheelFriction(frontLeftCollider);
        SetupWheelFriction(frontRightCollider);
        SetupWheelFriction(rearLeftCollider);
        SetupWheelFriction(rearRightCollider);
    }

    private static void SetSidewaysStiffness(WheelCollider collider, float stiffness)
    {
        if (collider == null)
            return;

        WheelFrictionCurve curve = collider.sidewaysFriction;
        curve.stiffness = stiffness;
        collider.sidewaysFriction = curve;
    }

    private static void SetForwardStiffness(WheelCollider collider, float stiffness)
    {
        if (collider == null)
            return;

        WheelFrictionCurve curve = collider.forwardFriction;
        curve.stiffness = stiffness;
        collider.forwardFriction = curve;
    }

    private void UpdateWheelVisual(WheelCollider collider, Transform meshTransform, int index)
    {
        if (collider == null || meshTransform == null)
            return;

        Transform pivot = meshTransform.parent != null && meshTransform.parent != transform
            ? meshTransform.parent
            : meshTransform;

        collider.GetWorldPose(out Vector3 position, out _);
        pivot.position = position;

        wheelSpin[index] += collider.rpm * 6f * Time.fixedDeltaTime;
        pivot.localRotation = Quaternion.Euler(wheelSpin[index], collider.steerAngle, 0f);
    }

    private static void RecenterPivotOnMesh(Transform meshTransform)
    {
        if (meshTransform == null || meshTransform.parent == null)
            return;

        MeshFilter filter = meshTransform.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null)
            return;

        Transform pivot = meshTransform.parent;
        Vector3 worldCenter = meshTransform.TransformPoint(filter.sharedMesh.bounds.center);

        meshTransform.SetParent(null, true);
        pivot.position = worldCenter;
        meshTransform.SetParent(pivot, true);
    }
}
