/*
using UnityEngine;
using System.Collections.Generic;

public class UnitCommander : MonoBehaviour
{
    public LayerMask groundLayer;
    public LayerMask unitLayer;
    
    private List<Unit> selectedUnits = new List<Unit>();
    private Pathfinding pathfinding;

    // Visual selection
    private Vector3 dragStartPos;
    private bool isDragging = false;

    private void Start()
    {
        pathfinding = FindObjectOfType<Pathfinding>();
    }

    private void Update()
    {
        HandleUnitSelection();
        HandleMovementCommand();
    }

    void HandleUnitSelection()
    {
        // Left click to select units
        if (Input.GetMouseButtonDown(0))
        {
            dragStartPos = Input.mousePosition;
            isDragging = true;

            // If not holding shift, clear previous selection
            if (!Input.GetKey(KeyCode.LeftShift))
            {
                DeselectAll();
            }
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;

            // Check if it was a click or a drag
            if (Vector3.Distance(dragStartPos, Input.mousePosition) < 5f)
            {
                // Single click selection
                SelectUnitAtPoint(Input.mousePosition);
            }
            else
            {
                // Box selection
                SelectUnitsInBox(dragStartPos, Input.mousePosition);
            }
        }
    }

    void HandleMovementCommand()
    {
        // Right click to move selected units
        if (Input.GetMouseButtonDown(1) && selectedUnits.Count > 0)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
            {
                Vector3 targetPosition = hit.point;

                if (selectedUnits.Count == 1)
                {
                    // Single unit - move directly to target
                    selectedUnits[0].MoveTo(targetPosition);
                }
                else
                {
                    // Multiple units - create formation
                    MoveUnitsInFormation(targetPosition);
                }
            }
        }
    }

    void SelectUnitAtPoint(Vector3 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, unitLayer))
        {
            Unit unit = hit.collider.GetComponent<Unit>();
            if (unit != null)
            {
                SelectUnit(unit);
            }
        }
    }

    void SelectUnitsInBox(Vector3 startPos, Vector3 endPos)
    {
        // Find all units in the scene
        Unit[] allUnits = FindObjectsOfType<Unit>();

        foreach (Unit unit in allUnits)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(unit.transform.position);

            // Check if unit is within selection box
            if (IsWithinSelectionBounds(screenPos, startPos, endPos))
            {
                SelectUnit(unit);
            }
        }
    }

    bool IsWithinSelectionBounds(Vector3 pos, Vector3 start, Vector3 end)
    {
        float minX = Mathf.Min(start.x, end.x);
        float maxX = Mathf.Max(start.x, end.x);
        float minY = Mathf.Min(start.y, end.y);
        float maxY = Mathf.Max(start.y, end.y);

        return pos.x >= minX && pos.x <= maxX && pos.y >= minY && pos.y <= maxY;
    }

    void SelectUnit(Unit unit)
    {
        if (!selectedUnits.Contains(unit))
        {
            selectedUnits.Add(unit);
            // TODO: Add visual selection indicator
            Debug.Log("Selected unit: " + unit.name);
        }
    }

    void DeselectAll()
    {
        selectedUnits.Clear();
        Debug.Log("Deselected all units");
    }

    void MoveUnitsInFormation(Vector3 centerTarget)
    {
        if (selectedUnits.Count == 0) return;

        // Simple grid formation
        int columns = Mathf.CeilToInt(Mathf.Sqrt(selectedUnits.Count));
        float spacing = 2f; // Space between units

        for (int i = 0; i < selectedUnits.Count; i++)
        {
            int row = i / columns;
            int col = i % columns;

            // Calculate offset from center
            float offsetX = (col - columns / 2f) * spacing;
            float offsetZ = (row - Mathf.FloorToInt(selectedUnits.Count / (float)columns) / 2f) * spacing;

            Vector3 targetPos = centerTarget + new Vector3(offsetX, 0, offsetZ);
            selectedUnits[i].MoveTo(targetPos);
        }
    }

    // Alternative: Move units in a line
    public void MoveUnitsInLine(Vector3 startPos, Vector3 endPos)
    {
        if (selectedUnits.Count == 0) return;

        for (int i = 0; i < selectedUnits.Count; i++)
        {
            float t = selectedUnits.Count > 1 ? i / (float)(selectedUnits.Count - 1) : 0.5f;
            Vector3 targetPos = Vector3.Lerp(startPos, endPos, t);
            selectedUnits[i].MoveTo(targetPos);
        }
    }

    private void OnGUI()
    {
        // Draw selection box while dragging
        if (isDragging)
        {
            Rect rect = GetScreenRect(dragStartPos, Input.mousePosition);
            DrawScreenRect(rect, new Color(0.8f, 0.8f, 0.95f, 0.25f));
            DrawScreenRectBorder(rect, 2, new Color(0.8f, 0.8f, 0.95f));
        }

        // Display selected unit count
        if (selectedUnits.Count > 0)
        {
            GUI.Label(new Rect(10, 10, 200, 20), "Selected Units: " + selectedUnits.Count);
        }
    }

    // Helper methods for drawing selection box
    Rect GetScreenRect(Vector3 screenPos1, Vector3 screenPos2)
    {
        screenPos1.y = Screen.height - screenPos1.y;
        screenPos2.y = Screen.height - screenPos2.y;

        Vector3 topLeft = Vector3.Min(screenPos1, screenPos2);
        Vector3 bottomRight = Vector3.Max(screenPos1, screenPos2);

        return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
    }

    void DrawScreenRect(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawScreenRectBorder(Rect rect, int thickness, Color color)
    {
        DrawScreenRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
        DrawScreenRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
        DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
        DrawScreenRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
    }

    public List<Unit> GetSelectedUnits()
    {
        return selectedUnits;
    }
}
*/