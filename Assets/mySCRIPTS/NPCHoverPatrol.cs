using UnityEngine;

public class NPCHoverPatrol : MonoBehaviour
{
    [Header("Movement Settings")]
    public float hoverHeight = 1f;
    public float speed = 3f;
    public float chaseSpeedMultiplier = 2f;

    [Header("Path Settings")]
    public PatrolPoint[] pathPoints;

    [Header("Detection Settings")]
    public Transform viewPoint;
    public float searchFOV = 60f;
    public float detectionDistance = 10f;
    public float contactDistance = 1f;

    [Header("Timing Settings")]
    public float delayAtPoint = 1f;
    public float searchDuration = 1f;
    public float lostSearchDuration = 3f;
    public float stationaryPivotDuration = 2f;
    public float stationaryPauseDuration = 1f;
    public float wallPivotDuration = 2f;

    [Header("Flashlight Settings")]
    public Color defaultLightColor = Color.white;
    public Color chaseLightColor = Color.red;
    public float flashlightRange = 10f;
    public float flashlightAngle = 60f;

    [Header("NPC Type")]
    [Tooltip("Check if this NPC should remain stationary and not patrol")]
    public bool isStationary = false;

    [System.Serializable]
    public class PatrolPoint
    {
        public Transform point;
        public bool isSearchPoint;
        [Tooltip("Check for pivot left, uncheck for pivot right (only for search points)")]
        public bool pivotLeft = true;
        [Tooltip("Total angle to pivot during search (only for search points)")]
        public float pivotRange = 60f;
    }

    private int currentPointIndex = 0;
    private bool movingForward = true;
    private Transform player;
    private Vector3 lastPosition;
    private bool isChasing = false;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private bool isSearching = false;
    private float searchTimer = 0f;
    private float initialYRotation;
    private bool isLostSearching = false;
    private float lostSearchTimer = 0f;
    private bool isStationarySearching = false;
    private float stationarySearchTimer = 0f;
    private bool isWallPivoting = false;
    private float wallPivotTimer = 0f;
    private float wallPivotStartAngle;
    private Light flashlight;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        lastPosition = transform.position;
        MaintainHoverHeight();

        if (viewPoint == null)
        {
            Debug.LogWarning("ViewPoint not assigned! Creating default at NPC position.");
            GameObject viewObj = new GameObject("ViewPoint");
            viewObj.transform.SetParent(transform);
            viewObj.transform.localPosition = new Vector3(0f, 1f, 0f);
            viewPoint = viewObj.transform;
        }

