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

using System.ComponentModel;
using System.Runtime.CompilerServices;
using AraonMC.Core.Domain.Enums;

namespace AraonMC.Core.Domain.Entities;

/// <summary>
/// Login identity (Microsoft / third-party / offline) — frontend-display DTO. Display fields only;
/// secrets live in <see cref="StoredAccount"/>.
/// </summary>
public sealed class MinecraftAccount : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _uuid = "";
    private string _username = "";
    private AccountType _accountType;
    private bool _isOnline;
    private bool _isActive;
    private string _avatarKey = "";
    private string _serverUrl = "";

    /// <summary>Stable account key (also the <c>active_account_id</c> config value).</summary>
    public string Uuid { get => _uuid; set => Set(ref _uuid, value); }

    public string Username { get => _username; set => Set(ref _username, value); }

    public AccountType AccountType { get => _accountType; set => Set(ref _accountType, value); }

    /// <summary>True for server-backed accounts (Microsoft / ThirdParty); false for Offline.</summary>
    public bool IsOnline { get => _isOnline; set => Set(ref _isOnline, value); }

    public bool IsActive { get => _isActive; set => Set(ref _isActive, value); }

    public string AvatarKey { get => _avatarKey; set => Set(ref _avatarKey, value); }

    /// <summary>ThirdParty auth-server base URL (shown as a label); empty otherwise.</summary>
    public string ServerUrl { get => _serverUrl; set => Set(ref _serverUrl, value); }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
