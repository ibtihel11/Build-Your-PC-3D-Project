using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class GearDataTimeSeriesChart : MonoBehaviour
{
    [Header("Data Sources")]
    public MonoBehaviour gearbox;                 // Drag GearBoxDirectDrive here
    public MonoBehaviour buttonsController;        // Optional: Start/Stop controller (has bool IsRunning?)

    [Header("Sampling")]
    [Range(5f, 60f)] public float sampleHz = 20f;
    [Range(5f, 120f)] public float windowSeconds = 30f;

    [Header("Scaling")]
    public float rpmMax = 1000f;

    [Tooltip("If true, torque scale adapts to recent values (recommended).")]
    public bool autoScaleTorque = true;

    [Tooltip("Used if AutoScaleTorque = false.")]
    public float torqueMaxManual = 5000f;

    [Header("Rendering")]
    public int texWidth = 640;
    public int texHeight = 260;
    public int padding = 12;

    public Color background = new Color(0.08f, 0.08f, 0.10f, 1f);
    public Color grid = new Color(0.18f, 0.18f, 0.22f, 1f);

    public Color rpmColor = Color.white;
    public Color inTorqueColor = new Color(1f, 0.8f, 0.2f, 1f);
    public Color outTorqueColor = new Color(0.2f, 0.9f, 1f, 1f);

    // internal
    RawImage img;
    Texture2D tex;
    Color32[] pixels;

    float sampleTimer;
    int sampleCount;
    int capacity;

    float[] rpmSeries;
    float[] inTSeries;
    float[] outTSeries;

    // reflection (to avoid hard dependencies if your class names differ)
    FieldInfo screwRpmField;
    PropertyInfo isRunningPropGearbox;
    PropertyInfo isRunningPropButtons;
    MethodInfo stopRunMethod;
    MethodInfo setScrewRpmMethod;

    void Awake()
    {
        img = GetComponent<RawImage>();
        BuildTexture();
        CacheReflection();
        ResetSeries();
    }

    void OnValidate()
    {
        texWidth = Mathf.Clamp(texWidth, 128, 2048);
        texHeight = Mathf.Clamp(texHeight, 128, 2048);
        padding = Mathf.Clamp(padding, 0, 100);
    }

    void BuildTexture()
    {
        tex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        pixels = new Color32[texWidth * texHeight];
        img.texture = tex;

        Clear(background);
        tex.SetPixels32(pixels);
        tex.Apply(false);
    }

    void ResetSeries()
    {
        capacity = Mathf.Max(10, Mathf.RoundToInt(windowSeconds * sampleHz));
        rpmSeries = new float[capacity];
        inTSeries = new float[capacity];
        outTSeries = new float[capacity];
        sampleCount = 0;
        sampleTimer = 0f;
    }

    void CacheReflection()
    {
        if (gearbox != null)
        {
            var gt = gearbox.GetType();
            screwRpmField = gt.GetField("screwRpm", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            isRunningPropGearbox = gt.GetProperty("IsRunning", BindingFlags.Public | BindingFlags.Instance);
            stopRunMethod = gt.GetMethod("StopRun", BindingFlags.Public | BindingFlags.Instance);
            setScrewRpmMethod = gt.GetMethod("SetScrewRpm", BindingFlags.Public | BindingFlags.Instance);
        }

        if (buttonsController != null)
        {
            var bt = buttonsController.GetType();
            isRunningPropButtons = bt.GetProperty("IsRunning", BindingFlags.Public | BindingFlags.Instance);
        }
    }

    void Update()
    {
        // if inspector values changed in play mode
        int desiredCap = Mathf.Max(10, Mathf.RoundToInt(windowSeconds * sampleHz));
        if (desiredCap != capacity)
        {
            ResetSeries();
        }

        sampleTimer += Time.deltaTime;
        float dt = 1f / Mathf.Max(1f, sampleHz);

        while (sampleTimer >= dt)
        {
            sampleTimer -= dt;
            SampleOnce();
        }
    }

    void SampleOnce()
    {
        float rpm = GetCurrentRpm();
        float inT = CalcInputTorque(rpm);
        float outT = inT * 4f;

        Push(rpmSeries, rpm);
        Push(inTSeries, inT);
        Push(outTSeries, outT);

        sampleCount = Mathf.Min(sampleCount + 1, capacity);

        Redraw();
    }

    float GetCurrentRpm()
    {
        // running? prefer buttonsController.IsRunning if available
        bool running = true;

        if (isRunningPropButtons != null && buttonsController != null)
        {
            try { running = (bool)isRunningPropButtons.GetValue(buttonsController); }
            catch { running = true; }
        }
        else if (isRunningPropGearbox != null && gearbox != null)
        {
            try { running = (bool)isRunningPropGearbox.GetValue(gearbox); }
            catch { running = true; }
        }

        float rpm = 0f;

        if (gearbox != null && screwRpmField != null)
        {
            try
            {
                object v = screwRpmField.GetValue(gearbox);
                if (v is float f) rpm = f;
                else if (v is int i) rpm = i;
            }
            catch { rpm = 0f; }
        }

        // requirement: if stopped => show 0
        if (!running) return 0f;
        return Mathf.Max(0f, rpm);
    }

    float CalcInputTorque(float rpm)
    {
        // Guard against division by zero
        if (rpm <= 0.01f) return 0f;
        return 500000f / rpm;
    }

    void Push(float[] arr, float value)
    {
        // shift left
        for (int i = 0; i < arr.Length - 1; i++)
            arr[i] = arr[i + 1];
        arr[arr.Length - 1] = value;
    }

    void Redraw()
    {
        Clear(background);
        DrawGrid();

        int w = texWidth;
        int h = texHeight;
        int left = padding;
        int right = w - 1 - padding;
        int bottom = padding;
        int top = h - 1 - padding;

        if (right <= left || top <= bottom) return;

        // torque scaling
        float torqueMax = torqueMaxManual;
        if (autoScaleTorque)
        {
            torqueMax = 1f;
            int n = sampleCount;
            for (int i = capacity - n; i < capacity; i++)
            {
                torqueMax = Mathf.Max(torqueMax, inTSeries[i], outTSeries[i]);
            }
            torqueMax *= 1.1f; // headroom
        }

        // plot lines
        PlotSeries(rpmSeries, sampleCount, left, right, bottom, top, 0f, Mathf.Max(1f, rpmMax), rpmColor);
        PlotSeries(inTSeries, sampleCount, left, right, bottom, top, 0f, Mathf.Max(1f, torqueMax), inTorqueColor);
        PlotSeries(outTSeries, sampleCount, left, right, bottom, top, 0f, Mathf.Max(1f, torqueMax), outTorqueColor);

        tex.SetPixels32(pixels);
        tex.Apply(false);
    }

    void DrawGrid()
    {
        int w = texWidth;
        int h = texHeight;
        int left = padding;
        int right = w - 1 - padding;
        int bottom = padding;
        int top = h - 1 - padding;

        // border
        DrawRect(left, bottom, right, top, grid);

        // vertical grid lines (time)
        for (int i = 1; i <= 5; i++)
        {
            int x = Mathf.RoundToInt(Mathf.Lerp(left, right, i / 6f));
            DrawLine(x, bottom, x, top, grid);
        }

        // horizontal grid lines
        for (int i = 1; i <= 3; i++)
        {
            int y = Mathf.RoundToInt(Mathf.Lerp(bottom, top, i / 4f));
            DrawLine(left, y, right, y, grid);
        }
    }

    void PlotSeries(float[] series, int nSamples, int left, int right, int bottom, int top, float vMin, float vMax, Color col)
    {
        if (nSamples <= 1) return;

        int w = right - left;
        int n = Mathf.Min(nSamples, capacity);
        int startIndex = capacity - n;

        int prevX = 0, prevY = 0;
        bool hasPrev = false;

        for (int i = 0; i < n; i++)
        {
            float t = (n == 1) ? 1f : (i / (float)(n - 1));
            int x = left + Mathf.RoundToInt(t * w);

            float v = series[startIndex + i];
            float yn = Mathf.InverseLerp(vMin, vMax, v);
            int y = bottom + Mathf.RoundToInt(yn * (top - bottom));

            if (hasPrev)
                DrawLine(prevX, prevY, x, y, col);

            prevX = x;
            prevY = y;
            hasPrev = true;
        }
    }

    // ---------- Drawing helpers (pixel based) ----------
    void Clear(Color c)
    {
        Color32 cc = c;
        for (int i = 0; i < pixels.Length; i++) pixels[i] = cc;
    }

    void SetPixel(int x, int y, Color c)
    {
        if (x < 0 || x >= texWidth || y < 0 || y >= texHeight) return;
        pixels[y * texWidth + x] = (Color32)c;
    }

    void DrawLine(int x0, int y0, int x1, int y1, Color c)
    {
        int dx = Mathf.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            SetPixel(x0, y0, c);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    void DrawRect(int left, int bottom, int right, int top, Color c)
    {
        DrawLine(left, bottom, right, bottom, c);
        DrawLine(right, bottom, right, top, c);
        DrawLine(right, top, left, top, c);
        DrawLine(left, top, left, bottom, c);
    }
}
