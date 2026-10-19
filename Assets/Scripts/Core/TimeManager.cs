using UnityEngine;

public class TimeManager : MonoBehaviour, ITimeProvider
{
    [Header("Time Calibration")]
    [Tooltip("How long one full in-game day takes in real-world seconds.")]
    public float dayDurationInSeconds = 60f;

    [Header("Current Time Data Source")]
    [Range(0f, 1f)]
    public float currentTimeOfDay = 0f;

    [Header("Calendar Settings")]
    [Tooltip("Hours in one in-game day.")]
    public int hoursInDay = 24;
    [Tooltip("Minutes in one in-game hour.")]
    public int minutesInHour = 60;
    [Tooltip("Seconds in one in-game minute.")]
    public int secondsInMinute = 60;
    [Tooltip("Days in one in-game month.")]
    public int daysInMonth = 30;
    [Tooltip("Months in one in-game year.")]
    public int monthsInYear = 12;

    [Header("Calculated Values (Read Only)")]
    public int currentSecond;
    public int currentMinute;
    public int currentHour;
    public int currentDay   = 1;
    public int currentMonth = 1;
    public int currentYear  = 2026;

    public float CurrentTimeOfDay
    {
        get => currentTimeOfDay;
        set => currentTimeOfDay = value;
    }

    public float DayDurationInSeconds
    {
        get => dayDurationInSeconds;
        set => dayDurationInSeconds = value;
    }

    public int CurrentDay   => currentDay;
    public int CurrentMonth => currentMonth;
    public int CurrentYear  => currentYear;

    public float TotalElapsedDays => completedDays + currentTimeOfDay;

    private int completedDays = 0;

    void Awake()
    {
        ServiceLocator.Register<ITimeProvider>(this);
        Debug.Log("[TimeManager] Registered as ITimeProvider.");
    }

    void Update()
    {
        CalculateInGameClock();
    }

    public string GetFormattedDate()
        => $"{currentMonth:D2}/{currentDay:D2}/{currentYear}";

    public string GetFormattedTime()
    {
        string period = currentHour >= 12 ? "PM" : "AM";
        int hour12    = currentHour % 12;
        if (hour12 == 0) hour12 = 12;
        return $"{hour12}:{currentMinute:D2} {period}";
    }

    public void SkipToNextDay()
    {
        currentTimeOfDay = 0f;
        AdvanceCalendarDay();
        UpdateClockDisplay();
        Debug.Log("[TimeManager] Skipped to the start of the next day.");
    }

    private void CalculateInGameClock()
    {
        if (dayDurationInSeconds <= 0) dayDurationInSeconds = 1f;

        currentTimeOfDay += Time.deltaTime / dayDurationInSeconds;

        while (currentTimeOfDay >= 1f)
        {
            currentTimeOfDay -= 1f;
            AdvanceCalendarDay();
        }

        UpdateClockDisplay();
    }

    private void UpdateClockDisplay()
    {
        float secondsPerHour        = minutesInHour * secondsInMinute;
        float totalSecondsInDay     = hoursInDay * secondsPerHour;
        float currentSecondsElapsed = currentTimeOfDay * totalSecondsInDay;

        currentHour   = Mathf.FloorToInt(currentSecondsElapsed / secondsPerHour);
        float remMins = currentSecondsElapsed % secondsPerHour;
        currentMinute = Mathf.FloorToInt(remMins / secondsInMinute);
        currentSecond = Mathf.FloorToInt(remMins % secondsInMinute);
    }

    private void AdvanceCalendarDay()
    {
        completedDays++;
        currentDay++;
        if (currentDay > daysInMonth)
        {
            currentDay = 1;
            currentMonth++;
            if (currentMonth > monthsInYear)
            {
                currentMonth = 1;
                currentYear++;
            }
        }
    }
}
