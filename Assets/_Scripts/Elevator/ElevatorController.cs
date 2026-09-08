using System.Collections;
using UnityEngine;

public class ElevatorController : MonoBehaviour
{
    [Header("Doors")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;
    [SerializeField] private Vector3 leftDoorOpenLocalOffset = new Vector3(-0.75f, 0f, 0f);
    [SerializeField] private Vector3 rightDoorOpenLocalOffset = new Vector3(0.75f, 0f, 0f);
    [SerializeField, Min(0f)] private float doorSlideSeconds = 1f;
    [SerializeField] private AnimationCurve doorCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private BoxCollider doorSafetyZone;
    [SerializeField] private CharacterController passengerBody;

    public bool IsDoorMoving { get; private set; }
    public bool IsDoorOpen { get; private set; }
    public bool LastCloseObstructed { get; private set; }

    [Header("Travel Feel")]
    [SerializeField] private Transform cameraShakeTarget;
    [SerializeField, Min(0f)] private float cameraShakeAmplitude = 0.015f;
    [SerializeField, Min(0f)] private float cameraShakeFrequency = 18f;
    [SerializeField] private AudioSource travelLoopAudioSource;

    private Vector3 leftDoorClosedLocalPosition;
    private Vector3 rightDoorClosedLocalPosition;
    private Vector3 cameraShakeBaseLocalPosition;
    private Coroutine doorRoutine;
    private bool isTravelling;

    private void Awake()
    {
        if (leftDoor != null)
        {
            leftDoorClosedLocalPosition = leftDoor.localPosition;
        }

        if (rightDoor != null)
        {
            rightDoorClosedLocalPosition = rightDoor.localPosition;
        }

        if (cameraShakeTarget != null)
        {
            cameraShakeBaseLocalPosition = cameraShakeTarget.localPosition;
        }
    }

    private void Update()
    {
        UpdateTravelShake();
    }

    public void OpenDoors()
    {
        StartDoorMove(1f);
    }

    public void CloseDoors()
    {
        LastCloseObstructed = false;
        StartDoorMove(0f);
    }

    public void SetTravelling(bool travelling)
    {
        if (isTravelling == travelling)
        {
            return;
        }

        isTravelling = travelling;

        if (travelLoopAudioSource != null)
        {
            if (isTravelling)
            {
                travelLoopAudioSource.loop = true;
                travelLoopAudioSource.Play();
            }
            else
            {
                travelLoopAudioSource.Stop();
            }
        }

        if (!isTravelling)
        {
            ResetCameraShake();
        }
    }

    private void StartDoorMove(float targetOpenAmount)
    {
        if (doorRoutine != null)
        {
            StopCoroutine(doorRoutine);
        }

        IsDoorMoving = true;
        doorRoutine = StartCoroutine(MoveDoors(targetOpenAmount));
    }

    private IEnumerator MoveDoors(float targetOpenAmount)
    {
        if (targetOpenAmount == 0f && IsDoorwayOccupied())
        {
            LastCloseObstructed = true;
            yield return MoveDoors(1f);
            yield break;
        }
        Vector3 leftStart = leftDoor != null ? leftDoor.localPosition : Vector3.zero;
        Vector3 rightStart = rightDoor != null ? rightDoor.localPosition : Vector3.zero;
        Vector3 leftTarget = Vector3.Lerp(leftDoorClosedLocalPosition, leftDoorClosedLocalPosition + leftDoorOpenLocalOffset, targetOpenAmount);
        Vector3 rightTarget = Vector3.Lerp(rightDoorClosedLocalPosition, rightDoorClosedLocalPosition + rightDoorOpenLocalOffset, targetOpenAmount);

        if (doorSlideSeconds <= 0f)
        {
            ApplyDoorPositions(leftTarget, rightTarget);
            IsDoorOpen = targetOpenAmount == 1f;
            IsDoorMoving = false;
            doorRoutine = null;
            yield break;
        }

        float elapsedSeconds = 0f;
        while (elapsedSeconds < doorSlideSeconds)
        {
            if (targetOpenAmount == 0f && IsDoorwayOccupied())
            {
                LastCloseObstructed = true;
                yield return MoveDoors(1f);
                yield break;
            }
            elapsedSeconds += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedSeconds / doorSlideSeconds);
            float eased = doorCurve != null ? doorCurve.Evaluate(normalizedTime) : normalizedTime;
            ApplyDoorPositions(Vector3.Lerp(leftStart, leftTarget, eased), Vector3.Lerp(rightStart, rightTarget, eased));
            yield return null;
        }

        ApplyDoorPositions(leftTarget, rightTarget);
        IsDoorOpen = targetOpenAmount == 1f;
        IsDoorMoving = false;
        doorRoutine = null;
    }

    private bool IsDoorwayOccupied()
    {
        return doorSafetyZone != null && passengerBody != null && passengerBody.enabled
            && doorSafetyZone.bounds.Intersects(passengerBody.bounds);
    }

    private void ApplyDoorPositions(Vector3 leftPosition, Vector3 rightPosition)
    {
        if (leftDoor != null)
        {
            leftDoor.localPosition = leftPosition;
        }

        if (rightDoor != null)
        {
            rightDoor.localPosition = rightPosition;
        }
    }

    private void UpdateTravelShake()
    {
        if (!isTravelling || cameraShakeTarget == null || cameraShakeAmplitude <= 0f || cameraShakeFrequency <= 0f)
        {
            return;
        }

        float verticalOffset = Mathf.Sin(Time.time * cameraShakeFrequency) * cameraShakeAmplitude;
        float horizontalOffset = Mathf.Cos(Time.time * cameraShakeFrequency * 0.73f) * cameraShakeAmplitude * 0.5f;
        cameraShakeTarget.localPosition = cameraShakeBaseLocalPosition + new Vector3(horizontalOffset, verticalOffset, 0f);
    }

    private void ResetCameraShake()
    {
        if (cameraShakeTarget != null)
        {
            cameraShakeTarget.localPosition = cameraShakeBaseLocalPosition;
        }
    }
}
