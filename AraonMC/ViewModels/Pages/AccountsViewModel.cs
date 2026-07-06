using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AraonMC.Accounts;
using AraonMC.Auth;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Application.Ports;
using AraonMC.Core.Domain.Entities;
using CommunityToolkit.Mvvm.Input;

namespace AraonMC.ViewModels.Pages;

public partial class AccountsViewModel : PageViewModelBase
{
    private readonly IAccountService _service;
    private readonly INotificationService _notifications;

    public AccountsViewModel(IAccountService service, INotificationService notifications)
    {
        _service = service;
        _notifications = notifications;
        Title = "Accounts";
        // Bind the service-owned live list directly so add/remove/active-switch reflect everywhere
        // (the top-bar switcher shares the same instance).
        Items = service.Accounts;
    }

    public ObservableCollection<MinecraftAccount> Items { get; }

    [RelayCommand]
    private async Task AddMicrosoftAsync()
    {
        DebugLog.Info("Accounts: 'Add Microsoft' pressed; starting device-code login.");
        try { await _service.LoginMicrosoftAsync(); }
        catch (OperationCanceledException) { DebugLog.Info("Accounts: Microsoft login cancelled by the user."); /* user cancelled the device-code flow */ }
        catch (MinecraftAuthException ex) { DebugLog.Warn($"Accounts: Microsoft login rejected ({ex.Kind}) — {ex.Message}."); await NotifyAuthErrorAsync(ex); }
        catch (Exception ex) { DebugLog.Error($"Accounts: Microsoft login threw {ex.GetType().Name}: {ex.Message}"); await NotifyAsync("Login failed", ex.Message, NotificationLevel.Error); }
    }

    [RelayCommand]
    private async Task AddOfflineAsync()
    {
        var name = await InputDialog.PromptAsync("Add offline account", "Username");
        if (string.IsNullOrWhiteSpace(name))
        {
            DebugLog.Info("Accounts: offline-add cancelled at the name prompt.");
            return;
        }
        DebugLog.Info($"Accounts: adding offline account '{name.Trim()}'.");
        try { await _service.AddOfflineAsync(name); }
        catch (Exception ex) { DebugLog.Error($"Accounts: offline add failed — {ex.GetType().Name}: {ex.Message}"); await NotifyAsync("Add failed", ex.Message, NotificationLevel.Error); }
    }

    [RelayCommand]
    private async Task SetActiveAsync(MinecraftAccount? account)
    {
        if (account is null) return;
        DebugLog.Info($"Accounts: setting active account → '{account.Username}'.");
        await _service.SetActiveAsync(account);
    }

    [RelayCommand]
    private async Task RemoveAsync(MinecraftAccount? account)
    {
        if (account is null) return;
        DebugLog.Info($"Accounts: removing account '{account.Username}'.");
        await _service.RemoveAsync(account);
    }

    private async Task NotifyAuthErrorAsync(MinecraftAuthException ex)
    {
        var level = ex.Kind is AuthErrorKind.NotPurchased
            or AuthErrorKind.XboxBanned
            or AuthErrorKind.ProfileNotCreated
            or AuthErrorKind.RegionBlocked
            or AuthErrorKind.Underage
                ? NotificationLevel.Error
                : NotificationLevel.Warning;
        var msg = string.IsNullOrEmpty(ex.HelpUrl) ? ex.Message : $"{ex.Message}\n\n{ex.HelpUrl}";
        await NotifyAsync("Login failed", msg, level);
    }

    private async Task NotifyAsync(string title, string message, NotificationLevel level)
        => await _notifications.ShowAsync(NotificationRequest.Dialog(title, message, level));
}
