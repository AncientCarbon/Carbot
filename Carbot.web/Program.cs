using Carbot;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Discord client config
builder.Services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.Guilds,
    AlwaysDownloadUsers = false,
    LogLevel = LogSeverity.Info
}));

builder.Services.AddSingleton(sp =>
    new InteractionService(sp.GetRequiredService<DiscordSocketClient>()));

// Shared prompt store
builder.Services.AddSingleton<PromptStore>();

// Slash commands module
builder.Services.AddSingleton<SlashModule>();

// Hosted Discord bot
builder.Services.AddHostedService<DiscordBotService>();

var app = builder.Build();

// HTML frontend build with AI assistance
app.MapGet("/", (PromptStore prompts) =>
{
    var truths = prompts.GetTruths();
    var dares = prompts.GetDares();

    var sb = new StringBuilder();
    sb.Append("""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <title>Carbot Prompts</title>
    <style>
        body {
            font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
            background: #0f172a;
            color: #e5e7eb;
            margin: 0;
            padding: 2rem;
        }
        .container {
            max-width: 960px;
            margin: 0 auto;
        }
        h1 {
            text-align: center;
            margin-bottom: 1rem;
        }
        .subtitle {
            text-align: center;
            color: #9ca3af;
            margin-bottom: 2rem;
            font-size: 0.95rem;
        }
        .grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
            gap: 1.5rem;
        }
        .card {
            background: #111827;
            border-radius: 0.75rem;
            padding: 1rem 1.25rem;
            box-shadow: 0 10px 30px rgba(0,0,0,0.35);
            border: 1px solid #1f2937;
        }
        .card h2 {
            font-size: 1.1rem;
            margin-top: 0;
            margin-bottom: 0.5rem;
        }
        .card small {
            color: #6b7280;
        }
        ul {
            list-style: none;
            padding-left: 0;
            margin: 0.5rem 0 0;
            max-height: 420px;
            overflow-y: auto;
        }
        li {
            padding: 0.35rem 0;
            border-bottom: 1px solid #1f2937;
            font-size: 0.95rem;
        }
        li:last-child {
            border-bottom: none;
        }
        .index {
            color: #9ca3af;
            font-size: 0.8rem;
            margin-right: 0.5rem;
            opacity: 0.9;
        }
        .footer {
            margin-top: 2rem;
            text-align: center;
            font-size: 0.8rem;
            color: #6b7280;
        }
        .code {
            font-family: ui-monospace, Menlo, Monaco, "SF Mono", monospace;
            background: #111827;
            padding: 0.15rem 0.4rem;
            border-radius: 0.35rem;
            border: 1px solid #1f2937;
        }
    </style>
</head>
<body>
<div class="container">
    <h1>Carbot Prompts</h1>
    <div class="subtitle">
        Live view of the prompts your Discord bot is using.<br />
        Add/remove via slash commands in Discord (e.g. <span class="code">/addtruth</span>, <span class="code">/removetruth</span>).
    </div>
    <div class="grid">
        <div class="card">
            <h2>Truths</h2>
            <small>Use <span class="code">/truth</span> or <span class="code">/addtruth</span> in Discord</small>
            <ul>
""");

    for (var i = 0; i < truths.Count; i++)
    {
        var text = System.Net.WebUtility.HtmlEncode(truths[i]);
        sb.Append($"                <li><span class=\"index\">#{i + 1}</span>{text}</li>\n");
    }

    sb.Append("""
            </ul>
        </div>
        <div class="card">
            <h2>Dares</h2>
            <small>Use <span class="code">/dare</span> or <span class="code">/adddare</span> in Discord</small>
            <ul>
""");

    for (var i = 0; i < dares.Count; i++)
    {
        var text = System.Net.WebUtility.HtmlEncode(dares[i]);
        sb.Append($"                <li><span class=\"index\">#{i + 1}</span>{text}</li>\n");
    }

    sb.Append("""
            </ul>
        </div>
    </div>
    <div class="footer">
        This page is backed by the same in-memory + JSON store the Discord bot uses.<br />
        Restart-safe thanks to <span class="code">prompts.json</span> in the mounted volume.
    </div>
</div>
</body>
</html>
""");

    return Results.Content(sb.ToString(), "text/html");
});

// keep JSON endpoints for debugging / future JS
app.MapGet("/truths", (PromptStore prompts) =>
    Results.Json(prompts.GetTruths()));

app.MapGet("/dares", (PromptStore prompts) =>
    Results.Json(prompts.GetDares()));

app.Run();
