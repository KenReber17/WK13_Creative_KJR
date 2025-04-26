using UnityEngine;

public class RandomTerrainGenerator : MonoBehaviour
{
    // Terrain settings
    [Header("Terrain Settings")]
    public float maxHeight = 100f;          // Maximum height of the terrain
    public float plateauHeight = 50f;       // Height of plateaus
    public float plateauDiameter = 20f;     // Diameter of plateaus (controls flat area size)
    public float noiseScale = 0.1f;         // Scale of Perlin noise for terrain variation

    // Tree settings for Tree Type 1
    [Header("Tree Type 1 Settings")]
    public GameObject treePrefab1;          // First tree prefab
    public int treeCount1 = 20;             // Number of trees for Type 1
    public float treeSpread1 = 0.9f;        // Spread factor for Type 1 (0-1)

    // Tree settings for Tree Type 2
    [Header("Tree Type 2 Settings")]
    public GameObject treePrefab2;          // Second tree prefab
    public int treeCount2 = 15;             // Number of trees for Type 2
    public float treeSpread2 = 0.7f;        // Spread factor for Type 2 (0-1)

    // Tree settings for Tree Type 3
    [Header("Tree Type 3 Settings")]
    public GameObject treePrefab3;          // Third tree prefab
    public int treeCount3 = 10;             // Number of trees for Type 3
    public float treeSpread3 = 0.5f;        // Spread factor for Type 3 (0-1)

    private Terrain terrain;
    private float[,] heightMap;

    void Start()
    {
        // Get the Terrain component
        terrain = GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("No Terrain component found on this GameObject!");
            return;
        }

        // Force terrain position to Y = 0
        Vector3 terrainPos = terrain.transform.position;
        terrainPos.y = 0f;
        terrain.transform.position = terrainPos;
        Debug.Log("Terrain position set to: " + terrain.transform.position);

        // Generate terrain and trees
        GenerateTerrain();
        GenerateTrees();
    }

    void GenerateTerrain()
    {
        TerrainData terrainData = terrain.terrainData;
        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;
        heightMap = new float[width, height];

        // Generate base terrain with Perlin noise
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float normalizedX = (float)x / width;
                float normalizedZ = (float)z / height;
                float noise = Mathf.PerlinNoise(normalizedX * noiseScale, normalizedZ * noiseScale);
                heightMap[x, z] = noise * maxHeight; // Absolute height in world units
            }
        }

        // Add plateaus
        AddPlateaus();

        // Apply heightmap to terrain
        terrainData.SetHeights(0, 0, heightMap);
    }

    void AddPlateaus()
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        int plateauCount = Random.Range(1, 5);
        for (int i = 0; i < plateauCount; i++)
        {
            int centerX = Random.Range(0, width);
            int centerZ = Random.Range(0, height);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    float distance = Vector2.Distance(new Vector2(x, z), new Vector2(centerX, centerZ));
                    if (distance < plateauDiameter / 2f)
                    {
                        float edgeFactor = Mathf.SmoothStep(1f, 0f, distance / (plateauDiameter / 2f));
                        heightMap[x, z] = Mathf.Lerp(heightMap[x, z], plateauHeight, edgeFactor);
                    }
                }
            }
        }
    }

    void GenerateTrees()
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainSize = terrainData.size;

        if (treePrefab1 != null)
        {
            for (int i = 0; i < treeCount1; i++)
            {
                float x = Random.Range(0.5f - treeSpread1 / 2f, 0.5f + treeSpread1 / 2f) * terrainSize.x;
                float z = Random.Range(0.5f - treeSpread1 / 2f, 0.5f + treeSpread1 / 2f) * terrainSize.z;
                float y = terrainData.GetHeight((int)(x / terrainSize.x * terrainData.heightmapResolution),
                                               (int)(z / terrainSize.z * terrainData.heightmapResolution));
                Vector3 position = new Vector3(x, y, z); // No terrain offset since Y = 0
                GameObject tree = Instantiate(treePrefab1, position, Quaternion.identity);
                tree.transform.parent = transform;
            }
        }
        else
        {
            Debug.LogWarning("Tree Prefab 1 not assigned!");
        }

        if (treePrefab2 != null)
        {
            for (int i = 0; i < treeCount2; i++)
            {
                float x = Random.Range(0.5f - treeSpread2 / 2f, 0.5f + treeSpread2 / 2f) * terrainSize.x;
                float z = Random.Range(0.5f - treeSpread2 / 2f, 0.5f + treeSpread2 / 2f) * terrainSize.z;
                float y = terrainData.GetHeight((int)(x / terrainSize.x * terrainData.heightmapResolution),
                                               (int)(z / terrainSize.z * terrainData.heightmapResolution));
                Vector3 position = new Vector3(x, y, z);
                GameObject tree = Instantiate(treePrefab2, position, Quaternion.identity);
                tree.transform.parent = transform;
            }
        }
        else
        {
            Debug.LogWarning("Tree Prefab 2 not assigned!");
        }

        if (treePrefab3 != null)
        {
            for (int i = 0; i < treeCount3; i++)
            {
                float x = Random.Range(0.5f - treeSpread3 / 2f, 0.5f + treeSpread3 / 2f) * terrainSize.x;
                float z = Random.Range(0.5f - treeSpread3 / 2f, 0.5f + treeSpread3 / 2f) * terrainSize.z;
                float y = terrainData.GetHeight((int)(x / terrainSize.x * terrainData.heightmapResolution),
                                               (int)(z / terrainSize.z * terrainData.heightmapResolution));
                Vector3 position = new Vector3(x, y, z);
                GameObject tree = Instantiate(treePrefab3, position, Quaternion.identity);
                tree.transform.parent = transform;
            }
        }
        else
        {
            Debug.LogWarning("Tree Prefab 3 not assigned!");
        }
    }

    [ContextMenu("Regenerate Terrain")]
    void Regenerate()
    {
        foreach (Transform child in transform)
        {
            if (child.gameObject != gameObject) DestroyImmediate(child.gameObject);
        }
        GenerateTerrain();
        GenerateTrees();
    }
}