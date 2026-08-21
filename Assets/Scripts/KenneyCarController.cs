using UnityEngine;
using UnityEngine.InputSystem;

public class KenneyCarController : MonoBehaviour
{
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

    [Header("Engine")]
    public float motorForce = 1550f;
    public float maxSpeed = 18f;
    public AnimationCurve torqueCurve;

    [Header("Brakes")]
    public float footBrakeForce = 2800f;
    public float handbrakeForce = 3500f;
    public float engineBrakeForce = 1400f;

    [Header("Steering")]
    public float maxSteerAngle = 17f;
    public float minSteerAngle = 8f;
    public float steerSpeed = 2.4f;
    [Range(1f, 2.5f)] public float steerFalloff = 1.25f;
    [Range(0.2f, 1f)] public float highSpeedSteerRate = 0.38f;

    [Header("Grip")]
    public float frontSidewaysStiffness = 2.4f;
    public float rearSidewaysStiffness = 2.35f;
    public float handbrakeRearStiffness = 0.65f;

    [Header("Assist")]
    public float downforce = 16f;
    public float stabilityYaw = 2100f;
    public float handbrakeYaw = 320f;
    public float maxSpinRate = 1.8f;

    [Header("Impact")]
    public float impactStopSeconds = 1.35f;
    public float impactBrakeForce = 8000f;

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

    private Rigidbody rb;
    private float stunUntil;
    private float moveInput;
    private float steerInputRaw;
    private float steerInput;
    private bool handbrake;
    private readonly float[] wheelSpin = new float[4];

