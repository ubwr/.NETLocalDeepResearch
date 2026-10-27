using LocalDeepResearch.Research;
using LocalDeepResearch.Resources;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace LocalDeepResearch.Export;

/// <summary>
/// Research report PDF generator.
/// </summary>
public static class ReportPdf
{
    private const string BackgroundColor = "#1E1E1E";   // Dark gray
    private const string TextColor = "#FFFFFF";         // White
    private const string HeadingColor = "#4FD1C5";      // Teal
    private const string LinkColor = "#90CDF4";         // Blue

    /// <summary>
    /// Creates a downloadable research report PDF.
    /// </summary>
    /// <param name="title"> Research report title </param>
    /// <param name="summary"> Optional research report executive summary </param>
    /// <param name="sections"> Research report sections </param>
    /// <returns> Byte array of the research report PDF </returns>
    public static byte[] Create(string title, string? summary, IReadOnlyList<Section> sections)
    {
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Margin(50);
                page.PageColor(BackgroundColor);
                page.DefaultTextStyle(style => style.FontFamily("Lato").FontSize(10).FontColor(TextColor));

                page.Content().Column(column =>
                {
                    column.Spacing(12);

                    // Title
                    column.Item().AlignCenter().Text(title).FontSize(20).Bold();

                    // Optional executive summary
                    if (summary is not null)
                        AddSection(column, Strings.ExecutiveSummary, summary);

                    // Text sections from subquestions
                    foreach (Section section in sections)
                        AddSection(column, section.Heading, section.Text);

                    // Sources heading
                    column.Item().PaddingTop(24).Text(Strings.Sources.ToUpperInvariant()).FontSize(12).Bold().FontColor(HeadingColor);

                    // Source list
                    foreach (Section section in sections)
                    {
                        column.Item().Column(sources =>
                        {
                            sources.Spacing(2);
                            sources.Item().Text(section.Heading).FontSize(10).Bold();

                            foreach (Source source in section.Sources)
                                sources.Item().Hyperlink(source.Url).Text(source.Title).FontSize(9).FontColor(LinkColor);
                        });
                    }
                });
            });
        }).GeneratePdf();
    }
    /// <summary>
    /// Adds a text section to a research report PDF in a consistent style.
    /// </summary>
    /// <param name="column"> Page column to add to </param>
    /// <param name="heading"> Section heading </param>
    /// <param name="text"> Section body </param>
    private static void AddSection(ColumnDescriptor column, string heading, string text)
    {
        column.Item().PaddingTop(12).Text(heading.ToUpperInvariant()).FontSize(14).Bold().Italic().FontColor(HeadingColor);
        column.Item().Text(text);
    }
}
