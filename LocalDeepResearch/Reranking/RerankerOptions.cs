namespace LocalDeepResearch.Reranking;

/// <summary>
/// Options configuration for our local reranker.
/// </summary>
public class RerankerOptions
{
    public string ModelId { get; set; } = "default";
    public double PageThreshold { get; set; } = 0.5;
    public double PassageThreshold { get; set; } = 0.95;
}
