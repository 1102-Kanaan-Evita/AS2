using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System;
public class Pathfinding : MonoBehaviour
{
    GridMaker grid;
    PathRequestManager requestManager;
    
    [Header("Path Simplification")]
    public bool simplifyPath = true;
    public float simplificationAngleThreshold = 5f; // Degrees - smaller = more waypoints, VERY IMPORTANT FOR OBSTACLES

    public void StartFindPath(Vector3 startPos, Vector3 targetPos)
    {
        StartCoroutine(FindPath(startPos, targetPos));
    }

    private void Awake()
    {
        requestManager = GetComponent<PathRequestManager>();
        grid = GetComponent<GridMaker>();
    }

    IEnumerator FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        
        Vector3[] waypoints = new Vector3[0];
        bool pathSuccess = false;

        Node startNode = grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(targetPos);

        if (startNode.walkable && targetNode.walkable)
        {
            Heap<Node> openSet = new Heap<Node>(grid.MaxSize);
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                Node currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);

                if (currentNode == targetNode)
                {
                    sw.Stop();
                    print("Path found: " + sw.ElapsedMilliseconds + " ms");
                    pathSuccess = true;
                    break;
                }

                foreach (Node neighbor in grid.GetNeighbors(currentNode))
                {
                    if (!neighbor.walkable || closedSet.Contains(neighbor))
                    {
                        continue;
                    }

                    int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                    if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                    {
                        neighbor.gCost = newMovementCostToNeighbor;
                        neighbor.hCost = GetDistance(neighbor, targetNode);
                        neighbor.parent = currentNode;

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                        else openSet.UpdateItem(neighbor);
                    }
                }
            }
        }

        yield return null;

        if (pathSuccess)
        {
            waypoints = RetracePath(startNode, targetNode);
        }
        requestManager.FinishedProcessingPath(waypoints, pathSuccess);
    }

    Vector3[] RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        Vector3[] waypoints;
        if (simplifyPath)
        {
            waypoints = SimplifyPath(path);
        }
        else
        {
            // Return all nodes as waypoints
            waypoints = new Vector3[path.Count];
            for (int i = 0; i < path.Count; i++)
            {
                waypoints[i] = path[path.Count - 1 - i].worldPosition;
            }
        }
        
        // DON'T reverse - SimplifyPath already handles order correctly
        // Array.Reverse(waypoints); // REMOVED THIS LINE

        return waypoints;
    }
    
    Vector3[] SimplifyPath(List<Node> path)
    {
        List<Vector3> waypoints = new List<Vector3>();
        Vector2 directionOld = Vector2.zero;

        // Always add the first point
        if (path.Count > 0)
        {
            waypoints.Add(path[path.Count - 1].worldPosition);
        }

        for (int i = path.Count - 2; i > 0; i--)
        {
            Vector2 directionNew = new Vector2(
                path[i + 1].gridX - path[i].gridX,
                path[i + 1].gridY - path[i].gridY
            ).normalized;

            // Check if direction changed significantly
            if (directionOld == Vector2.zero || Vector2.Angle(directionOld, directionNew) > simplificationAngleThreshold)
            {
                waypoints.Add(path[i].worldPosition);
                directionOld = directionNew;
            }
        }
        
        // Always add the last point
        if (path.Count > 0)
        {
            waypoints.Add(path[0].worldPosition);
        }

        return waypoints.ToArray();
    }

    int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (dstX > dstY)
        {
            return 14 * dstY + 10 * (dstX - dstY);
        }
        return 14 * dstX + 10 * (dstY - dstX);
    }
}