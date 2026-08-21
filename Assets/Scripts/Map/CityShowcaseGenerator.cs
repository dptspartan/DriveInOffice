using System;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-200)]
public class CityShowcaseGenerator : MonoBehaviour
{
    // Kept for later map tools. Not attached to CityShowcase — that scene uses a baked CityMap.
    public const float Tile = 10f;
    public const int Grid = 16;
    public const int Block = 4;

    [Header("Placement")]
    public Vector3 carSpawn = new Vector3(40f, 0.4f, 20f);
    public Vector3 carEuler = new Vector3(0f, 90f, 0f);

    private const int GroundLayer = 6;
    private const int ObstacleLayer = 7;
    private bool generating;

    private GameObject roadStraight;
    private GameObject roadStraightBarrier;
    private GameObject roadCrossing;
    private GameObject roadCrossroad;
    private GameObject roadCrossroadLine;
    private GameObject roadCrossroadPath;
    private GameObject roadTee;
    private GameObject roadTeeBarrier;
    private GameObject roadBendBarrier;
    private GameObject roadDriveway;
    private GameObject tileLow;
    private GameObject[] buildings = Array.Empty<GameObject>();
    private GameObject[] sidewalks = Array.Empty<GameObject>();
    private GameObject[] parkedCars = Array.Empty<GameObject>();
    private GameObject fountain;
    private GameObject palm;
    private GameObject[] props = Array.Empty<GameObject>();
    private GameObject drivableCar;

    private void OnEnable()
    {
        if (!Application.isPlaying || generating)
            return;
        if (transform.Find("Roads") != null)
            return;
        Generate();
    }

    private void Start()
    {
        if (Application.isPlaying && transform.Find("Roads") == null)
            Generate();
    }

