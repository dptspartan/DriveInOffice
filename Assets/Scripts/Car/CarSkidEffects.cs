using UnityEngine;
using UnityEngine.Rendering;

public class CarSkidEffects : MonoBehaviour
{
    public KenneyCarController car;
    public float slipThreshold = 0.3f;
    public float minSpeed = 3.5f;
    public float markWidth = 0.2f;
    public float markLifetime = 28f;
    public float groundOffset = 0.03f;
    public float smokeRate = 28f;
    public float exhaustIdleRate = 6f;
    public float exhaustThrottleRate = 16f;
    public Vector3 exhaustLocalOffset = new Vector3(0f, 0.16f, -1.12f);

    private WheelCollider[] wheels;
    private TrailRenderer[] marks;
    private ParticleSystem[] smoke;
    private ParticleSystem exhaust;
    private Transform markRoot;

    private void Start()
    {
        if (car == null)
            car = GetComponent<KenneyCarController>();
        if (car == null)
            return;

        wheels = new[]
        {
            car.frontLeftCollider,
            car.frontRightCollider,
            car.rearLeftCollider,
            car.rearRightCollider
        };

        markRoot = new GameObject("SkidMarks").transform;
        markRoot.SetParent(null);

        Material markMaterial = CreateMarkMaterial();
        Material smokeMaterial = CreateSmokeMaterial();

        marks = new TrailRenderer[wheels.Length];
        smoke = new ParticleSystem[wheels.Length];

        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] == null)
                continue;

            marks[i] = CreateMark(i, markMaterial);
            smoke[i] = CreateTireSmoke(i, smokeMaterial);
        }

        exhaust = CreateExhaustSmoke(smokeMaterial);
    }

    private void LateUpdate()
    {
        if (wheels == null)
            return;

        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] == null)
                continue;

            WheelHit hit = default;
            bool grounded = wheels[i].GetGroundHit(out hit);
            float intensity = car != null ? car.SkidIntensity : 0f;
            bool skidding = car != null
                && car.Speed > minSpeed
                && grounded
                && (car.IsHandbraking
                    || car.IsDrifting
                    || intensity >= slipThreshold
                    || (car.IsFootBraking && intensity > slipThreshold * 0.65f));

            if (marks[i] != null)
            {
                if (grounded)
                {
                    marks[i].transform.position = hit.point + hit.normal * groundOffset;
                    Vector3 ahead = car != null ? car.transform.forward : transform.forward;
                    if (ahead.sqrMagnitude > 0.001f)
                        marks[i].transform.rotation = Quaternion.LookRotation(ahead, hit.normal);

                    float width = markWidth * Mathf.Lerp(0.7f, 1.25f, intensity);
                    marks[i].startWidth = width;
                    marks[i].endWidth = width;
                }

                marks[i].emitting = skidding && grounded;
            }

            if (smoke[i] != null)
            {
                smoke[i].transform.position = TireSmokePosition(wheels[i], grounded, hit, i);
                Vector3 puffDir = -car.transform.forward * 0.75f + Vector3.up;
                if (puffDir.sqrMagnitude > 0.001f)
                    smoke[i].transform.rotation = Quaternion.LookRotation(puffDir.normalized, Vector3.up);

                ParticleSystem.EmissionModule emission = smoke[i].emission;
                bool rearWheel = i >= 2;
                float rate = skidding && grounded && rearWheel
                    ? smokeRate * Mathf.Lerp(0.45f, 1.25f, intensity)
                    : 0f;
                emission.rateOverTime = rate;
            }
        }

        UpdateExhaust();
    }

    private void OnDestroy()
    {
        if (markRoot != null)
            Destroy(markRoot.gameObject);
        if (exhaust != null)
            Destroy(exhaust.gameObject);
    }

    private TrailRenderer CreateMark(int index, Material material)
    {
        GameObject markObject = new GameObject("SkidMark_" + index);
        markObject.transform.SetParent(markRoot, true);

        TrailRenderer trail = markObject.AddComponent<TrailRenderer>();
        trail.time = markLifetime;
        trail.minVertexDistance = 0.1f;
        trail.widthMultiplier = 1f;
        trail.startWidth = markWidth;
        trail.endWidth = markWidth;
        trail.numCapVertices = 1;
        trail.numCornerVertices = 1;
        trail.alignment = LineAlignment.TransformZ;
        trail.textureMode = LineTextureMode.Stretch;
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.material = material;
        trail.emitting = false;
        trail.autodestruct = false;
        return trail;
    }

    private Vector3 TireSmokePosition(WheelCollider wheel, bool grounded, WheelHit hit, int index)
    {
        wheel.GetWorldPose(out Vector3 wheelPos, out _);
        Vector3 outward = (index % 2 == 0) ? -car.transform.right : car.transform.right;
        if (grounded)
            return Vector3.Lerp(hit.point, wheelPos, 0.55f) + hit.normal * 0.06f + outward * 0.06f;

        return wheelPos + Vector3.down * (wheel.radius * 0.4f) + outward * 0.06f;
    }

    private void UpdateExhaust()
    {
        if (exhaust == null || car == null)
            return;

        exhaust.transform.localPosition = exhaustLocalOffset;
        ParticleSystem.EmissionModule emission = exhaust.emission;
        float throttle = Mathf.Max(0f, car.Throttle);
        emission.rateOverTime = exhaustIdleRate + exhaustThrottleRate * throttle;
    }

    private ParticleSystem CreateTireSmoke(int index, Material material)
    {
        GameObject smokeObject = new GameObject("TireSmoke_" + index);
        smokeObject.transform.SetParent(markRoot, true);

        ParticleSystem particles = ConfigureSmoke(
            smokeObject,
            material,
            startLifetimeMin: 0.85f,
            startLifetimeMax: 1.45f,
            startSpeedMin: 0.35f,
            startSpeedMax: 1.1f,
            startSizeMin: 0.55f,
            startSizeMax: 1.05f,
            startColor: new Color(0.86f, 0.86f, 0.86f, 0.7f),
            maxParticles: 48,
            gravity: -0.35f,
            shapeRadius: 0.16f,
            sizeEnd: 1.55f,
            worldSpace: true);

        particles.Play();
        return particles;
    }

    private ParticleSystem CreateExhaustSmoke(Material material)
    {
        GameObject exhaustObject = new GameObject("ExhaustSmoke");
        exhaustObject.transform.SetParent(car.transform, false);
        exhaustObject.transform.localPosition = exhaustLocalOffset;
        exhaustObject.transform.localRotation = Quaternion.Euler(18f, 180f, 0f);

        ParticleSystem particles = ConfigureSmoke(
            exhaustObject,
            material,
            startLifetimeMin: 0.35f,
            startLifetimeMax: 0.7f,
            startSpeedMin: 0.45f,
            startSpeedMax: 1.15f,
            startSizeMin: 0.12f,
            startSizeMax: 0.24f,
            startColor: new Color(0.55f, 0.55f, 0.58f, 0.45f),
            maxParticles: 24,
            gravity: -0.15f,
            shapeRadius: 0.04f,
            sizeEnd: 1.35f,
            worldSpace: true);

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.62f, 0.62f, 0.65f), 0f),
                new GradientColorKey(new Color(0.78f, 0.78f, 0.8f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.5f, 0f),
                new GradientAlphaKey(0.18f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = gradient;

        particles.Play();
        return particles;
    }

    private static ParticleSystem ConfigureSmoke(
        GameObject host,
        Material material,
        float startLifetimeMin,
        float startLifetimeMax,
        float startSpeedMin,
        float startSpeedMax,
        float startSizeMin,
        float startSizeMax,
        Color startColor,
        int maxParticles,
        float gravity,
        float shapeRadius,
        float sizeEnd,
        bool worldSpace)
    {
        ParticleSystem particles = host.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(startLifetimeMin, startLifetimeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(startSpeedMin, startSpeedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(startSizeMin, startSizeMax);
        main.startColor = startColor;
        main.maxParticles = maxParticles;
        main.simulationSpace = worldSpace
            ? ParticleSystemSimulationSpace.World
            : ParticleSystemSimulationSpace.Local;
        main.gravityModifier = gravity;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 22f;
        shape.radius = shapeRadius;

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.92f, 0.92f, 0.92f), 0f),
                new GradientColorKey(new Color(0.75f, 0.75f, 0.75f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.72f, 0f),
                new GradientAlphaKey(0.32f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = gradient;

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, sizeEnd));

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.material = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;

        return particles;
    }

    private static Material CreateMarkMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        Color color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else
            material.color = color;
        return material;
    }

    private static Material CreateSmokeMaterial()
    {
        Texture2D texture = LoadSmokeTexture();

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        Color color = new Color(1f, 1f, 1f, 1f);
        if (material.HasProperty("_BaseMap") && texture != null)
            material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex") && texture != null)
            material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        material.color = color;

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
        return material;
    }

    private static Texture2D LoadSmokeTexture()
    {
        Texture2D texture = Resources.Load<Texture2D>("smoke_round");
        if (texture == null)
            texture = CreateFallbackPuffTexture();

        if (texture != null)
        {
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
        }

        return texture;
    }

    private static Texture2D CreateFallbackPuffTexture()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        float cx = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx) / (size * 0.48f);
                float dy = (y - cx) / (size * 0.48f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float t = Mathf.Clamp01(1f - d);
                float a = t * t * (3f - 2f * t);
                a *= a;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        texture.Apply(false, false);
        return texture;
    }
}
