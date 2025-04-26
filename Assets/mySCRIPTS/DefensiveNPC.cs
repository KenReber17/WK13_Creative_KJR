using UnityEngine;
using UnityEngine.AI; // Required for NavMeshAgent
using System.Collections;
using System.Linq; // Required for LINQ (for validation)

[RequireComponent(typeof(NavMeshAgent))]
public class DefensiveNPC : MonoBehaviour
{
    // Public variables for Unity Inspector
    public Transform player; // Reference to the player's Transform
    public Transform[] safeLocations = new Transform[7]; // Array for up to 7 safe locations
    public float detectionRadius = 50f; // Primary detection radius for player proximity
    public float minDistanceThreshold = 25f; // Minimum distance threshold when at safe location
    public float checkInterval = 5f; // Check interval when not at safe location or returning
    public float safeLocationCheckInterval = 2f; // Check interval when at safe location
    public float fleeSpeed = 5f; // Speed when moving to safe location
    public float returnSpeed = 2.5f; // Speed when returning to original position
    public float arrivalDistance = 0.5f; // Distance to consider NPC has reached destination
    public float velocityThreshold = 0.2f; // Minimum velocity to trigger unexpected movement warning
    public float positionChangeThreshold = 1.0f; // Minimum position change to trigger warning (new)

    // Private variables
    private NavMeshAgent navAgent; // Reference to the NPC's NavMeshAgent
    private Vector3 originalPosition; // NPC's starting position
    private Transform currentSafeLocation; // Current safe location being targeted
    private bool isMovingToSafeLocation = false; // Tracks if NPC is moving to a safe location
    private bool isMovingToOriginalPosition = false; // Tracks if NPC is returning to original position
    private bool isAtSafeLocation = false; // Tracks if NPC is at a safe location
    private float timeSinceReachedDestination = 0f; // Tracks time since last destination reached
    private Vector3 lastPosition; // Tracks last position to detect external movement

    void Start()
    {
        // Get the NavMeshAgent component
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            Debug.LogError("NavMeshAgent component missing on " + gameObject.name);
            enabled = false;
            return;
        }

        // Configure NavMeshAgent
        navAgent.autoBraking = true;
        navAgent.stoppingDistance = arrivalDistance;
        navAgent.angularSpeed = 120f; // Reduce angular adjustments
        navAgent.acceleration = 8f; // Smooth acceleration
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance; // Reduce avoidance when moving

        // Store the NPC's initial position
        originalPosition = transform.position;
        lastPosition = originalPosition;

        // Validate references
        if (player == null)
        {
            Debug.LogWarning("Player Transform not assigned in Inspector for " + gameObject.name);
        }

        // Check if any safe locations are assigned and on NavMesh
        bool hasValidSafeLocation = false;
        for (int i = 0; i < safeLocations.Length; i++)
        {
            if (safeLocations[i] != null)
            {
                if (!IsPositionOnNavMesh(safeLocations[i].position))
                {
                    Debug.LogWarning($"Safe location {safeLocations[i].name} (index {i}) is not on NavMesh for {gameObject.name}. NPC may behave erratically.");
                }
                hasValidSafeLocation = true;
            }
        }
        if (!hasValidSafeLocation)
        {
            Debug.LogWarning("No valid Safe Locations assigned for " + gameObject.name + ". Please assign at least one in Inspector.");
        }

        // Check if NPC's initial position is on NavMesh
        if (!IsPositionOnNavMesh(originalPosition))
        {
            Debug.LogWarning("NPC's initial position is not on NavMesh for " + gameObject.name + ". Movement may be erratic.");
        }

