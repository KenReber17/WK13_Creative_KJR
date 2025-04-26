using UnityEngine;

public class ColorTrigger : MonoBehaviour
{
    public ObstacleSpawner spawner; // Reference to the ObstacleSpawner
    public Color triggerColor; // Color to set when triggered

    void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col == null || !col.isTrigger)
        {
            Debug.LogError($"ColorTrigger on {gameObject.name} requires a trigger collider!");
        }
        if (spawner == null)
        {
            Debug.LogWarning($"ObstacleSpawner not assigned to {gameObject.name}!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<ThirdPersonController>() != null && spawner != null)
        {
            spawner.SetObstacleColor(triggerColor);
            Debug.Log($"{gameObject.name} triggered. Setting obstacles to {triggerColor}.");
        }
    }
}