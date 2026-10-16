namespace LocalDeepResearch.Research;

/// <summary>
/// A single source used to answer a prompt.
/// </summary>
/// <param name="Title"> Page title </param>
/// <param name="Url"> Page URL </param>
public record Source(string Title, string Url);
