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

public interface IDeviceCodeUI
{
    /// <summary>
    ///     向用户展示设备码信息并阻塞等待。
    ///     <para>
    ///         模块会同时在后台轮询 token；当轮询拿到 token 或出错时，会取消传入的
    ///         <paramref name="cancellationToken" />，实现应据此关闭登录提示界面并让本方法返回。
    ///     </para>
    /// </summary>
    /// <param name="info">设备码信息。</param>
    /// <param name="cancellationToken">
    ///     轮询完成后被取消。实现内部应捕获 <see cref="OperationCanceledException" /> 平滑退出。
    /// </param>
    Task DisplayAsync(DeviceCodeInfo info, CancellationToken cancellationToken);
}
