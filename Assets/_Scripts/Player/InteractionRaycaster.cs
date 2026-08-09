using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InteractionRaycaster : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private BeatStateMachine beatStateMachine;
    [SerializeField] private Image holdGaugeImage;

    [Header("Raycast")]
    [SerializeField, Min(0f)] private float interactDistance = 2f;
    [SerializeField] private LayerMask interactableLayers;

    [Header("Hold")]
    [SerializeField, Min(0.01f)] private float holdSeconds = 1.2f;

    private Interactable currentTarget;
    private BeatState currentBeatState = BeatState.Travel;
    private float holdTimer;
    private bool isHolding;
    private bool submittedForCurrentPress;

    private bool CanCommit => currentBeatState == BeatState.Diagnosis || currentBeatState == BeatState.Grace;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (beatStateMachine == null)
        {
            beatStateMachine = FindFirstObjectByType<BeatStateMachine>();
        }

        if (interactableLayers.value == 0)
        {
            interactableLayers = LayerMask.GetMask("Interactable");
        }

        ConfigureGauge();
    }

    private void OnEnable()
    {
        GameEvents.OnBeatStateChanged += HandleBeatStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnBeatStateChanged -= HandleBeatStateChanged;
        SetCurrentTarget(null);
        ResetHold();
    }

    private void Update()
    {
        ScanForInteractable();
        UpdateHold();
    }

    private void ScanForInteractable()
    {
        if (playerCamera == null)
        {
            SetCurrentTarget(null);
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableLayers, QueryTriggerInteraction.Ignore))
        {
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();
            SetCurrentTarget(interactable);
            return;
        }

        SetCurrentTarget(null);
    }

    private void UpdateHold()
    {
        if (Mouse.current == null)
        {
            ResetHold();
            return;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            submittedForCurrentPress = false;
            ResetHold();
            return;
        }

        if (currentTarget == null || !CanCommit || submittedForCurrentPress)
        {
            ResetHold();
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            BeginHold();
        }

        if (!Mouse.current.leftButton.isPressed)
        {
            ResetHold();
            return;
        }

        if (!isHolding)
        {
            return;
        }

        holdTimer += Time.deltaTime;
        SetGaugeFill(holdTimer / holdSeconds);

        if (holdTimer >= holdSeconds)
        {
            CommitCurrentTarget();
        }
    }

    private void BeginHold()
    {
        isHolding = true;
        holdTimer = 0f;
        SetGaugeVisible(true);
        SetGaugeFill(0f);
    }

    private void CommitCurrentTarget()
    {
        if (beatStateMachine == null || currentTarget == null)
        {
            ResetHold();
            return;
        }

        PlayerAction action = currentTarget.ActionType;
        int floorNumber = currentTarget.FloorNumber;
        Debug.Log($"[Interaction] Commit action={action} floor={floorNumber}", this);
        beatStateMachine.SubmitAction(action, floorNumber);

        submittedForCurrentPress = true;
        ResetHold();
    }

    private void SetCurrentTarget(Interactable nextTarget)
    {
        if (currentTarget == nextTarget)
        {
            return;
        }

        if (currentTarget != null)
        {
            currentTarget.SetHovered(false);
        }

        currentTarget = nextTarget;

        if (currentTarget != null)
        {
            currentTarget.SetHovered(true);
        }

        ResetHold();
    }

    private void HandleBeatStateChanged(BeatState newState)
    {
        currentBeatState = newState;

        if (!CanCommit)
        {
            ResetHold();
        }
    }

    private void ResetHold()
    {
        isHolding = false;
        holdTimer = 0f;
        SetGaugeFill(0f);
        SetGaugeVisible(false);
    }

    private void ConfigureGauge()
    {
        if (holdGaugeImage == null)
        {
            return;
        }

        holdGaugeImage.type = Image.Type.Filled;
        holdGaugeImage.fillMethod = Image.FillMethod.Radial360;
        holdGaugeImage.fillOrigin = (int)Image.Origin360.Top;
        holdGaugeImage.fillClockwise = false;
        SetGaugeFill(0f);
        SetGaugeVisible(false);
    }

    private void SetGaugeFill(float normalizedValue)
    {
        if (holdGaugeImage != null)
        {
            holdGaugeImage.fillAmount = Mathf.Clamp01(normalizedValue);
        }
    }

    private void SetGaugeVisible(bool visible)
    {
        if (holdGaugeImage != null)
        {
            holdGaugeImage.gameObject.SetActive(visible);
        }
    }
}
