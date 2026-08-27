using System.Collections.Generic;
using UnityEngine;

public class SimpleObjectPool
{
    private readonly GameObject prefab;
    private readonly Transform parent;
    private readonly Stack<GameObject> free = new Stack<GameObject>(32);

    public SimpleObjectPool(GameObject prefab, Transform parent, int prewarm = 0)
    {
        this.prefab = prefab;
        this.parent = parent;
        for (int i = 0; i < prewarm; i++)
        {
            GameObject go = Object.Instantiate(prefab, parent);
            go.SetActive(false);
            free.Push(go);
        }
    }

    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject go = free.Count > 0 ? free.Pop() : Object.Instantiate(prefab, parent);
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        return go;
    }

    public void Release(GameObject go)
    {
        if (go == null)
            return;
        go.SetActive(false);
        go.transform.SetParent(parent, false);
        free.Push(go);
    }
}
