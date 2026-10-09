namespace LocalDeepResearch;

/// <summary>
/// Static class for all of our generic helper functions.
/// </summary>
public static class Helpers
{
    /// <summary>
    /// Converts a timespan into a formatted string.
    /// </summary>
    /// <param name="duration"> Total duration to format </param>
    /// <returns> Formatted time string </returns>
    public static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return duration.ToString(@"h\:mm\:ss");

        if (duration.TotalMinutes >= 1)
            return duration.ToString(@"m\:ss");

        return duration.ToString(@"s\s");
    }
}
