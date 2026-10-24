using System.Text.Json;
using LMSupply;
using LocalDeepResearch.Resources;
using LocalDeepResearch.Chat;
using LocalDeepResearch.Export;
using LocalDeepResearch.Extraction;
using LocalDeepResearch.Reranking;
using LocalDeepResearch.Research;
using LocalDeepResearch.Search;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace LocalDeepResearch.Components.Pages;

public partial class Home : IDisposable
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
    /// Minimum number of unique sites required for a search query to be valid.
    /// </summary>
    private int _minSites = 2;
    /// <summary>
    /// Number of additional text sections to include in output after historical context.
    /// </summary>
    private int _sectionCount = 4;
    /// <summary>
    /// Maximum text length in chars a single source can be.
    /// </summary>
    private const int MaxSourceLengthChars = 100;

    private static readonly JsonSerializerOptions ModelJson = new(JsonSerializerDefaults.Web);
    
    private CancellationTokenSource? _cancellation;

    [Inject] private ChatClient Chat { get; set; }
    [Inject] private SearxngClient Searxng { get; set; }
    [Inject] private RerankerClient Reranker { get; set; }
    [Inject] private PageClient Pages { get; set; }
    [Inject] private ResearchLock ResearchLock { get; set; }
    [Inject] private IJSRuntime JS { get; set; }

    /// <summary>
    /// Prompt to the local LLM.
    /// </summary>
    private string? _prompt;
    /// <summary>
    /// List of completed research sections.
    /// </summary>
    private List<Section> _sections = new();
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
    /// The full set of subquestions to prompt our LLM.
    /// </summary>
    private ResearchPlan? _plan;
    /// <summary>
    /// Minimum relevance score (0 to 1) a search result needs for its page to get fetched.
    /// </summary>
    private double _pageThreshold;
    /// <summary>
    /// Minimum relevance score (0 to 1) a text passage needs for it to be used as context.
    /// </summary>
    private double _passageThreshold;
    /// <summary>
    /// Information message.
    /// </summary>
    private string? _notice;
    /// <summary>
    /// List of skipped research sections.
    /// </summary>
    private List<string> _skippedSections = new();
    /// <summary>
    /// List of research sections that received errors during search.
    /// </summary>
    private List<string> _failedSections = new();
    /// <summary>
    /// Summary of our research report.
    /// </summary>
    private string? _summary;

    protected override void OnInitialized()
    {
        _pageThreshold = Reranker.PageThreshold;
        _passageThreshold = Reranker.PassageThreshold;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Exit early if our models are already downloaded
        if (!firstRender || (Chat.IsModelLoaded && Reranker.IsModelLoaded))
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
            await Chat.LoadAsync(Chat.ModelId, progress);
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
    /// Sends our research request with the user's prompt.
    /// </summary>
    private async Task AskAsync()
    {
        if (_busy || string.IsNullOrWhiteSpace(_prompt))
            return;

        // Try to claim the research lock
        if (!ResearchLock.TryAcquire())
        {
            _error = null;
            _notice = Strings.RunInProgress;
            return;
        }

        _busy = true;
        _error = null;
        _notice = null;
        _sections = new();
        _results = [];
        _scores = [];
        _keptCount = 0;
        _runStatus = null;
        _plan = null;
        _summary = null;
        Chat.ResetTokenTotals();
        _skippedSections = new();
        _failedSections = new();
        _cancellation = new CancellationTokenSource();
        CancellationToken ct = _cancellation.Token;

        try
        {
            string query = _prompt.Trim();

            _runStatus = Strings.StatusPlanning;
            StateHasChanged();

            // Get our list of subquestions
            string planJson = await Chat.AskAsync(PromptHelper.PlannerSystemMessage(_sectionCount), query, PromptHelper.PlanSchema(_sectionCount), ct);
            _plan = JsonSerializer.Deserialize<ResearchPlan>(planJson, ModelJson)
                ?? throw new InvalidOperationException("Failed to create a research plan.");
            StateHasChanged();

            List<SubQuestion> subQuestions = new() { _plan.History };
            subQuestions.AddRange(_plan.SubQuestions);

            foreach (SubQuestion subQuestion in subQuestions)
            {
                _runStatus = string.Format(Strings.StatusResearching, subQuestion.Heading);
                StateHasChanged();

                List<string> otherHeadings = subQuestions
                    .Where(other => other != subQuestion)
                    .Select(other => other.Heading)
                    .ToList();

                Section? section = await ResearchAsync(subQuestion, otherHeadings, ct);

                if (section is null)
                    continue;

                _sections.Add(section);
                StateHasChanged();
            }

            if (_sections.Count == 0)
            {
                _error = Strings.NotEnoughSites;
                return;
            }

            _runStatus = Strings.StatusSummary;
            StateHasChanged();

            string reportSummaryMessage = PromptHelper.ReportSummaryMessage(_sections, _plan.Title);

            _summary = await Chat.AskAsync(PromptHelper.SummarySystemMessage, reportSummaryMessage, ct: ct);
        }
        catch (OperationCanceledException)
        {
            _notice = Strings.RunCancelled;
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _cancellation.Dispose();
            _cancellation = null;
            ResearchLock.Release();
            _busy = false;
        }
    }
    /// <summary>
    /// Downloads the completed research report PDF.
    /// </summary>
    private async Task DownloadPdfAsync()
    {
        if (_plan is null || _summary is null)
            return;

        byte[] pdf = ReportPdf.Create(_plan.Title, _summary, _sections);

        using var stream = new MemoryStream(pdf);
        using var streamReference = new DotNetStreamReference(stream);

        await JS.InvokeVoidAsync("downloadFileFromStream", $"{_plan.Title}.pdf", streamReference);
    }
    /// <summary>
    /// Cancels the user's research request.
    /// </summary>
    private void CancelRun()
    {
        _cancellation?.Cancel();
    }
    public void Dispose()
    {
        CancelRun();
    }
    /// <summary>
    /// Sends a research question to our local LLM for a single section.
    /// </summary>
    /// <param name="subQuestion"> Subquestion to research </param>
    /// <param name="otherHeadings"> Other section headings so that output information is not duplicated </param>
    /// <param name="ct"> Cancellation token </param>
    /// <returns> Completed research section </returns>
    private async Task<Section?> ResearchAsync(SubQuestion subQuestion, IReadOnlyList<string> otherHeadings, CancellationToken ct)
    {
        string question = subQuestion.Question;

        // Get our token count before adding any passages
        int fixedTokens = await Chat.CountPromptTokensAsync(PromptHelper.SystemMessage, PromptHelper.SectionRequest(question, otherHeadings), ct);
        int passageTokenBudget = Chat.PromptTokenBudget - fixedTokens;

        try
        {
            _results = await Searxng.SearchAsync(question, ct);
        }
        catch (HttpRequestException)
        {
            _failedSections.Add(subQuestion.Heading);
            return null;
        }

        StateHasChanged();

        List<SearchResult> relevantResults = [];

        // Score and filter our search results
        if (_results.Count > 0)
        {
            List<string> documents = _results.Select(r => $"{r.Title} {r.Content}").ToList();
            _scores = await Reranker.ScoreAsync(question, documents, ct);

            // Get relevant sources via filtering with index
            relevantResults = Enumerable.Range(0, _results.Count)
                .Where(i => Helpers.ToPercent(_scores[i]) >= Helpers.ToPercent(_pageThreshold))
                .OrderByDescending(i => _scores[i])
                .Select(i => _results[i])
                .ToList();

            _keptCount = relevantResults.Count;
            StateHasChanged();
        }

        List<ScoredPassage> relevantPassages = await GatherPassagesAsync(question, relevantResults, passageTokenBudget, ct);

        if (ContextBuilder.CountSites(relevantPassages) < _minSites)
        {
            _skippedSections.Add(subQuestion.Heading);
            return null;
        }

        IReadOnlyList<ScoredPassage> selected = ContextBuilder.Select(relevantPassages, passageTokenBudget);
        _runStatus = string.Format(Strings.StatusWriting, subQuestion.Heading);
        StateHasChanged();

        string userMessage = PromptHelper.UserMessage(selected, question, otherHeadings);
        string text = await Chat.AskAsync(PromptHelper.SystemMessage, userMessage, ct: ct);

        return new Section(subQuestion.Heading, text, ContextBuilder.Sources(selected));
    }
    /// <summary>
    /// Gets text passages from a list of relevant search results.
    /// </summary>
    /// <param name="query"> Search query </param>
    /// <param name="candidates"> Relevant search results </param>
    /// <param name="passageTokenBudget"> Maximum number of tokens our passages can use as context </param>
    /// <param name="ct"> Cancellation token </param>
    /// <returns> List of relevant text passages </returns>
    private async Task<List<ScoredPassage>> GatherPassagesAsync(string query, IReadOnlyList<SearchResult> candidates, int passageTokenBudget, CancellationToken ct)
    {
        List<ScoredPassage> relevantPassages = new();
        HashSet<string> seenTexts = new();
        int relevantPassageTokens = 0;
        int fetchedPageCount = 0;

        for (int start = 0; start < candidates.Count; start += BatchSize)
        {
            // Stop fetching pages if we've already hit our requirements
            if (fetchedPageCount >= MinPages && relevantPassageTokens >= passageTokenBudget)
                break;

            List<SearchResult> batch = candidates.Skip(start).Take(BatchSize).ToList();
            List<Task<IReadOnlyList<Passage>>> fetchTasks = new();

            foreach (SearchResult result in batch)
                fetchTasks.Add(FetchPassagesAsync(result, ct));
            
            IReadOnlyList<Passage>[] pages = await Task.WhenAll(fetchTasks);            
            fetchedPageCount += batch.Count;
            List<Passage> passages = new();

            // Flatten our list of passages and ignore duplicate passages
            foreach (Passage passage in pages.SelectMany(page => page))
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
                float[] scores = await Reranker.ScoreAsync(query, passages.Select(passage => passage.Text), ct);

                for (int i = 0; i < passages.Count; i++)
                {
                    if (scores[i] < _passageThreshold)
                        continue;

                    int tokens = await Chat.CountTokensAsync(PromptHelper.Document(passages[i].Text), ct);
                    relevantPassages.Add(new ScoredPassage(passages[i], scores[i], tokens));
                    relevantPassageTokens += tokens;
                }
            }
        }

        return relevantPassages;
    }
    /// <summary>
    /// Gets a list of text passages from a single search result.
    /// </summary>
    /// <param name="result"> Single search result </param>
    /// <param name="ct"> Cancellation token </param>
    /// <returns> List of text passages </returns>
    private async Task<IReadOnlyList<Passage>> FetchPassagesAsync(SearchResult result, CancellationToken ct)
    {
        try
        {
            string html = await Pages.ExtractAsync(result.Url, ct);
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
