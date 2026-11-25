using UnityEngine;
using System.Collections.Generic;

public class ColliderVisualizer : MonoBehaviour
{
    [Header("Debug")]
    public bool showColliders = true;

    [Header("Tags to Visualize")]
    [Tooltip("Objects with these tags will have their colliders drawn")]
    public List<string> tagsToVisualize = new List<string>();

    [Header("Colors")]
    public Color otherColor = Color.green;
    public Color capsuleColor = Color.yellow;

    [Header("Line Settings")]
    public float lineWidth = 0.02f;

    [Header("Refresh")]
    [Tooltip("How often (in seconds) to refresh the list of objects")]
    public float refreshInterval = 2f;

    private float refreshTimer = 0f;
    private List<GameObject> trackedObjects = new List<GameObject>();

    private Material otherMaterial;
    private Material capsuleMaterial;
    private MaterialPropertyBlock mpb;

    private Dictionary<GameObject, Collider[]> colliderCache = new Dictionary<GameObject, Collider[]>();
    private Queue<LineRenderer> linePool = new Queue<LineRenderer>();

    private class ColliderVisual
    {
        public Collider collider;
        public LineRenderer line;
    }

    private List<ColliderVisual> activeVisuals = new List<ColliderVisual>();

    void Start()
    {
        Shader shader = Shader.Find("Sprites/Default");
        otherMaterial = new Material(shader);
        capsuleMaterial = new Material(shader);
        mpb = new MaterialPropertyBlock();
    }

    void LateUpdate()
    {
        if (!showColliders)
        {
            if (activeVisuals.Count > 0) ClearAllVisuals();
            return;
        }

        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = refreshInterval;
            RefreshTrackedObjects();
        }

