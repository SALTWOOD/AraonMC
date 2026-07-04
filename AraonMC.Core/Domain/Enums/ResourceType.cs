namespace AraonMC.Core.Domain.Enums;

/// <summary>
/// A kind of browsable Minecraft content. Each platform maps a subset of these to its own notion of
/// project type / section (see <c>Infrastructure.Catalog.CatalogMappings</c>); types a platform does not
/// carry simply yield no results from that platform.
/// </summary>
public enum ResourceType
{
    Mod,
    Modpack,
    ResourcePack,
    ShaderPack,
    WorldSave,
    DataPack,
}
