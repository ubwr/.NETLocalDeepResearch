using System.Text;

namespace LocalDeepResearch.Services;

/// <summary>
/// Static class for prompting our local LLM.
/// </summary>
public static class PromptHelper
{
    /// <summary>
    /// Custom system message to include in our research prompts.
    /// </summary>
    public const string SystemMessage =
        "You are a research writer. " +
        "Write one section of a larger report as a single, detailed paragraph that answers only the given question. " +
        "Other sections cover related topics, so stay on this question. " +
        "Write in plain prose with a professional tone, without headings, lists, bullet points, or other formatting. " +
        "Use only the information in the provided documents. " +
        "Do not mention the documents, sources, or missing information. " +
        "Do not include citations. " +
        "Do not end with a conclusion or summary.";
    
    /// <summary>
    /// Custom system message to include when we need to generate a research plan.
    /// </summary>
    public const string PlannerSystemMessage =
        "You are a research planner. " +
        "Write one subquestion about the history of the topic. " +
        "Then break the user's research question into 3 to 5 subquestions that together fully answer it, " +
        "ordered so a report built from them reads naturally, from background to specifics. " +
        "Do not include any subquestions about history, since history is handled separately. " +
        "For each subquestion, give a short section heading and a short, concise, self-contained question. " +
        "Do not include a summary or introduction.";

    /// <summary>
    /// JSON schema to use for our research plan.
    /// </summary>
    public const string PlanSchema = """
        {
          "type": "object",
          "properties": {
            "history": {
              "type": "object",
              "properties": {
                "heading": { "type": "string" },
                "question": { "type": "string" }
              },
              "required": ["heading", "question"],
              "additionalProperties": false
            },
            "subQuestions": {
              "type": "array",
              "minItems": 3,
              "maxItems": 5,
              "items": {
                "type": "object",
                "properties": {
                  "heading": { "type": "string" },
                  "question": { "type": "string" }
                },
                "required": ["heading", "question"],
                "additionalProperties": false
              }
            }
          },
          "required": ["history", "subQuestions"],
          "additionalProperties": false
        }
        """;

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
