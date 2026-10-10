namespace LocalDeepResearch.Services;

/// <summary>
/// Static class for building our model input context.
/// </summary>
public static class ContextBuilder
{
    /// <summary>
    /// How much of the context budget a single site can use (%).
    /// </summary>
    private const double SiteShare = 0.25;
    
    /// <summary>
    /// Returns highest rated text passages and ensures we stay below our context token budget.
    /// </summary>
    /// <param name="passages"> Relevant text passages </param>
    /// <param name="budget"> Token budget </param>
    /// <returns> Final list of relevant text passages </returns>
    public static IReadOnlyList<ScoredPassage> Select(IReadOnlyList<ScoredPassage> passages, int budget)
    {
        // Token limit per site
        int siteLimit = (int)(budget * SiteShare);
        List<ScoredPassage> ordered = passages.OrderByDescending(passage => passage.Score).ToList();

        List<ScoredPassage> selectedPassages = new();
        List<ScoredPassage> skippedPassages = new();
        Dictionary<string, int> siteTokens = new();
        int usedTokens = 0;

        foreach (ScoredPassage passage in ordered)
        {
            siteTokens.TryGetValue(passage.Passage.Host, out int siteTokenCount);

            // Prevent token count from going over budget and prevent sites from going over their token limit
            if (usedTokens + passage.Tokens > budget || siteTokenCount + passage.Tokens > siteLimit)
            {
                skippedPassages.Add(passage);
                continue;
            }

            selectedPassages.Add(passage);
            usedTokens += passage.Tokens;
            siteTokens[passage.Passage.Host] = siteTokenCount + passage.Tokens;
        }

        // Fill any unused context space with previously skipped passages, even if it goes over site limit
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
    /// Counts how many unique sites there are in a list of passages.
    /// </summary>
    /// <param name="passages"> Collection of scored passages </param>
    /// <returns> Number of unique sites </returns>
    public static int CountSites(IEnumerable<ScoredPassage> passages)
    {
        return passages.Select(passage => passage.Passage.Host).Distinct().Count();
    }
    /// <summary>
    /// Gets the list of sources used to answer a prompt.
    /// </summary>
    /// <param name="passages"> Passages used to answer a prompt </param>
    /// <returns> List of distinct sources </returns>
    public static IReadOnlyList<Source> Sources(IEnumerable<ScoredPassage> passages)
    {
        return passages
            .OrderByDescending(passage => passage.Score)
            .Select(passage => new Source(passage.Passage.Title, passage.Passage.Url))
            .DistinctBy(source => source.Url)
            .ToList();
    }
}

/// <summary>
/// Passage with its score and total token count.
/// </summary>
/// <param name="Passage"> Scored passage </param>
/// <param name="Score"> Passage score </param>
/// <param name="Tokens"> Total token count for this passage </param>
public record ScoredPassage(Passage Passage, float Score, int Tokens);
/// <summary>
/// A single source used to answer a prompt.
/// </summary>
/// <param name="Title"> Page title </param>
/// <param name="Url"> Page URL </param>
public record Source(string Title, string Url);
