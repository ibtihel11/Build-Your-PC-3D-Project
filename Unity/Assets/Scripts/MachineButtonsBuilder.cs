
using UnityEngine;

[ExecuteAlways]
public class MachineButtonsBuilder : MonoBehaviour
{
    [Header("Build (Editor)")]
    [Tooltip("Im Inspector einmal aktivieren -> baut die 3D-Knöpfe im nächsten Update neu.")]
    public bool rebuildNow = false;
    bool pending;

    [Header("Layout (meters)")]
    public Vector3 panelSize = new Vector3(0.35f, 0.20f, 0.04f);
    public float buttonSpacing = 0.18f;

    [Header("Button (meters)")]
    public float buttonRadius = 0.05f;
    public float buttonHeight = 0.03f;
    public float ringRadius = 0.065f;
    public float ringHeight = 0.012f;

    [Header("Colors")]
    public Color startColor = new Color(0.12f, 0.95f, 0.25f, 1f);
    public Color stopColor = new Color(0.95f, 0.15f, 0.15f, 1f);
    public Color panelColor = new Color(0.08f, 0.08f, 0.08f, 1f);

    [Header("Lights")]
    public float lightRange = 2.2f;

    [Header("Output (auto-filled)")]
    public Transform panel;
    public Transform startButtonRoot;
    public Transform stopButtonRoot;

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

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }

        gameObject.name = "MachineButtonsRig";

        // Panel
        panel = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        panel.name = "Panel";
        panel.SetParent(transform, false);
        panel.localScale = panelSize;
        panel.localPosition = Vector3.zero;
        ApplyOpaqueMat(panel.GetComponent<Renderer>(), panelColor);

        float panelTopY = panelSize.y * 0.5f;
        float panelFrontZ = panelSize.z * 0.5f;

        Vector3 startPos = new Vector3(-buttonSpacing * 0.5f, panelTopY, panelFrontZ);
        Vector3 stopPos = new Vector3(+buttonSpacing * 0.5f, panelTopY, panelFrontZ);

        startButtonRoot = CreateButton("StartButton", startPos, startColor);
        stopButtonRoot = CreateButton("StopButton", stopPos, stopColor);

        // Controller anlegen & füllen
        var ctrl = GetComponent<MachineButtonsController3D>();
        if (!ctrl) ctrl = gameObject.AddComponent<MachineButtonsController3D>();

        // <<< NEUE FELDNAMEN >>>
        ctrl.startButtonRoot = startButtonRoot;
        ctrl.stopButtonRoot = stopButtonRoot;

        ctrl.startPlunger = startButtonRoot.Find("Plunger");
        ctrl.stopPlunger = stopButtonRoot.Find("Plunger");

        ctrl.startRingRenderer = startButtonRoot.Find("Ring")?.GetComponent<Renderer>();
        ctrl.stopRingRenderer = stopButtonRoot.Find("Ring")?.GetComponent<Renderer>();

        ctrl.startLight = startButtonRoot.Find("Lamp")?.GetComponent<Light>();
        ctrl.stopLight = stopButtonRoot.Find("Lamp")?.GetComponent<Light>();
    }

    Transform CreateButton(string rootName, Vector3 localPos, Color color)
    {
        var root = new GameObject(rootName).transform;
        root.SetParent(transform, false);
        root.localPosition = localPos;
        root.localRotation = Quaternion.identity;

        // Ring (LED-Ring)
        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Ring";
        ring.transform.SetParent(root, false);
        ring.transform.localScale = new Vector3(ringRadius * 2f, ringHeight * 0.5f, ringRadius * 2f);
        ring.transform.localPosition = new Vector3(0, ringHeight * 0.5f, 0);
        ApplyEmissiveMat(ring.GetComponent<Renderer>(), color, emissionOn: false);

        // Plunger
        var plunger = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        plunger.name = "Plunger";
        plunger.transform.SetParent(root, false);
        plunger.transform.localScale = new Vector3(buttonRadius * 2f, buttonHeight * 0.5f, buttonRadius * 2f);
        plunger.transform.localPosition = new Vector3(0, ringHeight + buttonHeight * 0.5f, 0);
        ApplyOpaqueMat(plunger.GetComponent<Renderer>(), color);

        // Cap (sichtbarer)
        var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cap.name = "Cap";
        cap.transform.SetParent(plunger.transform, false);
        cap.transform.localScale = new Vector3(1f, 0.35f, 1f);
        cap.transform.localPosition = new Vector3(0, buttonHeight * 0.45f, 0);
        ApplyOpaqueMat(cap.GetComponent<Renderer>(), Color.Lerp(color, Color.white, 0.25f));

        // Light (start OFF)
        var lamp = new GameObject("Lamp");
        lamp.transform.SetParent(root, false);
        lamp.transform.localPosition = new Vector3(0, ringHeight + buttonHeight + 0.03f, 0);

        var l = lamp.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.range = lightRange;
        l.intensity = 0f;

        return root;
    }

    void ApplyOpaqueMat(Renderer r, Color color)
    {
        if (!r) return;
        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Glossiness", 0.65f);
        mat.SetFloat("_Metallic", 0.05f);
        r.sharedMaterial = mat;
    }

    void ApplyEmissiveMat(Renderer r, Color emissionColor, bool emissionOn)
    {
        if (!r) return;
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emissionOn ? emissionColor * 3.0f : Color.black);
        mat.SetFloat("_Glossiness", 0.8f);
        r.sharedMaterial = mat;
    }
}
