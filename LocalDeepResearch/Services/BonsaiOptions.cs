namespace LocalDeepResearch.Services;

/// <summary>
/// Options configuration for our local LLM.
/// </summary>
public class BonsaiOptions
{
    public string Url { get; set; } = "http://localhost:8081";
    public string ReasoningEffort { get; set; } = "medium";
    public int MaxTokens { get; set; } = 2000;
    public double Temperature { get; set; } = 0.3;
    public int TimeoutMinutes { get; set; } = 10;
}
