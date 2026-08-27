using UnityEngine;

[DefaultExecutionOrder(100)]
public class CarFollowCamera : MonoBehaviour
{
    public Transform target;
    public KenneyCarController car;
    public Vector3 offset = new Vector3(0f, 1.05f, -3f);
    public float followSmooth = 0.14f;
    public float lookHeight = 0.55f;
    public float lookAhead = 0.06f;
    public float minFov = 55f;
    public float maxFov = 62f;
    public float driftLateral = 0.55f;
    public float rotationSharpness = 8f;
    public float speedPullBack = 0.65f;

    private Camera cam;
    private AudioListener audioListener;
    private Vector3 followVelocity;
    private Rect viewportRect = new Rect(0f, 0f, 1f, 1f);

    private void Awake()
    {
        cam = GetComponent<Camera>();
        audioListener = GetComponent<AudioListener>();
        transform.localScale = Vector3.one;
        ApplyViewportRect();

        if (target == null)
        {
            KenneyCarController found = FindAnyObjectByType<KenneyCarController>();
            if (found != null)
                Bind(found.transform, found);
        }
        else if (car == null)
        {
            car = target.GetComponent<KenneyCarController>();
        }
    }

    public void Bind(Transform followTarget, KenneyCarController controller)
    {
        target = followTarget;
        car = controller;
    }

    public void SetViewportRect(Rect rect)
    {
        viewportRect = rect;
        ApplyViewportRect();
    }

    public void SetPrimaryAudioListener(bool enabled)
    {
        if (audioListener != null)
            audioListener.enabled = enabled;
    }

    public void SetMainCameraTag(bool isMain)
    {
        gameObject.tag = isMain ? "MainCamera" : "Untagged";
    }

    private void ApplyViewportRect()
    {
        if (cam != null)
            cam.rect = viewportRect;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        float speed = car != null ? car.Speed : 0f;
        float forwardSpeed = car != null ? car.ForwardSpeed : 0f;
        bool reversing = forwardSpeed < -0.4f;

        float drift = car != null ? car.DriftAngle : 0f;
        if (reversing || Mathf.Abs(drift) > 80f)
            drift = 0f;
        float driftNorm = Mathf.Clamp(drift / 40f, -1f, 1f);

        float pullBack = Mathf.Clamp01(speed / 28f) * speedPullBack;
        Vector3 dynamicOffset = offset;
        dynamicOffset.z -= pullBack;

        Vector3 desired = target.TransformPoint(dynamicOffset);
        desired += target.right * driftNorm * driftLateral;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref followVelocity, followSmooth);

        Vector3 lookPoint = target.position + Vector3.up * lookHeight;
        if (!reversing)
            lookPoint += target.forward * (Mathf.Max(0f, forwardSpeed) * lookAhead);
        lookPoint += target.right * driftNorm * 0.6f;

        Vector3 toLook = lookPoint - transform.position;
        if (toLook.sqrMagnitude < 0.001f)
            return;

        Quaternion desiredRotation = Quaternion.LookRotation(toLook, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));

        if (cam != null)
            cam.fieldOfView = Mathf.Lerp(minFov, maxFov, Mathf.Clamp01(speed / 28f));
    }
}
