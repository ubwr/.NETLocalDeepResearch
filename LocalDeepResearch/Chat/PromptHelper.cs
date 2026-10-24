using System.Text;
using LocalDeepResearch.Research;

namespace LocalDeepResearch.Chat;

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
        "Do not cover the topics of the other sections listed, since they are covered separately. " +
        "Write in plain prose with a professional tone, without headings, lists, bullet points, or other formatting. " +
        "Use only the information in the provided documents. " +
        "Do not mention the documents, sources, or missing information. " +
        "Do not include citations. " +
        "Do not end with a conclusion or summary.";
    
    /// <summary>
    /// Custom system message to include when we need to generate a research plan.
    /// </summary>
    /// <param name="sectionCount"> Number of additional text sections to include in output after historical context </param>
    public static string PlannerSystemMessage(int sectionCount)
    {
        return "You are a research planner. " +
            "Write a short title for the report. " +
            "Write one subquestion about the history of the topic. " +
            $"Then break the user's research question into exactly {sectionCount} subquestions that together fully answer it, " +
            "ordered so a report built from them reads naturally, from background to specifics. " +
            "Do not include any subquestions about history, since history is handled separately. " +
            "For each subquestion, give a short section heading and a short, concise, self-contained question. " +
            "Do not include a summary or introduction.";
    }

    /// <summary>
    /// JSON schema to use for our research plan.
    /// </summary>
    /// <param name="sectionCount"> Number of additional text sections to include in output after historical context </param>
    public static string PlanSchema(int sectionCount)
    {
        return $$"""
        {
          "type": "object",
          "properties": {
            "title": { "type": "string" },
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
              "minItems": {{sectionCount}},
              "maxItems": {{sectionCount}},
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
          "required": ["title", "history", "subQuestions"],
          "additionalProperties": false
        }
        """;
    }

    /// <summary>
    /// Custom system message to include when we need to generate a report summary.
    /// </summary>
    public const string SummarySystemMessage =
        "You are a research writer. " +
        "Write the executive summary of a report as a single paragraph that gives a high-level overview of the report's topic, " +
        "based on the provided report sections. " +
        "Write in plain prose with a professional tone, without headings, lists, bullet points, or other formatting. " +
        "Use only the information in the sections. " +
        "Do not mention the sections, sources, or missing information. " +
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
    /// Creates a single query string to get a report summary from a list of text sections and a report title.
    /// </summary>
    /// <param name="sections"> Text sections used to get report summary </param>
    /// <param name="title"> Report title; used as additional context </param>
    /// <returns> Report summary query as a single string </returns>
    public static string ReportSummaryMessage(IReadOnlyList<Section> sections, string title)
    {
        var builder = new StringBuilder();

        foreach (Section section in sections)
            builder.Append($"<section>\n{section.Heading}\n{section.Text}\n</section>\n\n");

        builder.Append($"Report title: {title}");
        return builder.ToString();
    }
    /// <summary>
    /// Creates a single query string from a list of passages and a question.
    /// </summary>
    /// <param name="passages"> List of text passages </param>
    /// <param name="question"> Question for local LLM </param>
    /// <param name="otherHeadings"> Other section headings so that output information is not duplicated </param>
    /// <returns> User query as a single string </returns>
    public static string UserMessage(IReadOnlyList<ScoredPassage> passages, string question, IReadOnlyList<string> otherHeadings)
    {
        var builder = new StringBuilder();

        foreach (ScoredPassage passage in passages)
            builder.Append(Document(passage.Passage.Text));

        builder.Append(SectionRequest(question, otherHeadings));
        return builder.ToString();
    }
    /// <summary>
    /// Section specific information for this single request.
    /// Contains other section headers (so that this specific response ignores them) and the research question.
    /// </summary>
    /// <param name="question"> Research question </param>
    /// <param name="otherHeadings"> Other section headings so that output information is not duplicated </param>
    /// <returns> Section request string </returns>
    public static string SectionRequest(string question, IReadOnlyList<string> otherHeadings)
    {
        return $"Other sections: {string.Join(", ", otherHeadings)}\n{Question(question)}";
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
