using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WarningBeaconAudioFollowGear : MonoBehaviour
{
    public GearBoxDirectDrive gearbox;

    [Header("Beep Clip")]
    public AudioClip beepClip;     // z.B. beep_double_loop_1s.wav
    public bool loop = true;
    [Range(0f, 1f)] public float volume = 0.7f;

    [Header("3D Sound (optional)")]
    public bool use3DSound = true;
    public float minDistance = 1.0f;
    public float maxDistance = 10.0f;

    private AudioSource src;
    private bool lastRunning;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.volume = volume;
        src.clip = beepClip;

        src.spatialBlend = use3DSound ? 1f : 0f;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.rolloffMode = AudioRolloffMode.Logarithmic;

        // Startzustand: aus
        src.Stop();
        lastRunning = false;
    }

    void Update()
    {
        bool running = gearbox != null && gearbox.IsRunning;

        // Zustand hat sich geändert?
        if (running != lastRunning)
        {
            lastRunning = running;

            if (running)
                StartBeep();
            else
                StopBeep();
        }
    }

    void StartBeep()
    {
        if (beepClip != null && src.clip == null) src.clip = beepClip;
        if (src.clip == null) return;

        if (!src.isPlaying)
            src.Play();
    }

    void StopBeep()
    {
        if (src.isPlaying)
            src.Stop();
    }
}
