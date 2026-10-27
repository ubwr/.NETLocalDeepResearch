using LMSupply;
using LMSupply.Generator;
using LMSupply.Llama.Server;
using LMSupply.Generator.Abstractions;
using LMSupply.Generator.Models;
using Microsoft.Extensions.Options;

namespace LocalDeepResearch.Chat;
/// <summary>
/// Client for interacting with our locally running LLM.
/// </summary>
public class ChatClient
{
    private readonly ChatOptions _options;
    private IGeneratorModel? _generatorModel;

    public ChatClient(IOptions<ChatOptions> options)
    {
        _options = options.Value;
        ModelId = _options.ModelId;
    }
    
    /// <summary>
    /// Id of local model to use for generation.
    /// </summary>
    public string ModelId { get; private set; }
    /// <summary>
    /// Total text generation tokens used in a single run.
    /// </summary>
    public int TotalTokens { get; private set; }
    /// <summary>
    /// Total text generation time for a single run.
    /// </summary>
    public TimeSpan TotalGenerationTime { get; private set; }

    /// <summary>
    /// If our local LLM is loaded.
    /// </summary>
    public bool IsModelLoaded => _generatorModel is not null;
    /// <summary>
    /// If we are using a GPU for inference.
    /// </summary>
    public bool IsGpuActive => _generatorModel?.IsGpuActive ?? false;
    /// <summary>
    /// Maximum number of tokens we can use as context for a prompt.
    /// </summary>
    public int PromptTokenBudget => (_generatorModel?.MaxContextLength ?? 0) - _options.MaxOutputTokens;

    /// <summary>
    /// Loads a local LLM.
    /// </summary>
    /// <param name="modelId"> Id of local model to use for generation </param>
    /// <param name="progress"> Current download progress for a local LLM </param>
    /// <param name="ct"> Cancellation token </param>
    public async Task LoadAsync(string modelId, IProgress<DownloadProgress>? progress = null, CancellationToken ct = default)
    {
        if (IsModelLoaded && modelId == ModelId)
            return;

        // Clear previously loaded model
        if (_generatorModel is not null)
        {
            await _generatorModel.DisposeAsync();
            _generatorModel = null;
        }

        // Allow custom context lengths
        var generatorOptions = new GeneratorOptions
        {
            MaxContextLength = _options.MaxContextLength
        };

        // Allow custom locations for llama server installations
        if (!string.IsNullOrWhiteSpace(_options.ServerBinaryPath))
        {
            generatorOptions.ServerUpdateOptions = new LlamaServerUpdateOptions
            {
                ServerBinaryPath = _options.ServerBinaryPath
            };
        }

        _generatorModel = await LocalGenerator.LoadAsync(
            modelId,
            generatorOptions,
            progress: progress,
            cancellationToken: ct);
        
        ModelId = modelId;
    }
    /// <summary>
    /// Sends a request to our local LLM with a given prompt.
    /// </summary>
    /// <param name="systemMessage"> System message for our local LLM </param>
    /// <param name="userMessage"> Full user prompt for our local LLM </param>
    /// <param name="jsonSchema"> Json schema to request for our text generation response </param>
    /// <param name="ct"> Cancellation token </param>
    /// <returns> Response from model </returns>
    /// <exception cref="InvalidOperationException"> Error when LLM is not loaded </exception>
    public async Task<string> AskAsync(string systemMessage, string userMessage, string? jsonSchema = null, CancellationToken ct = default)
    {
        if (!IsModelLoaded)
            throw new InvalidOperationException("Selected LLM is not loaded.");

        var result = await _generatorModel.GenerateChatCompleteResultAsync(
            CreateMessages(systemMessage, userMessage),
            CreateGenerationOptions(jsonSchema),
            ct);

        TotalTokens += result.Usage.CompletionTokens;
        TotalGenerationTime += result.Timings?.CompletionDuration ?? TimeSpan.Zero;

        return result.Content;
    }
    /// <summary>
    /// Counts total tokens from a prompt and its generation options.
    /// </summary>
    /// <param name="systemMessage"> System message for our local LLM </param>
    /// <param name="userMessage"> Full user prompt for our local LLM </param>
    /// <param name="ct"> Cancellation token </param>
    /// <returns> Token count </returns>
    /// <exception cref="InvalidOperationException"> Error when LLM is not loaded </exception>
    public async Task<int> CountPromptTokensAsync(string systemMessage, string userMessage, CancellationToken ct = default)
    {
        if (!IsModelLoaded)
            throw new InvalidOperationException("Selected LLM is not loaded.");

        return await _generatorModel.CountTokensAsync(
            CreateMessages(systemMessage, userMessage),
            CreateGenerationOptions(),
            ct);
    }
    /// <summary>
    /// Resets the total token count and text generation time.
    /// </summary>
    public void ResetTokenTotals()
    {
        TotalTokens = 0;
        TotalGenerationTime = TimeSpan.Zero;
    }
    /// <summary>
    /// Creates total prompt for our local LLM.
    /// </summary>
    /// <param name="systemMessage"> System message for our local LLM </param>
    /// <param name="userMessage"> Full user prompt for our local LLM </param>
    /// <returns> List of messages </returns>
    private static List<ChatMessage> CreateMessages(string systemMessage, string userMessage)
    {
        return
        [
            new ChatMessage { Role = ChatRole.System, Content = systemMessage },
            new ChatMessage { Role = ChatRole.User, Content = userMessage }
        ];
    }
    /// <summary>
    /// Creates a <see cref="GenerationOptions"/> object with our custom configuration.
    /// </summary>
    /// <param name="jsonSchema"> Json schema to request for our text generation response </param>
    /// <returns> Generation configuration </returns>
    private GenerationOptions CreateGenerationOptions(string? jsonSchema = null)
    {
        return new GenerationOptions
        {
            MaxTokens = _options.MaxOutputTokens,
            Temperature = (float)_options.Temperature,
            FilterReasoningTokens = true,
            JsonSchema = jsonSchema
        };
    }
    /// <summary>
    /// Counts the exact number of tokens in the given text using the model's tokenizer.
    /// </summary>
    /// <param name="text"> Text content </param>
    /// <param name="ct"> Cancellation token </param>
    /// <returns> Token count </returns>
    /// <exception cref="InvalidOperationException"> Error when LLM is not loaded </exception>
    public async Task<int> CountTokensAsync(string text, CancellationToken ct = default)
    {
        if (!IsModelLoaded)
            throw new InvalidOperationException("Selected LLM is not loaded.");

        return await _generatorModel.CountTokensAsync(text, ct);
    }
}
