using UnityEngine;

public class TorqueDisplayController : MonoBehaviour
{
    [Header("Source")]
    public GearBoxDirectDrive gearbox;

    [Header("UI")]
    public TextMesh inputText;
    public TextMesh outputText;
    public Transform marker;

    [Header("Bar")]
    public float barWidth = 0.22f;

    [Header("Torque calculation")]
    public float inputFactor = 500000f;   // Input = factor / RPM
    public float gearRatio = 4f;          // Output = Input * ratio

    [Header("RPM limits")]
    public float rpmMin = 0f;
    public float rpmMax = 1000f;

    [Header("Zone thresholds (based on OUTPUT torque)")]
    public float greenMaxOutNm = 1000f;
    public float yellowMaxOutNm = 2500f;

    [Header("Colors")]
    public Color greenColor = new Color(0.10f, 0.90f, 0.20f, 1f);
    public Color yellowColor = new Color(0.95f, 0.85f, 0.10f, 1f);
    public Color redColor = new Color(0.95f, 0.15f, 0.15f, 1f);

    [Header("Behavior")]
    public bool showZeroOnFirstStartUntilKnobMoves = true;
    public float knobChangeThresholdRpm = 1f;

    bool wasRunning = false;
    bool firstStartHandled = false;
    bool waitingForKnobChange = false;
    float startSetpointRpm = 0f;

    void Update()
    {
        if (inputText == null || outputText == null || marker == null) return;

        if (gearbox == null)
        {
            SetDisplay(0f, 0f, greenColor);
            return;
        }

        bool running = gearbox.IsRunning;

        if (running && !wasRunning)
        {
            if (showZeroOnFirstStartUntilKnobMoves && !firstStartHandled)
            {
                firstStartHandled = true;
                waitingForKnobChange = true;
                startSetpointRpm = gearbox.screwRpm;
            }
        }

        if (!running && wasRunning)
        {
            waitingForKnobChange = false;
        }

        wasRunning = running;

        float rpm = 0f;
        if (running)
        {
            if (waitingForKnobChange)
            {
                if (Mathf.Abs(gearbox.screwRpm - startSetpointRpm) > knobChangeThresholdRpm)
                    waitingForKnobChange = false;

                rpm = 0f;
            }
            else
            {
                rpm = Mathf.Clamp(gearbox.screwRpm, rpmMin, rpmMax);
            }
        }

        float inNm = 0f, outNm = 0f;
        if (rpm > 0.01f)
        {
            inNm = inputFactor / rpm;
            outNm = inNm * gearRatio;
        }

        Color zone = GetZoneColor(outNm);
        SetDisplay(inNm, outNm, zone);
        SetMarker(outNm, zone);
    }

    void SetDisplay(float inNm, float outNm, Color zone)
    {
        inputText.text = $"IN: {Mathf.RoundToInt(inNm)} Nm";
        outputText.text = $"OUT: {Mathf.RoundToInt(outNm)} Nm";
        inputText.color = zone;
        outputText.color = zone;
    }

    void SetMarker(float outNm, Color zone)
    {
        float displayMax = Mathf.Max(1f, yellowMaxOutNm * 1.25f);
        float t = Mathf.InverseLerp(0f, displayMax, outNm);
        float x = Mathf.Lerp(-barWidth * 0.5f, barWidth * 0.5f, t);

        var lp = marker.localPosition;
        lp.x = x;
        marker.localPosition = lp;

        var r = marker.GetComponent<Renderer>();
        if (r != null && r.material != null && r.material.HasProperty("_Color"))
            r.material.SetColor("_Color", zone);
    }

    Color GetZoneColor(float outNm)
    {
        if (outNm <= greenMaxOutNm) return greenColor;
        if (outNm <= yellowMaxOutNm) return yellowColor;
        return redColor;
    }
}
