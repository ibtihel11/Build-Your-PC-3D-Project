using UnityEngine;

[ExecuteAlways]
public class GearDirectionSwitchBuilder3D : MonoBehaviour
{
    [Header("Build")]
    public bool rebuildNow = false;
    bool pending;

    [Header("Sizes (m)")]
    public Vector3 baseSize = new Vector3(0.20f, 0.05f, 0.12f);
    public Vector3 rockerSize = new Vector3(0.10f, 0.02f, 0.05f);
    public Vector3 leverSize = new Vector3(0.012f, 0.055f, 0.012f);
    public float leverTopKnobRadius = 0.015f;

    [Header("Look")]
    public Color baseColor = new Color(0.10f, 0.10f, 0.10f, 1f);
    public Color rockerColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    public Color leverColor = new Color(0.80f, 0.80f, 0.80f, 1f);

    [Header("Label")]
    public float labelCharSize = 0.015f;

    [Header("Output (auto wired)")]
    public Transform leverPivot;
    public Transform leverVisual;
    public Collider clickCollider;
    public Light ledForward;
    public Light ledReverse;

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
        gameObject.name = "GearDirectionSwitchRig";

        // Base
        var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObj.name = "Base";
        baseObj.transform.SetParent(transform, false);
        baseObj.transform.localScale = baseSize;
        baseObj.transform.localPosition = Vector3.zero;
        ApplyStandard(baseObj.GetComponent<Renderer>(), baseColor, 0.6f);

        float topY = baseSize.y * 0.5f;

        // Rocker plate
        var rocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rocker.name = "Rocker";
        rocker.transform.SetParent(transform, false);
        rocker.transform.localScale = rockerSize;
        rocker.transform.localPosition = new Vector3(0f, topY + rockerSize.y * 0.5f, 0f);
        ApplyStandard(rocker.GetComponent<Renderer>(), rockerColor, 0.75f);

        // Lever pivot
        leverPivot = new GameObject("LeverPivot").transform;
        leverPivot.SetParent(transform, false);
        leverPivot.localPosition = new Vector3(0f, topY + rockerSize.y, 0f);
        leverPivot.localRotation = Quaternion.identity;

        // Lever visual
        leverVisual = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        leverVisual.name = "Lever";
        leverVisual.SetParent(leverPivot, false);
        leverVisual.localScale = leverSize;
        leverVisual.localPosition = new Vector3(0f, leverSize.y * 0.5f, 0f);
        ApplyStandard(leverVisual.GetComponent<Renderer>(), leverColor, 0.8f);

        // Top knob
        var knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        knob.name = "LeverKnob";
        knob.transform.SetParent(leverPivot, false);
        knob.transform.localScale = Vector3.one * (leverTopKnobRadius * 2f);
        knob.transform.localPosition = new Vector3(0f, leverSize.y + leverTopKnobRadius, 0f);
        ApplyStandard(knob.GetComponent<Renderer>(), leverColor, 0.9f);

        // Click collider (Trigger, so no physics pushing)
        var box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(0.16f, 0.18f, 0.12f);
        box.center = new Vector3(0f, topY + 0.09f, 0f);
        clickCollider = box;

        // LEDs
        ledForward = CreateLed("LED_FWD", transform, new Vector3(-0.06f, topY + 0.01f, +0.045f), Color.green);
        ledReverse = CreateLed("LED_REV", transform, new Vector3(+0.06f, topY + 0.01f, +0.045f), Color.red);
        ledForward.intensity = 0f;
        ledReverse.intensity = 0f;

        // Labels (FWD / REV)
        CreateLabel(transform, "FWD", new Vector3(-0.06f, topY + 0.001f, +0.03f), labelCharSize);
        CreateLabel(transform, "REV", new Vector3(+0.06f, topY + 0.001f, +0.03f), labelCharSize);

        // Auto add + wire controller
        var ctrl = GetComponent<GearDirectionSwitchController3D>();
        if (!ctrl) ctrl = gameObject.AddComponent<GearDirectionSwitchController3D>();

        ctrl.leverPivot = leverPivot;
        ctrl.clickCollider = clickCollider;
        ctrl.ledForward = ledForward;
        ctrl.ledReverse = ledReverse;
    }

    Light CreateLed(string name, Transform parent, Vector3 localPos, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.range = 1.2f;
        l.intensity = 0f;
        return l;
    }

    void CreateLabel(Transform parent, string text, Vector3 localPos, float charSize)
    {
        var go = new GameObject("Label_" + text);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.characterSize = charSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        tm.fontSize = 64;
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void ApplyStandard(Renderer r, Color c, float gloss)
    {
        if (!r) return;
        var mat = new Material(Shader.Find("Standard"));
        mat.color = c;
        mat.SetFloat("_Glossiness", gloss);
        mat.SetFloat("_Metallic", 0.05f);
        r.sharedMaterial = mat;
    }
}
