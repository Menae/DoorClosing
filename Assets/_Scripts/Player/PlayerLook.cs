using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private CharacterController characterController;

    [Header("Look")]
    [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
    [SerializeField, Range(0f, 89f)] private float pitchLimit = 80f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 2.5f;
    [SerializeField] private bool lockCursorOnStart = true;
    [SerializeField] private bool enableSprint;
    [SerializeField, Min(0f)] private float sprintSpeed = 4f;
    [SerializeField] private bool applyGravity;

    private float pitch;
    private float verticalSpeed;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }
    }

    private void Start()
    {
        if (lockCursorOnStart)
        {
            LockCursor();
        }
    }

    private void Update()
    {
        HandleCursorLock();

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Look();
        }

        Move();
    }

    private void HandleCursorLock()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            UnlockCursor();
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }
    }

    private void Look()
    {
        if (Mouse.current == null || cameraTransform == null)
        {
            return;
        }

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float yaw = mouseDelta.x * mouseSensitivity;
        float pitchDelta = mouseDelta.y * mouseSensitivity;

        transform.Rotate(Vector3.up, yaw, Space.Self);

        pitch = Mathf.Clamp(pitch - pitchDelta, -pitchLimit, pitchLimit);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move()
    {
        if (Keyboard.current == null || moveSpeed <= 0f)
        {
            return;
        }

        Vector2 input = Vector2.zero;
        if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        if (Keyboard.current.dKey.isPressed) input.x += 1f;
        if (Keyboard.current.sKey.isPressed) input.y -= 1f;
        if (Keyboard.current.wKey.isPressed) input.y += 1f;

        input = Vector2.ClampMagnitude(input, 1f);
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        float speed = enableSprint && Keyboard.current.leftShiftKey.isPressed ? sprintSpeed : moveSpeed;
        move *= speed * Time.deltaTime;

        if (characterController != null)
        {
            if (applyGravity)
            {
                if (characterController.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
                verticalSpeed += Physics.gravity.y * Time.deltaTime;
                move.y = verticalSpeed * Time.deltaTime;
            }
            characterController.Move(move);
            return;
        }

        transform.position += move;
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
