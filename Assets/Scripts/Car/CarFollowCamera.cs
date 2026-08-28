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
    private Vector3 followVelocity;
    private float reverseBlend;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        transform.localScale = Vector3.one;

        if (target == null)
        {
            KenneyCarController found = FindAnyObjectByType<KenneyCarController>();
            if (found != null)
            {
                car = found;
                target = found.transform;
            }
        }
        else if (car == null)
        {
            car = target.GetComponent<KenneyCarController>();
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        float speed = car != null ? car.Speed : 0f;
        float forwardSpeed = car != null ? car.ForwardSpeed : 0f;
        bool reversing = forwardSpeed < -0.4f;
        reverseBlend = Mathf.MoveTowards(reverseBlend, reversing ? 1f : 0f, Time.deltaTime * 3f);

        float drift = car != null ? car.DriftAngle : 0f;
        if (reversing || Mathf.Abs(drift) > 80f)
            drift = 0f;
        float driftNorm = Mathf.Clamp(drift / 40f, -1f, 1f) * (1f - reverseBlend);

        float pullBack = Mathf.Clamp01(speed / 28f) * speedPullBack;
        Vector3 dynamicOffset = offset;
        dynamicOffset.z -= pullBack;

        // In reverse, follow a yaw-smoothed forward so car wobble doesn't whip the camera.
        Vector3 followForward = target.forward;
        if (reverseBlend > 0.01f)
        {
            Vector3 flatFwd = target.forward;
            flatFwd.y = 0f;
            if (flatFwd.sqrMagnitude > 0.001f)
                flatFwd.Normalize();
            else
                flatFwd = Vector3.forward;

            Vector3 stableFwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (stableFwd.sqrMagnitude < 0.001f)
                stableFwd = flatFwd;
            else
                stableFwd.Normalize();

            followForward = Vector3.Slerp(flatFwd, stableFwd, reverseBlend * 0.75f).normalized;
        }

        Vector3 desired = target.position
            + Vector3.up * dynamicOffset.y
            + followForward * dynamicOffset.z
            + target.right * dynamicOffset.x;
        desired += target.right * driftNorm * driftLateral;

        float smooth = Mathf.Lerp(followSmooth, followSmooth * 1.8f, reverseBlend);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref followVelocity, smooth);

        Vector3 lookPoint = target.position + Vector3.up * lookHeight;
        if (!reversing)
            lookPoint += target.forward * (Mathf.Max(0f, forwardSpeed) * lookAhead);
        lookPoint += target.right * driftNorm * 0.6f;

        Vector3 toLook = lookPoint - transform.position;
        if (toLook.sqrMagnitude < 0.001f)
            return;

        Quaternion desiredRotation = Quaternion.LookRotation(toLook, Vector3.up);
        float rotSharp = Mathf.Lerp(rotationSharpness, rotationSharpness * 0.35f, reverseBlend);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            1f - Mathf.Exp(-rotSharp * Time.deltaTime));

        if (cam != null)
            cam.fieldOfView = Mathf.Lerp(minFov, maxFov, Mathf.Clamp01(speed / 28f));
    }
}
