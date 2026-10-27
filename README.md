# LocalDeepResearch

A self-hosted deep research tool built with .NET 10 and Blazor Server. Ask a question and it plans a set of subquestions, searches the web through your own SearXNG instance, reads and filters the most relevant pages, and writes a full report with sources, all using models that run on your own machine.

## How it works

1. **Plan:** a local LLM turns your prompt into a structured research plan with proper headings and subquestions.
2. **Search:** each subquestion is searched through SearXNG.
3. **Filter:** a local reranker scores every search result, and only relevant results have their pages fetched.
4. **Extract:** pages are parsed and split into text passages.
5. **Select:** passages are reranked, and the most relevant passages that fit within the model's context window are selected.
6. **Write:** the LLM writes one section per subquestion, using only the selected passages. Sections without enough supporting sites are skipped.
7. **Summarize:** an optional executive summary is written from the finished sections.

The finished report can be downloaded as a PDF.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://docs.docker.com/get-docker/)
- A GPU is recommended but not required. Models run on the CPU if no supported GPU is found.

## Setup

### 1. Start SearXNG

Create a `.env` file in the repository root with a random secret:

**macOS / Linux**

```bash
echo "SEARXNG_SECRET=$(openssl rand -hex 32)" > .env
```

**Windows (PowerShell)**

```powershell
$b = [byte[]]::new(32); [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); "SEARXNG_SECRET=$(-join ($b | % { $_.ToString('x2') }))" | Set-Content .env -Encoding ascii
```

Then start SearXNG:

```bash
docker compose up -d
```

SearXNG runs on `http://localhost:8080` and is only reachable from your own machine. Its settings live in `searxng/settings.yml`. Additional documentation for SearXNG settings can be found [here](https://docs.searxng.org/admin/settings/index.html).

### 2. Run the app

```bash
cd LocalDeepResearch
dotnet run
```

Then open `http://localhost:5032`.

The first run downloads llama-server, the default language model, and the reranker model.

## Research

- **Model:** pick one of the built-in models, or choose **Custom** and enter a compatible Hugging Face GGUF model ID. If a model fails to load, the app falls back to its configured default.
- **Page relevance threshold:** how relevant a search result must be for its page to be fetched.
- **Passage relevance threshold:** how relevant a text passage must be for it to be used as context for a section.
- **Minimum sites per section:** how many different websites a section requires before it can be written.
- **Additional sections:** how many sections to write, not counting historical context.
- **Include executive summary** / **Include historical context:** turn those parts of the report on or off.

Only one research run can happen at a time across all open tabs. A run can be cancelled at any point.

## Configuration

Defaults live in `LocalDeepResearch/appsettings.json`.

| Setting | Default | Description                                  |
|---|---|----------------------------------------------|
| `Chat:ModelId` | `gguf:qwen3-balanced` | Default language model                       |
| `Chat:ServerBinaryPath` | empty | Optional path to your own llama-server build |
| `Chat:MaxContextLength` | `8192` | Maximum context window size in tokens      |
| `Chat:MaxOutputTokens` | `2000` | Maximum tokens per model response            |
| `Chat:Temperature` | `0.3` | Controls randomness in token selection                         |
| `Reranker:PageThreshold` | `0.5` | Default page relevance threshold             |
| `Reranker:PassageThreshold` | `0.95` | Default passage relevance threshold          |
| `Searxng:Url` | `http://localhost:8080` | SearXNG address                              |
| `Searxng:Categories` | `general,science` | SearXNG categories to search                 |
| `Searxng:MaxResults` | `100` | Maximum search results per subquestion       |
| `Searxng:PageCount` | `2` | Search result pages requested per subquestion |

## Built with

- [LMSupply](https://github.com/iyulab/lm-supply) for local LLM and reranker inference
- [SearXNG](https://github.com/searxng/searxng) for search
- [Trafilatura](https://www.nuget.org/packages/Trafilatura) for page extraction
- [AngleSharp](https://anglesharp.github.io/) for HTML parsing
- [MudBlazor](https://mudblazor.com/) for the UI
- [QuestPDF](https://www.questpdf.com/) for PDF export

## Licensing notes

PDF export uses QuestPDF under its [Community License](https://www.questpdf.com/license/community.html), which is free for individuals, open-source projects, and organizations with under $1M USD in annual revenue. Larger organizations need a QuestPDF commercial license.
