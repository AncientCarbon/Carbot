using Carbot;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Text;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Discord client config
builder.Services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildVoiceStates,
    AlwaysDownloadUsers = true,
    LogLevel = LogSeverity.Info
}));

builder.Services.AddSingleton(sp =>
    new InteractionService(sp.GetRequiredService<DiscordSocketClient>()));

var dbConnectionString = builder.Configuration.GetConnectionString("CarbotDb");
if (string.IsNullOrWhiteSpace(dbConnectionString))
{
    throw new Exception("DB_CONNECTION_STRING is not set in .env");
}

builder.Services.AddDbContextFactory<CarbotDbContext>(options =>
    options.UseNpgsql(dbConnectionString));

builder.Services.AddSingleton<PromptStore>();
builder.Services.AddSingleton<SlashModule>();
builder.Services.AddHostedService<DiscordBotService>();

var app = builder.Build();
app.UseStaticFiles();


app.MapGet("/", async (PromptStore prompts) =>
{
    var truths = await prompts.GetTruthsAsync();
    var dares = await prompts.GetDaresAsync();

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
            <ul id="truths-list">
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
            <ul id="dares-list">
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
        This page is backed by the same EF Core store the Discord bot uses.<br />
        Changes are stored in a remote PostgreSQL DB.
    </div>
</div>

<script>
async function refreshPrompts() {
    try {
        const [truthsRes, daresRes] = await Promise.all([
            fetch('/truths'),
            fetch('/dares')
        ]);

        if (!truthsRes.ok || !daresRes.ok) {
            console.error('Failed to fetch prompts');
            return;
        }

        const truths = await truthsRes.json();
        const dares = await daresRes.json();

        const truthsList = document.getElementById('truths-list');
        const daresList = document.getElementById('dares-list');

        truthsList.innerHTML = truths.map((t, i) =>
            `<li><span class="index">#${i + 1}</span>${t}</li>`).join('');

        daresList.innerHTML = dares.map((d, i) =>
            `<li><span class="index">#${i + 1}</span>${d}</li>`).join('');
    } catch (err) {
        console.error('Error refreshing prompts:', err);
    }
}

setInterval(refreshPrompts, 5000);
refreshPrompts();
</script>
</body>
</html>
""");

    return Results.Content(sb.ToString(), "text/html");
});


app.MapGet("/truths", async (PromptStore prompts) =>
    Results.Json(await prompts.GetTruthsAsync()));

app.MapGet("/dares", async (PromptStore prompts) =>
    Results.Json(await prompts.GetDaresAsync()));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CarbotDbContext>();

    // Create tables based on the model if they don't exist yet
    db.Database.EnsureCreated();

    // Seed only if Prompts table is empty
    if (!db.Prompts.Any())
    {
        db.Prompts.AddRange(
            new Prompt { Type = PromptType.Truth, Text = "What is your biggest irrational fear?" },
            new Prompt { Type = PromptType.Truth, Text = "Have you ever lied about something serious?" },
            new Prompt { Type = PromptType.Truth, Text = "Who is your crush currently?" },
            new Prompt { Type = PromptType.Dare, Text = "Eat healthier today" },
            new Prompt { Type = PromptType.Dare, Text = "Drink some water" },
            new Prompt { Type = PromptType.Dare, Text = "Do 10 push-ups" }
        );
        db.SaveChanges();
    }
}


app.Run();
