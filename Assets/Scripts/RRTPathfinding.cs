using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class RRTPathfinding : MonoBehaviour
{
    [Header("RRT Settings")]
    public float stepSize = 2f;
    public int maxIterations = 1000;
    public float goalTolerance = 2f;
    public float collisionCheckResolution = 0.5f;
    
    [Header("Path Smoothing")]
    public bool enableSmoothing = true;
    
    [Header("Debug")]
    public bool debugRRT = false;
    public bool visualizeTree = false;
    public Color treeColor = Color.yellow;
    
    private GridMaker grid;
    private PathRequestManager requestManager;
    private List<RRTNode> lastTree = new List<RRTNode>();
    private List<Vector3> lastFinalPath = new List<Vector3>();
    
    private class RRTNode
    {
        public Vector3 position;
        public RRTNode parent;
        
        public RRTNode(Vector3 pos, RRTNode parentNode = null)
        {
            position = pos;
            parent = parentNode;
        }
    }
    
    void Awake()
    {
        requestManager = GetComponent<PathRequestManager>();
        grid = GetComponent<GridMaker>();
    }
    
    public void StartFindPath(Vector3 startPos, Vector3 targetPos)
    {
        StartCoroutine(FindPath(startPos, targetPos));
    }
    
    IEnumerator FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        
        Vector3[] waypoints = new Vector3[0];
        bool pathSuccess = false;
        
        if (debugRRT)
        {
            Debug.Log("=== RRT DEBUG START ===");
            Debug.Log("Start Position: " + startPos);
            Debug.Log("Target Position: " + targetPos);
            Debug.Log("Distance to target: " + Vector3.Distance(startPos, targetPos).ToString("F2"));
            Debug.Log("Step Size: " + stepSize);
            Debug.Log("Goal Tolerance: " + goalTolerance);
            Debug.Log("Max Iterations: " + maxIterations);
        }
        
        // Build RRT tree
        List<RRTNode> tree = new List<RRTNode>();
        RRTNode startNode = new RRTNode(startPos);
        tree.Add(startNode);
        
        RRTNode goalNode = null;
        int iterationsUsed = 0;
        
        for (int i = 0; i < maxIterations; i++)
        {
            iterationsUsed = i + 1;
            
            // Sample random point (bias towards goal occasionally)
            Vector3 randomPoint;
            bool sampledGoal = false;
            if (Random.value < 0.1f) // 10% chance to sample goal
            {
                randomPoint = targetPos;
                sampledGoal = true;
            }
            else
            {
                randomPoint = GetRandomPoint();
            }
            
            // Find nearest node in tree
            RRTNode nearestNode = FindNearest(tree, randomPoint);
            float distToRandom = Vector3.Distance(nearestNode.position, randomPoint);
            
            // Steer towards random point
            Vector3 newPosition = Steer(nearestNode.position, randomPoint);
            
            // Check if path is collision-free
            bool collisionFree = IsPathCollisionFree(nearestNode.position, newPosition);
            
            if (debugRRT && i < 10) // Log first 10 iterations in detail
            {
                Debug.Log(string.Format("Iteration {0}: Sampled={1}{2}, Nearest={3}, Dist={4:F2}, New={5}, CollisionFree={6}",
                    i, randomPoint, sampledGoal ? " (GOAL)" : "", 
                    nearestNode.position, distToRandom, newPosition, collisionFree));
            }
            
            if (collisionFree)
            {
                RRTNode newNode = new RRTNode(newPosition, nearestNode);
                tree.Add(newNode);
                
                // Check if we reached the goal
                float distToGoal = Vector3.Distance(newPosition, targetPos);
                
                if (debugRRT && (i < 10 || distToGoal < goalTolerance * 2))
                {
                    Debug.Log(string.Format("  -> Added node {0} to tree. Distance to goal: {1:F2}", 
                        tree.Count, distToGoal));
                }
                
                if (distToGoal < goalTolerance)
                {
                    goalNode = newNode;
                    pathSuccess = true;
                    
                    if (debugRRT)
                    {
                        Debug.Log("=== RRT SUCCESS ===");
                        Debug.Log("Reached goal in " + i + " iterations");
                        Debug.Log("Tree size: " + tree.Count + " nodes");
                        Debug.Log("Final distance to target: " + distToGoal.ToString("F2"));
                    }
                    break;
                }
            }
            
            // Yield every 50 iterations to avoid freezing
            if (i % 50 == 0)
            {
                yield return null;
            }
        }
        
        sw.Stop();
        
        if (pathSuccess)
        {
            waypoints = ExtractPath(goalNode, targetPos);
            
            if (debugRRT)
            {
                Debug.Log("=== RRT PATH FOUND ===");
                Debug.Log("Time: " + sw.ElapsedMilliseconds + " ms");
                Debug.Log("Iterations: " + iterationsUsed + " / " + maxIterations);
                Debug.Log("Tree nodes: " + tree.Count);
                Debug.Log("Final waypoints: " + waypoints.Length);
                Debug.Log("======================");
            }
            
            // Store for visualization
            if (visualizeTree)
            {
                lastTree = new List<RRTNode>(tree);
                lastFinalPath = new List<Vector3>(waypoints);
            }
        }
        else
        {
            Debug.LogWarning("=== RRT FAILED ===");
            Debug.LogWarning("Failed to find path after " + maxIterations + " iterations");
            Debug.LogWarning("Tree size: " + tree.Count + " nodes");
            
            if (tree.Count > 1)
            {
                RRTNode closestToGoal = FindNearest(tree, targetPos);
                float distToGoal = Vector3.Distance(closestToGoal.position, targetPos);
                Debug.LogWarning("Closest node was " + distToGoal.ToString("F2") + " units from goal (tolerance: " + goalTolerance + ")");
            }
        }
        
        requestManager.FinishedProcessingPath(waypoints, pathSuccess);
    }
    
    Vector3 GetRandomPoint()
    {
        // Sample within grid bounds
        float x = Random.Range(-grid.gridWorldSize.x / 2, grid.gridWorldSize.x / 2);
        float z = Random.Range(-grid.gridWorldSize.y / 2, grid.gridWorldSize.y / 2);
        return transform.position + new Vector3(x, 0, z);
    }
    
    RRTNode FindNearest(List<RRTNode> tree, Vector3 point)
    {
        RRTNode nearest = tree[0];
        float minDist = Vector3.Distance(nearest.position, point);
        
        for (int i = 1; i < tree.Count; i++)
        {
            float dist = Vector3.Distance(tree[i].position, point);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = tree[i];
            }
        }
        
        return nearest;
    }
    
    Vector3 Steer(Vector3 from, Vector3 to)
    {
        Vector3 direction = (to - from).normalized;
        float distance = Vector3.Distance(from, to);
        
        if (distance < stepSize)
        {
            return to;
        }
        else
        {
            return from + direction * stepSize;
        }
    }
    
    bool IsPathCollisionFree(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        float distance = direction.magnitude;
        direction.Normalize();
        
        int steps = Mathf.CeilToInt(distance / collisionCheckResolution);
        
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3 checkPoint = Vector3.Lerp(from, to, t);
            
            Node node = grid.NodeFromWorldPoint(checkPoint);
            if (node == null || !node.walkable)
            {
                return false;
            }
        }
        
        return true;
    }
    
    Vector3[] ExtractPath(RRTNode goalNode, Vector3 targetPos)
    {
        List<Vector3> path = new List<Vector3>();
        
        // Trace back from goal to start
        RRTNode current = goalNode;
        int pathLength = 0;
        while (current != null)
        {
            path.Add(current.position);
            current = current.parent;
            pathLength++;
        }
        
        if (debugRRT)
        {
            Debug.Log("=== PATH EXTRACTION ===");
            Debug.Log("Traced back " + pathLength + " nodes from goal to start");
        }
        
        // Reverse to get start to goal order
        path.Reverse();
        
        if (debugRRT)
        {
            Debug.Log("After reverse, path goes from " + path[0] + " to " + path[path.Count - 1]);
        }
        
        // Add the actual target position at the end
        if (path.Count > 0 && Vector3.Distance(path[path.Count - 1], targetPos) > 0.1f)
        {
            path.Add(targetPos);
            if (debugRRT)
            {
                Debug.Log("Added final target position: " + targetPos);
            }
        }
        
        if (debugRRT)
        {
            Debug.Log("Raw RRT path: " + path.Count + " waypoints");
            for (int i = 0; i < Mathf.Min(5, path.Count); i++)
            {
                Debug.Log("  Waypoint[" + i + "]: " + path[i]);
            }
            if (path.Count > 5)
            {
                Debug.Log("  ... (" + (path.Count - 5) + " more)");
            }
        }
        
        // Smooth the path if enabled
        List<Vector3> finalPath;
        if (enableSmoothing)
        {
            finalPath = SmoothPath(path);
            
            if (debugRRT)
            {
                Debug.Log("=== PATH SMOOTHING ===");
                Debug.Log("Smoothed from " + path.Count + " to " + finalPath.Count + " waypoints");
                for (int i = 0; i < finalPath.Count; i++)
                {
                    Debug.Log("  Smoothed[" + i + "]: " + finalPath[i]);
                }
            }
        }
        else
        {
            finalPath = path;
            if (debugRRT)
            {
                Debug.Log("Smoothing disabled - using raw path");
            }
        }
        
        // Remove the first point (start position) since the unit is already there
        if (finalPath.Count > 1)
        {
            if (debugRRT)
            {
                Debug.Log("Removing start position: " + finalPath[0]);
            }
            finalPath.RemoveAt(0);
        }
        
        if (debugRRT)
        {
            Debug.Log("=== FINAL PATH ===");
            Debug.Log("Total waypoints to follow: " + finalPath.Count);
        }
        
        return finalPath.ToArray();
    }
    
    List<Vector3> SmoothPath(List<Vector3> path)
    {
        if (path.Count <= 2)
        {
            if (debugRRT)
            {
                Debug.Log("Path too short to smooth (only " + path.Count + " points)");
            }
            return path;
        }
        
        List<Vector3> smoothed = new List<Vector3>();
        smoothed.Add(path[0]); // Always add start
        
        if (debugRRT)
        {
            Debug.Log("Smoothing path with " + path.Count + " points...");
        }
        
        int currentIndex = 0;
        int smoothingSteps = 0;
        
        while (currentIndex < path.Count - 1)
        {
            // Try to connect to furthest visible point
            int furthestIndex = currentIndex + 1;
            
            for (int i = path.Count - 1; i > currentIndex + 1; i--)
            {
                if (IsPathCollisionFree(path[currentIndex], path[i]))
                {
                    furthestIndex = i;
                    if (debugRRT)
                    {
                        Debug.Log("  Step " + smoothingSteps + ": Can see from index " + currentIndex + " to " + furthestIndex + 
                                 " (skipping " + (furthestIndex - currentIndex - 1) + " points)");
                    }
                    break;
                }
            }
            
            // Only add if it's a meaningful waypoint (not too close to previous)
            if (furthestIndex > currentIndex)
            {
                smoothed.Add(path[furthestIndex]);
                currentIndex = furthestIndex;
                smoothingSteps++;
            }
            else
            {
                if (debugRRT)
                {
                    Debug.Log("  Could not find further visible point from index " + currentIndex);
                }
                break;
            }
        }
        
        // Ensure we have at least start and end
        if (smoothed.Count < 2 || Vector3.Distance(smoothed[smoothed.Count - 1], path[path.Count - 1]) > 0.1f)
        {
            smoothed.Add(path[path.Count - 1]);
            if (debugRRT)
            {
                Debug.Log("  Added final endpoint to ensure complete path");
            }
        }
        
        if (debugRRT)
        {
            Debug.Log("Smoothing complete: " + path.Count + " -> " + smoothed.Count + " waypoints");
        }
        
        return smoothed;
    }
    
    void OnDrawGizmos()
    {
        if (!visualizeTree || lastTree == null || lastTree.Count == 0) return;
        
        // Draw the RRT tree
        Gizmos.color = treeColor;
        foreach (RRTNode node in lastTree)
        {
            if (node.parent != null)
            {
                Gizmos.DrawLine(node.position + Vector3.up * 0.3f, node.parent.position + Vector3.up * 0.3f);
            }
            Gizmos.DrawWireSphere(node.position + Vector3.up * 0.3f, 0.2f);
        }
        
        // Draw the final path in a different color
        if (lastFinalPath != null && lastFinalPath.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < lastFinalPath.Count; i++)
            {
                Gizmos.DrawSphere(lastFinalPath[i] + Vector3.up * 0.5f, 0.4f);
                if (i > 0)
                {
                    Gizmos.DrawLine(lastFinalPath[i - 1] + Vector3.up * 0.5f, lastFinalPath[i] + Vector3.up * 0.5f);
                }
            }
        }
    }
}