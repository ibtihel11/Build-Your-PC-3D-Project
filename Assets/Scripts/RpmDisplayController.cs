using UnityEngine;

public class RpmDisplayController : MonoBehaviour
{
    [Header("Source")]
    public GearBoxDirectDrive gearbox;

    [Header("UI")]
    public TextMesh rpmText;
    public Transform marker;

    [Header("Bar")]
    public float barWidth = 0.18f;

    [Header("RPM thresholds")]
    public float rpmMin = 0f;
    public float rpmGreenMax = 500f;
    public float rpmYellowMax = 800f;
    public float rpmMax = 1000f;

    [Header("Colors")]
    public Color greenColor = new Color(0.10f, 0.90f, 0.20f, 1f);
    public Color yellowColor = new Color(0.95f, 0.85f, 0.10f, 1f);
    public Color redColor = new Color(0.95f, 0.15f, 0.15f, 1f);

    [Header("Behavior")]
    [Tooltip("Beim allerersten Start nach App-Start: Anzeige bleibt erst 0, bis der Drehknopf (screwRpm) verändert wird.")]
    public bool showZeroOnFirstStartUntilKnobMoves = true;

    [Tooltip("Schwellwert, ab wann eine knob-Änderung erkannt wird (RPM).")]
    public float knobChangeThresholdRpm = 1f;

    bool wasRunning = false;
    bool firstStartHandled = false;
    bool waitingForKnobChange = false;
    float startSetpointRpm = 0f;

    void Update()
    {
        if (rpmText == null || marker == null) return;
        if (gearbox == null)
        {
            SetDisplay(0f);
            return;
        }

        bool running = gearbox.IsRunning;

        // Übergang: AUS -> AN
        if (running && !wasRunning)
        {
            if (showZeroOnFirstStartUntilKnobMoves && !firstStartHandled)
            {
                firstStartHandled = true;
                waitingForKnobChange = true;
                startSetpointRpm = gearbox.screwRpm; // Sollwert beim ersten Start merken
            }
        }

        // Übergang: AN -> AUS
        if (!running && wasRunning)
        {
            waitingForKnobChange = false;
        }

        wasRunning = running;

        float rpmToShow = 0f;

        if (!running)
        {
            rpmToShow = 0f; // Stop/Stilstand immer 0
        }
        else
        {
            if (waitingForKnobChange)
            {
                // solange der Nutzer den Knopf noch nicht verändert hat -> 0 anzeigen
                if (Mathf.Abs(gearbox.screwRpm - startSetpointRpm) > knobChangeThresholdRpm)
                    waitingForKnobChange = false;

                rpmToShow = 0f;
            }
            else
            {
                rpmToShow = Mathf.Clamp(gearbox.screwRpm, rpmMin, rpmMax);
            }
        }

        SetDisplay(rpmToShow);
    }

    void SetDisplay(float rpm)
    {
        rpmText.text = $"RPM: {Mathf.RoundToInt(rpm)}";

        Color col = GetZoneColor(rpm);
        rpmText.color = col;

        float t = Mathf.InverseLerp(rpmMin, rpmMax, rpm);
        float x = Mathf.Lerp(-barWidth * 0.5f, barWidth * 0.5f, t);
        var lp = marker.localPosition;
        lp.x = x;
        marker.localPosition = lp;

        var r = marker.GetComponent<Renderer>();
        if (r != null && r.material != null && r.material.HasProperty("_Color"))
            r.material.SetColor("_Color", col);
    }

    Color GetZoneColor(float rpm)
    {
        if (rpm < rpmGreenMax) return greenColor;     // 0..499
        if (rpm < rpmYellowMax) return yellowColor;   // 500..799
        return redColor;                               // 800..1000
    }
}
