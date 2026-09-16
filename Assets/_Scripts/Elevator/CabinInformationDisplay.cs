using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// The housing belongs to the scene. Encounters change its content, never spawn a wall fixture.
public class CabinInformationDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text content;
    [SerializeField] private string standbyMessage = "運転中\n扉から離れて\nお待ちください";
    [SerializeField, TextArea(2, 8)] private List<string> additionalMessages = new List<string>();
    [SerializeField, Min(.05f)] private float displaySeconds = 5f;
    [SerializeField, Min(0f)] private float fadeSeconds = .5f;
    private Coroutine transition;
    public bool IsShowingAnnouncement { get; private set; }

    private void OnEnable() => Restore();
    private void OnDisable()
    {
        if (transition != null) StopCoroutine(transition);
        transition = null;
        IsShowingAnnouncement = false;
    }

    public void Present(string message)
    {
        if (transition != null) StopCoroutine(transition);
        transition = StartCoroutine(ChangeContent(message));
    }

    public void Restore()
    {
        if (transition != null) StopCoroutine(transition);
        transition = null;
        IsShowingAnnouncement = false;
        if (content == null) return;
        content.text = GameTextCollection.Get(this, "cabin.standby", standbyMessage);
        content.color = new Color(.78f,.86f,.80f,1f);
        if (isActiveAndEnabled) transition = StartCoroutine(Slideshow());
    }

    private IEnumerator Slideshow()
    {
        int index = 0;
        while (true)
        {
            yield return new WaitForSeconds(Mathf.Max(.05f, displaySeconds));
            int count = 1 + (additionalMessages?.Count ?? 0);
            if (count == 1) continue; // A single authored message stays readable without blinking.
            float duration = Mathf.Max(0f, fadeSeconds);
            yield return FadeTo(0f, duration);
            count = 1 + (additionalMessages?.Count ?? 0);
            index = (index + 1) % count;
            content.text = index == 0 ? GameTextCollection.Get(this, "cabin.standby", standbyMessage)
                : additionalMessages[index - 1] ?? "";
            yield return FadeTo(1f, duration);
        }
    }

    private IEnumerator FadeTo(float alpha, float seconds)
    {
        float start = content.alpha;
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
        {
            content.alpha = Mathf.Lerp(start, alpha, elapsed / seconds);
            yield return null;
        }
        content.alpha = alpha;
    }

    private IEnumerator ChangeContent(string message)
    {
        if (content == null) yield break;
        IsShowingAnnouncement = false;
        for (float elapsed=0;elapsed<.15f;elapsed+=Time.deltaTime)
        {
            content.alpha = 1f-Mathf.Clamp01(elapsed/.15f);
            yield return null;
        }
        content.text = message;
        content.color = new Color(.95f,.70f,.36f,0f);
        for (float elapsed=0;elapsed<.15f;elapsed+=Time.deltaTime)
        {
            content.alpha = Mathf.Clamp01(elapsed/.15f);
            yield return null;
        }
        content.alpha = 1f;
        IsShowingAnnouncement = true;
        transition = null;
    }
}
