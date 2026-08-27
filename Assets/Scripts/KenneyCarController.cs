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
    [Tooltip("Future CarDrivingAssist will scale grip through this multiplier.")]
    [Range(0.5f, 1.5f)]
    public float assistGripMultiplier = 1f;

    [Tooltip("Future CarDrivingAssist will scale steering through this multiplier.")]
    [Range(0.5f, 1.5f)]
    public float assistSteerMultiplier = 1f;

    [Header("Input")]
    public CarPlayerInput playerInput;
    public bool useExternalInput;

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

    public float maxSpeed => physics.maxSpeed;

    private Rigidbody rb;
    private float stunUntil;
    private float moveInput;
    private float steerInputRaw;
    private float steerInput;
    private bool handbrake;
    private bool analogSteerInput;
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
            float powerScale = 1f - speedRatio * speedRatio;
            motor = moveInput * physics.motorPower * powerScale;
        }
        else if (moveInput < -0.01f)
        {
            if (ForwardSpeed > 1f)
                footBrake = -moveInput * physics.brakeForce;
            else
                motor = moveInput * physics.motorPower * physics.reversePower;
        }
        else if (Mathf.Abs(ForwardSpeed) > 0.5f)
        {
            coastBrake = physics.coastBrake * speedRatio;
        }

        IsFootBraking = footBrake > 0.01f;

        float frontBrake = Mathf.Max(footBrake, coastBrake);
        float rearBrake = Mathf.Max(footBrake, coastBrake);
        if (handbrake)
            rearBrake = Mathf.Max(rearBrake, physics.handbrakeForce);

        if (IsStunned)
        {
            motor = 0f;
            frontBrake = physics.impactBrakeForce;
            rearBrake = physics.impactBrakeForce;
        }

        rearLeftCollider.motorTorque = motor;
        rearRightCollider.motorTorque = motor;
        frontLeftCollider.motorTorque = 0f;
        frontRightCollider.motorTorque = 0f;

        frontLeftCollider.brakeTorque = frontBrake;
        frontRightCollider.brakeTorque = frontBrake;
        rearLeftCollider.brakeTorque = rearBrake;
        rearRightCollider.brakeTorque = rearBrake;

        float steerAngle = steerInput
            * GetSteerAngleLimit(speedRatio)
            * assistSteerMultiplier;

        frontLeftCollider.steerAngle = steerAngle;
        frontRightCollider.steerAngle = steerAngle;

        ApplyGrip();
        ApplyBodyForces(speedRatio);
        UpdateSkidState(speedRatio);

        UpdateWheelVisual(frontLeftCollider, frontLeftMesh, 0);
        UpdateWheelVisual(frontRightCollider, frontRightMesh, 1);
        UpdateWheelVisual(rearLeftCollider, rearLeftMesh, 2);
        UpdateWheelVisual(rearRightCollider, rearRightMesh, 3);
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

        bool releasing = Mathf.Abs(targetSteer) < 0.01f
            || Mathf.Sign(targetSteer) != Mathf.Sign(steerInput);
        float ramp = releasing ? physics.steerRampOut : physics.steerRampIn;
        float rateScale = Mathf.Lerp(1f, physics.steerHighSpeedRate, speedRatio);
        float step = ramp * rateScale * Time.fixedDeltaTime;

        steerInput = Mathf.MoveTowards(steerInput, targetSteer, step);
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
        rb.angularDamping = 0.5f;
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

        SetSidewaysStiffness(frontLeftCollider, front);
        SetSidewaysStiffness(frontRightCollider, front);
        SetSidewaysStiffness(rearLeftCollider, rear);
        SetSidewaysStiffness(rearRightCollider, rear);
    }

    private void ApplyBodyForces(float speedRatio)
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
        }
        else if (Speed > 4f)
        {
            float align = -DriftAngle * physics.driftAlignStrength * (0.35f + speedRatio * 0.65f);
            float damp = -yawRate * 900f * speedRatio;
            rb.AddTorque(transform.up * (align + damp));
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
        if (useExternalInput && playerInput != null)
        {
            playerInput.Read(out moveInput, out steerInputRaw, out handbrake, out analogSteerInput);
            moveInput = Mathf.Clamp(moveInput, -1f, 1f);
            steerInputRaw = Mathf.Clamp(steerInputRaw, -1f, 1f);
            Throttle = moveInput;
            return;
        }

        ReadDefaultInput();
    }

    private void ReadDefaultInput()
    {
        moveInput = 0f;
        steerInputRaw = 0f;
        handbrake = false;
        analogSteerInput = false;

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

        WheelFrictionCurve forward = collider.forwardFriction;
        forward.extremumSlip = 0.35f;
        forward.extremumValue = 1f;
        forward.asymptoteSlip = 0.75f;
        forward.asymptoteValue = 0.65f;
        forward.stiffness = 1.6f;
        collider.forwardFriction = forward;

        WheelFrictionCurve sideways = collider.sidewaysFriction;
        sideways.extremumSlip = 0.22f;
        sideways.extremumValue = 1f;
        sideways.asymptoteSlip = 0.55f;
        sideways.asymptoteValue = 0.7f;
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
