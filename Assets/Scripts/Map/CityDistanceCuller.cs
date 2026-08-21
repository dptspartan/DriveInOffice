using UnityEngine;

[DefaultExecutionOrder(-50)]
public class CityDistanceCuller : MonoBehaviour
{
    public Transform buildingsRoot;
    public Transform propsRoot;
    public Transform roadsRoot;
    public float buildingDistance = 210f;
    public float propDistance = 95f;
    public float roadDistance = 250f;
    public float hysteresis = 25f;
    public float updateInterval = 0.12f;

    private Transform follow;
    private Renderer[][] groups;
    private float[] hideSqr;
    private float[] showSqr;
    private float nextUpdate;

    private void Awake()
    {
        Camera cam = GetComponentInChildren<Camera>(true);
        if (cam == null)
            cam = FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            follow = cam.transform;
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, roadDistance + 20f);
        }

        groups = new[]
        {
            Collect(buildingsRoot),
            Collect(propsRoot),
            Collect(roadsRoot)
        };
        hideSqr = new[]
        {
            buildingDistance * buildingDistance,
            propDistance * propDistance,
            roadDistance * roadDistance
        };
        showSqr = new[]
        {
            (buildingDistance + hysteresis) * (buildingDistance + hysteresis),
            (propDistance + hysteresis) * (propDistance + hysteresis),
            (roadDistance + hysteresis) * (roadDistance + hysteresis)
        };
    }

    private void LateUpdate()
    {
        if (follow == null || Time.unscaledTime < nextUpdate)
            return;

        nextUpdate = Time.unscaledTime + updateInterval;
        Vector3 origin = follow.position;
        for (int g = 0; g < groups.Length; g++)
        {
            Renderer[] renderers = groups[g];
            float hide = hideSqr[g];
            float show = showSqr[g];
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;
                float dist = (renderer.bounds.center - origin).sqrMagnitude;
                if (renderer.enabled)
                {
                    if (dist > hide)
                        renderer.enabled = false;
                }
                else if (dist < show)
                    renderer.enabled = true;
            }
        }
    }

    private static Renderer[] Collect(Transform root)
    {
        if (root == null)
            return System.Array.Empty<Renderer>();
        return root.GetComponentsInChildren<Renderer>(true);
    }
}
