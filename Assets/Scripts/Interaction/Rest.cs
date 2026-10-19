using UnityEngine;

public class Rest : Interactable
{
    [Header("Rest Settings")]
    [Tooltip("ON = instantly skip to the next day. OFF = fast-forward in real time.")]
    public bool instantSkip = true;

    [Tooltip("Only used if Instant Skip is OFF. How many real seconds the fast-forward takes.")]
    public float fastForwardDuration = 2f;

    [Header("Rest Dialogue (Editable in Inspector)")]
    [TextArea(2, 5)]
    public string restPrompt = "Press Enter to rest and skip to the next day.";

    private ITimeProvider timeProvider;
    private bool  isFastForwarding = false;
    private float originalDayDuration;
    private int   startDay;
    private int   startMonth;
    private int   startYear;

    void Start()
    {
        timeProvider = ServiceLocator.Get<ITimeProvider>();

        if (timeProvider == null)
            Debug.LogError("Rest.cs could not find an ITimeProvider via ServiceLocator!");

        dialogueText = restPrompt;
    }

    void Update()
    {
        if (!isFastForwarding || timeProvider == null) return;

        bool dayChanged = timeProvider.CurrentDay   != startDay
                       || timeProvider.CurrentMonth != startMonth
                       || timeProvider.CurrentYear  != startYear;

        if (dayChanged)
            StopFastForward();
    }

    public override void Interact(GameObject playerObject)
    {
        if (timeProvider == null)
        {
            Debug.LogWarning("Rest: No ITimeProvider found, cannot skip day.");
            return;
        }

        if (isFastForwarding) return;

        if (instantSkip) SkipToNextDay();
        else             StartFastForward();
    }

    private void SkipToNextDay()
    {
        timeProvider.SkipToNextDay();
        Debug.Log("Rested! Skipping to the next day.");
    }

    private void StartFastForward()
    {
        isFastForwarding    = true;
        originalDayDuration = timeProvider.DayDurationInSeconds;
        startDay            = timeProvider.CurrentDay;
        startMonth          = timeProvider.CurrentMonth;
        startYear           = timeProvider.CurrentYear;

        float remainingFraction = 1f - timeProvider.CurrentTimeOfDay;
        if (remainingFraction <= 0.0001f) remainingFraction = 1f;

        float safeDuration = Mathf.Max(0.01f, fastForwardDuration);
        timeProvider.DayDurationInSeconds = safeDuration / remainingFraction;
        Debug.Log($"Rest: Fast-forwarding to next day over {safeDuration} real second(s).");
    }

    private void StopFastForward()
    {
        isFastForwarding = false;
        timeProvider.DayDurationInSeconds = originalDayDuration;
        Debug.Log("Rest: Fast-forward complete. Day has changed, time restored to normal speed.");
    }
}
