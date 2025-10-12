using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using Debug = UnityEngine.Debug;

public class Pathfinding : MonoBehaviour
{
    GridMaker grid;
    PathRequestManager requestManager;
    
    [Header("Path Simplification")]
    public bool simplifyPath = true;
    public float simplificationAngleThreshold = 5f;
    
    [Header("Debug")]
    public bool debugPathfinding = false; // Degrees - smaller = more waypoints, VERY IMPORTANT FOR OBSTACLES

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
        
        if (debugPathfinding)
        {
            Debug.Log("=== A* DEBUG ===");
            Debug.Log("Start World Position: " + startPos);
            Debug.Log("Start Node Grid Position: (" + startNode.gridX + ", " + startNode.gridY + ")");
            Debug.Log("Start Node World Position: " + startNode.worldPosition);
            Debug.Log("Target World Position: " + targetPos);
            Debug.Log("Target Node Grid Position: (" + targetNode.gridX + ", " + targetNode.gridY + ")");
            Debug.Log("Target Node World Position: " + targetNode.worldPosition);
        }

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

        // Trace backwards from end to start
        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        
        // IMPORTANT: Add the start node to complete the path
        path.Add(startNode);
        
        if (debugPathfinding)
        {
            Debug.Log("=== PATH RETRACE ===");
            Debug.Log("Raw path has " + path.Count + " nodes");
            Debug.Log("Path[" + (path.Count - 1) + "] (START): Grid(" + path[path.Count - 1].gridX + "," + path[path.Count - 1].gridY + ") World: " + path[path.Count - 1].worldPosition);
            if (path.Count > 1)
            {
                Debug.Log("Path[" + (path.Count - 2) + "] (NEXT):  Grid(" + path[path.Count - 2].gridX + "," + path[path.Count - 2].gridY + ") World: " + path[path.Count - 2].worldPosition);
            }
            if (path.Count > 2)
            {
                Debug.Log("Path[" + (path.Count - 3) + "] (NEXT):  Grid(" + path[path.Count - 3].gridX + "," + path[path.Count - 3].gridY + ") World: " + path[path.Count - 3].worldPosition);
            }
            Debug.Log("...");
            Debug.Log("Path[1] (BEFORE END): Grid(" + path[1].gridX + "," + path[1].gridY + ") World: " + path[1].worldPosition);
            Debug.Log("Path[0] (END):        Grid(" + path[0].gridX + "," + path[0].gridY + ") World: " + path[0].worldPosition);
        }
        
        // Now path = [endNode, ..., ..., startNode]
        // We need waypoints going [startPos -> endPos]

        Vector3[] waypoints;
        if (simplifyPath)
        {
            waypoints = SimplifyPath(path);
        }
        else
        {
            // Return all nodes reversed (start to end)
            waypoints = new Vector3[path.Count];
            for (int i = 0; i < path.Count; i++)
            {
                waypoints[i] = path[path.Count - 1 - i].worldPosition;
            }
        }
        
        if (debugPathfinding)
        {
            Debug.Log("=== FINAL WAYPOINTS ===");
            for (int i = 0; i < waypoints.Length; i++)
            {
                Debug.Log("Waypoint[" + i + "]: " + waypoints[i]);
            }
            Debug.Log("==================");
        }
        
        return waypoints;
    }
    
    Vector3[] SimplifyPath(List<Node> path)
    {
        // Path list: [endNode, node, node, startNode]
        // We want: [startPos, waypoint, waypoint, endPos]
        
        List<Vector3> waypoints = new List<Vector3>();
        
        if (path.Count == 0) return waypoints.ToArray();
        
        // Always add start position (last in path list)
        waypoints.Add(path[path.Count - 1].worldPosition);
        
        if (debugPathfinding)
        {
            Debug.Log("=== SIMPLIFY PATH ===");
            Debug.Log("Added waypoint[0] (START): " + path[path.Count - 1].worldPosition);
        }
        
        Vector2 directionOld = Vector2.zero;
        
        // Iterate from second-to-last down to index 1
        for (int i = path.Count - 2; i > 0; i--)
        {
            Vector2 directionNew = new Vector2(
                path[i + 1].gridX - path[i].gridX,
                path[i + 1].gridY - path[i].gridY
            ).normalized;

            float angle = directionOld == Vector2.zero ? 180f : Vector2.Angle(directionOld, directionNew);
            
            // Add waypoint when direction changes significantly
            if (directionOld == Vector2.zero || angle > simplificationAngleThreshold)
            {
                waypoints.Add(path[i].worldPosition);
                
                if (debugPathfinding)
                {
                    Debug.Log("Added waypoint[" + waypoints.Count + "] at index " + i + ": " + path[i].worldPosition + 
                             " (angle change: " + angle.ToString("F1") + "°)");
                }
            }
            else if (debugPathfinding)
            {
                Debug.Log("Skipped index " + i + ": " + path[i].worldPosition + 
                         " (angle: " + angle.ToString("F1") + "° < threshold " + simplificationAngleThreshold + "°)");
            }
            
            directionOld = directionNew;
        }
        
        // Always add end position (first in path list)
        if (path.Count > 0)
        {
            waypoints.Add(path[0].worldPosition);
            
            if (debugPathfinding)
            {
                Debug.Log("Added waypoint[" + (waypoints.Count - 1) + "] (END): " + path[0].worldPosition);
            }
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