    [ContextMenu("Generate City")]
    public void Generate()
    {
        if (generating)
            return;

        generating = true;
        try
        {
            ClearChildren();
            LoadPrefabs();

            Transform roads = Child("Roads");
            Transform lots = Child("CityBlocks");
            Transform extra = Child("Props");
            Transform parked = Child("ParkedCars");
            Transform walls = Child("Constraints");

            BuildRoadGrid(roads);
            BuildCityBlocks(lots, extra, parked);
            BuildEdgeConstraints(walls);
            PlacePlayer();
            EnsureSceneCamera();
            ApplyPerformance();

            Debug.Log($"CityShowcaseGenerator built {transform.hierarchyCount} objects.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            generating = false;
        }
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Kill(transform.GetChild(i).gameObject);
    }

    private void LoadPrefabs()
    {
        roadStraight = Load("Assets/Prefabs/Map/Roads/Road_Straight.prefab");
        roadStraightBarrier = Load("Assets/Prefabs/Map/Roads/Road_Straight_Barrier.prefab");
        roadCrossing = Load("Assets/Prefabs/Map/Roads/Road_Crossing.prefab");
        roadCrossroad = Load("Assets/Prefabs/Map/Roads/Road_Crossroad.prefab");
        roadCrossroadLine = Load("Assets/Prefabs/Map/Roads/Road_Crossroad_Line.prefab");
        roadCrossroadPath = Load("Assets/Prefabs/Map/Roads/Road_Crossroad_Path.prefab");
        roadTee = Load("Assets/Prefabs/Map/Roads/Road_Intersection.prefab");
        roadTeeBarrier = Load("Assets/Prefabs/Map/Roads/Road_Intersection_Barrier.prefab");
        roadBendBarrier = Load("Assets/Prefabs/Map/Roads/Road_Bend_Square_Barrier.prefab");
        roadDriveway = Load("Assets/Prefabs/Map/Roads/Road_Driveway_Single.prefab");
        tileLow = Load("Assets/Prefabs/Map/Tiles/Tile_Low.prefab");
        fountain = Load("Assets/ithappy/Cartoon_City_Free/Prefabs/Props/Fountain_03.prefab");
        palm = Load("Assets/ithappy/Cartoon_City_Free/Prefabs/Vegetation/Palm_03.prefab");
        drivableCar = Load("Assets/Prefabs/Cars/DrivableCar.prefab");

        buildings = LoadMany(
            "Assets/ithappy/Cartoon_City_Free/Prefabs/Buildings/Eco_Building_Grid.prefab",
            "Assets/ithappy/Cartoon_City_Free/Prefabs/Buildings/Eco_Building_Terrace.prefab",
            "Assets/ithappy/Cartoon_City_Free/Prefabs/Buildings/Eco_Building_Slope.prefab",
            "Assets/ithappy/Cartoon_City_Free/Prefabs/Buildings/Regular_Building_TwistedTower_Large.prefab");

        sidewalks = LoadMany(
            "Assets/ithappy/Cartoon_City_Free/Prefabs/Sidewalks/Set_B_Tiles_01.prefab",
            "Assets/Prefabs/Map/Tiles/Tile_Low.prefab");

        parkedCars = LoadMany(
            "Assets/ithappy/Cartoon_City_Free/Prefabs/Cars/Car_06.prefab",
            "Assets/ithappy/Cartoon_City_Free/Prefabs/Cars/Car_13.prefab",
            "Assets/ithappy/Cartoon_City_Free/Prefabs/Cars/Car_16.prefab");

        props = LoadMany(
            "Assets/ithappy/Cartoon_City_Free/Prefabs/Props/Trash_Can_04.prefab",
            "Assets/ithappy/Cartoon_City_Free/Prefabs/Props/Bus_Stop_02.prefab",
            "Assets/Prefabs/Map/Props/Prop_Dumpster.prefab",
            "Assets/Prefabs/Map/Roads/Road_Sign_Stop.prefab");

        if (roadStraight == null)
            Debug.LogError("CityShowcaseGenerator could not load Kenney road prefabs from Assets/Prefabs/Map/Roads.");
        if (buildings.Length == 0)
            Debug.LogError("CityShowcaseGenerator could not load Cartoon City building prefabs.");
    }

    private void BuildRoadGrid(Transform parent)
    {
        for (int x = 0; x <= Grid; x++)
        {
            for (int z = 0; z <= Grid; z++)
            {
                bool roadX = x % Block == 0;
                bool roadZ = z % Block == 0;
                if (!roadX && !roadZ)
                    continue;

                bool edgeX = x == 0 || x == Grid;
                bool edgeZ = z == 0 || z == Grid;
                Vector3 pos = new Vector3(x * Tile, 0f, z * Tile);
                GameObject prefab;
                float yaw;

                if (roadX && roadZ)
                {
                    if (edgeX && edgeZ)
                    {
                        prefab = First(roadBendBarrier, roadStraight);
                        yaw = CornerYaw(x, z);
                    }
                    else if (edgeX || edgeZ)
                    {
                        prefab = First(roadTeeBarrier, roadTee, roadCrossroad, roadStraight);
                        yaw = TeeYaw(x, z, edgeZ);
                    }
                    else
                    {
                        int mix = (x + z) % 3;
                        prefab = mix == 0 ? First(roadCrossroad, roadStraight)
                            : mix == 1 ? First(roadCrossroadLine, roadStraight)
                            : First(roadCrossroadPath, roadStraight);
                        yaw = 0f;
                    }
                }
                else
                {
                    bool ns = roadX;
                    yaw = ns ? 0f : 90f;
                    bool outer = (ns && edgeX) || (!ns && edgeZ);
                    if (outer)
                        prefab = First(roadStraightBarrier, roadStraight);
                    else if ((x + z) % 5 == 0)
                        prefab = First(roadCrossing, roadStraight);
                    else if ((x + z) % 7 == 0)
                        prefab = First(roadDriveway, roadStraight);
                    else
                        prefab = roadStraight;
                }

                if (!Spawn(prefab, pos, Quaternion.Euler(0f, yaw, 0f), parent, $"Road_{x}_{z}", GroundLayer))
                    SpawnFallbackRoad(pos, yaw, parent, $"Road_{x}_{z}");
            }
        }
    }

    private static float CornerYaw(int x, int z)
    {
        bool minX = x == 0;
        bool minZ = z == 0;
        if (minX && !minZ) return 0f;
        if (!minX && !minZ) return 90f;
        if (!minX && minZ) return 180f;
        return 270f;
    }

    private static float TeeYaw(int x, int z, bool edgeZ)
    {
        if (edgeZ && z == Grid) return 0f;
        if (edgeZ && z == 0) return 180f;
        if (x == Grid) return 90f;
        return 270f;
    }

    private void BuildCityBlocks(Transform lots, Transform extra, Transform parked)
    {
        int seed = 0;
        for (int bx = 0; bx < Grid / Block; bx++)
        {
            for (int bz = 0; bz < Grid / Block; bz++)
            {
                Vector3 center = BlockCenter(bx, bz);
                Transform block = Child($"Block_{bx}_{bz}", lots);
                bool plaza = bx == 1 && bz == 1;
                FillLotFloor(block, bx, bz, plaza);
                if (plaza)
                    BuildPlaza(block, center);
                else
                    ScatterBuildings(block, center, seed);
                ScatterProps(extra, center, seed, plaza);
                if (!plaza)
                    ParkCars(parked, center, seed);
                AddBlockCurbs(block, center);
                seed++;
            }
        }
    }

    private void FillLotFloor(Transform parent, int bx, int bz, bool plaza)
    {
        Vector3 pos = BlockCenter(bx, bz);
        GameObject prefab = plaza || sidewalks.Length == 0 ? tileLow : sidewalks[(bx + bz) % sidewalks.Length];
        GameObject tile = PlaceFitted(prefab, pos, Quaternion.identity, parent, (Block - 1) * Tile - 0.4f, 0.05f, 8f);
        if (tile == null)
            tile = SpawnFallbackBox(pos + Vector3.up * 0.04f, new Vector3(28.8f, 0.08f, 28.8f), parent, $"Lot_{bx}_{bz}", new Color(0.55f, 0.55f, 0.5f));
        if (tile != null)
        {
            SetLayerRecursive(tile, GroundLayer);
            SetCheapRendering(tile, false);
        }
    }

    private void ScatterBuildings(Transform parent, Vector3 center, int seed)
    {
        Vector3[] offsets =
        {
            new Vector3(-7.5f, 0f, -7.5f),
            new Vector3(7.5f, 0f, -7.5f),
            new Vector3(-7.5f, 0f, 7.5f),
            new Vector3(7.5f, 0f, 7.5f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 pos = center + offsets[i];
            GameObject prefab = buildings.Length == 0 ? null : buildings[(seed + i) % buildings.Length];
            GameObject building = PlaceFitted(prefab, pos, Quaternion.Euler(0f, 90f * ((seed + i) % 4), 0f), parent, 12.5f, 0.08f, 0.9f);
            if (building == null)
            {
                float height = 18f + (seed + i) % 5 * 6f;
                building = SpawnFallbackBox(pos + Vector3.up * (height * 0.5f), new Vector3(10f, height, 10f), parent, $"Building_{seed}_{i}", new Color(0.35f, 0.55f, 0.75f));
            }

            SetLayerRecursive(building, ObstacleLayer);
            EnsureMeshColliders(building);
            SetCheapRendering(building, true);
        }
    }

    private void BuildPlaza(Transform parent, Vector3 center)
    {
        GameObject fountainGo = PlaceFitted(fountain, center, Quaternion.identity, parent, 10f, 0.12f, 0.7f);
        if (fountainGo == null)
            fountainGo = SpawnFallbackBox(center + Vector3.up, new Vector3(6f, 2f, 6f), parent, "Fountain", new Color(0.3f, 0.55f, 0.8f));
        SetLayerRecursive(fountainGo, ObstacleLayer);
        EnsureMeshColliders(fountainGo);
        SetCheapRendering(fountainGo, false);

        Vector3[] ring =
        {
            new Vector3(8f, 0f, 0f), new Vector3(-8f, 0f, 0f),
            new Vector3(0f, 0f, 8f), new Vector3(0f, 0f, -8f)
        };
        foreach (Vector3 offset in ring)
        {
            GameObject tree = PlaceFitted(palm, center + offset, Quaternion.Euler(0f, offset.x * 12f, 0f), parent, 3.2f, 0.15f, 1.3f);
            if (tree == null)
                tree = SpawnFallbackBox(center + offset + Vector3.up * 3f, new Vector3(0.6f, 6f, 0.6f), parent, "Palm", new Color(0.2f, 0.55f, 0.25f));
            SetLayerRecursive(tree, ObstacleLayer);
            SetCheapRendering(tree, false);
        }
    }

    private void ScatterProps(Transform parent, Vector3 center, int seed, bool plaza)
    {
        if (plaza)
            return;
        Vector3[] spots =
        {
            new Vector3(13.2f, 0f, 3f),
            new Vector3(-13.2f, 0f, -4f),
            new Vector3(4f, 0f, 13.2f),
            new Vector3(-5f, 0f, -13.2f)
        };

        for (int i = 0; i < 2; i++)
        {
            GameObject prefab = props.Length == 0 ? null : props[(seed + i) % props.Length];
            GameObject instance = PlaceFitted(prefab, center + spots[i], Quaternion.Euler(0f, 45f * i, 0f), parent, 2.4f, 0.12f, 2.8f);
            if (instance == null)
                continue;
            SetLayerRecursive(instance, ObstacleLayer);
            SetCheapRendering(instance, false);
        }
    }

    private void ParkCars(Transform parent, Vector3 center, int seed)
    {
        Vector3[] spots =
        {
            new Vector3(11.5f, 0f, -4f),
            new Vector3(-11.5f, 0f, 5f)
        };

        for (int i = 0; i < 1; i++)
        {
            GameObject prefab = parkedCars.Length == 0 ? null : parkedCars[(seed + i) % parkedCars.Length];
            GameObject instance = PlaceFitted(prefab, center + spots[i], Quaternion.Euler(0f, 90f * (i + seed), 0f), parent, 4.4f, 0.15f, 1.2f);
            if (instance == null)
                continue;
            SetLayerRecursive(instance, ObstacleLayer);
            EnsureMeshColliders(instance);
            SetCheapRendering(instance, false);
            foreach (Rigidbody rb in instance.GetComponentsInChildren<Rigidbody>(true))
                Kill(rb);
        }
    }

    private void AddBlockCurbs(Transform parent, Vector3 center)
    {
        float half = (Block - 1) * Tile * 0.5f;
        float height = 1.35f;
        float thick = 0.55f;
        float length = (Block - 1) * Tile - 0.4f;
        MakeWall(parent, center + new Vector3(half, height * 0.5f, 0f), new Vector3(thick, height, length), "Curb_E");
        MakeWall(parent, center + new Vector3(-half, height * 0.5f, 0f), new Vector3(thick, height, length), "Curb_W");
        MakeWall(parent, center + new Vector3(0f, height * 0.5f, half), new Vector3(length, height, thick), "Curb_N");
        MakeWall(parent, center + new Vector3(0f, height * 0.5f, -half), new Vector3(length, height, thick), "Curb_S");
    }

    private void BuildEdgeConstraints(Transform parent)
    {
        float min = -Tile * 0.5f;
        float max = Grid * Tile + Tile * 0.5f;
        float span = max - min;
        float mid = (min + max) * 0.5f;
        float h = 6f;
        float t = 2.2f;
        MakeWall(parent, new Vector3(mid, h * 0.5f, max + t * 0.5f), new Vector3(span + t * 2f, h, t), "Edge_N");
        MakeWall(parent, new Vector3(mid, h * 0.5f, min - t * 0.5f), new Vector3(span + t * 2f, h, t), "Edge_S");
        MakeWall(parent, new Vector3(max + t * 0.5f, h * 0.5f, mid), new Vector3(t, h, span), "Edge_E");
        MakeWall(parent, new Vector3(min - t * 0.5f, h * 0.5f, mid), new Vector3(t, h, span), "Edge_W");
    }

    private void PlacePlayer()
    {
        if (drivableCar == null)
        {
            Debug.LogError("CityShowcaseGenerator missing DrivableCar prefab.");
            return;
        }

        GameObject car = InstantiatePrefab(drivableCar, transform);
        if (car == null)
            return;

        car.name = "DrivableCar";
        car.transform.SetPositionAndRotation(carSpawn, Quaternion.Euler(carEuler));
        KenneyCarController controller = car.GetComponentInChildren<KenneyCarController>(true);
        if (controller != null && controller.GetComponent<CarImpactStop>() == null)
            controller.gameObject.AddComponent<CarImpactStop>();

        Camera cam = car.GetComponentInChildren<Camera>(true);
        if (cam != null)
            cam.farClipPlane = 180f;
    }

    private void EnsureSceneCamera()
    {
        if (FindAnyObjectByType<Camera>() != null)
            return;

        GameObject camObject = new GameObject("CityCamera");
        camObject.transform.SetParent(transform, false);
        camObject.transform.position = carSpawn + new Vector3(0f, 18f, -22f);
        camObject.transform.LookAt(carSpawn);
        Camera camera = camObject.AddComponent<Camera>();
        camera.farClipPlane = 180f;
        camObject.AddComponent<AudioListener>();
    }

    private static void MakeWall(Transform parent, Vector3 pos, Vector3 size, string name)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent, false);
        wall.transform.position = pos;
        wall.transform.localScale = size;
        MeshRenderer renderer = wall.GetComponent<MeshRenderer>();
        if (renderer != null)
            Kill(renderer);
        MeshFilter filter = wall.GetComponent<MeshFilter>();
        if (filter != null)
            Kill(filter);
        wall.layer = ObstacleLayer;
    }

