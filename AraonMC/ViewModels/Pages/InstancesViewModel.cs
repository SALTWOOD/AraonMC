using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AraonMC.Accounts;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Application.Ports;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AraonMC.ViewModels.Pages;

public partial class InstancesViewModel : PageViewModelBase
{
    private readonly IInstanceRepository _repo;
    private readonly IGameLauncher _launcher;
    private readonly IAccountService _accounts;
    private readonly INotificationService _notifications;
    private readonly Action _showVersionSelect;
    private readonly List<GameInstance> _all;

    public InstancesViewModel(
        IInstanceRepository repo,
        IGameLauncher launcher,
        IAccountService accounts,
        INotificationService notifications,
        Action showVersionSelect)
    {
        _repo = repo;
        _launcher = launcher;
        _accounts = accounts;
        _notifications = notifications;
        _showVersionSelect = showVersionSelect;
        _all = [.. repo.GetAll()];

        Title = "Instances";
        Items = new ObservableCollection<GameInstance>(_all);
    }

    public ObservableCollection<GameInstance> Items { get; }

    [ObservableProperty] private string _searchText = string.Empty;

    [RelayCommand]
    private async Task PlayAsync(GameInstance? instance)
    {
        if (instance is null) return;
        var active = _accounts.GetActive()!;
        DebugLog.Info($"Instances: Play '{instance.Name}' (version='{instance.MinecraftVersion}') with account '{active?.Username ?? "(none)"}'.");
        await _launcher.LaunchAsync(instance, active);
    }

    [RelayCommand]
    private void New()
    {
        DebugLog.Info("Instances: 'New instance' pressed; opening version select.");
        _showVersionSelect();
    }

    [RelayCommand]
    private async Task RenameAsync(GameInstance? instance)
    {
        if (instance is null) return;

        var newName = await InputDialog.PromptAsync("Rename instance", "New name", instance.Name);
        if (string.IsNullOrWhiteSpace(newName) || newName == instance.Name) return;

        try
        {
            var oldName = instance.Name;
            await _repo.RenameAsync(instance, newName.Trim());
            ApplyFilter(); // GameInstance isn't INPC, so re-bind the card to show the new folder/name.
            DebugLog.Info($"Instances: renamed '{oldName}' → '{instance.Name}'.");
            await _notifications.ShowAsync(NotificationRequest.Toast(
                "Instance renamed", instance.Name, NotificationLevel.Info));
        }
        catch (Exception ex)
        {
            DebugLog.Error($"Instances: rename failed — {ex.Message}");
            await _notifications.ShowAsync(NotificationRequest.Toast(
                "Couldn't rename", ex.Message, NotificationLevel.Error));
        }
    }

    [RelayCommand]
    private async Task RemoveAsync(GameInstance? instance)
    {
        if (instance is null) return;

        var ok = await ConfirmDialog.ShowAsync(
            "Remove instance?",
            $"Remove '{instance.Name}'? This deletes its version files and can't be undone.",
            "Remove");
        if (!ok) return;

        try
        {
            await _repo.DeleteAsync(instance);
            _all.Remove(instance);
            ApplyFilter();
            DebugLog.Info($"Instances: removed '{instance.Name}'.");
            await _notifications.ShowAsync(NotificationRequest.Toast(
                "Instance removed", instance.Name, NotificationLevel.Info));
        }
        catch (Exception ex)
        {
            DebugLog.Error($"Instances: remove failed — {ex.Message}");
            await _notifications.ShowAsync(NotificationRequest.Toast(
                "Couldn't remove", ex.Message, NotificationLevel.Error));
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    /// <summary>Rebuilds <see cref="Items"/> from <c>_all</c>, filtered by the current search text.</summary>
    private void ApplyFilter()
    {
        var q = SearchText.Trim();
        Items.Clear();
        foreach (var i in _all.Where(x => x.Name.Contains(q, StringComparison.OrdinalIgnoreCase)))
            Items.Add(i);
    }
}
