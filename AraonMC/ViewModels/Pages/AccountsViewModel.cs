using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AraonMC.Core.Application.Ports;
using AraonMC.Core.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AraonMC.ViewModels.Pages;

public partial class AccountsViewModel : PageViewModelBase
{
    private readonly IAccountService _service;

    public AccountsViewModel(IAccountService service)
    {
        _service = service;
        Title = "Accounts";
        Items = new ObservableCollection<MinecraftAccount>(service.GetAccounts());
    }

    public ObservableCollection<MinecraftAccount> Items { get; }

    [RelayCommand]
    private async Task AddMicrosoftAsync()
    {
        try { var a = await _service.LoginMicrosoftAsync(); Items.Add(a); }
        catch (NotImplementedException) { /* backend pending */ }
    }

    [RelayCommand]
    private async Task AddOfflineAsync()
    {
        try { var a = await _service.AddOfflineAsync("Player"); Items.Add(a); }
        catch (NotImplementedException) { /* backend pending */ }
    }

    [RelayCommand]
    private async Task SetActiveAsync(MinecraftAccount? account)
    {
        if (account is null) return;
        foreach (var a in Items) a.IsActive = a == account;
        try { await _service.SetActiveAsync(account); }
        catch (NotImplementedException) { /* backend pending */ }
    }

    [RelayCommand]
    private async Task RemoveAsync(MinecraftAccount? account)
    {
        if (account is null) return;
        try { await _service.RemoveAsync(account); Items.Remove(account); }
        catch (NotImplementedException) { /* backend pending */ }
    }
}