        UpdateAllVisuals();
    }

    void RefreshTrackedObjects()
    {
        trackedObjects.Clear();
        colliderCache.Clear();

        foreach (string tag in tagsToVisualize)
        {
            if (string.IsNullOrEmpty(tag)) continue;

            GameObject[] objectsWithTag;
            try
            {
                objectsWithTag = GameObject.FindGameObjectsWithTag(tag);
            }
            catch
            {
                continue;
            }

            foreach (GameObject obj in objectsWithTag)
            {
                if (obj != null && !trackedObjects.Contains(obj))
                {
                    trackedObjects.Add(obj);
                }
            }
        }

        RebuildVisuals();
    }

    void RebuildVisuals()
    {
        // Возвращаем все линии в пул
        for (int i = 0; i < activeVisuals.Count; i++)
        {
            if (activeVisuals[i].line != null)
            {
                ReturnLineToPool(activeVisuals[i].line);
            }
        }
        activeVisuals.Clear();

        foreach (GameObject obj in trackedObjects)
        {
            if (obj == null) continue;

            Collider[] colliders = GetCachedColliders(obj);

            foreach (Collider col in colliders)
            {
                if (col == null) continue;
                CreateVisualFor(col);
            }
        }
    }

    Collider[] GetCachedColliders(GameObject obj)
    {
        if (!colliderCache.ContainsKey(obj))
        {
            colliderCache[obj] = obj.GetComponentsInChildren<Collider>();
        }
        return colliderCache[obj];
    }

    void CreateVisualFor(Collider col)
    {
        bool isCapsule = col is CapsuleCollider;

        LineRenderer lr = GetLineFromPool();
        lr.material = isCapsule ? capsuleMaterial : otherMaterial;

        // Цвет через MaterialPropertyBlock
        Color color = isCapsule ? capsuleColor : otherColor;
        mpb.Clear();
        mpb.SetColor("_Color", color);
        lr.SetPropertyBlock(mpb);

        ColliderVisual visual = new ColliderVisual
        {
            collider = col,
            line = lr
        };

        activeVisuals.Add(visual);
        DrawColliderOutline(visual);
    }

    LineRenderer GetLineFromPool()
    {
        LineRenderer lr;

        if (linePool.Count > 0)
        {
            lr = linePool.Dequeue();
            lr.gameObject.SetActive(true);
        }
        else
        {
            GameObject lineObj = new GameObject("ColliderVisual");
            lineObj.transform.SetParent(transform);
            lr = lineObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
        }

        lr.positionCount = 0;
        return lr;
    }

    void ReturnLineToPool(LineRenderer lr)
    {
        if (lr == null) return;
        lr.positionCount = 0;
        lr.gameObject.SetActive(false);
        linePool.Enqueue(lr);
    }

    void DrawColliderOutline(ColliderVisual visual)
    {
        Collider col = visual.collider;
        if (col == null) return;

        if (col is BoxCollider box)
        {
            DrawBox(box, visual.line);
        }
        else if (col is SphereCollider sphere)
        {
            DrawSphere(sphere, visual.line);
        }
        else if (col is CapsuleCollider capsule)
        {
            DrawCapsule(capsule, visual.line);
        }
        else if (col is MeshCollider mesh)
        {
            DrawBounds(mesh, visual.line);
        }
    }

    void DrawBox(BoxCollider box, LineRenderer lr)
    {
        Vector3 half = box.size / 2f;

        Vector3[] localCorners = new Vector3[8];
        localCorners[0] = new Vector3(-half.x, -half.y, -half.z);
        localCorners[1] = new Vector3(half.x, -half.y, -half.z);
        localCorners[2] = new Vector3(half.x, -half.y, half.z);
        localCorners[3] = new Vector3(-half.x, -half.y, half.z);
        localCorners[4] = new Vector3(-half.x, half.y, -half.z);
        localCorners[5] = new Vector3(half.x, half.y, -half.z);
        localCorners[6] = new Vector3(half.x, half.y, half.z);
        localCorners[7] = new Vector3(-half.x, half.y, half.z);

        Vector3[] corners = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            Vector3 localPos = box.center + localCorners[i];
            corners[i] = box.transform.TransformPoint(localPos);
        }

        int[] edges = new int[]
        {
            0,1, 1,2, 2,3, 3,0,
            4,5, 5,6, 6,7, 7,4,
            0,4, 1,5, 2,6, 3,7
        };

        lr.positionCount = edges.Length;
        for (int i = 0; i < edges.Length; i++)
        {
            lr.SetPosition(i, corners[edges[i]]);
        }
    }

    void DrawSphere(SphereCollider sphere, LineRenderer lr)
    {
        Vector3 center = sphere.transform.TransformPoint(sphere.center);
        float radius = sphere.radius * Mathf.Max(
            sphere.transform.lossyScale.x,
            Mathf.Max(sphere.transform.lossyScale.y, sphere.transform.lossyScale.z)
        );

        int segments = 32;
        lr.positionCount = segments + 1;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            lr.SetPosition(i, pos);
        }
    }

    void DrawCapsule(CapsuleCollider capsule, LineRenderer lr)
    {
        float radius = capsule.radius;
        float height = capsule.height;

        Vector3 half = new Vector3(radius, height / 2f, radius);

        Vector3[] localCorners = new Vector3[8];
        localCorners[0] = new Vector3(-half.x, -half.y, -half.z);
        localCorners[1] = new Vector3(half.x, -half.y, -half.z);
        localCorners[2] = new Vector3(half.x, -half.y, half.z);
        localCorners[3] = new Vector3(-half.x, -half.y, half.z);
        localCorners[4] = new Vector3(-half.x, half.y, -half.z);
        localCorners[5] = new Vector3(half.x, half.y, -half.z);
        localCorners[6] = new Vector3(half.x, half.y, half.z);
        localCorners[7] = new Vector3(-half.x, half.y, half.z);

        Vector3[] corners = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            Vector3 localPos = capsule.center + localCorners[i];
            corners[i] = capsule.transform.TransformPoint(localPos);
        }

        int[] edges = new int[]
        {
            0,1, 1,2, 2,3, 3,0,
            4,5, 5,6, 6,7, 7,4,
            0,4, 1,5, 2,6, 3,7
        };

        lr.positionCount = edges.Length;
        for (int i = 0; i < edges.Length; i++)
        {
            lr.SetPosition(i, corners[edges[i]]);
        }
    }

    void DrawBounds(Collider col, LineRenderer lr)
    {
        Bounds bounds = col.bounds;
        Vector3 center = bounds.center;
        Vector3 half = bounds.extents;

        Vector3[] corners = new Vector3[8];
        corners[0] = center + new Vector3(-half.x, -half.y, -half.z);
        corners[1] = center + new Vector3(half.x, -half.y, -half.z);
        corners[2] = center + new Vector3(half.x, -half.y, half.z);
        corners[3] = center + new Vector3(-half.x, -half.y, half.z);
        corners[4] = center + new Vector3(-half.x, half.y, -half.z);
        corners[5] = center + new Vector3(half.x, half.y, -half.z);
        corners[6] = center + new Vector3(half.x, half.y, half.z);
        corners[7] = center + new Vector3(-half.x, half.y, half.z);

        int[] edges = new int[]
        {
            0,1, 1,2, 2,3, 3,0,
            4,5, 5,6, 6,7, 7,4,
            0,4, 1,5, 2,6, 3,7
        };

        lr.positionCount = edges.Length;
        for (int i = 0; i < edges.Length; i++)
        {
            lr.SetPosition(i, corners[edges[i]]);
        }
    }

    void UpdateAllVisuals()
    {
        for (int i = activeVisuals.Count - 1; i >= 0; i--)
        {
            ColliderVisual visual = activeVisuals[i];

            if (visual.collider == null)
            {
                if (visual.line != null) ReturnLineToPool(visual.line);
                activeVisuals.RemoveAt(i);
                continue;
            }

            DrawColliderOutline(visual);
        }
    }

    void ClearAllVisuals()
    {
        for (int i = 0; i < activeVisuals.Count; i++)
        {
            if (activeVisuals[i].line != null)
            {
                ReturnLineToPool(activeVisuals[i].line);
            }
        }
        activeVisuals.Clear();
    }

    void OnDisable()
    {
        ClearAllVisuals();
    }
}
