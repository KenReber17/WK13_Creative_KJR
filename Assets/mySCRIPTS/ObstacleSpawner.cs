using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    public Transform player;
    public GameObject groundSpawnPrefab;
    public GameObject skyFallSpawnPrefab;

    public float groundSpawnHeight = 1f;
    public float initialFollowSpeed = 2f;
    public float followSpeedIncrease = 0.05f;
    private float currentFollowSpeed;

    public float skySpawnHeight = 20f;
    public float fallSpeed = 5f;

    public float initialGroundSpawnRate = 2f;
    public float initialSkySpawnRate = 3f;
    public float minSpawnRate = 0.5f;
    public float groundSpawnRateIncrease = 0.1f;
    public float skySpawnRateIncrease = 0.05f;

    public Vector3 minSpawnBounds = new Vector3(-10f, 0f, -10f);
    public Vector3 maxSpawnBounds = new Vector3(10f, 20f, 10f);

    public GameObject yellowTrigger;
    public GameObject orangeTrigger;
    public GameObject redTrigger;

    private float currentGroundSpawnRate;
    private float currentSkySpawnRate;
    private float groundSpawnTimer;
    private float skySpawnTimer;
    private float timeElapsed;

    public LayerMask terrainLayer;

    private List<GameObject> spawnedObstacles = new List<GameObject>();
    private Color currentColor = Color.white;

    void Start()
    {
        currentGroundSpawnRate = initialGroundSpawnRate;
        currentSkySpawnRate = initialSkySpawnRate;
        currentFollowSpeed = initialFollowSpeed;
        groundSpawnTimer = 0f;
        skySpawnTimer = 0f;
        timeElapsed = 0f;

        if (player == null)
        {
            player = FindObjectOfType<ThirdPersonController>().transform;
            if (player == null)
            {
                Debug.LogError("Player not found! Please assign the player Transform in the Inspector.");
            }
        }

        if (yellowTrigger == null) Debug.LogWarning("Yellow trigger not assigned!");
        if (orangeTrigger == null) Debug.LogWarning("Orange trigger not assigned!");
        if (redTrigger == null) Debug.LogWarning("Red trigger not assigned!");

        ValidatePrefabMaterials(groundSpawnPrefab, "GroundSpawnPrefab");
        ValidatePrefabMaterials(skyFallSpawnPrefab, "SkyFallSpawnPrefab");
    }

    void Update()
    {
        timeElapsed += Time.deltaTime;
        currentGroundSpawnRate = Mathf.Max(minSpawnRate, initialGroundSpawnRate - (groundSpawnRateIncrease * timeElapsed));
        currentSkySpawnRate = Mathf.Max(minSpawnRate, initialSkySpawnRate - (skySpawnRateIncrease * timeElapsed));
        currentFollowSpeed = initialFollowSpeed + (followSpeedIncrease * timeElapsed);

        if (IsPlayerInBounds())
        {
            groundSpawnTimer += Time.deltaTime;
            if (groundSpawnTimer >= currentGroundSpawnRate)
            {
                SpawnGroundObstacle();
                groundSpawnTimer = 0f;
            }

            skySpawnTimer += Time.deltaTime;
            if (skySpawnTimer >= currentSkySpawnRate)
            {
                SpawnSkyFallObstacle();
                skySpawnTimer = 0f;
            }
        }
    }

    void SpawnGroundObstacle()
    {
        Vector3 spawnPos = GetRandomGroundSpawnPosition();
        if (GetTerrainHeight(spawnPos, out float terrainHeight))
        {
            spawnPos.y = terrainHeight;
            GameObject groundSpawn = Instantiate(groundSpawnPrefab, spawnPos, Quaternion.identity);
            SetupObstacleCollision(groundSpawn);
            spawnedObstacles.Add(groundSpawn);
            SetObjectColor(groundSpawn, currentColor);
            StartCoroutine(MoveGroundSpawn(groundSpawn));
        }
    }

    void SpawnSkyFallObstacle()
    {
        Vector3 spawnPos = GetRandomSkySpawnPosition();
        spawnPos.y = skySpawnHeight;
        GameObject skyFallSpawn = Instantiate(skyFallSpawnPrefab, spawnPos, Quaternion.identity);
        SetupObstacleCollision(skyFallSpawn);
        spawnedObstacles.Add(skyFallSpawn);
        SetObjectColor(skyFallSpawn, currentColor);
        StartCoroutine(MoveSkyFallSpawn(skyFallSpawn));
    }

    IEnumerator MoveGroundSpawn(GameObject obstacle)
    {
        Vector3 targetPos = obstacle.transform.position;
        targetPos.y += groundSpawnHeight;
        float riseTime = 0.5f;

        while (obstacle != null && Vector3.Distance(obstacle.transform.position, targetPos) > 0.1f)
        {
            obstacle.transform.position = Vector3.Lerp(obstacle.transform.position, targetPos, Time.deltaTime / riseTime);
            yield return null;
        }

        while (obstacle != null)
        {
            Vector3 direction = (player.position - obstacle.transform.position).normalized;
            direction.y = 0f;
            obstacle.transform.position += direction * currentFollowSpeed * Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator MoveSkyFallSpawn(GameObject obstacle)
    {
        while (obstacle != null)
        {
            obstacle.transform.position += Vector3.down * fallSpeed * Time.deltaTime;
            if (GetTerrainHeight(obstacle.transform.position, out float terrainHeight) &&
                obstacle.transform.position.y <= terrainHeight)
            {
                Destroy(obstacle);
                spawnedObstacles.Remove(obstacle);
                yield break;
            }
            yield return null;
        }
    }

    Vector3 GetRandomGroundSpawnPosition()
    {
        float x = Random.Range(minSpawnBounds.x, maxSpawnBounds.x);
        float z = Random.Range(minSpawnBounds.z, maxSpawnBounds.z);
        return new Vector3(x, minSpawnBounds.y, z);
    }

    Vector3 GetRandomSkySpawnPosition()
    {
        float x = Random.Range(minSpawnBounds.x, maxSpawnBounds.x);
        float z = Random.Range(minSpawnBounds.z, maxSpawnBounds.z);
        return new Vector3(x, maxSpawnBounds.y, z);
    }

    bool IsPlayerInBounds()
    {
        Vector3 playerPos = player.position;
        return playerPos.x >= minSpawnBounds.x && playerPos.x <= maxSpawnBounds.x &&
               playerPos.y >= minSpawnBounds.y && playerPos.y <= maxSpawnBounds.y &&
               playerPos.z >= minSpawnBounds.z && playerPos.z <= maxSpawnBounds.z;
    }

    private bool GetTerrainHeight(Vector3 position, out float height)
    {
        Ray ray = new Ray(new Vector3(position.x, 1000f, position.z), Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 2000f, terrainLayer))
        {
            height = hit.point.y;
            return true;
        }

        height = 0f;
        return false;
    }

    public void SetObstacleColor(Color newColor)
    {
        currentColor = newColor;
        foreach (GameObject obj in spawnedObstacles)
        {
            if (obj != null)
            {
                SetObjectColor(obj, newColor);
            }
        }
        Debug.Log($"Obstacle color set to {newColor}. Future spawns will use this color.");
    }

    private void SetObjectColor(GameObject obj, Color newColor)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null && renderer.material.shader.name.Contains("Universal Render Pipeline/Lit"))
            {
                renderer.material.color = newColor;
            }
            else if (renderer != null)
            {
                Debug.LogWarning($"Renderer on {obj.name} uses shader {renderer.material.shader.name}, which may not support color changes with URP Lit.");
            }
        }
    }

    private void SetupObstacleCollision(GameObject obstacle)
    {
        Collider col = obstacle.GetComponent<Collider>();
        if (col == null)
        {
            col = obstacle.AddComponent<BoxCollider>();
            Debug.LogWarning($"Added BoxCollider to {obstacle.name} as it had no collider.");
        }

        col.isTrigger = false;

        ObstacleCollision collisionScript = obstacle.GetComponent<ObstacleCollision>();
        if (!collisionScript)
        {
            collisionScript = obstacle.AddComponent<ObstacleCollision>();
            collisionScript.spawner = this;
            Debug.Log($"Added ObstacleCollision to {obstacle.name}");
        }
    }

    public void OnPlayerHit()
    {
        ThirdPersonController playerController = player.GetComponent<ThirdPersonController>();
        if (playerController != null)
        {
            Debug.Log("Player hit detected in ObstacleSpawner, calling OnObstacleCollision");
            playerController.OnObstacleCollision();
        }
        else
        {
            Debug.LogWarning("No ThirdPersonController found on player!");
        }
    }

    private void ValidatePrefabMaterials(GameObject prefab, string prefabName)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"{prefabName} is not assigned!");
            return;
        }

        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"{prefabName} has no Renderer components!");
            return;
        }

        foreach (Renderer renderer in renderers)
        {
            if (renderer.material == null || !renderer.material.shader.name.Contains("Universal Render Pipeline/Lit"))
            {
                Debug.LogWarning($"{prefabName} uses a material with shader '{renderer.material?.shader.name ?? "null"}'. Ensure it’s 'Universal Render Pipeline/Lit' for color changes.");
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 center = (minSpawnBounds + maxSpawnBounds) / 2f;
        Vector3 size = maxSpawnBounds - minSpawnBounds;
        Gizmos.DrawWireCube(center, size);

        if (yellowTrigger != null)
        {
            Gizmos.color = Color.yellow;
            Collider triggerCollider = yellowTrigger.GetComponent<Collider>();
            if (triggerCollider != null)
            {
                Gizmos.DrawWireCube(yellowTrigger.transform.position, triggerCollider.bounds.size);
            }
        }
        if (orangeTrigger != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Collider triggerCollider = orangeTrigger.GetComponent<Collider>();
            if (triggerCollider != null)
            {
                Gizmos.DrawWireCube(orangeTrigger.transform.position, triggerCollider.bounds.size);
            }
        }
        if (redTrigger != null)
        {
            Gizmos.color = Color.red;
            Collider triggerCollider = redTrigger.GetComponent<Collider>();
            if (triggerCollider != null)
            {
                Gizmos.DrawWireCube(redTrigger.transform.position, triggerCollider.bounds.size);
            }
        }
    }
}