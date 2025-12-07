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

builder.Services.AddSingleton<PromptStore>();

builder.Services.AddSingleton<SlashModule>();

builder.Services.AddHostedService<DiscordBotService>();

var app = builder.Build();
app.UseStaticFiles();


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
    <link rel="stylesheet" href="/css/site.css" />
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
        This page is backed by the same in-memory + JSON/volume store the Discord bot uses.<br />
        Restart-safe thanks to <span class="code">prompts.json</span> in the mounted volume.
    </div>
</div>
</body>
</html>
""");

    return Results.Content(sb.ToString(), "text/html");
});


app.MapGet("/truths", (PromptStore prompts) =>
    Results.Json(prompts.GetTruths()));

app.MapGet("/dares", (PromptStore prompts) =>
    Results.Json(prompts.GetDares()));

app.Run();
