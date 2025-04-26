using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TreeData
{
    public GameObject prefab; // The tree prefab
    public float minHeight = 1f; // Minimum height for this tree type
    public float maxHeight = 3f; // Maximum height for this tree type
}

public class MeshGenerator : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;
    
    public int xSize = 200;
    public int zSize = 200;
    
    private MeshCollider meshCollider;

    public Texture2D heightMapTexture;
    public float maxHeight = 2f;
    public float steepnessFactor = 1.0f;
    [SerializeField] private bool generateInEditor = true;
    private bool isInitialized = false;

    public TreeData[] treeData = new TreeData[10];
    public int numberOfGroups = 10;
    public int treesPerGroup = 7;
    public float groupRadius = 20f;
    public float groupSpacing = 25f;
    public LayerMask terrainLayer;
    public float maxSlopeAngle = 17f;

    private List<GameObject> spawnedTrees = new List<GameObject>();

    void Start()
    {
        InitializeMesh();
        GenerateMeshAndTrees();
        UpdateMesh();
        isInitialized = true;
    }

    void InitializeMesh()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        
        meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
    }

    void GenerateMeshAndTrees()
    {
        if (heightMapTexture == null)
        {
            Debug.LogError("Please assign a height map texture in the Inspector!");
            return;
        }

        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float xNorm = (float)x / xSize;
                float zNorm = (float)z / zSize;
                float baseHeight = SampleHeightFromTexture(xNorm, zNorm);
                float adjustedHeight = Mathf.Pow(baseHeight, steepnessFactor);
                float y = adjustedHeight * maxHeight;
                vertices[i] = new Vector3(x, y, z);
                i++;
            }
        }

        triangles = new int[xSize * zSize * 6];
        int vert = 0;
        int tris = 0;

        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }

        UpdateMesh();

        if (treeData != null && treeData.Length > 0)
        {
            bool hasValidPrefab = false;
            foreach (TreeData data in treeData)
            {
                if (data != null && data.prefab != null)
                {
                    hasValidPrefab = true;
                    break;
                }
            }
            if (hasValidPrefab)
            {
                SpawnTreesOnMesh();
            }
        }
    }

    float SampleHeightFromTexture(float x, float z)
    {
        x = Mathf.Clamp01(x);
        z = Mathf.Clamp01(z);
        Color pixelColor = heightMapTexture.GetPixelBilinear(x, z);
        return pixelColor.grayscale;
    }

    void UpdateMesh()
    {
        if (mesh == null || vertices == null || triangles == null) return;

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        UpdateMeshCollider();
    }

    void UpdateMeshCollider()
    {
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }
    }

    void ClearSpawnedTrees()
    {
        foreach (GameObject tree in spawnedTrees)
        {
            if (tree != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(tree);
                }
                else
                {
                    DestroyImmediate(tree);
                }
            }
        }
        spawnedTrees.Clear();
    }

    void SpawnTreesOnMesh()
    {
        ClearSpawnedTrees();

        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        Vector3 terrainPos = transform.position;
        Vector3 terrainScale = transform.localScale;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (Vector3 vertex in vertices)
        {
            Vector3 worldVertex = terrainPos + Vector3.Scale(vertex, terrainScale);
            minX = Mathf.Min(minX, worldVertex.x);
            maxX = Mathf.Max(maxX, worldVertex.x);
            minZ = Mathf.Min(minZ, worldVertex.z);
            maxZ = Mathf.Max(maxZ, worldVertex.z);
            minY = Mathf.Min(minY, worldVertex.y);
            maxY = Mathf.Max(maxY, worldVertex.y);
        }

        for (int i = 0; i < numberOfGroups; i++)
        {
            float randomX = Random.Range(minX + groupRadius, maxX - groupRadius);
            float randomZ = Random.Range(minZ + groupRadius, maxZ - groupRadius);
            Vector3 groupCenter = new Vector3(randomX, maxY + 500f, randomZ);

            groupCenter.y = SampleMeshHeight(groupCenter, out Vector3 groupNormal);
            if (groupCenter.y == float.MinValue)
            {
                groupCenter.y = terrainPos.y;
            }

            float groupAngle = Vector3.Angle(groupNormal, Vector3.up);
            if (groupAngle > maxSlopeAngle)
            {
                continue;
            }

            for (int k = 0; k < treesPerGroup; k++)
            {
                Vector2 treeOffset = Random.insideUnitCircle * groupRadius;
                Vector3 spawnPosition = groupCenter + new Vector3(treeOffset.x, 0f, treeOffset.y);

                spawnPosition.x = Mathf.Clamp(spawnPosition.x, minX, maxX);
                spawnPosition.z = Mathf.Clamp(spawnPosition.z, minZ, maxZ);
                spawnPosition.y = maxY + 500f;

                spawnPosition.y = SampleMeshHeight(spawnPosition, out Vector3 treeNormal);
                if (spawnPosition.y == float.MinValue)
                {
                    spawnPosition.y = groupCenter.y;
                }

                float treeAngle = Vector3.Angle(treeNormal, Vector3.up);
                if (treeAngle > maxSlopeAngle)
                {
                    continue;
                }

                TreeData selectedData = null;
                List<TreeData> validTreeData = new List<TreeData>();
                foreach (TreeData data in treeData)
                {
                    if (data != null && data.prefab != null)
                    {
                        validTreeData.Add(data);
                    }
                }
                if (validTreeData.Count > 0)
                {
                    selectedData = validTreeData[Random.Range(0, validTreeData.Count)];
                }
                if (selectedData == null)
                {
                    continue;
                }

                float randomHeight = Random.Range(selectedData.minHeight, selectedData.maxHeight);
                GameObject spawnedTree = Instantiate(selectedData.prefab, spawnPosition, Quaternion.identity);
                Vector3 scale = spawnedTree.transform.localScale;
                scale.y = randomHeight;
                spawnedTree.transform.localScale = scale;

                MeshRenderer treeRenderer = spawnedTree.GetComponent<MeshRenderer>();
                if (treeRenderer != null)
                {
                    treeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                    treeRenderer.receiveShadows = true;
                }

                spawnedTrees.Add(spawnedTree);
            }
        }
    }

    // Public method to sample mesh height (for player controller)
    public float GetMeshHeight(Vector3 position, out Vector3 normal)
    {
        return SampleMeshHeight(position, out normal);
    }

    private float SampleMeshHeight(Vector3 position, out Vector3 normal)
    {
        Ray ray = new Ray(position, Vector3.down);
        RaycastHit hit;

        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider != null && Physics.Raycast(ray, out hit, 1000f, terrainLayer))
        {
            if (hit.collider == meshCollider)
            {
                normal = hit.normal;
                return hit.point.y;
            }
        }

        normal = Vector3.up;
        return float.MinValue;
    }

    private void OnValidate()
    {
        if (!generateInEditor || !isInitialized) return;

        StartCoroutine(DelayedGenerateMeshAndTrees());
    }

    private IEnumerator DelayedGenerateMeshAndTrees()
    {
        yield return null;
        InitializeMesh();
        GenerateMeshAndTrees();
        UpdateMesh();
    }
}