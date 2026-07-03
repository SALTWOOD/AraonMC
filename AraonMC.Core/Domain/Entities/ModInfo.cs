namespace AraonMC.Core.Domain.Entities;

/// <summary>
/// A browsable mod / modpack entry shown on the Mods page.
/// </summary>
public sealed class ModInfo
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public long Downloads { get; set; }
    public string IconKey { get; set; } = string.Empty;
    public bool Installed { get; set; }
}
