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

using Gurux.Net.Enums;
namespace Gurux.Net;

/// <summary>Stores portable network settings for source-generated JSON serialization.</summary>
internal sealed class NetSettings
{
    /// <summary>Gets or sets the TCP or UDP transport selection.</summary>
    public NetworkType Protocol { get; set; } = NetworkType.Tcp;
    /// <summary>Gets or sets the remote host or local bind address.</summary>
    public string HostName { get; set; } = string.Empty;
    /// <summary>Gets or sets the remote or listening port.</summary>
    public int Port { get; set; } = 0;
    /// <summary>Gets or sets whether the media listens for clients.</summary>
    public bool Server { get; set; } = false;
    /// <summary>Gets or sets whether name resolution and wildcard binding use IPv6.</summary>
    public bool UseIPv6 { get; set; } = false;
    /// <summary>Gets or sets the maximum admitted TCP client count.</summary>
    public int MaxClientCount { get; set; } = 0;
    /// <summary>Gets or sets the connection timeout in milliseconds.</summary>
    public int WaitTime { get; set; } = 60000;
    /// <summary>Gets or sets whether TCP keepalive probes are enabled.</summary>
    public bool EnableTcpKeepAlive { get; set; } = true;
    /// <summary>Gets or sets the idle interval before TCP keepalive probes.</summary>
    public int TcpKeepAliveIdleSeconds { get; set; } = 60;
    /// <summary>Gets or sets the interval between TCP keepalive probes.</summary>
    public int TcpKeepAliveIntervalSeconds { get; set; } = 10;
    /// <summary>Gets or sets the TCP keepalive retry limit.</summary>
    public int TcpKeepAliveRetryCount { get; set; } = 3;
}
