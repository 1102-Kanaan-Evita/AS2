using UnityEngine;
using System.Collections;

public class Unit : MonoBehaviour
{
    public Transform target;
    float speed = 20; //this whole section needs renovation to work with potential fields
    Vector3[] path;
    int targetIndex;
    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.5f;
        lineRenderer.endWidth = 0.5f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // Simple visible shader
        lineRenderer.positionCount = 0;
        PathRequestManager.RequestPath(transform.position, target.position, OnPathFound);
    }

    public void OnPathFound(Vector3[] newPath, bool pathSuccessful)
    {
        if (pathSuccessful)
        {
            path = newPath;
            StopCoroutine("FollowPath");
            StartCoroutine("FollowPath");

            lineRenderer.positionCount = path.Length + 1;
            lineRenderer.SetPosition(0, transform.position + Vector3.up * 0.1f); // first point = unit's current position with offset

            for (int i = 0; i < path.Length; i++)
            {
                lineRenderer.SetPosition(i + 1, path[i] + Vector3.up * 0.1f); // subsequent points with vertical offset
            }

        }
    }

    IEnumerator FollowPath()
    {
        Vector3 currentWaypoint = path[0];

        while (true)
        {
            if (transform.position == currentWaypoint)
            {
                targetIndex++;
                if (targetIndex >= path.Length)
                {
                    yield break;
                }
                currentWaypoint = path[targetIndex];
            }

            transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, speed * Time.deltaTime); //don't forget to change this
            yield return null;
        }
    }

    public void OnDrawGizmos()
    {
        if (path != null)
        {
            for (int i = targetIndex; i < path.Length; i++)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawCube(path[i], Vector3.one);

                if (i == targetIndex)
                {
                    Gizmos.DrawLine(transform.position, path[i]);
                }
                else
                {
                    Gizmos.DrawLine(path[i - 1], path[i]);
                }
            }
        }
    }
}