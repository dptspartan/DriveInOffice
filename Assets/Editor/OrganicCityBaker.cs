using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class OrganicCityBaker
{
    private const string ScenePath = "Assets/Scenes/CityShowcase.unity";
    private const float Tile = 10f;
    private const int GroundLayer = 6;
    private const int ObstacleLayer = 7;
    private const int N = 1;
    private const int E = 2;
    private const int S = 4;
    private const int W = 8;
    private const int MapX0 = 2;
    private const int MapZ0 = 2;
    private const int MapX1 = 78;
    private const int MapZ1 = 62;

    private static readonly HashSet<Vector2Int> Roads = new HashSet<Vector2Int>();
    private static readonly List<Vector4Int> Lots = new List<Vector4Int>();

    private struct Vector4Int
    {
        public int x0, z0, x1, z1;
        public Vector4Int(int x0, int z0, int x1, int z1)
        {
            this.x0 = x0;
            this.z0 = z0;
            this.x1 = x1;
            this.z1 = z1;
        }
    }

    [MenuItem("DriveInOffice/Bake Organic City Scene")]
    public static void Bake()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ClearGenerated(scene);
        StripVolumeAndGenerator(scene);

        GameObject root = new GameObject("CityMap");
        Transform roads = Child("Roads", root.transform);
        Transform lots = Child("Lots", root.transform);
        Transform buildings = Child("Buildings", root.transform);
        Transform props = Child("Props", root.transform);
        Transform rails = Child("Constraints", root.transform);

        BuildCity();
        PlaceRoads(roads);
        PlaceGround(lots);
        PlaceLots(buildings, props);
        PlaceStreetFurniture(props);
        PlaceBounds(rails);
        PlaceCar();
        AttachSystems(root, buildings, props, roads);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"Organic city baked: {Roads.Count} roads, {Lots.Count} blocks, {buildings.childCount} buildings.");
    }

    private static void ClearGenerated(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "CityMap" || root.name == "CityShowcase" || root.name == "DrivableCar")
                Object.DestroyImmediate(root);
        }
    }

    private static void StripVolumeAndGenerator(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Global Volume")
                Object.DestroyImmediate(root);
            else if (root.GetComponent<CityShowcaseGenerator>() != null)
                Object.DestroyImmediate(root.GetComponent<CityShowcaseGenerator>());
        }
    }

    private static void BuildCity()
    {
        Roads.Clear();
        Lots.Clear();
        Partition(MapX0, MapZ0, MapX1, MapZ1, 0);
    }

    private static int MaxDepth(int cx, int cz)
    {
        if (cx >= 22 && cx <= 52 && cz >= 14 && cz <= 42)
            return 6;
        if (cx >= 54 && cz <= 24)
            return 5;
        if (cx <= 22 && cz >= 38)
            return 3;
        return 4;
    }

    private static void Partition(int x0, int z0, int x1, int z1, int depth)
    {
        int w = x1 - x0;
        int h = z1 - z0;
        int maxDepth = MaxDepth((x0 + x1) / 2, (z0 + z1) / 2);
        bool compact = w <= 8 && h <= 8;
        if (w < 4 || h < 4 || (depth >= maxDepth && compact) || depth >= maxDepth + 2)
        {
            DrawRect(x0, z0, x1, z1);
            Lots.Add(new Vector4Int(x0, z0, x1, z1));
            return;
        }

        bool splitX = w >= h;
        if (splitX)
        {
            int minCut = x0 + 2;
            int maxCut = x1 - 2;
            int cut = minCut + (x0 * 17 + z0 * 13 + depth * 9) % (maxCut - minCut + 1);
            int mid = (x0 + x1) / 2;
            if (Mathf.Abs(cut - mid) < 2)
                cut = x0 + w / 3;
            Partition(x0, z0, cut, z1, depth + 1);
            Partition(cut, z0, x1, z1, depth + 1);
        }
        else
        {
            int minCut = z0 + 2;
            int maxCut = z1 - 2;
            int cut = minCut + (x0 * 11 + z0 * 19 + depth * 7) % (maxCut - minCut + 1);
            int mid = (z0 + z1) / 2;
            if (Mathf.Abs(cut - mid) < 2)
                cut = z0 + h / 3;
            Partition(x0, z0, x1, cut, depth + 1);
            Partition(x0, cut, x1, z1, depth + 1);
        }
    }

    private static void DrawRect(int x0, int z0, int x1, int z1)
    {
        HLine(z0, x0, x1);
        HLine(z1, x0, x1);
        VLine(x0, z0, z1);
        VLine(x1, z0, z1);
    }

    private static void HLine(int z, int x0, int x1)
    {
        if (x1 < x0)
            (x0, x1) = (x1, x0);
        for (int x = x0; x <= x1; x++)
            Roads.Add(new Vector2Int(x, z));
    }

    private static void VLine(int x, int z0, int z1)
    {
        if (z1 < z0)
            (z0, z1) = (z1, z0);
        for (int z = z0; z <= z1; z++)
            Roads.Add(new Vector2Int(x, z));
    }

    private static float FixYaw(float yaw)
    {
        return Mathf.Repeat(90f - yaw, 360f);
    }

    private static void PlaceRoads(Transform parent)
    {
        GameObject straight = Load("Assets/Prefabs/Map/Roads/Road_Straight.prefab");
        GameObject crossing = Load("Assets/Prefabs/Map/Roads/Road_Crossing.prefab");
        GameObject bend = Load("Assets/Prefabs/Map/Roads/Road_Bend.prefab");
        GameObject tee = Load("Assets/Prefabs/Map/Roads/Road_Intersection.prefab");
        GameObject cross = Load("Assets/Prefabs/Map/Roads/Road_Crossroad.prefab");
        GameObject end = Load("Assets/Prefabs/Map/Roads/Road_End.prefab");
        if (straight == null)
            return;

        foreach (Vector2Int cell in Roads)
        {
            int mask = Mask(cell);
            ResolveTile(mask, cell, straight, crossing, bend, tee, cross, end, out GameObject prefab, out float yaw);
            Vector3 pos = new Vector3(cell.x * Tile, 0f, cell.y * Tile);
            GameObject instance = Place(prefab != null ? prefab : straight, pos, yaw, parent, $"Road_{cell.x}_{cell.y}");
            SetLayer(instance, GroundLayer);
            SetCheap(instance, false);
            SetStatic(instance);
        }
    }

    private static int Mask(Vector2Int cell)
    {
        int mask = 0;
        if (Roads.Contains(cell + Vector2Int.up)) mask |= N;
        if (Roads.Contains(cell + Vector2Int.right)) mask |= E;
        if (Roads.Contains(cell + Vector2Int.down)) mask |= S;
        if (Roads.Contains(cell + Vector2Int.left)) mask |= W;
        return mask;
    }

    private static void ResolveTile(
        int mask, Vector2Int cell,
        GameObject straight, GameObject crossing, GameObject bend, GameObject tee, GameObject cross, GameObject end,
        out GameObject prefab, out float yaw)
    {
        int bits = Bits(mask);
        prefab = straight;
        yaw = 0f;

        if (bits >= 4)
            prefab = cross != null ? cross : straight;
        else if (bits == 3)
        {
            prefab = tee != null ? tee : straight;
            if ((mask & N) == 0) yaw = 0f;
            else if ((mask & W) == 0) yaw = 90f;
            else if ((mask & S) == 0) yaw = 180f;
            else yaw = 270f;
        }
        else if (bits == 1)
        {
            prefab = end != null ? end : straight;
            if (mask == S) yaw = 0f;
            else if (mask == W) yaw = 90f;
            else if (mask == N) yaw = 180f;
            else yaw = 270f;
        }
        else if (mask == (N | S))
        {
            prefab = UseCrossing(cell) && crossing != null ? crossing : straight;
            yaw = 0f;
        }
        else if (mask == (E | W))
        {
            prefab = UseCrossing(cell) && crossing != null ? crossing : straight;
            yaw = 90f;
        }
        else
        {
            prefab = bend != null ? bend : straight;
            if (mask == (E | S)) yaw = 0f;
            else if (mask == (N | E)) yaw = 90f;
            else if (mask == (N | W)) yaw = 180f;
            else yaw = 270f;
        }

        yaw = FixYaw(yaw);
    }

    private static bool UseCrossing(Vector2Int cell) => (cell.x + cell.y * 3) % 13 == 0;

    private static int Bits(int mask)
    {
        int count = 0;
        if ((mask & N) != 0) count++;
        if ((mask & E) != 0) count++;
        if ((mask & S) != 0) count++;
        if ((mask & W) != 0) count++;
        return count;
    }

    private static void PlaceGround(Transform parent)
    {
        float width = (MapX1 - MapX0 + 4) * Tile;
        float depth = (MapZ1 - MapZ0 + 4) * Tile;
        Vector3 center = new Vector3((MapX0 + MapX1) * 0.5f * Tile, -0.12f, (MapZ0 + MapZ1) * 0.5f * Tile);
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "CityGround";
        ground.transform.SetParent(parent, false);
        ground.transform.position = center;
        ground.transform.localScale = new Vector3(width, 0.2f, depth);
        ground.layer = GroundLayer;
        MeshRenderer renderer = ground.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = new Material(renderer.sharedMaterial)
            {
                color = new Color(0.48f, 0.5f, 0.44f)
            };
        }

        SetCheap(ground, false);
        SetStatic(ground);
    }

    private static void PlaceLots(Transform buildings, Transform props)
    {
        GameObject[] buildingPrefabs =
        {
            Load("Assets/ithappy/Cartoon_City_Free/Prefabs/Buildings/Eco_Building_Grid.prefab"),
            Load("Assets/ithappy/Cartoon_City_Free/Prefabs/Buildings/Eco_Building_Terrace.prefab"),
            Load("Assets/ithappy/Cartoon_City_Free/Prefabs/Buildings/Eco_Building_Slope.prefab"),
            Load("Assets/ithappy/Cartoon_City_Free/Prefabs/Buildings/Regular_Building_TwistedTower_Large.prefab")
        };
        GameObject[] propPrefabs =
        {
            Load("Assets/Prefabs/Map/Props/Prop_Dumpster.prefab"),
            Load("Assets/Prefabs/Map/Props/Prop_Traffic_Light.prefab"),
            Load("Assets/Prefabs/Map/Roads/Road_Sign_Stop.prefab"),
            Load("Assets/Prefabs/Map/Props/Prop_Cone.prefab"),
            Load("Assets/Prefabs/Map/Props/Prop_Box.prefab"),
            Load("Assets/Prefabs/Map/Props/Prop_Construction_Barrier.prefab"),
            Load("Assets/Prefabs/Map/Props/Prop_Construction_Fence.prefab"),
            Load("Assets/Prefabs/Map/Props/Prop_Electricity_Pole.prefab"),
            Load("Assets/Prefabs/Map/Props/Prop_Light_Curved.prefab"),
            Load("Assets/Prefabs/Map/Props/Prop_Sign_Highway.prefab"),
            Load("Assets/ithappy/Cartoon_City_Free/Prefabs/Props/Trash_Can_04.prefab"),
            Load("Assets/ithappy/Cartoon_City_Free/Prefabs/Props/Trash_02.prefab"),
            Load("Assets/ithappy/Cartoon_City_Free/Prefabs/Props/Bus_Stop_02.prefab"),
            Load("Assets/ithappy/Cartoon_City_Free/Prefabs/Props/traffic_light_001.prefab"),
            Load("Assets/ithappy/Cartoon_City_Free/Prefabs/Props/Spotlight_01.prefab"),
            Load("Assets/ithappy/Cartoon_City_Free/Prefabs/Props/Fountain_03.prefab")
        };
        GameObject[] cars =
        {
            Load("Assets/ithappy/Cartoon_City_Free/Prefabs/Cars/Car_06.prefab"),
            Load("Assets/ithappy/Cartoon_City_Free/Prefabs/Cars/Car_13.prefab"),
            Load("Assets/ithappy/Cartoon_City_Free/Prefabs/Cars/Car_16.prefab"),
            Load("Assets/ithappy/Cartoon_City_Free/Prefabs/Cars/Car_19.prefab"),
            Load("Assets/ithappy/Cartoon_City_Free/Prefabs/Cars/Van.prefab")
        };

        int index = 0;
        foreach (Vector4Int lot in Lots)
        {
            int innerW = lot.x1 - lot.x0 - 1;
            int innerH = lot.z1 - lot.z0 - 1;
            if (innerW < 1 || innerH < 1)
                continue;

            int step = Mathf.Min(innerW, innerH) <= 7 ? 1 : 2;
            float span = step == 1 ? 8.6f : 12.5f;

            for (int x = lot.x0 + 1; x < lot.x1; x += step)
            {
                for (int z = lot.z0 + 1; z < lot.z1; z += step)
                {
                    if (Roads.Contains(new Vector2Int(x, z)))
                        continue;

                    Vector3 pos = new Vector3(x * Tile, 0f, z * Tile);
                    GameObject prefab = First(buildingPrefabs, index);
                    GameObject building = Fit(prefab, pos, 90f * (index % 4), buildings, span);
                    if (building == null)
                    {
                        float height = 14f + index % 5 * 4f;
                        building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        building.name = $"Building_{index}";
                        building.transform.SetParent(buildings, false);
                        building.transform.position = pos + Vector3.up * (height * 0.5f);
                        building.transform.localScale = new Vector3(span, height, span);
                    }

                    SetLayer(building, ObstacleLayer);
                    EnsureMeshColliders(building);
                    SetCheap(building, true);
                    SetStatic(building);
                    index++;
                }
            }

            int key = lot.x0 + lot.z0 * 3;
            PlaceFittedProp(First(propPrefabs, key), Corner(lot, 0), 0f, props, 2.2f);
            PlaceFittedProp(First(propPrefabs, key + 1), Corner(lot, 1), 90f, props, 2.4f);
            PlaceFittedProp(First(propPrefabs, key + 2), Corner(lot, 2), 45f, props, 2.0f);
            PlaceFittedProp(First(propPrefabs, key + 3), Corner(lot, 3), 180f, props, 2.2f);

            GameObject car = Fit(First(cars, key), Corner(lot, key % 4), 90f * (key % 4), props, 4.2f);
            if (car != null)
            {
                SetLayer(car, ObstacleLayer);
                foreach (Rigidbody rb in car.GetComponentsInChildren<Rigidbody>(true))
                    Object.DestroyImmediate(rb);
                SetCheap(car, false);
                SetStatic(car);
            }

            if (innerW >= 6 && innerH >= 6)
                PlaceFittedProp(Load("Assets/ithappy/Cartoon_City_Free/Prefabs/Props/Fountain_03.prefab"), LotCenter(lot), 0f, props, 6f);
        }
    }

    private static Vector3 LotCenter(Vector4Int lot)
    {
        return new Vector3((lot.x0 + lot.x1) * 0.5f * Tile, 0f, (lot.z0 + lot.z1) * 0.5f * Tile);
    }

    private static void PlaceStreetFurniture(Transform props)
    {
        GameObject light = Load("Assets/Prefabs/Map/Props/Prop_Light_Curved.prefab");
        GameObject pole = Load("Assets/Prefabs/Map/Props/Prop_Electricity_Pole.prefab");
        GameObject bus = Load("Assets/ithappy/Cartoon_City_Free/Prefabs/Props/Bus_Stop_02.prefab");
        GameObject sign = Load("Assets/Prefabs/Map/Roads/Road_Sign_Stop.prefab");
        foreach (Vector2Int cell in Roads)
        {
            int mask = Mask(cell);
            if (!IsStraight(mask))
            {
                if (Bits(mask) >= 3 && (cell.x + cell.y) % 2 == 0)
                    PlaceFittedProp(sign, Offset(cell, mask, 0.55f), 0f, props, 2.4f);
                continue;
            }

            if ((cell.x + cell.y) % 6 == 0)
                PlaceFittedProp(light, Offset(cell, mask, 0.58f), mask == (N | S) ? 90f : 0f, props, 2.8f);
            if ((cell.x + cell.y) % 8 == 0)
                PlaceFittedProp(pole, Offset(cell, mask, -0.58f), 0f, props, 2.6f);
            if (mask == (E | W) && cell.x % 10 == 0)
                PlaceFittedProp(bus, Offset(cell, mask, 0.62f), 90f, props, 3.4f);
        }
    }

    private static bool IsStraight(int mask)
    {
        return mask == (N | S) || mask == (E | W);
    }

    private static Vector3 Offset(Vector2Int cell, int mask, float side)
    {
        Vector2Int perp = mask == (N | S) ? Vector2Int.right : Vector2Int.up;
        return new Vector3((cell.x + perp.x * side) * Tile, 0f, (cell.y + perp.y * side) * Tile);
    }

    private static Vector3 Corner(Vector4Int lot, int corner)
    {
        float inset = 1.35f;
        float x = corner == 0 || corner == 3 ? lot.x0 + inset : lot.x1 - inset;
        float z = corner <= 1 ? lot.z0 + inset : lot.z1 - inset;
        return new Vector3(x * Tile, 0f, z * Tile);
    }

    private static void PlaceFittedProp(GameObject prefab, Vector3 pos, float yaw, Transform parent, float span)
    {
        GameObject instance = Fit(prefab, pos, yaw, parent, span);
        if (instance == null)
            return;
        SetLayer(instance, ObstacleLayer);
        SetCheap(instance, false);
        SetStatic(instance);
    }

    private static void PlaceBounds(Transform parent)
    {
        float minX = (MapX0 - 1.5f) * Tile;
        float maxX = (MapX1 + 1.5f) * Tile;
        float minZ = (MapZ0 - 1.5f) * Tile;
        float maxZ = (MapZ1 + 1.5f) * Tile;
        float midX = (minX + maxX) * 0.5f;
        float midZ = (minZ + maxZ) * 0.5f;
        MakeWall(parent, new Vector3(midX, 4f, minZ), new Vector3(maxX - minX + 8f, 8f, 4f), "Edge_S");
        MakeWall(parent, new Vector3(midX, 4f, maxZ), new Vector3(maxX - minX + 8f, 8f, 4f), "Edge_N");
        MakeWall(parent, new Vector3(minX, 4f, midZ), new Vector3(4f, 8f, maxZ - minZ), "Edge_W");
        MakeWall(parent, new Vector3(maxX, 4f, midZ), new Vector3(4f, 8f, maxZ - minZ), "Edge_E");
    }

    private static void PlaceCar()
    {
        GameObject prefab = Load("Assets/Prefabs/Cars/DrivableCar.prefab");
        if (prefab == null)
            return;
        GameObject car = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        car.name = "DrivableCar";
        car.transform.SetPositionAndRotation(new Vector3(120f, 0.4f, 20f), Quaternion.Euler(0f, 90f, 0f));
        Camera cam = car.GetComponentInChildren<Camera>(true);
        if (cam != null)
            cam.farClipPlane = 270f;
    }

    private static void AttachSystems(GameObject root, Transform buildings, Transform props, Transform roads)
    {
        if (root.GetComponent<MapRuntimeQuality>() == null)
            root.AddComponent<MapRuntimeQuality>();
        CityDistanceCuller culler = root.GetComponent<CityDistanceCuller>();
        if (culler == null)
            culler = root.AddComponent<CityDistanceCuller>();
        culler.buildingsRoot = buildings;
        culler.propsRoot = props;
        culler.roadsRoot = roads;
    }

    private static void MakeWall(Transform parent, Vector3 pos, Vector3 size, string name)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent, false);
        wall.transform.position = pos;
        wall.transform.localScale = size;
        wall.layer = ObstacleLayer;
        Object.DestroyImmediate(wall.GetComponent<MeshRenderer>());
        Object.DestroyImmediate(wall.GetComponent<MeshFilter>());
        SetStatic(wall);
    }

    private static GameObject Fit(GameObject prefab, Vector3 pos, float yaw, Transform parent, float span)
    {
        if (prefab == null)
            return null;
        GameObject instance = Place(prefab, pos, yaw, parent, prefab.name);
        Bounds bounds = Encapsulate(instance);
        float size = Mathf.Max(bounds.size.x, bounds.size.z);
        if (size > 0.01f)
        {
            instance.transform.localScale *= Mathf.Clamp(span / size, 0.08f, 1.1f);
            bounds = Encapsulate(instance);
        }

        instance.transform.position += pos - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        return instance;
    }

    private static GameObject Place(GameObject prefab, Vector3 pos, float yaw, Transform parent, string name)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        return instance;
    }

    private static Transform Child(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static GameObject First(GameObject[] prefabs, int index)
    {
        List<GameObject> valid = new List<GameObject>();
        foreach (GameObject prefab in prefabs)
        {
            if (prefab != null)
                valid.Add(prefab);
        }

        return valid.Count == 0 ? null : valid[Mathf.Abs(index) % valid.Count];
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
        foreach (MeshFilter filter in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || filter.GetComponent<Collider>() != null)
                continue;
            MeshCollider col = filter.gameObject.AddComponent<MeshCollider>();
            col.sharedMesh = filter.sharedMesh;
        }
    }

    private static void SetCheap(GameObject go, bool castShadows)
    {
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

    private static void SetLayer(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayer(child.gameObject, layer);
    }

    private static void SetStatic(GameObject go)
    {
        GameObjectUtility.SetStaticEditorFlags(
            go,
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
        foreach (Transform child in go.transform)
            SetStatic(child.gameObject);
    }

    private static GameObject Load(string path)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }
}
