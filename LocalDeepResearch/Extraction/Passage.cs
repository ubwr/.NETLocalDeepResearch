namespace LocalDeepResearch.Extraction;

/// <summary>
/// A chunk of text that gets passed into a reranker for relevance scoring.
/// </summary>
/// <param name="Url"> URL that contains this passage </param>
/// <param name="Title"> Title of the page that contains this passage</param>
/// <param name="Text"> Text content from the page </param>
public record Passage(string Url, string Title, string Text)
{
    public string Host => new Uri(Url).Host;
}
