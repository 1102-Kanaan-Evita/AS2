using UnityEngine;
using System.Collections.Generic;

public class RTSController : MonoBehaviour
{
    [Header("Selection")]
    public LayerMask selectableLayer;
    public Color selectionBoxColor = new Color(0, 1, 0, 0.3f);
    public Color selectionBorderColor = Color.green;
    
    [Header("Movement Settings")]
    public bool usePotentialFields = false;
    public LayerMask obstacleLayer;
    
    private Vector3 mouseDownPos;
    private bool isDragging = false;
    private List<EntityUnit> selectedUnits = new List<EntityUnit>();
    private Rect selectionRect;
    
    // For shift+right click waypoint chaining
    private List<Vector3> waypointChain = new List<Vector3>();
    private LineRenderer waypointLineRenderer;
    
    void Start()
    {
        // Create line renderer for showing waypoint chains
        GameObject waypointLine = new GameObject("WaypointLine");
        waypointLineRenderer = waypointLine.AddComponent<LineRenderer>();
        waypointLineRenderer.startWidth = 0.3f;
        waypointLineRenderer.endWidth = 0.3f;
        waypointLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        waypointLineRenderer.startColor = Color.yellow;
        waypointLineRenderer.endColor = Color.yellow;
        waypointLineRenderer.positionCount = 0;
    }
    
    void Update()
    {
        HandleSelection();
        HandleMovementCommands();
        HandleKeyCommands();
    }
    
    void HandleKeyCommands()
    {
        // Toggle potential fields with 'P' key
        if (Input.GetKeyDown(KeyCode.P))
        {
            usePotentialFields = !usePotentialFields;
            Debug.Log("Potential Fields: " + (usePotentialFields ? "ON" : "OFF"));
        }
        
        // Clear waypoint chain with 'C' key
        if (Input.GetKeyDown(KeyCode.C))
        {
            waypointChain.Clear();
            waypointLineRenderer.positionCount = 0;
        }
    }
    
