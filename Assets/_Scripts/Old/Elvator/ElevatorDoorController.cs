using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// エレベーターの左右ドアを制御する。
/// 開閉ボタンのOnButtonPressedから
/// RequestOpen / RequestClose を呼んでもらう。
///
/// 将来のElevatorMovementSystemは
/// SetElevatorMoving(bool) を呼ぶか、
/// IElevatorStatusを実装したコンポーネントを
/// RegisterElevatorStatus() で登録する。
/// </summary>
public class ElevatorDoorController : MonoBehaviour
{
    // ─── ドアの参照 ───────────────────────────────────────
    [Header("Door Transforms")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;

    // ─── ドアの移動距離 ───────────────────────────────────
    [Header("Door Movement")]
    [Tooltip("ドアが開閉時に動く距離(m)。0にすると左ドアのScaleXから自動計算する。")]
    [SerializeField] private float doorOpenDistance = 0f;

    // ─── アニメーション設定 ───────────────────────────────
    [Header("Animation")]
    [Tooltip("ドアが開ききるまでの秒数")]
    [SerializeField] private float openDuration = 1.8f;

    [Tooltip("ドアが閉まりきるまでの秒数")]
    [SerializeField] private float closeDuration = 2.0f;

    [Tooltip("開ボタンを押してからドアが動き始めるまでの猶予時間(秒)")]
    [SerializeField] private float openDelay = 0f;

    [Tooltip("全開後、自動で閉じるまでの待機秒数。0以下なら自動で閉じない")]
    [SerializeField] private float autoCloseDelay = 4.0f;

    // ─── イベント（将来の演出・SE接続用） ────────────────
    [Header("Events")]
    public UnityEvent OnDoorFullyOpened;
    public UnityEvent OnDoorFullyClosed;

    // ─── 内部状態 ─────────────────────────────────────────
    public enum DoorState { Closed, Opening, Open, Closing }
    private DoorState _state = DoorState.Closed;

    // ドアの開閉位置（Awakeで自動計算）
    private Vector3 _leftClosedPos;
    private Vector3 _rightClosedPos;
    private Vector3 _leftOpenPos;
    private Vector3 _rightOpenPos;

    // エレベーター移動状態の参照
    private IElevatorStatus _elevatorStatus;

    // 暫定フラグ（IElevatorStatusが登録されるまでの代替）
    private bool _isMovingFallback = false;

    private Coroutine _doorCoroutine;

    // =====================================================
    private void Awake()
    {
        CacheDoorPositions();
    }

    /// <summary>
    /// Inspector値またはScaleXからOpen/Closed位置を計算する。
    /// doorOpenDistanceが0以下の場合はleftDoor.localScale.xを使う。
    /// </summary>
    private void CacheDoorPositions()
    {
        _leftClosedPos = leftDoor.localPosition;
        _rightClosedPos = rightDoor.localPosition;

        // 手動値が設定されていればそちらを優先、なければScaleXで自動計算
        float distance = doorOpenDistance > 0f
            ? doorOpenDistance
            : leftDoor.localScale.x;

        _leftOpenPos = _leftClosedPos + Vector3.left * distance;
        _rightOpenPos = _rightClosedPos + Vector3.right * distance;
    }

    // =====================================================
    // 外部からの登録API（将来のMovementSystemが呼ぶ）
    // =====================================================

    /// <summary>
    /// IElevatorStatusを実装したコンポーネントを登録する。
    /// 登録後はSetElevatorMovingより優先される。
    /// 【将来のElevatorMovementSystem.csのStart()内で呼ぶ】
    /// </summary>
    public void RegisterElevatorStatus(IElevatorStatus status)
    {
        _elevatorStatus = status;
    }

    /// <summary>
    /// IElevatorStatusが未登録の場合の暫定フラグ。
    /// テストや簡易実装時に使う。
    /// 【将来のElevatorMovementSystem.csから呼ぶ】
    /// </summary>
    public void SetElevatorMoving(bool isMoving)
    {
        _isMovingFallback = isMoving;

        // 動き始めたらドアを強制的に閉じる
        if (isMoving) ForceClose();
    }

    // =====================================================
    // ボタンから呼ばれるAPI
    // =====================================================

    /// <summary>開ボタンのOnButtonPressedから呼ぶ</summary>
    public void RequestOpen()
    {
        if (IsElevatorMoving())
        {
            Debug.Log("[DoorController] 移動中のためドアは開きません");
            return;
        }

        if (_state == DoorState.Open || _state == DoorState.Opening) return;

        RestartCoroutine(OpenRoutine());
    }

    /// <summary>閉ボタンのOnButtonPressedから呼ぶ</summary>
    public void RequestClose()
    {
        if (_state == DoorState.Closed || _state == DoorState.Closing) return;

        RestartCoroutine(CloseRoutine());
    }

    /// <summary>移動開始時の強制クローズ（SetElevatorMovingから呼ばれる）</summary>
    public void ForceClose()
    {
        if (_state == DoorState.Closed) return;
        RestartCoroutine(CloseRoutine());
    }

    // =====================================================
    // コルーチン本体
    // =====================================================

    private IEnumerator OpenRoutine()
    {
        _state = DoorState.Opening;

        // 猶予時間が設定されている場合は待機
        // この間もstateはOpeningなので二重呼び出しはブロックされる
        // RequestClose()が呼ばれた場合はRestartCoroutineにより
        // このコルーチンごと中断されCloseRoutineに切り替わる
        if (openDelay > 0f)
            yield return new WaitForSeconds(openDelay);

        yield return MoveDoors(
            leftDoor, _leftClosedPos, _leftOpenPos,
            rightDoor, _rightClosedPos, _rightOpenPos,
            openDuration
        );

        _state = DoorState.Open;
        OnDoorFullyOpened?.Invoke();

        // 自動クローズ
        if (autoCloseDelay > 0f)
        {
            yield return new WaitForSeconds(autoCloseDelay);

            if (_state == DoorState.Open)
                RestartCoroutine(CloseRoutine());
        }
    }

    private IEnumerator CloseRoutine()
    {
        _state = DoorState.Closing;

        yield return MoveDoors(
            leftDoor, _leftOpenPos, _leftClosedPos,
            rightDoor, _rightOpenPos, _rightClosedPos,
            closeDuration
        );

        _state = DoorState.Closed;
        OnDoorFullyClosed?.Invoke();
    }

    /// <summary>
    /// 左右ドアを同時にSmoothStepで補間移動する。
    /// SmoothStepにより自然な加減速が得られる。
    /// </summary>
    private IEnumerator MoveDoors(
        Transform tLeft, Vector3 leftFrom, Vector3 leftTo,
        Transform tRight, Vector3 rightFrom, Vector3 rightTo,
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            tLeft.localPosition = Vector3.Lerp(leftFrom, leftTo, t);
            tRight.localPosition = Vector3.Lerp(rightFrom, rightTo, t);

            yield return null;
        }

        // 終端を確定（浮動小数点誤差リセット）
        tLeft.localPosition = leftTo;
        tRight.localPosition = rightTo;
    }

    // =====================================================
    // ユーティリティ
    // =====================================================

    private bool IsElevatorMoving()
    {
        return _elevatorStatus != null
            ? _elevatorStatus.IsMoving
            : _isMovingFallback;
    }

    private void RestartCoroutine(IEnumerator routine)
    {
        if (_doorCoroutine != null)
            StopCoroutine(_doorCoroutine);
        _doorCoroutine = StartCoroutine(routine);
    }

    public DoorState CurrentState => _state;
}