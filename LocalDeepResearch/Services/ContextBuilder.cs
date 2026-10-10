namespace LocalDeepResearch.Services;

/// <summary>
/// Static class for building our model input context.
/// </summary>
public static class ContextBuilder
{
    /// <summary>
    /// How much of the context budget a single source can use (%).
    /// </summary>
    private const double SourceShare = 0.25;
    
    /// <summary>
    /// Returns highest rated text passages and ensures we stay below our context token budget.
    /// </summary>
    /// <param name="passages"> Relevant text passages </param>
    /// <param name="budget"> Token budget </param>
    /// <returns> Final list of relevant text passages </returns>
    public static IReadOnlyList<ScoredPassage> Select(IReadOnlyList<ScoredPassage> passages, int budget)
    {
        // Token limit per source
        int sourceLimit = (int)(budget * SourceShare);
        List<ScoredPassage> ordered = passages.OrderByDescending(passage => passage.Score).ToList();

        List<ScoredPassage> selectedPassages = new();
        List<ScoredPassage> skippedPassages = new();
        Dictionary<string, int> sourceTokens = new();
        int usedTokens = 0;

        foreach (ScoredPassage passage in ordered)
        {
            sourceTokens.TryGetValue(passage.Passage.Host, out int sourceTokenCount);

            // Prevent token count from going over budget and prevent sources from going over their token limit
            if (usedTokens + passage.Tokens > budget || sourceTokenCount + passage.Tokens > sourceLimit)
            {
                skippedPassages.Add(passage);
                continue;
            }

            selectedPassages.Add(passage);
            usedTokens += passage.Tokens;
            sourceTokens[passage.Passage.Host] = sourceTokenCount + passage.Tokens;
        }

        // Fill any unused context space with previously skipped passages, even if it goes over source limit
        foreach (ScoredPassage passage in skippedPassages)
        {
            if (usedTokens + passage.Tokens > budget)
                continue;

            selectedPassages.Add(passage);
            usedTokens += passage.Tokens;
        }

        return selectedPassages;
    }
    /// <summary>
    /// Counts how many unique sources there are in a list of passages.
    /// </summary>
    /// <param name="passages"> Collection of scored passages </param>
    /// <returns> Number of unique sources </returns>
    public static int CountSources(IEnumerable<ScoredPassage> passages)
    {
        return passages.Select(passage => passage.Passage.Host).Distinct().Count();
    }
}

/// <summary>
/// Passage with its score and total token count.
/// </summary>
/// <param name="Passage"> Scored passage </param>
/// <param name="Score"> Passage score </param>
/// <param name="Tokens"> Total token count for this passage </param>
public record ScoredPassage(Passage Passage, float Score, int Tokens);
