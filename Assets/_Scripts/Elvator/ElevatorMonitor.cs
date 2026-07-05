using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// エレベーター内のモニターを制御する。
/// 階数表示・移動方向矢印・移動アニメーション・グリッチ演出を担当。
/// </summary>
public class ElevatorMonitor : MonoBehaviour
{
    // ─── 参照 ──────────────────────────────────────────────
    [Header("References")]
    [SerializeField] private TextMeshProUGUI displayText;

    [Tooltip("矢印を表示する専用TMP。FloorTextの上に配置する")]
    [SerializeField] private TextMeshProUGUI arrowText;

    // ─── 通常表示設定 ──────────────────────────────────────
    [Header("Normal Display")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.8f, 0.2f);

    // ─── 移動アニメーション設定 ───────────────────────────
    [Header("Floor Movement")]
    [Tooltip("階数ボタンを押してから動き出すまでの待機時間（秒）")]
    [SerializeField] private float startDelay = 1.2f;

    [Tooltip("1階分の移動にかかる時間（秒）")]
    [SerializeField] private float timePerFloor = 0.8f;

    [Tooltip("移動中の階数・矢印表示カラー")]
    [SerializeField] private Color movingColor = new Color(0.8f, 0.8f, 0.2f);

    // ─── Phase1：予兆ちらつき ─────────────────────────────
    [Header("Glitch Phase1 - Flicker")]
    [Tooltip("文字が変わっている時間（秒）")]
    [SerializeField] private float flickerHoldTime = 0.07f;

    [Tooltip("1回目と2回目のちらつきの間の一拍（秒）")]
    [SerializeField] private float flickerPauseTime = 0.6f;

    [Tooltip("ちらつく回数")]
    [SerializeField] private int flickerCount = 2;

    [SerializeField] private Color phase1GlitchColor = new Color(0.9f, 0.9f, 0.4f);

    // ─── Phase2：安定 ──────────────────────────────────────
    [Header("Glitch Phase2 - Stable")]
    [Tooltip("グリッチ前の安定期間（秒）")]
    [SerializeField] private float phase2Duration = 1.5f;

    // ─── Phase3：高速グリッチ ─────────────────────────────
    [Header("Glitch Phase3 - Rapid")]
    [Tooltip("文字が切り替わる間隔（秒）。小さいほど激しい")]
    [SerializeField] private float phase3CharInterval = 0.05f;

    [SerializeField] private Color phase3GlitchColor = new Color(1.0f, 0.2f, 0.2f);

    // ─── 内部変数 ─────────────────────────────────────────
    private int _displayedFloor = 0;
    private int _targetFloor = 1;
    private bool _isMoving = false;
    private bool _isGlitching = false;

    // 現在の移動方向を保持（矢印の復帰に使う）
    private int _currentDirection = 0; // 1:上 / -1:下 / 0:停止

    private Coroutine _glitchCoroutine;
    private Coroutine _movementCoroutine;

    private const string ArrowUp = "↑";
    private const string ArrowDown = "↓";
    private const string ArrowNone = "";

    private static readonly string[] GlitchChars =
    {
        "?", "!", "#", "%", "@", "X", "E", "Ω", "Σ", "(", "8", "B", "∞", "//", "T"
    };

    // =====================================================
    private void Start()
    {
        UpdateDisplay(_displayedFloor.ToString(), normalColor);
        UpdateArrow(ArrowNone, normalColor); // 初期状態：矢印非表示
    }

    private void Update()
    {
        if (Keyboard.current.pKey.wasPressedThisFrame)
            TriggerGlitch();

        if (Keyboard.current.uKey.wasPressedThisFrame)
            ResetDisplay();
    }

    // =====================================================
    // 外部API
    // =====================================================

    public void SetFloor(int floorNumber)
    {
        _targetFloor = floorNumber;

        if (_displayedFloor == _targetFloor && !_isMoving) return;

        RestartMovement();
    }

    public void SetFloorDirect(string floorText)
    {
        if (_movementCoroutine != null)
        {
            StopCoroutine(_movementCoroutine);
            _movementCoroutine = null;
            _isMoving = false;
        }

        _currentDirection = 0;

        if (!_isGlitching)
        {
            UpdateDisplay(floorText, normalColor);
            UpdateArrow(ArrowNone, normalColor);
        }
    }

    public void TriggerGlitch()
    {
        if (_glitchCoroutine != null)
            StopCoroutine(_glitchCoroutine);

        _glitchCoroutine = StartCoroutine(GlitchRoutine());
    }

