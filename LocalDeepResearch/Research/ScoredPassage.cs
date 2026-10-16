using LocalDeepResearch.Extraction;

namespace LocalDeepResearch.Research;

/// <summary>
/// Passage with its score and total token count.
/// </summary>
/// <param name="Passage"> Scored passage </param>
/// <param name="Score"> Passage score </param>
/// <param name="Tokens"> Total token count for this passage </param>
public record ScoredPassage(Passage Passage, float Score, int Tokens);
