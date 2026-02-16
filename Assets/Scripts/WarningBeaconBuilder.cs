using UnityEngine;

[ExecuteAlways]
public class WarningBeaconBuilder : MonoBehaviour
{
    [Header("Build (Editor)")]
    [Tooltip("Im Inspector kurz aktivieren -> Modell wird im nächsten Update neu gebaut.")]
    public bool rebuildNow = false;

    [Header("Sizes")]
    public float poleHeight = 1.2f;
    public float poleRadius = 0.04f;

    public float baseHeight = 0.08f;
    public float baseRadius = 0.10f;

    [Header("Transparent Housing")]
    public float headHeight = 0.28f;
    public float headRadius = 0.09f;

    [Range(0.05f, 0.9f)]
    public float housingAlpha = 0.18f; // klar/transparent
    public Color housingTint = new Color(0.95f, 0.95f, 0.98f, 1f); // weißlich/klar

    [Header("Inner Lens (Red)")]
    public float lensRadius = 0.06f;
    public float lensHeight = 0.09f;

    [Header("Light")]
    public float lightRange = 2.5f;
    public float lightIntensityOn = 3.0f;

    [Header("Output References (auto-filled)")]
    public Renderer housingRenderer;
    public Renderer redLensRenderer;
    public Light redLight;

    // internal flag so OnValidate doesn't destroy objects
    private bool pendingRebuild = false;

    void OnValidate()
    {
        // NIEMALS hier DestroyImmediate/Build ausführen!
        if (rebuildNow)
        {
            rebuildNow = false;
            pendingRebuild = true;
        }
    }

    void Update()
    {
        // Rebuild sicher im Update ausführen (Editor + Play Mode)
        if (pendingRebuild)
        {
            pendingRebuild = false;
            Build();
        }
    }

    public void Build()
    {
        // Children löschen (sicher: in EditMode DestroyImmediate, im PlayMode Destroy)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }

        name = "WarningBeacon";

        // ---------- Base ----------
        var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObj.name = "Base";
        baseObj.transform.SetParent(transform, false);
        baseObj.transform.localScale = new Vector3(baseRadius * 2f, baseHeight * 0.5f, baseRadius * 2f);
        baseObj.transform.localPosition = new Vector3(0, baseHeight * 0.5f, 0);

        // ---------- Pole ----------
        var poleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        poleObj.name = "Pole";
        poleObj.transform.SetParent(transform, false);
        poleObj.transform.localScale = new Vector3(poleRadius * 2f, poleHeight * 0.5f, poleRadius * 2f);
        poleObj.transform.localPosition = new Vector3(0, baseHeight + poleHeight * 0.5f, 0);

        // ---------- Housing (transparent) ----------
        var housingObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        housingObj.name = "Housing";
        housingObj.transform.SetParent(transform, false);
        housingObj.transform.localScale = new Vector3(headRadius * 2f, headHeight * 0.5f, headRadius * 2f);
        housingObj.transform.localPosition = new Vector3(0, baseHeight + poleHeight + headHeight * 0.5f, 0);

        ApplyOpaqueMat(baseObj.GetComponent<Renderer>(), new Color(0.12f, 0.12f, 0.12f));
        ApplyOpaqueMat(poleObj.GetComponent<Renderer>(), new Color(0.18f, 0.18f, 0.18f));

        housingRenderer = housingObj.GetComponent<Renderer>();
        var glassColor = new Color(housingTint.r, housingTint.g, housingTint.b, housingAlpha);
        ApplyTransparentMat(housingRenderer, glassColor);

        // ---------- Red lens inside housing ----------
        var redLens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        redLens.name = "RedLens";
        redLens.transform.SetParent(housingObj.transform, false);
        redLens.transform.localScale = new Vector3(lensRadius * 2f, lensHeight * 0.5f, lensRadius * 2f);
        redLens.transform.localPosition = Vector3.zero;

        redLensRenderer = redLens.GetComponent<Renderer>();
        ApplyEmissiveMat(redLensRenderer, new Color(1f, 0.15f, 0.15f));

        // ---------- Red light inside (start OFF) ----------
        var lightObj = new GameObject("RedLight");
        lightObj.transform.SetParent(redLens.transform, false);
        lightObj.transform.localPosition = Vector3.zero;

        redLight = lightObj.AddComponent<Light>();
        redLight.type = LightType.Point;
        redLight.color = new Color(1f, 0.15f, 0.15f);
        redLight.range = lightRange;
        redLight.intensity = 0f;

        // Controller automatisch hinzufügen/füllen (Grün bleibt bewusst weg/aus)
        var ctrl = GetComponent<WarningBeaconController>();
        if (!ctrl) ctrl = gameObject.AddComponent<WarningBeaconController>();

        ctrl.redLensRenderer = redLensRenderer;
        ctrl.redLight = redLight;
        ctrl.defaultLightIntensity = lightIntensityOn;
    }

    // ---------- Material Helpers (Built-in / Standard shader) ----------

    void ApplyOpaqueMat(Renderer r, Color color)
    {
        if (!r) return;
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(color.r, color.g, color.b, 1f);
        r.sharedMaterial = mat;
    }

    void ApplyEmissiveMat(Renderer r, Color baseColor)
    {
        if (!r) return;
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.black); // Controller schaltet an/aus
        r.sharedMaterial = mat;
    }

    void ApplyTransparentMat(Renderer r, Color rgba)
    {
        if (!r) return;

        var mat = new Material(Shader.Find("Standard"));
        mat.color = rgba;

        // Standard Shader -> Transparent Mode
        mat.SetFloat("_Mode", 3f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHABLEND_ON");
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        r.sharedMaterial = mat;
    }
}
