using UnityEngine;
using System.Linq;

public class EnvironmentGenerator : MonoBehaviour
{
    [Header("Obstacle Prefabs")]
    public GameObject cubePrefab;
    public GameObject spherePrefab;
    public GameObject wallPrefab; // Assign a thin, tall cube prefab

    [Header("Fixed Environment Prefabs")]
    public GameObject aStarShowcasePrefab; // Drag your built A* H-layout here
    public GameObject officePrefab; // Drag your built Office layout here

    [Header("Ground / spawn area")]
    public Transform groundTransform; // assign GroundPlane here
    public Vector2 spawnAreaSize = new Vector2(40f, 40f); // X (width), Z (depth)
    public Vector2 spawnAreaCenterOffset = Vector2.zero; // optional offset on ground

    [Header("Start and Target Areas")]
    public Transform startAreaMarker; // Position at (-16, 0, -9.5) or similar
    public Transform targetAreaMarker; // Position at (16, 0, 13) or similar

    [Header("Wall Settings")]
    public float wallHeight = 4f;
    public float wallThickness = 1.5f; // Thicker to prevent units squeezing through

    [Header("Unit Spawning")]
    public GameObject unitPrefab;

    private GameObject obstacleParent;
    private GameObject unitParent;

    public void GenerateEnvironment(GameState.Preset preset)
    {
        // First, always hide the default zone markers
        HideDefaultZoneMarkers();
        
        if (obstacleParent != null)
            Destroy(obstacleParent);
        
        if (unitParent != null)
            Destroy(unitParent);

        obstacleParent = new GameObject("Obstacles");
        obstacleParent.transform.parent = this.transform;
        
        unitParent = new GameObject("Units");
        unitParent.transform.parent = this.transform;

        int obstacleCount = 20;
        bool circular = false;
        
        GridMaker gridMaker = FindObjectOfType<GridMaker>();

        switch (preset)
        {
            case GameState.Preset.RandomCircles20:
                obstacleCount = 20; circular = true;
                ShowDefaultZoneMarkers();
                GenerateRandomObstacles(obstacleCount, circular, gridMaker);
                break;
            case GameState.Preset.RandomCircles30:
                obstacleCount = 30; circular = true;
                ShowDefaultZoneMarkers();
                GenerateRandomObstacles(obstacleCount, circular, gridMaker);
                break;
            case GameState.Preset.RandomCircles100:
                obstacleCount = 100; circular = true;
                ShowDefaultZoneMarkers();
                GenerateRandomObstacles(obstacleCount, circular, gridMaker);
                break;
            case GameState.Preset.RandomRects20:
                obstacleCount = 20; circular = false;
                ShowDefaultZoneMarkers();
                GenerateRandomObstacles(obstacleCount, circular, gridMaker);
                break;
            case GameState.Preset.RandomRects30:
                obstacleCount = 30; circular = false;
                ShowDefaultZoneMarkers();
                GenerateRandomObstacles(obstacleCount, circular, gridMaker);
                break;
            case GameState.Preset.RandomRects100:
                obstacleCount = 100; circular = false;
                ShowDefaultZoneMarkers();
                GenerateRandomObstacles(obstacleCount, circular, gridMaker);
                break;
            case GameState.Preset.AStarShowcase:
                if (gridMaker != null)
                {
                    gridMaker.obstacleCheckMultiplier = 1.0f;
                }
                
                // Hide default zone markers
                HideDefaultZoneMarkers();
                
                if (aStarShowcasePrefab != null)
                {
                    GameObject layout = Instantiate(aStarShowcasePrefab, obstacleParent.transform);
                    
                    // Hide any zone visuals in the prefab
                    HideZoneVisualsInPrefab(layout);
                    
                    // Check if prefab has its own markers
                    Transform prefabStart = layout.GetComponentsInChildren<Transform>(true)
                     .FirstOrDefault(t => t.name == "StartMarker");

                    Transform prefabTarget = layout.transform.Find("TargetMarker");
                    
                    if (prefabStart != null && prefabTarget != null)
                    {
                        // Use prefab's markers temporarily for spawning
                        Transform originalStart = startAreaMarker;
                        Transform originalTarget = targetAreaMarker;
                        
                        startAreaMarker = prefabStart;
                        targetAreaMarker = prefabTarget;
                        
                        SpawnUnits();
                        
                        // Restore original markers
                        startAreaMarker = originalStart;
                        targetAreaMarker = originalTarget;
                    }
                    else
                    {
                        // No custom markers, use defaults and spawn units
                        SpawnUnits();
                    }
                    
                    Debug.Log("A* Showcase environment instantiated from prefab");
                }
                else
                {
                    Debug.LogError("A* Showcase Prefab not assigned!");
                }
                break;
            case GameState.Preset.Office:
                if (gridMaker != null)
                {
                    gridMaker.obstacleCheckMultiplier = 1.0f;
                }
                
                // Hide default zone markers
                HideDefaultZoneMarkers();
                
                if (officePrefab != null)
                {
                    GameObject layout = Instantiate(officePrefab, obstacleParent.transform);
                    
                    // Hide any zone visuals in the prefab
                    HideZoneVisualsInPrefab(layout);
                    
                    // Check if prefab has its own markers
                    Transform prefabStart = layout.transform.Find("StartMarker");
                    Transform prefabTarget = layout.transform.Find("TargetMarker");
                    
                    if (prefabStart != null && prefabTarget != null)
                    {
                        // Use prefab's markers temporarily for spawning
                        Transform originalStart = startAreaMarker;
                        Transform originalTarget = targetAreaMarker;
                        
                        startAreaMarker = prefabStart;
                        targetAreaMarker = prefabTarget;
                        
                        SpawnUnits();
                        
                        // Restore original markers
                        startAreaMarker = originalStart;
                        targetAreaMarker = originalTarget;
                    }
                    else
                    {
                        // No custom markers, use defaults and spawn units
                        SpawnUnits();
                    }
                    
                    Debug.Log("Office environment instantiated from prefab");
                }
                else
                {
                    Debug.LogError("Office Prefab not assigned!");
                }
                break;
            default:
                obstacleCount = 25;
                ShowDefaultZoneMarkers(); // Show for random levels
                GenerateRandomObstacles(obstacleCount, false, gridMaker);
                break;
        }
        
        if (preset != GameState.Preset.AStarShowcase && preset != GameState.Preset.Office)
        {
            SpawnUnits();
        }
    }

