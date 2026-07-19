// -----------------------------------------------------------------------------
// <copyright file="CavemanMcpTools.cs" company="Digitalsolutions.it">
//   Caveman.Mcp — MCP tools for Caveman NLP compression.
//   Copyright (c) 2026 Passaro Francesco Paolo — Digitalsolutions.it.
//   See: https://github.com/francescopaolopassaro/caveman
// </copyright>
// -----------------------------------------------------------------------------
using System.ComponentModel;
using System.Text.Json;
using caveman.core;
using caveman.core.entities;
using caveman.core.services;
using ModelContextProtocol.Server;

namespace Caveman.Mcp;

[McpServerToolType]
internal sealed class CavemanMcpTools(
    CavemanCompressionService compression,
    CavemanLanguageDetector detector,
    CavemanSummarizer summarizer,
    CavemanRetriever retriever,
    CavemanCodeCompressor codeCompressor)
{
    [McpServerTool(Name = "compress")]
    [Description("Removes stop words and lemmatizes text to reduce LLM token count. Up to 70% token reduction across 50+ languages. Language is auto-detected.")]
    public async Task<string> Compress(
        [Description("Text to compress")] string text,
        [Description("Compression level: light, semantic, aggressive, statistical, or syntactic (default: semantic)")] string level = "semantic")
    {
        var lvl = level.ToLowerInvariant() switch
        {
            "light"       => CavemanCompressionLevel.Light,
            "aggressive"  => CavemanCompressionLevel.Aggressive,
            "statistical" => CavemanCompressionLevel.Statistical,
            "syntactic"   => CavemanCompressionLevel.Syntactic,
            _             => CavemanCompressionLevel.Semantic,
        };

        var r = await compression.CompressAsync(text, lvl);
        return JsonSerializer.Serialize(new
        {
            compressed        = r.CompressedText,
            original_tokens   = r.OriginalTokens,
            compressed_tokens = r.CompressedTokens,
            efficiency_pct    = Math.Round(r.EfficiencyPercentage, 2),
            energy_mwh        = Math.Round(r.EstimatedEnergySavedMWh, 3),
            co2_mg            = Math.Round(r.EstimatedCO2SavedMg, 3),
        });
    }

    [McpServerTool(Name = "detect_language")]
    [Description("Identifies the language of a text and returns its ISO 639-3 code (e.g. 'eng', 'ita', 'fra'). Supports 50+ languages.")]
    public string DetectLanguage(
        [Description("Text to analyse")] string text)
    {
        var iso3   = detector.Detect(text);
        var scores = detector.DetectWithScores(text)
            .OrderByDescending(kv => kv.Value)
            .Take(3)
            .ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value, 4));

        return JsonSerializer.Serialize(new { language = iso3, top_scores = scores });
    }

    [McpServerTool(Name = "route_content")]
    [Description("Auto-detects content type (JSON array, HTML, git diff, log, source code, plain text) and applies the best compression algorithm.")]
    public string RouteContent(
        [Description("Content to compress (JSON, HTML, diff, log, code, or prose)")] string content,
        [Description("Compression profile: light, balanced, agent, or aggressive (default: balanced)")] string profile = "balanced")
    {
        var p = profile.ToLowerInvariant() switch
        {
            "light"      => CompressionProfile.Light,
            "agent"      => CompressionProfile.Agent,
            "aggressive" => CompressionProfile.Aggressive,
            _            => CompressionProfile.Balanced,
        };

        var r = CavemanContentRouter.FromProfile(p).Route(content);
        return JsonSerializer.Serialize(new
        {
            compressed    = r.Compressed,
            detected_type = r.DetectedType.ToString(),
            strategy      = r.StrategyUsed,
            savings_pct   = Math.Round(r.SavingsPercent, 2),
            tokens_before = r.TokensBefore,
            tokens_after  = r.TokensAfter,
        });
    }

    [McpServerTool(Name = "summarize")]
    [Description("Extractive summarization — selects the most important sentences using TF-IDF or TextRank. Keeps the meaning, discards redundancy. Set topicAware=true to segment the text into topics first and allocate the sentence budget proportionally across them, so one dense topic can't starve the rest.")]
    public string Summarize(
        [Description("Text to summarize")] string text,
        [Description("Number of sentences to keep (overrides ratio when set)")] int? sentences = null,
        [Description("Fraction of sentences to keep, e.g. 0.3 for 30% (default: 0.3)")] float ratio = 0.3f,
        [Description("Algorithm: tfidf or textrank (default: textrank). Ignored when topicAware=true.")] string algo = "textrank",
        [Description("Segment text into topics first and allocate the sentence budget across them (default: false)")] bool topicAware = false,
        [Description("ISO 639-3 language code hint for topic-aware mode (default: auto-detect)")] string? iso3 = null)
    {
        string summary;
        string algorithm = algo;

        if (topicAware)
        {
            algorithm = "topic-aware";
            summary = summarizer.CondenseTextTopicAware(text, sentences ?? 5, iso3);
        }
        else if (algo.Equals("tfidf", StringComparison.OrdinalIgnoreCase))
        {
            summary = sentences.HasValue
                ? summarizer.Summarize(text, sentences.Value)
                : summarizer.Summarize(text, ratio);
        }
        else
        {
            var tr = new CavemanTextRank(new FunctionWordProvider());
            summary = sentences.HasValue
                ? tr.Summarize(text, sentences.Value)
                : tr.Summarize(text, ratio);
        }

        return JsonSerializer.Serialize(new { summary, algorithm });
    }

    [McpServerTool(Name = "compress_batch")]
    [Description("Compresses a list of texts in one call. Returns an array of compressed results with savings for each item.")]
    public async Task<string> CompressBatch(
        [Description("Array of texts to compress")] string[] texts,
        [Description("Compression level: light, semantic, aggressive, statistical, or syntactic (default: semantic)")] string level = "semantic")
    {
        var lvl = level.ToLowerInvariant() switch
        {
            "light"       => CavemanCompressionLevel.Light,
            "aggressive"  => CavemanCompressionLevel.Aggressive,
            "statistical" => CavemanCompressionLevel.Statistical,
            "syntactic"   => CavemanCompressionLevel.Syntactic,
            _             => CavemanCompressionLevel.Semantic,
        };

        var results = await compression.CompressBatchAsync(texts, lvl);
        var output  = results.Select(r => new
        {
            compressed        = r.CompressedText,
            efficiency_pct    = Math.Round(r.EfficiencyPercentage, 2),
            original_tokens   = r.OriginalTokens,
            compressed_tokens = r.CompressedTokens,
        });

        return JsonSerializer.Serialize(output);
    }

    [McpServerTool(Name = "retrieve")]
    [Description("Ranks a list of text chunks (sentences, log lines, search results, conversation turns, …) against a query using BM25+. Set feedback=true to additionally run RM3 pseudo-relevance feedback, which expands the query with vocabulary from the top initial results — surfaces relevant chunks that don't literally contain the query's words. Pure term statistics, no embeddings, no network call.")]
    public string Retrieve(
        [Description("Array of text chunks to rank")] string[] documents,
        [Description("The query to rank chunks against")] string query,
        [Description("Maximum number of results to return (default: 5)")] int topK = 5,
        [Description("Run RM3 pseudo-relevance feedback query expansion (default: false)")] bool feedback = false,
        [Description("ISO 639-3 language code hint for feedback mode's function-word filtering (default: none)")] string? iso3 = null)
    {
        var results = feedback
            ? retriever.RetrieveWithFeedback(documents, query, topK, iso3)
            : retriever.Retrieve(documents, query, topK);

        var output = results.Select(r => new { index = r.Index, document = r.Document, score = Math.Round(r.Score, 4) });
        return JsonSerializer.Serialize(output);
    }

    [McpServerTool(Name = "skeletonize_code")]
    [Description("Strips comments and blank-line runs from source code, then replaces function/method bodies with a placeholder, keeping only signatures. Uses real brace-depth counting (C-family) or indentation (Python) to find each body — never a container like a class. Lossy by design: use to let an agent see a file's shape without spending tokens on implementation detail.")]
    public string SkeletonizeCode(
        [Description("Source code to skeletonize")] string code)
    {
        var r = codeCompressor.Compress(code, skeletonize: true);
        return JsonSerializer.Serialize(new
        {
            compressed             = r.Compressed,
            was_compressed         = r.WasCompressed,
            detected_language      = r.DetectedLanguage,
            comments_removed       = r.CommentsRemoved,
            functions_skeletonized = r.FunctionsSkeletonized,
            blank_lines_removed    = r.BlankLinesRemoved,
        });
    }

    [McpServerTool(Name = "near_duplicate")]
    [Description("Checks whether two texts are near-duplicates using a 64-bit Charikar SimHash fingerprint — catches templated text that differs only in a substituted value (e.g. a username or IP address), unlike exact hash matching. Returns the Hamming distance between fingerprints; lower means more similar.")]
    public string NearDuplicate(
        [Description("First text")] string a,
        [Description("Second text")] string b,
        [Description("Maximum Hamming distance to still count as a near-duplicate (default: 3)")] int maxDistance = 3,
        [Description("Shingle size in words for the fingerprint (default: 1)")] int shingleSize = 1)
    {
        bool isDuplicate = CavemanSimHash.AreNearDuplicates(a, b, maxDistance, shingleSize);
        int distance = CavemanSimHash.HammingDistance(
            CavemanSimHash.Compute(a, shingleSize),
            CavemanSimHash.Compute(b, shingleSize));

        return JsonSerializer.Serialize(new { is_near_duplicate = isDuplicate, hamming_distance = distance, max_distance = maxDistance });
    }
}