        // Start the proximity check coroutine
        Debug.Log($"{gameObject.name} starting CheckPlayerProximity coroutine.");
        StartCoroutine(CheckPlayerProximity());
    }

    void Update()
    {
        // Check if NPC has reached its destination (safe location or original position)
        if ((isMovingToSafeLocation || isMovingToOriginalPosition) && navAgent.hasPath)
        {
            float distanceToDestination = Vector3.Distance(transform.position, navAgent.destination);
            if (distanceToDestination <= arrivalDistance)
            {
                OnReachedDestination();
            }
        }

        // Update time since last destination reached
        if (!isMovingToSafeLocation && !isMovingToOriginalPosition)
        {
            timeSinceReachedDestination += Time.deltaTime;
        }

        // Safeguard: Stop unintended movement when not supposed to be moving
        if (!isMovingToSafeLocation && !isMovingToOriginalPosition && navAgent.velocity.magnitude > velocityThreshold && timeSinceReachedDestination > 0.1f)
        {
            Debug.LogWarning($"{gameObject.name} is moving unexpectedly (velocity: {navAgent.velocity.magnitude:F2}, time since reached: {timeSinceReachedDestination:F2}s). Forcing stop.");
            navAgent.isStopped = true;
            navAgent.ResetPath();
            navAgent.velocity = Vector3.zero; // Explicitly clear velocity
        }

        // Debug external position changes (e.g., physics or colliders)
        if (!isMovingToSafeLocation && !isMovingToOriginalPosition)
        {
            float positionChange = Vector3.Distance(transform.position, lastPosition);
            if (positionChange > positionChangeThreshold)
            {
                Debug.LogWarning($"{gameObject.name} position changed unexpectedly (change: {positionChange:F2} units). Possible physics or collider interaction.");
            }
        }
        lastPosition = transform.position;

        // Debug physics interactions if Rigidbody is present
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && rb.linearVelocity.magnitude > 0)
        {
            Debug.LogWarning($"{gameObject.name} has Rigidbody velocity ({rb.linearVelocity.magnitude:F2}). Check for physics interactions.");
        }
    }

    // Detect collision events to debug physics interactions
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"{gameObject.name} collided with {collision.gameObject.name} (contact point: {collision.contacts[0].point}). Possible cause of position change.");
    }

    // Coroutine to periodically check player proximity
    private IEnumerator CheckPlayerProximity()
    {
        while (true)
        {
            Debug.Log($"{gameObject.name} CheckPlayerProximity tick. isMovingToSafeLocation={isMovingToSafeLocation}, isAtSafeLocation={isAtSafeLocation}");

            // Only skip checks if moving to a safe location
            if (!isMovingToSafeLocation)
            {
                // Validate references
                if (player == null)
                {
                    Debug.LogWarning($"{gameObject.name} Player reference is null. Skipping proximity check.");
                }
                if (safeLocations.Length == 0 || safeLocations.All(loc => loc == null))
                {
                    Debug.LogWarning($"{gameObject.name} No valid safe locations assigned. Skipping proximity check.");
                }

                // Perform proximity check if references are valid
                if (player != null && safeLocations.Any(loc => loc != null))
                {
                    // Calculate distance to player
                    float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                    Debug.Log($"{gameObject.name} checked player distance: {distanceToPlayer:F2} units (Detection Radius: {detectionRadius:F2}, Min Distance Threshold: {minDistanceThreshold:F2}). At Safe Location: {isAtSafeLocation}, Moving to Safe: {isMovingToSafeLocation}, Moving to Original: {isMovingToOriginalPosition}");

                    // If at a safe location, use minDistanceThreshold
                    if (isAtSafeLocation)
                    {
                        // If player is too close, flee to a new safe location
                        if (distanceToPlayer <= minDistanceThreshold)
                        {
                            MoveToSafeLocation();
                        }
                        // If player is outside detection radius, return to original position
                        else if (distanceToPlayer > detectionRadius)
                        {
                            ReturnToOriginalPosition();
                        }
                        // If minDistanceThreshold < distanceToPlayer <= detectionRadius, stay hidden
                        else
                        {
                            Debug.Log($"{gameObject.name} staying hidden at safe location (player within detection radius but outside min threshold).");
                        }
                    }
                    // If not at a safe location (e.g., at original position or returning)
                    else
                    {
                        // If player is within detection radius, flee to a safe location
                        if (distanceToPlayer <= detectionRadius)
                        {
                            MoveToSafeLocation();
                        }
                        // If at original position and player is out of range, stay put
                        else
                        {
                            Debug.Log($"{gameObject.name} staying at original position (player outside detection radius).");
                        }
                    }
                }
            }
            else
            {
                Debug.Log($"{gameObject.name} skipping proximity check because it is moving to a safe location.");
            }

            // Use different check interval based on state
            float interval = isAtSafeLocation ? safeLocationCheckInterval : checkInterval;
            Debug.Log($"{gameObject.name} waiting for next check in {interval} seconds.");
            yield return new WaitForSeconds(interval);
        }
    }

    // Move the NPC to a random safe location
    private void MoveToSafeLocation()
    {
        // Select a random safe location
        currentSafeLocation = GetRandomSafeLocation();
        if (currentSafeLocation == null)
        {
            Debug.LogWarning($"{gameObject.name} No valid safe locations found. Cannot move.");
            return;
        }

        isMovingToSafeLocation = true;
        isMovingToOriginalPosition = false;
        isAtSafeLocation = true; // Will be at a safe location after reaching it
        navAgent.speed = fleeSpeed;
        navAgent.isStopped = false; // Ensure agent is active
        navAgent.updatePosition = true; // Ensure position updates
        navAgent.updateRotation = true; // Ensure rotation updates
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance; // Enable avoidance when moving
        navAgent.SetDestination(currentSafeLocation.position);
        Debug.Log($"{gameObject.name} is fleeing to random safe location at {currentSafeLocation.position}{(isMovingToOriginalPosition ? " (interrupted return)" : "")}");
    }

    // Return the NPC to its original position
    private void ReturnToOriginalPosition()
    {
        isMovingToSafeLocation = false;
        isMovingToOriginalPosition = true;
        isAtSafeLocation = false; // No longer at a safe location
        navAgent.speed = returnSpeed;
        navAgent.isStopped = false; // Ensure agent is active
        navAgent.updatePosition = true;
        navAgent.updateRotation = true;
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        navAgent.SetDestination(originalPosition);
        Debug.Log($"{gameObject.name} is returning to original position at {originalPosition}");
    }

    // Called when the NPC reaches its destination (safe location or original position)
    private void OnReachedDestination()
    {
        isMovingToSafeLocation = false;
        isMovingToOriginalPosition = false;
        navAgent.isStopped = true; // Explicitly stop the agent
        navAgent.ResetPath(); // Clear any remaining path
        navAgent.velocity = Vector3.zero; // Explicitly clear velocity
        navAgent.speed = 0f; // Prevent internal movement adjustments
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance; // Disable avoidance when stationary
        navAgent.updatePosition = false; // Prevent position updates when stationary
        navAgent.updateRotation = false; // Prevent rotation updates
        timeSinceReachedDestination = 0f; // Reset timer
        Debug.Log($"{gameObject.name} has reached destination at {transform.position} (At Safe Location: {isAtSafeLocation})");
    }

    // Select a random safe location from the array
    private Transform GetRandomSafeLocation()
    {
        // Filter out null safe locations
        var validSafeLocations = safeLocations
            .Where(loc => loc != null)
            .ToList();

        // Check if there are any valid safe locations
        if (validSafeLocations.Count == 0)
        {
            Debug.LogWarning($"{gameObject.name} No valid safe locations available.");
            return null;
        }

        // Select a random safe location
        int randomIndex = Random.Range(0, validSafeLocations.Count);
        Debug.Log($"{gameObject.name} selected random safe location index {randomIndex} ({validSafeLocations[randomIndex].name})");
        return validSafeLocations[randomIndex];
    }

    // Helper method to check if a position is on the NavMesh
    private bool IsPositionOnNavMesh(Vector3 position)
    {
        NavMeshHit hit;
        bool isOnNavMesh = NavMesh.SamplePosition(position, out hit, 1.0f, NavMesh.AllAreas);
        if (!isOnNavMesh)
        {
            Debug.LogWarning($"{gameObject.name} Position {position} is not on NavMesh (closest valid: {hit.position}).");
        }
        return isOnNavMesh;
    }

    // Optional: Visualize detection radius and safe locations in the Editor
    void OnDrawGizmosSelected()
    {
        // Draw a wire sphere to represent the primary detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw a wire sphere for the minimum distance threshold (when at safe location)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minDistanceThreshold);

        // Draw lines to valid safe locations for debugging
        Gizmos.color = Color.green;
        for (int i = 0; i < safeLocations.Length; i++)
        {
            if (safeLocations[i] != null)
            {
                Gizmos.DrawLine(transform.position, safeLocations[i].position);
                // Optionally draw index labels for debugging
                // UnityEditor.Handles.Label(safeLocations[i].position, $"Safe {i}");
            }
        }
    }
}