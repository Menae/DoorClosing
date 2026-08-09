using System.Collections;
using UnityEngine;

public class BeatStateMachine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BeatDefinition initialBeat;
    [SerializeField] private ResponseEvaluator responseEvaluator;
    [SerializeField] private ElevatorController elevatorController;
    [SerializeField] private FloorIndicator floorIndicator;
    [SerializeField] private Transform anomalyParent;

    [Header("Startup")]
    [SerializeField] private bool beginInitialBeatOnStart;

    [Header("Door Timing")]
    [SerializeField, Min(0f)] private float doorOpenSeconds = 1f;
    [SerializeField, Min(0f)] private float doorCloseSeconds = 1f;

    [Header("State Timing")]
    [SerializeField, Min(0f)] private float resolveSeconds = 0.5f;
    [SerializeField, Min(0f)] private float representedTravelSeconds = 3f;

    private Coroutine beatRoutine;
    private BeatDefinition currentBeat;
    private BeatState currentState;
    private GameObject currentAnomalyInstance;
    private AnomalyBehaviour currentAnomaly;
    private SubmittedAction pendingAction;
    private bool hasPendingAction;
    private bool acceptsPlayerAction;

    private struct SubmittedAction
    {
        public PlayerAction Action;
        public int FloorNumber;

        public SubmittedAction(PlayerAction action, int floorNumber)
        {
            Action = action;
            FloorNumber = floorNumber;
        }
    }

    private void Awake()
    {
        if (responseEvaluator == null)
        {
            responseEvaluator = GetComponent<ResponseEvaluator>();
        }

        if (elevatorController == null)
        {
            elevatorController = FindFirstObjectByType<ElevatorController>();
        }

        if (floorIndicator == null)
        {
            floorIndicator = FindFirstObjectByType<FloorIndicator>();
        }
    }

    private void Start()
    {
        if (beginInitialBeatOnStart && initialBeat != null)
        {
            BeginBeat(initialBeat);
        }
    }

    public void BeginBeat(BeatDefinition def)
    {
        if (def == null)
        {
            Debug.LogError($"{nameof(BeatStateMachine)} cannot begin a null beat definition.", this);
            return;
        }

        if (beatRoutine != null)
        {
            StopCoroutine(beatRoutine);
        }

        CleanupCurrentAnomaly();
        currentBeat = def;
        ClearPendingAction();
        acceptsPlayerAction = false;
        beatRoutine = StartCoroutine(RunBeat(def));
    }

    public void SubmitAction(PlayerAction action, int floorNumber = -1)
    {
        if (!acceptsPlayerAction)
        {
            return;
        }

        pendingAction = new SubmittedAction(action, floorNumber);
        hasPendingAction = true;
    }

    private IEnumerator RunBeat(BeatDefinition def)
    {
        SetState(BeatState.Travel);
        elevatorController?.CloseDoors();
        elevatorController?.SetTravelling(true);
        yield return WaitForSecondsFromDefinition(def.TravelSeconds);

        while (true)
        {
            yield return ArriveForDiagnosis(def);

            SetState(BeatState.Diagnosis);
            currentAnomaly?.OnDiagnosisStart();
            yield return WaitForDiagnosisCommit(def);

            SetState(BeatState.Committed);
            GameEvents.RaisePlayerCommitted(pendingAction.Action);

            BeatOutcome diagnosisOutcome = Evaluate(pendingAction);
            if (diagnosisOutcome == BeatOutcome.Correct)
            {
                yield return ResolveAndDepart(def);
                GameEvents.RaiseBeatResolved(BeatOutcome.Correct);
                yield break;
            }

            if (diagnosisOutcome == BeatOutcome.Represented)
            {
                yield return RepresentSameBeat();
                continue;
            }

            GameEvents.RaiseBeatResolved(BeatOutcome.WrongRevealed);

            SetState(BeatState.Reveal);
            currentAnomaly?.OnReveal();
            yield return WaitForSecondsFromDefinition(def.RevealSeconds);

            SetState(BeatState.Grace);
            currentAnomaly?.OnGraceStart();
            bool recovered = false;
            yield return WaitForGraceRecovery(def, value => recovered = value);

            if (recovered)
            {
                currentAnomaly?.OnGraceEnd(true);
                yield return ResolveAndDepart(def);
                GameEvents.RaiseBeatResolved(BeatOutcome.GraceRecovered);
                yield break;
            }

            currentAnomaly?.OnGraceEnd(false);
            CleanupCurrentAnomaly();
            GameEvents.RaiseBeatResolved(BeatOutcome.Death);
            beatRoutine = null;
            yield break;
        }
    }

    private IEnumerator ArriveForDiagnosis(BeatDefinition def)
    {
        SetState(BeatState.Arrive);
        SpawnAnomaly(def);
        floorIndicator?.SetFloor(def.DisplayFloor);
        floorIndicator?.Flicker();

        if (def.DoorsStayClosed)
        {
            yield break;
        }

        elevatorController?.SetTravelling(false);
        elevatorController?.OpenDoors();
        yield return WaitForSecondsFromDefinition(doorOpenSeconds);
    }

    private IEnumerator RepresentSameBeat()
    {
        acceptsPlayerAction = false;
        ClearPendingAction();
        CleanupCurrentAnomaly();

        SetState(BeatState.Depart);
        elevatorController?.CloseDoors();
        yield return WaitForSecondsFromDefinition(doorCloseSeconds);

        SetState(BeatState.Travel);
        elevatorController?.SetTravelling(true);
        yield return WaitForSecondsFromDefinition(representedTravelSeconds);
    }

    private IEnumerator ResolveAndDepart(BeatDefinition def)
    {
        SetState(BeatState.Resolve);
        elevatorController?.SetTravelling(false);
        yield return WaitForSecondsFromDefinition(resolveSeconds);

        SetState(BeatState.Depart);
        elevatorController?.CloseDoors();
        yield return WaitForSecondsFromDefinition(doorCloseSeconds);

        CleanupCurrentAnomaly();
        beatRoutine = null;
    }

    private IEnumerator WaitForDiagnosisCommit(BeatDefinition def)
    {
        ClearPendingAction();
        acceptsPlayerAction = true;

        float elapsedSeconds = 0f;
        while (!hasPendingAction)
        {
            if (def.HasHijackDeadline && elapsedSeconds >= def.HijackDeadlineSeconds)
            {
                pendingAction = new SubmittedAction(PlayerAction.None, -1);
                hasPendingAction = true;
                break;
            }

            elapsedSeconds += Time.deltaTime;
            yield return null;
        }

        acceptsPlayerAction = false;
    }

    private IEnumerator WaitForGraceRecovery(BeatDefinition def, System.Action<bool> onComplete)
    {
        ClearPendingAction();
        acceptsPlayerAction = true;

        float elapsedSeconds = 0f;
        while (elapsedSeconds < def.GraceSeconds)
        {
            if (hasPendingAction)
            {
                GameEvents.RaisePlayerCommitted(pendingAction.Action);

                if (Evaluate(pendingAction) == BeatOutcome.Correct)
                {
                    acceptsPlayerAction = false;
                    onComplete?.Invoke(true);
                    yield break;
                }

                acceptsPlayerAction = false;
                onComplete?.Invoke(false);
                yield break;
            }

            elapsedSeconds += Time.deltaTime;
            yield return null;
        }

        acceptsPlayerAction = false;
        onComplete?.Invoke(false);
    }

    private BeatOutcome Evaluate(SubmittedAction submittedAction)
    {
        if (responseEvaluator == null)
        {
            Debug.LogWarning($"{nameof(BeatStateMachine)} has no {nameof(ResponseEvaluator)}. Falling back to wrong.", this);
            return BeatOutcome.WrongRevealed;
        }

        return responseEvaluator.Evaluate(currentBeat, submittedAction.Action, submittedAction.FloorNumber);
    }

    private void SpawnAnomaly(BeatDefinition def)
    {
        CleanupCurrentAnomaly();

        if (def.AnomalyPrefab == null)
        {
            return;
        }

        currentAnomalyInstance = Instantiate(def.AnomalyPrefab, anomalyParent);
        currentAnomaly = currentAnomalyInstance.GetComponentInChildren<AnomalyBehaviour>();

        if (currentAnomaly == null)
        {
            Debug.LogWarning($"{nameof(BeatDefinition)} '{def.DebugLabel}' anomaly prefab has no {nameof(AnomalyBehaviour)}.", this);
        }
    }

    private void CleanupCurrentAnomaly()
    {
        if (currentAnomaly != null)
        {
            currentAnomaly.OnCleanup();
        }

        if (currentAnomalyInstance != null)
        {
            Destroy(currentAnomalyInstance);
        }

        currentAnomaly = null;
        currentAnomalyInstance = null;
    }

    private void SetState(BeatState newState)
    {
        currentState = newState;
        GameEvents.RaiseBeatStateChanged(newState);
    }

    private void ClearPendingAction()
    {
        pendingAction = new SubmittedAction(PlayerAction.None, -1);
        hasPendingAction = false;
    }

    private IEnumerator WaitForSecondsFromDefinition(float seconds)
    {
        if (seconds <= 0f)
        {
            yield break;
        }

        yield return new WaitForSeconds(seconds);
    }
}
