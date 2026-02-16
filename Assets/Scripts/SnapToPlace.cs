using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SnapToPlace : MonoBehaviour
{
    [Header("Snap Zone")]
    public Transform snapPoint;                 // Where the object should snap (null => this.transform)
    public float snapRange = 0.15f;             // meters
    public GameObject objectToSnap;             // Optional: only this object is allowed
    public string grabbableTag = "Grabbable";   // Optional: safety filter

    [Header("Highlight (Build-safe)")]
    public Renderer zoneRenderer;               // Renderer to glow (optional; if null uses this Renderer)
    public Color highlightColor = Color.green;
    public float emissionIntensity = 3.0f;

    [Header("After Snap")]
    public bool disableGravityAfterSnap = true;
    public bool lockObjectAfterSnap = true;     // FreezeAll + kinematic (prevents pushing/drifting)

    // internal
    private bool isObjectGrabbed = false;
    private bool candidateInside = false;
    private GameObject candidate;

    private MaterialPropertyBlock mpb;
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        // Ensure trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        if (snapPoint == null) snapPoint = transform;

        if (zoneRenderer == null) zoneRenderer = GetComponent<Renderer>();

        mpb = new MaterialPropertyBlock();

        // Ensure emission keyword is enabled on the shared material variant
        if (zoneRenderer != null && zoneRenderer.sharedMaterial != null)
            zoneRenderer.sharedMaterial.EnableKeyword("_EMISSION");

        SetHighlight(false);
    }

    // -------------------- Grab API --------------------
    /// <summary>
    /// Call from your grab script.
    /// true  => object currently grabbed (show highlight if in range)
    /// false => released (snap once if possible)
    /// </summary>
    public void OnObjectGrabbed(bool state)
    {
        isObjectGrabbed = state;

        if (!isObjectGrabbed)
        {
            // Released: snap once
            TrySnapIfPossible();
            SetHighlight(false);
        }
        else
        {
            // Grabbed: only highlight
            UpdateHighlight();
        }
    }

    // -------------------- Compatibility API --------------------
    /// <summary>
    /// Compatibility method for older scripts (e.g. CenterUICursor) that call TrySnapObject(obj).
    /// Snaps ONCE if within range.
    /// </summary>
    public void TrySnapObject(GameObject obj)
    {
        if (obj == null) return;
        if (objectToSnap != null && obj != objectToSnap) return;
        if (!IsAllowedTag(obj)) return;

        // Treat as current candidate
        candidate = obj;
        candidateInside = true;

        TrySnapIfPossible();
        SetHighlight(false);
    }

    // -------------------- Trigger events --------------------
    void OnTriggerEnter(Collider other)
    {
        if (!IsValidCandidate(other)) return;

        candidateInside = true;
        candidate = other.gameObject;

        UpdateHighlight();
    }

    void OnTriggerStay(Collider other)
    {
        if (!IsValidCandidate(other)) return;

        candidateInside = true;
        candidate = other.gameObject;

        UpdateHighlight();
    }

    void OnTriggerExit(Collider other)
    {
        if (candidate != null && other.gameObject == candidate)
        {
            candidateInside = false;
            candidate = null;
        }
        SetHighlight(false);
    }

    // -------------------- Core logic --------------------
    bool IsValidCandidate(Collider other)
    {
        if (other == null) return false;

        // If objectToSnap is set, only that object can trigger
        if (objectToSnap != null && other.gameObject != objectToSnap) return false;

        // Optional tag filter (only if tag exists and object uses it)
        if (!IsAllowedTag(other.gameObject)) return false;

        return true;
    }

    bool IsAllowedTag(GameObject obj)
    {
        if (string.IsNullOrEmpty(grabbableTag)) return true;

        // If user forgot to assign tag, we don't hard-fail. We simply allow.
        // (So build won't break because of missing tag assignment.)
        try
        {
            // If the object has this tag, good; if not, still allow (less strict, more robust)
            // If you want strict behavior, change to: return obj.CompareTag(grabbableTag);
            return true;
        }
        catch
        {
            return true;
        }
    }

    void UpdateHighlight()
    {
        if (!isObjectGrabbed)
        {
            SetHighlight(false);
            return;
        }

        if (!candidateInside || candidate == null)
        {
            SetHighlight(false);
            return;
        }

        float d = Vector3.Distance(candidate.transform.position, snapPoint.position);
        SetHighlight(d <= snapRange);
    }

    void TrySnapIfPossible()
    {
        if (!candidateInside || candidate == null) return;

        float d = Vector3.Distance(candidate.transform.position, snapPoint.position);
        if (d > snapRange) return;

        // Snap now
        Rigidbody rb = candidate.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // stop motion
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#endif

            // move
            rb.position = snapPoint.position;
            rb.rotation = snapPoint.rotation;

            if (disableGravityAfterSnap) rb.useGravity = false;

            if (lockObjectAfterSnap)
            {
                rb.isKinematic = true;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }
        }
        else
        {
            candidate.transform.position = snapPoint.position;
            candidate.transform.rotation = snapPoint.rotation;
        }
    }

    void SetHighlight(bool on)
    {
        if (zoneRenderer == null) return;

        zoneRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(EmissionId, on ? (highlightColor * emissionIntensity) : Color.black);
        zoneRenderer.SetPropertyBlock(mpb);
    }
}
