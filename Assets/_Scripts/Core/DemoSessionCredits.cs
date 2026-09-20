using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class DemoSession
{
    private Coroutine creditsRoutine;
    private GameObject creditsSkip;
    private CanvasGroup creditsFade;

    private void ShowCredits()
    {
        var credits = FindFirstObjectByType<HomecomingCredits>();
        if (credits == null || credits.pages == null || credits.pages.Length == 0) { Restart(); return; }
        Clear("Credits");
        creditsFade = panel.GetComponent<CanvasGroup>();
        if (creditsFade == null) creditsFade = panel.gameObject.AddComponent<CanvasGroup>();
        // Keep skip outside the scrolling/fading copy, even when an author writes a long page.
        Button(credits.skipLabel, FinishCredits);
        creditsSkip = panel.GetChild(panel.childCount - 1).gameObject;
        var rect = (RectTransform)creditsSkip.transform; rect.SetParent(overlay.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0); rect.pivot = new Vector2(.5f, 0);
        rect.anchoredPosition = new Vector2(0, 22); rect.sizeDelta = new Vector2(480, 56);
        creditsRoutine = StartCoroutine(RollCredits(credits));
    }
    private IEnumerator RollCredits(HomecomingCredits credits)
    {
        foreach (var entry in credits.pages)
        {
            if (entry == null) continue;
            Clear("Credits"); Heading(entry.heading ?? ""); Copy(entry.body ?? "", 25);
            yield return FadeCredits(0, 1, credits.fadeSeconds);
            float remaining = Mathf.Max(1, credits.secondsPerPage);
            while (remaining > 0)
            {
                // Losing focus must not consume the viewer's reading time.
                if (Application.isFocused) remaining -= Time.unscaledDeltaTime;
                yield return null;
            }
            yield return FadeCredits(1, 0, credits.fadeSeconds);
        }
        creditsRoutine = null; FinishCredits();
    }
    private IEnumerator FadeCredits(float from, float to, float seconds)
    {
        creditsFade.alpha = from;
        for (float elapsed = 0; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
        { creditsFade.alpha = Mathf.Lerp(from, to, elapsed / seconds); yield return null; }
        creditsFade.alpha = to;
    }
    private void FinishCredits()
    {
        if (creditsRoutine != null) StopCoroutine(creditsRoutine);
        creditsRoutine = null;
        if (creditsFade != null) creditsFade.alpha = 1;
        if (creditsSkip != null) { creditsSkip.SetActive(false); Destroy(creditsSkip); }
        Restart();
    }
}
