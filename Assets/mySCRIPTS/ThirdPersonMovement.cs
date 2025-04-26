using System.Collections;
using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    public CharacterController controller;
    public Transform cam;

    public float speed = 6f;
    public float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;

    // Height offset above the mesh
    public float heightOffset = 0.5f;

    // Layer mask to raycast against the mesh
    public LayerMask terrainLayer;

    // Smoothing for height adjustment
    public float heightSmoothTime = 0.1f;
    private float heightVelocity;

    // Gravity and jumping variables
    public float gravity = 9.81f;
    public float jumpForce = 5f; // Public variable for jump strength
    private float verticalVelocity; // Tracks vertical movement (jumping/falling)

    // Reference to the MeshGenerator
    public MeshGenerator meshGenerator;

    void Start()
    {
        if (meshGenerator == null)
        {
            meshGenerator = FindObjectOfType<MeshGenerator>();
            if (meshGenerator == null)
            {
                Debug.LogError("MeshGenerator not found in scene! Please assign it in the Inspector.");
            }
        }

        SnapToMeshSurface();
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
        }

        // Check for jump input
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            verticalVelocity = jumpForce;
        }

        // Apply movement and adjust height
        AdjustHeightToMesh(moveDir);
    }

    // Snap player to mesh surface at start
    private void SnapToMeshSurface()
    {
        if (GetMeshHeight(transform.position, out float meshHeight))
        {
            Vector3 newPosition = transform.position;
            newPosition.y = meshHeight + heightOffset + controller.height / 2f; // Center of controller
            transform.position = newPosition;
            verticalVelocity = 0f; // Ensure grounded at start
        }
    }

    // Adjust player height to stay 0.5f above the mesh, including jumping
    private void AdjustHeightToMesh(Vector3 horizontalMove)
    {
        if (GetMeshHeight(transform.position, out float meshHeight))
        {
            float targetHeight = meshHeight + heightOffset + controller.height / 2f; // Center of controller
            Vector3 currentPosition = transform.position;
            float currentHeight = currentPosition.y;

            // Apply gravity to vertical velocity
            verticalVelocity -= gravity * Time.deltaTime;

            // Combine horizontal and vertical movement
            Vector3 totalMove = horizontalMove;
            totalMove.y = verticalVelocity * Time.deltaTime;

            // Move the controller
            controller.Move(totalMove);

            // Update current position after movement
            currentPosition = transform.position;

            // Only adjust height if grounded (close to mesh and not jumping)
            if (IsGrounded())
            {
                float smoothedHeight = Mathf.SmoothDamp(currentHeight, targetHeight, ref heightVelocity, heightSmoothTime);
                Vector3 newPosition = currentPosition;
                newPosition.y = smoothedHeight;
                transform.position = newPosition;
                verticalVelocity = 0f; // Reset velocity when grounded
            }
            // Clamp to prevent sinking below mesh
            else if (currentPosition.y < targetHeight)
            {
                currentPosition.y = targetHeight;
                transform.position = currentPosition;
                verticalVelocity = 0f; // Reset velocity when hitting ground
            }
        }
        else
        {
            // If no mesh hit, apply gravity to fall naturally
            verticalVelocity -= gravity * Time.deltaTime;
            controller.Move(horizontalMove + Vector3.down * verticalVelocity * Time.deltaTime);
        }
    }

    // Check if player is grounded on the mesh
    private bool IsGrounded()
    {
        if (GetMeshHeight(transform.position, out float meshHeight))
        {
            float targetHeight = meshHeight + heightOffset + controller.height / 2f;
            float currentHeight = transform.position.y;
            bool isCloseToGround = Mathf.Abs(currentHeight - targetHeight) < 0.2f;
            bool isNotJumping = verticalVelocity <= 0f;

            return isCloseToGround && isNotJumping;
        }
        return false;
    }

    // Raycast to get mesh height at position
    private bool GetMeshHeight(Vector3 position, out float height)
    {
        float maxPossibleHeight = meshGenerator != null ? meshGenerator.maxHeight * transform.localScale.y + transform.position.y : 1000f;
        Ray ray = new Ray(new Vector3(position.x, maxPossibleHeight + 500f, position.z), Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, terrainLayer))
        {
            height = hit.point.y;
            return true;
        }

        height = 0f; // Default fallback if raycast fails
        return false;
    }
}