    void GenerateRandomObstacles(int obstacleCount, bool circular, GridMaker gridMaker)
    {
        // Set larger check radius for random obstacles
        if (gridMaker != null)
        {
            gridMaker.obstacleCheckMultiplier = 3.0f;
        }
        
        for (int i = 0; i < obstacleCount; i++)
        {
            GameObject prefab = circular ? spherePrefab : cubePrefab;
            Vector3 pos = GetRandomPositionOnGround();
            GameObject obj = Instantiate(prefab, pos, Quaternion.identity, obstacleParent.transform);

            // Wider range of sizes now safe with multiplier
            float s = Random.Range(0.8f, 2.5f);
            obj.transform.localScale = new Vector3(s, s, s);
        }
    }

    Vector3 GetRandomPositionOnGround()
    {
        // ground center and size
        Vector3 basePos = groundTransform != null ? groundTransform.position : Vector3.zero;
        float halfX = spawnAreaSize.x * 0.5f;
        float halfZ = spawnAreaSize.y * 0.5f;

        Vector3 position = Vector3.zero;
        int attempts = 0;
        int maxAttempts = 50;
        
        // Keep trying until we find a position outside start/target zones
        do
        {
            float x = Random.Range(basePos.x - halfX + spawnAreaCenterOffset.x, basePos.x + halfX + spawnAreaCenterOffset.x);
            float z = Random.Range(basePos.z - halfZ + spawnAreaCenterOffset.y, basePos.z + halfZ + spawnAreaCenterOffset.y);
            float y = (groundTransform != null) ? groundTransform.position.y + 0.5f : 0.5f;
            
            position = new Vector3(x, y, z);
            attempts++;
            
        } while (IsInExclusionZone(position) && attempts < maxAttempts);

        return position;
    }
    
    bool IsInExclusionZone(Vector3 position)
    {
        float exclusionRadius = 5f; // Adjust this to control the size of cleared zones
        
        // Check if too close to start area
        if (startAreaMarker != null)
        {
            float distToStart = Vector3.Distance(new Vector3(position.x, 0, position.z), 
                                                 new Vector3(startAreaMarker.position.x, 0, startAreaMarker.position.z));
            if (distToStart < exclusionRadius)
                return true;
        }
        
        // Check if too close to target area
        if (targetAreaMarker != null)
        {
            float distToTarget = Vector3.Distance(new Vector3(position.x, 0, position.z), 
                                                  new Vector3(targetAreaMarker.position.x, 0, targetAreaMarker.position.z));
            if (distToTarget < exclusionRadius)
                return true;
        }
        
        return false;
    }

    public void ClearEnvironment()
    {
        if (obstacleParent != null)
            Destroy(obstacleParent);
        
        if (unitParent != null)
            Destroy(unitParent);
    }
    
