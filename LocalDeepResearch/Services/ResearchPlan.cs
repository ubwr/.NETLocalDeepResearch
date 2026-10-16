namespace LocalDeepResearch.Services;

/// <summary>
/// Complete research plan for our deep research engine.
/// </summary>
/// <param name="Title"> Title of our research report </param>
/// <param name="History"> History specific subquestion </param>
/// <param name="SubQuestions"> Additional subquestions </param>
public record ResearchPlan(string Title, SubQuestion History, List<SubQuestion> SubQuestions);
/// <summary>
/// Single subquestion that will get used as a prompt for our local LLM.
/// A single user generated prompt will get split into multiple subquestions to be used in our <see cref="ResearchPlan"/>.
/// </summary>
/// <param name="Heading"> Section heading </param>
/// <param name="Question"> Section subquestion </param>
public record SubQuestion(string Heading, string Question);
