namespace AraonMC.Core.Domain.Entities;

/// <summary>
/// A login identity (Microsoft online or offline profile).
/// </summary>
public sealed class MinecraftAccount
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Uuid { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Avatar key/initial used by the UI; no real skin fetch in this build.</summary>
    public string AvatarKey { get; set; } = string.Empty;
}
