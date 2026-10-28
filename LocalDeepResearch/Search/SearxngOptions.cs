namespace LocalDeepResearch.Search;
/// <summary>
/// Options configuration for our local search engine.
/// </summary>
public class SearxngOptions
{
    public string Url { get; set; } = "http://127.0.0.1:8080";
    public string Categories { get; set; } = "general,science";
    public string Language { get; set; } = "en";
    public int MaxResults { get; set; } = 100;
    public int PageCount { get; set; } = 2;
    public int TimeoutSeconds { get; set; } = 15;
}
