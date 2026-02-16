using UnityEngine;

public class GearBoxDirectDrive : MonoBehaviour
{
    [Header("Parts (assign your objects here)")]
    public Transform screw;   // Screw
    public Transform gear1;   // Gear_1
    public Transform gear2;   // Gear_2

    [Header("Speed")]
    [Tooltip("Screw speed in RPM")]
    public float screwRpm = 60f;

    [Header("Ratios (absolute values)")]
    [Tooltip("Gear_1 speed relative to screw (0.5 = half speed, 2 = double speed)")]
    public float ratioScrewToGear1 = 1.0f;

    [Tooltip("Gear_2 speed relative to gear_1 (1 = same magnitude)")]
    public float ratioGear1ToGear2 = 1.0f;

    [Header("Direction")]
    [Tooltip("If true, the whole gearbox rotates in reverse direction.")]
    public bool reverseDirection = false;

    /// <summary>Read-only direction status for other scripts.</summary>
    public bool IsReverse => reverseDirection;

    [Header("Start")]
    [Tooltip("If false, gearbox will NOT rotate on play.")]
    public bool startRunning = false;

    // internal state
    [SerializeField] private bool running = false;

    /// <summary>Read-only status for other scripts.</summary>
    public bool IsRunning => running;

    void Awake()
    {
        // Guarantee predictable start state
        running = startRunning;
    }

    void OnEnable()
    {
        // Safety: if you don't want auto-start, force off when enabled
        if (!startRunning) running = false;
    }

    void Update()
    {
        if (!running) return;
        if (screw == null || gear1 == null || gear2 == null) return;

        // RPM -> degrees per second (RPM * 360 / 60 = RPM * 6)
        float screwDegPerSec = screwRpm * 6f;
        float dScrew = screwDegPerSec * Time.deltaTime;

        // Global direction switch: reverse the whole chain
        if (reverseDirection)
            dScrew = -dScrew;

        // Keep ratios as magnitudes; sign comes from dScrew
        float dGear1 = dScrew * Mathf.Abs(ratioScrewToGear1);
        float dGear2 = -dGear1 * Mathf.Abs(ratioGear1ToGear2); // opposite direction to gear1

        // Axes:
        // Screw: local Z axis
        // Gear_1: local X axis
        // Gear_2: local X axis (opposite)
        screw.Rotate(Vector3.forward, dScrew, Space.Self);
        gear1.Rotate(Vector3.right, dGear1, Space.Self);
        gear2.Rotate(Vector3.right, dGear2, Space.Self);
    }

    // ---------- UI / External Control ----------
    public void ToggleRun()
    {
        running = !running;
    }

    public void StartRun()
    {
        running = true;
    }

    public void StopRun()
    {
        running = false;
    }

    public void SetRunning(bool value)
    {
        running = value;
    }

    public void SetScrewRpm(float rpm)
    {
        screwRpm = Mathf.Max(0f, rpm);
    }

    // ---------- Direction Control ----------
    public void SetReverse(bool reverse)
    {
        reverseDirection = reverse;
    }

    public void ToggleReverse()
    {
        reverseDirection = !reverseDirection;
    }
}
