using UnityEngine;

public class EnvironmentGenerator : MonoBehaviour
{
    [Header("Obstacle Prefabs")]
    public GameObject cubePrefab;
    public GameObject spherePrefab;

    private GameObject obstacleParent;

    public void GenerateEnvironment(GameState.Preset preset)
    {
        if (obstacleParent != null)
            Destroy(obstacleParent);

        obstacleParent = new GameObject("Obstacles");

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
            GameObject obj = Instantiate(prefab, GetRandomPosition(), Quaternion.identity);
            obj.transform.localScale = Vector3.one * Random.Range(1f, 3f);
            obj.transform.parent = obstacleParent.transform;
        }


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
