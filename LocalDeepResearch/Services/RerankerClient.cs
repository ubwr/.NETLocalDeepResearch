using LMSupply;
using LMSupply.Reranker;
using Microsoft.Extensions.Options;

namespace LocalDeepResearch.Services;

/// <summary>
/// Client for interacting with our locally running reranker.
/// </summary>
public class RerankerClient
{
    private readonly RerankerOptions _options;
    private IRerankerModel? _rerankerModel;

    public RerankerClient(IOptions<RerankerOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// If our reranker model is loaded.
    /// </summary>
    public bool IsModelLoaded => _rerankerModel is not null;
    /// <summary>
    /// Threshold that we compare source scores to for filtering.
    /// </summary>
    public double Threshold => _options.Threshold;

    /// <summary>
    /// Does the initial loading of our local reranker.
    /// </summary>
    /// <param name="progress"> Current download progress for our reranker </param>
    /// <param name="ct"> Cancellation token </param>
    public async Task LoadAsync(IProgress<DownloadProgress>? progress = null, CancellationToken ct = default)
    {
        if (IsModelLoaded)
            return;

        _rerankerModel = await LocalReranker.LoadAsync(
            _options.ModelId,
            progress: progress,
            cancellationToken: ct);
    }
    /// <summary>
    /// Scores a set of sources by their relevance to a search query.
    /// Scores will be between 0 and 1.
    /// </summary>
    /// <param name="query"> Search query </param>
    /// <param name="documents"> Search results </param>
    /// <param name="ct"> Cancellation token </param>
    /// <returns> Array of scores for a set of sources </returns>
    /// <exception cref="InvalidOperationException"> Error when reranker is not loaded </exception>
    public async Task<float[]> ScoreAsync(
        string query,
        IEnumerable<string> documents,
        CancellationToken ct = default)
    {
        if (!IsModelLoaded)
            throw new InvalidOperationException("Reranker is not loaded.");

        return await _rerankerModel.ScoreAsync(query, documents, ct);
    }
}
