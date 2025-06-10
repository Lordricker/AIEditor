using UnityEngine;

public class LineManager : MonoBehaviour
{
    private LineRenderer lineRenderer;

    public void Initialize(Vector3 startPosition, Vector3 endPosition)
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
    }

    public void UpdateLine(Vector3 startPosition, Vector3 endPosition)
    {
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
    }
}
