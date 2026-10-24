using System.Collections;
using UnityEngine;

public class Rest : Interactable
{
    [Header("Rest Settings")]
    public bool instantSkip = true;
    public float fastForwardDuration = 2f;

    [Tooltip("ON: you wake up at the Wake Up Hour set on the TimeManager. OFF: you wake up at midnight (start of the next day).")]
    public bool useFixedWakeUpTime = true;

    [Header("Fade Settings")]
    [Tooltip("ON: screen fades to black while resting. OFF: no fade.")]
    public bool useFade = true;

    [Tooltip("Seconds to fade to black.")]
    public float fadeOutDuration = 1f;

    [Tooltip("Seconds to stay black before fading back in.")]
    public float blackScreenHoldDuration = 0.5f;

    [Tooltip("Seconds to fade back from black.")]
    public float fadeInDuration = 1f;

    [Header("UI Reference")]
    public OptionUI optionUI;

    [Header("Rest Dialogue (Editable in Inspector)")]
    [TextArea(2, 5)]
    public string restPrompt = "Rest and skip to the next day?";

    [TextArea(2, 5)]
    public string wakeUpDialogue = "You slept well. A new day begins!";

    private ITimeProvider timeProvider;
    private IScreenFader screenFader;
    private PlayerAction pendingPlayerAction;
    private bool isFastForwarding = false;
    private bool isResting = false;
    private bool waitingForMidnight = false;
    private float originalDayDuration;
    private float previousTimeOfDay;
    private float targetTimeOfDay;

    void Start()
    {
        timeProvider = ServiceLocator.Get<ITimeProvider>();
        screenFader  = ServiceLocator.Get<IScreenFader>();

        if (timeProvider == null)
            Debug.LogError("Rest could not find an ITimeProvider via ServiceLocator!");

        if (optionUI == null)
            optionUI = FindFirstObjectByType<OptionUI>();
    }

    void Update()
    {
        if (!isFastForwarding || timeProvider == null) return;

        float now = timeProvider.CurrentTimeOfDay;

        if (now < previousTimeOfDay)
            waitingForMidnight = false;

        previousTimeOfDay = now;

        if (!waitingForMidnight && now >= targetTimeOfDay)
            StopFastForward();
    }

    public override void Interact(GameObject playerObject)
    {
        if (timeProvider == null)
            timeProvider = ServiceLocator.Get<ITimeProvider>();

        if (timeProvider == null)
        {
            Debug.LogWarning("Rest: No ITimeProvider found, cannot skip day.");
            return;
        }

        if (isFastForwarding || isResting) return;

        if (optionUI == null)
            optionUI = FindFirstObjectByType<OptionUI>();

        PlayerAction playerAction = playerObject.GetComponent<PlayerAction>();
        playerAction?.DisplayDialogue(restPrompt);

        optionUI?.ShowYesNo(
            onYes: () =>
            {
                playerAction?.CloseDialogue();
                BeginRest(playerAction);
            },
            onNo: () => playerAction?.CloseDialogue()
        );
    }

    private void BeginRest(PlayerAction playerAction)
    {
        if (screenFader == null)
            screenFader = ServiceLocator.Get<IScreenFader>();

        StartCoroutine(RestRoutine(playerAction));
    }

    private IEnumerator RestRoutine(PlayerAction playerAction)
    {
        isResting = true;
        PlayerStateManager.SetState(PlayerState.Cutscene);

        yield return FadeOutRoutine();

        if (instantSkip)
        {
            SkipToNextDay();
            yield return FinishRestRoutine(playerAction);
        }
        else
        {
            pendingPlayerAction = playerAction;
            StartFastForward();
        }
    }

    private IEnumerator FinishRestRoutine(PlayerAction playerAction)
    {
        if (blackScreenHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(blackScreenHoldDuration);

        yield return FadeInRoutine();

        isResting = false;
        PlayerStateManager.SetState(PlayerState.Free);
        playerAction?.DisplayDialogue(wakeUpDialogue);
    }

    private IEnumerator FadeOutRoutine()
    {
        if (useFade && screenFader != null)
            yield return screenFader.FadeOut(fadeOutDuration);
    }

    private IEnumerator FadeInRoutine()
    {
        if (useFade && screenFader != null)
            yield return screenFader.FadeIn(fadeInDuration);
    }

    private float GetTargetTimeOfDay()
    {
        return useFixedWakeUpTime ? timeProvider.WakeUpTimeOfDay : 0f;
    }

    private void SkipToNextDay()
    {
        timeProvider.SkipToTimeOfDay(GetTargetTimeOfDay());
    }

    private void StartFastForward()
    {
        isFastForwarding = true;
        originalDayDuration = timeProvider.DayDurationInSeconds;
        previousTimeOfDay = timeProvider.CurrentTimeOfDay;
        targetTimeOfDay = GetTargetTimeOfDay();
        waitingForMidnight = previousTimeOfDay >= targetTimeOfDay;

        float remainingFraction = waitingForMidnight
            ? (1f - previousTimeOfDay) + targetTimeOfDay
            : targetTimeOfDay - previousTimeOfDay;

        if (remainingFraction <= 0f) remainingFraction = 1f;

        float duration = Mathf.Max(0.1f, fastForwardDuration);
        timeProvider.DayDurationInSeconds = originalDayDuration * remainingFraction / duration;
    }

    private void StopFastForward()
    {
        isFastForwarding = false;
        timeProvider.DayDurationInSeconds = originalDayDuration;

        PlayerAction playerAction = pendingPlayerAction;
        pendingPlayerAction = null;

        StartCoroutine(FinishRestRoutine(playerAction));
    }
}
