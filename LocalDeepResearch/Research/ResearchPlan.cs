namespace LocalDeepResearch.Research;

/// <summary>
/// Complete research plan for our deep research engine.
/// </summary>
/// <param name="Title"> Title of our research report </param>
/// <param name="History"> Optional history specific subquestion </param>
/// <param name="SubQuestions"> Additional subquestions </param>
public record ResearchPlan(string Title, SubQuestion? History, List<SubQuestion> SubQuestions);
