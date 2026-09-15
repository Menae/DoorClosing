using System.Collections;
using UnityEngine;

public class HijackAnomaly : AnomalyBehaviour
{
    private const float RevealPitch = 2f;
    private const float RevealVolume = 1f;
    private const float JitterAmplitude = 0.035f;
    private const float JitterInterval = 0.03f;

    [Header("Hijack")]
    [SerializeField] private AudioSource motor;
    [SerializeField, Min(.05f)] private float driftIntervalSeconds = 1f;
    private ElevatorTuning tuning;
    [SerializeField, Min(0f)] private float rampSeconds = 20f;
    [SerializeField] private float pitchStart = 1f;
    [SerializeField] private float pitchEnd = 1.6f;

    private Coroutine rampRoutine;
    private Coroutine jitterRoutine;
    private Vector3 originalLocalPosition;
    private float originalMotorPitch = 1f;
    private float originalMotorVolume = 1f;
    private int normalDisplayFloor;
    private AudioClip generatedMotorClip;

    public bool IsMotorPlaying => motor != null && motor.isPlaying;

    protected override void Awake()
    {
        base.Awake();
        tuning=FindFirstObjectByType<ElevatorTuning>();
        originalLocalPosition = transform.localPosition;
        EnsurePrototypeMotor();

        if (motor != null)
        {
            originalMotorPitch = motor.pitch;
            originalMotorVolume = motor.volume;
        }
    }

    public override void OnDiagnosisStart()
    {
        base.OnDiagnosisStart();

        FloorIndicator floorIndicator = FloorIndicator.Instance;
        if (floorIndicator != null)
        {
            normalDisplayFloor = floorIndicator.CurrentDisplayedFloor;
            floorIndicator.StartContinuousRise(tuning!=null ? tuning.怪異の階数上昇間隔 : driftIntervalSeconds);
        }
        else
        {
            WarnMissingReferenceOnce(nameof(FloorIndicator.Instance));
        }

        if (motor != null)
        {
            motor.loop = true;
            motor.pitch = pitchStart;
            if(tuning!=null) motor.volume=tuning.怪異の走行音;
            motor.Play();
        }
        else
        {
            WarnMissingReferenceOnce(nameof(motor));
            LogFallback("Hijack motor loop playback");
        }

        StopRampRoutine();
        rampRoutine = StartCoroutine(RampMotor());
    }

    public override void OnReveal()
    {
        base.OnReveal();
        StopRampRoutine();

        if (motor != null)
        {
            motor.loop = true;
            motor.pitch = RevealPitch;
            motor.volume = tuning!=null ? tuning.怪異の走行音 : originalMotorVolume;
            if (!motor.isPlaying)
            {
                motor.Play();
            }
        }
        else
        {
            WarnMissingReferenceOnce(nameof(motor));
            LogFallback("Hijack reveal motor surge");
        }

        FloorIndicator floorIndicator = FloorIndicator.Instance;
        if (floorIndicator != null)
        {
            floorIndicator.Flicker();
            // Keep the same uninterrupted cadence after reveal.
        }
        else
        {
            WarnMissingReferenceOnce(nameof(FloorIndicator.Instance));
        }

        StopJitterRoutine();
        jitterRoutine = StartCoroutine(JitterRoutine());
    }

    public override void OnGraceEnd(bool recovered)
    {
        base.OnGraceEnd(recovered);

        if (!recovered)
        {
            return;
        }

        StopMotor();

        FloorIndicator floorIndicator = FloorIndicator.Instance;
        if (floorIndicator != null)
        {
            floorIndicator.StopDrift();
            floorIndicator.SetFloor(normalDisplayFloor);
        }
        else
        {
            WarnMissingReferenceOnce(nameof(FloorIndicator.Instance));
        }
    }

    public override void OnCleanup()
    {
        base.OnCleanup();
        StopRampRoutine();
        StopJitterRoutine();
        StopMotor();

        FloorIndicator floorIndicator = FloorIndicator.Instance;
        if (floorIndicator != null)
        {
            floorIndicator.StopDrift();
        }

        transform.localPosition = originalLocalPosition;

        if (generatedMotorClip != null)
        {
            Destroy(generatedMotorClip);
            generatedMotorClip = null;
        }
    }

    private IEnumerator RampMotor()
    {
        int nextMilestone = 25;

        if (rampSeconds <= 0f)
        {
            ApplyMotorPitch(pitchEnd);
            LogUrgency(100);
            rampRoutine = null;
            yield break;
        }

        float elapsedSeconds = 0f;
        while (elapsedSeconds < rampSeconds)
        {
            elapsedSeconds += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedSeconds / rampSeconds);
            ApplyMotorPitch(Mathf.Lerp(pitchStart, pitchEnd, normalizedTime));

            int percent = Mathf.FloorToInt(normalizedTime * 100f);
            while (nextMilestone <= 100 && percent >= nextMilestone)
            {
                LogUrgency(nextMilestone);
                nextMilestone += 25;
            }

            yield return null;
        }

        ApplyMotorPitch(pitchEnd);
        rampRoutine = null;
    }

    private IEnumerator JitterRoutine()
    {
        while (true)
        {
            transform.localPosition = originalLocalPosition + Random.insideUnitSphere * JitterAmplitude;
            yield return new WaitForSeconds(JitterInterval);
        }
    }

    private void ApplyMotorPitch(float pitch)
    {
        if (motor != null)
        {
            motor.pitch = pitch;
        }
    }

    private void LogUrgency(int percent)
    {
        Debug.Log($"[Hijack] 切迫 {percent}%", this);
    }

    private void StopMotor()
    {
        if (motor != null)
        {
            motor.Stop();
            motor.pitch = originalMotorPitch;
            motor.volume = originalMotorVolume;
        }
        else
        {
            WarnMissingReferenceOnce(nameof(motor));
        }
    }

    private void EnsurePrototypeMotor()
    {
        if (motor != null)
        {
            return;
        }

        motor = gameObject.AddComponent<AudioSource>();
        motor.playOnAwake = false;
        motor.loop = true;
        motor.spatialBlend = 0.2f;
        motor.volume = 0.65f;
        generatedMotorClip = GeneratedTone.CreateMechanicalLoop("HijackTrialMotor");
        motor.clip = generatedMotorClip;
    }

    private void StopRampRoutine()
    {
        if (rampRoutine == null)
        {
            return;
        }

        StopCoroutine(rampRoutine);
        rampRoutine = null;
    }

    private void StopJitterRoutine()
    {
        if (jitterRoutine == null)
        {
            return;
        }

        StopCoroutine(jitterRoutine);
        jitterRoutine = null;
        transform.localPosition = originalLocalPosition;
    }
}
