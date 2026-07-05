using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] private float mouseSensitivity = 0.1f;

    [Header("Vertical Clamp")]
    [SerializeField] private float verticalClamp = 80.0f;

    [Header("Smoothing")]
    [Tooltip("値が大きいほど遅延が強くなる。0にすると即時反応（従来の挙動）")]
    [SerializeField] private float smoothTime = 0.08f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    // ─── 目標値（マウス入力を即時加算する先） ────────────
    private float _targetXRotation = 0f; // 上下の目標角度
    private float _targetYRotation = 0f; // 左右の目標角度

    // ─── 現在値（SmoothDampで目標値に追従する） ──────────
    private float _currentXRotation = 0f;
    private float _currentYRotation = 0f;

    // ─── SmoothDampの速度変数（内部計算用・触らない） ────
    private float _xVelocity = 0f;
    private float _yVelocity = 0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 初期YRotationをPlayerの現在角度に合わせる
        // （ゲーム開始時に突然回転しないようにするため）
        _targetYRotation = transform.eulerAngles.y;
        _currentYRotation = transform.eulerAngles.y;
    }

    private void Update()
    {
        ReadInput();
        ApplySmoothedRotation();
    }

    // ─── マウス入力を目標値に加算するだけ ────────────────
    private void ReadInput()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        // 目標値を更新（現在値は更新しない）
        _targetYRotation += mouseX;

        _targetXRotation -= mouseY;
        _targetXRotation = Mathf.Clamp(_targetXRotation, -verticalClamp, verticalClamp);
    }

    // ─── SmoothDampで現在値を目標値に追従させて反映 ──────
    private void ApplySmoothedRotation()
    {
        // SmoothDamp：目標値に向かって滑らかに加減速しながら追従する
        // smoothTime が大きいほどゆっくり追いつく
        _currentXRotation = Mathf.SmoothDamp(
            _currentXRotation,
            _targetXRotation,
            ref _xVelocity,
            smoothTime
        );

        _currentYRotation = Mathf.SmoothDamp(
            _currentYRotation,
            _targetYRotation,
            ref _yVelocity,
            smoothTime
        );

        // 水平回転：Playerオブジェクトごと回す
        transform.rotation = Quaternion.Euler(0f, _currentYRotation, 0f);

        // 垂直回転：Cameraのみ
        cameraTransform.localRotation = Quaternion.Euler(_currentXRotation, 0f, 0f);
    }

    public void SetSensitivity(float value)
    {
        mouseSensitivity = value;
    }

    public void SetSmoothTime(float value)
    {
        smoothTime = value;
    }
}