    private void Awake()
    {
        if (torqueCurve == null || torqueCurve.length == 0)
            torqueCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.28f);
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.centerOfMass = new Vector3(0f, 0.16f, 0.05f);
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.angularDamping = 0.55f;
        }

        ApplyBaseFriction(frontLeftCollider, frontSidewaysStiffness);
        ApplyBaseFriction(frontRightCollider, frontSidewaysStiffness);
        ApplyBaseFriction(rearLeftCollider, rearSidewaysStiffness);
        ApplyBaseFriction(rearRightCollider, rearSidewaysStiffness);

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

        Vector3 flatVelocity = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude > 0.001f && flatVelocity.sqrMagnitude > 0.25f)
            DriftAngle = Vector3.SignedAngle(flatForward.normalized, flatVelocity.normalized, Vector3.up);
        else
            DriftAngle = 0f;

        float speedFactor = Mathf.Clamp01(Speed / maxSpeed);
        float forwardSpeed = Vector3.Dot(velocity, transform.forward);
        ForwardSpeed = forwardSpeed;
        IsHandbraking = handbrake;

        float steerRate = Mathf.Lerp(steerSpeed, steerSpeed * highSpeedSteerRate, speedFactor);
        steerInput = Mathf.MoveTowards(steerInput, steerInputRaw, steerRate * Time.fixedDeltaTime);

        float motor = 0f;
        float footBrake = 0f;
        float coastBrake = 0f;
        if (moveInput > 0.01f)
        {
            float turnLoad = Mathf.Abs(steerInput) * Mathf.InverseLerp(0.35f, 1f, speedFactor);
            motor = moveInput * motorForce * torqueCurve.Evaluate(speedFactor) * Mathf.Lerp(1f, 0.72f, turnLoad);
        }
        else if (moveInput < -0.01f)
        {
            if (forwardSpeed > 1f)
                footBrake = -moveInput * footBrakeForce;
            else
                motor = moveInput * motorForce * 0.55f;
        }
        else if (Mathf.Abs(forwardSpeed) > 0.2f)
        {
            coastBrake = engineBrakeForce * Mathf.Lerp(0.45f, 1f, speedFactor);
        }

        IsFootBraking = footBrake > 0.01f;

        float frontBrake = Mathf.Max(footBrake, coastBrake);
        float rearBrake = Mathf.Max(footBrake, coastBrake);
        if (handbrake)
            rearBrake = Mathf.Max(rearBrake, handbrakeForce);
        if (IsStunned)
        {
            motor = 0f;
            frontBrake = impactBrakeForce;
            rearBrake = impactBrakeForce;
        }

        rearLeftCollider.motorTorque = motor;
        rearRightCollider.motorTorque = motor;
        frontLeftCollider.motorTorque = 0f;
        frontRightCollider.motorTorque = 0f;

        frontLeftCollider.brakeTorque = frontBrake;
        frontRightCollider.brakeTorque = frontBrake;
        rearLeftCollider.brakeTorque = rearBrake;
        rearRightCollider.brakeTorque = rearBrake;

        float steerBlend = Mathf.SmoothStep(0f, 1f, Mathf.Pow(speedFactor, steerFalloff));
        float steer = steerInput * Mathf.Lerp(maxSteerAngle, minSteerAngle, steerBlend);
        float slipLimit = 1f - Mathf.InverseLerp(8f, 20f, Mathf.Abs(DriftAngle)) * Mathf.Lerp(0.15f, 0.4f, speedFactor);
        if (!handbrake)
            steer *= Mathf.Clamp(slipLimit, 0.55f, 1f);
        frontLeftCollider.steerAngle = steer;
        frontRightCollider.steerAngle = steer;

        UpdateRearGrip(speedFactor, IsFootBraking);
        ApplyAssists(velocity, speedFactor);

        MaxSidewaysSlip = 0f;
        MaxSidewaysSlip = Mathf.Max(MaxSidewaysSlip, ReadSlip(frontLeftCollider));
        MaxSidewaysSlip = Mathf.Max(MaxSidewaysSlip, ReadSlip(frontRightCollider));
        MaxSidewaysSlip = Mathf.Max(MaxSidewaysSlip, ReadSlip(rearLeftCollider));
        MaxSidewaysSlip = Mathf.Max(MaxSidewaysSlip, ReadSlip(rearRightCollider));

        float speedSkid = Mathf.InverseLerp(maxSpeed * 0.45f, maxSpeed, Speed);
        SkidIntensity = 0f;
        if (handbrake)
            SkidIntensity = Mathf.Clamp01(0.45f + speedFactor);
        else
            SkidIntensity = Mathf.Clamp01(Mathf.Max(MaxSidewaysSlip * speedFactor, speedSkid * Mathf.Abs(DriftAngle) / 35f));

        IsDrifting = handbrake
            || (Speed > 6f && SkidIntensity > 0.35f && (Mathf.Abs(DriftAngle) > 10f || IsFootBraking));

        UpdateWheelVisual(frontLeftCollider, frontLeftMesh, 0);
        UpdateWheelVisual(frontRightCollider, frontRightMesh, 1);
        UpdateWheelVisual(rearLeftCollider, rearLeftMesh, 2);
        UpdateWheelVisual(rearRightCollider, rearRightMesh, 3);
    }

    public void StunFromImpact(float duration = -1f)
    {
        float seconds = duration > 0f ? duration : impactStopSeconds;
        stunUntil = Mathf.Max(stunUntil, Time.time + seconds);
        IsStunned = true;

        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (rb == null)
            return;

        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(v.x * 0.35f, v.y, v.z * 0.35f);
        rb.angularVelocity *= 0.4f;
    }

    private void ReadInput()
    {
        moveInput = 0f;
        steerInputRaw = 0f;
        handbrake = false;

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
                steerInputRaw = stick.x;
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

    private void UpdateRearGrip(float speedFactor, bool footBraking)
    {
        float speedLoss = Mathf.InverseLerp(0.55f, 1f, speedFactor);
        float front = Mathf.Lerp(frontSidewaysStiffness, frontSidewaysStiffness * 0.94f, speedLoss);
        float rear = Mathf.Lerp(rearSidewaysStiffness, rearSidewaysStiffness * 0.9f, speedLoss);

        if (footBraking)
        {
            front *= Mathf.Lerp(1f, 0.88f, speedLoss);
            rear *= Mathf.Lerp(1f, 0.8f, speedLoss);
        }

        if (handbrake)
            rear = handbrakeRearStiffness;

        SetSidewaysStiffness(frontLeftCollider, front);
        SetSidewaysStiffness(frontRightCollider, front);
        SetSidewaysStiffness(rearLeftCollider, rear);
        SetSidewaysStiffness(rearRightCollider, rear);
    }

    private void ApplyAssists(Vector3 velocity, float speedFactor)
    {
        rb.AddForce(-transform.up * downforce * velocity.sqrMagnitude);

        if (!AnyWheelGrounded())
            return;

        Vector3 angular = rb.angularVelocity;
        float yawRate = Vector3.Dot(angular, transform.up);

        if (handbrake)
        {
            float initiate = steerInput * handbrakeYaw * (0.35f + speedFactor);
            float damp = yawRate * 550f;
            rb.AddTorque(transform.up * (initiate - damp));

            if (Mathf.Abs(yawRate) > maxSpinRate)
            {
                float clamped = Mathf.Clamp(yawRate, -maxSpinRate, maxSpinRate);
                rb.angularVelocity = angular - transform.up * (yawRate - clamped);
            }
        }
        else
        {
            float straighten = -DriftAngle * 22f * (0.45f + speedFactor);
            float damp = -yawRate * stabilityYaw * Mathf.Lerp(0.28f, 0.55f, speedFactor);
            rb.AddTorque(transform.up * (straighten + damp));
        }
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

    private static void ApplyBaseFriction(WheelCollider collider, float sidewaysStiffness)
    {
        if (collider == null)
            return;

        WheelFrictionCurve forward = collider.forwardFriction;
        forward.extremumSlip = 0.4f;
        forward.extremumValue = 1f;
        forward.asymptoteSlip = 0.8f;
        forward.asymptoteValue = 0.55f;
        forward.stiffness = 1.8f;
        collider.forwardFriction = forward;

        WheelFrictionCurve sideways = collider.sidewaysFriction;
        sideways.extremumSlip = 0.2f;
        sideways.extremumValue = 1f;
        sideways.asymptoteSlip = 0.5f;
        sideways.asymptoteValue = 0.75f;
        sideways.stiffness = sidewaysStiffness;
        collider.sidewaysFriction = sideways;
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
