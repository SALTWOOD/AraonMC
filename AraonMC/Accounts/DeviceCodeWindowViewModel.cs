// Copyright (C) 2026 SALTWOOD and contributors
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

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
