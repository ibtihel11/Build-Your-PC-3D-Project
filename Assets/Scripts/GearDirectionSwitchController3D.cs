using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GearDirectionSwitchController3D : MonoBehaviour
{
    [Header("Wiring")]
    public GearBoxDirectDrive gearbox;      // im Inspector zuweisen
    public Camera raycastCamera;            // optional (empfohlen)

    [Header("References (auto from Builder)")]
    public Transform leverPivot;
    public Collider clickCollider;
    public Light ledForward;
    public Light ledReverse;

    [Header("Mouse (after ESC)")]
    public bool requireCursorUnlocked = true;
    public bool unlockCursorOnEsc = true;
    public float maxClickDistance = 10f;
    public LayerMask mask = ~0;

    [Header("Lever Angles")]
    public float forwardAngle = -25f;   // lever tilt
    public float reverseAngle = +25f;

    [Header("Animation")]
    public float leverLerpSpeed = 16f;

    [Header("LED")]
    public float ledIntensity = 2.5f;

    bool isReverse;

    void Start()
    {
        if (gearbox != null) isReverse = gearbox.IsReverse;
        ApplyStateImmediate();
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
            AnimateLever();
            return;
        }

        if (MouseDownThisFrame() && HitMeRaycastAll())
        {
            isReverse = !isReverse;
            if (gearbox != null) gearbox.SetReverse(isReverse);
            ApplyLeds();
        }

        AnimateLever();
    }

    void ApplyStateImmediate()
    {
        ApplyLeds();
        if (leverPivot != null)
        {
            float a = isReverse ? reverseAngle : forwardAngle;
            leverPivot.localRotation = Quaternion.Euler(a, 0f, 0f);
        }
    }

    void AnimateLever()
    {
        if (leverPivot == null) return;
        float a = isReverse ? reverseAngle : forwardAngle;
        var target = Quaternion.Euler(a, 0f, 0f);
        leverPivot.localRotation = Quaternion.Slerp(leverPivot.localRotation, target, 1f - Mathf.Exp(-leverLerpSpeed * Time.deltaTime));
    }

    void ApplyLeds()
    {
        if (ledForward != null) ledForward.intensity = isReverse ? 0f : ledIntensity;
        if (ledReverse != null) ledReverse.intensity = isReverse ? ledIntensity : 0f;
    }

    bool HitMeRaycastAll()
    {
        if (clickCollider == null) return false;

        Camera cam = raycastCamera != null ? raycastCamera : Camera.main;
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(GetMousePos());
        var hits = Physics.RaycastAll(ray, maxClickDistance, mask, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0) return false;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            if (h.collider == clickCollider || h.collider.transform.IsChildOf(clickCollider.transform))
                return true;
        }
        return false;
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
