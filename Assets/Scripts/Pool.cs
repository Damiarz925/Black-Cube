// Developer map: Legacy scenery reuse by string key. ZoneManager sets placement and activation after Get; Release deactivates and returns the object to its key stack.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;
using System.Collections.Generic;

public class Pool : MonoBehaviour
{
    private Dictionary<string, Stack<GameObject>> pool = new();

    public GameObject Get(string key, GameObject prefab)
    {
        GameObject obj;

        if (pool.TryGetValue(key, out var stack) && stack.Count > 0)
            obj = stack.Pop();
        else
            obj = Instantiate(prefab);

        var po = obj.GetComponent<PooledObject>();
        if (po == null) po = obj.AddComponent<PooledObject>();
        po.Key = key;

        return obj;
    }

    public void Release(GameObject obj)
    {
        obj.SetActive(false);

        var po = obj.GetComponent<PooledObject>();
        var key = (po != null) ? po.Key : obj.name;

        if (!pool.TryGetValue(key, out var stack))
            pool[key] = stack = new Stack<GameObject>();

        stack.Push(obj);
    }
}
