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
    CavemanSummarizer summarizer)
{
    [McpServerTool(Name = "compress")]
    [Description("Removes stop words and lemmatizes text to reduce LLM token count. Up to 70% token reduction across 50+ languages. Language is auto-detected.")]
    public async Task<string> Compress(
        [Description("Text to compress")] string text,
        [Description("Compression level: light, semantic, or aggressive (default: semantic)")] string level = "semantic")
    {
        var lvl = level.ToLowerInvariant() switch
        {
            "light"      => CavemanCompressionLevel.Light,
            "aggressive" => CavemanCompressionLevel.Aggressive,
            _            => CavemanCompressionLevel.Semantic,
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
    [Description("Extractive summarization — selects the most important sentences using TF-IDF or TextRank. Keeps the meaning, discards redundancy.")]
    public string Summarize(
        [Description("Text to summarize")] string text,
        [Description("Number of sentences to keep (overrides ratio when set)")] int? sentences = null,
        [Description("Fraction of sentences to keep, e.g. 0.3 for 30% (default: 0.3)")] float ratio = 0.3f,
        [Description("Algorithm: tfidf or textrank (default: textrank)")] string algo = "textrank")
    {
        string summary;
        if (algo.Equals("tfidf", StringComparison.OrdinalIgnoreCase))
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

        return JsonSerializer.Serialize(new { summary, algorithm = algo });
    }

    [McpServerTool(Name = "compress_batch")]
    [Description("Compresses a list of texts in one call. Returns an array of compressed results with savings for each item.")]
    public async Task<string> CompressBatch(
        [Description("Array of texts to compress")] string[] texts,
        [Description("Compression level: light, semantic, or aggressive (default: semantic)")] string level = "semantic")
    {
        var lvl = level.ToLowerInvariant() switch
        {
            "light"      => CavemanCompressionLevel.Light,
            "aggressive" => CavemanCompressionLevel.Aggressive,
            _            => CavemanCompressionLevel.Semantic,
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
}
