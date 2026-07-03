namespace AraonMC.Auth;

/// <summary>
///     设备代码流申请到的设备码信息，由 <c>/devicecode</c> 端点返回。
/// </summary>
public sealed class DeviceCodeInfo
{
    public required string UserCode { get; init; }

    public required string DeviceCode { get; init; }

    public required string VerificationUrl { get; init; }

    public string? DirectVerificationUrl { get; init; }

    public int ExpiresIn { get; init; }

    public int Interval { get; init; }
}
