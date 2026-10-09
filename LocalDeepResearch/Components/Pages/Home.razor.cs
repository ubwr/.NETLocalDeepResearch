using LMSupply;
using LocalDeepResearch.Resources;
using LocalDeepResearch.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace LocalDeepResearch.Components.Pages;

public partial class Home
{
    [Inject] private BonsaiClient Bonsai { get; set; }
    [Inject] private SearxngClient Searxng { get; set; }
    [Inject] private RerankerClient Reranker { get; set; }

    /// <summary>
    /// Prompt to the local LLM.
    /// </summary>
    private string? _prompt;
    /// <summary>
    /// Response from the local LLM.
    /// </summary>
    private string? _answer;
    /// <summary>
    /// Error message.
    /// </summary>
    private string? _error;
    /// <summary>
    /// If a prompt is already being processed.
    /// </summary>
    private bool _busy;
    /// <summary>
    /// Full list of search results.
    /// </summary>
    private IReadOnlyList<SearchResult> _results = [];
    /// <summary>
    /// Scores that correspond to the full list of search results.
    /// </summary>
    private float[] _scores = [];
    /// <summary>
    /// How many sources passed the reranker.
    /// </summary>
    private int _keptCount;
    /// <summary>
    /// If we are actively loading the model.
    /// </summary>
    private bool _loadingModel;
    /// <summary>
    /// Model download percentage.
    /// </summary>
    private int _downloadPercent;
    /// <summary>
    /// Description of the current loading status for the user.
    /// </summary>
    private string? _loadingStatus;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Exit early if our models are already downloaded
        if (!firstRender || (Bonsai.IsModelLoaded && Reranker.IsModelLoaded))
            return;

        _loadingModel = true;
        _loadingStatus = Strings.ModelPreparing;
        StateHasChanged();

        var progress = new Progress<DownloadProgress>(report =>
        {
            _downloadPercent = (int)report.OverallPercentComplete;
            _loadingStatus = $"{report.Phase} {report.FileName}";
            InvokeAsync(StateHasChanged);
        });

        try
        {
            await Bonsai.LoadAsync(progress);
            await Reranker.LoadAsync(progress);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _loadingModel = false;
            StateHasChanged();
        }
    }

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
    /// Sends our request with the user's prompt.
    /// </summary>
    private async Task AskAsync()
    {
        if (_busy || string.IsNullOrWhiteSpace(_prompt))
            return;

        _busy = true;
        _error = null;
        _answer = null;
        _results = [];
        _scores = [];
        _keptCount = 0;

        try
        {
            var query = _prompt.Trim();

            _results = await Searxng.SearchAsync(query);
            StateHasChanged();

            if (_results.Count > 0)
            {
                var documents = _results.Select(r => $"{r.Title} {r.Content}").ToList();
                _scores = await Reranker.ScoreAsync(query, documents);
                _keptCount = _scores.Count(score => score >= Reranker.Threshold);
                StateHasChanged();
            }

            _answer = await Bonsai.AskAsync(query);
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
