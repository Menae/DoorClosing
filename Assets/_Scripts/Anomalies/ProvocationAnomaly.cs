using System.Collections;
using TMPro;
using UnityEngine;

public class ProvocationAnomaly : AnomalyBehaviour
{
    // ASCII trial copy keeps the generated stand-in legible with the bundled TMP font.
    // Final wording and localization remain an authored-content task.
    private const string NormalAnnouncement = "SAFETY CHECK: PRESS EMERGENCY STOP";
    private const string RevealedAnnouncement = "PRESS IT. PRESS IT. PRESS IT.";
    private const float RevealedPitch = 0.6f;
    private const float LampBlinkIntervalSeconds = 0.2f;

    [Header("Provocation")]
    [SerializeField] private AudioSource speaker;
    [SerializeField] private AudioClip announceClip;
    [SerializeField] private TMP_Text subtitle;
    [SerializeField] private GameObject revealLamp;
    [SerializeField, Min(0f)] private float firstDelay = 3f;
    [SerializeField, Min(0f)] private float repeatInterval = 15f;

    private Coroutine announcementRoutine;
    private Coroutine revealLampRoutine;
    private float originalSpeakerPitch = 1f;
    private AudioClip generatedAnnouncementClip;
    private bool usesGeneratedTrialPanel;

    public bool IsAnnouncementVisible => subtitle != null && subtitle.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(subtitle.text);
    public bool IsAnnouncementPlaying => speaker != null && speaker.isPlaying;

    protected override void Awake()
    {
        base.Awake();
        EnsurePrototypePresentation();

        if (speaker != null)
        {
            originalSpeakerPitch = speaker.pitch;
        }

        if (revealLamp != null)
        {
            revealLamp.SetActive(false);
        }
    }

    public override void OnDiagnosisStart()
    {
        base.OnDiagnosisStart();
        if (usesGeneratedTrialPanel)
        {
            SetTrialPanelColor(new Color(0.025f, 0.025f, 0.03f));
        }
        StopAnnouncementRoutine();
        announcementRoutine = StartCoroutine(AnnouncementLoop());
    }

    public override void OnReveal()
    {
        base.OnReveal();
        StopAnnouncementRoutine();
        PresentAnnouncement(RevealedAnnouncement, RevealedPitch);

        if (HasReference(revealLamp, nameof(revealLamp)))
        {
            StopRevealLampRoutine();
            revealLampRoutine = StartCoroutine(BlinkRevealLamp());
        }
    }

    public override void OnCleanup()
    {
        base.OnCleanup();
        StopAnnouncementRoutine();
        StopRevealLampRoutine();

        if (speaker != null)
        {
            speaker.Stop();
            speaker.pitch = originalSpeakerPitch;
        }
        else
        {
            WarnMissingReferenceOnce(nameof(speaker));
        }

        if (revealLamp != null)
        {
            revealLamp.SetActive(false);
        }

        if (generatedAnnouncementClip != null)
        {
            Destroy(generatedAnnouncementClip);
            generatedAnnouncementClip = null;
        }
    }

    private IEnumerator AnnouncementLoop()
    {
        yield return WaitForSecondsIfPositive(firstDelay);

        while (true)
        {
            PresentAnnouncement(NormalAnnouncement, originalSpeakerPitch);
            yield return WaitForSecondsIfPositive(repeatInterval);
        }
    }

    private void PresentAnnouncement(string message, float pitch)
    {
        SetTextOrLog(subtitle, nameof(subtitle), message);

        if (speaker == null)
        {
            WarnMissingReferenceOnce(nameof(speaker));
            LogFallback($"Announcement audio: {message}");
            return;
        }

        AudioClip clip = announceClip != null ? announceClip : speaker.clip;
        if (clip == null)
        {
            WarnMissingReferenceOnce(nameof(announceClip));
            LogFallback($"Announcement audio clip: {message}");
            return;
        }

        speaker.pitch = pitch;
        speaker.clip = clip;
        speaker.Play();
    }

    private void EnsurePrototypePresentation()
    {
        if (subtitle == null)
        {
            usesGeneratedTrialPanel = true;
            GameObject display = new GameObject("TrialAnnouncementDisplay");
            display.transform.SetParent(transform, false);
            display.transform.localPosition = new Vector3(0f, 0f, -0.56f);
            display.transform.localRotation = Quaternion.identity;
            display.transform.localScale = new Vector3(0.45f, 0.55f, 1f);
            TextMeshPro text = display.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 1f;
            text.fontSizeMax = 5f;
            text.color = new Color(1f, 0.12f, 0.08f);
            text.rectTransform.sizeDelta = new Vector2(4.2f, 1.8f);
            subtitle = text;
        }

        if (speaker == null)
        {
            speaker = gameObject.AddComponent<AudioSource>();
            speaker.playOnAwake = false;
            speaker.spatialBlend = 0.35f;
            speaker.volume = 0.8f;
            generatedAnnouncementClip = GeneratedTone.CreateWarningChime("ProvocationTrialChime");
            speaker.clip = generatedAnnouncementClip;
        }

        if (revealLamp == null)
        {
            revealLamp = new GameObject("TrialRevealLamp");
            revealLamp.transform.SetParent(transform, false);
            revealLamp.transform.localPosition = new Vector3(0f, 0.38f, -0.58f);
            Light light = revealLamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Color.red;
            light.range = 2.5f;
            light.intensity = 2f;
        }
    }

    private void SetTrialPanelColor(Color color)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            foreach (Material material in renderer.materials)
            {
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    private IEnumerator BlinkRevealLamp()
    {
        while (true)
        {
            revealLamp.SetActive(!revealLamp.activeSelf);
            yield return new WaitForSeconds(LampBlinkIntervalSeconds);
        }
    }

    private IEnumerator WaitForSecondsIfPositive(float seconds)
    {
        if (seconds <= 0f)
        {
            yield return null;
            yield break;
        }

        yield return new WaitForSeconds(seconds);
    }

    private void StopAnnouncementRoutine()
    {
        if (announcementRoutine == null)
        {
            return;
        }

        StopCoroutine(announcementRoutine);
        announcementRoutine = null;
    }

    private void StopRevealLampRoutine()
    {
        if (revealLampRoutine == null)
        {
            return;
        }

        StopCoroutine(revealLampRoutine);
        revealLampRoutine = null;
    }
}