    void HandleSelection()
    {
        // Left mouse button down - start selection
        if (Input.GetMouseButtonDown(0))
        {
            mouseDownPos = Input.mousePosition;
            isDragging = true;
            
            // Single click selection
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectableLayer))
            {
                EntityUnit unit = hit.collider.GetComponent<EntityUnit>();
                if (unit != null)
                {
                    if (!Input.GetKey(KeyCode.LeftShift))
                    {
                        DeselectAll();
                    }
                    SelectUnit(unit);
                }
            }
            else if (!Input.GetKey(KeyCode.LeftShift))
            {
                DeselectAll();
            }
        }
        
        // Left mouse button held - update selection box
        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 currentMousePos = Input.mousePosition;
            float distance = Vector3.Distance(mouseDownPos, currentMousePos);
            
            // Only do box selection if dragged more than a threshold
            if (distance > 10f)
            {
                UpdateSelectionBox(mouseDownPos, currentMousePos);
            }
        }
        
        // Left mouse button up - finalize selection
        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging && Vector3.Distance(mouseDownPos, Input.mousePosition) > 10f)
            {
                SelectUnitsInBox();
            }
            isDragging = false;
        }
    }
    
    void HandleMovementCommands()
    {
        // Right click - move selected units
        if (Input.GetMouseButtonDown(1) && selectedUnits.Count > 0)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                bool isQueueing = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                bool isInterceptMode = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                
                // Check if right-clicked on another entity to follow
                EntityUnit targetEntity = hit.collider.GetComponent<EntityUnit>();
                
                if (targetEntity != null && !selectedUnits.Contains(targetEntity))
                {
                    // Follow mode
                    if (isInterceptMode)
                    {
                        // Intercept mode - move to predicted location
                        Vector3 interceptPos = targetEntity.PredictPosition(2f); // 2 second prediction
                        IssueMovementCommand(interceptPos, isQueueing, null);
                    }
                    else
                    {
                        // Follow mode - track the entity
                        IssueMovementCommand(hit.point, isQueueing, targetEntity);
                    }
                }
                else
                {
                    // Normal move to location
                    Vector3 targetPos = hit.point;
                    IssueMovementCommand(targetPos, isQueueing, null);
                }
            }
        }
        
        // Enter key to execute waypoint chain (kept for backward compatibility)
        if (Input.GetKeyDown(KeyCode.Return) && waypointChain.Count > 0 && selectedUnits.Count > 0)
        {
            foreach (EntityUnit unit in selectedUnits)
            {
                unit.SetWaypointChain(new List<Vector3>(waypointChain), usePotentialFields);
            }
            waypointChain.Clear();
            waypointLineRenderer.positionCount = 0;
        }
    }
    
    void IssueMovementCommand(Vector3 destination, bool queueCommand, EntityUnit followTarget)
    {
        if (selectedUnits.Count == 1)
        {
            if (followTarget != null)
            {
                selectedUnits[0].FollowEntity(followTarget, usePotentialFields, queueCommand);
            }
            else
            {
                selectedUnits[0].MoveTo(destination, usePotentialFields, queueCommand);
            }
        }
        else
        {
            // Formation movement - spread units out slightly
            int index = 0;
            int cols = Mathf.CeilToInt(Mathf.Sqrt(selectedUnits.Count));
            float spacing = 3f;
            
            foreach (EntityUnit unit in selectedUnits)
            {
                int row = index / cols;
                int col = index % cols;
                Vector3 offset = new Vector3((col - cols/2f) * spacing, 0, (row - cols/2f) * spacing);
                
                if (followTarget != null)
                {
                    // Follow with formation offset
                    unit.FollowEntity(followTarget, usePotentialFields, queueCommand, offset);
                }
                else
                {
                    unit.MoveTo(destination + offset, usePotentialFields, queueCommand);
                }
                index++;
            }
        }
    }
    
    void UpdateSelectionBox(Vector3 start, Vector3 end)
    {
        float minX = Mathf.Min(start.x, end.x);
        float maxX = Mathf.Max(start.x, end.x);
        float minY = Mathf.Min(start.y, end.y);
        float maxY = Mathf.Max(start.y, end.y);
        
        selectionRect = new Rect(minX, minY, maxX - minX, maxY - minY);
    }
    
    void SelectUnitsInBox()
    {
        if (!Input.GetKey(KeyCode.LeftShift))
        {
            DeselectAll();
        }
        
        EntityUnit[] allUnits = FindObjectsOfType<EntityUnit>();
        foreach (EntityUnit unit in allUnits)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(unit.transform.position);
            if (selectionRect.Contains(screenPos))
            {
                SelectUnit(unit);
            }
        }
    }
    
    void SelectUnit(EntityUnit unit)
    {
        if (!selectedUnits.Contains(unit))
        {
            selectedUnits.Add(unit);
            unit.SetSelected(true);
        }
    }
    
    void DeselectAll()
    {
        foreach (EntityUnit unit in selectedUnits)
        {
            unit.SetSelected(false);
        }
        selectedUnits.Clear();
    }
    
    void UpdateWaypointLineRenderer()
    {
        waypointLineRenderer.positionCount = waypointChain.Count;
        for (int i = 0; i < waypointChain.Count; i++)
        {
            waypointLineRenderer.SetPosition(i, waypointChain[i] + Vector3.up * 0.2f);
        }
    }
    
    void OnGUI()
    {
        // Draw selection box
        if (isDragging && Vector3.Distance(mouseDownPos, Input.mousePosition) > 10f)
        {
            Rect screenRect = new Rect(
                selectionRect.x,
                Screen.height - selectionRect.y - selectionRect.height,
                selectionRect.width,
                selectionRect.height
            );
            
            // Fill
            GUI.color = selectionBoxColor;
            GUI.DrawTexture(screenRect, Texture2D.whiteTexture);
            
            // Border
            GUI.color = selectionBorderColor;
            DrawRectBorder(screenRect, 2);
        }
        
        // Display mode
        GUI.color = Color.white;
        GUI.Label(new Rect(10, 10, 300, 30), "Movement Mode: " + (usePotentialFields ? "A* + Potential Fields" : "A* Only"));
        GUI.Label(new Rect(10, 30, 300, 30), "Press 'P' to toggle | Shift+RClick for waypoints");
        GUI.Label(new Rect(10, 50, 300, 30), "Selected Units: " + selectedUnits.Count);
    }
    
    void DrawRectBorder(Rect rect, int thickness)
    {
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
    }
}