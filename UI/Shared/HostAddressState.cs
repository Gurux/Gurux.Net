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
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Gurux.Net.UI;

/// <summary>Stages separate remote and local addresses and discovers local bind choices.</summary>
internal sealed class HostAddressState
{
    /// <summary>Retains the remote host while editing server settings.</summary>
    private string _clientHost;
    /// <summary>Retains the local bind address while editing client settings.</summary>
    private string _serverHost;
    /// <summary>Tracks the selected IP address family.</summary>
    private bool _useIPv6;
    /// <summary>Caches a snapshot of local addresses for this settings session.</summary>
    private List<(IPAddress Address, string Name)>? _addresses;

    /// <summary>Stages the source address in its current connection mode.</summary>
    /// <param name="server">Whether the source is a listener.</param>
    /// <param name="useIPv6">Whether the source uses IPv6.</param>
    /// <param name="host">The existing remote host or local bind address.</param>
    internal HostAddressState(bool server, bool useIPv6, string host)
    {
        Server = server;
        _useIPv6 = useIPv6;
        _clientHost = server ? "" : host;
        _serverHost = server ? host : "";
    }

    /// <summary>Gets or sets whether the editor shows the local bind address.</summary>
    internal bool Server { get; set; }
    /// <summary>Gets or sets the address for the current mode without discarding the other mode.</summary>
    internal string Host
    {
        get => Server ? _serverHost : _clientHost;
        set { if (Server) { _serverHost = value; } else { _clientHost = value; } }
    }
    /// <summary>Gets or sets the address family, adapting the standard loopback choice.</summary>
    internal bool UseIPv6
    {
        get => _useIPv6;
        set
        {
            if (value != _useIPv6 && (_serverHost == "127.0.0.1" || _serverHost == "::1"))
            {
                _serverHost = value ? "::1" : "127.0.0.1";
            }
            _useIPv6 = value;
        }
    }

    /// <summary>Builds localized choices for the selected family, including manual entry.</summary>
    /// <param name="all">The caption for binding all interfaces.</param>
    /// <param name="loopback">The caption for binding only this device.</param>
    /// <param name="custom">The caption for entering an address manually.</param>
    /// <param name="interfaceFormat">A localized format with address and interface name placeholders.</param>
    /// <returns>Address/caption pairs; a null address denotes manual entry.</returns>
    internal List<(string? Address, string Caption)> GetOptions(string all, string loopback, string custom, string interfaceFormat)
    {
        var family = UseIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
        var options = new List<(string? Address, string Caption)>
        {
            ("", all),
            (UseIPv6 ? "::1" : "127.0.0.1", loopback)
        };
        _addresses ??= ReadAddresses();
        foreach (var item in _addresses.Where(item => item.Address.AddressFamily == family))
        {
            options.Add((item.Address.ToString(), string.Format(CultureInfo.CurrentCulture, interfaceFormat, item.Address, item.Name)));
        }
        options.Add((null, custom));
        return options;
    }

    /// <summary>Reads available unicast addresses while retaining manual entry on restricted platforms.</summary>
    /// <returns>Local addresses and interface names, preserving IPv6 scope identifiers.</returns>
    private static List<(IPAddress Address, string Name)> ReadAddresses()
    {
        var result = new List<(IPAddress Address, string Name)>();
        try
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                try
                {
                    if (adapter.OperationalStatus != OperationalStatus.Up) { continue; }
                    foreach (var unicast in adapter.GetIPProperties().UnicastAddresses)
                    {
                        var address = unicast.Address;
                        if (!IPAddress.IsLoopback(address) &&
                            (address.AddressFamily == AddressFamily.InterNetwork || address.AddressFamily == AddressFamily.InterNetworkV6) &&
                            !address.Equals(IPAddress.Any) && !address.Equals(IPAddress.IPv6Any) &&
                            !result.Any(item => item.Address.Equals(address)))
                        {
                            result.Add((address, adapter.Name));
                        }
                    }
                }
                catch (Exception exception) when (exception is NetworkInformationException or SocketException or NotSupportedException or UnauthorizedAccessException)
                {
                    // One restricted interface must not hide the remaining interfaces.
                }
            }
        }
        catch (Exception exception) when (exception is NetworkInformationException or SocketException or NotSupportedException or UnauthorizedAccessException)
        {
            // Browser and mobile restrictions must not prevent manual address entry.
        }
        return result;
    }
}
