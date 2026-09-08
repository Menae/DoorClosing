using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RunManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BeatStateMachine beatStateMachine;
    [SerializeField] private NormalJourneyController nightJourney;
    [SerializeField] private Image blackFadeImage;
    [SerializeField] private TMP_Text nightClearText;

    [Header("Beat Script")]
    [SerializeField] private List<BeatDefinition> beatDefinitions = new List<BeatDefinition>();
    [SerializeField] private bool startRunOnStart = true;

    [Header("Debug Input")]
    [SerializeField] private bool enableDebugCommitKey = true;
    [SerializeField] private Key debugCommitKey = Key.Space;
    [SerializeField] private Key debugWrongCommitKey = Key.X;
    [SerializeField] private Key debugCloseCommitKey = Key.C;
    [SerializeField] private PlayerAction debugWrongAction = PlayerAction.PressOpen;
    [SerializeField] private int debugWrongFloorNumber = -1;

    [Header("Clear Presentation")]
    [SerializeField, Min(0f)] private float clearFadeSeconds = 1.5f;
    [SerializeField] private string clearMessage = "NIGHT CLEAR";

    [Header("Death Presentation")]
    [SerializeField, Min(0f)] private float deathFadeOutSeconds = 0.75f;
    [SerializeField, Min(0f)] private float deathBlackHoldSeconds = 0.5f;
    [SerializeField, Min(0f)] private float deathFadeInSeconds = 0.75f;

    private int currentBeatIndex = -1;
    private BeatDefinition currentBeat;
    private Coroutine clearRoutine;
    private Coroutine deathRoutine;
    private bool runClearInProgress;
    private bool deathRestartInProgress;
    private bool awaitingHomeReturn;

    private void Awake()
    {
        if (beatStateMachine == null)
        {
            beatStateMachine = GetComponent<BeatStateMachine>();
        }

        PrepareClearUi();
    }

    private void OnEnable()
    {
        GameEvents.OnBeatStateChanged += HandleBeatStateChanged;
        GameEvents.OnBeatResolved += HandleBeatResolved;
    }

    private void OnDisable()
    {
        GameEvents.OnBeatStateChanged -= HandleBeatStateChanged;
        GameEvents.OnBeatResolved -= HandleBeatResolved;
    }

    private void Start()
    {
        if (startRunOnStart)
        {
            StartRunFromBeginning();
        }
    }

    private void Update()
    {
        if (!enableDebugCommitKey || runClearInProgress || deathRestartInProgress || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[debugCommitKey].wasPressedThisFrame)
        {
            SubmitDebugCorrectAction();
        }

        if (Keyboard.current[debugWrongCommitKey].wasPressedThisFrame)
        {
            SubmitDebugWrongAction();
        }

        if (Keyboard.current[debugCloseCommitKey].wasPressedThisFrame)
        {
            SubmitDebugCloseAction();
        }
    }

    public void StartRunFromBeginning()
    {
        if (beatDefinitions == null || beatDefinitions.Count == 0)
        {
            Debug.LogError($"{nameof(RunManager)} needs at least one {nameof(BeatDefinition)}.", this);
            return;
        }

        if (beatStateMachine == null)
        {
            Debug.LogError($"{nameof(RunManager)} needs a {nameof(BeatStateMachine)} reference.", this);
            return;
        }

        runClearInProgress = false;
        deathRestartInProgress = false;
        awaitingHomeReturn = false;
        PrepareClearUi();
        StartBeatAtIndex(0);
    }

    public void BeginEncounterRun()
    {
        StartRunFromBeginning();
    }

    public void CompleteRunAfterHome()
    {
        if (!awaitingHomeReturn || runClearInProgress)
        {
            Debug.LogWarning("[Run] Ignored home completion outside the post-encounter return.", this);
            return;
        }

        awaitingHomeReturn = false;
        CompleteRun();
    }

    private void StartBeatAtIndex(int beatIndex)
    {
        if (beatIndex < 0 || beatIndex >= beatDefinitions.Count)
        {
            Debug.LogError($"{nameof(RunManager)} beat index {beatIndex} is out of range.", this);
            return;
        }

        currentBeatIndex = beatIndex;
        currentBeat = beatDefinitions[currentBeatIndex];

        if (currentBeat == null)
        {
            Debug.LogError($"{nameof(RunManager)} beat {currentBeatIndex + 1} is not assigned.", this);
            return;
        }

        WarnIfFinalBeatCannotClearWithDebugKey();
        Debug.Log($"[Run] Begin beat {currentBeatIndex + 1}/{beatDefinitions.Count}: {currentBeat.DebugLabel}", this);
        beatStateMachine.BeginBeat(currentBeat);
    }

    private void WarnIfFinalBeatCannotClearWithDebugKey()
    {
        if (!IsFinalBeat() || currentBeat == null)
        {
            return;
        }

        if (currentBeat.Category == AnomalyCategory.Normal && currentBeat.CorrectAction == PlayerAction.PressClose)
        {
            Debug.LogWarning($"[Run] Final beat '{currentBeat.DebugLabel}' is Normal + PressClose. Space submits PressClose, but ResponseEvaluator treats Normal/PressClose as Represented, so RunClear will not fire. Use CorrectAction={PlayerAction.TouchHomeDoor} for clear testing.", this);
        }
    }

    private void SubmitDebugCorrectAction()
    {
        if (currentBeat == null || beatStateMachine == null)
        {
            return;
        }

        Debug.Log($"[Run][Debug] Submit {currentBeat.CorrectAction} floor={currentBeat.CorrectFloorNumber} for {currentBeat.DebugLabel}", this);
        beatStateMachine.SubmitAction(currentBeat.CorrectAction, currentBeat.CorrectFloorNumber);
    }

    private void SubmitDebugWrongAction()
    {
        if (currentBeat == null || beatStateMachine == null)
        {
            return;
        }

        Debug.Log($"[Run][Debug] Submit wrong {debugWrongAction} floor={debugWrongFloorNumber} for {currentBeat.DebugLabel}", this);
        beatStateMachine.SubmitAction(debugWrongAction, debugWrongFloorNumber);
    }

    private void SubmitDebugCloseAction()
    {
        if (currentBeat == null || beatStateMachine == null)
        {
            return;
        }

        Debug.Log($"[Run][Debug] Submit {PlayerAction.PressClose} for {currentBeat.DebugLabel}", this);
        beatStateMachine.SubmitAction(PlayerAction.PressClose);
    }

    private void HandleBeatStateChanged(BeatState newState)
    {
        string beatLabel = currentBeat != null ? currentBeat.DebugLabel : "No Beat";
        Debug.Log($"[BeatState] {beatLabel}: {newState}", this);
    }

    private void HandleBeatResolved(BeatOutcome outcome)
    {
        Debug.Log($"[BeatOutcome] {outcome}", this);

        if (outcome == BeatOutcome.RunClear || runClearInProgress || deathRestartInProgress)
        {
            return;
        }

        if (outcome == BeatOutcome.Death)
        {
            Debug.Log("[Run] Death. Restarting from the first beat after fade.", this);
            BeginDeathRestart();
            return;
        }

        if (outcome != BeatOutcome.Correct && outcome != BeatOutcome.GraceRecovered)
        {
            return;
        }

        if (IsFinalBeat())
        {
            if (nightJourney != null)
            {
                awaitingHomeReturn = true;
                nightJourney.PresentHomeAfterEncounters();
                return;
            }

            CompleteRun();
            return;
        }

        int nextBeatIndex = currentBeatIndex + 1;
        if (nextBeatIndex < beatDefinitions.Count)
        {
            StartBeatAtIndex(nextBeatIndex);
            return;
        }

        Debug.LogWarning("[Run] Success outcome received but no next beat exists. Treating as run clear.", this);
        CompleteRun();
    }

    private bool IsFinalBeat()
    {
        return beatDefinitions != null && currentBeatIndex == beatDefinitions.Count - 1;
    }

    private void CompleteRun()
    {
        runClearInProgress = true;
        Debug.Log("[Run] Run clear.", this);
        GameEvents.RaiseBeatResolved(BeatOutcome.RunClear);

        if (clearRoutine != null)
        {
            StopCoroutine(clearRoutine);
        }

        clearRoutine = StartCoroutine(ShowNightClear());
    }

    private void BeginDeathRestart()
    {
        deathRestartInProgress = true;

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
        }

        deathRoutine = StartCoroutine(RestartAfterDeathFade());
    }

    private IEnumerator RestartAfterDeathFade()
    {
        if (blackFadeImage == null)
        {
            Debug.Log("[Run] Death fade image is not assigned. Restarting immediately.", this);
            StartRunFromBeginning();
            deathRoutine = null;
            yield break;
        }

        if (nightClearText != null)
        {
            nightClearText.gameObject.SetActive(false);
        }

        blackFadeImage.gameObject.SetActive(true);
        yield return FadeBlackImage(0f, 1f, deathFadeOutSeconds);
        yield return WaitForSecondsFromDefinition(deathBlackHoldSeconds);

        if (nightJourney != null)
        {
            runClearInProgress = false;
            awaitingHomeReturn = false;
            currentBeatIndex = -1;
            currentBeat = null;
            PrepareClearUi();
            nightJourney.RestartNightAtEntrance();
        }
        else
        {
            StartRunFromBeginning();
        }
        deathRestartInProgress = true;

        blackFadeImage.gameObject.SetActive(true);
        yield return FadeBlackImage(1f, 0f, deathFadeInSeconds);
        blackFadeImage.gameObject.SetActive(false);

        deathRestartInProgress = false;
        deathRoutine = null;
    }

    private IEnumerator ShowNightClear()
    {
        if (blackFadeImage == null || nightClearText == null)
        {
            Debug.Log(clearMessage, this);
            yield break;
        }

        nightClearText.text = clearMessage;
        nightClearText.gameObject.SetActive(false);
        blackFadeImage.gameObject.SetActive(true);

        yield return FadeBlackImage(0f, 1f, clearFadeSeconds);
        nightClearText.gameObject.SetActive(true);
    }

    private IEnumerator FadeBlackImage(float fromAlpha, float toAlpha, float seconds)
    {
        if (blackFadeImage == null)
        {
            yield break;
        }

        Color fadeColor = blackFadeImage.color;
        fadeColor.a = fromAlpha;
        blackFadeImage.color = fadeColor;

        if (seconds <= 0f)
        {
            fadeColor.a = toAlpha;
            blackFadeImage.color = fadeColor;
            yield break;
        }

        float elapsedSeconds = 0f;
        while (elapsedSeconds < seconds)
        {
            elapsedSeconds += Time.deltaTime;
            fadeColor.a = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(elapsedSeconds / seconds));
            blackFadeImage.color = fadeColor;
            yield return null;
        }

        fadeColor.a = toAlpha;
        blackFadeImage.color = fadeColor;
    }

    private IEnumerator WaitForSecondsFromDefinition(float seconds)
    {
        if (seconds <= 0f)
        {
            yield break;
        }

        yield return new WaitForSeconds(seconds);
    }

    private void PrepareClearUi()
    {
        if (blackFadeImage != null)
        {
            Color fadeColor = blackFadeImage.color;
            fadeColor.a = 0f;
            blackFadeImage.color = fadeColor;
            blackFadeImage.gameObject.SetActive(false);
        }

        if (nightClearText != null)
        {
            nightClearText.text = clearMessage;
            nightClearText.gameObject.SetActive(false);
        }
    }
}
