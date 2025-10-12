using UnityEngine;
using System.Collections.Generic;

public enum MovementMode
{
    AStar,              // Pure A* pathfinding, no avoidance
    AStarPF,            // A* pathfinding + Potential Fields for avoidance
    PotentialFieldsOnly // No pathfinding, just move toward goal with forces
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
    
    [Header("Visual")]
    public bool showFormationGizmos = true;
    
    private Vector3 mouseDownPos;
    private bool isDragging = false;
    private List<EntityUnit> selectedUnits = new List<EntityUnit>();
    private Rect selectionRect;
    
    private List<Vector3> waypointChain = new List<Vector3>();
    private LineRenderer waypointLineRenderer;
    
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
    }
    
    void Update()
    {
        HandleSelection();
        HandleMovementCommands();
        HandleKeyCommands();
    }
    
    void HandleKeyCommands()
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
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            waypointChain.Clear();
            waypointLineRenderer.positionCount = 0;
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
                unit.SetWaypointChain(new List<Vector3>(waypointChain), movementMode);
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
                selectedUnits[0].FollowEntity(followTarget, movementMode, queueCommand);
            }
            else
            {
                selectedUnits[0].MoveTo(destination, movementMode, queueCommand);
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
                    selectedUnits[i].FollowEntity(followTarget, movementMode, queueCommand, unitDestination - destination);
                }
                else
                {
                    selectedUnits[i].MoveTo(unitDestination, movementMode, queueCommand);
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
        if (isDragging && Vector3.Distance(mouseDownPos, Input.mousePosition) > 10f)
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
        GUI.Label(new Rect(10, 30, 400, 30), "Press 1: A* | 2: A*+PF | 3: PF Only");
        GUI.Label(new Rect(10, 50, 400, 30), "RClick entity to follow | Ctrl+RClick to intercept");
        GUI.Label(new Rect(10, 70, 400, 30), "Selected Units: " + selectedUnits.Count);
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
        if (!showFormationGizmos || selectedUnits.Count <= 1) return;
        
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
}