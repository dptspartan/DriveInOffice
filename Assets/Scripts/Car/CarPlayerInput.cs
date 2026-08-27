using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class CarPlayerInput : MonoBehaviour
{
    public CarControlScheme controlScheme = CarControlScheme.KeyboardWASD;

    public void Read(out float throttle, out float steer, out bool handbrake, out bool analogSteer)
    {
        throttle = 0f;
        steer = 0f;
        handbrake = false;
        analogSteer = false;

        switch (controlScheme)
        {
            case CarControlScheme.KeyboardArrows:
                ReadKeyboardArrows(out throttle, out steer, out handbrake);
                break;
            case CarControlScheme.Gamepad0:
                ReadGamepad(0, out throttle, out steer, out handbrake, out analogSteer);
                break;
            case CarControlScheme.Gamepad1:
                ReadGamepad(1, out throttle, out steer, out handbrake, out analogSteer);
                break;
            default:
                ReadKeyboardWasd(out throttle, out steer, out handbrake);
                break;
        }
    }

    private static void ReadKeyboardWasd(out float throttle, out float steer, out bool handbrake)
    {
        throttle = 0f;
        steer = 0f;
        handbrake = false;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.wKey.isPressed)
            throttle += 1f;
        if (keyboard.sKey.isPressed)
            throttle -= 1f;

        if (keyboard.dKey.isPressed)
            steer += 1f;
        if (keyboard.aKey.isPressed)
            steer -= 1f;

        handbrake = keyboard.spaceKey.isPressed;
    }

    private static void ReadKeyboardArrows(out float throttle, out float steer, out bool handbrake)
    {
        throttle = 0f;
        steer = 0f;
        handbrake = false;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.upArrowKey.isPressed)
            throttle += 1f;
        if (keyboard.downArrowKey.isPressed)
            throttle -= 1f;

        if (keyboard.rightArrowKey.isPressed)
            steer += 1f;
        if (keyboard.leftArrowKey.isPressed)
            steer -= 1f;

        handbrake = keyboard.rightCtrlKey.isPressed || keyboard.leftCtrlKey.isPressed;
    }

    private static void ReadGamepad(
        int index,
        out float throttle,
        out float steer,
        out bool handbrake,
        out bool analogSteer)
    {
        throttle = 0f;
        steer = 0f;
        handbrake = false;
        analogSteer = false;

        if (index < 0 || index >= Gamepad.all.Count)
            return;

        Gamepad gamepad = Gamepad.all[index];
        if (gamepad == null)
            return;

        Vector2 stick = gamepad.leftStick.ReadValue();
        if (Mathf.Abs(stick.x) > 0.05f)
        {
            steer = stick.x;
            analogSteer = true;
        }

        if (Mathf.Abs(stick.y) > 0.05f)
            throttle = stick.y;

        float throttleTrigger = gamepad.rightTrigger.ReadValue();
        float brakeTrigger = gamepad.leftTrigger.ReadValue();
        if (throttleTrigger > 0.05f || brakeTrigger > 0.05f)
            throttle = throttleTrigger - brakeTrigger;

        handbrake = gamepad.buttonEast.isPressed || gamepad.buttonSouth.isPressed;
    }
}
