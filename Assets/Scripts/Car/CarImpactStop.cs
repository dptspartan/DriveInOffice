using UnityEngine;

[RequireComponent(typeof(KenneyCarController))]
public class CarImpactStop : MonoBehaviour
{
    [SerializeField] private KenneyCarController car;
    [SerializeField] private float minImpactSpeed = 5.5f;
    [SerializeField] private float maxGroundNormalY = 0.62f;
    [SerializeField] private float cooldown = 0.45f;

    private float nextAllowedTime;

    private void Awake()
    {
        if (car == null)
            car = GetComponent<KenneyCarController>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryStop(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (car != null && car.IsStunned)
            return;
        if (collision.relativeVelocity.magnitude < minImpactSpeed * 1.35f)
            return;
        TryStop(collision);
    }

    private void TryStop(Collision collision)
    {
        if (car == null || collision == null || collision.contactCount == 0)
            return;
        if (Time.time < nextAllowedTime)
            return;
        if (collision.collider == null)
            return;

        int layer = collision.collider.gameObject.layer;
        if (layer == LayerMask.NameToLayer("Ground"))
            return;

        if (collision.relativeVelocity.magnitude < minImpactSpeed)
            return;

        Vector3 normal = Vector3.zero;
        int count = collision.contactCount;
        for (int i = 0; i < count; i++)
            normal += collision.GetContact(i).normal;
        if (count > 0)
            normal /= count;

        if (normal.y > maxGroundNormalY)
            return;

        nextAllowedTime = Time.time + cooldown;
        car.StunFromImpact();
    }
}
