using UnityEngine;

public class ZoneVisualizer : MonoBehaviour
{
    [Header("Zone Settings")]
    public float zoneRadius = 5f; // Should match exclusionRadius in EnvironmentGenerator
    public Color zoneColor = Color.green;
    [Range(0f, 1f)]
    public float transparency = 0.5f;
    public float zoneHeight = 0.15f;
    public int segments = 64; // Circle smoothness
    
    [Header("Optional: Add a marker")]
    public bool showMarker = true;
    public float markerHeight = 2f;
    public Color markerColor = Color.white;
    
    private GameObject zoneCircle;
    private GameObject marker;
    private Material zoneMaterial;
    
    void Start()
    {
        CreateZoneVisualization();
    }
    
    void Update()
    {
        // Continuously update color in case it changes
        if (zoneMaterial != null)
        {
            Color colorWithAlpha = zoneColor;
            colorWithAlpha.a = transparency;
            zoneMaterial.color = colorWithAlpha;
        }
    }
    
    void CreateZoneVisualization()
    {
        // Create the filled circle on the ground
        zoneCircle = new GameObject("ZoneCircle");
        zoneCircle.transform.parent = transform;
        zoneCircle.transform.localPosition = new Vector3(0, zoneHeight, 0);
        
        MeshFilter meshFilter = zoneCircle.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = zoneCircle.AddComponent<MeshRenderer>();
        
        meshFilter.mesh = CreateCircleMesh(zoneRadius, segments);
        
        // Create material with transparency
        zoneMaterial = new Material(Shader.Find("Sprites/Default"));
        Color colorWithAlpha = zoneColor;
        colorWithAlpha.a = transparency;
        zoneMaterial.color = colorWithAlpha;
        meshRenderer.material = zoneMaterial;
        
        // Create optional marker
        if (showMarker)
        {
            marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "ZoneMarker";
            marker.transform.parent = transform;
            marker.transform.localPosition = new Vector3(0, markerHeight / 2, 0);
            marker.transform.localScale = new Vector3(0.5f, markerHeight / 2, 0.5f);
            
            Material markerMat = new Material(Shader.Find("Standard"));
            markerMat.color = markerColor;
            markerMat.SetFloat("_Metallic", 0.3f);
            markerMat.SetFloat("_Glossiness", 0.6f);
            marker.GetComponent<Renderer>().material = markerMat;
            
            // Remove collider so it doesn't interfere
            Destroy(marker.GetComponent<Collider>());
        }
    }
    
    Mesh CreateCircleMesh(float radius, int segments)
    {
        Mesh mesh = new Mesh();
        
        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];
        Vector2[] uv = new Vector2[segments + 1];
        
        // Center vertex
        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);
        
        // Circle vertices
        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            
            vertices[i + 1] = new Vector3(x, 0, z);
            uv[i + 1] = new Vector2(x / radius * 0.5f + 0.5f, z / radius * 0.5f + 0.5f);
        }
        
        // Create triangles (both sides for visibility)
        int[] trianglesFront = new int[segments * 3];
        int[] trianglesBack = new int[segments * 3];
        
        for (int i = 0; i < segments; i++)
        {
            // Front face
            trianglesFront[i * 3] = 0;
            trianglesFront[i * 3 + 1] = i + 1;
            trianglesFront[i * 3 + 2] = (i + 1) % segments + 1;
            
            // Back face (reversed winding)
            trianglesBack[i * 3] = 0;
            trianglesBack[i * 3 + 1] = (i + 1) % segments + 1;
            trianglesBack[i * 3 + 2] = i + 1;
        }
        
        // Combine both faces
        triangles = new int[segments * 6];
        System.Array.Copy(trianglesFront, 0, triangles, 0, segments * 3);
        System.Array.Copy(trianglesBack, 0, triangles, segments * 3, segments * 3);
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    void OnDrawGizmos()
    {
        // Draw the zone radius in the editor
        Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.3f);
        
        // Draw filled circle in editor
        Vector3 center = transform.position + Vector3.up * 0.2f;
        int gizmoSegments = 32;
        float angleStep = 360f / gizmoSegments;
        
        for (int i = 0; i < gizmoSegments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            
            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1) * zoneRadius, 0, Mathf.Sin(angle1) * zoneRadius);
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2) * zoneRadius, 0, Mathf.Sin(angle2) * zoneRadius);
            
            Gizmos.DrawLine(center, point1);
            Gizmos.DrawLine(point1, point2);
        }
    }
}