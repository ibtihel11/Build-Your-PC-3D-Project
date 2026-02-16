using UnityEngine;

[ExecuteAlways]
public class MachineKnobBuilder : MonoBehaviour
{
    [Header("Build (Editor)")]
    [Tooltip("Im Inspector einmal aktivieren -> baut den Drehknopf im nächsten Update neu.")]
    public bool rebuildNow = false;
    private bool pending;

    [Header("Sizes (meters)")]
    public Vector3 baseSize = new Vector3(0.18f, 0.08f, 0.10f);     // Sockel
    public float knobRadius = 0.045f;
    public float knobHeight = 0.035f;
    public float ringRadius = 0.055f;
    public float ringHeight = 0.010f;

    [Header("Colors")]
    public Color baseColor = new Color(0.10f, 0.10f, 0.10f, 1f);
    public Color knobColor = new Color(0.22f, 0.22f, 0.22f, 1f);
    public Color ringColor = new Color(0.10f, 0.90f, 1.00f, 1f);   // cyan/bläulich

    [Header("Light")]
    public float ringLightRange = 1.2f;

    [Header("Output (auto-filled)")]
    public Transform knobRotator;     // das Teil, das gedreht wird
    public Collider knobCollider;     // Klickfläche
    public Renderer ringRenderer;
    public Light ringLight;

    void OnValidate()
    {
        if (rebuildNow)
        {
            rebuildNow = false;
            pending = true;
        }
    }

    void Update()
    {
        if (!pending) return;
        pending = false;
        Build();
    }

    public void Build()
    {
        transform.localScale = Vector3.one;

        // Children löschen
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }

        gameObject.name = "MachineKnobRig";

        // --- Base ---
        var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObj.name = "Base";
        baseObj.transform.SetParent(transform, false);
        baseObj.transform.localScale = baseSize;
        baseObj.transform.localPosition = Vector3.zero;
        ApplyOpaqueMat(baseObj.GetComponent<Renderer>(), baseColor);

        float topY = baseSize.y * 0.5f;

        // --- Rotator Root (dreht sich) ---
        var rot = new GameObject("KnobRotator");
        rot.transform.SetParent(transform, false);
        rot.transform.localPosition = new Vector3(0f, topY, 0f);
        rot.transform.localRotation = Quaternion.identity;
        knobRotator = rot.transform;

        // --- LED Ring (Emission) ---
        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Ring";
        ring.transform.SetParent(knobRotator, false);
        ring.transform.localScale = new Vector3(ringRadius * 2f, ringHeight * 0.5f, ringRadius * 2f);
        ring.transform.localPosition = new Vector3(0f, ringHeight * 0.5f, 0f);
        ringRenderer = ring.GetComponent<Renderer>();
        ApplyEmissiveRingMat(ringRenderer, ringColor, emissionOn: false);

        // --- Knob Body ---
        var knob = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        knob.name = "Knob";
        knob.transform.SetParent(knobRotator, false);
        knob.transform.localScale = new Vector3(knobRadius * 2f, knobHeight * 0.5f, knobRadius * 2f);
        knob.transform.localPosition = new Vector3(0f, ringHeight + knobHeight * 0.5f, 0f);
        ApplyOpaqueMat(knob.GetComponent<Renderer>(), knobColor, glossy: 0.75f);

        // --- Marker (zeigt Winkel) ---
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "Marker";
        marker.transform.SetParent(knobRotator, false);
        marker.transform.localScale = new Vector3(0.01f, 0.01f, 0.03f);
        marker.transform.localPosition = new Vector3(0f, ringHeight + knobHeight + 0.01f, knobRadius * 0.65f);
        ApplyOpaqueMat(marker.GetComponent<Renderer>(), Color.white, glossy: 0.2f);

        // --- Collider (groß genug zum Anklicken) ---
        // Wir machen einen extra SphereCollider auf dem Rotator, damit man gut trifft.
        var sc = knobRotator.gameObject.AddComponent<SphereCollider>();
        sc.radius = knobRadius * 1.35f;
        sc.center = new Vector3(0f, ringHeight + knobHeight * 0.55f, 0f);
        knobCollider = sc;

        // --- Ring Light (optional) ---
        var lampObj = new GameObject("RingLight");
        lampObj.transform.SetParent(transform, false);
        lampObj.transform.localPosition = new Vector3(0f, topY + 0.09f, 0f);
        ringLight = lampObj.AddComponent<Light>();
        ringLight.type = LightType.Point;
        ringLight.color = ringColor;
        ringLight.range = ringLightRange;
        ringLight.intensity = 0f; // Controller schaltet

        // --- Auto: Controller hinzufügen & füllen ---
        var ctrl = GetComponent<MachineKnobController3D>();
        if (!ctrl) ctrl = gameObject.AddComponent<MachineKnobController3D>();

        ctrl.knobRotator = knobRotator;
        ctrl.knobCollider = knobCollider;
        ctrl.ringRenderer = ringRenderer;
        ctrl.ringLight = ringLight;
    }

    // ---------- Materials (Built-in / Standard) ----------
    void ApplyOpaqueMat(Renderer r, Color color, float glossy = 0.6f)
    {
        if (!r) return;
        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Glossiness", glossy);
        mat.SetFloat("_Metallic", 0.05f);
        r.sharedMaterial = mat;
    }

    void ApplyEmissiveRingMat(Renderer r, Color emissionColor, bool emissionOn)
    {
        if (!r) return;
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.10f, 0.10f, 0.10f, 1f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emissionOn ? emissionColor * 2.8f : Color.black);
        mat.SetFloat("_Glossiness", 0.85f);
        r.sharedMaterial = mat;
    }
}