        flashlight = viewPoint.GetComponent<Light>();
        if (flashlight == null)
        {
            flashlight = viewPoint.gameObject.AddComponent<Light>();
            flashlight.type = LightType.Spot;
            flashlight.range = flashlightRange;
            flashlight.spotAngle = flashlightAngle;
            flashlight.color = defaultLightColor;
            flashlight.intensity = 2f;
        }
        viewPoint.localRotation = Quaternion.identity;
    }

    void Update()
    {
        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
        MaintainHoverHeight();

        bool playerVisible = CanSeePlayer();
        bool playerInContact = IsPlayerInContact();

        if (playerVisible || playerInContact)
        {
            isChasing = true;
            isWaiting = false;
            isSearching = false;
            isLostSearching = false;
            isStationarySearching = false;
            isWallPivoting = false;
            flashlight.color = chaseLightColor;

            if (playerInContact)
            {
                ThirdPersonController playerController = player.GetComponent<ThirdPersonController>();
                if (playerController != null)
                {
                    playerController.OnNPCContact();
                }
                Debug.Log("Player in contact! Locking direction and notifying player.");
            }
            else
            {
                Debug.Log("Player detected! Starting chase.");
            }
        }

        Debug.Log($"Current state - isChasing: {isChasing}, isWaiting: {isWaiting}, isSearching: {isSearching}, isLostSearching: {isLostSearching}, isStationarySearching: {isStationarySearching}, isWallPivoting: {isWallPivoting}");

        if (isChasing)
        {
            ChasePlayer();
        }
        else if (isLostSearching)
        {
            LostPlayerSearch();
        }
        else if (isStationarySearching)
        {
            StationarySearch();
        }
        else if (isSearching)
        {
            SearchAtPoint();
        }
        else if (isWaiting)
        {
            WaitAtPatrolPoint();
        }
        else if (isWallPivoting)
        {
            PivotAtWall();
        }
        else
        {
            Patrol();
        }
    }

    void MaintainHoverHeight()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
        {
            Vector3 targetPosition = hit.point + Vector3.up * hoverHeight;
            transform.position = new Vector3(transform.position.x, targetPosition.y, transform.position.z);
        }
    }

    void Patrol()
    {
        if (isStationary || pathPoints.Length == 0)
        {
            Debug.Log("NPC is stationary. No patrolling.");
            return;
        }

        Vector3 target = pathPoints[currentPointIndex].point.position;
        Vector3 direction = (target - transform.position).normalized;
        float currentSpeed = speed;
        float distanceToTarget = Vector3.Distance(transform.position, target);

        Debug.Log($"Patrolling to point {currentPointIndex} at {target}. Distance: {distanceToTarget}");

        MoveInDirection(direction, currentSpeed);

        if (distanceToTarget < 0.5f)
        {
            if (pathPoints[currentPointIndex].isSearchPoint)
            {
                Debug.Log($"Reached search point {currentPointIndex}. Starting search.");
                StartSearching();
            }
            else
            {
                Debug.Log($"Reached patrol point {currentPointIndex}. Starting 1-second delay.");
                StartWaiting();
            }
        }
    }

    void StartWaiting()
    {
        isWaiting = true;
        waitTimer = 0f;
        Debug.Log($"Waiting at point {currentPointIndex}. Timer reset to 0.");
    }

    void WaitAtPatrolPoint()
    {
        waitTimer += Time.deltaTime;
        Debug.Log($"Waiting at point {currentPointIndex}. Timer: {waitTimer}/{delayAtPoint}");

        if (waitTimer >= delayAtPoint)
        {
            isWaiting = false;
            Debug.Log($"Finished waiting at point {currentPointIndex}. Proceeding to next point.");
            UpdatePointIndex();
        }
    }

    void StartSearching()
    {
        isSearching = true;
        searchTimer = 0f;
        initialYRotation = transform.eulerAngles.y;
        Debug.Log($"Searching at point {currentPointIndex}. Initial rotation: {initialYRotation}, Pivot left: {pathPoints[currentPointIndex].pivotLeft}");
    }

    void SearchAtPoint()
    {
        searchTimer += Time.deltaTime;
        float progress = searchTimer / searchDuration;
        float pivotRange = pathPoints[currentPointIndex].pivotRange;
        bool pivotLeft = pathPoints[currentPointIndex].pivotLeft;

        Debug.Log($"Searching at point {currentPointIndex}. Progress: {progress}, Timer: {searchTimer}/{searchDuration}");

        float angle;
        if (progress < 0.5f)
        {
            angle = pivotLeft ? Mathf.Lerp(0f, pivotRange, progress / 0.5f) : Mathf.Lerp(0f, -pivotRange, progress / 0.5f);
        }
        else
        {
            angle = pivotLeft ? Mathf.Lerp(pivotRange, 0f, (progress - 0.5f) / 0.5f) : Mathf.Lerp(-pivotRange, 0f, (progress - 0.5f) / 0.5f);
        }
        transform.rotation = Quaternion.Euler(0f, initialYRotation + angle, 0f);

        if (progress >= 1f)
        {
            isSearching = false;
            transform.rotation = Quaternion.Euler(0f, initialYRotation, 0f);
            Debug.Log($"Finished searching at point {currentPointIndex}. Proceeding to next point.");
            UpdatePointIndex();
        }
    }

    void ChasePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        float chaseSpeed = speed * chaseSpeedMultiplier;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Always face the player smoothly, even when in contact
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed * 2f);

        if (IsPlayerInContact())
        {
            Debug.Log("Player in contact! Locking direction, no movement.");
            // No movement when in contact, just face the player
        }
        else if (!CanSeePlayer())
        {
            isChasing = false;
            if (!isStationary)
            {
                isLostSearching = true;
                lostSearchTimer = 0f;
                Debug.Log("Player lost! Starting 180-degree search (patrolling NPC).");
            }
            else
            {
                isStationarySearching = true;
                stationarySearchTimer = 0f;
                initialYRotation = transform.eulerAngles.y;
                Debug.Log("Player lost! Starting 45-degree incremental search (stationary NPC).");
            }
            return;
        }
        else
        {
            // Move only if outside contact distance
            if (distanceToPlayer > contactDistance)
            {
                MoveInDirection(direction, chaseSpeed);
            }
            else
            {
                Debug.Log("Player within contact distance! Stopping movement.");
            }
        }
    }

    void LostPlayerSearch()
    {
        lostSearchTimer += Time.deltaTime;
        float progress = lostSearchTimer / lostSearchDuration;

        Debug.Log($"Lost player search (patrolling). Progress: {progress}, Timer: {lostSearchTimer}/{lostSearchDuration}");

        float angle;
        if (progress < 0.33f)
        {
            angle = Mathf.Lerp(0f, 180f, progress / 0.33f);
        }
        else if (progress < 0.66f)
        {
            angle = Mathf.Lerp(180f, 0f, (progress - 0.33f) / 0.33f);
        }
        else
        {
            angle = Mathf.Lerp(0f, -180f, (progress - 0.66f) / 0.34f);
        }
        transform.rotation = Quaternion.Euler(0f, initialYRotation + angle, 0f);

        if (CanSeePlayer())
        {
            isLostSearching = false;
            isChasing = true;
            flashlight.color = chaseLightColor;
            Debug.Log("Player found during search! Resuming chase.");
            return;
        }

        if (progress >= 1f)
        {
            isLostSearching = false;
            flashlight.color = defaultLightColor;
            transform.rotation = Quaternion.Euler(0f, initialYRotation, 0f);
            Debug.Log("Player not found. Resuming patrol with default light color.");
        }
    }

    void StationarySearch()
    {
        stationarySearchTimer += Time.deltaTime;

        float stepDuration = stationaryPivotDuration + stationaryPauseDuration;
        int pivotStep = Mathf.FloorToInt(stationarySearchTimer / stepDuration);
        float timeInStep = stationarySearchTimer % stepDuration;

        int patternLength = 9;
        int patternIndex = pivotStep % patternLength;
        float[] angles = { 0f, 45f, 90f, 45f, 0f, -45f, -90f, -45f, 0f };
        float targetAngle = angles[patternIndex];
        float previousAngle = (patternIndex == 0) ? angles[patternLength - 1] : angles[(patternIndex - 1 + patternLength) % patternLength];

        float angle;
        if (timeInStep < stationaryPivotDuration)
        {
            float pivotProgress = timeInStep / stationaryPivotDuration;
            angle = Mathf.Lerp(previousAngle, targetAngle, pivotProgress);
        }
        else
        {
            angle = targetAngle;
        }
        transform.rotation = Quaternion.Euler(0f, initialYRotation + angle, 0f);

        Debug.Log($"Stationary search. Step: {pivotStep}, Pattern Index: {patternIndex}, Target Angle: {targetAngle}, Current Angle: {angle}, Timer: {timeInStep}/{stepDuration}");

        if (timeInStep >= stepDuration - Time.deltaTime)
        {
            if (CanSeePlayer())
            {
                isStationarySearching = false;
                isChasing = true;
                flashlight.color = chaseLightColor;
                Debug.Log("Player found during stationary search! Resuming chase.");
            }
        }
    }

    void MoveInDirection(Vector3 direction, float moveSpeed)
    {
        float checkDistance = moveSpeed * Time.deltaTime * 2f;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Prevent moving closer than contactDistance to the player
        if (isChasing && distanceToPlayer <= contactDistance)
        {
            Debug.Log("Within contact distance of player, stopping movement.");
            return;
        }

        if (!IsPathClear(direction, checkDistance))
        {
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 left = -right;

            if (IsPathClear(right, checkDistance))
            {
                direction = right;
                Debug.Log($"Path blocked. Adjusting direction to right: {direction}");
            }
            else if (IsPathClear(left, checkDistance))
            {
                direction = left;
                Debug.Log($"Path blocked. Adjusting direction to left: {direction}");
            }
            else
            {
                Debug.Log("All paths blocked. Initiating 180-degree pivot.");
                isWallPivoting = true;
                wallPivotTimer = 0f;
                wallPivotStartAngle = transform.eulerAngles.y;
                return;
            }
        }

        Vector3 moveVector = direction * moveSpeed * Time.deltaTime;
        transform.position += moveVector;

        if (moveVector != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed);
        }

        lastPosition = transform.position;
    }

    void PivotAtWall()
    {
        wallPivotTimer += Time.deltaTime;
        float progress = wallPivotTimer / wallPivotDuration;

        float angle = Mathf.Lerp(0f, 180f, progress);
        transform.rotation = Quaternion.Euler(0f, wallPivotStartAngle + angle, 0f);

        Debug.Log($"Pivoting at wall. Progress: {progress}, Timer: {wallPivotTimer}/{wallPivotDuration}, Angle: {angle}");

        if (progress >= 1f)
        {
            isWallPivoting = false;
            Debug.Log("Finished pivoting 180 degrees at wall. Remaining in this direction.");
        }
    }

    void UpdatePointIndex()
    {
        if (movingForward)
        {
            currentPointIndex++;
            if (currentPointIndex >= pathPoints.Length)
            {
                currentPointIndex = pathPoints.Length - 2;
                movingForward = false;
            }
        }
        else
        {
            currentPointIndex--;
            if (currentPointIndex < 0)
            {
                currentPointIndex = 1;
                movingForward = true;
            }
        }
        currentPointIndex = Mathf.Clamp(currentPointIndex, 0, pathPoints.Length - 1);
        Debug.Log($"Updated point index to {currentPointIndex}, movingForward: {movingForward}");
    }

    bool CanSeePlayer()
    {
        if (!viewPoint) return false;

        Vector3 forwardDirection = viewPoint.forward;
        Vector3 directionToPlayer = (player.position - viewPoint.position).normalized;
        float angleToPlayer = Vector3.Angle(forwardDirection, directionToPlayer);

        if (angleToPlayer <= searchFOV / 2)
        {
            float distanceToPlayer = Vector3.Distance(viewPoint.position, player.position);
            if (distanceToPlayer <= detectionDistance)
            {
                RaycastHit hit;
                if (Physics.Raycast(viewPoint.position, directionToPlayer, out hit, detectionDistance))
                {
                    if (hit.transform == player)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    bool IsPlayerInContact()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        return distanceToPlayer <= contactDistance;
    }

    bool IsPathClear(Vector3 direction, float distance)
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, distance))
        {
            Debug.Log($"Raycast hit: {hit.collider.name}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}, IsTrigger: {hit.collider.isTrigger}");
            if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Ground") && !hit.collider.isTrigger)
            {
                return false;
            }
        }
        return true;
    }

    void OnDrawGizmos()
    {
        if (viewPoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(viewPoint.position, detectionDistance);

            Vector3 forwardDirection = viewPoint.forward;
            Vector3 leftFOV = Quaternion.Euler(0, -searchFOV / 2, 0) * forwardDirection * detectionDistance;
            Vector3 rightFOV = Quaternion.Euler(0, searchFOV / 2, 0) * forwardDirection * detectionDistance;
            Gizmos.DrawLine(viewPoint.position, viewPoint.position + leftFOV);
            Gizmos.DrawLine(viewPoint.position, viewPoint.position + rightFOV);
        }
    }
}