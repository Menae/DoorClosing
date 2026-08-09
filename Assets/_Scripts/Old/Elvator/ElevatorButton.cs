using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using TMPro;

public class ElevatorButton : MonoBehaviour
{
    // ─── ラベル設定 ───────────────────────────────────────
    [Header("Label")]
    [SerializeField] private string buttonLabel = "1";
    [SerializeField] private TextMeshProUGUI buttonText;

    // ─── 押し込みアニメーション ───────────────────────────
    [Header("Press Animation")]
    [SerializeField] private float pressDepth = 0.004f; // 沈む距離(m)
    [SerializeField] private float pressSpeed = 0.06f;  // アニメーション時間(秒)

    // ─── 発光設定 ─────────────────────────────────────────
    [Header("Emission")]
    [SerializeField] private Renderer buttonRenderer;

    [SerializeField] private Color normalEmissionColor = new Color(0.05f, 0.25f, 0.05f);
    [SerializeField] private float normalIntensity = 1.0f;

    [SerializeField] private Color pressedEmissionColor = new Color(0.2f, 1.0f, 0.2f);
    [SerializeField] private float pressedIntensity = 4.0f;
    [SerializeField] private float pressedDuration = 0.6f;

    // ─── テキスト色設定 ───────────────────────────────────
    [Header("Text Color")]
    [SerializeField] private Color normalTextColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color pressedTextColor = new Color(0.6f, 1.0f, 0.6f, 1f);

    // ─── 外部通知用イベント ───────────────────────────────
    [Header("Event")]
    public UnityEvent OnButtonPressed;

    // ─── 内部変数 ─────────────────────────────────────────
    private Vector3 _originalLocalPos;
    private MaterialPropertyBlock _mpb;
    private bool _isAnimating;

    // =====================================================
    private void Awake()
    {
        _originalLocalPos = transform.localPosition;
        _mpb = new MaterialPropertyBlock();

        // ラベルを反映
        if (buttonText != null)
        {
            buttonText.text = buttonLabel;
            buttonText.color = normalTextColor;
        }

        // 通常発光をセット
        ApplyEmission(normalEmissionColor, normalIntensity);
    }

    // =====================================================
    // PlayerInteraction から呼ばれるエントリポイント
    // =====================================================
    public void Press()
    {
        if (_isAnimating) return;
        StartCoroutine(PressRoutine());
        OnButtonPressed?.Invoke();
    }

    // =====================================================
    private IEnumerator PressRoutine()
    {
        _isAnimating = true;

        // ── 沈む ────────────────────────────────────────
        // ローカルZ軸方向（ボタン面の奥方向）に沈む
        // ProBuilderで正面がローカル+Z向きの場合
        Vector3 pressedPos = _originalLocalPos + new Vector3(0f, 0f, -pressDepth);

        yield return MoveLocal(_originalLocalPos, pressedPos, pressSpeed);

        // ── 発光 ────────────────────────────────────────
        ApplyEmission(pressedEmissionColor, pressedIntensity);
        if (buttonText != null) buttonText.color = pressedTextColor;

        yield return new WaitForSeconds(pressedDuration);

        // ── 戻る ────────────────────────────────────────
        yield return MoveLocal(pressedPos, _originalLocalPos, pressSpeed);
        transform.localPosition = _originalLocalPos; // 誤差リセット

        // ── 通常発光に戻す ───────────────────────────────
        ApplyEmission(normalEmissionColor, normalIntensity);
        if (buttonText != null) buttonText.color = normalTextColor;

        _isAnimating = false;
    }

    // ─── ローカル座標の補間移動ユーティリティ ─────────────
    private IEnumerator MoveLocal(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration); // なめらかに
            transform.localPosition = Vector3.Lerp(from, to, t);
            yield return null;
        }
    }

    // ─── MaterialPropertyBlock で発光カラーを適用 ─────────
    // ※ PropertyBlock を使うことでマテリアルのインスタンス化を防ぐ
    private void ApplyEmission(Color color, float intensity)
    {
        if (buttonRenderer == null) return;
        buttonRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_EmissionColor", color * intensity);
        buttonRenderer.SetPropertyBlock(_mpb);
    }

    // ─── 上位スクリプトからの強制発光制御（異変用） ────────
    // 後で使う。今は触らなくてOK
    public void ForceSetEmission(Color color, float intensity)
    {
        ApplyEmission(color, intensity);
    }

    public void ForceSetLabel(string label)
    {
        if (buttonText != null) buttonText.text = label;
    }
}