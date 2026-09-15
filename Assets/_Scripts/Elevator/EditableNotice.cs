using TMPro;
using UnityEngine;

// The authored copy belongs to the scene, not to the interior generator.
[ExecuteAlways, DisallowMultipleComponent]
[AddComponentMenu("Graduation Project/掲示の文章編集")]
public sealed class EditableNotice : MonoBehaviour
{
    [SerializeField, HideInInspector] private string contentId;
    [Header("ここで文章を編集します（Play中の変更は保存されません）")]
    [SerializeField, TextArea(1, 3)] private string heading;
    [SerializeField, TextArea(10, 22)] private string body;
    [Header("文字の大きさ（枠内に収まる範囲で調整）")]
    [SerializeField, Range(.1f, .5f)] private float headingSize = .26f;
    [SerializeField, Range(.07f, .3f)] private float bodySize = .16f;
    [SerializeField, HideInInspector] private TMP_Text headingView;
    [SerializeField, HideInInspector] private TMP_Text bodyView;
    private bool pending;

    public string ContentId => contentId;
    public string Heading => heading;
    public string Body => body;

    public void Bind(string id, TMP_Text title, TMP_Text text, string initialHeading, string initialBody)
    {
        if (string.IsNullOrEmpty(contentId))
        {
            contentId = id;
            heading = initialHeading;
            body = initialBody;
        }
        headingView = title;
        bodyView = text;
        Apply();
    }

    private void OnEnable() => pending = true;
    private void OnValidate() => pending = true;
    private void Update() { if (pending) Apply(); }

    public void Apply()
    {
        pending = false;
        Write(headingView, heading, headingSize);
        Write(bodyView, body, bodySize);
    }

    private static void Write(TMP_Text view, string copy, float size)
    {
        if (view == null) return;
        // Auto-sizing writes the fitted fontSize. Do not reset it on every scene reload:
        // that would dirty an otherwise unchanged scene merely by opening it.
        if (view.text == (copy ?? "") && view.enableAutoSizing && view.richText &&
            Mathf.Approximately(view.fontSizeMax, size) && Mathf.Approximately(view.fontSizeMin, size * .8f) &&
            view.textWrappingMode == TextWrappingModes.Normal) return;
        view.text = copy ?? "";
        view.richText = true;
        view.enableAutoSizing = true;
        view.fontSizeMax = size;
        view.fontSizeMin = size * .8f;
        view.fontSize = size;
        view.textWrappingMode = TextWrappingModes.Normal;
    }
}
