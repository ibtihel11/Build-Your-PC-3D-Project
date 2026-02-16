using UnityEngine;

public class MachineButtonsController3D : MonoBehaviour
{
    [Header("Gearbox")]
    public GearBoxDirectDrive gearbox;

    [Header("Buttons (assigned by Builder)")]
    public Transform startButtonRoot;
    public Transform stopButtonRoot;

    [Header("Plungers (click targets)")]
    public Transform startPlunger;
    public Transform stopPlunger;

    [Header("Ring + Lamps (optional visuals)")]
    public Renderer startRingRenderer;
    public Renderer stopRingRenderer;
    public Light startLight;
    public Light stopLight;

    [Header("Press Animation")]
    public float pressDepth = 0.010f;
    public float pressSpeed = 18f;

    [Header("Lighting")]
    public float lampIntensity = 3.5f;
    public float ringEmissionMultiplier = 3.0f;

    [Header("Click (Mouse after ESC)")]
    public float maxClickDistance = 10f;
    public LayerMask buttonMask = ~0;
    public bool requireCursorUnlocked = true;

    [Header("Init Behaviour")]
    [Tooltip("Wenn TRUE: beim Start sind beide Knöpfe inaktiv. Erst nach erstem Start-Klick aktiv.")]
    public bool startInactiveUntilStartClick = true;

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    Vector3 startUp, startDown;
    Vector3 stopUp, stopDown;

    bool systemArmed = false;   // <- erst nach Start-Klick true
    bool startPressed = false;  // Start gedrückt? (wenn System aktiv)

    void Start()
    {
        CachePositions();

        // Beim Start: System ggf. inaktiv + alles aus
        systemArmed = !startInactiveUntilStartClick;

        // Getriebe beim Start sicher stoppen
        if (gearbox != null) gearbox.StopRun();

        // Visuell alles AUS + Knöpfe oben
        startPressed = false;
        ApplyVisualsInactive();
    }

    void CachePositions()
    {
        if (startPlunger != null)
        {
            startUp = startPlunger.localPosition;
            startDown = startUp - startPlunger.up * pressDepth;
        }
        if (stopPlunger != null)
        {
            stopUp = stopPlunger.localPosition;
            stopDown = stopUp - stopPlunger.up * pressDepth;
        }
    }

    void Update()
    {
        // Optional: nur nach ESC klicken erlauben
        if (requireCursorUnlocked)
        {
            if (Cursor.lockState == CursorLockMode.Locked || !Cursor.visible)
            {
                AnimatePlungers();
                return;
            }
        }

        if (Input.GetMouseButtonDown(0))
            TryClickMouse();

        AnimatePlungers();
    }

    void TryClickMouse()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, maxClickDistance, buttonMask, QueryTriggerInteraction.Collide))
            return;

        Transform t = hit.transform;

        bool clickedStart = (startPlunger != null && (t == startPlunger || t.IsChildOf(startPlunger))) ||
                            (startButtonRoot != null && t.IsChildOf(startButtonRoot));

        bool clickedStop = (stopPlunger != null && (t == stopPlunger || t.IsChildOf(stopPlunger))) ||
                           (stopButtonRoot != null && t.IsChildOf(stopButtonRoot));

        // --- Phase 1: System ist inaktiv ---
        if (!systemArmed)
        {
            // Nur START darf das System "scharf schalten"
            if (clickedStart)
            {
                systemArmed = true;
                SetRunning(true); // Start wird gedrückt + Getriebe startet
            }
            else
            {
                // Klick auf Stop ignorieren solange inaktiv
                ApplyVisualsInactive();
            }
            return;
        }

        // --- Phase 2: System aktiv -> normal togglen ---
        if (clickedStart) SetRunning(true);
        else if (clickedStop) SetRunning(false);
    }

    public void SetRunning(bool running)
    {
        if (!systemArmed)
        {
            // Wenn aus irgendeinem Grund hier aufgerufen wird: erst scharf schalten
            systemArmed = true;
        }

        startPressed = running;

        if (gearbox != null)
        {
            if (running) gearbox.StartRun();
            else gearbox.StopRun();
        }

        ApplyVisualsActive(startPressed);
    }

    void AnimatePlungers()
    {
        // Inaktiv: beide "oben"
        if (!systemArmed)
        {
            if (startPlunger != null)
                startPlunger.localPosition = Vector3.Lerp(startPlunger.localPosition, startUp, 1f - Mathf.Exp(-pressSpeed * Time.deltaTime));
            if (stopPlunger != null)
                stopPlunger.localPosition = Vector3.Lerp(stopPlunger.localPosition, stopUp, 1f - Mathf.Exp(-pressSpeed * Time.deltaTime));
            return;
        }

        // Aktiv: gedrückter rein, anderer raus
        if (startPlunger != null)
        {
            Vector3 target = startPressed ? startDown : startUp;
            startPlunger.localPosition = Vector3.Lerp(startPlunger.localPosition, target, 1f - Mathf.Exp(-pressSpeed * Time.deltaTime));
        }

        if (stopPlunger != null)
        {
            Vector3 target = startPressed ? stopUp : stopDown;
            stopPlunger.localPosition = Vector3.Lerp(stopPlunger.localPosition, target, 1f - Mathf.Exp(-pressSpeed * Time.deltaTime));
        }
    }

    // ---------- Visuals ----------

    void ApplyVisualsInactive()
    {
        // Alles aus, beide nicht gedrückt
        SetLamp(startLight, false);
        SetLamp(stopLight, false);
        SetRingEmission(startRingRenderer, Color.green, false);
        SetRingEmission(stopRingRenderer, Color.red, false);
    }

    void ApplyVisualsActive(bool startIsPressed)
    {
        // Start leuchtet wenn gedrückt, Stop leuchtet wenn gedrückt
        SetLamp(startLight, startIsPressed);
        SetLamp(stopLight, !startIsPressed);

        Color startCol = startLight != null ? startLight.color : Color.green;
        Color stopCol = stopLight != null ? stopLight.color : Color.red;

        SetRingEmission(startRingRenderer, startCol, startIsPressed);
        SetRingEmission(stopRingRenderer, stopCol, !startIsPressed);
    }

    void SetLamp(Light l, bool on)
    {
        if (l == null) return;
        l.intensity = on ? lampIntensity : 0f;
    }

    void SetRingEmission(Renderer r, Color emissionColor, bool on)
    {
        if (r == null || r.material == null) return;
        r.material.EnableKeyword("_EMISSION");
        r.material.SetColor(EmissionId, on ? emissionColor * ringEmissionMultiplier : Color.black);
    }
}