    private void SpawnFallbackRoad(Vector3 pos, float yaw, Transform parent, string name)
    {
        GameObject road = SpawnFallbackBox(pos + Vector3.up * 0.05f, new Vector3(10f, 0.1f, 10f), parent, name, new Color(0.18f, 0.18f, 0.2f));
        road.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        SetLayerRecursive(road, GroundLayer);
    }

    private static GameObject SpawnFallbackBox(Vector3 pos, Vector3 size, Transform parent, string name, Color color)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.position = pos;
        box.transform.localScale = size;
        MeshRenderer renderer = box.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = new Material(renderer.sharedMaterial);
            renderer.sharedMaterial.color = color;
        }

        return box;
    }

    private GameObject PlaceFitted(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent, float targetSpan, float minScale, float maxScale)
    {
        if (prefab == null)
            return null;

        GameObject instance = InstantiatePrefab(prefab, parent);
        if (instance == null)
            return null;

        instance.transform.SetPositionAndRotation(pos, rot);
        Bounds bounds = Encapsulate(instance);
        float span = Mathf.Max(bounds.size.x, bounds.size.z);
        if (span > 0.01f)
        {
            float scale = Mathf.Clamp(targetSpan / span, minScale, maxScale);
            instance.transform.localScale *= scale;
            bounds = Encapsulate(instance);
        }

        Vector3 aligned = pos - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        instance.transform.position += aligned;
        return instance;
    }

    private bool Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent, string name, int layer)
    {
        if (prefab == null)
            return false;
        GameObject instance = InstantiatePrefab(prefab, parent);
        if (instance == null)
            return false;
        instance.name = name;
        instance.transform.SetPositionAndRotation(pos, rot);
        SetLayerRecursive(instance, layer);
        SetCheapRendering(instance, false);
        return true;
    }

    private static void Kill(UnityEngine.Object target)
    {
        if (target == null)
            return;
        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    private static GameObject InstantiatePrefab(GameObject prefab, Transform parent)
    {
        if (prefab == null)
            return null;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
#endif
        return Instantiate(prefab, parent);
    }

    private static Bounds Encapsulate(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        bool has = false;
        Bounds bounds = new Bounds(go.transform.position, Vector3.one);
        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled)
                continue;
            if (!has)
            {
                bounds = renderer.bounds;
                has = true;
            }
            else
                bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }

    private static void EnsureMeshColliders(GameObject go)
    {
        if (go == null)
            return;
        MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter filter in filters)
        {
            if (filter.sharedMesh == null || filter.GetComponent<Collider>() != null)
                continue;
            MeshCollider col = filter.gameObject.AddComponent<MeshCollider>();
            col.sharedMesh = filter.sharedMesh;
        }
    }

    private static void SetCheapRendering(GameObject go, bool castShadows)
    {
        if (go == null)
            return;

        foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        foreach (Light light in go.GetComponentsInChildren<Light>(true))
            light.enabled = false;

        foreach (ParticleSystem particles in go.GetComponentsInChildren<ParticleSystem>(true))
            particles.gameObject.SetActive(false);
    }

    private static void ApplyPerformance()
    {
        if (!Application.isPlaying)
            return;

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        QualitySettings.shadows = ShadowQuality.HardOnly;
        QualitySettings.shadowDistance = 70f;
        QualitySettings.shadowResolution = ShadowResolution.Low;
        QualitySettings.lodBias = 0.7f;
        QualitySettings.particleRaycastBudget = 0;
        QualitySettings.realtimeReflectionProbes = false;
        RenderSettings.fog = false;
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        if (go == null)
            return;
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    private static Vector3 BlockCenter(int bx, int bz)
    {
        return new Vector3(bx * Block * Tile + Block * Tile * 0.5f, 0f, bz * Block * Tile + Block * Tile * 0.5f);
    }

    private Transform Child(string name, Transform parent = null)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent != null ? parent : transform, false);
        return go.transform;
    }

    private static GameObject First(params GameObject[] options)
    {
        foreach (GameObject option in options)
        {
            if (option != null)
                return option;
        }

        return null;
    }

    private static GameObject[] LoadMany(params string[] paths)
    {
        var list = new System.Collections.Generic.List<GameObject>();
        foreach (string path in paths)
        {
            GameObject prefab = Load(path);
            if (prefab != null)
                list.Add(prefab);
        }

        return list.ToArray();
    }

    private static GameObject Load(string path)
    {
#if UNITY_EDITOR
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            Debug.LogWarning("CityShowcaseGenerator missing " + path);
        return prefab;
#else
        return null;
#endif
    }
}
