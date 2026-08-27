using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Places pooled Kenny barriers/cones along nearby straight road pieces without editing the map mesh.
/// </summary>
public class RoadsideDecorSpawner : MonoBehaviour
{
    public Transform mapRoot;
    public GameObject barrierPrefab;
    public GameObject conePrefab;
    public float searchRadius = 220f;
    public float sideOffset = 6.2f;
    public float spacing = 14f;
    public int maxBarriers = 48;
    public int maxCones = 24;
    public string[] roadNameHints = { "Freeway Straight", "Road Straight", "Highway" };

    private Transform follow;
    private Transform decorRoot;
    private SimpleObjectPool barrierPool;
    private SimpleObjectPool conePool;
    private readonly List<(GameObject go, SimpleObjectPool pool)> live = new List<(GameObject, SimpleObjectPool)>(64);
    private bool spawned;

    private void Start()
    {
        if (mapRoot == null)
        {
            GameObject map = GameObject.Find("Map");
            if (map != null)
                mapRoot = map.transform;
        }

        KenneyCarController car = FindAnyObjectByType<KenneyCarController>();
        if (car != null)
            follow = car.transform;
        else if (Camera.main != null)
            follow = Camera.main.transform;

        decorRoot = new GameObject("RoadsideDecor").transform;
        decorRoot.SetParent(transform, false);

        if (barrierPrefab != null)
            barrierPool = new SimpleObjectPool(barrierPrefab, decorRoot, 16);
        if (conePrefab != null)
            conePool = new SimpleObjectPool(conePrefab, decorRoot, 8);

        SpawnOnce();
    }

    private void SpawnOnce()
    {
        if (spawned || mapRoot == null || follow == null)
            return;
        spawned = true;

        Vector3 origin = follow.position;
        float radiusSqr = searchRadius * searchRadius;
        int barriers = 0;
        int cones = 0;

        for (int i = 0; i < mapRoot.childCount; i++)
        {
            Transform road = mapRoot.GetChild(i);
            if (road == null || !NameMatches(road.name))
                continue;
            if ((road.position - origin).sqrMagnitude > radiusSqr)
                continue;

            Vector3 forward = road.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            if (barrierPool != null && barriers < maxBarriers)
            {
                Place(barrierPool, road.position + right * sideOffset, Quaternion.LookRotation(forward), ref barriers, maxBarriers);
                Place(barrierPool, road.position - right * sideOffset, Quaternion.LookRotation(-forward), ref barriers, maxBarriers);
            }

            if (conePool != null && cones < maxCones && (i % 4 == 0))
            {
                Place(conePool, road.position + right * (sideOffset * 0.5f) + forward * 3f,
                    Quaternion.identity, ref cones, maxCones);
            }

            if (barriers >= maxBarriers && cones >= maxCones)
                break;
        }
    }

    private void Place(SimpleObjectPool pool, Vector3 pos, Quaternion rot, ref int count, int max)
    {
        if (count >= max || pool == null)
            return;

        pos.y = follow != null ? follow.position.y + 8f : pos.y + 8f;
        if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, 40f))
            pos = hit.point;
        else if (follow != null)
            pos.y = follow.position.y;

        live.Add((pool.Get(pos, rot), pool));
        count++;
    }

    private bool NameMatches(string name)
    {
        if (string.IsNullOrEmpty(name) || roadNameHints == null)
            return false;
        for (int i = 0; i < roadNameHints.Length; i++)
        {
            if (!string.IsNullOrEmpty(roadNameHints[i])
                && name.IndexOf(roadNameHints[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    private void OnDestroy()
    {
        for (int i = 0; i < live.Count; i++)
            live[i].pool?.Release(live[i].go);
        live.Clear();
    }
}
