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
