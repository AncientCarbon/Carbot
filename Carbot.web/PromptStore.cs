using Microsoft.EntityFrameworkCore;

namespace Carbot;

public class PromptStore
{
    private readonly IDbContextFactory<CarbotDbContext> _dbFactory;
    private readonly Random _random = new();

    public PromptStore(IDbContextFactory<CarbotDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<string>> GetTruthsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Prompts
            .Where(p => p.Type == PromptType.Truth)
            .OrderBy(p => p.Id)
            .Select(p => p.Text)
            .ToListAsync();
    }

    public async Task<List<string>> GetDaresAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Prompts
            .Where(p => p.Type == PromptType.Dare)
            .OrderBy(p => p.Id)
            .Select(p => p.Text)
            .ToListAsync();
    }

    public async Task<string> GetRandomTruthAsync()
    {
        var truths = await GetTruthsAsync();
        if (truths.Count == 0) return "No truths yet.";
        return truths[_random.Next(truths.Count)];
    }

    public async Task<string> GetRandomDareAsync()
    {
        var dares = await GetDaresAsync();
        if (dares.Count == 0) return "No dares yet.";
        return dares[_random.Next(dares.Count)];
    }

    public async Task AddTruthAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Prompts.Add(new Prompt
        {
            Type = PromptType.Truth,
            Text = text
        });
        await db.SaveChangesAsync();
    }

    public async Task AddDareAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Prompts.Add(new Prompt
        {
            Type = PromptType.Dare,
            Text = text
        });
        await db.SaveChangesAsync();
    }

    public async Task<bool> RemoveTruthAsync(int index)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var truth = await db.Prompts
            .Where(p => p.Type == PromptType.Truth)
            .OrderBy(p => p.Id)
            .Skip(index)
            .Take(1)
            .FirstOrDefaultAsync();

        if (truth is null) return false;

        db.Prompts.Remove(truth);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveDareAsync(int index)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var dare = await db.Prompts
            .Where(p => p.Type == PromptType.Dare)
            .OrderBy(p => p.Id)
            .Skip(index)
            .Take(1)
            .FirstOrDefaultAsync();

        if (dare is null) return false;

        db.Prompts.Remove(dare);
        await db.SaveChangesAsync();
        return true;
    }
}
