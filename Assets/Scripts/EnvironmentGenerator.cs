using UnityEngine;

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

    private GameObject obstacleParent;

    public void GenerateEnvironment(GameState.Preset preset)
    {
        if (obstacleParent != null)
            Destroy(obstacleParent);

        obstacleParent = new GameObject("Obstacles");
        obstacleParent.transform.parent = this.transform;

        int obstacleCount = 20;
        bool circular = false;

        switch (preset)
        {
            case GameState.Preset.RandomCircles20:
                obstacleCount = 20; circular = true;
                GenerateRandomObstacles(obstacleCount, circular);
                break;
            case GameState.Preset.RandomCircles30:
                obstacleCount = 30; circular = true;
                GenerateRandomObstacles(obstacleCount, circular);
                break;
            case GameState.Preset.RandomCircles100:
                obstacleCount = 100; circular = true;
                GenerateRandomObstacles(obstacleCount, circular);
                break;
            case GameState.Preset.RandomRects20:
                obstacleCount = 20; circular = false;
                GenerateRandomObstacles(obstacleCount, circular);
                break;
            case GameState.Preset.RandomRects30:
                obstacleCount = 30; circular = false;
                GenerateRandomObstacles(obstacleCount, circular);
                break;
            case GameState.Preset.RandomRects100:
                obstacleCount = 100; circular = false;
                GenerateRandomObstacles(obstacleCount, circular);
                break;
            case GameState.Preset.AStarShowcase:
                if (aStarShowcasePrefab != null)
                {
                    GameObject layout = Instantiate(aStarShowcasePrefab, obstacleParent.transform);
                    Debug.Log("A* Showcase environment instantiated from prefab");
                }
                else
                {
                    Debug.LogError("A* Showcase Prefab not assigned!");
                }
                break;
            case GameState.Preset.Office:
                if (officePrefab != null)
                {
                    GameObject layout = Instantiate(officePrefab, obstacleParent.transform);
                    Debug.Log("Office environment instantiated from prefab");
                }
                else
                {
                    Debug.LogError("Office Prefab not assigned!");
                }
                break;
            default:
                obstacleCount = 25;
                GenerateRandomObstacles(obstacleCount, false);
                break;
        }
    }

    void GenerateRandomObstacles(int obstacleCount, bool circular)
    {
        for (int i = 0; i < obstacleCount; i++)
        {
            GameObject prefab = circular ? spherePrefab : cubePrefab;
            Vector3 pos = GetRandomPositionOnGround();
            GameObject obj = Instantiate(prefab, pos, Quaternion.identity, obstacleParent.transform);

            // Scale to match your node size better
            float s = Random.Range(0.5f, 1.5f); // Much smaller range, closer to node radius
            obj.transform.localScale = new Vector3(s, s, s);
        }
    }
    Vector3 GetRandomPositionOnGround()
    {
        // ground center and size
        Vector3 basePos = groundTransform != null ? groundTransform.position : Vector3.zero;
        float halfX = spawnAreaSize.x * 0.5f;
        float halfZ = spawnAreaSize.y * 0.5f;

        float x = Random.Range(basePos.x - halfX + spawnAreaCenterOffset.x, basePos.x + halfX + spawnAreaCenterOffset.x);
        float z = Random.Range(basePos.z - halfZ + spawnAreaCenterOffset.y, basePos.z + halfZ + spawnAreaCenterOffset.y);

        // sit slightly above ground so colliders detect
        float y = (groundTransform != null) ? groundTransform.position.y + 0.5f : 0.5f;

        return new Vector3(x, y, z);
    }

    public void ClearEnvironment()
    {
        if (obstacleParent != null)
            Destroy(obstacleParent);
    }
}