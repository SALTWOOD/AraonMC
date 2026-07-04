namespace AraonMC.LaunchArgs.Version;

/// <summary>合并 <see cref="VersionMetadata.InheritsFrom"/> 继承链（模组版本继承原版）。</summary>
public static class VersionMerger
{
    // TODO: libraries 同名子覆盖父，arguments 先父后子拼接，标量子覆盖。

    /// <param name="chain">从根父版本到目标版本的有序链。</param>
    public static VersionMetadata Merge(IReadOnlyList<VersionMetadata> chain)
        => throw new NotImplementedException();
}
