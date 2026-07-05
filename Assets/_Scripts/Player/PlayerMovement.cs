using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3.0f;
    [SerializeField] private float gravity = -9.81f;

    private CharacterController _controller;
    private Vector3 _velocity;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Move();
        ApplyGravity();
    }

    private void Move()
    {
        // 新Input SystemはKeyboardクラスで取得
        Vector2 input = Vector2.zero;
        if (Keyboard.current.dKey.isPressed) input.x += 1f;
        if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        if (Keyboard.current.wKey.isPressed) input.y += 1f;
        if (Keyboard.current.sKey.isPressed) input.y -= 1f;

        Vector3 move = transform.right * input.x + transform.forward * input.y;
        _controller.Move(move * moveSpeed * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (_controller.isGrounded && _velocity.y < 0)
            _velocity.y = -2f;

        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }
}