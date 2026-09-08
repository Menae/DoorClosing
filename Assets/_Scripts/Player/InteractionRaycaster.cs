using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InteractionRaycaster : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private BeatStateMachine beatStateMachine;
    [SerializeField] private NormalJourneyController normalJourney;
    // Keep the existing scene reference so legacy gauges can be hidden without scene migration.
    [SerializeField, HideInInspector] private Image holdGaugeImage;

    [Header("Raycast")]
    [SerializeField, Min(0f)] private float interactDistance = 2f;
    [SerializeField] private LayerMask interactableLayers;

    private Interactable currentTarget;
    private int stateChangedFrame = -1;
    private int processedPressFrame = -1;

    private void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (beatStateMachine == null) beatStateMachine = FindFirstObjectByType<BeatStateMachine>();
        if (interactableLayers.value == 0) interactableLayers = LayerMask.GetMask("Interactable");
        HideLegacyGauge();
    }

    private void OnEnable()
    {
        GameEvents.OnBeatStateChanged += HandleBeatStateChanged;
        stateChangedFrame = Time.frameCount;
        HideLegacyGauge();
    }

    private void OnDisable()
    {
        GameEvents.OnBeatStateChanged -= HandleBeatStateChanged;
        SetCurrentTarget(null);
        HideLegacyGauge();
    }

    private void Update()
    {
        ScanForInteractable();
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame || processedPressFrame == Time.frameCount)
            return;

        // Consume the edge even if the target/state is invalid. Never buffer a held press.
        processedPressFrame = Time.frameCount;
        // Input is processed before Update; a state opened this frame must not inherit that input.
        if (Time.frameCount == stateChangedFrame || currentTarget == null)
            return;

        if (normalJourney != null && normalJourney.isActiveAndEnabled)
            normalJourney.SubmitAction(currentTarget.ActionType, currentTarget.FloorNumber);
        else if (beatStateMachine != null)
            beatStateMachine.SubmitAction(currentTarget.ActionType, currentTarget.FloorNumber);
    }

    private void ScanForInteractable()
    {
        if (playerCamera == null)
        {
            SetCurrentTarget(null);
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && (interactableLayers.value & (1 << hit.collider.gameObject.layer)) != 0)
        {
            SetCurrentTarget(hit.collider.GetComponentInParent<Interactable>());
            return;
        }
        SetCurrentTarget(null);
    }

    private void SetCurrentTarget(Interactable nextTarget)
    {
        if (currentTarget == nextTarget) return;
        if (currentTarget != null) currentTarget.SetHovered(false);
        currentTarget = nextTarget;
        if (currentTarget != null) currentTarget.SetHovered(true);
    }

    private void HandleBeatStateChanged(BeatState newState) => stateChangedFrame = Time.frameCount;

    private void HideLegacyGauge()
    {
        if (holdGaugeImage == null) return;
        holdGaugeImage.fillAmount = 0f;
        holdGaugeImage.gameObject.SetActive(false);
    }
}
