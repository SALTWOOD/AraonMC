namespace AraonMC.Core.Domain.Enums;

/// <summary>
/// The hosting platform a single <see cref="Entities.ResourceInfo"/> entry originated from. Used to badge
/// cards in the browser. Distinct from <see cref="ResourceSourceFilter"/>, which is a query selector.
/// </summary>
public enum ResourceSource
{
    Modrinth,
    CurseForge,
}
