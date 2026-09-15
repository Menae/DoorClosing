using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A two-night demo wrapper. Standalone M1/M2 remain independently playable.
[DefaultExecutionOrder(-500)]
public sealed class DemoSession : MonoBehaviour
{
    [SerializeField] private NormalJourneyController journey;
    [SerializeField] private RunManager run;
    [SerializeField] private PlayerLook player;
    [SerializeField] private Camera view;
    [SerializeField] private TMP_FontAsset font;
    private static DemoSession instance;
    private int blockedFrame;
    private bool menuOpen = true, introduction = true, transition, completed;
    private string page = "Title";
    private RectTransform panel;
    private ScrollRect copyScroll;
    private GameObject overlay;
    private Image dimmer;
    private float oldTimeScale, oldVolume, sensitivity = .12f, brightness;
    private bool oldAudioPause, inverted;
    private VolumeProfile volumeProfile;
    private ColorAdjustments colorAdjustments;
    private float fov;

    public static bool BlocksGameplay => instance != null &&
        (instance.menuOpen || instance.transition || Time.frameCount <= instance.blockedFrame);
    public bool IsIntroduction => introduction;
    public bool IsPaused => menuOpen;
    public bool IsComplete => completed;

    private void Awake()
    {
        instance = this;
        oldTimeScale = Time.timeScale; oldAudioPause = AudioListener.pause; oldVolume = AudioListener.volume;
        fov = view.fieldOfView;
        journey.SetDemoNight(false);
        journey.IntroductionCompleted += AfterIntroduction;
        run.HomeRunCompleted += AfterHome;
        volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        var volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true; volume.priority = 50; volume.sharedProfile = volumeProfile;
        colorAdjustments = volumeProfile.Add<ColorAdjustments>();
        colorAdjustments.postExposure.overrideState = true;
        view.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        CreateCanvas();
        SetMenu(true);
        ShowTitle();
    }

