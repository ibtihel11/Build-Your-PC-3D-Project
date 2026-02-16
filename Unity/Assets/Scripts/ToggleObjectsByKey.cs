using UnityEngine;

public class ToggleObjectsByKey : MonoBehaviour
{
    [Header("Objects to Toggle")]
    [Tooltip("Hier alle GameObjects reinziehen, die ein-/ausgeblendet werden sollen.")]
    public GameObject[] targets;

    [Header("Keyboard")]
    public KeyCode toggleKey = KeyCode.T;

    [Header("Behaviour")]
    [Tooltip("Wenn TRUE: Objekte beim Start direkt ausblenden.")]
    public bool startHidden = false;

    [Tooltip("Wenn TRUE: nicht das GameObject deaktivieren, sondern nur Renderer ein/aus (Colliders bleiben aktiv).")]
    public bool rendererOnly = false;

    private bool isHidden;

    void Start()
    {
        isHidden = startHidden;
        Apply(isHidden);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isHidden = !isHidden;
            Apply(isHidden);
        }
    }

    void Apply(bool hide)
    {
        if (targets == null) return;

        foreach (var go in targets)
        {
            if (go == null) continue;

            if (!rendererOnly)
            {
                go.SetActive(!hide);
            }
            else
            {
                // Nur Renderer togglen (auch in Children)
                var renderers = go.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers) r.enabled = !hide;
            }
        }
    }
}
