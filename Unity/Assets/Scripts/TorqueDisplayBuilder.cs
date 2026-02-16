using UnityEngine;

[ExecuteAlways]
public class TorqueDisplayBuilder : MonoBehaviour
{
    [Header("Build (Editor)")]
    public bool rebuildNow = false;
    private bool pending;

    [Header("Sizes (meters)")]
    public Vector3 panelSize = new Vector3(0.28f, 0.14f, 0.01f);
    public float barWidth = 0.22f;
    public float barHeight = 0.02f;
    public float barY = -0.02f;

    [Header("Colors")]
    public Color panelColor = new Color(0.08f, 0.08f, 0.08f, 1f);
    public Color greenColor = new Color(0.10f, 0.90f, 0.20f, 1f);
    public Color yellowColor = new Color(0.95f, 0.85f, 0.10f, 1f);
    public Color redColor = new Color(0.95f, 0.15f, 0.15f, 1f);
    public Color markerColor = Color.white;

    [Header("Output (auto-filled)")]
    public Transform barRoot;
    public Transform marker;
    public TextMesh inputText;
    public TextMesh outputText;

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

        gameObject.name = "TorqueDisplayRig";

        // Panel
        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "Panel";
        panel.transform.SetParent(transform, false);
        panel.transform.localScale = panelSize;
        panel.transform.localPosition = Vector3.zero;
        ApplyUnlit(panel.GetComponent<Renderer>(), panelColor);

        // Titel (nur optisch)
        CreateText("TITLE", "TORQUE (Nm)",
            new Vector3(0f, panelSize.y * 0.33f, panelSize.z * 0.65f),
            120, 0.02f, Color.white);

        // Input / Output
        inputText = CreateText("InputText", "IN: 0 Nm",
            new Vector3(0f, panelSize.y * 0.08f, panelSize.z * 0.65f),
            110, 0.018f, greenColor);

        outputText = CreateText("OutputText", "OUT: 0 Nm",
            new Vector3(0f, -panelSize.y * 0.10f, panelSize.z * 0.65f),
            110, 0.018f, greenColor);

        // BarRoot
        barRoot = new GameObject("BarRoot").transform;
        barRoot.SetParent(transform, false);
        barRoot.localPosition = new Vector3(0f, barY, panelSize.z * 0.60f);

        // Segmente
        CreateSegment("Green", -barWidth * 0.33f, barWidth / 3f, greenColor);
        CreateSegment("Yellow", 0f, barWidth / 3f, yellowColor);
        CreateSegment("Red", +barWidth * 0.33f, barWidth / 3f, redColor);

        // Marker
        marker = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        marker.name = "Marker";
        marker.SetParent(barRoot, false);
        marker.localScale = new Vector3(0.006f, barHeight * 1.6f, 0.006f);
        marker.localPosition = new Vector3(-barWidth * 0.5f, barHeight * 0.8f, 0f);
        ApplyUnlit(marker.GetComponent<Renderer>(), markerColor);

        // Controller verlinken
        var ctrl = GetComponent<TorqueDisplayController>();
        if (!ctrl) ctrl = gameObject.AddComponent<TorqueDisplayController>();

        ctrl.inputText = inputText;
        ctrl.outputText = outputText;
        ctrl.marker = marker;
        ctrl.barWidth = barWidth;

        ctrl.greenColor = greenColor;
        ctrl.yellowColor = yellowColor;
        ctrl.redColor = redColor;
    }

    TextMesh CreateText(string name, string txt, Vector3 pos, int fontSize, float charSize, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = pos;

        var tm = go.AddComponent<TextMesh>();
        tm.text = txt;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = fontSize;
        tm.characterSize = charSize;
        tm.color = color;
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return tm;
    }

    void CreateSegment(string name, float xCenter, float width, Color c)
    {
        var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seg.name = name;
        seg.transform.SetParent(barRoot, false);
        seg.transform.localScale = new Vector3(Mathf.Max(0.001f, width), barHeight, 0.004f);
        seg.transform.localPosition = new Vector3(xCenter, 0f, 0f);
        ApplyUnlit(seg.GetComponent<Renderer>(), c);

        var col = seg.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }
    }

    // ✅ nur EINMAL vorhanden
    void ApplyUnlit(Renderer r, Color c)
    {
        if (!r) return;
        var sh = Shader.Find("Unlit/Color");
        if (sh == null) sh = Shader.Find("Standard");
        var mat = new Material(sh);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        r.sharedMaterial = mat;
    }
}
