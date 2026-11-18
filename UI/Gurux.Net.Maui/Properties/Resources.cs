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

namespace Gurux.Net.Maui.Properties;

/// <summary>Resolves localized native settings labels and messages.</summary>
internal static class Resources
{
    /// <summary>Reads this assembly's settings resources.</summary>
    private static readonly ResourceManager _manager = new("Gurux.Net.Maui.Properties.Resources", typeof(Resources).Assembly);
    /// <summary>Gets the localized text: Media settings.</summary>
    internal static string Title => _manager.GetString(nameof(Title), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Save.</summary>
    internal static string Save => _manager.GetString(nameof(Save), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Cancel.</summary>
    internal static string Cancel => _manager.GetString(nameof(Cancel), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: This editor is already showing settings..</summary>
    internal static string Busy => _manager.GetString(nameof(Busy), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Close the media before editing settings..</summary>
    internal static string Open => _manager.GetString(nameof(Open), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: The media settings changed while this form was open. Reopen the form..</summary>
    internal static string Changed => _manager.GetString(nameof(Changed), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: This editor does not support the supplied media type..</summary>
    internal static string WrongMedia => _manager.GetString(nameof(WrongMedia), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: The settings owner must be a ContentPage or a NavigationPage showing a ContentPage..</summary>
    internal static string Owner => _manager.GetString(nameof(Owner), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: The parent must be a MauiMediaWindow from this settings library..</summary>
    internal static string Parent => _manager.GetString(nameof(Parent), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: The MAUI window must have a page..</summary>
    internal static string WindowPage => _manager.GetString(nameof(WindowPage), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: {0} must be a whole number..</summary>
    internal static string WholeNumber => _manager.GetString(nameof(WholeNumber), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Select a value for {0}..</summary>
    internal static string SelectValue => _manager.GetString(nameof(SelectValue), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: The settings could not be saved. Check the values and try again..</summary>
    internal static string Invalid => _manager.GetString(nameof(Invalid), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Port name.</summary>
    internal static string PortName => _manager.GetString(nameof(PortName), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Baud rate.</summary>
    internal static string BaudRate => _manager.GetString(nameof(BaudRate), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Data bits.</summary>
    internal static string DataBits => _manager.GetString(nameof(DataBits), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Parity.</summary>
    internal static string Parity => _manager.GetString(nameof(Parity), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Stop bits.</summary>
    internal static string StopBits => _manager.GetString(nameof(StopBits), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Handshake.</summary>
    internal static string Handshake => _manager.GetString(nameof(Handshake), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: DTR enabled.</summary>
    internal static string Dtr => _manager.GetString(nameof(Dtr), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: RTS enabled.</summary>
    internal static string Rts => _manager.GetString(nameof(Rts), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Phone number.</summary>
    internal static string Phone => _manager.GetString(nameof(Phone), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Initialization commands (one per line).</summary>
    internal static string Commands => _manager.GetString(nameof(Commands), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Connection timeout (seconds).</summary>
    internal static string Timeout => _manager.GetString(nameof(Timeout), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: SIM PIN.</summary>
    internal static string Pin => _manager.GetString(nameof(Pin), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Message encoding.</summary>
    internal static string Encoding => _manager.GetString(nameof(Encoding), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Protocol.</summary>
    internal static string Protocol => _manager.GetString(nameof(Protocol), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Host name.</summary>
    internal static string Host => _manager.GetString(nameof(Host), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Port.</summary>
    internal static string Port => _manager.GetString(nameof(Port), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Server.</summary>
    internal static string Server => _manager.GetString(nameof(Server), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: IPv6.</summary>
    internal static string IPv6 => _manager.GetString(nameof(IPv6), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Maximum clients.</summary>
    internal static string Clients => _manager.GetString(nameof(Clients), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Wait time (milliseconds).</summary>
    internal static string Wait => _manager.GetString(nameof(Wait), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: TCP keepalive.</summary>
    internal static string KeepAlive => _manager.GetString(nameof(KeepAlive), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: TCP keepalive idle (seconds).</summary>
    internal static string Idle => _manager.GetString(nameof(Idle), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: TCP keepalive interval (seconds).</summary>
    internal static string Interval => _manager.GetString(nameof(Interval), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: TCP keepalive retries.</summary>
    internal static string Retries => _manager.GetString(nameof(Retries), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized ListenAddress caption.</summary>
    internal static string ListenAddress => _manager.GetString(nameof(ListenAddress), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized AllInterfaces caption.</summary>
    internal static string AllInterfaces => _manager.GetString(nameof(AllInterfaces), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized LoopbackOnly caption.</summary>
    internal static string LoopbackOnly => _manager.GetString(nameof(LoopbackOnly), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized CustomAddress caption.</summary>
    internal static string CustomAddress => _manager.GetString(nameof(CustomAddress), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized InterfaceAddress caption.</summary>
    internal static string InterfaceAddress => _manager.GetString(nameof(InterfaceAddress), System.Globalization.CultureInfo.CurrentUICulture)!;
}
