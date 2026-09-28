using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace LocalDeepResearch.Services;
/// <summary>
/// Client for interacting with our locally running search engine.
/// </summary>
public class SearxngClient
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient _http;
    private readonly SearxngOptions _options;

    public SearxngClient(HttpClient http, IOptions<SearxngOptions> options)
    {
        _http = http;
        _options = options.Value;
    }
    /// <summary>
    /// Executes search query using local SearXNG API.
    /// </summary>
    /// <param name="query"> Search query </param>
    /// <param name="ct"> Cancellation token </param>
    /// <returns> List of <see cref="SearchResult"/> objects that we get back from API </returns>
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken ct = default)
    {
        // SearXNG expects a query string instead of a json request body
        var url = QueryHelpers.AddQueryString("/search", new Dictionary<string, string?>
        {
            ["q"] = query,
            ["format"] = "json",
            ["categories"] = _options.Categories,
            ["language"] = _options.Language
        });

        var response = await _http.GetFromJsonAsync<SearchResponse>(url, Json, ct);

        return response?.Results
            .Where(result => !string.IsNullOrWhiteSpace(result.Url))
            .Take(_options.MaxResults)
            .ToList() ?? [];
    }
    /// <summary>
    /// List of search results that we receive back from local SearXNG API.
    /// </summary>
    /// <param name="Results"> Search results </param>
    private record SearchResponse(IReadOnlyList<SearchResult> Results);
}
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