    void SpawnUnits()
    {
        Debug.Log("Spawning units at: " + (startAreaMarker != null ? startAreaMarker.position.ToString() : "null"));

        // Get unit count from GameState
        int unitsToSpawn = GameState.UnitCount;
        
        if (unitPrefab == null || unitsToSpawn <= 0)
        {
            Debug.Log("No units to spawn (unitPrefab not assigned or UnitCount = 0)");
            return;
        }
        
        if (startAreaMarker == null)
        {
            Debug.LogWarning("Cannot spawn units - startAreaMarker not assigned!");
            return;
        }
        
        Debug.Log("Spawning " + unitsToSpawn + " unit(s)...");
        
        Vector3 center = startAreaMarker.position;
        
        if (unitsToSpawn == 1)
        {
            // Spawn single unit at start marker position
            GameObject unit = Instantiate(unitPrefab, center, Quaternion.identity, unitParent.transform);
            unit.name = "Unit_1";
            Debug.Log("Spawned 1 unit at " + center);
        }
        else
        {
            // Spawn multiple units in a circular or grid formation
            float spacing = 1.5f;
            
            if (unitsToSpawn <= 5)
            {
                // Small group: circular formation
                for (int i = 0; i < unitsToSpawn; i++)
                {
                    Vector3 offset = Vector3.zero;
                    
                    if (unitsToSpawn == 2)
                    {
                        offset = new Vector3((i - 0.5f) * spacing, 0, 0);
                    }
                    else if (unitsToSpawn == 3)
                    {
                        float angle = i * 120f * Mathf.Deg2Rad;
                        offset = new Vector3(Mathf.Cos(angle) * spacing, 0, Mathf.Sin(angle) * spacing);
                    }
                    else if (unitsToSpawn == 4)
                    {
                        offset = new Vector3((i % 2 == 0 ? -1 : 1) * spacing * 0.5f, 0, (i < 2 ? -1 : 1) * spacing * 0.5f);
                    }
                    else // 5
                    {
                        if (i == 0)
                            offset = Vector3.zero;
                        else
                        {
                            float angle = (i - 1) * 90f * Mathf.Deg2Rad;
                            offset = new Vector3(Mathf.Cos(angle) * spacing, 0, Mathf.Sin(angle) * spacing);
                        }
                    }
                    
                    Vector3 spawnPos = center + offset;
                    GameObject unit = Instantiate(unitPrefab, spawnPos, Quaternion.identity, unitParent.transform);
                    unit.name = "Unit_" + (i + 1);
                }
            }
            else
            {
                // Large group: grid formation
                int columns = Mathf.CeilToInt(Mathf.Sqrt(unitsToSpawn));
                int rows = Mathf.CeilToInt((float)unitsToSpawn / columns);
                
                float totalWidth = (columns - 1) * spacing;
                float totalDepth = (rows - 1) * spacing;
                Vector3 startOffset = new Vector3(-totalWidth / 2f, 0, -totalDepth / 2f);
                
                int unitIndex = 0;
                for (int row = 0; row < rows && unitIndex < unitsToSpawn; row++)
                {
                    for (int col = 0; col < columns && unitIndex < unitsToSpawn; col++)
                    {
                        Vector3 offset = startOffset + new Vector3(col * spacing, 0, row * spacing);
                        Vector3 spawnPos = center + offset;
                        GameObject unit = Instantiate(unitPrefab, spawnPos, Quaternion.identity, unitParent.transform);
                        unit.name = "Unit_" + (unitIndex + 1);
                        unitIndex++;
                    }
                }
            }
            
            Debug.Log("Spawned " + unitsToSpawn + " units in formation at " + center);
        }
    }
    
    void HideDefaultZoneMarkers()
    {
        if (startAreaMarker != null)
        {
            ZoneVisualizer viz = startAreaMarker.GetComponent<ZoneVisualizer>();
            if (viz != null) viz.enabled = false;
        }
        
        if (targetAreaMarker != null)
        {
            ZoneVisualizer viz = targetAreaMarker.GetComponent<ZoneVisualizer>();
            if (viz != null) viz.enabled = false;
        }
    }
    
    void ShowDefaultZoneMarkers()
    {
        if (startAreaMarker != null)
        {
            ZoneVisualizer viz = startAreaMarker.GetComponent<ZoneVisualizer>();
            if (viz != null) viz.enabled = true;
        }
        
        if (targetAreaMarker != null)
        {
            ZoneVisualizer viz = targetAreaMarker.GetComponent<ZoneVisualizer>();
            if (viz != null) viz.enabled = true;
        }
    }
    
    void HideZoneVisualsInPrefab(GameObject prefabInstance)
    {
        // Don't hide anything in the prefab - we want those zones visible
        // This function is not needed anymore
    }
}