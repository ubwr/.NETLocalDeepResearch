using System.Net.Http.Json;
using System.Text.Json;

namespace LocalDeepResearch.Services;
/// <summary>
/// Client for interacting with our locally running LLM.
/// </summary>
public class BonsaiClient
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient _http;

    public BonsaiClient(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri("http://localhost:8081");
        _http.Timeout = TimeSpan.FromMinutes(10);
    }
    /// <summary>
    /// Sends our <see cref="CompletionRequest"/> to our local API and returns a <see cref="ResponseMessage"/>.
    /// </summary>
    /// <param name="question"> Research question being asked </param>
    /// <param name="ct"> Cancellation token </param>
    /// <returns> The response message from a choice in our <see cref="CompletionResponse"/> </returns>
    public async Task<string> AskAsync(string question, CancellationToken ct = default)
    {
        var request = new CompletionRequest(
            Messages: [new RequestMessage("user", question)],
            ReasoningEffort: "medium",
            MaxTokens: 2000,
            Temperature: 0.3);

        using var response = await _http.PostAsJsonAsync("/v1/chat/completions", request, Json, ct);
        response.EnsureSuccessStatusCode();

        var completion = await response.Content.ReadFromJsonAsync<CompletionResponse>(Json, ct);
        return completion?.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;
    }
    /// <summary>
    /// The message that we send in our <see cref="CompletionRequest"/>.
    /// </summary>
    /// <param name="Role"> Role for this message (system, user, assistant) </param>
    /// <param name="Content"> Message content </param>
    private record RequestMessage(string Role, string Content);
    /// <summary>
    /// The request that we send to our local API.
    /// </summary>
    /// <param name="Messages"> The messages we're sending to the model </param>
    /// <param name="ReasoningEffort"> Model effort (low, medium, xhigh) </param>
    /// <param name="MaxTokens"> Maximum number of tokens we want back in the response, including reasoning </param>
    /// <param name="Temperature"> How creative the model can be in its response </param>
    /// <param name="N"> Number of choices we get back in the completed response </param>
    private record CompletionRequest(
        IReadOnlyList<RequestMessage> Messages,
        string ReasoningEffort,
        int MaxTokens,
        double Temperature,
        int N = 1);
    /// <summary>
    /// List of choices that we get back in response to our prompt.
    /// Number of choices we want in the response is specified when calling <see cref="CompletionRequest"/>
    /// </summary>
    /// <param name="Choices"> List of choices in the completed response </param>
    private record CompletionResponse(IReadOnlyList<Choice> Choices);
    /// <summary>
    /// A single completion for this prompt. 
    /// </summary>
    /// <param name="Message"> The completed response </param>
    /// <param name="FinishReason"> The reason why the model finished </param>
    private record Choice(ResponseMessage Message, string FinishReason);
    /// <summary>
    /// A single response message that we get back from calls to our local LLM.
    /// </summary>
    /// <param name="Content"> Completed message to our original prompt </param>
    /// <param name="ReasoningContent"> Thought process that lead to the completed message </param>
    private record ResponseMessage(string Content, string? ReasoningContent);
}
