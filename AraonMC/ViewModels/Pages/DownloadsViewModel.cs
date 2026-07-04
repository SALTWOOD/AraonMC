using System.Collections.ObjectModel;
using AraonMC.Downloads;
using CommunityToolkit.Mvvm.Input;

namespace AraonMC.ViewModels.Pages;

public partial class DownloadsViewModel : PageViewModelBase
{
    private readonly IDownloadManager _manager;

    public DownloadsViewModel(IDownloadManager manager)
    {
        _manager = manager;
        Title = "Downloads";
    }

    public ObservableCollection<DownloadJob> Jobs => _manager.Jobs;

    [RelayCommand]
    private void Cancel(DownloadJob? job)
    {
        if (job is not null) _manager.Cancel(job);
    }

    [RelayCommand]
    private void ClearFinished() => _manager.ClearFinished();
}
