using UnityEngine;

[RequireComponent(typeof(KenneyCarController))]
public class CarImpactStop : MonoBehaviour
{
    [SerializeField] private KenneyCarController car;
    [SerializeField] private float minImpactSpeed = 8.5f;
    [SerializeField] private float fullStunSpeed = 13f;
    [SerializeField] private float minHeadOnSpeed = 5.5f;
    [SerializeField] private float grazeTangentRatio = 1.75f;
    [SerializeField] private float maxGroundNormalY = 0.62f;
    [SerializeField] private float cooldown = 0.55f;
    [SerializeField] private float lightBumpCooldown = 0.15f;

    private float nextAllowedTime;
    private float nextBumpTime;

    private void Awake()
    {
        if (car == null)
            car = GetComponent<KenneyCarController>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryStop(collision);
    }

    private void TryStop(Collision collision)
    {
        if (car == null || collision == null || collision.contactCount == 0)
            return;
        if (collision.collider == null)
            return;

        if (IsIgnoredCollider(collision.collider))
            return;

        Vector3 normal = AverageContactNormal(collision);
        if (normal.sqrMagnitude < 0.001f)
            return;

        if (normal.y > maxGroundNormalY)
            return;

        Vector3 relativeVelocity = collision.relativeVelocity;
        float impactSpeed = relativeVelocity.magnitude;
        if (impactSpeed < minHeadOnSpeed * 0.5f)
            return;

        float headOnSpeed = Mathf.Abs(Vector3.Dot(relativeVelocity, normal));
        Vector3 tangentVelocity = relativeVelocity - normal * Vector3.Dot(relativeVelocity, normal);
        float tangentSpeed = tangentVelocity.magnitude;

        if (headOnSpeed < minHeadOnSpeed && tangentSpeed > headOnSpeed * grazeTangentRatio)
            return;

        if (impactSpeed < minImpactSpeed)
        {
            TryLightBump(headOnSpeed, impactSpeed);
            return;
        }

        if (Time.time < nextAllowedTime)
            return;

        float severity = Mathf.InverseLerp(minImpactSpeed, fullStunSpeed, Mathf.Max(headOnSpeed, impactSpeed * 0.65f));
        if (severity < 0.25f)
        {
            TryLightBump(headOnSpeed, impactSpeed);
            return;
        }

        nextAllowedTime = Time.time + cooldown;

        float duration = car.physics.impactStopSeconds * Mathf.Lerp(0.35f, 1f, severity);
        float velocityRetention = Mathf.Lerp(0.82f, 0.35f, severity);
        car.StunFromImpact(duration, velocityRetention, severity);
    }

    private void TryLightBump(float headOnSpeed, float impactSpeed)
    {
        if (Time.time < nextBumpTime)
            return;
        if (headOnSpeed < minHeadOnSpeed * 0.35f && impactSpeed < minImpactSpeed * 0.75f)
            return;

        nextBumpTime = Time.time + lightBumpCooldown;
        car.ApplyLightBump(Mathf.Lerp(0.94f, 0.82f, headOnSpeed / minImpactSpeed));
    }

    private static Vector3 AverageContactNormal(Collision collision)
    {
        Vector3 normal = Vector3.zero;
        int count = collision.contactCount;
        for (int i = 0; i < count; i++)
            normal += collision.GetContact(i).normal;

        if (count > 0)
            normal /= count;

        return normal;
    }

    private static bool IsIgnoredCollider(Collider collider)
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0 && collider.gameObject.layer == groundLayer)
            return true;

        for (Transform node = collider.transform; node != null; node = node.parent)
        {
            string name = node.name.ToLowerInvariant();
            if (name.Contains("light-curved")
                || name.Contains("light-square")
                || name.Contains("electricity-pole")
                || name.Contains("electricity-side"))
            {
                return true;
            }
        }

        return false;
    }
}
