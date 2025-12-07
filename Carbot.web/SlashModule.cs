using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Carbot;

public class SlashModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PromptStore _prompts;

    private static readonly ulong[] OwnerIds = { 196629088151011328 }; // ancientcarbon
    public SlashModule(PromptStore prompts)
    {
        _prompts = prompts;
        
    }
    private bool IsAdmin()
    {
        if (OwnerIds.Contains(Context.User.Id))
            return true;
        if (Context.User is SocketGuildUser guildUser)
        {
            if (guildUser.GuildPermissions.Administrator)
                return true;
            if (guildUser.Roles.Any(r => r.Name == "Carbot Admin"))
                return true;
        }
        return false;
    }
    
    [SlashCommand("ping", "Check if carbot is alive")]
    public async Task Ping() => await RespondAsync("Pong!");

    [SlashCommand("truth", "Carbot will ask you a question")]
    public async Task Truth()
    {
        var pick = await _prompts.GetRandomTruthAsync();
        await RespondAsync(pick);
    }

    [SlashCommand("dare", "Carbot will give you a dare")]
    public async Task Dare()
    {
        var pick = await _prompts.GetRandomDareAsync();
        await RespondAsync(pick);
    }

    [SlashCommand("addtruth", "Add a new truth prompt")]
    public async Task AddTruth(string text)
    {
        if (!IsAdmin())
        {
            await RespondAsync("You don't have permission to add prompts.", ephemeral: true);
            return;
        }
        await _prompts.AddTruthAsync(text);
        await RespondAsync($"Added new truth: {text}", ephemeral: true);
    }

    [SlashCommand("adddare", "Add a new dare prompt")]
    public async Task AddDare(string text)
    {
        if (!IsAdmin())
        {
            await RespondAsync("You don't have permission to add prompts.", ephemeral: true);
            return;
        }
        await _prompts.AddDareAsync(text);
        await RespondAsync($"Added new dare: {text}", ephemeral: true);
    }

    [SlashCommand("removetruth", "Remove a truth by its index (see /truths on the web)")]
    public async Task RemoveTruth(int index)
    {
        if (!IsAdmin())
        {
            await RespondAsync("You don't have permission to remove prompts.", ephemeral: true);
            return;
        }
        var ok = await _prompts.RemoveTruthAsync(index - 1);

        if (!ok)
        {
            await RespondAsync($"Invalid truth index: {index}", ephemeral: true);
            return;
        }
        await RespondAsync($"Removed truth at index {index}", ephemeral: true);
    }

    [SlashCommand("removedare", "Remove a dare by its index (see /dares on the web)")]
    public async Task RemoveDare(int index)
    {
        if (!IsAdmin())
        {
            await RespondAsync("You don't have permission to remove prompts.", ephemeral: true);
            return;
        }
        var ok = await _prompts.RemoveDareAsync(index - 1);

        if (!ok)
        {
            await RespondAsync($"Invalid dare index: {index}", ephemeral: true);
            return;
        }
        await RespondAsync($"Removed dare at index {index}", ephemeral: true);
    }
}