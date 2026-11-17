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
    private static readonly ResourceManager Manager = new("Gurux.Net.Properties.Resources", typeof(Resources).Assembly);
    /// <summary>Gets the localized message: A remote host is required.</summary>
    internal static string RemoteHostRequired => Manager.GetString(nameof(RemoteHostRequired), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: UDP preserves datagram boundaries; Eop framing is only supported for TCP.</summary>
    internal static string UdpFramingUnsupported => Manager.GetString(nameof(UdpFramingUnsupported), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: An attached media cannot reconnect. Attach a connected client instead.</summary>
    internal static string AttachedReconnect => Manager.GetString(nameof(AttachedReconnect), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: A lifecycle operation is already in progress.</summary>
    internal static string LifecycleInProgress => Manager.GetString(nameof(LifecycleInProgress), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: Client is not connected.</summary>
    internal static string ClientNotConnected => Manager.GetString(nameof(ClientNotConnected), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: Client is disconnected.</summary>
    internal static string ClientDisconnected => Manager.GetString(nameof(ClientDisconnected), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: Media is closed.</summary>
    internal static string MediaClosed => Manager.GetString(nameof(MediaClosed), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: A client address is required.</summary>
    internal static string ClientAddressRequired => Manager.GetString(nameof(ClientAddressRequired), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: A destination endpoint is required.</summary>
    internal static string DestinationRequired => Manager.GetString(nameof(DestinationRequired), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: Socket closed during send.</summary>
    internal static string SocketClosedDuringSend => Manager.GetString(nameof(SocketClosedDuringSend), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: Attach a client endpoint before receiving synchronously; listener data cannot be combined across clients.</summary>
    internal static string AttachBeforeReceive => Manager.GetString(nameof(AttachBeforeReceive), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: Expected GXNet.</summary>
    internal static string ExpectedNetworkMedia => Manager.GetString(nameof(ExpectedNetworkMedia), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: Close the media before changing settings.</summary>
    internal static string CloseBeforeSettings => Manager.GetString(nameof(CloseBeforeSettings), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: The network port must be between 1 and 65535.</summary>
    internal static string PortOutOfRange => Manager.GetString(nameof(PortOutOfRange), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: Select a supported network protocol.</summary>
    internal static string InvalidProtocol => Manager.GetString(nameof(InvalidProtocol), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: The maximum client count must be zero or greater.</summary>
    internal static string InvalidMaxClientCount => Manager.GetString(nameof(InvalidMaxClientCount), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: The connection timeout must be -1 or greater.</summary>
    internal static string InvalidWaitTime => Manager.GetString(nameof(InvalidWaitTime), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: The TCP keepalive idle time must be greater than zero.</summary>
    internal static string InvalidKeepAliveIdle => Manager.GetString(nameof(InvalidKeepAliveIdle), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: The TCP keepalive interval must be greater than zero.</summary>
    internal static string InvalidKeepAliveInterval => Manager.GetString(nameof(InvalidKeepAliveInterval), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized message: The TCP keepalive retry count must be greater than zero.</summary>
    internal static string InvalidKeepAliveRetryCount => Manager.GetString(nameof(InvalidKeepAliveRetryCount), CultureInfo.CurrentUICulture)!;
}
