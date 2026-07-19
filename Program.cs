// -----------------------------------------------------------------------------
// <copyright file="Program.cs" company="Digitalsolutions.it">
//   Caveman.Mcp — MCP server for Caveman NLP compression.
//   Copyright (c) 2026 Passaro Francesco Paolo — Digitalsolutions.it.
//   See: https://github.com/francescopaolopassaro/caveman
// </copyright>
// -----------------------------------------------------------------------------
using caveman.core;
using caveman.core.services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddSingleton<FunctionWordProvider>()
    .AddSingleton<CavemanLanguageDetector>(sp =>
        new CavemanLanguageDetector(sp.GetRequiredService<FunctionWordProvider>()))
    .AddSingleton<CavemanCompressionService>()
    .AddSingleton<CavemanSummarizer>(sp =>
        new CavemanSummarizer(sp.GetRequiredService<FunctionWordProvider>()))
    .AddSingleton<CavemanRetriever>(sp =>
        new CavemanRetriever(sp.GetRequiredService<FunctionWordProvider>()))
    .AddSingleton<CavemanCodeCompressor>()
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(Program).Assembly);

await builder.Build().RunAsync();
