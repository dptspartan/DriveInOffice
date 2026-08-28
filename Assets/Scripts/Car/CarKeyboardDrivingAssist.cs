using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Light keyboard-only driving assist: a bit more grip/steer response and gentle slide recovery.
/// Does not steer or throttle for the player. Toggle in the Inspector or dev panel (M).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(KenneyCarController))]
[DefaultExecutionOrder(-40)]
public class CarKeyboardDrivingAssist : MonoBehaviour
{
    [SerializeField] bool assistEnabled = true;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("0 = off effect, 1 = full light assist.")]
    float assistStrength = 0.4f;

    private KenneyCarController controller;
    private Rigidbody rb;

    public bool AssistEnabled
    {
        get => assistEnabled;
        set => assistEnabled = value;
    }

    public float AssistStrength
    {
        get => assistStrength;
        set => assistStrength = Mathf.Clamp01(value);
    }

    private void Awake()
    {
        controller = GetComponent<KenneyCarController>();
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (controller == null)
            return;

        if (!assistEnabled || !IsKeyboardDriving())
        {
            controller.assistGripMultiplier = 1f;
            controller.assistSteerMultiplier = 1f;
            return;
        }

        float strength = assistStrength;
        controller.assistGripMultiplier = 1f + 0.14f * strength;
        controller.assistSteerMultiplier = 1f + 0.1f * strength;
        ApplySlideRecovery(strength);
    }

    private void ApplySlideRecovery(float strength)
    {
        if (rb == null || controller.IsHandbraking || controller.Speed < 3.5f)
            return;

        float drift = controller.DriftAngle;
        if (Mathf.Abs(drift) < 6f)
            return;

        float speedRatio = controller.physics.maxSpeed > 0.01f
            ? Mathf.Clamp01(controller.Speed / controller.physics.maxSpeed)
            : 0f;

        float torque = -drift * 4f * strength * (0.3f + speedRatio * 0.7f);
        rb.AddTorque(transform.up * torque, ForceMode.Acceleration);
    }

    private static bool IsKeyboardDriving()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stick = gamepad.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.04f)
                return false;

            if (gamepad.rightTrigger.ReadValue() > 0.15f || gamepad.leftTrigger.ReadValue() > 0.15f)
                return false;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        return keyboard.wKey.isPressed
            || keyboard.sKey.isPressed
            || keyboard.aKey.isPressed
            || keyboard.dKey.isPressed
            || keyboard.upArrowKey.isPressed
            || keyboard.downArrowKey.isPressed
            || keyboard.leftArrowKey.isPressed
            || keyboard.rightArrowKey.isPressed;
    }
}
