// TrailerTickerText.cs
// トレーラー専用・提出後は削除すること

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TrailerTickerText : MonoBehaviour
{
    [Header("Content")]
    [TextArea]
    [SerializeField]
    private string tickerMessage =
        "WARNING  ///  ELEVATOR MALFUNCTION  ///  DO NOT EXIT  ///  " +
        "ABNORMAL DETECTED  ///  DO NOT OPEN  ///  WARNING  ///  ";

    [Header("Scroll")]
    [Tooltip("スクロール速度（Canvas単位/秒）")]
    [SerializeField] private float scrollSpeed = 0.5f;

    [Header("Glitch")]
    [SerializeField] private float glitchProbability = 0.04f;
    [SerializeField] private int glitchCharCount = 3;
    [SerializeField] private float glitchDuration = 0.08f;

    [Header("Color")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color glitchColor = new Color(1.0f, 0.15f, 0.15f);

    [Header("Fade")]
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float fadeOutDuration = 0.6f;

    private TextMeshProUGUI _tmp;
    private RectTransform _rect;
    private RectTransform _maskRect;

    private float _singleMessageWidth;
    private float _maskWidth;
    private float _currentX;
    private bool _isScrolling;
    private string _baseMessage;
    private Coroutine _glitchCoroutine;
    private Coroutine _mainCoroutine;

    private static readonly string[] GlitchChars =
    {
        "█", "▓", "▒", "░", "X", "?", "#", "@",
        "Ω", "Z", "!", "E", "R", "0", "/"
    };

    // =====================================================
    private void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
        _rect = GetComponent<RectTransform>();
        _maskRect = transform.parent.GetComponent<RectTransform>();

        _tmp.text = tickerMessage;

        // ① 最初から glitchColor で初期化・alpha=0 で非表示
        ApplyColor(glitchColor, 0f);
    }

    private void Update()
    {
        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            if (_isScrolling) StopTicker();
            else StartTicker();
        }

        if (Keyboard.current.uKey.wasPressedThisFrame)
            StopTicker();
    }

    // =====================================================
    // 外部API
    // =====================================================

    public void StartTicker()
    {
        if (_isScrolling) return;
        if (_mainCoroutine != null) StopCoroutine(_mainCoroutine);
        _mainCoroutine = StartCoroutine(TickerRoutine());
    }

    public void StopTicker()
    {
        if (_mainCoroutine != null) StopCoroutine(_mainCoroutine);
        if (_glitchCoroutine != null) StopCoroutine(_glitchCoroutine);
        _mainCoroutine = null;
        _glitchCoroutine = null;
        _isScrolling = false;
        StartCoroutine(FadeOut());
    }

    // =====================================================
    // メインルーチン
    // =====================================================

    private IEnumerator TickerRoutine()
    {
        _isScrolling = true;

        // ── 1周分の幅を計測 ─────────────────────────────
        _tmp.text = tickerMessage;

        // ② テキスト変更直後に色を再セット（メッシュ再構築による黒フラッシュを防ぐ）
        ApplyColor(glitchColor, 0f);

        yield return null; // レイアウト確定を1フレーム待つ

        _tmp.ForceMeshUpdate();

        // ③ ForceMeshUpdate 直後にも再セット（再構築後の頂点色リセットを上書き）
        ApplyColor(glitchColor, 0f);

        _singleMessageWidth = _tmp.preferredWidth;
        _maskWidth = (_maskRect != null) ? _maskRect.rect.width : 1.0f;

        // ── 繰り返し回数を計算してテキストを連結 ─────────
        int reps = Mathf.CeilToInt(_maskWidth / _singleMessageWidth) + 2;
        _baseMessage = "";
        for (int i = 0; i < reps; i++)
            _baseMessage += tickerMessage;

        _tmp.text = _baseMessage;

        // ④ 連結後のテキスト変更でも再セット
        ApplyColor(glitchColor, 0f);

        _tmp.ForceMeshUpdate();

        // ⑤ ForceMeshUpdate 後にも再セット
        ApplyColor(glitchColor, 0f);

        float totalWidth = _tmp.preferredWidth;

        Vector2 sd = _rect.sizeDelta;
        sd.x = totalWidth;
        _rect.sizeDelta = sd;

        _currentX = _maskWidth;
        SetPositionX(_currentX);

        // フェードイン（glitchColor のまま alpha 0→1）
        yield return FadeIn();

        // ── スクロールループ ─────────────────────────────
        while (_isScrolling)
        {
            _currentX -= scrollSpeed * Time.deltaTime;

            if (_currentX < -_singleMessageWidth)
                _currentX += _singleMessageWidth;

            SetPositionX(_currentX);

            if (_glitchCoroutine == null &&
                Random.value < glitchProbability)
            {
                _glitchCoroutine = StartCoroutine(GlitchRoutine());
            }

            yield return null;
        }
    }

    // ─── グリッチ ─────────────────────────────────────────
    private IEnumerator GlitchRoutine()
    {
        char[] chars = _baseMessage.ToCharArray();

        for (int i = 0; i < glitchCharCount; i++)
        {
            int idx;
            int safety = 0;
            do { idx = Random.Range(0, chars.Length); safety++; }
            while (chars[idx] == ' ' && safety < 30);
            chars[idx] = GlitchChars[Random.Range(0, GlitchChars.Length)][0];
        }

        _tmp.text = new string(chars);

        // グリッチ中：明るい赤
        ApplyColor(glitchColor, _tmp.color.a);

        yield return new WaitForSeconds(glitchDuration);

        // ⑥ 復帰時も glitchColor を維持（normalColor には戻さない）
        _tmp.text = _baseMessage;
        ApplyColor(glitchColor, _tmp.color.a);

        _glitchCoroutine = null;
    }

    // ─── フェード ─────────────────────────────────────────
    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            // RGBはそのまま（glitchColor）、alphaだけ上げる
            SetAlpha(Mathf.Clamp01(elapsed / fadeInDuration));
            yield return null;
        }
        SetAlpha(1f);
    }

    private IEnumerator FadeOut()
    {
        float startA = _tmp.color.a;
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(startA, 0f, elapsed / fadeOutDuration));
            yield return null;
        }
        SetAlpha(0f);
    }

    // ─── ユーティリティ ───────────────────────────────────
    private void SetPositionX(float x)
    {
        Vector2 pos = _rect.anchoredPosition;
        pos.x = x;
        _rect.anchoredPosition = pos;
    }

    private void SetAlpha(float a)
    {
        Color c = _tmp.color;
        c.a = a;
        _tmp.color = c;
    }

    /// <summary>
    /// RGB と alpha を同時に設定するユーティリティ。
    /// テキスト変更・ForceMeshUpdate 後に必ず呼ぶことで
    /// TMP のメッシュ再構築による頂点色リセットを上書きする。
    /// </summary>
    private void ApplyColor(Color rgb, float alpha)
    {
        _tmp.color = new Color(rgb.r, rgb.g, rgb.b, alpha);
    }
}