using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The introduction's normal journey. Encounter/night sequencing remains owned by RunManager.
public class NormalJourneyController : MonoBehaviour
{
    public enum JourneyState { WaitingForCall, Boarding, Closing, Travelling, EmergencyStopped, Arrived, Complete }

    [SerializeField] private ElevatorController elevator;
    [SerializeField] private FloorIndicator indicator;
    [SerializeField] private CharacterController passenger;
    [SerializeField] private BoxCollider cabin;
    [SerializeField] private GameObject entranceHall;
    [SerializeField] private GameObject homeCorridor;
    [SerializeField] private Image fade;
    [SerializeField] private TMP_Text completionText;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private RunManager encounterRun;
    [SerializeField] private bool routeThroughEncounterRun;
    [SerializeField, Min(0.1f)] private float travelSeconds = 4f;
    [SerializeField, Min(0.1f)] private float emergencyStopSeconds = 1f;
    [SerializeField, Min(0f)] private float fadeSeconds = 0.75f;
    [SerializeField] private bool showCompletionText = true;

    public JourneyState State { get; private set; }
    public int AcceptedEmergencyStops { get; private set; }
    internal float EmergencyStopSeconds => emergencyStopSeconds;
    private int changedFrame = -1;
    private float travelRemaining;
    private float stopRemaining;
    private bool homePresented;
    private Vector3 nightStartPosition;
    private Quaternion nightStartRotation;
    internal event System.Action IntroductionCompleted;
    internal void SetNightStartPose(Vector3 position, Quaternion rotation)
    {
        nightStartPosition = position; nightStartRotation = rotation;
    }

    internal void SetDemoNight(bool encounters)
    {
        routeThroughEncounterRun = encounters;
        if (fade != null) fade.gameObject.SetActive(false);
        if (playerLook != null) playerLook.enabled = true;
    }

    public bool IsBodyInside
    {
        get => CabinOccupancy.FullyContains(cabin, passenger);
    }

    private void Awake()
    {
        if (passenger != null)
        {
            nightStartPosition = passenger.transform.position;
            nightStartRotation = passenger.transform.rotation;
        }
    }

    private void Start()
    {
        if (fade != null) fade.gameObject.SetActive(false);
        if (completionText != null) completionText.gameObject.SetActive(false);
        RestartNightAtEntrance();
    }

    private void Update()
    {
        if (State == JourneyState.EmergencyStopped)
        {
            stopRemaining -= Time.deltaTime;
            if (stopRemaining <= 0f)
            {
                elevator.SetTravelling(true);
                ChangeState(JourneyState.Travelling);
            }
        }
        else if (State == JourneyState.Travelling)
        {
            travelRemaining -= Time.deltaTime;
            elevator.SetTravelProgress(1f - travelRemaining / travelSeconds);
            indicator?.SetFloor(Mathf.Clamp(1 + Mathf.FloorToInt(7f * (1f - travelRemaining / travelSeconds)), 1, 8));
            if (travelRemaining <= 0f)
            {
                if (routeThroughEncounterRun && encounterRun != null && !homePresented)
                {
                    ChangeState(JourneyState.Closing);
                    enabled = false;
                    encounterRun.BeginEncounterRun();
                }
                else
                {
                    StartCoroutine(ArriveHome());
                }
            }
        }
    }

    public void SubmitAction(PlayerAction action, int floorNumber = -1)
    {
        if (Time.frameCount == changedFrame || State == JourneyState.Complete || elevator == null) return;
        if (State == JourneyState.Travelling)
        {
            if (action == PlayerAction.PressEmergencyStop)
            {
                AcceptedEmergencyStops++;
                stopRemaining = emergencyStopSeconds;
                elevator.SetTravelling(false);
                ChangeState(JourneyState.EmergencyStopped);
                if (routeThroughEncounterRun) encounterRun?.AcceptNormalStop();
            }
            return; // Introduction is exempt; close/open/floors are inert.
        }
        if (State == JourneyState.Closing || State == JourneyState.EmergencyStopped || elevator.IsDoorMoving) return;

        if (action == PlayerAction.PressOpen)
        {
            elevator.OpenDoors();
            if (State == JourneyState.WaitingForCall) ChangeState(JourneyState.Boarding);
        }
        else if (State == JourneyState.Boarding && action == PlayerAction.PressFloor && floorNumber == 8 && IsBodyInside)
        {
            elevator.SelectDestination(true);
            StartCoroutine(CloseAndTravel());
        }
        else if (action == PlayerAction.PressClose && (State == JourneyState.Boarding || State == JourneyState.Arrived))
        {
            StartCoroutine(State == JourneyState.Boarding ? CloseWithoutDeparture() : CloseAndReopen());
        }
        else if (State == JourneyState.Arrived && action == PlayerAction.TouchHomeDoor && !IsBodyInside)
        {
            ChangeState(JourneyState.Complete);
            if (routeThroughEncounterRun && encounterRun != null)
            {
                encounterRun.CompleteRunAfterHome();
            }
            else
            {
                StartCoroutine(CompleteIntroduction());
            }
        }
    }

