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
using Gurux.Common;
using Microsoft.Maui.Controls;
using Gurux.Net.Maui.Properties;

namespace Gurux.Net.Maui;

/// <summary>Connects native controls to an unopened private media copy.</summary>
internal sealed class MediaSettingsDraft : IDisposable
{
    /// <summary>Owns the staged copy and conflict checks.</summary>
    private readonly MediaSettingsSession _session;
    /// <summary>Reads this media's native fields.</summary>
    private readonly SettingsFields _fields;
    /// <summary>Reads reused serial fields when this media has a port.</summary>
    private readonly Action? _readSerial = null;
    /// <summary>Gets the native form layout.</summary>
    internal View Form => _fields.Layout;
    /// <summary>Copies settings and creates fields without opening a transport.</summary>
    /// <param name="source">The closed media whose settings will be staged.</param>
    internal MediaSettingsDraft(GXNet source)
    {
        var copy = new GXNet();
        _session = new MediaSettingsSession(source, copy);
        _fields = new SettingsFields();
        try
        {
            SettingsFields fields = _fields;
            var allowed = (AvailableMediaSettings)source.ConfigurableSettings;
            var address = new UI.HostAddressState(copy.Server, copy.UseIPv6, copy.HostName);
            HostAddressControl? host = null;
            View? maxClients = null;
            if (allowed.HasFlag(AvailableMediaSettings.Server))
            {
                fields.Toggle(Resources.Server, copy.Server, value =>
                {
                    copy.Server = value;
                    if (!value) { copy.MaxClientCount = source.MaxClientCount; }
                }, value =>
                {
                    address.Server = value;
                    host?.Refresh();
                    if (maxClients is not null)
                    {
                        maxClients.IsEnabled = value;
                    }
                });
            }
            if (allowed.HasFlag(AvailableMediaSettings.Protocol))
            {
                fields.Choice(Resources.Protocol, copy.Protocol, value => copy.Protocol = value);
            }
            if (allowed.HasFlag(AvailableMediaSettings.Host))
            {
                host = fields.HostAddress(address, value => copy.HostName = value);
            }
            if (allowed.HasFlag(AvailableMediaSettings.Port))
            {
                fields.Number(Resources.Port, copy.Port, value => copy.Port = value);
            }
            if (allowed.HasFlag(AvailableMediaSettings.UseIPv6))
            {
                fields.Toggle(Resources.IPv6, copy.UseIPv6, value => copy.UseIPv6 = value,
                    value => { address.UseIPv6 = value; host?.Refresh(); });
            }
            if (allowed.HasFlag(AvailableMediaSettings.Server | AvailableMediaSettings.MaxClientCount))
            {
                fields.Number(Resources.Clients, copy.MaxClientCount, value => copy.MaxClientCount = value);
                maxClients = (View)fields.Layout.Children[^1];
                maxClients.IsEnabled = copy.Server;
            }
            if (allowed.HasFlag(AvailableMediaSettings.WaitTime))
            {
                fields.Number(Resources.Wait, copy.WaitTime, value => copy.WaitTime = value);
            }
            if (allowed.HasFlag(AvailableMediaSettings.EnableTcpKeepAlive))
            {
                fields.Toggle(Resources.KeepAlive, copy.EnableTcpKeepAlive, value => copy.EnableTcpKeepAlive = value);
            }
            if (allowed.HasFlag(AvailableMediaSettings.TcpKeepAliveIdleSeconds))
            {
                fields.Number(Resources.Idle, copy.TcpKeepAliveIdleSeconds, value => copy.TcpKeepAliveIdleSeconds = value);
            }
            if (allowed.HasFlag(AvailableMediaSettings.TcpKeepAliveIntervalSeconds))
            {
                fields.Number(Resources.Interval, copy.TcpKeepAliveIntervalSeconds, value => copy.TcpKeepAliveIntervalSeconds = value);
            }
            if (allowed.HasFlag(AvailableMediaSettings.TcpKeepAliveRetryCount))
            {
                fields.Number(Resources.Retries, copy.TcpKeepAliveRetryCount, value => copy.TcpKeepAliveRetryCount = value);
            }
        }
        catch { _session.Dispose(); throw; }
    }
    /// <summary>Reads native controls and saves only after validation and conflict checks.</summary>
    internal void Commit() => _session.Commit(() => { _readSerial?.Invoke(); _fields.Read(); });
    /// <summary>Releases the unopened private media instance.</summary>
    public void Dispose() => _session.Dispose();
}
