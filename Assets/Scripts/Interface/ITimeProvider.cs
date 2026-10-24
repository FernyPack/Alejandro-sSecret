public interface ITimeProvider
{
    float CurrentTimeOfDay     { get; set; }
    float DayDurationInSeconds { get; set; }
    int   CurrentDay           { get; }
    int   CurrentMonth         { get; }
    int   CurrentYear          { get; }
    float TotalElapsedDays     { get; }
    float WakeUpTimeOfDay      { get; }

    string GetFormattedDate();
    string GetFormattedTime();

    void SkipToNextDay();
    void SkipToTimeOfDay(float targetTimeOfDay);
}
