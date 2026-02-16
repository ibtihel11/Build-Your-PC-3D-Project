using UnityEngine;

public class WarningBeaconController : MonoBehaviour
{
    [Header("Reference")]
    public GearBoxDirectDrive gearbox;

    [Header("Blink")]
    public float blinkHz = 2.0f;            // Rot blinkt bei RUN

    [Header("Assigned by Builder (or manually)")]
    public Renderer redLensRenderer;
    public Light redLight;

    [Header("Intensity / Emission")]
    public float defaultLightIntensity = 3.0f;
    public float emissionMultiplier = 2.0f;

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        // Start: alles AUS
        SetRed(false);
    }

    void Update()
    {
        bool running = gearbox != null && gearbox.IsRunning;

        if (!running)
        {
            SetRed(false);
            return;
        }

        bool on = Mathf.Sin(Time.time * Mathf.PI * 2f * blinkHz) > 0f;
        SetRed(on);
    }

    void SetRed(bool on)
    {
        if (redLight) redLight.intensity = on ? defaultLightIntensity : 0f;

        if (redLensRenderer && redLensRenderer.material)
        {
            redLensRenderer.material.EnableKeyword("_EMISSION");
            Color baseCol = redLensRenderer.material.color;
            redLensRenderer.material.SetColor(EmissionId, on ? baseCol * emissionMultiplier : Color.black);
        }
    }
}
