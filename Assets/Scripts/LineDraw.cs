using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class LineDraw : MonoBehaviour
{
    [SerializeField] private float _boxWidth = 10f;
    [SerializeField] private float _boxDepth = 5f;

    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponentInChildren<LineRenderer>();
        DrawLine();
    }

    private void Start()
    {
        DrawLine();
    }

    private void DrawLine()
    {
        float halfWidth = _boxWidth * 0.5f;

        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        Bounds quadBounds = mesh.bounds;

        Vector3 goalLineCenter = transform.TransformPoint(new Vector3(
            quadBounds.center.x,
            quadBounds.min.y,
            quadBounds.center.z));

        Vector3 right = transform.right;
        Vector3 depth = -transform.forward * _boxDepth;

        Vector3 leftGoalLine = goalLineCenter - right * halfWidth;
        Vector3 rightGoalLine = goalLineCenter + right * halfWidth;

        _lineRenderer.useWorldSpace = true;
        _lineRenderer.loop = false;
        _lineRenderer.positionCount = 4;
        _lineRenderer.SetPositions(new[]
        {
            leftGoalLine,
            leftGoalLine + depth,
            rightGoalLine + depth,
            rightGoalLine
        });
    }
}
