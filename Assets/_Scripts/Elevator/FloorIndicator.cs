using System.Collections;
using TMPro;
using UnityEngine;

public class FloorIndicator : MonoBehaviour
{
    public static FloorIndicator Instance;

    [Header("References")]
    [SerializeField] private TMP_Text floorText;
    [SerializeField] private TMP_Text[] additionalDisplays = new TMP_Text[0];

    [Header("Display")]
    [SerializeField] private string displayFormat = "{0}";
    [SerializeField] private int initialDisplayedFloor = 1;

    [Header("Flicker")]
    [SerializeField, Min(1)] private int flickerCount = 4;
    [SerializeField, Min(0.01f)] private float flickerIntervalSeconds = 0.06f;

    private Coroutine driftRoutine;
    private Coroutine flickerRoutine;
    private int displayedFloor;

    public int CurrentDisplayedFloor => displayedFloor;

    private void Awake()
    {
        Instance = this;

        if (floorText == null)
        {
            floorText = GetComponent<TMP_Text>();
        }

        SetFloor(initialDisplayedFloor);
    }

    public void SetFloor(int floor)
    {
        StopDrift();

        displayedFloor = floor;
        RefreshText();
    }

    public void StartDrift(int targetFloor, float interval)
    {
        StopDrift();

        driftRoutine = StartCoroutine(DriftToFloor(targetFloor, interval));
    }

    public void StopDrift()
    {
        if (driftRoutine == null)
        {
            return;
        }

        StopCoroutine(driftRoutine);
        driftRoutine = null;
    }

    public void Flicker()
    {
        if (flickerRoutine != null)
        {
            StopCoroutine(flickerRoutine);
        }

        flickerRoutine = StartCoroutine(FlickerRoutine());
    }

    private IEnumerator DriftToFloor(int targetFloor, float interval)
    {
        if (interval <= 0f)
        {
            displayedFloor = targetFloor;
            RefreshText();
            driftRoutine = null;
            yield break;
        }

        while (displayedFloor != targetFloor)
        {
            displayedFloor += displayedFloor < targetFloor ? 1 : -1;
            RefreshText();
            yield return new WaitForSeconds(interval);
        }

        driftRoutine = null;
    }

    private IEnumerator FlickerRoutine()
    {
        if (floorText == null)
        {
            yield break;
        }

        for (int i = 0; i < flickerCount; i++)
        {
            SetDisplaysEnabled(false);
            yield return new WaitForSeconds(flickerIntervalSeconds);
            SetDisplaysEnabled(true);
            yield return new WaitForSeconds(flickerIntervalSeconds);
        }

        SetDisplaysEnabled(true);
        flickerRoutine = null;
    }

    private void RefreshText()
    {
        if (floorText != null)
        {
            floorText.text = string.Format(displayFormat, displayedFloor);
        }
        foreach (var display in additionalDisplays)
            if (display != null) display.text = string.Format(displayFormat, displayedFloor);
    }

    private void SetDisplaysEnabled(bool visible)
    {
        if (floorText != null) floorText.enabled = visible;
        foreach (var display in additionalDisplays)
            if (display != null) display.enabled = visible;
    }
}
