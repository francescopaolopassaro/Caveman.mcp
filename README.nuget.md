# Caveman.Mcp

MCP (Model Context Protocol) server for Caveman. It exposes NLP token compression, language detection, content
routing, summarization, retrieval and a semantic cache as tools for Claude Code, Cursor, OpenCode and any other MCP
client. No model and no network call are needed: the language data ships inside the package.

Version 1.5.0 targets .NET 10 and references Caveman 1.5.0.

## Install

    dotnet tool install -g Caveman.Mcp

Register it in your MCP client as a stdio server whose command is caveman-mcp.

## Tools

| Tool | Description |
| :--- | :--- |
| compress | Removes function words and lemmatizes text. Levels: light, semantic, aggressive, statistical, syntactic |
| detect_language | Language of any text, ISO 639-3 code plus confidence scores |
| route_content | Detects JSON, HTML, diff, log, code or prose and applies the best algorithm |
| summarize | Extractive summarization with TF-IDF or TextRank |
| compress_batch | Compresses a list of texts in one call |
| retrieve | BM25+ ranking of text chunks against a query |
| skeletonize_code | Strips comments and collapses bodies to signatures |
| near_duplicate | SimHash near-duplicate detection |
| compress_sql | Whitespace-safe SQL compression |
| idf_languages | Lists the shipped global IDF tables |
| semantic_cache_get | New in 1.5.0. Looks a query up; a paraphrase of an earlier query returns the cached answer |
| semantic_cache_set | New in 1.5.0. Stores an answer under a query |
| semantic_cache_stats | New in 1.5.0. Entries, hits, misses, hit rate, characters served from the cache |
| semantic_cache_clear | New in 1.5.0. Empties the cache |

## Semantic cache

The agent calls semantic_cache_get before spending a model call, and semantic_cache_set after producing a fresh
answer.

| Call | Result |
| :--- | :--- |
| semantic_cache_set: query "How do I delete the file Program.cs from the project?", response "git rm Program.cs" | stored |
| semantic_cache_get: query "How to delete file Program.cs from project?" | hit, response "git rm Program.cs" |
| semantic_cache_get: query "How do I delete the file Startup.cs from the project?" | miss |

Numbers, file names, paths, identifiers and quoted strings must match exactly for a hit. Queries are compared after
removing function words and reducing words to their lemma, in the language of the query (50+ languages).

## Requirements

| Item | Value |
| :--- | :--- |
| Runtime | .NET 10 |
| Installed as | .NET tool, command caveman-mcp |
| Core package | Caveman 1.5.0 |

## License

MIT with mandatory attribution to Passaro Francesco Paolo, Digitalsolutions.it. See the LICENSE file in the package.
