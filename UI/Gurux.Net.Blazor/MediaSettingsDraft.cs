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
using Gurux.Net;

namespace Gurux.Net.Blazor;
/// <summary>Owns a private unopened media copy until validated settings are committed.</summary>
public sealed class MediaSettingsDraft : IDisposable
{
    /// <summary>Tracks ownership and concurrent source changes.</summary>
    private readonly MediaSettingsSession _session;
    /// <summary>Builds and applies staged controls.</summary>
    private readonly SettingsFields _controls = new();
    /// <summary>Gets the staged controls in display order.</summary>
    public List<SettingsField> Fields => _controls.Fields;
    /// <summary>Creates an owned transaction over an unopened media copy.</summary>
    /// <param name="source">The caller-owned media.</param>
    /// <param name="copy">The editor-owned media copy.</param>
    private MediaSettingsDraft(IGXMedia source, IGXMedia copy) { _session = new(source, copy); }
    /// <summary>Copies settings without opening or taking ownership of the source.</summary>
    /// <param name="source">The closed media to edit.</param>
    /// <returns>An independently owned settings draft.</returns>
    /// <exception cref="ArgumentException">The media is not the exact supported type.</exception>
    public static MediaSettingsDraft Create(IGXMedia source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.GetType() != typeof(GXNet)) { throw new ArgumentException(Properties.Resources.UnsupportedMedia, nameof(source)); }
        var copy = new GXNet();
        var result = new MediaSettingsDraft(source, copy);
        try
        {
            var settings = (AvailableMediaSettings)((GXNet)source).ConfigurableSettings;
            var address = new UI.HostAddressState(copy.Server, copy.UseIPv6, copy.HostName);
            SettingsField? server = null;
            if (settings.HasFlag(AvailableMediaSettings.Server))
            {
                result._controls.Toggle(Properties.Resources.Server, copy.Server, value =>
                {
                    copy.Server = value;
                    if (!value) { copy.MaxClientCount = ((GXNet)source).MaxClientCount; }
                });
                server = result.Fields[^1];
                server.IsSwitch = true;
                server.Changed = value => address.Server = value;
            }
            if (settings.HasFlag(AvailableMediaSettings.Protocol))
            {
                result._controls.Choice(Properties.Resources.Protocol, copy.Protocol, value => copy.Protocol = value);
            }
            if (settings.HasFlag(AvailableMediaSettings.Host))
            {
                result._controls.Text(Properties.Resources.Hostname, copy.HostName, value => copy.HostName = value);
                result.Fields[^1].Address = address;
            }
            if (settings.HasFlag(AvailableMediaSettings.Port))
            {
                result._controls.Number(Properties.Resources.Port, copy.Port, value => copy.Port = value);
            }
            if (settings.HasFlag(AvailableMediaSettings.UseIPv6))
            {
                result._controls.Toggle(Properties.Resources.IPv6, copy.UseIPv6, value => copy.UseIPv6 = value);
                result.Fields[^1].Changed = value => address.UseIPv6 = value;
            }
            if (settings.HasFlag(AvailableMediaSettings.Server) && settings.HasFlag(AvailableMediaSettings.MaxClientCount))
            {
                result._controls.Number(Properties.Resources.Maximumclients, copy.MaxClientCount, value => copy.MaxClientCount = value);
                result.Fields[^1].IsDisabled = () => !(server?.Checked ?? copy.Server);
            }
            if (settings.HasFlag(AvailableMediaSettings.WaitTime))
            {
                result._controls.Number(Properties.Resources.Waittimemilliseconds, copy.WaitTime, value => copy.WaitTime = value);
            }
            if (settings.HasFlag(AvailableMediaSettings.EnableTcpKeepAlive))
            {
                result._controls.Toggle(Properties.Resources.TCPkeepalive, copy.EnableTcpKeepAlive, value => copy.EnableTcpKeepAlive = value);
            }
            if (settings.HasFlag(AvailableMediaSettings.TcpKeepAliveIdleSeconds))
            {
                result._controls.Number(Properties.Resources.TCPkeepaliveidleseconds, copy.TcpKeepAliveIdleSeconds, value => copy.TcpKeepAliveIdleSeconds = value);
            }
            if (settings.HasFlag(AvailableMediaSettings.TcpKeepAliveIntervalSeconds))
            {
                result._controls.Number(Properties.Resources.TCPkeepaliveintervalseconds, copy.TcpKeepAliveIntervalSeconds, value => copy.TcpKeepAliveIntervalSeconds = value);
            }
            if (settings.HasFlag(AvailableMediaSettings.TcpKeepAliveRetryCount))
            {
                result._controls.Number(Properties.Resources.TCPkeepaliveretries, copy.TcpKeepAliveRetryCount, value => copy.TcpKeepAliveRetryCount = value);
            }

            return result;
        }
        catch { result.Dispose(); throw; }
    }
    /// <summary>Validates staged settings and applies them if the closed source has not changed.</summary>
    /// <exception cref="InvalidOperationException">The source opened or changed during editing.</exception>
    /// <exception cref="ObjectDisposedException">The draft was already discarded.</exception>
    public void Commit() { _session.Commit(_controls.Apply); }
    /// <summary>Discards and disposes the private copy without disposing the caller's media.</summary>
    public void Dispose()
    {
        foreach (var field in Fields) { if (field.Kind == "password") { field.Value = string.Empty; } }
        _session.Dispose();
    }
}
