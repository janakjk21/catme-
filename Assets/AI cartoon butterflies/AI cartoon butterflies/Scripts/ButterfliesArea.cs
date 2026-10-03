using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButterfliesArea : MonoBehaviour {
    public GameObject[] prefabs;
    public int count;
    public int Count {
        get {
            return count;
        }
        set {
            if (count != value)
            {
                count = value;
                SpawnButterflies();
                transform.hasChanged = false;
            }
        }
    }

    Butterfly[] butterflies;
    BoxCollider collider;
    Vector3 center;
    Vector3 max;
    Vector3 min;
    public float maxHeight;
    public float speed = 10f;

    void Start () {
        butterflies = transform.GetComponentsInChildren<Butterfly>();
        collider = GetComponent<BoxCollider>();
        InitializeAllButterflies();
    }

    public void InitializeAllButterflies()
    {
        maxHeight = collider.size.y / 2f;
        if (collider == null)
            collider = GetComponent<BoxCollider>();
        for (int i = 0; i < butterflies.Length; i++)
            butterflies[i].Initialize(this);
    }

    public void RemoveButterflies()
    {
        butterflies = transform.GetComponentsInChildren<Butterfly>();
        for (int i = 0; i < butterflies.Length; i++)
#if UNITY_EDITOR
            DestroyImmediate(butterflies[i].gameObject);
#else
                Destroy(butterflies[i].gameObject);
#endif
    }

    public void SpawnButterflies()
    {
        butterflies = transform.GetComponentsInChildren<Butterfly>();
        if (collider == null)
            collider = GetComponent<BoxCollider>();
        center = collider.center;
        max = center + collider.size / 2f;
        min = center - collider.size/2f;
        if (butterflies.Length < count)
            for (int i = butterflies.Length; i < count; i++)
                SpawnButterFly();
        else
            if (butterflies.Length > count)
            for (int i = 0; i < butterflies.Length - count; i++)
#if UNITY_EDITOR
                DestroyImmediate(butterflies[i].gameObject);
#else
                Destroy(butterflies[i].gameObject);
#endif
        butterflies = transform.GetComponentsInChildren<Butterfly>();
        MixPositions();
    }

    public void Update()
    {
        UpdateButterflies();
    }

    private void UpdateButterflies()
    {
        for (int i = 0; i < butterflies.Length; i++)
        {
            butterflies[i].Move();
        }
    }

    public void SpawnButterFly() {
        GameObject temp = Instantiate(GetRandomPrefab(), transform);        
    }

    public void MixPositions()
    {
        for (int i = 0; i < butterflies.Length; i++)
        {
            Transform temp = butterflies[i].transform;
            Vector3 normal;
            temp.position = GetFreeRandomPoint(out normal);
            temp.up = normal;
            temp.Rotate(0f, UnityEngine.Random.Range(0f, 360f), 0f, Space.Self);
        }
        
    }

    protected GameObject GetRandomPrefab()
    {
        int id = UnityEngine.Random.Range(0, prefabs.Length);
        return prefabs[id];
    }

    protected Vector3 GetFreeRandomPoint(out Vector3 normal) {
        float x = UnityEngine.Random.Range(min.x, max.x);
        float y = max.y;
        float z = UnityEngine.Random.Range(min.z, max.z);
        Vector3 p = new Vector3(x,y,z);
        RaycastHit hitInfo;
        Physics.Raycast(new Ray(transform.TransformPoint(p), Vector3.down), out hitInfo, collider.size.y);
        normal = hitInfo.normal;
        return hitInfo.point;
    }


}
