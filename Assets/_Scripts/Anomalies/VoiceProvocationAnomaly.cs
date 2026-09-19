using System.Collections;
using UnityEngine;

/// <summary>Audio-only Provocation. BeatStateMachine owns all decisions and deadlines.</summary>
public sealed class VoiceProvocationAnomaly : AnomalyBehaviour
{
    [Header("扉の外からの声")]
    [SerializeField] private AudioSource speaker;
    [SerializeField] private AudioClip pleaClip;
    [SerializeField] private AudioClip revealedClip;
    [SerializeField, Range(0, 1)] private float voiceVolume = .6f;
    [SerializeField, Min(0)] private float firstDelay = .6f;
    [SerializeField, Min(0)] private float silenceBetweenCalls = 12f;
    [Header("編集用台本（変更後は音声ファイルも差し替える）")]
    [SerializeField, TextArea] private string pleaScript;
    [SerializeField, TextArea] private string revealedScript;

    private Coroutine voiceRoutine;
    public bool IsVoicePlaying => speaker != null && speaker.isPlaying;
    public bool HasRevealed { get; private set; }

    // No surface materials or monitor belong to this variant.
    protected override void Awake() { }

    public override void OnDiagnosisStart()
    {
        HasRevealed = false;
        StartVoice(pleaClip, firstDelay);
    }

    public override void OnReveal()
    {
        HasRevealed = true;
        StartVoice(revealedClip, 0);
    }

    public override void OnGraceStart() { }
    public override void OnGraceEnd(bool recovered) { }
    public override void OnCleanup() => StopVoice();
    private void OnDisable() => StopVoice();
    protected override void OnDestroy() => StopVoice();

    private void StartVoice(AudioClip clip, float delay)
    {
        StopVoice();
        if (speaker == null || clip == null)
        {
            Debug.LogError("外からの声にAudioSourceと音声クリップを設定してください。", this);
            return;
        }
        voiceRoutine = StartCoroutine(VoiceLoop(clip, delay));
    }

    private IEnumerator VoiceLoop(AudioClip clip, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        while (true)
        {
            speaker.clip = clip;
            speaker.volume = voiceVolume;
            speaker.Play();
            // Scaled time and AudioListener.pause preserve the sentence across pause.
            yield return new WaitForSeconds(clip.length / Mathf.Max(.01f, Mathf.Abs(speaker.pitch)) + silenceBetweenCalls);
        }
    }

    private void StopVoice()
    {
        if (voiceRoutine != null) StopCoroutine(voiceRoutine);
        voiceRoutine = null;
        if (speaker != null) speaker.Stop();
    }
}
