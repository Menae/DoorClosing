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
    [SerializeField] private CharacterController passenger;
    [SerializeField] private BoxCollider cabin;

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

    public bool IsPassengerInsideCabin => CabinOccupancy.FullyContains(cabin, passenger);

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
        if (!acceptsPlayerAction || hasPendingAction)
        {
            return;
        }

        // These controls are inert during encounter diagnosis/correction (GAME-006/008).
        // Normal departure/open-door routing is handled separately from encounter responses.
        if (action == PlayerAction.PressFloor || action == PlayerAction.None ||
            (action == PlayerAction.PressOpen && currentBeat.Category != AnomalyCategory.Normal))
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
            bool completedPassively = false;
            yield return WaitForDiagnosisCommit(def, value => completedPassively = value);

            if (completedPassively)
            {
                yield return ResolveAndDepart(def);
                GameEvents.RaiseBeatResolved(BeatOutcome.Correct);
                yield break;
            }

            SetState(BeatState.Committed);
            GameEvents.RaisePlayerCommitted(pendingAction.Action);

            if (IsLureCloseOutside(def, pendingAction))
            {
                yield return CloseDoorsAndWait();
                CleanupCurrentAnomaly();
                GameEvents.RaiseBeatResolved(BeatOutcome.Death);
                beatRoutine = null;
                yield break;
            }

            BeatOutcome diagnosisOutcome = Evaluate(pendingAction);
            if (diagnosisOutcome == BeatOutcome.Correct)
            {
                bool lureDoorsClosed = false;
                if (def.Category == AnomalyCategory.Lure)
                {
                    yield return TryCloseLureDoors(value => lureDoorsClosed = value);
                    if (!lureDoorsClosed)
                    {
                        ClearPendingAction();
                        continue;
                    }
                }

                yield return ResolveAndDepart(def, lureDoorsClosed);
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
            bool recoveryClosedLureDoors = false;
            yield return WaitForGraceRecovery(def, (value, doorsClosed) =>
            {
                recovered = value;
                recoveryClosedLureDoors = doorsClosed;
            });

            if (recovered)
            {
                currentAnomaly?.OnGraceEnd(true);
                yield return ResolveAndDepart(def, recoveryClosedLureDoors);
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

    private IEnumerator ResolveAndDepart(BeatDefinition def, bool doorsAlreadyClosed = false)
    {
        SetState(BeatState.Resolve);
        if (def.Category != AnomalyCategory.Provocation)
        {
            elevatorController?.SetTravelling(false);
        }
        yield return WaitForSecondsFromDefinition(resolveSeconds);

        SetState(BeatState.Depart);
        if (!doorsAlreadyClosed)
        {
            elevatorController?.CloseDoors();
            yield return WaitForSecondsFromDefinition(doorCloseSeconds);
        }

        if (def.Category != AnomalyCategory.Normal)
        {
            elevatorController?.SetTravelling(true);
        }

        CleanupCurrentAnomaly();
        beatRoutine = null;
    }

    private IEnumerator WaitForDiagnosisCommit(BeatDefinition def, System.Action<bool> onPassiveCompletion)
    {
        ClearPendingAction();
        acceptsPlayerAction = true;

        float elapsedSeconds = 0f;
        bool lureWasInside = def.Category != AnomalyCategory.Lure || IsPassengerInsideCabin;
        while (!hasPendingAction)
        {
            if (def.Category == AnomalyCategory.Lure && passenger != null && cabin != null)
            {
                if (IsPassengerInsideCabin)
                {
                    lureWasInside = true;
                }
                else if (lureWasInside)
                {
                    pendingAction = new SubmittedAction(PlayerAction.ExitCab, -1);
                    hasPendingAction = true;
                    break;
                }
            }

            if (def.HasHijackDeadline && elapsedSeconds >= def.HijackDeadlineSeconds)
            {
                pendingAction = new SubmittedAction(PlayerAction.None, -1);
                hasPendingAction = true;
                break;
            }

            if (def.HasPassiveSuccess && elapsedSeconds >= def.PassiveSuccessSeconds)
            {
                acceptsPlayerAction = false;
                onPassiveCompletion?.Invoke(true);
                yield break;
            }

            elapsedSeconds += Time.deltaTime;
            yield return null;
        }

        acceptsPlayerAction = false;
        onPassiveCompletion?.Invoke(false);
    }

    private IEnumerator WaitForGraceRecovery(BeatDefinition def, System.Action<bool, bool> onComplete)
    {
        ClearPendingAction();
        acceptsPlayerAction = true;

        float remainingSeconds = def.GraceSeconds;
        while (remainingSeconds > 0f)
        {
            if (hasPendingAction)
            {
                GameEvents.RaisePlayerCommitted(pendingAction.Action);

                if (def.Category == AnomalyCategory.Lure && pendingAction.Action == PlayerAction.PressClose)
                {
                    acceptsPlayerAction = false;
                    if (!IsPassengerInsideCabin)
                    {
                        yield return CloseDoorsAndWait();
                        onComplete?.Invoke(false, false);
                        yield break;
                    }

                    bool doorsClosed = false;
                    yield return TryCloseLureDoors(value => doorsClosed = value);
                    if (doorsClosed)
                    {
                        onComplete?.Invoke(true, true);
                        yield break;
                    }

                    ClearPendingAction();
                    acceptsPlayerAction = true;
                    continue;
                }

                if (Evaluate(pendingAction) == BeatOutcome.Correct)
                {
                    acceptsPlayerAction = false;
                    onComplete?.Invoke(true, false);
                    yield break;
                }

                acceptsPlayerAction = false;
                onComplete?.Invoke(false, false);
                yield break;
            }

            remainingSeconds -= Time.deltaTime;
            yield return null;
        }

        acceptsPlayerAction = false;
        onComplete?.Invoke(def.HasPassiveSuccess, false);
    }

    private bool IsLureCloseOutside(BeatDefinition def, SubmittedAction action)
    {
        return def.Category == AnomalyCategory.Lure && action.Action == PlayerAction.PressClose
            && !IsPassengerInsideCabin;
    }

    private IEnumerator TryCloseLureDoors(System.Action<bool> onComplete)
    {
        if (elevatorController == null || !IsPassengerInsideCabin)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        yield return CloseDoorsAndWait();
        if (!elevatorController.LastCloseObstructed && IsPassengerInsideCabin)
        {
            onComplete?.Invoke(true);
            yield break;
        }

        if (!elevatorController.IsDoorOpen)
        {
            elevatorController.OpenDoors();
            while (elevatorController.IsDoorMoving) yield return null;
        }

        onComplete?.Invoke(false);
    }

    private IEnumerator CloseDoorsAndWait()
    {
        if (elevatorController == null) yield break;
        elevatorController.CloseDoors();
        while (elevatorController.IsDoorMoving) yield return null;
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