    private void Update()
    {
        if (transition || completed || Keyboard.current == null) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (!menuOpen) { SetMenu(true); ShowPause(); }
            else if (page == "Pause") Resume();
            else if (page == "Settings") ShowPrevious();
        }
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && !menuOpen && !transition && !completed) { SetMenu(true); ShowPause(); }
    }

    private void OnDestroy()
    {
        if (journey != null) journey.IntroductionCompleted -= AfterIntroduction;
        if (run != null) run.HomeRunCompleted -= AfterHome;
        if (instance == this)
        {
            instance = null; Time.timeScale = oldTimeScale;
            AudioListener.pause = oldAudioPause; AudioListener.volume = oldVolume;
        }
        if (volumeProfile != null)
        {
            foreach (var component in volumeProfile.components) Destroy(component);
            Destroy(volumeProfile);
        }
    }

    private void SetMenu(bool open)
    {
        menuOpen = open; blockedFrame = Time.frameCount + 1;
        Time.timeScale = open ? 0 : oldTimeScale;
        AudioListener.pause = open || oldAudioPause;
        overlay.SetActive(open);
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    private void Resume() { page = "Playing"; SetMenu(false); }
    private void AfterIntroduction() { if (introduction && !transition) StartCoroutine(NextNight()); }
    private IEnumerator NextNight()
    {
        transition = true;
        yield return new WaitForSecondsRealtime(1.2f);
        introduction = false;
        journey.SetDemoNight(true);
        journey.RestartNightAtEntrance();
        transition = false;
        SetMenu(true); Clear("Night");
        Heading(W("menu.NextNight.1", "翌夜"));
        Copy(W("menu.NextNight.2", "いつもの８階へ、帰りましょう。\n気になるときは、入口の掲示を読み返せます。"));
        Button(W("menu.NextNight.3", "夜を始める"), Resume);
    }

    private void AfterHome()
    {
        completed = true; SetMenu(true); Clear("Complete");
        Heading(W("menu.AfterHome.1", "帰宅しました"));
        Copy(W("menu.AfterHome.2", "デモはここまでです。\nお疲れさまでした。"));
        Button(W("menu.AfterHome.3", "最初から遊ぶ"), Restart);
        Button(W("menu.AfterHome.4", "終了"), Quit);
    }

    private void Restart()
    {
        Clear("Loading"); Heading(W("menu.Restart.1", "読み込み中"));
        Time.timeScale = oldTimeScale; AudioListener.pause = oldAudioPause;
        SceneManager.LoadSceneAsync(gameObject.scene.path);
    }

    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowTitle()
    {
        Clear("Title"); Heading(W("menu.ShowTitle.1", "帰宅 / プレイアブルデモ"));
        Copy(W("menu.ShowTitle.2", "あなたの自宅は８階です。\nまずは普段どおりに帰り、廊下の様子を覚えてください。\n\n乗る前に、エレベーター横の注意書きをご確認ください。"));
        Copy(W("menu.ShowTitle.3", "WASD：移動　Shift：走る　マウス：視点\n左クリック：操作　Esc：一時停止"), 24);
        Button(W("menu.ShowTitle.4", "はじめる"), Resume);
        Button(W("menu.ShowTitle.5", "設定"), ShowSettings);
        Button(W("menu.ShowTitle.6", "終了"), Quit);
    }

    private void ShowPause()
    {
        Clear("Pause"); Heading(W("menu.ShowPause.1", "一時停止"));
        Copy(W("menu.ShowPause.2", "WASD：移動　Shift：走る　左クリック：操作\n掲示は入口のエレベーター横にあります。"), 24);
        Button(W("menu.ShowPause.3", "再開"), Resume); Button(W("menu.ShowPause.4", "設定"), ShowSettings);
        Button(W("menu.ShowPause.5", "最初からやり直す"), () =>
        {
            Clear("Confirm"); Heading(W("menu.ShowPause.6", "最初からやり直しますか？"));
            Copy(W("menu.ShowPause.7", "今回の進行は失われ、通常の帰宅から始まります。"));
            Button(W("menu.ShowPause.8", "やり直す"), Restart); Button(W("menu.ShowPause.9", "戻る"), ShowPause);
        });
        Button(W("menu.ShowPause.10", "終了"), () =>
        {
            Clear("Confirm"); Heading(W("menu.ShowPause.11", "ゲームを終了しますか？"));
            Copy(W("menu.ShowPause.12", "このデモは進行を保存しません。"));
            Button(W("menu.ShowPause.13", "終了する"), Quit); Button(W("menu.ShowPause.14", "戻る"), ShowPause);
        });
    }

    private bool settingsFromTitle;
    private void ShowSettings()
    {
        settingsFromTitle = page == "Title";
        Clear("Settings"); Heading(W("menu.ShowSettings.1", "設定"));
        Slider(W("menu.ShowSettings.2", "視点感度"), .04f, .24f, sensitivity, v => { sensitivity = v; player.SetLookSettings(v, inverted); });
        Button(W("menu.ShowSettings.3", "上下反転：") + (inverted ? W("settings.on", "オン") : W("settings.off", "オフ")), () =>
        { inverted = !inverted; player.SetLookSettings(sensitivity, inverted); RefreshSettings(); });
        Slider(W("menu.ShowSettings.4", "視野角"), 50, 85, fov, v => { fov = v; view.fieldOfView = v; });
        Slider(W("menu.ShowSettings.5", "明るさ"), -1, 1, brightness, v => { brightness = v; colorAdjustments.postExposure.value = v; });
        Slider(W("menu.ShowSettings.6", "音量"), 0, 1, AudioListener.volume, v => AudioListener.volume = v);
        Button(W("menu.ShowSettings.7", "全画面／ウィンドウを切り替える"), () =>
        { Screen.fullScreen = !Screen.fullScreen; });
        Button(W("menu.ShowSettings.8", "表示サイズ：1280 × 720"), () => Screen.SetResolution(1280, 720, Screen.fullScreenMode));
        Button(W("menu.ShowSettings.9", "表示サイズ：1920 × 1080"), () => Screen.SetResolution(1920, 1080, Screen.fullScreenMode));
        Copy(W("menu.ShowSettings.10", "設定は今回のプレイ中に適用されます。"), 21);
        Button(W("menu.ShowSettings.11", "戻る"), ShowPrevious);
    }

    private void RefreshSettings() { bool wasTitle = settingsFromTitle; ShowSettings(); settingsFromTitle = wasTitle; }
    private void ShowPrevious() { if (settingsFromTitle) ShowTitle(); else ShowPause(); }

    private void CreateCanvas()
    {
        overlay = new GameObject("デモメニュー", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        overlay.transform.SetParent(transform, false);
        var canvas = overlay.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 500;
        var scaler = overlay.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        dimmer = overlay.AddComponent<Image>(); dimmer.color = new Color(.018f, .025f, .022f, .96f);
        var viewport = new GameObject("文章スクロール", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect)).GetComponent<RectTransform>();
        viewport.SetParent(overlay.transform, false); viewport.anchorMin = viewport.anchorMax = new Vector2(.5f, .5f);
        viewport.sizeDelta = new Vector2(820, 960);
        viewport.GetComponent<Image>().color = Color.clear;
        panel = new GameObject("内容", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement)).GetComponent<RectTransform>();
        panel.SetParent(viewport, false); panel.anchorMin = new Vector2(0, 1); panel.anchorMax = Vector2.one;
        panel.pivot = new Vector2(.5f, 1); panel.sizeDelta = Vector2.zero;
        panel.GetComponent<LayoutElement>().minHeight = 960;
        panel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        copyScroll = viewport.GetComponent<ScrollRect>(); copyScroll.viewport = viewport; copyScroll.content = panel;
        copyScroll.horizontal = false; copyScroll.movementType = ScrollRect.MovementType.Clamped; copyScroll.scrollSensitivity = 40;
        var layout = panel.GetComponent<VerticalLayoutGroup>(); layout.spacing = 16;
        layout.childAlignment = TextAnchor.MiddleCenter; layout.childControlHeight = true; layout.childControlWidth = true;
        layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
        var eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var go = new GameObject("Menu Input", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetParent(transform, false); go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }

    private void Clear(string nextPage)
    {
        page = nextPage;
        foreach (Transform child in panel) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        copyScroll.StopMovement(); panel.anchoredPosition = Vector2.zero;
    }

    private string W(string key, string fallback) => GameTextCollection.Get(this, key, fallback);

    private void Heading(string value) => Copy(value, 38);
    private void Copy(string value, float size = 27)
    {
        var text = Text(panel, value, size); text.alignment = TextAlignmentOptions.Center;
        var layout = text.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = Mathf.Max(size * 1.5f, text.GetPreferredValues(value, 820, Mathf.Infinity).y + size * .75f);
    }

    private TMP_Text Text(Transform parent, string value, float size)
    {
        var text = new GameObject(value.Split('\n')[0], typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false); text.font = font; text.text = value; text.fontSize = size;
        text.color = new Color(.91f, .91f, .83f); text.raycastTarget = false;
        return text;
    }

    private void Button(string label, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(panel, false);
        go.GetComponent<Image>().color = new Color(.14f, .18f, .16f);
        var button = go.GetComponent<Button>(); button.onClick.AddListener(action);
        var text = Text(go.transform, label, 27); text.alignment = TextAlignmentOptions.Center;
        go.GetComponent<LayoutElement>().preferredHeight = Mathf.Max(56, text.GetPreferredValues(label, 800, Mathf.Infinity).y + 16);
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.sizeDelta = Vector2.zero;
    }

    private void Slider(string label, float min, float max, float value, UnityEngine.Events.UnityAction<float> changed)
    {
        var row = new GameObject(label, typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
        row.SetParent(panel, false); row.GetComponent<LayoutElement>().preferredHeight = 44;
        var caption = Text(row, label, 25); caption.alignment = TextAlignmentOptions.MidlineLeft;
        caption.rectTransform.anchorMin = Vector2.zero; caption.rectTransform.anchorMax = new Vector2(.45f, 1); caption.rectTransform.sizeDelta = Vector2.zero;
        var bar = new GameObject("Slider", typeof(RectTransform), typeof(Image), typeof(Slider)).GetComponent<RectTransform>();
        bar.SetParent(row, false); bar.anchorMin = new Vector2(.48f,.25f); bar.anchorMax = new Vector2(1,.75f); bar.sizeDelta = Vector2.zero;
        bar.GetComponent<Image>().color = new Color(.19f,.23f,.20f);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        handle.SetParent(bar, false); handle.sizeDelta = new Vector2(22,34); handle.GetComponent<Image>().color = new Color(.8f,.83f,.69f);
        var slider = bar.GetComponent<Slider>(); slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
        slider.minValue = min; slider.maxValue = max; slider.value = value; slider.onValueChanged.AddListener(changed);
    }
}
