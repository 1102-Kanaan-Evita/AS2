using UnityEngine;
using System.Collections;

public class GridMaker : MonoBehaviour
{
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

    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;

        for (int x = 0; x < gridSizeX; x ++)  {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius); //finds points nodes will occupy.
                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkable)); //checksphere returns true if collision detected. Passing unwalkable mask to the function at the end there.
                grid[x, y] = new Node(walkable, worldPoint); //populates array
            }
        }
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


    private void OnDrawGizmos() //helps to visualize
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y)); 

        if (grid!= null)
        {
            foreach (Node n in grid)
            {
                Gizmos.color = (n.walkable) ? Color.white : Color.red; //If true, set to white, else set to red gizmo color.
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
            }
        }
    }
}
