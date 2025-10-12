using UnityEngine;
using System.Collections.Generic;

public enum MovementMode
{
    AStar,
    AStarPF,
    PotentialFieldsOnly
}

public class RTSController : MonoBehaviour
{
    [Header("Selection")]
    public LayerMask selectableLayer;
    public Color selectionBoxColor = new Color(0, 1, 0, 0.3f);
    public Color selectionBorderColor = Color.green;
    
    [Header("Movement Settings")]
    public MovementMode movementMode = MovementMode.AStarPF;
    public LayerMask obstacleLayer;
    public float formationSpacing = 3f;
    
    [Header("Pathfinding Algorithm")]
    public bool useRRT = false; // Toggle between A* and RRT
    
    [Header("Visual")]
    public bool showFormationGizmos = true;
    
    [Header("Path Visualization Mode")]
    public bool pathVisualizationMode = false;
    public Color pathVisualizationColor = Color.cyan;
    public float pathPointSize = 0.3f;
    
    private Vector3 mouseDownPos;
    private bool isDragging = false;
    private List<EntityUnit> selectedUnits = new List<EntityUnit>();
    private Rect selectionRect;
    
    private List<Vector3> waypointChain = new List<Vector3>();
    private LineRenderer waypointLineRenderer;
    
    private Vector3? pathVisStart = null;
    private Vector3? pathVisEnd = null;
    private Vector3[] visualizedPath = null;
    private LineRenderer pathVisLineRenderer;
    
    void Start()
    {
        GameObject waypointLine = new GameObject("WaypointLine");
        waypointLineRenderer = waypointLine.AddComponent<LineRenderer>();
        waypointLineRenderer.startWidth = 0.3f;
        waypointLineRenderer.endWidth = 0.3f;
        waypointLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        waypointLineRenderer.startColor = Color.yellow;
        waypointLineRenderer.endColor = Color.yellow;
        waypointLineRenderer.positionCount = 0;
        
        GameObject pathVisLine = new GameObject("PathVisualizationLine");
        pathVisLineRenderer = pathVisLine.AddComponent<LineRenderer>();
        pathVisLineRenderer.startWidth = 0.4f;
        pathVisLineRenderer.endWidth = 0.4f;
        pathVisLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        pathVisLineRenderer.startColor = pathVisualizationColor;
        pathVisLineRenderer.endColor = pathVisualizationColor;
        pathVisLineRenderer.positionCount = 0;
    }
    
    void Update()
    {
        if (pathVisualizationMode)
        {
            HandlePathVisualization();
        }
        else
        {
            HandleSelection();
            HandleMovementCommands();
        }
        
        HandleKeyCommands();
    }
    
