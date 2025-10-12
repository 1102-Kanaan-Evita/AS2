using UnityEngine;

public class SetupChecker : MonoBehaviour
{
    void Start()
    {
        CheckSetup();
    }
    
    [ContextMenu("Check Setup")]
    void CheckSetup()
    {
        Debug.Log("=== SETUP CHECKER ===");
        
        // Check layers exist
        CheckLayer("Selectable");
        CheckLayer("Obstacle");
        
        // Check units
        EntityUnit[] units = FindObjectsOfType<EntityUnit>();
        Debug.Log("Found " + units.Length + " EntityUnits");
        
        foreach (EntityUnit unit in units)
        {
            string layerName = LayerMask.LayerToName(unit.gameObject.layer);
            
            if (layerName == "Obstacle")
            {
                Debug.LogError("⚠️ " + unit.gameObject.name + " is on OBSTACLE layer! Should be on Selectable!", unit);
            }
            else if (layerName == "Selectable")
            {
                Debug.Log("✓ " + unit.gameObject.name + " is correctly on Selectable layer");
            }
            else
            {
                Debug.LogWarning("⚠️ " + unit.gameObject.name + " is on '" + layerName + "' layer. Should be on Selectable!", unit);
            }
            
            // Check collider
            if (unit.GetComponent<Collider>() == null)
            {
                Debug.LogError("⚠️ " + unit.gameObject.name + " has NO COLLIDER!", unit);
            }
            
            // Check for multiple LineRenderers
            LineRenderer[] lineRenderers = unit.GetComponents<LineRenderer>();
            if (lineRenderers.Length > 1)
            {
                Debug.LogError("⚠️ " + unit.gameObject.name + " has " + lineRenderers.Length + " LineRenderers! Should only have 1!", unit);
            }
            
            // Check layer masks
            Debug.Log("  - Obstacle Layer Mask: " + unit.obstacleLayer.value);
            Debug.Log("  - Entity Layer Mask: " + unit.entityLayer.value);
        }
        
        // Check GridMaker
        GridMaker grid = FindObjectOfType<GridMaker>();
        if (grid != null)
        {
            Debug.Log("✓ GridMaker found");
            Debug.Log("  - Unwalkable Mask: " + grid.unwalkable.value);
            Debug.Log("  - Node Radius: " + grid.nodeRadius);
            
            // Check if Selectable is in unwalkable mask
            if (IsLayerInMask(LayerMask.NameToLayer("Selectable"), grid.unwalkable))
            {
                Debug.LogError("⚠️ GridMaker has 'Selectable' in unwalkable mask! Units will be treated as obstacles!", grid);
            }
        }
        
        Debug.Log("=== END SETUP CHECK ===");
    }
    
    [ContextMenu("Fix Duplicate LineRenderers")]
    void FixDuplicateLineRenderers()
    {
        EntityUnit[] units = FindObjectsOfType<EntityUnit>();
        int fixedCount = 0;
        
        foreach (EntityUnit unit in units)
        {
            LineRenderer[] lineRenderers = unit.GetComponents<LineRenderer>();
            if (lineRenderers.Length > 1)
            {
                Debug.Log("Fixing " + unit.gameObject.name + " - removing " + (lineRenderers.Length - 1) + " extra LineRenderers");
                for (int i = 1; i < lineRenderers.Length; i++)
                {
                    DestroyImmediate(lineRenderers[i]);
                }
                fixedCount++;
            }
        }
        
        if (fixedCount > 0)
        {
            Debug.Log("✓ Fixed " + fixedCount + " units with duplicate LineRenderers");
        }
        else
        {
            Debug.Log("✓ No duplicate LineRenderers found");
        }
    }
    
    void CheckLayer(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer == -1)
        {
            Debug.LogError("⚠️ Layer '" + layerName + "' does not exist! Create it in Project Settings > Tags and Layers");
        }
        else
        {
            Debug.Log("✓ Layer '" + layerName + "' exists (index: " + layer + ")");
        }
    }
    
    bool IsLayerInMask(int layer, LayerMask mask)
    {
        return mask == (mask | (1 << layer));
    }
}