    public void ResetDisplay()
    {
        if (_glitchCoroutine != null)
        {
            StopCoroutine(_glitchCoroutine);
            _glitchCoroutine = null;
        }

        _isGlitching = false;

        // 移動中・停止中それぞれの正しい状態に復帰
        if (_isMoving)
        {
            UpdateDisplay(_displayedFloor.ToString(), movingColor);
            UpdateArrow(DirectionToArrow(_currentDirection), movingColor);
        }
        else
        {
            UpdateDisplay(_displayedFloor.ToString(), normalColor);
            UpdateArrow(ArrowNone, normalColor);
        }
    }

    // =====================================================
    // 移動アニメーションコルーチン
    // =====================================================

    private void RestartMovement()
    {
        if (_movementCoroutine != null)
            StopCoroutine(_movementCoroutine);

        _movementCoroutine = StartCoroutine(MovementRoutine());
    }

    private IEnumerator MovementRoutine()
    {
        // ── 一拍待機 ─────────────────────────────────────
        _isMoving = false;
        _currentDirection = 0;
        yield return new WaitForSeconds(startDelay);

        // ── 移動開始 ─────────────────────────────────────
        _isMoving = true;

        while (_displayedFloor != _targetFloor)
        {
            // 方向を決定して保持
            _currentDirection = (_targetFloor > _displayedFloor) ? 1 : -1;
            _displayedFloor += _currentDirection;

            if (!_isGlitching)
            {
                UpdateDisplay(_displayedFloor.ToString(), movingColor);
                // 移動方向に応じた矢印を表示
                UpdateArrow(DirectionToArrow(_currentDirection), movingColor);
            }

            yield return new WaitForSeconds(timePerFloor);
        }

        // ── 目的階に到着 ─────────────────────────────────
        _isMoving = false;
        _currentDirection = 0;

        if (!_isGlitching)
        {
            UpdateDisplay(_displayedFloor.ToString(), normalColor);
            UpdateArrow(ArrowNone, normalColor); // 到着したら矢印を消す
        }

        _movementCoroutine = null;
    }

    // =====================================================
    // グリッチコルーチン
    // =====================================================

    private IEnumerator GlitchRoutine()
    {
        _isGlitching = true;

        // ① 削除：矢印を消す処理をなくす
        // UpdateArrow(ArrowNone, phase1GlitchColor); ← 削除

        // ── Phase1：予兆ちらつき ─────────────────────────
        for (int i = 0; i < flickerCount; i++)
        {
            UpdateDisplay(GetRandomGlitchChar(), phase1GlitchColor);
            yield return new WaitForSeconds(flickerHoldTime);

            // ② 復帰時に displayText と arrowText を両方更新する
            Color restoreColor = _isMoving ? movingColor : normalColor;
            UpdateDisplay(_displayedFloor.ToString(), restoreColor);
            UpdateArrow(DirectionToArrow(_currentDirection), restoreColor);

            if (i < flickerCount - 1)
                yield return new WaitForSeconds(flickerPauseTime);
        }

        // ── Phase2：安定 ─────────────────────────────────
        Color stableColor = _isMoving ? movingColor : normalColor;
        UpdateDisplay(_displayedFloor.ToString(), stableColor);
        UpdateArrow(DirectionToArrow(_currentDirection), stableColor);
        yield return new WaitForSeconds(phase2Duration);

        // ── Phase3：高速グリッチ（無限） ─────────────────
        while (true)
        {
            UpdateDisplay(GetRandomGlitchChar(), phase3GlitchColor);
            yield return new WaitForSeconds(phase3CharInterval);
        }
    }

    // =====================================================
    // ユーティリティ
    // =====================================================

    private void UpdateDisplay(string text, Color color)
    {
        if (displayText == null) return;
        displayText.text = text;
        displayText.color = color;
    }

    private void UpdateArrow(string arrow, Color color)
    {
        if (arrowText == null) return;
        arrowText.text = arrow;
        arrowText.color = color;
    }

    /// <summary>方向値を矢印文字に変換する</summary>
    private string DirectionToArrow(int direction)
    {
        if (direction > 0) return ArrowUp;
        if (direction < 0) return ArrowDown;
        return ArrowNone;
    }

    private string GetRandomGlitchChar()
    {
        return GlitchChars[Random.Range(0, GlitchChars.Length)];
    }

    public bool IsMoving => _isMoving;
    public int DisplayedFloor => _displayedFloor;
    public int TargetFloor => _targetFloor;
}