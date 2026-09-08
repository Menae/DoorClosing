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
    [SerializeField, Min(0.1f)] private float travelSeconds = 4f;
    [SerializeField, Min(0.1f)] private float emergencyStopSeconds = 1f;
    [SerializeField, Min(0f)] private float fadeSeconds = 0.75f;

    public JourneyState State { get; private set; }
    public int AcceptedEmergencyStops { get; private set; }
    private int changedFrame = -1;
    private float travelRemaining;
    private float stopRemaining;
    private bool homePresented;

    public bool IsBodyInside
    {
        get
        {
            if (passenger == null || cabin == null) return false;
            var body = passenger.bounds;
            var space = cabin.bounds;
            // Full horizontal body must clear the threshold; floor contact is not an exclusion.
            return body.min.x >= space.min.x && body.max.x <= space.max.x
                && body.min.z >= space.min.z && body.max.z <= space.max.z
                && space.Contains(body.center);
        }
    }

    private void Start()
    {
        if (entranceHall != null) entranceHall.SetActive(true);
        if (homeCorridor != null) homeCorridor.SetActive(false);
        if (fade != null) fade.gameObject.SetActive(false);
        if (completionText != null) completionText.gameObject.SetActive(false);
        indicator?.SetFloor(1);
        ChangeState(JourneyState.WaitingForCall);
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
            indicator?.SetFloor(Mathf.Clamp(1 + Mathf.FloorToInt(7f * (1f - travelRemaining / travelSeconds)), 1, 8));
            if (travelRemaining <= 0f) StartCoroutine(ArriveHome());
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
            }
            return; // Introduction has no extra-anomaly draw; close/open/floors are inert.
        }
        if (State == JourneyState.Closing || State == JourneyState.EmergencyStopped || elevator.IsDoorMoving) return;

        if (action == PlayerAction.PressOpen)
        {
            elevator.OpenDoors();
            if (State == JourneyState.WaitingForCall) ChangeState(JourneyState.Boarding);
        }
        else if (State == JourneyState.Boarding && action == PlayerAction.PressFloor && floorNumber == 8 && IsBodyInside)
        {
            StartCoroutine(CloseAndTravel());
        }
        else if (action == PlayerAction.PressClose && (State == JourneyState.Boarding || State == JourneyState.Arrived))
        {
            StartCoroutine(CloseAndReopen());
        }
        else if (State == JourneyState.Arrived && action == PlayerAction.TouchHomeDoor && !IsBodyInside)
        {
            ChangeState(JourneyState.Complete);
            StartCoroutine(CompleteIntroduction());
        }
    }

    private IEnumerator CloseAndTravel()
    {
        ChangeState(JourneyState.Closing);
        elevator.CloseDoors();
        while (elevator.IsDoorMoving) yield return null;
        if (elevator.LastCloseObstructed || !IsBodyInside)
        {
            elevator.OpenDoors();
            ChangeState(JourneyState.Boarding);
            yield break;
        }
        travelRemaining = travelSeconds;
        elevator.SetTravelling(true);
        ChangeState(JourneyState.Travelling);
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
        if (completionText != null) completionText.gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
