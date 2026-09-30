using LocalDeepResearch.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace LocalDeepResearch.Components.Pages;

public partial class Home
{
    [Inject] private BonsaiClient Bonsai { get; set; }
    [Inject] private SearxngClient Searxng { get; set; }

    private string? _question;
    private string? _answer;
    private string? _error;
    private bool _busy;
    /// <summary>
    /// Full list of search results.
    /// </summary>
    private IReadOnlyList<SearchResult> _results = [];

    /// <summary>
    /// When the user hits 'Enter', fire off the API call to use our local models.
    /// </summary>
    /// <param name="args"> Keyboard event </param>
    private async Task OnKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Enter" && !args.ShiftKey)
            await AskAsync();
    }
    /// <summary>
    /// Sends our API request with the user's prompt.
    /// </summary>
    private async Task AskAsync()
    {
        if (_busy || string.IsNullOrWhiteSpace(_question))
            return;

        _busy = true;
        _error = null;
        _answer = null;
        _results = [];

        try
        {
            _results = await Searxng.SearchAsync(_question.Trim());
            StateHasChanged();

            _answer = await Bonsai.AskAsync(_question.Trim());
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }
}
