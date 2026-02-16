using System;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GearboxExplodedViewCustom : MonoBehaviour
{
    [Serializable]
    public class ExplodeEntry
    {
        public Transform part;

        [Tooltip("Explode offset in WORLD space (X,Y,Z). Example: (0.2, 0.1, -0.3) moves in all 3 directions.")]
        public Vector3 offsetWorld = new Vector3(0.2f, 0f, 0f);
    }

    [Header("Parts (per-part XYZ offset)")]
    public List<ExplodeEntry> entries = new List<ExplodeEntry>();

    [Header("Toggle")]
    public KeyCode toggleKey = KeyCode.X;

    [Header("Animation")]
    public float animationDuration = 0.35f;

    [Tooltip("While exploded (and during animation), parts are held at target pose.")]
    public bool holdPartsWhileExploded = true;

    [Tooltip("If TRUE: when explode starts, current pose becomes the base pose.")]
    public bool captureBaseOnExplode = true;

    [Header("Physics compatibility")]
    [Tooltip("If a part has a Rigidbody, it will be set to kinematic during explode/animation so it can move visibly.")]
    public bool forceKinematicDuringExplode = true;

    [Header("Debug")]
    public bool debugLog = false;

    bool exploded = false;
    bool animating = false;
    float animT = 0f;

    readonly Dictionary<Transform, Vector3> basePos = new();
    readonly Dictionary<Transform, Quaternion> baseRot = new();
    readonly Dictionary<Transform, Vector3> targetPos = new();
    readonly Dictionary<Transform, Quaternion> targetRot = new();

    readonly Dictionary<Rigidbody, bool> rbWasKinematic = new();

    void OnEnable()
    {
        Cleanup();
        CaptureBasePose();
        BuildTargets();
    }

    void OnDisable()
    {
        RestoreRigidbodies();
    }

    void Update()
    {
        if (TogglePressedThisFrame())
        {
            if (debugLog) Debug.Log($"[Explode] Toggle pressed. Entries={entries.Count}", this);
            ToggleExplode();
        }

        if (animating)
        {
            AnimateStep();
            return;
        }

        // Hold pose only when exploded (so normal mode stays grabbable)
        if (exploded && holdPartsWhileExploded)
        {
            ApplyPoseInstant(targetPos, targetRot);
        }
        else
        {
            RestoreRigidbodies();
        }
    }

    bool TogglePressedThisFrame()
    {
        // Legacy Input (works with Input Handling = Both)
        if (Input.GetKeyDown(toggleKey)) return true;

#if ENABLE_INPUT_SYSTEM
        // Input System fallback
        if (Keyboard.current != null && toggleKey == KeyCode.X && Keyboard.current.xKey.wasPressedThisFrame)
            return true;
#endif
        return false;
    }

    public void ToggleExplode()
    {
        Cleanup();
        if (entries.Count == 0)
        {
            Debug.LogWarning("[Explode] No entries assigned (Parts list is empty).", this);
            return;
        }

        // Starting explode: capture current assembled pose (respects moved/grabbed positions)
        if (!exploded && captureBaseOnExplode)
            CaptureBasePose();

        BuildTargets();

        exploded = !exploded;
        animating = true;
        animT = 0f;

        // During animation (explode or collapse) we want scripted motion visible
        PrepareRigidbodiesForExplode();
    }

    void AnimateStep()
    {
        animT += Time.deltaTime;
        float u = (animationDuration <= 0.001f) ? 1f : Mathf.Clamp01(animT / animationDuration);

        if (exploded)
            ApplyPoseLerp(basePos, baseRot, targetPos, targetRot, u);
        else
            ApplyPoseLerp(targetPos, targetRot, basePos, baseRot, u);

        if (u >= 1f)
        {
            animating = false;

            if (exploded) ApplyPoseInstant(targetPos, targetRot);
            else ApplyPoseInstant(basePos, baseRot);

            if (!exploded) RestoreRigidbodies();
        }
    }

    void Cleanup()
    {
        entries.RemoveAll(e => e == null || e.part == null);
    }

    void CaptureBasePose()
    {
        basePos.Clear();
        baseRot.Clear();

        foreach (var e in entries)
        {
            if (e.part == null) continue;
            basePos[e.part] = e.part.position;
            baseRot[e.part] = e.part.rotation;
        }
    }

    void BuildTargets()
    {
        targetPos.Clear();
        targetRot.Clear();

        foreach (var e in entries)
        {
            if (e.part == null) continue;

            if (!basePos.ContainsKey(e.part))
            {
                basePos[e.part] = e.part.position;
                baseRot[e.part] = e.part.rotation;
            }

            // ✅ Now we use XYZ offset directly
            targetPos[e.part] = basePos[e.part] + e.offsetWorld;
            targetRot[e.part] = baseRot[e.part];
        }
    }

    void PrepareRigidbodiesForExplode()
    {
        if (!forceKinematicDuringExplode) return;

        foreach (var e in entries)
        {
            if (e.part == null) continue;
            var rb = e.part.GetComponent<Rigidbody>();
            if (rb == null) continue;

            if (!rbWasKinematic.ContainsKey(rb))
                rbWasKinematic[rb] = rb.isKinematic;

            rb.isKinematic = true;
        }
    }

    void RestoreRigidbodies()
    {
        if (!forceKinematicDuringExplode) return;

        foreach (var kv in rbWasKinematic)
        {
            if (kv.Key != null)
                kv.Key.isKinematic = kv.Value;
        }
        rbWasKinematic.Clear();
    }

    void ApplyPoseInstant(Dictionary<Transform, Vector3> pos, Dictionary<Transform, Quaternion> rot)
    {
        foreach (var e in entries)
        {
            if (e.part == null) continue;
            if (!pos.TryGetValue(e.part, out var p)) continue;
            if (!rot.TryGetValue(e.part, out var r)) r = e.part.rotation;

            var rb = e.part.GetComponent<Rigidbody>();
            if (rb != null && forceKinematicDuringExplode)
            {
                rb.position = p;
                rb.rotation = r;
            }
            else
            {
                e.part.position = p;
                e.part.rotation = r;
            }
        }
    }

    void ApplyPoseLerp(
        Dictionary<Transform, Vector3> fromPos, Dictionary<Transform, Quaternion> fromRot,
        Dictionary<Transform, Vector3> toPos, Dictionary<Transform, Quaternion> toRot,
        float t)
    {
        foreach (var e in entries)
        {
            if (e.part == null) continue;

            if (!fromPos.TryGetValue(e.part, out var p0)) p0 = e.part.position;
            if (!toPos.TryGetValue(e.part, out var p1)) p1 = e.part.position;

            if (!fromRot.TryGetValue(e.part, out var r0)) r0 = e.part.rotation;
            if (!toRot.TryGetValue(e.part, out var r1)) r1 = e.part.rotation;

            Vector3 p = Vector3.Lerp(p0, p1, t);
            Quaternion r = Quaternion.Slerp(r0, r1, t);

            var rb = e.part.GetComponent<Rigidbody>();
            if (rb != null && forceKinematicDuringExplode)
            {
                rb.position = p;
                rb.rotation = r;
            }
            else
            {
                e.part.position = p;
                e.part.rotation = r;
            }
        }
    }
}
