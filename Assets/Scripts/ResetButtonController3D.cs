using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ResetButtonController3D : MonoBehaviour
{
    [Header("References (auto from Builder)")]
    public Transform plunger;
    public Renderer ringRenderer;
    public Light ringLight;
    public Collider clickCollider;

    [Header("Wiring (assign in Inspector)")]
    public GearBoxDirectDrive gearbox;
    public MachineButtonsController3D startStopButtons;   // <- WICHTIG: hier zuweisen!

    [Header("Click (after ESC)")]
    public bool requireCursorUnlocked = true;
    public bool unlockCursorOnEsc = true;
    public Camera raycastCamera;                  // optional (empfohlen)
    public float maxClickDistance = 10f;
    public LayerMask mask = ~0;

    [Header("Visual feedback")]
    public float pressDepth = 0.010f;
    public float pressSpeed = 18f;
    public float flashSeconds = 0.25f;
    public float emissionMultiplier = 3.0f;
    public float lightIntensity = 3.0f;

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    Vector3 upPos, downPos;
    float flashT;

    void Start()
    {
        if (plunger != null)
        {
            upPos = plunger.localPosition;
            downPos = upPos - Vector3.up * pressDepth;
        }
        SetLit(false);
    }

    void Update()
    {
        if (unlockCursorOnEsc && EscDownThisFrame())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (requireCursorUnlocked && (Cursor.lockState == CursorLockMode.Locked || !Cursor.visible))
        {
            Animate(false);
            TickFlash();
            return;
        }

        if (MouseDownThisFrame() && HitMeRaycastAll())
            DoResetLikeStop();

        Animate(flashT > 0f);
        TickFlash();
    }

    void DoResetLikeStop()
    {
        // 1) Start/Stop-Buttons auf "STOP" setzen -> Start-Leuchte AUS, Stop-Leuchte AN
        if (startStopButtons != null)
            startStopButtons.SetRunning(false);

        // 2) Getriebe wirklich stoppen + RPM auf 0 (damit Torque/RPM-Displays 0 zeigen)
        if (gearbox != null)
        {
            gearbox.StopRun();
            gearbox.SetScrewRpm(0f);
        }

        // Visual feedback
        flashT = flashSeconds;
        SetLit(true);
    }

    bool HitMeRaycastAll()
    {
        if (clickCollider == null) return false;

        Camera cam = raycastCamera != null ? raycastCamera : Camera.main;
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(GetMousePos());

        // ✅ RaycastAll verhindert “Blockieren” durch andere Collider
        var hits = Physics.RaycastAll(ray, maxClickDistance, mask, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0) return false;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
            if (h.collider == clickCollider || h.collider.transform.IsChildOf(clickCollider.transform))
                return true;

        return false;
    }

    void TickFlash()
    {
        if (flashT <= 0f) return;
        flashT -= Time.deltaTime;
        if (flashT <= 0f) SetLit(false);
    }

    void SetLit(bool on)
    {
        if (ringLight != null) ringLight.intensity = on ? lightIntensity : 0f;

        if (ringRenderer != null && ringRenderer.material != null)
        {
            ringRenderer.material.EnableKeyword("_EMISSION");
            Color c = ringLight != null ? ringLight.color : Color.white;
            ringRenderer.material.SetColor(EmissionId, on ? c * emissionMultiplier : Color.black);
        }
    }

    void Animate(bool pressed)
    {
        if (plunger == null) return;
        var target = pressed ? downPos : upPos;
        plunger.localPosition = Vector3.Lerp(plunger.localPosition, target, 1f - Mathf.Exp(-pressSpeed * Time.deltaTime));
    }

    // ---- Input helpers ----
    bool MouseDownThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    bool EscDownThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    Vector3 GetMousePos()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
#else
        return Input.mousePosition;
#endif
    }
}
