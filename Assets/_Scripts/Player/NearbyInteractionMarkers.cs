using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Scene opt-in. Occlusion and distance use the same physical targets as interaction.
public sealed class NearbyInteractionMarkers : MonoBehaviour
{
    [SerializeField] private Camera view;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField, Range(8, 24)] private float size = 18;
    [SerializeField, Min(0)] private float fadeSeconds = .18f;
    [SerializeField, Min(0)] private float floatPixels = 2.5f;
    [SerializeField, Min(.1f)] private float floatPeriod = 2.4f;
    [SerializeField, Range(1, 1.6f)] private float focusScale = 1.25f;
    private InteractionRaycaster raycaster;
    private RectTransform canvasRect;
    private sealed class Marker
    {
        public Interactable target;
        public Collider collider;
        public TMP_Text mark;
        public Transform keypad;
        public bool compact;
        public float alpha, focus;
    }
    private readonly List<Marker> marks = new();
    private void Start()
    {
        raycaster = GetComponent<InteractionRaycaster>();
        var canvasObject = new GameObject("操作対象の▲", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 15;
        canvasRect = (RectTransform)canvas.transform;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1;
        var keypads = new HashSet<Transform>();
        foreach (var root in gameObject.scene.GetRootGameObjects())
        foreach (var target in root.GetComponentsInChildren<Interactable>(true))
        {
            if (target.ActionType == PlayerAction.PressFloor && target.FloorNumber != 8) continue;
            var collider = target.GetComponent<Collider>();
            if (collider == null) continue;
            Transform keypad = null;
            if (target.TryGetComponent<EntranceControl>(out var control) && control.IsKey)
            {
                keypad = target.transform.parent;
                if (!keypads.Add(keypad)) continue;
            }
            var mark = new GameObject(target.name + "_▲", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            mark.transform.SetParent(canvas.transform, false); mark.font = font; mark.text = "▲";
            mark.fontSize = size; mark.alignment = TextAlignmentOptions.Center; mark.raycastTarget = false;
            mark.outlineColor = new Color32(12, 15, 16, 230); mark.outlineWidth = .18f;
            mark.rectTransform.sizeDelta = new Vector2(48, 48); mark.gameObject.SetActive(false);
            marks.Add(new Marker { target = target, collider = collider, mark = mark, keypad = keypad,
                compact = target.TryGetComponent<EntranceControl>(out var entry) && !entry.IsKey });
        }
    }
    private void LateUpdate()
    {
        foreach (var item in marks)
        {
            bool visible = !DemoSession.BlocksGameplay && item.target != null && item.target.isActiveAndEnabled &&
                item.collider != null && item.collider.enabled && view != null;
            bool available = visible && (!item.target.TryGetComponent<EntranceControl>(out var entrance) || entrance.Available);
            Vector3 point = visible ? item.collider.bounds.center : Vector3.zero;
            bool inRange = false;
            if (visible)
            {
                Vector3 direction = point - view.transform.position;
                inRange = direction.magnitude <= raycaster.InteractionDistance;
                visible = Physics.Raycast(view.transform.position, direction.normalized, out var hit,
                        direction.magnitude + .1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                    hit.collider.GetComponentInParent<Interactable>() == item.target;
            }
            bool focused = raycaster.CurrentTarget != null && (raycaster.CurrentTarget == item.target ||
                (item.keypad != null && raycaster.CurrentTarget.transform.parent == item.keypad));
            // Pin to the physical object, never smooth camera tracking (which creates screen-space lag).
            if (visible) point = item.keypad != null ? item.keypad.TransformPoint(new Vector3(0, .49f, -.05f)) :
                point + Vector3.up * (item.compact ? 0 : item.collider.bounds.extents.y);
            Vector3 screen = visible ? view.WorldToScreenPoint(point) : Vector3.zero;
            visible &= screen.z > 0 && screen.x >= 0 && screen.x <= Screen.width && screen.y >= 0 && screen.y <= Screen.height;
            // Occlusion, menus and disabled targets hide immediately; proximity/availability fade out.
            if (!visible) { item.alpha = 0; item.focus = 0; item.mark.gameObject.SetActive(false); continue; }
            float step = fadeSeconds <= 0 ? 1 : Time.deltaTime / fadeSeconds;
            item.alpha = Mathf.MoveTowards(item.alpha, available && inRange ? 1 : 0, step);
            item.focus = Mathf.MoveTowards(item.focus, focused && available && inRange ? 1 : 0, step);
            item.mark.gameObject.SetActive(item.alpha > 0);
            if (item.alpha <= 0) continue;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
            float bob = Mathf.Sin(Time.time * Mathf.PI * 2 / Mathf.Max(.1f, floatPeriod)) * floatPixels;
            item.mark.rectTransform.anchoredPosition = local + new Vector2(0, (item.compact ? size * .55f : size + 3) + bob * (1 - item.focus * .7f));
            item.mark.fontSize = size;
            item.mark.transform.localScale = Vector3.one * Mathf.Lerp(1, focusScale, Mathf.SmoothStep(0, 1, item.focus));
            var color = Color.Lerp(new Color(.93f, .94f, .90f, .68f), new Color(1, .94f, .78f, 1), item.focus);
            color.a *= Mathf.SmoothStep(0, 1, item.alpha); item.mark.color = color;
        }
    }
    private void OnDisable()
    {
        foreach (var item in marks) { item.alpha = 0; item.focus = 0; if (item.mark != null) item.mark.gameObject.SetActive(false); }
    }
}
