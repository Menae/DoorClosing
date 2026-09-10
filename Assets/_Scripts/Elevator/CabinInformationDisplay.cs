using System.Collections;
using TMPro;
using UnityEngine;

// The housing belongs to the scene. Encounters change its content, never spawn a wall fixture.
public class CabinInformationDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text content;
    [SerializeField] private string standbyMessage = "運転中\n扉から離れて\nお待ちください";
    private Coroutine transition;
    public bool IsShowingAnnouncement { get; private set; }

    private void Awake() => Restore();

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
        content.text = standbyMessage;
        content.color = new Color(.78f,.86f,.80f,1f);
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
