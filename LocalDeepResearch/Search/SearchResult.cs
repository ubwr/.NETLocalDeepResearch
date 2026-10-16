namespace LocalDeepResearch.Search;

/// <summary>
/// Single search result that we receive back from local SearXNG API. 
/// </summary>
/// <param name="Url"> Source URL </param>
/// <param name="Title"> Page title </param>
/// <param name="Content"> Truncated snippet of the page's text content </param>
/// <param name="Engine"> Search engine used </param>
/// <param name="Category"> Search category that this result came from (general, science) </param>
public record SearchResult(
    string Url,
    string Title,
    string? Content,
    string? Engine,
    string? Category);
