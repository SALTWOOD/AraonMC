using Avalonia.Controls;
using Avalonia.Input;
using AraonMC.Core.Domain.Entities;
using AraonMC.ViewModels.Pages;

namespace AraonMC.Views.Pages;

public partial class BrowseView : UserControl
{
    public BrowseView()
    {
        InitializeComponent();
    }

    private void ResourceCard_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border) return;

        var point = e.GetCurrentPoint(border);
        if (!point.Properties.IsLeftButtonPressed) return;

        if (border.DataContext is ResourceInfo resource
            && DataContext is BrowseViewModel vm
            && vm.DetailCommand.CanExecute(resource))
        {
            vm.DetailCommand.Execute(resource);
        }
    }
}
