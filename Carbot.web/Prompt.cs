namespace Carbot;

public enum PromptType
{
    Truth = 0,
    Dare = 1
}

public class Prompt
{
    public int Id { get; set; }
    public PromptType Type { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
