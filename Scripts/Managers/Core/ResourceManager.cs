using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager
{
    public T Load<T>(string path) where T : Object
    {
        // 🔹 경로 확인 로그
        //Debug.Log($"[Resource Load] Type: {typeof(T).Name}, Path: {path}");

        if (typeof(T) == typeof(GameObject))
        {
            string name = path;
            int index = name.LastIndexOf('/');
            if (index >= 0)
                name = name.Substring(index + 1);

            GameObject go = Managers.Pool.GetOriginal(name);
            if (go != null)
                return go as T;
        }

        T resource = Resources.Load<T>(path);

        // 🔹 성공/실패 여부 로그
        //if (resource == null)
        //    Debug.LogError($"[Resource Load ❌] Failed to load: {path}");
        //else
        //    Debug.Log($"[Resource Load ✅] Successfully loaded: {path}");

        return resource;
    }

    public GameObject Instantiate(GameObject go, Transform parent = null)
    {
        if(go == null)
        {
            Debug.Log($"{go.name} Is Null");
        }

        if (go.GetComponent<Poolable>() != null)
            return Managers.Pool.Pop(go, parent).gameObject;

        GameObject gos = Object.Instantiate(go, parent);
        gos.name = go.name;
        return gos;
    }

    public GameObject Instantiate(GameObject go, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        GameObject obj = Instantiate(go, parent);
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        return obj;
    }

    public GameObject Instantiate(GameObject go, Quaternion rotation, Transform parent = null)
    {
        GameObject obj = Instantiate(go, parent);
        obj.transform.rotation = rotation;
        return obj;
    }

    public GameObject Instantiate(GameObject go, Vector3 position, Transform parent = null)
    {
        GameObject obj = Instantiate(go, parent);
        obj.transform.position = position;
        return obj;
    }

    public T Instantiate<T>(GameObject go, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
    {
        go.transform.position = position;
        go.transform.rotation = rotation;

        return Instantiate<T>(go, parent);
    }

    public T Instantiate<T>(GameObject go, Vector3 position, Transform parent = null) where T : Component
    {
        go.transform.position = position;

        return Instantiate<T>(go, parent);

    }

    public T Instantiate<T>(GameObject go, Quaternion rotation, Transform parent = null) where T : Component
    {
        go.transform .rotation = rotation;

        return Instantiate<T>(go, parent);
    }

    public T Instantiate<T>(GameObject go, Transform parent = null) where T : Component
    {
        if (go == null)
        {
            Debug.LogWarning("Instantiate Failed: input GameObject is null");
            return null;
        }

        GameObject instance;

        if (go.GetComponent<Poolable>() != null)
            instance = Managers.Pool.Pop(go, parent).gameObject;
        else
            instance = Object.Instantiate(go, parent);

        instance.name = go.name;
        return instance.GetComponent<T>();
    }


    public GameObject Instantiate(string path, Transform parent = null)
    {
        if (path.Contains("Prefabs/") == false)
            path = $"Prefabs/{path}";

        GameObject original = Load<GameObject>($"{path}");
        if (original == null)
        {
            Debug.Log($"Failed to load prefab : {path}");
            return null;
        }

        if (original.GetComponent<Poolable>() != null)
            return Managers.Pool.Pop(original, parent).gameObject;

        GameObject go = Object.Instantiate(original, parent);
        go.name = original.name;
        return go;
    }

    public void Destroy(GameObject go)
    {
        if (go == null)
            return;

        Poolable poolable = go.GetComponent<Poolable>();
        if (poolable != null)
        {
            Managers.Pool.Push(poolable);
            return;
        }

        Object.Destroy(go);
    }
}
