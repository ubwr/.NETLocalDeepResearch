namespace LocalDeepResearch.Services;

/// <summary>
/// Options configuration for our local LLM.
/// </summary>
public class ChatOptions
{
    public string ModelId { get; set; } = "Qwen/Qwen3-8B-GGUF";
    public string? ServerBinaryPath { get; set; }
    public int? MaxContextLength { get; set; }
    public int MaxOutputTokens { get; set; } = 2000;
    public double Temperature { get; set; } = 0.3;
}
