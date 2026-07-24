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

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace AraonMC.Controls;

/// <summary>
/// An <see cref="Image"/> that loads its <see cref="Url"/> asynchronously and caches the decoded bitmap
/// process-wide. Renders nothing (transparent) until the bitmap arrives, so it can be layered over a
/// fallback element (e.g. initials) that shows through in the meantime. Network/decode failures and empty
/// URLs leave <see cref="Image.Source"/> null (the fallback remains visible). Byte fetching and bitmap
/// decoding run off the UI thread; <see cref="Image.Source"/> is assigned back on it.
/// </summary>
/// <remarks>
/// The cache is unbounded (one bitmap per distinct URL per session); thumbnails are small and the working
/// set is bounded by what the user actually browses, so this is fine for a launcher.
/// </remarks>
public sealed class RemoteImage : Image
{
    public static readonly StyledProperty<string?> UrlProperty =
        AvaloniaProperty.Register<RemoteImage, string?>(nameof(Url));

    public static readonly StyledProperty<bool> IsLoadedProperty =
        AvaloniaProperty.Register<RemoteImage, bool>(nameof(IsLoaded));

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

    // null entry = a URL known to have failed, so we don't refetch on every scroll.
    private static readonly ConcurrentDictionary<string, Bitmap?> Cache = new();

    private CancellationTokenSource? _cts;

    static RemoteImage()
    {
        // Re-run UrlChanged for every instance whenever Url changes.
        UrlProperty.Changed.AddClassHandler<RemoteImage>((img, e) => img.OnUrlChanged(e));
    }

    public string? Url
    {
        get => GetValue(UrlProperty);
        set => SetValue(UrlProperty, value);
    }

    public new bool IsLoaded
    {
        get => GetValue(IsLoadedProperty);
        private set => SetValue(IsLoadedProperty, value);
    }

    private void OnUrlChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // Clear the displayed bitmap; it's set again when the new URL resolves (or stays null on failure).
        Source = null;
        IsLoaded = false;
        _cts?.Cancel();
        var url = (string?)e.NewValue;
        if (string.IsNullOrWhiteSpace(url)) return;

        _cts = new CancellationTokenSource();
        _ = LoadAsync(url, _cts.Token);
    }

    private async Task LoadAsync(string url, CancellationToken ct)
    {
        Bitmap? bmp;
        try
        {
            if (!Cache.TryGetValue(url, out bmp))
            {
                var bytes = await Http.GetByteArrayAsync(url, ct).ConfigureAwait(false);
                using var ms = new MemoryStream(bytes);
                bmp = new Bitmap(ms); // raster decode; safe off the UI thread.
                Cache[url] = bmp;
            }
        }
        catch (Exception ex)
        {
            DebugLog.Warn($"RemoteImage: failed to load '{url}' — {ex.GetType().Name}: {ex.Message}");
            Cache.TryAdd(url, null); // remember the failure
            return;
        }

        if (ct.IsCancellationRequested || bmp is null) return;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!ct.IsCancellationRequested)
            {
                Source = bmp;
                IsLoaded = true;
            }
        });
    }
}
