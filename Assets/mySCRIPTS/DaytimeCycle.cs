using UnityEngine;

[ExecuteAlways]
public class DaytimeCycle : MonoBehaviour
{
    // References
    [SerializeField] private Light DirectionalLight;

    // Variables
    [SerializeField, Range(0, 24)] private float TimeOfDay;
    [SerializeField] public float dayDuration = 24f; // Duration of a full day in seconds

    // Public color variables for five stages of the day-night cycle
    public Color midnightColor = new Color(0.1f, 0.1f, 0.3f);      // Color at midnight (deep blue)
    public Color earlyMorningColor = new Color(0.9f, 0.7f, 0.5f); // Color just after sunrise (warm yellow)
    public Color noonColor = Color.white;                         // Color at noon (bright white)
    public Color lateAfternoonColor = new Color(1f, 0.6f, 0.4f);  // Color before sunset (orange)
    public Color nightColor = new Color(0.2f, 0.2f, 0.4f);        // Color at night (dark blue)

    private void Update()
    {
        if (Application.isPlaying)
        {
            // Scale Time.deltaTime by the inverse of dayDuration to control cycle speed
            TimeOfDay += Time.deltaTime * (24f / dayDuration);
            TimeOfDay %= 24;   // Clamp between 0-24
            UpdateLighting(TimeOfDay / 24f);
        }
        else
        {
            UpdateLighting(TimeOfDay / 24f);
        }
    }

    private void UpdateLighting(float timePercent)
    {
        // Interpolate ambient light color based on time of day
        Color ambientColor = Color.black; // Default to black if no interpolation
        if (timePercent <= 0.1f) // Midnight to early morning (0 to 2.4 hours)
        {
            ambientColor = Color.Lerp(midnightColor, earlyMorningColor, timePercent * 10f);
        }
        else if (timePercent <= 0.4f) // Early morning to noon (2.4 to 9.6 hours)
        {
            float t = (timePercent - 0.1f) / 0.3f;
            ambientColor = Color.Lerp(earlyMorningColor, noonColor, t);
        }
        else if (timePercent <= 0.6f) // Noon to late afternoon (9.6 to 14.4 hours)
        {
            float t = (timePercent - 0.4f) / 0.2f;
            ambientColor = Color.Lerp(noonColor, lateAfternoonColor, t);
        }
        else if (timePercent <= 0.9f) // Late afternoon to night (14.4 to 21.6 hours)
        {
            float t = (timePercent - 0.6f) / 0.3f;
            ambientColor = Color.Lerp(lateAfternoonColor, nightColor, t);
        }
        else // Night to midnight (21.6 to 24 hours)
        {
            float t = (timePercent - 0.9f) / 0.1f;
            ambientColor = Color.Lerp(nightColor, midnightColor, t);
        }
        RenderSettings.ambientLight = ambientColor;

        // Interpolate fog color based on time of day
        Color fogColor = Color.black; // Default to black if no interpolation
        if (timePercent <= 0.1f)
        {
            fogColor = Color.Lerp(midnightColor, earlyMorningColor, timePercent * 10f);
        }
        else if (timePercent <= 0.4f)
        {
            float t = (timePercent - 0.1f) / 0.3f;
            fogColor = Color.Lerp(earlyMorningColor, noonColor, t);
        }
        else if (timePercent <= 0.6f)
        {
            float t = (timePercent - 0.4f) / 0.2f;
            fogColor = Color.Lerp(noonColor, lateAfternoonColor, t);
        }
        else if (timePercent <= 0.9f)
        {
            float t = (timePercent - 0.6f) / 0.3f;
            fogColor = Color.Lerp(lateAfternoonColor, nightColor, t);
        }
        else
        {
            float t = (timePercent - 0.9f) / 0.1f;
            fogColor = Color.Lerp(nightColor, midnightColor, t);
        }
        RenderSettings.fogColor = fogColor;

        if (DirectionalLight != null)
        {
            // Interpolate directional light color
            Color lightColor = noonColor; // Default to noon color
            if (timePercent <= 0.1f)
            {
                lightColor = Color.Lerp(midnightColor, earlyMorningColor, timePercent * 10f);
            }
            else if (timePercent <= 0.4f)
            {
                float t = (timePercent - 0.1f) / 0.3f;
                lightColor = Color.Lerp(earlyMorningColor, noonColor, t);
            }
            else if (timePercent <= 0.6f)
            {
                float t = (timePercent - 0.4f) / 0.2f;
                lightColor = Color.Lerp(noonColor, lateAfternoonColor, t);
            }
            else if (timePercent <= 0.9f)
            {
                float t = (timePercent - 0.6f) / 0.3f;
                lightColor = Color.Lerp(lateAfternoonColor, nightColor, t);
            }
            else
            {
                float t = (timePercent - 0.9f) / 0.1f;
                lightColor = Color.Lerp(nightColor, midnightColor, t);
            }
            DirectionalLight.color = lightColor;

            // Rotate the Directional Light based on time of day
            DirectionalLight.transform.localRotation = Quaternion.Euler(new Vector3((timePercent * 360f) - 90f, -170, 0));
        }
    }

    private void OnValidate()
    {
        if (DirectionalLight != null)
            return;

        if (RenderSettings.sun != null)
        {
            DirectionalLight = RenderSettings.sun;
        }
        else
        {
            Light[] lights = GameObject.FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    DirectionalLight = light;
                    return;
                }
            }
        }
    }
}