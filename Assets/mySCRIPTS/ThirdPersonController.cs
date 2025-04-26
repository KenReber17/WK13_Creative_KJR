using UnityEngine;
using System.Collections;

public class ThirdPersonController : MonoBehaviour
{
    public CharacterController controller;
    public Transform cam;

    public float speed = 6f;
    public float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;

    public float heightOffset = 0.5f;
    public LayerMask terrainLayer;

    public float heightSmoothTime = 0.1f;
    private float heightVelocity;

    public float gravity = 9.81f;
    public float jumpForce = 5f;
    private float verticalVelocity;

    public AudioSource footstepSound;

    public float footstepInterval = 0.5f;
    private float footstepTimer;

    private Renderer playerRenderer;
    private Color originalColor;
    private int npcHitCount = 0;
    private float lastNPCHitTime = -3f; // Tracks last NPC contact time, initialized to allow first hit
    private float npcHitCooldown = 3f; // 3-second delay between NPC contacts

    void Start()
    {
        SnapToTerrainSurface();

        if (footstepSound == null)
        {
            Debug.LogWarning($"No footstep AudioSource assigned to {gameObject.name}!");
        }
        else
        {
            footstepSound.loop = false;
            footstepSound.playOnAwake = false;
        }

        footstepTimer = 0f;

        playerRenderer = GetComponentInChildren<Renderer>();
        if (playerRenderer == null)
        {
            Debug.LogError($"No Renderer found on {gameObject.name} or its children! Color effects won’t work.");
        }
        else
        {
            originalColor = playerRenderer.material.color;
            Debug.Log($"Player original color set to {originalColor}");
        }
    }

    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        Vector3 moveDir = Vector3.zero;
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            moveDir = moveDir.normalized * speed * Time.deltaTime;

            if (footstepSound != null && IsGrounded())
            {
                footstepTimer -= Time.deltaTime;
                if (footstepTimer <= 0f)
                {
                    footstepSound.Play();
                    footstepTimer = footstepInterval;
                }
            }
        }
        else
        {
            footstepTimer = 0f;
        }

        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            verticalVelocity = jumpForce;
            footstepTimer = 0f;
        }

        AdjustHeightToTerrain(moveDir);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.GetComponent<ObstacleCollision>() != null)
        {
            // Keep existing obstacle collision logic
            if (Time.time - lastNPCHitTime >= npcHitCooldown) // Reuse cooldown for consistency, though this is for obstacles
            {
                Debug.Log("Player collided with obstacle via CharacterController");
                OnObstacleCollision();
                lastNPCHitTime = Time.time; // Update last hit time (shared for simplicity)
            }
            else
            {
                Debug.Log($"Obstacle collision ignored due to cooldown. Time since last hit: {Time.time - lastNPCHitTime}");
            }
        }
    }

    private void SnapToTerrainSurface()
    {
        if (GetTerrainHeight(transform.position, out float terrainHeight))
        {
            Vector3 newPosition = transform.position;
            newPosition.y = terrainHeight + heightOffset + controller.height / 2f;
            transform.position = newPosition;
            verticalVelocity = 0f;
        }
    }

    private void AdjustHeightToTerrain(Vector3 horizontalMove)
    {
        if (GetTerrainHeight(transform.position, out float terrainHeight))
        {
            float targetHeight = terrainHeight + heightOffset + controller.height / 2f;
            Vector3 currentPosition = transform.position;
            float currentHeight = currentPosition.y;

            verticalVelocity -= gravity * Time.deltaTime;
            Vector3 totalMove = horizontalMove;
            totalMove.y = verticalVelocity * Time.deltaTime;

            controller.Move(totalMove);
            currentPosition = transform.position;

            if (IsGrounded())
            {
                float smoothedHeight = Mathf.SmoothDamp(currentHeight, targetHeight, ref heightVelocity, heightSmoothTime);
                Vector3 newPosition = currentPosition;
                newPosition.y = smoothedHeight;
                transform.position = newPosition;
                verticalVelocity = 0f;
            }
            else if (currentPosition.y < targetHeight)
            {
                currentPosition.y = targetHeight;
                transform.position = currentPosition;
                verticalVelocity = 0f;
            }
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
            controller.Move(horizontalMove + Vector3.down * verticalVelocity * Time.deltaTime);
        }
    }

    private bool IsGrounded()
    {
        if (GetTerrainHeight(transform.position, out float terrainHeight))
        {
            float targetHeight = terrainHeight + heightOffset + controller.height / 2f;
            float currentHeight = transform.position.y;
            bool isCloseToGround = Mathf.Abs(currentHeight - targetHeight) < 0.2f;
            bool isNotJumping = verticalVelocity <= 0f;

            return isCloseToGround && isNotJumping;
        }
        return false;
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

    // Called by NPCHoverPatrol when in contact
    public void OnNPCContact()
    {
        if (Time.time - lastNPCHitTime >= npcHitCooldown)
        {
            npcHitCount++;
            Debug.Log($"Player hit by NPC {npcHitCount} times");
            if (npcHitCount < 3)
            {
                StartCoroutine(FlashRed(2));
            }
            else if (npcHitCount == 3)
            {
                StopAllCoroutines();
                StartCoroutine(FlashRed(3, true));
            }
            lastNPCHitTime = Time.time;
        }
        else
        {
            Debug.Log($"NPC contact ignored due to cooldown. Time since last hit: {Time.time - lastNPCHitTime}");
        }
    }

    // Modified from OnObstacleCollision for clarity
    public void OnObstacleCollision()
    {
        // Keep separate obstacle logic if needed
        npcHitCount++; // Assuming obstacles count toward hits; adjust if separate
        Debug.Log($"Player hit by obstacle {npcHitCount} times");
        if (npcHitCount < 3)
        {
            StartCoroutine(FlashRed(2));
        }
        else if (npcHitCount == 3)
        {
            StopAllCoroutines();
            StartCoroutine(FlashRed(3, true));
        }
    }

    private IEnumerator FlashRed(int flashCount, bool stayRed = false)
    {
        if (playerRenderer == null)
        {
            Debug.LogWarning("No player renderer found, skipping flash");
            yield break;
        }

        for (int i = 0; i < flashCount; i++)
        {
            SetPlayerColor(Color.red);
            yield return new WaitForSeconds(0.2f);
            SetPlayerColor(originalColor);
            yield return new WaitForSeconds(0.2f);
        }

        if (stayRed)
        {
            SetPlayerColor(Color.red);
            Debug.Log("Player turned red permanently after 3 NPC hits");
        }
        else
        {
            Debug.Log($"Player flashed red {flashCount} times");
        }
    }

    private void SetPlayerColor(Color newColor)
    {
        if (playerRenderer != null)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.material.color = newColor;
            }
            Debug.Log($"Player color set to {newColor}");
        }
    }
}