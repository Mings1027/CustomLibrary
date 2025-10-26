using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    private static ObjectPoolManager _instance;

    public static ObjectPoolManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var obj = new GameObject("ObjectPoolManager");
                _instance = obj.AddComponent<ObjectPoolManager>();
            }

            return _instance;
        }
    }

    private Dictionary<string, Stack<GameObject>> _pool = new Dictionary<string, Stack<GameObject>>();
    public List<Transform> monsterCloseList;

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        string key = prefab.name;

        if (_pool.TryGetValue(key, out var stack) && stack.Count > 0)
        {
            GameObject obj = stack.Pop();
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        GameObject newObj = Instantiate(prefab, position, rotation);
        newObj.name = key; // 이름 유지
        return newObj;
    }

    public T Get<T>(GameObject prefab, Vector3 position, Quaternion rotation) where T : Component
    {
        string key = prefab.name;
        GameObject obj;

        if (_pool.TryGetValue(key, out var stack) && stack.Count > 0)
        {
            obj = stack.Pop();
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab, position, rotation);
            obj.name = key;
        }

        if (obj.TryGetComponent(out T component)) return component;
        component = obj.AddComponent<T>();

        return component;
    }


    public void Return(GameObject obj)
    {
        string key = obj.name;

        if (!_pool.TryGetValue(key, out var stack))
        {
            stack = new Stack<GameObject>();
            _pool[key] = stack;
        }

        obj.SetActive(false);
        stack.Push(obj);
    }

    private void OnDestroy()
    {
        _pool.Clear();
    }
}