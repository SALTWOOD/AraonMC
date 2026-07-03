namespace AraonMC.Core.Domain.Entities;

/// <summary>
/// A news / update entry shown on the Home page.
/// </summary>
public sealed class NewsItem
{
    public string Title { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public string? Link { get; set; }
}
