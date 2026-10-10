using System.Text;

namespace LocalDeepResearch.Services;

/// <summary>
/// Static class for prompting our local LLM.
/// </summary>
public static class PromptHelper
{
    /// <summary>
    /// Custom system message to include in all of our prompts.
    /// </summary>
    public const string SystemMessage =
        "You are a research writer. " +
        "Answer the user's question as a clear, well-organized report written in a professional tone. " +
        "Use only the information in the provided documents. " +
        "Do not mention the documents, sources, or missing information. " +
        "Do not include citations.";

    /// <summary>
    /// Formats passages by wrapping them in document tags to help the local LLM discern different passages.
    /// </summary>
    /// <param name="text"> Passage text </param>
    /// <returns> Passage text wrapped in document tags </returns>
    public static string Document(string text)
    {
        return $"<document>\n{text}\n</document>\n\n";
    }
    /// <summary>
    /// Creates a single user message string from a list of passages and a question.
    /// </summary>
    /// <param name="passages"> List of text passages </param>
    /// <param name="question"> Question for local LLM </param>
    /// <returns> User message as a single string </returns>
    public static string UserMessage(IReadOnlyList<ScoredPassage> passages, string question)
    {
        var builder = new StringBuilder();

        foreach (ScoredPassage passage in passages)
            builder.Append(Document(passage.Passage.Text));

        builder.Append(Question(question));
        return builder.ToString();
    }
    /// <summary>
    /// Prepends question prompt with a label. This function is used so we can get a more accurate token count.
    /// </summary>
    /// <param name="question"> Question in prompt </param>
    /// <returns> Prompt question with a label </returns>
    public static string Question(string question)
    {
        return $"Question: {question}";
    }
}
