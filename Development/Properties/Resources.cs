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

using System.Globalization;
using System.Resources;

namespace Gurux.Net.Properties;

/// <summary>Provides localized validation and connection failure messages.</summary>
internal static class Resources
{
    /// <summary>Resolves messages from the transport assembly for the current UI culture.</summary>
    private static readonly ResourceManager _manager = new("Gurux.Net.Properties.Resources", typeof(Resources).Assembly);
    /// <summary>Gets the localized message: A remote host is required.</summary>
    internal static string RemoteHostRequired => _manager.GetString(nameof(RemoteHostRequired), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: UDP preserves datagram boundaries; Eop framing is only supported for TCP.</summary>
    internal static string UdpFramingUnsupported => _manager.GetString(nameof(UdpFramingUnsupported), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: An attached media cannot reconnect. Attach a connected client instead.</summary>
    internal static string AttachedReconnect => _manager.GetString(nameof(AttachedReconnect), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: A lifecycle operation is already in progress.</summary>
    internal static string LifecycleInProgress => _manager.GetString(nameof(LifecycleInProgress), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: Client is not connected.</summary>
    internal static string ClientNotConnected => _manager.GetString(nameof(ClientNotConnected), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: Client is disconnected.</summary>
    internal static string ClientDisconnected => _manager.GetString(nameof(ClientDisconnected), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: Media is closed.</summary>
    internal static string MediaClosed => _manager.GetString(nameof(MediaClosed), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: A client address is required.</summary>
    internal static string ClientAddressRequired => _manager.GetString(nameof(ClientAddressRequired), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: A destination endpoint is required.</summary>
    internal static string DestinationRequired => _manager.GetString(nameof(DestinationRequired), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: Socket closed during send.</summary>
    internal static string SocketClosedDuringSend => _manager.GetString(nameof(SocketClosedDuringSend), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: Attach a client endpoint before receiving synchronously; listener data cannot be combined across clients.</summary>
    internal static string AttachBeforeReceive => _manager.GetString(nameof(AttachBeforeReceive), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: Expected GXNet.</summary>
    internal static string ExpectedNetworkMedia => _manager.GetString(nameof(ExpectedNetworkMedia), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: Close the media before changing settings.</summary>
    internal static string CloseBeforeSettings => _manager.GetString(nameof(CloseBeforeSettings), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: The network port must be between 1 and 65535.</summary>
    internal static string PortOutOfRange => _manager.GetString(nameof(PortOutOfRange), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: Select a supported network protocol.</summary>
    internal static string InvalidProtocol => _manager.GetString(nameof(InvalidProtocol), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: The maximum client count must be zero or greater.</summary>
    internal static string InvalidMaxClientCount => _manager.GetString(nameof(InvalidMaxClientCount), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: The connection timeout must be -1 or greater.</summary>
    internal static string InvalidWaitTime => _manager.GetString(nameof(InvalidWaitTime), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: The TCP keepalive idle time must be greater than zero.</summary>
    internal static string InvalidKeepAliveIdle => _manager.GetString(nameof(InvalidKeepAliveIdle), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: The TCP keepalive interval must be greater than zero.</summary>
    internal static string InvalidKeepAliveInterval => _manager.GetString(nameof(InvalidKeepAliveInterval), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: The TCP keepalive retry count must be greater than zero.</summary>
    internal static string InvalidKeepAliveRetryCount => _manager.GetString(nameof(InvalidKeepAliveRetryCount), CultureInfo.CurrentUICulture)!;
}
