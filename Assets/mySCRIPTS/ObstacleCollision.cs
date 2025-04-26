using UnityEngine;

public class ObstacleCollision : MonoBehaviour
{
    public ObstacleSpawner spawner; // Reference to the ObstacleSpawner

    void OnCollisionEnter(Collision collision)
    {
        // Check if the collided object has ThirdPersonController
        ThirdPersonController playerController = collision.gameObject.GetComponent<ThirdPersonController>();
        if (playerController != null && spawner != null)
        {
            spawner.OnPlayerHit();
            Destroy(gameObject); // Destroy obstacle after hitting player (optional)
        }
    }
}