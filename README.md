# Caveman.Mcp

**MCP (Model Context Protocol) server for [Caveman](https://github.com/francescopaolopassaro/caveman)** — exposes NLP token compression, language detection, content routing and summarization as MCP tools for Claude Code, OpenCode, Cursor, Windsurf, and any MCP-compatible agent.

But for Claude and other Agents we recommend our dedicated Synthelion project which can be found here https://github.com/francescopaolopassaro/Synthelion. This MCP was created to have a package to remain in the C#/Caveman environment for future developments facilitated for this kind of purpose. So if you want to use Caveman as an independent MCP and plugin for your agent, use Synthelion.

> Part of the Caveman ecosystem: NLP prompt compressor for LLMs. Up to 70% token reduction, 50+ languages, zero ML models.

---

## Technology Partnership

<img src="https://www.digitalsolutions.it/img/partners/novaroutelogo.png" alt="NovaRouteAI" height="180" style="max-width: 100%; height: auto; min-height: 180px; max-height: 190px;">

**[NovaRouteAI](https://novarouteai.com/?ref=synthelion)** — Build with Chinese AI models through one simple API.

NovaRouteAI helps developers and AI SaaS teams test, compare, and run models like DeepSeek, Qwen, Doubao, Kimi, and GLM without managing multiple provider accounts. Start with test credits and optimize your cost per successful task.

[Click here to know NovaRouteAI](https://novarouteai.com/?ref=synthelion)

---

## Quick install — Claude Code

```json
{
  "mcpServers": {
    "caveman": {
      "command": "caveman-mcp"
    }
  }
}
```

Or with dotnet tool:

```bash
dotnet tool install --global Caveman.Mcp
```

---

## Tools

| Tool | Description |
|---|---|
| **compress** | Removes stop words and lemmatizes text. Up to 70% token reduction. Levels: light, semantic, aggressive, statistical, syntactic. |
| **detect_language** | Identifies language of any text. Returns ISO 639-3 code + confidence scores. |
| **route_content** | Auto-detects JSON, HTML, diff, log, code or prose and applies best algorithm. |
| **summarize** | Extractive summarization via TF-IDF or TextRank; `topicAware` segments by topic first. |
| **compress_batch** | Compresses a list of texts in one call. |
| **retrieve** | BM25+ ranking of text chunks against a query; optional RM3 pseudo-relevance feedback. |
| **skeletonize_code** | Strips comments and collapses function/method bodies to signatures only. |
| **near_duplicate** | SimHash-based near-duplicate detection (Hamming distance) for templated/near-identical text. |
| **compress_sql** | Whitespace-safe SQL compression (optional comment stripping and CCR value-folding) for sending SQL to an LLM cheaply. |
| **idf_languages** | Lists the 56 shipped global-IDF tables and, for a given ISO 639-3 code, whether a table exists and its corpus size. |

---

## Usage

### compress

```
compress(text: "I would like to know if it is possible to receive information.", level: "semantic")
→ { "compressed": "know possible receive information", "efficiency_pct": 65.0, "energy_mwh": 0.05, "co2_mg": 0.02 }
```

### route_content

```
route_content(content: "[{\"name\":\"Alice\",\"age\":30}]", profile: "balanced")
→ { "compressed": "| name | age |\n| Alice | 30 |", "strategy": "JsonCrush:MarkdownTable", "savings_pct": 47.0 }
```

### retrieve

```
retrieve(documents: ["Tesla ships a new battery", "Rivian raises truck price", "GDP grew 2%"], query: "car", topK: 2)
→ [{ "index": 0, "document": "Tesla ships a new battery", "score": 1.83 }, ...]
```

### skeletonize_code

```
skeletonize_code(code: "public int Add(int a, int b) {\n    // sums two numbers\n    return a + b;\n}")
→ { "compressed": "public int Add(int a, int b) {\n    /* ... */\n}", "functions_skeletonized": 1, ... }
```

### near_duplicate

```
near_duplicate(a: "User bob logged in from 10.0.0.1", b: "User alice logged in from 10.0.0.2")
→ { "is_near_duplicate": true, "hamming_distance": 2, "max_distance": 3 }
```

### compress_sql

```
compress_sql(sql: "SELECT  id, name\nFROM     users\nWHERE    age > 18;")
→ { "compressed": "SELECT id, name FROM users WHERE age > 18;", "was_compressed": true, "tuples_dropped": 0, "ccr_hash": null }

compress_sql(sql: "INSERT INTO t VALUES (1,'a'),(2,'b'),(3,'c');", foldValues: true)
→ { "compressed": "INSERT INTO t VALUES (1,'a'),(2,'b'),(3,'c'); -- CCR:3f2a9b1c8d4e", "was_compressed": true, "tuples_dropped": 0, "ccr_hash": "3f2a9b1c8d4e" }
```

### idf_languages

```
idf_languages(iso3: "eng")
→ { "language_count": 56, "languages": [ "ben", "bul", "cat", ... ], "iso3": "eng", "has_data": true, "corpus_size": 1150000000 }
```

---

## Supported languages (50+)

Afrikaans · Arabic · Armenian · Basque · Bengali · Bulgarian · Catalan · Chinese · Croatian · Czech · Danish · Dutch · English · Estonian · Finnish · French · Galician · German · Greek · Hebrew · Hindi · Hungarian · Icelandic · Indonesian · Irish · Italian · Japanese · Kannada · Kazakh · Korean · Latin · Latvian · Lithuanian · Macedonian · Malay · Marathi · Norwegian · Persian · Polish · Portuguese · Romanian · Russian · Serbian · Slovak · Slovenian · Spanish · Swedish · Tamil · Telugu · Thai · Turkish · Ukrainian · Urdu · Vietnamese

---

## Links

- **Caveman core:** https://github.com/francescopaolopassaro/caveman
- **NuGet (core):** https://www.nuget.org/packages/Caveman
- **Python port (Synthelion):** https://pypi.org/project/synthelion/

© 2026 Passaro Francesco Paolo — Digitalsolutions.it
