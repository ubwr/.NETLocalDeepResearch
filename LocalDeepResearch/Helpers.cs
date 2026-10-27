namespace LocalDeepResearch;

/// <summary>
/// Static class for all of our generic helper functions.
/// </summary>
public static class Helpers
{
    /// <summary>
    /// Truncates text to a given length.
    /// </summary>
    /// <param name="text"> Text to truncate </param>
    /// <param name="maxLength"> Maximum text length </param>
    /// <returns> Truncated string </returns>
    public static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }
    /// <summary>
    /// Converts a double to a percentage.
    /// </summary>
    /// <param name="value"> Value between 0 and 1 </param>
    /// <returns> Percentage </returns>
    public static int ToPercent(double value)
    {
        return (int)Math.Round(value * 100, MidpointRounding.AwayFromZero);
    }
}
