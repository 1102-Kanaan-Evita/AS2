using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

//It's worth noting that this whole thing can be optimized by using a heap instead of a list, but it'll be easier to implement the list and ya boy is needing to work on compilers too.

public class Pathfinding : MonoBehaviour
{
    public Transform seeker, target;

    GridMaker grid;

    private void Awake()
    {
        grid = GetComponent<GridMaker>();
    }

    private void Update()
    {
        if (Input.GetButtonDown("Jump")) //CHANGE THIS TO BE ON CLICK
        {
            FindPath(seeker.position, target.position);
        }
    }
    void FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        //convert worldpositions into nodes. Method exists in grid.
        Node startNode = grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(targetPos);

        Heap<Node> openSet = new Heap<Node>(grid.MaxSize);
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet.RemoveFirst();
            closedSet.Add(currentNode);

            if (currentNode == targetNode) //path found
            {
                sw.Stop();
                print("Path found: " + sw.ElapsedMilliseconds + " ms");
                ReTracePath(startNode, targetNode);
                return;
            }

            //loop through each of current set
            foreach (Node neighbor in grid.GetNeighbors(currentNode))
            {
                //check whether neighbor is walkable or in closed list
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

                    //check if openset neighbor
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                } 
            }
        }
    }

    void ReTracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode; //trace backwards through parents

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();

        grid.path = path;
    }

    int GetDistance(Node nodeA, Node nodeB)
    {
        //count distance on x axis and distance on y axis. Take lowest number and that will give number of diagonal moves. Then subtract lower number from higher. Diagonal costs 14, 10 costs for vert/horizontal
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (dstX > dstY)
        {
            return 14 * dstY + 10 * (dstX - dstY);
        }
        return 14 * dstX + 10 * (dstY - dstX);
    }
}
