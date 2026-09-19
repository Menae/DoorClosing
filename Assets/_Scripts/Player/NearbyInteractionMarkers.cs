using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Scene opt-in. Occlusion and distance use the same physical targets as interaction.
public sealed class NearbyInteractionMarkers : MonoBehaviour
{
    [SerializeField] private Camera view;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField, Range(8, 24)] private float size = 12;
    private InteractionRaycaster raycaster;
    private readonly List<(Interactable target, Collider collider, TMP_Text mark)> marks = new();
    private void Start()
    {
        raycaster = GetComponent<InteractionRaycaster>();
        var canvasObject = new GameObject("操作対象の▲", typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 15;
        foreach (var root in gameObject.scene.GetRootGameObjects())
        foreach (var target in root.GetComponentsInChildren<Interactable>(true))
        {
            if (target.ActionType == PlayerAction.PressFloor && target.FloorNumber != 8) continue;
            var collider = target.GetComponent<Collider>();
            if (collider == null) continue;
            var mark = new GameObject(target.name + "_▲", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            mark.transform.SetParent(canvas.transform, false); mark.font = font; mark.text = "▲";
            mark.fontSize = size; mark.alignment = TextAlignmentOptions.Center; mark.raycastTarget = false;
            mark.rectTransform.sizeDelta = new Vector2(24, 24); mark.gameObject.SetActive(false);
            marks.Add((target, collider, mark));
        }
    }
    private void LateUpdate()
    {
        foreach (var item in marks)
        {
            bool visible = !DemoSession.BlocksGameplay && item.target != null && item.target.isActiveAndEnabled &&
                item.collider != null && item.collider.enabled && view != null;
            if (visible && item.target.TryGetComponent<EntranceControl>(out var entrance)) visible = entrance.Available;
            Vector3 point = visible ? item.collider.bounds.center : Vector3.zero;
            if (visible)
            {
                Vector3 direction = point - view.transform.position;
                visible = direction.magnitude <= raycaster.InteractionDistance &&
                    Physics.Raycast(view.transform.position, direction.normalized, out var hit,
                        direction.magnitude + .1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                    hit.collider.GetComponentInParent<Interactable>() == item.target;
            }
            Vector3 screen = visible ? view.WorldToScreenPoint(point) : Vector3.zero;
            visible &= screen.z > 0 && screen.x >= 0 && screen.x <= Screen.width && screen.y >= 0 && screen.y <= Screen.height;
            item.mark.gameObject.SetActive(visible);
            if (!visible) continue;
            item.mark.transform.position = new Vector3(screen.x, screen.y + size + 6, 0);
            item.mark.color = raycaster.CurrentTarget == item.target ? new Color(1, .91f, .65f) : new Color(.95f, .95f, .9f, .7f);
        }
    }
}
