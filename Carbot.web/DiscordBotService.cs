using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DotNetEnv;

namespace Carbot;

public class DiscordBotService : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly ILogger<DiscordBotService> _logger;
    
    private bool _registered;
    private ulong _guildId;
    private string? _token;

    public DiscordBotService(
        DiscordSocketClient client,
        InteractionService interactions,
        IServiceProvider services,
        ILogger<DiscordBotService> logger)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        Env.Load();
        _token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
        var guildIdStr = Environment.GetEnvironmentVariable("TEST_SERVER"); // private testing server

        if (string.IsNullOrWhiteSpace(_token) ||
            string.IsNullOrWhiteSpace(guildIdStr) ||
            !ulong.TryParse(guildIdStr, out _guildId))
        {
            _logger.LogError("Set tokens DISCORD_TOKEN and TEST_SERVER in .env");
            return;
        }
        _client.Log += msg => { _logger.LogInformation("{Msg}", msg.ToString()); return Task.CompletedTask; };
        _interactions.Log += msg => { _logger.LogInformation("{Msg}", msg.ToString()); return Task.CompletedTask; };
        _client.InteractionCreated += async socketInteraction =>
        {
            try
            {
                var ctx = new SocketInteractionContext(_client, socketInteraction);
                await _interactions.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while handling interaction");
                if (socketInteraction.Type == InteractionType.ApplicationCommand)
                {
                    await socketInteraction.GetOriginalResponseAsync()
                        .ContinueWith(async m => await (await m).DeleteAsync());
                }
            }
        };

        _client.Ready += async() =>
        {
            if (_registered) return;
            _registered = true;

            await _interactions.AddModuleAsync<SlashModule>(_services);
            await _interactions.RegisterCommandsToGuildAsync(_guildId);

            _logger.LogInformation("Slash commands synced to guild {GuildId}", _guildId);
            _logger.LogInformation("Commands: {Commands}", string.Join(", ", _interactions.SlashCommands));
        };
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_token))
        {
            _logger.LogWarning("Discord bot not started because token is missing");
            return;
        }
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();

        _logger.LogInformation("Discord bot is running");

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // shutting down
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
        await base.StopAsync(cancellationToken);
    }
}