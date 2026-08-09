using System.Collections;
using TMPro;
using UnityEngine;

public class ProvocationAnomaly : AnomalyBehaviour
{
    private const string NormalAnnouncement = "安全確認のため、非常停止ボタンを押してください。";
    private const string RevealedAnnouncement = "押してください。押してください。押してください。";
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

    protected override void Awake()
    {
        base.Awake();

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

        if (announceClip == null)
        {
            WarnMissingReferenceOnce(nameof(announceClip));
            LogFallback($"Announcement audio clip: {message}");
            return;
        }

        speaker.pitch = pitch;
        speaker.clip = announceClip;
        speaker.Play();
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