    public void PresentHomeAfterEncounters()
    {
        if (!routeThroughEncounterRun)
        {
            return;
        }

        enabled = true;
        StartCoroutine(ArriveHome());
    }

    public void PrepareEncounterEnvironment()
    {
        // Swap while doors are closed. False arrivals must be comparable with the real eighth floor.
        // Keep homePresented false: only completing the encounter run permits home-door completion.
        if (!routeThroughEncounterRun) return;
        if (entranceHall != null) entranceHall.SetActive(false);
        if (homeCorridor != null) homeCorridor.SetActive(true);
    }

    public void RestartNightAtEntrance()
    {
        StopAllCoroutines();
        enabled = true;
        homePresented = false;
        travelRemaining = 0f;
        stopRemaining = 0f;
        elevator?.SetTravelling(false);
        elevator?.ResetDoorsClosed();
        indicator?.SetFloor(1);
        if (entranceHall != null) entranceHall.SetActive(true);
        if (homeCorridor != null) homeCorridor.SetActive(false);
        if (completionText != null) completionText.gameObject.SetActive(false);

        if (passenger != null)
        {
            bool wasEnabled = passenger.enabled;
            passenger.enabled = false;
            passenger.transform.SetPositionAndRotation(nightStartPosition, nightStartRotation);
            passenger.enabled = wasEnabled;
            Physics.SyncTransforms();
        }

        playerLook?.ResetView(nightStartRotation);
        ChangeState(JourneyState.WaitingForCall);
    }

    private IEnumerator CloseAndTravel()
    {
        ChangeState(JourneyState.Closing);
        elevator.CloseDoors();
        while (elevator.IsDoorMoving) yield return null;
        if (elevator.LastCloseObstructed || !IsBodyInside)
        {
            elevator.SelectDestination(false);
            elevator.OpenDoors();
            ChangeState(JourneyState.Boarding);
            yield break;
        }
        travelRemaining = travelSeconds;
        elevator.SetTravelling(true);
        ChangeState(JourneyState.Travelling);
    }

    private IEnumerator CloseWithoutDeparture()
    {
        ChangeState(JourneyState.Closing);
        elevator.CloseDoors();
        // The elevator's existing safety sensor reopens an obstructed door.
        while (elevator.IsDoorMoving) yield return null;
        ChangeState(JourneyState.Boarding);
    }

    private IEnumerator CloseAndReopen()
    {
        ChangeState(JourneyState.Closing);
        elevator.CloseDoors();
        while (elevator.IsDoorMoving) yield return null;
        if (!elevator.LastCloseObstructed) yield return new WaitForSeconds(0.5f);
        elevator.OpenDoors();
        while (elevator.IsDoorMoving) yield return null;
        ChangeState(homePresented ? JourneyState.Arrived : JourneyState.Boarding);
    }

    private IEnumerator ArriveHome()
    {
        ChangeState(JourneyState.Closing); // Discard arrival-transition clicks.
        elevator.SetTravelling(false);
        indicator?.SetFloor(8);
        elevator.PlayArrival();
        yield return new WaitForSeconds(elevator.ArrivalDoorDelay);
        homePresented = true;
        if (entranceHall != null) entranceHall.SetActive(false);
        if (homeCorridor != null) homeCorridor.SetActive(true);
        elevator.OpenDoors();
        while (elevator.IsDoorMoving) yield return null;
        ChangeState(JourneyState.Arrived);
    }

    private IEnumerator CompleteIntroduction()
    {
        if (playerLook != null) playerLook.enabled = false;
        if (fade != null)
        {
            fade.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.deltaTime;
                fade.color = new Color(0, 0, 0, Mathf.Clamp01(elapsed / fadeSeconds));
                yield return null;
            }
            fade.color = Color.black;
        }
        if (completionText != null) completionText.gameObject.SetActive(showCompletionText);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        IntroductionCompleted?.Invoke();
    }

    private void ChangeState(JourneyState state)
    {
        State = state;
        changedFrame = Time.frameCount;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[NormalJourney] {state} bodyInside={IsBodyInside} stops={AcceptedEmergencyStops}", this);
#endif
    }
}
