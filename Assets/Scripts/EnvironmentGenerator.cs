using UnityEngine;

public class EnvironmentGenerator : MonoBehaviour
{
    [Header("Obstacle Prefabs")]
    public GameObject cubePrefab;
    public GameObject spherePrefab;

    [Header("Ground / spawn area")]
    public Transform groundTransform; // assign GroundPlane here
    public Vector2 spawnAreaSize = new Vector2(40f, 40f); // X (width), Z (depth)
    public Vector2 spawnAreaCenterOffset = Vector2.zero; // optional offset on ground


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
                obstacleCount = 20; circular = true; break;
            case GameState.Preset.RandomCircles30:
                obstacleCount = 30; circular = true; break;
            case GameState.Preset.RandomCircles100:
                obstacleCount = 100; circular = true; break;
            case GameState.Preset.RandomRects20:
                obstacleCount = 20; circular = false; break;
            case GameState.Preset.RandomRects30:
                obstacleCount = 30; circular = false; break;
            case GameState.Preset.RandomRects100:
                obstacleCount = 100; circular = false; break;
            default:
                obstacleCount = 25; break;
        }

        for (int i = 0; i < obstacleCount; i++)
        {
            GameObject prefab = circular ? spherePrefab : cubePrefab;
            Vector3 pos = GetRandomPositionOnGround();
            GameObject obj = Instantiate(prefab, pos, Quaternion.identity, obstacleParent.transform);

            // random size
            float s = Random.Range(1f, 3f);
            obj.transform.localScale = new Vector3(s, s, s);

            // Optional: ensure the object sits on the plane exactly (if prefab pivot isn't at bottom)
            // Raycast down to plane and set y if needed (not necessary if using ground Y)
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

        // sit slightly above ground so colliders detect (use ground Y + half obstacle height / pivot)
        float y = (groundTransform != null) ? groundTransform.position.y + 0.5f : 0.5f;

        return new Vector3(x, y, z);
    }

    Vector3 GetRandomPosition()
    {
        return new Vector3(
            Random.Range(-20f, 20f),
            0.5f,
            Random.Range(-20f, 20f)
        );
    }

    public void ClearEnvironment()
    {
        if (obstacleParent != null)
            Destroy(obstacleParent);
    }
}