    void HandleKeyCommands()
    {
        // Toggle RRT pathfinding - works in both modes
        if (Input.GetKeyDown(KeyCode.R))
        {
            useRRT = !useRRT;
            Debug.Log("Pathfinding Algorithm: " + (useRRT ? "RRT" : "A*"));
        }
        
        if (Input.GetKeyDown(KeyCode.V))
        {
            pathVisualizationMode = !pathVisualizationMode;
            
            if (pathVisualizationMode)
            {
                Debug.Log("Path Visualization Mode: ON - Click start, then click end to show path");
                DeselectAll();
                pathVisStart = null;
                pathVisEnd = null;
                visualizedPath = null;
                pathVisLineRenderer.positionCount = 0;
            }
            else
            {
                Debug.Log("Path Visualization Mode: OFF - Normal RTS controls restored");
                pathVisStart = null;
                pathVisEnd = null;
                visualizedPath = null;
                pathVisLineRenderer.positionCount = 0;
            }
        }
        
        if (!pathVisualizationMode)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                movementMode = MovementMode.AStar;
                Debug.Log("Movement Mode: A* Only");
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                movementMode = MovementMode.AStarPF;
                Debug.Log("Movement Mode: A* + Potential Fields");
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                movementMode = MovementMode.PotentialFieldsOnly;
                Debug.Log("Movement Mode: Potential Fields Only");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (pathVisualizationMode)
            {
                pathVisStart = null;
                pathVisEnd = null;
                visualizedPath = null;
                pathVisLineRenderer.positionCount = 0;
                Debug.Log("Path visualization cleared");
            }
            else
            {
                waypointChain.Clear();
                waypointLineRenderer.positionCount = 0;
            }
        }
    }
    
    void HandleSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mouseDownPos = Input.mousePosition;
            isDragging = true;
            
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
        
        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 currentMousePos = Input.mousePosition;
            float distance = Vector3.Distance(mouseDownPos, currentMousePos);
            
            if (distance > 10f)
            {
                UpdateSelectionBox(mouseDownPos, currentMousePos);
            }
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging && Vector3.Distance(mouseDownPos, Input.mousePosition) > 10f)
            {
                SelectUnitsInBox();
            }
            isDragging = false;
        }
    }
    
    void HandlePathVisualization()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                if (!pathVisStart.HasValue)
                {
                    pathVisStart = hit.point;
                    Debug.Log("Path start set at: " + hit.point);
                }
                else
                {
                    pathVisEnd = hit.point;
                    Debug.Log("Path end set at: " + hit.point);
                    
                    // Pass useRRT flag to path request
                    PathRequestManager.RequestPath(pathVisStart.Value, pathVisEnd.Value, OnPathVisualizationComplete, useRRT);
                }
            }
        }
        
        if (Input.GetMouseButtonDown(1))
        {
            pathVisStart = null;
            pathVisEnd = null;
            visualizedPath = null;
            pathVisLineRenderer.positionCount = 0;
            Debug.Log("Path visualization reset - click to set new start point");
        }
    }
    
    void OnPathVisualizationComplete(Vector3[] path, bool success)
    {
        if (success && path.Length > 0)
        {
            visualizedPath = path;
            
            Vector3 startPoint = pathVisStart.HasValue ? pathVisStart.Value : path[0];
            Vector3 endPoint = pathVisEnd.HasValue ? pathVisEnd.Value : path[path.Length - 1];
            
            pathVisLineRenderer.positionCount = path.Length + 1;
            pathVisLineRenderer.SetPosition(0, startPoint + Vector3.up * 0.5f);
            
            for (int i = 0; i < path.Length; i++)
            {
                pathVisLineRenderer.SetPosition(i + 1, path[i] + Vector3.up * 0.5f);
            }
            
            float pathLength = CalculatePathLength(startPoint, path);
            
            Debug.Log("Path found with " + path.Length + " waypoints");
            Debug.Log("Total distance: " + pathLength.ToString("F2") + " units");
            Debug.Log("Algorithm used: " + (useRRT ? "RRT" : "A*"));
            
            pathVisStart = null;
            pathVisEnd = null;
        }
        else
        {
            Debug.LogWarning("No path found between points!");
            pathVisStart = null;
            pathVisEnd = null;
        }
    }
    
    float CalculatePathLength(Vector3 startPoint, Vector3[] path)
    {
        if (path == null || path.Length == 0) return 0;
        
        float length = Vector3.Distance(startPoint, path[0]);
        
        for (int i = 1; i < path.Length; i++)
        {
            length += Vector3.Distance(path[i - 1], path[i]);
        }
        
        return length;
    }
    
    void HandleMovementCommands()
    {
        if (Input.GetMouseButtonDown(1) && selectedUnits.Count > 0)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                bool isQueueing = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                bool isInterceptMode = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                
                EntityUnit targetEntity = hit.collider.GetComponent<EntityUnit>();
                
                if (targetEntity != null && !selectedUnits.Contains(targetEntity))
                {
                    if (isInterceptMode)
                    {
                        Vector3 interceptPos = targetEntity.PredictPosition(2f);
                        IssueMovementCommand(interceptPos, isQueueing, null);
                    }
                    else
                    {
                        IssueMovementCommand(hit.point, isQueueing, targetEntity);
                    }
                }
                else
                {
                    Vector3 targetPos = hit.point;
                    IssueMovementCommand(targetPos, isQueueing, null);
                }
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Return) && waypointChain.Count > 0 && selectedUnits.Count > 0)
        {
            foreach (EntityUnit unit in selectedUnits)
            {
                unit.SetWaypointChain(new List<Vector3>(waypointChain), movementMode, useRRT);
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
                selectedUnits[0].FollowEntity(followTarget, movementMode, queueCommand, Vector3.zero, useRRT);
            }
            else
            {
                selectedUnits[0].MoveTo(destination, movementMode, queueCommand, useRRT);
            }
        }
        else
        {
            int unitCount = selectedUnits.Count;
            
            for (int i = 0; i < unitCount; i++)
            {
                Vector3 unitDestination;
                
                if (unitCount == 2)
                {
                    unitDestination = destination + new Vector3((i == 0 ? -formationSpacing : formationSpacing), 0, 0);
                }
                else if (unitCount <= 5)
                {
                    float angle = (360f / unitCount) * i * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(
                        Mathf.Cos(angle) * formationSpacing,
                        0,
                        Mathf.Sin(angle) * formationSpacing
                    );
                    unitDestination = destination + offset;
                }
                else
                {
                    int ring = i / 6;
                    int posInRing = i % 6;
                    float ringRadius = formationSpacing * (ring + 1);
                    float angle = (360f / 6) * posInRing * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(
                        Mathf.Cos(angle) * ringRadius,
                        0,
                        Mathf.Sin(angle) * ringRadius
                    );
                    unitDestination = destination + offset;
                }
                
                if (followTarget != null)
                {
                    selectedUnits[i].FollowEntity(followTarget, movementMode, queueCommand, unitDestination - destination, useRRT);
                }
                else
                {
                    selectedUnits[i].MoveTo(unitDestination, movementMode, queueCommand, useRRT);
                }
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
        if (isDragging && Vector3.Distance(mouseDownPos, Input.mousePosition) > 10f && !pathVisualizationMode)
        {
            Rect screenRect = new Rect(
                selectionRect.x,
                Screen.height - selectionRect.y - selectionRect.height,
                selectionRect.width,
                selectionRect.height
            );
            
            GUI.color = selectionBoxColor;
            GUI.DrawTexture(screenRect, Texture2D.whiteTexture);
            
            GUI.color = selectionBorderColor;
            DrawRectBorder(screenRect, 2);
        }
        
        GUI.color = Color.white;
        
        if (pathVisualizationMode)
        {
            GUI.Label(new Rect(10, 10, 500, 30), "PATH VISUALIZATION MODE");
            GUI.Label(new Rect(10, 30, 500, 30), "Algorithm: " + (useRRT ? "RRT" : "A*"));
            GUI.Label(new Rect(10, 50, 500, 30), "Left Click: Set start point, then end point");
            GUI.Label(new Rect(10, 70, 500, 30), "Right Click: Clear and reset");
            GUI.Label(new Rect(10, 90, 500, 30), "Press V: Exit visualization mode");
            GUI.Label(new Rect(10, 110, 500, 30), "Press R: Toggle RRT/A*");
            GUI.Label(new Rect(10, 130, 500, 30), "Press C: Clear current path");
            
            if (pathVisStart.HasValue)
            {
                GUI.Label(new Rect(10, 160, 500, 30), "Start point set - click to set end point");
            }
            
            if (visualizedPath != null && visualizedPath.Length > 0)
            {
                float displayLength = 0;
                if (visualizedPath.Length > 1)
                {
                    for (int i = 1; i < visualizedPath.Length; i++)
                    {
                        displayLength += Vector3.Distance(visualizedPath[i - 1], visualizedPath[i]);
                    }
                }
                
                GUI.Label(new Rect(10, 180, 500, 30), "Path: " + visualizedPath.Length + " waypoints, " + 
                         displayLength.ToString("F2") + " units");
            }
        }
        else
        {
            string modeText = "";
            switch (movementMode)
            {
                case MovementMode.AStar:
                    modeText = "A* Only";
                    break;
                case MovementMode.AStarPF:
                    modeText = "A* + Potential Fields";
                    break;
                case MovementMode.PotentialFieldsOnly:
                    modeText = "Potential Fields Only";
                    break;
            }
            GUI.Label(new Rect(10, 10, 400, 30), "Movement Mode: " + modeText);
            GUI.Label(new Rect(10, 30, 400, 30), "Pathfinding: " + (useRRT ? "RRT" : "A*"));
            GUI.Label(new Rect(10, 50, 400, 30), "Press 1: A* | 2: A*+PF | 3: PF Only | R: Toggle RRT | V: Path Viz");
            GUI.Label(new Rect(10, 70, 400, 30), "RClick entity to follow | Ctrl+RClick to intercept");
            GUI.Label(new Rect(10, 90, 400, 30), "Selected Units: " + selectedUnits.Count);
        }
    }
    
    void DrawRectBorder(Rect rect, int thickness)
    {
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
    }
    
    void OnDrawGizmos()
    {
        if (!pathVisualizationMode && showFormationGizmos && selectedUnits.Count > 1)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                Vector3 destination = hit.point;
                int unitCount = selectedUnits.Count;
                
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                
                for (int i = 0; i < unitCount; i++)
                {
                    Vector3 unitDestination;
                    
                    if (unitCount == 2)
                    {
                        unitDestination = destination + new Vector3((i == 0 ? -formationSpacing : formationSpacing), 0, 0);
                    }
                    else if (unitCount <= 5)
                    {
                        float angle = (360f / unitCount) * i * Mathf.Deg2Rad;
                        Vector3 offset = new Vector3(
                            Mathf.Cos(angle) * formationSpacing,
                            0,
                            Mathf.Sin(angle) * formationSpacing
                        );
                        unitDestination = destination + offset;
                    }
                    else
                    {
                        int ring = i / 6;
                        int posInRing = i % 6;
                        float ringRadius = formationSpacing * (ring + 1);
                        float angle = (360f / 6) * posInRing * Mathf.Deg2Rad;
                        Vector3 offset = new Vector3(
                            Mathf.Cos(angle) * ringRadius,
                            0,
                            Mathf.Sin(angle) * ringRadius
                        );
                        unitDestination = destination + offset;
                    }
                    
                    Gizmos.DrawWireSphere(unitDestination, 0.5f);
                    Gizmos.DrawLine(destination, unitDestination);
                }
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(destination, 0.3f);
            }
        }
        
        if (pathVisualizationMode)
        {
            if (pathVisStart.HasValue)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(pathVisStart.Value, pathPointSize);
                Gizmos.DrawWireSphere(pathVisStart.Value, pathPointSize * 2f);
            }
            
            if (pathVisEnd.HasValue)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(pathVisEnd.Value, pathPointSize);
                Gizmos.DrawWireSphere(pathVisEnd.Value, pathPointSize * 2f);
            }
            
            if (visualizedPath != null)
            {
                Gizmos.color = pathVisualizationColor;
                foreach (Vector3 waypoint in visualizedPath)
                {
                    Gizmos.DrawSphere(waypoint, pathPointSize * 0.7f);
                }
            }
        }
    }
}