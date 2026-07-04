namespace AraonMC.LaunchArgs;

/// <summary>构建出的启动命令。由调用方用 <c>Process</c> 执行，本库不拉起进程。</summary>
public sealed class LaunchCommand
{
    public string JavaExecutable { get; init; } = string.Empty;
    public IReadOnlyList<string> JvmArguments { get; init; } = [];
    public string MainClass { get; init; } = string.Empty;
    public IReadOnlyList<string> GameArguments { get; init; } = [];

    /// <summary>java 之后的全部参数，可直接喂给 <c>ProcessStartInfo.ArgumentList</c>。</summary>
    public IEnumerable<string> Arguments
    {
        get
        {
            foreach (var a in JvmArguments) yield return a;
            yield return MainClass;
            foreach (var a in GameArguments) yield return a;
        }
    }

    public override string ToString() => $"{JavaExecutable} {string.Join(' ', Arguments)}";
}
