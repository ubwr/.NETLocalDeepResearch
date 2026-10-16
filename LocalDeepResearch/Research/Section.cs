namespace LocalDeepResearch.Research;

/// <summary>
/// A single section in our research output.
/// </summary>
/// <param name="Heading"> Section heading </param>
/// <param name="Text"> Section text content </param>
/// <param name="Sources"> List of sources used to write this section </param>
public record Section(string Heading, string Text, IReadOnlyList<Source> Sources);
