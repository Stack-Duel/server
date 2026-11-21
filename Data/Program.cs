using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(cfg =>
    {
        cfg.AddJsonFile("appsettings.json", optional: true);
        cfg.AddEnvironmentVariables(prefix: "TODO_");
    })
    .ConfigureLogging(l => l.AddConsole())
    .ConfigureServices((ctx, services) => { });

using var host = builder.Build();

var root = new RootCommand("StackDuel Database Migration/Seeding Tool") { };
