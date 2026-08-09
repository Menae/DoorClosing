using System.Collections;
using UnityEngine;

public class HijackAnomaly : AnomalyBehaviour
{
    private const float SlowDriftInterval = 1f;
    private const float FastDriftInterval = 0.08f;
    private const float RevealPitch = 2f;
    private const float RevealVolume = 1f;
    private const float JitterAmplitude = 0.035f;
    private const float JitterInterval = 0.03f;

    [Header("Hijack")]
    [SerializeField] private AudioSource motor;
    [SerializeField] private int driftTargetFloor = 13;
    [SerializeField, Min(0f)] private float rampSeconds = 20f;
    [SerializeField] private float pitchStart = 1f;
    [SerializeField] private float pitchEnd = 1.6f;

    private Coroutine rampRoutine;
    private Coroutine jitterRoutine;
    private Vector3 originalLocalPosition;
    private float originalMotorPitch = 1f;
    private float originalMotorVolume = 1f;
    private int normalDisplayFloor;

    protected override void Awake()
    {
        base.Awake();
        originalLocalPosition = transform.localPosition;

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
            floorIndicator.StartDrift(driftTargetFloor, SlowDriftInterval);
        }
        else
        {
            WarnMissingReferenceOnce(nameof(FloorIndicator.Instance));
        }

        if (motor != null)
        {
            motor.loop = true;
            motor.pitch = pitchStart;
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
            motor.volume = RevealVolume;
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
            floorIndicator.StartDrift(driftTargetFloor, FastDriftInterval);
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
