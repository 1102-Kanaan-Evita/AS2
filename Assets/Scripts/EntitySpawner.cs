using UnityEngine;

public class EntitySpawner : MonoBehaviour
{
    public GameObject entityPrefab;
    public Transform startArea;
    public Vector3 areaSize = new Vector3(10, 0, 10);

    void Start()
    {
        string mode = PlayerPrefs.GetString("Mode", "AStar"); // from menu
        int entityCount = mode == "PF" ? 10 : 5;

        for (int i = 0; i < entityCount; i++)
        {
            Vector3 randomPos = GetRandomPositionInArea();
            Instantiate(entityPrefab, randomPos, Quaternion.Euler(0, Random.Range(0, 360f), 0));
        }
    }

    Vector3 GetRandomPositionInArea()
    {
        Vector3 center = startArea != null ? startArea.position : Vector3.zero;
        return new Vector3(
            center.x + Random.Range(-areaSize.x / 2, areaSize.x / 2),
            0,
            center.z + Random.Range(-areaSize.z / 2, areaSize.z / 2)
        );
    }
}
