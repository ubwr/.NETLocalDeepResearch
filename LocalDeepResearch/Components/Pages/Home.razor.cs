using LMSupply;
using LocalDeepResearch.Resources;
using LocalDeepResearch.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace LocalDeepResearch.Components.Pages;

public partial class Home
{
    /// <summary>
    /// How many pages to fetch at once.
    /// </summary>
    private const int BatchSize = 4;
    /// <summary>
    /// Minimum number of pages to fetch before the fetch loop can stop early.
    /// </summary>
    private const int MinPages = 10;
    /// <summary>
    /// Minimum number of unique sources required for a search query to be valid.
    /// </summary>
    private const int MinSources = 2;
    /// <summary>
    /// Additional padding for the input context to account for original prompt, system message, etc.
    /// </summary>
    private const int PromptReserve = 200;

    [Inject] private BonsaiClient Bonsai { get; set; }
    [Inject] private SearxngClient Searxng { get; set; }
    [Inject] private RerankerClient Reranker { get; set; }
    [Inject] private PageClient Pages { get; set; }

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
    /// <summary>
    /// Current status on our source and passage retrieval and scoring.
    /// </summary>
    private string? _runStatus;
    /// <summary>
    /// Text passages that are relevant to the original prompt and fit within the token budget.
    /// </summary>
    private IReadOnlyList<ScoredPassage> _selected = [];
    /// <summary>
    /// Maximum number of tokens we can use as context for a prompt.
    /// </summary>
    private int PromptBudget => Bonsai.PromptBudget - PromptReserve;

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
        _runStatus = null;
        _selected = [];

        try
        {
            var query = _prompt.Trim();

            _results = await Searxng.SearchAsync(query);
            StateHasChanged();

            List<SearchResult> relevantResults = [];

            // Score and filter our search results
            if (_results.Count > 0)
            {
                var documents = _results.Select(r => $"{r.Title} {r.Content}").ToList();
                _scores = await Reranker.ScoreAsync(query, documents);

                // Get relevant sources via filtering with index
                relevantResults = Enumerable.Range(0, _results.Count)
                    .Where(i => _scores[i] >= Reranker.Threshold)
                    .OrderByDescending(i => _scores[i])
                    .Select(i => _results[i])
                    .ToList();

                _keptCount = relevantResults.Count;
                StateHasChanged();
            }

            var relevantPassages = await GatherPassagesAsync(query, relevantResults);

            if (ContextBuilder.CountSources(relevantPassages) < MinSources)
            {
                _error = Strings.NotEnoughSources;
                return;
            }

            _selected = ContextBuilder.Select(relevantPassages, PromptBudget);
            _runStatus = string.Format(Strings.StatusSelected, _selected.Count, relevantPassages.Count);
            StateHasChanged();

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
    /// <summary>
    /// Gets text passages from a list of relevant search results.
    /// </summary>
    /// <param name="query"> Search query </param>
    /// <param name="candidates"> Relevant search results </param>
    /// <returns> List of relevant text passages </returns>
    private async Task<List<ScoredPassage>> GatherPassagesAsync(string query, IReadOnlyList<SearchResult> candidates)
    {
        var relevantSources = new List<ScoredPassage>();
        var seenTexts = new HashSet<string>();
        var relevantSourceTokens = 0;
        var fetchedPageCount = 0;

        for (var start = 0; start < candidates.Count; start += BatchSize)
        {
            // Stop fetching pages if we've already hit our requirements
            if (fetchedPageCount >= MinPages && relevantSourceTokens >= PromptBudget)
                break;

            var batch = candidates.Skip(start).Take(BatchSize).ToList();
            var fetchTasks = new List<Task<IReadOnlyList<Passage>>>();

            foreach (var result in batch)
                fetchTasks.Add(FetchPassagesAsync(result));
            
            var pages = await Task.WhenAll(fetchTasks);            
            fetchedPageCount += batch.Count;
            var passages = new List<Passage>();

            // Flatten our list of passages and ignore duplicate passages
            foreach (var passage in pages.SelectMany(page => page))
            {
                if (seenTexts.Contains(passage.Text))
                    continue;

                seenTexts.Add(passage.Text);
                passages.Add(passage);
            }

            // Scores passages and adds relevant passages to a list
            if (passages.Count > 0)
            {
                // Score passages for relevance
                var scores = await Reranker.ScoreAsync(query, passages.Select(passage => passage.Text));

                for (var i = 0; i < passages.Count; i++)
                {
                    if (scores[i] < Reranker.Threshold)
                        continue;

                    var tokens = await Bonsai.CountTokensAsync(passages[i].Text);
                    relevantSources.Add(new ScoredPassage(passages[i], scores[i], tokens));
                    relevantSourceTokens += tokens;
                }
            }

            _runStatus = string.Format(Strings.StatusGathering, fetchedPageCount, candidates.Count, relevantSources.Count);
            StateHasChanged();
        }

        return relevantSources;
    }
    /// <summary>
    /// Gets a list of text passages from a single search result.
    /// </summary>
    /// <param name="result"> Single search result </param>
    /// <returns> List of text passages </returns>
    private async Task<IReadOnlyList<Passage>> FetchPassagesAsync(SearchResult result)
    {
        try
        {
            var html = await Pages.ExtractAsync(result.Url);
            return Chunker.Chunk(html, result.Title, result.Url);
        }
        catch (HttpRequestException)
        {
            return [];
        }
        catch (InvalidOperationException)
        {
            return [];
        }
    }
}
