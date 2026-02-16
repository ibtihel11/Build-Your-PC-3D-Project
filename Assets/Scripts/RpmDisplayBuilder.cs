using UnityEngine;

[ExecuteAlways]
public class RpmDisplayBuilder : MonoBehaviour
{
    [Header("Build (Editor)")]
    public bool rebuildNow = false;
    private bool pending;

    [Header("Sizes (meters)")]
    public Vector3 panelSize = new Vector3(0.22f, 0.10f, 0.01f);
    public float barWidth = 0.18f;
    public float barHeight = 0.02f;
    public float barY = 0.0f;

    [Header("RPM Ranges")]
    public float rpmMin = 0f;
    public float rpmGreenMax = 500f;
    public float rpmYellowMax = 800f;
    public float rpmMax = 1000f;

    [Header("Colors")]
    public Color panelColor = new Color(0.08f, 0.08f, 0.08f, 1f);
    public Color greenColor = new Color(0.10f, 0.90f, 0.20f, 1f);
    public Color yellowColor = new Color(0.95f, 0.85f, 0.10f, 1f);
    public Color redColor = new Color(0.95f, 0.15f, 0.15f, 1f);
    public Color markerColor = Color.white;

    [Header("Output (auto-filled)")]
    public Transform barRoot;
    public Transform marker;
    public TextMesh rpmText;

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

        gameObject.name = "RpmDisplayRig";

        // Panel
        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "Panel";
        panel.transform.SetParent(transform, false);
        panel.transform.localScale = panelSize;
        panel.transform.localPosition = Vector3.zero;
        ApplyUnlit(panel.GetComponent<Renderer>(), panelColor);

        // BarRoot
        barRoot = new GameObject("BarRoot").transform;
        barRoot.SetParent(transform, false);
        barRoot.localPosition = new Vector3(0f, barY, panelSize.z * 0.6f);
        barRoot.localRotation = Quaternion.identity;

        // Segment widths proportional to RPM ranges
        float total = Mathf.Max(1f, rpmMax - rpmMin);
        float wGreen = barWidth * Mathf.Clamp01((rpmGreenMax - rpmMin) / total);
        float wYellow = barWidth * Mathf.Clamp01((rpmYellowMax - rpmGreenMax) / total);
        float wRed = barWidth * Mathf.Clamp01((rpmMax - rpmYellowMax) / total);

        // Segments: left to right
        float xLeft = -barWidth * 0.5f;
        CreateSegment("Green", xLeft + wGreen * 0.5f, wGreen, greenColor);
        xLeft += wGreen;
        CreateSegment("Yellow", xLeft + wYellow * 0.5f, wYellow, yellowColor);
        xLeft += wYellow;
        CreateSegment("Red", xLeft + wRed * 0.5f, wRed, redColor);

        // Marker (zeigt aktuellen RPM Wert)
        marker = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        marker.name = "Marker";
        marker.SetParent(barRoot, false);
        marker.localScale = new Vector3(0.006f, barHeight * 1.4f, 0.006f);
        marker.localPosition = new Vector3(-barWidth * 0.5f, barHeight * 0.8f, 0f);
        ApplyUnlit(marker.GetComponent<Renderer>(), markerColor);

        // Text
        var textGO = new GameObject("RPM_Text");
        textGO.transform.SetParent(transform, false);
        textGO.transform.localPosition = new Vector3(0f, panelSize.y * 0.25f, panelSize.z * 0.65f);
        textGO.transform.localRotation = Quaternion.identity;

        rpmText = textGO.AddComponent<TextMesh>();
        rpmText.text = "RPM: 0";
        rpmText.anchor = TextAnchor.MiddleCenter;
        rpmText.alignment = TextAlignment.Center;
        rpmText.fontSize = 120;
        rpmText.characterSize = 0.02f;
        rpmText.color = greenColor;

        // Unity 6: Built-in Font
        rpmText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Controller auto add + referenzen setzen
        var ctrl = GetComponent<RpmDisplayController>();
        if (!ctrl) ctrl = gameObject.AddComponent<RpmDisplayController>();

        ctrl.rpmText = rpmText;
        ctrl.marker = marker;
        ctrl.barWidth = barWidth;

        ctrl.rpmMin = rpmMin;
        ctrl.rpmGreenMax = rpmGreenMax;
        ctrl.rpmYellowMax = rpmYellowMax;
        ctrl.rpmMax = rpmMax;

        ctrl.greenColor = greenColor;
        ctrl.yellowColor = yellowColor;
        ctrl.redColor = redColor;
    }

    void CreateSegment(string name, float xCenter, float width, Color c)
    {
        var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seg.name = name;
        seg.transform.SetParent(barRoot, false);
        seg.transform.localScale = new Vector3(Mathf.Max(0.001f, width), barHeight, 0.004f);
        seg.transform.localPosition = new Vector3(xCenter, 0f, 0f);
        ApplyUnlit(seg.GetComponent<Renderer>(), c);

        // Collider für Segmente nicht nötig
        var col = seg.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }
    }

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
