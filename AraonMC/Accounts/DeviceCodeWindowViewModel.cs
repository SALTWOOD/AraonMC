using AraonMC.Auth;

namespace AraonMC.Accounts;

/// <summary>View data for <see cref="DeviceCodeWindow"/>: the user code and verification URL.</summary>
public sealed class DeviceCodeWindowViewModel
{
    public DeviceCodeWindowViewModel(DeviceCodeInfo info)
    {
        UserCode = info.UserCode;
        VerificationUrl = info.VerificationUrl;
    }

    /// <summary>The short code the user enters at the verification page (e.g. ABCD-EFGH).</summary>
    public string UserCode { get; }

    /// <summary>The verification page URL (typically https://microsoft.com/link).</summary>
    public string VerificationUrl { get; }
}
