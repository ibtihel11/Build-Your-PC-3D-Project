using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MachineKnobController3D : MonoBehaviour
{
    [Header("Gearbox")]
    public GearBoxDirectDrive gearbox;

    [Header("References (auto-filled by Builder)")]
    public Transform knobRotator;
    public Collider knobCollider;
    public Renderer ringRenderer;
    public Light ringLight;

    [Header("Mouse Control (after ESC)")]
    public bool requireCursorUnlocked = true;
    public float maxClickDistance = 10f;
    public LayerMask knobMask = ~0;

    [Tooltip("Grad pro Pixel Mausbewegung")]
    public float rotateSensitivity = 0.25f;

    [Header("Angle Range")]
    public float minAngle = -135f;
    public float maxAngle = 135f;

    [Header("RPM Range")]
    public float minRpm = 0f;
    public float maxRpm = 1000f;

    [Header("Enable Rule")]
    public bool onlyWhenRunning = true;

    [Header("Visual Feedback")]
    public float ringEmissionMultiplier = 3.0f;
    public float ringLightIntensity = 1.8f;

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    private bool dragging;
    private float currentAngle;
    private float lastMouseX;

    void Start()
    {
        // Startwinkel aus aktueller Drehzahl (falls vorhanden)
        float rpm = (gearbox != null) ? gearbox.screwRpm : 0f;
        float t = Mathf.InverseLerp(minRpm, maxRpm, rpm);
        currentAngle = Mathf.Lerp(minAngle, maxAngle, t);
        ApplyAngle(currentAngle);

        UpdateVisuals(IsKnobEnabled());
    }

    void Update()
    {
        // nur nach ESC bedienen
        if (requireCursorUnlocked)
        {
            if (Cursor.lockState == CursorLockMode.Locked || !Cursor.visible)
            {
                dragging = false;
                UpdateVisuals(false);
                return;
            }
        }

        bool enabled = IsKnobEnabled();
        UpdateVisuals(enabled);

        if (!enabled)
        {
            dragging = false;
            return;
        }

        // Mouse down -> prüfen ob Knopf getroffen
        if (GetMouseButtonDown0())
        {
            if (HitKnob())
            {
                dragging = true;
                lastMouseX = GetMousePositionX();
            }
        }

        // Mouse up -> drag stoppen
        if (GetMouseButtonUp0())
            dragging = false;

        // Dragging -> Winkel ändern
        if (dragging && GetMouseButton0())
        {
            float mx = GetMousePositionX();
            float dx = mx - lastMouseX;
            lastMouseX = mx;

            currentAngle += dx * rotateSensitivity;
            currentAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);

            ApplyAngle(currentAngle);

            float tt = Mathf.InverseLerp(minAngle, maxAngle, currentAngle);
            float rpm = Mathf.Lerp(minRpm, maxRpm, tt);

            if (gearbox != null)
                gearbox.SetScrewRpm(rpm);
        }
    }

    bool IsKnobEnabled()
    {
        if (gearbox == null) return false;
        if (!onlyWhenRunning) return true;
        return gearbox.IsRunning;
    }

    bool HitKnob()
    {
        Camera cam = Camera.main;
        if (cam == null || knobCollider == null) return false;

        Ray ray = cam.ScreenPointToRay(GetMousePosition());
        if (Physics.Raycast(ray, out RaycastHit hit, maxClickDistance, knobMask, QueryTriggerInteraction.Collide))
        {
            return hit.collider == knobCollider || hit.collider.transform.IsChildOf(knobCollider.transform);
        }
        return false;
    }

    void ApplyAngle(float angle)
    {
        if (knobRotator == null) return;
        knobRotator.localRotation = Quaternion.Euler(0f, angle, 0f);
    }

    void UpdateVisuals(bool on)
    {
        if (ringRenderer != null && ringRenderer.material != null)
        {
            ringRenderer.material.EnableKeyword("_EMISSION");
            Color c = ringLight != null ? ringLight.color : Color.cyan;
            ringRenderer.material.SetColor(EmissionId, on ? c * ringEmissionMultiplier : Color.black);
        }

        if (ringLight != null)
            ringLight.intensity = on ? ringLightIntensity : 0f;
    }

    // ---------- Input helpers (works with new + old input) ----------
    bool GetMouseButtonDown0()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    bool GetMouseButtonUp0()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
        return Input.GetMouseButtonUp(0);
#endif
    }

    bool GetMouseButton0()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
        return Input.GetMouseButton(0);
#endif
    }

    Vector3 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
#else
        return Input.mousePosition;
#endif
    }

    float GetMousePositionX()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue().x : 0f;
#else
        return Input.mousePosition.x;
#endif
    }
}
