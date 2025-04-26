using UnityEngine;

public class GateTrigger : MonoBehaviour
{
    // Array of gates to move when triggered
    public GameObject[] gates;

    // Distance to move gates downward on the y-axis
    public float moveDistance = 5f;

    // Speed of gate movement downward
    public float moveSpeed = 2f;

    // Speed of gate movement upward (closing), optional (0 or negative to disable)
    public float closeSpeed = 2f;

    // Delay before gates start closing (in seconds)
    public float closeDelay = 15f;

    // AudioSource for gate opening sound
    public AudioSource gateOpenSound;

    // Optional AudioSource for important door sound
    public AudioSource importantDoorSound;

    // Original and target positions for each gate
    private Vector3[] originalPositions;
    private Vector3[] targetPositions;
    private bool isTriggered = false;
    private bool isClosing = false;
    private float closeTimer = 0f;

    // Renderer for the button to change color
    private Renderer buttonRenderer;
    private Color originalColor; // Store the default color

    void Start()
    {
        // Get the renderer of the button (this object)
        buttonRenderer = GetComponent<Renderer>();
        if (buttonRenderer == null)
        {
            Debug.LogWarning($"No Renderer found on {gameObject.name}! Color change won’t work.");
        }
        else
        {
            // Store the original color
            originalColor = buttonRenderer.material.color;
        }

        // Initialize arrays for gate positions
        originalPositions = new Vector3[gates.Length];
        targetPositions = new Vector3[gates.Length];

        // Store original positions and calculate target positions for all gates
        for (int i = 0; i < gates.Length; i++)
        {
            if (gates[i] != null)
            {
                originalPositions[i] = gates[i].transform.position;
                targetPositions[i] = originalPositions[i] + Vector3.down * moveDistance;
            }
            else
            {
                Debug.LogWarning($"Gate at index {i} is not assigned on {gameObject.name}!");
            }
        }

        // Ensure the collider is set up correctly
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogError($"No Collider found on {gameObject.name}!");
        }
        else if (!collider.isTrigger)
        {
            Debug.LogError($"Collider on {gameObject.name} is not set as a trigger!");
        }

        // Check if AudioSources are assigned
        if (gateOpenSound == null)
        {
            Debug.LogWarning($"No gate opening AudioSource assigned to {gameObject.name}!");
        }
        if (importantDoorSound == null)
        {
            Debug.LogWarning($"No important door AudioSource assigned to {gameObject.name}. This is optional.");
        }
    }

    void Update()
    {
        // Move gates toward target positions if triggered (opening)
        if (isTriggered && !isClosing)
        {
            for (int i = 0; i < gates.Length; i++)
            {
                if (gates[i] != null)
                {
                    gates[i].transform.position = Vector3.MoveTowards(
                        gates[i].transform.position,
                        targetPositions[i],
                        moveSpeed * Time.deltaTime
                    );
                }
            }

            // Check if gates have reached the target (fully open)
            bool allGatesOpen = true;
            for (int i = 0; i < gates.Length; i++)
            {
                if (gates[i] != null && Vector3.Distance(gates[i].transform.position, targetPositions[i]) > 0.01f)
                {
                    allGatesOpen = false;
                    break;
                }
            }

            // Start close timer once gates are fully open (if closeSpeed > 0)
            if (allGatesOpen && closeSpeed > 0f)
            {
                closeTimer += Time.deltaTime;
                if (closeTimer >= closeDelay)
                {
                    isClosing = true; // Begin closing
                }
            }
        }

        // Move gates back to original positions if closing
        if (isClosing)
        {
            for (int i = 0; i < gates.Length; i++)
            {
                if (gates[i] != null)
                {
                    gates[i].transform.position = Vector3.MoveTowards(
                        gates[i].transform.position,
                        originalPositions[i],
                        closeSpeed * Time.deltaTime
                    );
                }
            }

            // Check if gates have returned to original positions
            bool allGatesClosed = true;
            for (int i = 0; i < gates.Length; i++)
            {
                if (gates[i] != null && Vector3.Distance(gates[i].transform.position, originalPositions[i]) > 0.01f)
                {
                    allGatesClosed = false;
                    break;
                }
            }

            // Reset trigger state and color if fully closed
            if (allGatesClosed)
            {
                isTriggered = false;
                isClosing = false;
                closeTimer = 0f;
                if (buttonRenderer != null)
                {
                    buttonRenderer.material.color = originalColor; // Reset to default color
                }
                Debug.Log($"{gameObject.name} reset and ready for next trigger.");
            }
        }
    }

    // Trigger when player enters the collider
    void OnTriggerEnter(Collider other)
    {
        // Check if the colliding object or its parent has ThirdPersonController
        if (other.GetComponentInParent<ThirdPersonController>() != null && !isTriggered)
        {
            // Activate gate movement
            isTriggered = true;

            // Change button color to green
            if (buttonRenderer != null)
            {
                buttonRenderer.material.color = Color.green;
            }

            // Play both audio sources if assigned
            if (gateOpenSound != null)
            {
                gateOpenSound.Play();
            }
            if (importantDoorSound != null)
            {
                importantDoorSound.Play();
            }

            // Build gate names string manually
            string gateNames = "";
            for (int i = 0; i < gates.Length; i++)
            {
                gateNames += (gates[i] != null ? gates[i].name : "null");
                if (i < gates.Length - 1) gateNames += ", ";
            }
            Debug.Log($"{gameObject.name} triggered by player. Moving gates: {gateNames}");
        }
        else if (other.GetComponentInParent<ThirdPersonController>() != null)
        {
            Debug.Log($"{gameObject.name} contacted by player but already triggered or closing.");
        }
    }

    // Optional: Visualize the button in the editor
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}