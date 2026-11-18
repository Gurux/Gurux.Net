// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Avalonia.Controls;
using Avalonia.Threading;
using Gurux.Net.Example.Properties;

namespace Gurux.Net.Example.Desktop;

/// <summary>Hosts the network sample controls and owns their connection lifetime.</summary>
public sealed class MediaExampleWindow : Window, IAsyncDisposable
{
    /// <summary>The TCP or UDP connection used by this window.</summary>
    private readonly ExampleSession _session = new();
    /// <summary>The compact network controls displayed in the window.</summary>
    private readonly NetworkExamplePanel _networkPanel;
    /// <summary>Cancels pending settings when the window closes.</summary>
    private readonly CancellationTokenSource _lifetime = new();
    /// <summary>Refreshes transport state without handling transport callbacks on the UI thread.</summary>
    private readonly DispatcherTimer _timer;
    /// <summary>Shares one asynchronous shutdown operation between close requests.</summary>
    private Task? _disposal;

    /// <summary>Creates a window for the network sample's own connection and settings.</summary>
    /// <remarks>Native close requests await shared cleanup and then close the window even when cleanup fails.</remarks>
    public MediaExampleWindow()
    {
        Title = ExampleResources.ApplicationTitle;
        ExampleLayout.ConfigureWindow(this);
        _networkPanel = new NetworkExamplePanel(_session,
            () => new AvaloniaMediaSettingsEditor(this).ShowAsync(_session.Media, cancellationToken: _lifetime.Token));
        Content = _networkPanel;
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Background, (_, _) => _networkPanel.Refresh());
        _timer.Start();
        _networkPanel.Refresh();
        Closing += async (_, args) =>
        {
            if (_disposal is null)
            {
                args.Cancel = true;
                try
                {
                    await DisposeAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError("{0}", ex);
                }
                finally
                {
                    // Finish the current native closing event before issuing the final close request.
                    Dispatcher.UIThread.Post(() => Close());
                }
            }
            else if (!_disposal.IsCompleted) { args.Cancel = true; }
        };
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => new(_disposal ??= DisposeCoreAsync());

    /// <summary>Stops UI updates, cancels background work and releases the transport.</summary>
    /// <returns>A task completing after all resources are released.</returns>
    private async Task DisposeCoreAsync()
    {
        _timer.Stop();
        _lifetime.Cancel();
        await _networkPanel.DisposeAsync();
        await _session.DisposeAsync();
        _lifetime.Dispose();
    }
}