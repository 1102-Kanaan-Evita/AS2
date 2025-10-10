using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GridMaker : MonoBehaviour
{
    public bool onlyDisplayPathGizmos;
    public LayerMask unwalkable;
    public Vector2 gridWorldSize;
    public float nodeRadius;
    Node[,] grid;

    float nodeDiameter;
    int gridSizeX, gridSizeY;
    private void Start()
    {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter); // gives how many nodes we can fit in world size.
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        CreateGrid();
    }

    public int MaxSize
    {
        get
        {
            return gridSizeX * gridSizeY;
        }
    }

    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;

        for (int x = 0; x < gridSizeX; x ++)  {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius); //finds points nodes will occupy.
                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkable)); //checksphere returns true if collision detected. Passing unwalkable mask to the function at the end there.
                grid[x, y] = new Node(walkable, worldPoint, x, y); //populates array
            }
        }
    }

    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue; //must be in center of 3x3 block

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                //is this inside grid?
                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }
        return neighbors;
    }

    public Node NodeFromWorldPoint(Vector3 WorldPosition)
    {
        //find percentage of world it's on, left being 0
        float percentX = (WorldPosition.x + gridWorldSize.x/2) / gridWorldSize.x;
        float percentY = (WorldPosition.z + gridWorldSize.y/2) / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX); //clamps between 0 and 1
        percentY = Mathf.Clamp01(percentY);

        //get x and y indices of grid array
        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return grid[x, y]; 
    }

    public List<Node> path;
    private void OnDrawGizmos() //helps to visualize
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));
        if (onlyDisplayPathGizmos)
        {
            if (path != null)
            {
                foreach (Node n in path)
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
                }
            }
        }
        else
        {
            if (grid != null)
            {
                foreach (Node n in grid)
                {
                    Gizmos.color = (n.walkable) ? Color.white : Color.red; //If true, set to white, else set to red gizmo color.
                    if (path != null)
                    {
                        if (path.Contains(n))
                        {
                            Gizmos.color = Color.black;
                        }
                    }
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
                }
            }
        }
    }
}
