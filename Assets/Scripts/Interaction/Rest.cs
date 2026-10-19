using UnityEngine;

public class Rest : Interactable
{
    [Header("Rest Settings")]
    [Tooltip("ON: resting skips straight to the next day. OFF: time fast-forwards in real time.")]
    public bool instantSkip = true;

    [Tooltip("Only used if Instant Skip is OFF. How many real seconds the fast-forward takes.")]
    public float fastForwardDuration = 2f;

    private ITimeProvider timeProvider;
    private bool isFastForwarding = false;
    private float originalDayDuration;

    void Start()
    {
        timeProvider = ServiceLocator.Get<ITimeProvider>();

        if (timeProvider == null)
            Debug.LogError("Rest could not find an ITimeProvider via ServiceLocator!");
    }

    void Update()
    {
        if (isFastForwarding && timeProvider != null)
        {
            if (timeProvider.CurrentTimeOfDay < 0.01f)
                StopFastForward();
        }
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
        else StartFastForward();
    }

    private void SkipToNextDay()
    {
        timeProvider.CurrentTimeOfDay = 1f;
        Debug.Log("Rested! Skipping to the next day.");
    }

    private void StartFastForward()
    {
        isFastForwarding = true;
        originalDayDuration = timeProvider.DayDurationInSeconds;

        float remainingFraction = 1f - timeProvider.CurrentTimeOfDay;
        if (remainingFraction <= 0f) remainingFraction = 1f;

        timeProvider.DayDurationInSeconds = originalDayDuration * remainingFraction / fastForwardDuration;
        Debug.Log($"Rest: Fast-forwarding over {fastForwardDuration} real second(s).");
    }

    private void StopFastForward()
    {
        isFastForwarding = false;
        timeProvider.DayDurationInSeconds = originalDayDuration;
        Debug.Log("Rest: Fast-forward complete. Time restored to normal speed.");
    }
}