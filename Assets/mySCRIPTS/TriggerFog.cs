using UnityEngine;
using System.Collections;

public class TriggerFog : MonoBehaviour
{
    // Public variable for the mesh object to constrain fog
    public GameObject meshObject;

    // Public variable for how quickly fog density increases/decreases (units per second)
    public float fogSpeed = 0.01f;

    // Public variable for maximum fog density (0 to 1 range)
    public float maxFogDensity = 0.05f; // Lowered for realism with ExponentialSquared

    // Public variable for the fog color
    public Color fogColor = new Color(0.8f, 0.8f, 0.85f); // Light gray-blue for natural fog

    // Public variable for the collider that turns off the fog
    public Collider fogOffTrigger;

    // Public variable for subtle color variation over time
    public float colorVariationSpeed = 0.1f;
    public float colorVariationAmount = 0.05f;

    // Track if fog is triggered and if it's being turned off
    private bool isFogTriggered = false;
    private bool isFogTurningOff = false;
    private Coroutine fogCoroutine;

    // Original fog color for variation
    private Color baseFogColor;

    // Called when another collider enters this trigger
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isFogTriggered)
        {
            if (meshObject == null)
            {
                Debug.LogError("Mesh object not assigned in TriggerFog script!");
                return;
            }

            isFogTriggered = true;
            if (fogCoroutine != null) StopCoroutine(fogCoroutine);
            fogCoroutine = StartCoroutine(IncreaseFogDensity());
        }
    }

    // Called when another collider enters the fogOffTrigger
    private void OnFogOffTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && isFogTriggered && !isFogTurningOff)
        {
            if (fogCoroutine != null) StopCoroutine(fogCoroutine);
            isFogTurningOff = true;
            fogCoroutine = StartCoroutine(DecreaseFogDensity());
        }
    }

    // Set up the fog off trigger
    private void Awake()
    {
        if (fogOffTrigger != null)
        {
            fogOffTrigger.isTrigger = true;
            TriggerFogOff fogOffScript = fogOffTrigger.gameObject.GetComponent<TriggerFogOff>();
            if (fogOffScript == null)
            {
                fogOffScript = fogOffTrigger.gameObject.AddComponent<TriggerFogOff>();
            }
            fogOffScript.onTriggerEnter = OnFogOffTriggerEnter;
        }
        baseFogColor = fogColor; // Store the base color
    }

    // Coroutine to gradually increase fog density with easing
    private IEnumerator IncreaseFogDensity()
    {
        if (!RenderSettings.fog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared; // Good for dense, natural fog
            RenderSettings.fogDensity = 0f;
            RenderSettings.fogColor = baseFogColor;
        }

        float currentDensity = RenderSettings.fogDensity;
        float elapsedTime = 0f;
        float duration = maxFogDensity / fogSpeed; // Approximate time to reach max

        while (currentDensity < maxFogDensity)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration); // Easing
            currentDensity = Mathf.Lerp(0f, maxFogDensity, t);
            RenderSettings.fogDensity = currentDensity;

            // Subtle color variation
            float variation = Mathf.Sin(Time.time * colorVariationSpeed) * colorVariationAmount;
            RenderSettings.fogColor = baseFogColor + new Color(variation, variation, variation);
            yield return null;
        }

        RenderSettings.fogDensity = maxFogDensity;
        RenderSettings.fogColor = baseFogColor; // Reset to base color at max
    }

    // Coroutine to gradually decrease fog density with easing
    private IEnumerator DecreaseFogDensity()
    {
        float currentDensity = RenderSettings.fogDensity;
        float elapsedTime = 0f;
        float duration = currentDensity / fogSpeed; // Approximate time to clear

        while (currentDensity > 0f)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration); // Easing
            currentDensity = Mathf.Lerp(maxFogDensity, 0f, t);
            RenderSettings.fogDensity = currentDensity;

            // Subtle color variation
            float variation = Mathf.Sin(Time.time * colorVariationSpeed) * colorVariationAmount;
            RenderSettings.fogColor = baseFogColor + new Color(variation, variation, variation);
            yield return null;
        }

        RenderSettings.fogDensity = 0f;
        RenderSettings.fog = false;
        isFogTurningOff = false;
    }

    // Visualize the trigger area in the editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);

        if (fogOffTrigger != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(fogOffTrigger.transform.position, fogOffTrigger.bounds.size);
        }

        if (meshObject != null)
        {
            Gizmos.color = Color.blue;
            Renderer renderer = meshObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                Gizmos.DrawWireCube(renderer.bounds.center, renderer.bounds.size);
            }
        }
    }
}

// Helper class to handle the fog off trigger's OnTriggerEnter event
public class TriggerFogOff : MonoBehaviour
{
    public System.Action<Collider> onTriggerEnter;

    private void OnTriggerEnter(Collider other)
    {
        onTriggerEnter?.Invoke(other);
    }
}