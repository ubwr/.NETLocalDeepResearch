using Trafilatura;

namespace LocalDeepResearch.Services;

/// <summary>
/// Client for extracting text content from webpages.
/// </summary>
public class PageClient
{
    private readonly HttpClient _http;

    public PageClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Extracts text content from a URL.
    /// </summary>
    /// <param name="url"> URL that we are extracting text content from </param>
    /// <param name="ct"> Cancellation token </param>
    /// <returns> Text content from a webpage </returns>
    /// <exception cref="HttpRequestException"> Error from webpage </exception>
    /// <exception cref="InvalidOperationException"> Error getting content from this page </exception>
    /// <exception cref="TaskCanceledException"> Request was cancelled </exception>
    public async Task<string> ExtractAsync(string url, CancellationToken ct = default)
    {
        string html;

        try
        {
            using var response = await _http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to fetch {url}: {(int)response.StatusCode} {response.ReasonPhrase}");

            html = await response.Content.ReadAsStringAsync(ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new HttpRequestException($"Timed out fetching {url}", ex);
        }

        try
        {
            var options = Extractor.DefaultOptions() with
            {
                originalUrl = url,      // Provide the source URL (improves metadata extraction)
                enableFallback = true,  // Enable readability fallback for difficult pages
                excludeTables = true    // Excludes table elements from result
            };
            return Extractor.Extract(html, options).contentHtml;
        }
        catch (TrafilaturaException ex)
        {
            throw new InvalidOperationException($"Failed to extract content from {url}: {ex.Message}", ex);
        }
    }
}
