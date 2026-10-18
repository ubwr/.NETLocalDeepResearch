using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace LocalDeepResearch.Search;
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
    /// <exception cref="HttpRequestException"> Error during search </exception>
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken ct = default)
    {
        List<SearchResult> results = new();

        for (int page = 1; page <= _options.PageCount; page++)
        {
            // SearXNG expects a query string instead of a json request body
            string url = QueryHelpers.AddQueryString("/search", new Dictionary<string, string?>
            {
                ["q"] = query,
                ["format"] = "json",
                ["categories"] = _options.Categories,
                ["language"] = _options.Language,
                ["pageno"] = page.ToString()
            });

            SearchResponse? response;

            try
            {
                response = await _http.GetFromJsonAsync<SearchResponse>(url, Json, ct);
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                throw new HttpRequestException($"Timed out searching for {query}", ex);
            }
            catch (JsonException ex)
            {
                throw new HttpRequestException($"Invalid search response for {query}", ex);
            }

            if (response is null || response.Results.Count == 0)
                break;

            results.AddRange(response.Results);
        }

        return results
            .Where(result => !string.IsNullOrWhiteSpace(result.Url) && !string.IsNullOrWhiteSpace(result.Title))
            .DistinctBy(result => result.Url)
            .Take(_options.MaxResults)
            .ToList();
    }
    /// <summary>
    /// List of search results that we receive back from local SearXNG API.
    /// </summary>
    /// <param name="Results"> Search results </param>
    private record SearchResponse(IReadOnlyList<SearchResult> Results);
}
