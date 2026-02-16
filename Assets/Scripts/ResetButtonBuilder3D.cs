using UnityEngine;

[ExecuteAlways]
public class ResetButtonBuilder3D : MonoBehaviour
{
    [Header("Build")]
    public bool rebuildNow = false;
    bool pending;

    [Header("Sizes (m)")]
    public Vector3 baseSize = new Vector3(0.14f, 0.06f, 0.10f);
    public float ringRadius = 0.055f;
    public float ringHeight = 0.010f;
    public float plungerRadius = 0.042f;
    public float plungerHeight = 0.028f;

    [Header("Look")]
    public Color baseColor = new Color(0.10f, 0.10f, 0.10f, 1f);
    public Color ringColor = new Color(0.12f, 0.12f, 0.12f, 1f);
    public Color lightColor = new Color(0.95f, 0.95f, 0.95f, 1f);

    [Header("Label")]
    public string labelText = "RESET";
    public float labelSize = 0.018f;

    [Header("Auto-wiring output")]
    public Transform plunger;
    public Renderer ringRenderer;
    public Light ringLight;
    public Collider clickCollider;

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

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
        // clear children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var go = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }

        transform.localScale = Vector3.one;
        gameObject.name = "ResetButtonRig";

        // base
        var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObj.name = "Base";
        baseObj.transform.SetParent(transform, false);
        baseObj.transform.localScale = baseSize;
        baseObj.transform.localPosition = Vector3.zero;
        ApplyOpaque(baseObj.GetComponent<Renderer>(), baseColor, 0.55f);

        float topY = baseSize.y * 0.5f;

        // root
        var root = new GameObject("ResetButton").transform;
        root.SetParent(transform, false);
        root.localPosition = new Vector3(0f, topY, 0f);
        root.localRotation = Quaternion.identity;

        // ring
        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Ring";
        ring.transform.SetParent(root, false);
        ring.transform.localScale = new Vector3(ringRadius * 2f, ringHeight * 0.5f, ringRadius * 2f);
        ring.transform.localPosition = new Vector3(0f, ringHeight * 0.5f, 0f);
        ringRenderer = ring.GetComponent<Renderer>();
        ApplyEmissive(ringRenderer, ringColor, lightColor, false);

        // plunger
        var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        p.name = "Plunger";
        p.transform.SetParent(root, false);
        p.transform.localScale = new Vector3(plungerRadius * 2f, plungerHeight * 0.5f, plungerRadius * 2f);
        p.transform.localPosition = new Vector3(0f, ringHeight + plungerHeight * 0.5f, 0f);
        ApplyOpaque(p.GetComponent<Renderer>(), new Color(0.18f, 0.18f, 0.18f, 1f), 0.75f);
        plunger = p.transform;

        // bigger click collider (SphereCollider, trigger -> weniger physikalische Nebenwirkungen)
        var sc = root.gameObject.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = plungerRadius * 1.25f;
        sc.center = new Vector3(0f, ringHeight + plungerHeight * 0.55f, 0f);
        clickCollider = sc;

        // light
        var lamp = new GameObject("Lamp");
        lamp.transform.SetParent(root, false);
        lamp.transform.localPosition = new Vector3(0f, ringHeight + plungerHeight + 0.03f, 0f);

        ringLight = lamp.AddComponent<Light>();
        ringLight.type = LightType.Point;
        ringLight.color = lightColor;
        ringLight.range = 2.0f;
        ringLight.intensity = 0f;

        // label (TextMesh) – Unity 6: LegacyRuntime.ttf verwenden
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(root, false);
        labelGo.transform.localPosition = new Vector3(0f, ringHeight + plungerHeight + 0.004f, plungerRadius * 0.55f);
        labelGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        var tm = labelGo.AddComponent<TextMesh>();
        tm.text = labelText;
        tm.characterSize = labelSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        tm.fontSize = 64;
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // controller add + wire
        var ctrl = GetComponent<ResetButtonController3D>();
        if (!ctrl) ctrl = gameObject.AddComponent<ResetButtonController3D>();

        ctrl.plunger = plunger;
        ctrl.ringRenderer = ringRenderer;
        ctrl.ringLight = ringLight;
        ctrl.clickCollider = clickCollider;
    }

    void ApplyOpaque(Renderer r, Color c, float gloss)
    {
        if (!r) return;
        var mat = new Material(Shader.Find("Standard"));
        mat.color = c;
        mat.SetFloat("_Glossiness", gloss);
        mat.SetFloat("_Metallic", 0.05f);
        r.sharedMaterial = mat;
    }

    void ApplyEmissive(Renderer r, Color baseC, Color emissionC, bool on)
    {
        if (!r) return;
        var mat = new Material(Shader.Find("Standard"));
        mat.color = baseC;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor(EmissionId, on ? emissionC * 3.0f : Color.black);
        mat.SetFloat("_Glossiness", 0.85f);
        r.sharedMaterial = mat;
    }
}
