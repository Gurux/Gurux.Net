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

using System;

namespace Gurux.Net
{
    /// <summary>Controls which network settings are offered by optional media settings editors.</summary>
    /// <remarks>Assign a combination of these flags to <see cref="Gurux.Common.IGXMedia.ConfigurableSettings"/>.
    /// Hidden settings retain their configured values. Server-specific controls additionally require <see cref="Server"/>.</remarks>
    [Flags]
    public enum AvailableMediaSettings
    {
        /// <summary>No network settings are shown.</summary>
        None = 0,
        /// <summary>All network settings are shown.</summary>
        All = -1,
        /// <summary>The server switch and permitted server-specific settings are shown.</summary>
        Server = 0x1,
        /// <summary>The TCP or UDP protocol selector is shown.</summary>
        Protocol = 0x2,
        /// <summary>The network port number is shown.</summary>
        Port = 0x4,
        /// <summary>The remote host or editable local listen address is shown, according to connection mode.</summary>
        Host = 0x8,
        /// <summary>The IPv6 selector is shown.</summary>
        UseIPv6 = 0x10,
        /// <summary>The maximum client count is shown when <see cref="Server"/> is also permitted.</summary>
        MaxClientCount = 0x20,
        /// <summary>The connection wait time is shown.</summary>
        WaitTime = 0x40,
        /// <summary>The TCP keepalive selector is shown.</summary>
        EnableTcpKeepAlive = 0x80,
        /// <summary>The TCP keepalive idle time is shown.</summary>
        TcpKeepAliveIdleSeconds = 0x100,
        /// <summary>The TCP keepalive probe interval is shown.</summary>
        TcpKeepAliveIntervalSeconds = 0x200,
        /// <summary>The TCP keepalive retry count is shown.</summary>
        TcpKeepAliveRetryCount = 0x400
    }
}
