using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace LocalDeepResearch.Services;

/// <summary>
/// Static class for chunking web page text content to feed into our local models.
/// </summary>
public static class Chunker
{
    /// <summary>
    /// Max number of characters allowed in a chunk.
    /// </summary>
    private const int MaxChars = 1000;
    /// <summary>
    /// Used to separate different sections of text inside a passage.
    /// </summary>
    private const string Separator = "\n\n";

    private static readonly Regex Whitespace = new(@"\s+");
    private static readonly Regex SentenceEnd = new(@"(?<=[.!?])\s+");

    /// <summary>
    /// Creates formatted chunks of text from a webpage that we can feed into our local models.
    /// </summary>
    /// <param name="html"> Content HTML from webpage </param>
    /// <param name="title"> Page title </param>
    /// <param name="url"> Page URL </param>
    /// <returns> List of chunks to feed into our models </returns>
    public static IReadOnlyList<Passage> Chunk(string html, string title, string url)
    {
        var document = new HtmlParser().ParseDocument(html);

        List<Passage> passages = new();
        var buffer = new StringBuilder();

        foreach (var element in document.QuerySelectorAll("h1, h2, h3, h4, h5, h6, p, li"))
        {
            // Ignore paragraph tags that are in lists, as we already account for lists
            if (element.LocalName == "p" && element.Closest("li") is not null)
                continue;

            string text = Normalize(TextWithoutNestedLists(element));

            if (text.Length == 0)
                continue;

            // New headings indicate new sections which should be in their own passage
            if (element is IHtmlHeadingElement)
                Flush();

            if (text.Length <= MaxChars)
            {
                Add(text);
                continue;
            }
            
            // If this text alone is over the char limit, break it up by sentence and add those instead
            foreach (string sentence in SentenceEnd.Split(text))
            {
                if (sentence.Length <= MaxChars)
                    Add(sentence);
            }
        }

        Flush();
        return passages;

        // Adds text content to our string buffer
        void Add(string text)
        {
            // Flush the current passage if this text would bring a passage over its character limit
            if (buffer.Length > 0 && buffer.Length + Separator.Length + text.Length > MaxChars)
                Flush();

            if (buffer.Length > 0)
                buffer.Append(Separator);

            buffer.Append(text);
        }

        // Marks a single passage as complete and adds it to the list
        void Flush()
        {
            if (buffer.Length == 0)
                return;

            passages.Add(new Passage(url, title, buffer.ToString()));
            buffer.Clear();
        }
    }
    /// <summary>
    /// Gets text from an element, excluding nested list content.
    /// </summary>
    /// <param name="element"> Page element </param>
    /// <returns> Text content </returns>
    private static string TextWithoutNestedLists(IElement element)
    {
        var text = new StringBuilder();

        foreach (var node in element.ChildNodes)
        {
            // Don't count "ul" and "ol" as additional elements to pull text from because it's already included by "li"
            if (node is IElement child && (child.LocalName == "ul" || child.LocalName == "ol"))
                continue;

            text.Append(node.TextContent);
        }

        return text.ToString();
    }
    /// <summary>
    /// Replace all runs of whitespace with a single space.
    /// </summary>
    /// <param name="text"> Original text </param>
    /// <returns> Normalized text </returns>
    private static string Normalize(string text)
    {
        return Whitespace.Replace(text, " ").Trim();
    }
}

/// <summary>
/// A chunk of text that gets passed into a reranker for relevance scoring.
/// </summary>
/// <param name="Url"> URL that contains this passage </param>
/// <param name="Title"> Title of the page that contains this passage</param>
/// <param name="Text"> Text content from the page </param>
public record Passage(string Url, string Title, string Text)
{
    public string Host => new Uri(Url).Host;
}
