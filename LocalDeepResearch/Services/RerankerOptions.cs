namespace LocalDeepResearch.Services;

/// <summary>
/// Options configuration for our local reranker.
/// </summary>
public class RerankerOptions
{
    public string ModelId { get; set; } = "default";
    public double Threshold { get; set; } = 0.95;
}
