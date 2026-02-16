using UnityEngine;
using UnityEngine.UI;

public class GearChartLegendBuilder : MonoBehaviour
{
    [Header("Target Chart (optional)")]
    public GearDataTimeSeriesChart chart; // Drag your chart script here (optional)

    [Header("Build")]
    public bool rebuildNow = false;

    [Header("Layout")]
    public Vector2 legendAnchoredPos = new Vector2(12f, -12f); // top-left inside chart
    public Vector2 legendSize = new Vector2(180f, 70f);

    public float rowHeight = 20f;
    public float lineWidth = 22f;
    public float lineHeight = 4f;
    public float fontSize = 14f;

    void OnValidate()
    {
        if (rebuildNow)
        {
            rebuildNow = false;
            Build();
        }
    }

    void Reset()
    {
        // auto-find chart on same object
        if (chart == null) chart = GetComponent<GearDataTimeSeriesChart>();
    }

    public void Build()
    {
        if (chart == null) chart = GetComponent<GearDataTimeSeriesChart>();

        // Remove old legend if exists
        var existing = transform.Find("Legend");
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        // Create legend root
        var legendGO = new GameObject("Legend", typeof(RectTransform));
        legendGO.transform.SetParent(transform, false);

        var rt = legendGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = legendAnchoredPos;
        rt.sizeDelta = legendSize;

        // Transparent background (optional – you can set alpha > 0 if you want a box)
        var img = legendGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);

        // Use chart colors if chart exists
        Color rpmCol = chart != null ? chart.rpmColor : Color.white;
        Color inCol = chart != null ? chart.inTorqueColor : new Color(1f, 0.8f, 0.2f, 1f);
        Color outCol = chart != null ? chart.outTorqueColor : new Color(0.2f, 0.9f, 1f, 1f);

        CreateRow(legendGO.transform, 0, rpmCol, "RPM");
        CreateRow(legendGO.transform, 1, inCol, "In-Torque (Nm)");
        CreateRow(legendGO.transform, 2, outCol, "Out-Torque (Nm)");
    }

    void CreateRow(Transform parent, int index, Color color, string label)
    {
        float y = -index * rowHeight;

        // Color line
        var lineGO = new GameObject("Line_" + label, typeof(RectTransform));
        lineGO.transform.SetParent(parent, false);

        var lrt = lineGO.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f, 1f);
        lrt.anchorMax = new Vector2(0f, 1f);
        lrt.pivot = new Vector2(0f, 0.5f);
        lrt.anchoredPosition = new Vector2(0f, y - rowHeight * 0.5f);
        lrt.sizeDelta = new Vector2(lineWidth, lineHeight);

        var limg = lineGO.AddComponent<Image>();
        limg.color = color;

        // Text
        var textGO = new GameObject("Text_" + label, typeof(RectTransform));
        textGO.transform.SetParent(parent, false);

        var trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(0f, 1f);
        trt.pivot = new Vector2(0f, 0.5f);
        trt.anchoredPosition = new Vector2(lineWidth + 8f, y - rowHeight * 0.5f);
        trt.sizeDelta = new Vector2(legendSize.x - (lineWidth + 8f), rowHeight);

        var txt = textGO.AddComponent<Text>();
        txt.text = label;
        txt.fontSize = Mathf.RoundToInt(fontSize);
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleLeft;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.raycastTarget = false;
    }
}
