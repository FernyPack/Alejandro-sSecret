using UnityEngine;

public class Rest : Interactable
{
    [Header("Rest Settings")]
    public bool instantSkip = true;
    public float fastForwardDuration = 2f;

    [Header("UI Reference")]
    public OptionUI optionUI;

    [Header("Rest Dialogue (Editable in Inspector)")]
    [TextArea(2, 5)]
    public string restPrompt = "Rest and skip to the next day?";

    [TextArea(2, 5)]
    public string wakeUpDialogue = "You slept well. A new day begins!";

    private ITimeProvider timeProvider;
    private PlayerAction pendingPlayerAction;
    private bool isFastForwarding = false;
    private float originalDayDuration;
    private float previousTimeOfDay;

    void Start()
    {
        timeProvider = ServiceLocator.Get<ITimeProvider>();

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
            StopFastForward();
        else
            previousTimeOfDay = now;
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

        if (isFastForwarding) return;

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
        if (instantSkip)
        {
            SkipToNextDay();
            playerAction?.DisplayDialogue(wakeUpDialogue);
        }
        else
        {
            pendingPlayerAction = playerAction;
            StartFastForward();
        }
    }

    private void SkipToNextDay()
    {
        timeProvider.CurrentTimeOfDay = 1f;
    }

    private void StartFastForward()
    {
        isFastForwarding = true;
        originalDayDuration = timeProvider.DayDurationInSeconds;
        previousTimeOfDay = timeProvider.CurrentTimeOfDay;

        float remainingFraction = 1f - timeProvider.CurrentTimeOfDay;
        if (remainingFraction <= 0f) remainingFraction = 1f;

        float duration = Mathf.Max(0.1f, fastForwardDuration);
        timeProvider.DayDurationInSeconds = originalDayDuration * remainingFraction / duration;
    }

    private void StopFastForward()
    {
        isFastForwarding = false;
        timeProvider.DayDurationInSeconds = originalDayDuration;

        pendingPlayerAction?.DisplayDialogue(wakeUpDialogue);
        pendingPlayerAction = null;
    }
}
