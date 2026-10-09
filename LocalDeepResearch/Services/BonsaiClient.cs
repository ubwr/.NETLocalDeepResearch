using LMSupply;
using LMSupply.Generator;
using LMSupply.Llama.Server;
using LMSupply.Generator.Abstractions;
using LMSupply.Generator.Models;
using Microsoft.Extensions.Options;

namespace LocalDeepResearch.Services;
/// <summary>
/// Client for interacting with our locally running LLM.
/// </summary>
public class BonsaiClient
{
    private readonly BonsaiOptions _options;
    private IGeneratorModel? _generatorModel;

    public BonsaiClient(IOptions<BonsaiOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// If our local LLM is loaded.
    /// </summary>
    public bool IsModelLoaded => _generatorModel is not null;
    /// <summary>
    /// If we are using a GPU for inference.
    /// </summary>
    public bool IsGpuActive => _generatorModel?.IsGpuActive ?? false;
    /// <summary>
    /// Result of the local LLM text generation.
    /// </summary>
    public GenerationResult? Result { get; private set; }

    /// <summary>
    /// Does the initial loading of our local LLM.
    /// </summary>
    /// <param name="progress"> Current download progress for our local LLM </param>
    /// <param name="ct"> Cancellation token </param>
    public async Task LoadAsync(IProgress<DownloadProgress>? progress = null, CancellationToken ct = default)
    {
        if (IsModelLoaded)
            return;
        
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
            _options.ModelId,
            generatorOptions,
            progress: progress,
            cancellationToken: ct);
    }
    /// <summary>
    /// Sends a request to our local LLM with a given prompt.
    /// </summary>
    /// <param name="prompt"> Prompt for our local LLM </param>
    /// <param name="ct"> Cancellation token </param>
    /// <returns> Response from model </returns>
    /// <exception cref="InvalidOperationException"> Error when LLM is not loaded </exception>
    public async Task<string> AskAsync(string prompt, CancellationToken ct = default)
    {
        if (!IsModelLoaded)
            throw new InvalidOperationException("Selected LLM is not loaded.");

        var result = await _generatorModel.GenerateChatCompleteResultAsync(
            [new ChatMessage { Role = ChatRole.User, Content = prompt }],
            new GenerationOptions
            {
                MaxTokens = _options.MaxTokens,
                Temperature = (float)_options.Temperature,
                FilterReasoningTokens = true
            },
            ct);

        Result = result;

        return result.Content;
    }
}
