using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class HomecomingPresentation : MonoBehaviour
{
    [Header("本編の開始・夜のつなぎ")]
    [SerializeField, Min(0)] private float fadeInSeconds = 1.1f;
    [SerializeField, Min(0)] private float nightBlackSeconds = 1.4f;
    [SerializeField] private Transform followingNightStart;
    private Image cover;
    internal float NightBlackSeconds => nightBlackSeconds;
    internal void PrepareFollowingNight(NormalJourneyController journey)
    {
        if (followingNightStart != null) journey.SetNightStartPose(followingNightStart.position, followingNightStart.rotation);
    }
    internal void Cover()
    {
        if (cover == null)
        {
            var go = new GameObject("夜の暗転", typeof(RectTransform), typeof(Canvas), typeof(Image));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 490;
            cover = go.GetComponent<Image>(); cover.raycastTarget = false;
        }
        cover.gameObject.SetActive(true); cover.color = Color.black;
    }
    internal IEnumerator Reveal()
    {
        Cover();
        float elapsed = 0;
        while (elapsed < fadeInSeconds)
        {
            elapsed += Time.deltaTime;
            cover.color = new Color(0, 0, 0, 1 - Mathf.Clamp01(elapsed / fadeInSeconds));
            yield return null;
        }
        cover.gameObject.SetActive(false);
    }
}
