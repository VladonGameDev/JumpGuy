using UnityEngine;
using System.Collections.Generic;

public class PlayerTrailDebug : MonoBehaviour
{
    [Header("Trail Target")]
    public CapsuleCollider playerCollider;

    [Header("Debug")]
    public bool showTopTrail = true;
    public bool showBottomTrail = true;
    public float trailDuration = 2f;
    public float pointsPerSecond = 30f;
    public Color topTrailColor = Color.red;
    public Color bottomTrailColor = Color.blue;
    public float trailWidth = 0.05f;

    [Header("World Speed")]
    public float obstacleSpeed = 5f;
    public RunProgressionManager progressionManager;

    private LineRenderer topLine;
    private LineRenderer bottomLine;
    private List<TrailPoint> topPoints = new List<TrailPoint>();
    private List<TrailPoint> bottomPoints = new List<TrailPoint>();
    private float pointTimer = 0f;

    private class TrailPoint
    {
        public Vector3 position;
        public float timestamp;
    }

    void Start()
    {
        if (playerCollider == null) playerCollider = GetComponent<CapsuleCollider>();
        topLine = CreateLineRenderer("TopTrail", topTrailColor);
        bottomLine = CreateLineRenderer("BottomTrail", bottomTrailColor);
    }

    LineRenderer CreateLineRenderer(string name, Color color)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(transform);
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = trailWidth;
        lr.endWidth = trailWidth;
        lr.positionCount = 0;
        lr.useWorldSpace = true;
        return lr;
    }

    void Update()
    {
        if (PlayerPrefs.GetInt("isGameStarted") != 1) return;

        float worldSpeed = obstacleSpeed;
        if (progressionManager != null) worldSpeed = obstacleSpeed * progressionManager.TotalSpeedIndex;

        float moveAmount = worldSpeed * Time.deltaTime;

        for (int i = 0; i < topPoints.Count; i++) topPoints[i].position += Vector3.left * moveAmount;
        for (int i = 0; i < bottomPoints.Count; i++) bottomPoints[i].position += Vector3.left * moveAmount;

        float currentTime = Time.time;
        topPoints.RemoveAll(p => currentTime - p.timestamp > trailDuration);
        bottomPoints.RemoveAll(p => currentTime - p.timestamp > trailDuration);

        pointTimer += Time.deltaTime;
        float interval = 1f / pointsPerSecond;

        if (pointTimer >= interval)
        {
            pointTimer = 0f;
            topPoints.Insert(0, new TrailPoint { position = GetTopPointWorld(), timestamp = currentTime });
            bottomPoints.Insert(0, new TrailPoint { position = GetBottomPointWorld(), timestamp = currentTime });
        }

        UpdateLine(topLine, topPoints, showTopTrail);
        UpdateLine(bottomLine, bottomPoints, showBottomTrail);
    }

    Vector3 GetTopPointWorld()
    {
        Vector3 center = playerCollider.transform.TransformPoint(playerCollider.center);
        return center + Vector3.up * (playerCollider.height / 2f);
    }

    Vector3 GetBottomPointWorld()
    {
        Vector3 center = playerCollider.transform.TransformPoint(playerCollider.center);
        return center - Vector3.up * (playerCollider.height / 2f);
    }

    void UpdateLine(LineRenderer line, List<TrailPoint> points, bool visible)
    {
        if (line == null) return;
        if (!visible) { line.positionCount = 0; return; }
        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++) line.SetPosition(i, points[i].position);
    }
}