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
        if (job is not null)
        {
            DebugLog.Info($"Downloads: user cancelled job '{job.Title}' (id={job.Id}).");
            _manager.Cancel(job);
        }
    }

    [RelayCommand]
    private void ClearFinished()
    {
        DebugLog.Info("Downloads: user clicked 'clear finished'.");
        _manager.ClearFinished();
    }
}
