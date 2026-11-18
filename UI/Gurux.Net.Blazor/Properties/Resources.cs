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
namespace Gurux.Net.Blazor.Properties;
/// <summary>Provides localized settings labels and complete validation messages.</summary>
internal static class Resources
{
    /// <summary>Looks up the embedded resources using the current UI culture.</summary>
    private static readonly System.Resources.ResourceManager _manager = new("Gurux.Net.Blazor.Properties.Resources", typeof(Resources).Assembly);
    /// <summary>Gets the localized WholeNumberRequired text.</summary>
    internal static string WholeNumberRequired => _manager.GetString(nameof(WholeNumberRequired), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Protocol text.</summary>
    internal static string Protocol => _manager.GetString(nameof(Protocol), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Hostname text.</summary>
    internal static string Hostname => _manager.GetString(nameof(Hostname), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Port text.</summary>
    internal static string Port => _manager.GetString(nameof(Port), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Server text.</summary>
    internal static string Server => _manager.GetString(nameof(Server), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized IPv6 text.</summary>
    internal static string IPv6 => _manager.GetString(nameof(IPv6), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Maximumclients text.</summary>
    internal static string Maximumclients => _manager.GetString(nameof(Maximumclients), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Waittimemilliseconds text.</summary>
    internal static string Waittimemilliseconds => _manager.GetString(nameof(Waittimemilliseconds), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized TCPkeepalive text.</summary>
    internal static string TCPkeepalive => _manager.GetString(nameof(TCPkeepalive), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized TCPkeepaliveidleseconds text.</summary>
    internal static string TCPkeepaliveidleseconds => _manager.GetString(nameof(TCPkeepaliveidleseconds), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized TCPkeepaliveintervalseconds text.</summary>
    internal static string TCPkeepaliveintervalseconds => _manager.GetString(nameof(TCPkeepaliveintervalseconds), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized TCPkeepaliveretries text.</summary>
    internal static string TCPkeepaliveretries => _manager.GetString(nameof(TCPkeepaliveretries), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Closethemediabeforeeditingsettings text.</summary>
    internal static string Closethemediabeforeeditingsettings => _manager.GetString(nameof(Closethemediabeforeeditingsettings), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Closethemediabeforesavingsettings text.</summary>
    internal static string Closethemediabeforesavingsettings => _manager.GetString(nameof(Closethemediabeforesavingsettings), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized ThemediasettingschangedwhilethisformwasopenReopentheform text.</summary>
    internal static string ThemediasettingschangedwhilethisformwasopenReopentheform => _manager.GetString(nameof(ThemediasettingschangedwhilethisformwasopenReopentheform), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Phonenumber text.</summary>
    internal static string Phonenumber => _manager.GetString(nameof(Phonenumber), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Initializationcommandsoneperline text.</summary>
    internal static string Initializationcommandsoneperline => _manager.GetString(nameof(Initializationcommandsoneperline), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Connectiontimeoutseconds text.</summary>
    internal static string Connectiontimeoutseconds => _manager.GetString(nameof(Connectiontimeoutseconds), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized SIMPIN text.</summary>
    internal static string SIMPIN => _manager.GetString(nameof(SIMPIN), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Messageencoding text.</summary>
    internal static string Messageencoding => _manager.GetString(nameof(Messageencoding), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Portname text.</summary>
    internal static string Portname => _manager.GetString(nameof(Portname), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Baudrate text.</summary>
    internal static string Baudrate => _manager.GetString(nameof(Baudrate), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Databits text.</summary>
    internal static string Databits => _manager.GetString(nameof(Databits), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Parity text.</summary>
    internal static string Parity => _manager.GetString(nameof(Parity), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Stopbits text.</summary>
    internal static string Stopbits => _manager.GetString(nameof(Stopbits), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Handshake text.</summary>
    internal static string Handshake => _manager.GetString(nameof(Handshake), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized DTRenabled text.</summary>
    internal static string DTRenabled => _manager.GetString(nameof(DTRenabled), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized RTSenabled text.</summary>
    internal static string RTSenabled => _manager.GetString(nameof(RTSenabled), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized MediaSettings text.</summary>
    internal static string MediaSettings => _manager.GetString(nameof(MediaSettings), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Save text.</summary>
    internal static string Save => _manager.GetString(nameof(Save), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized Cancel text.</summary>
    internal static string Cancel => _manager.GetString(nameof(Cancel), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized UnsupportedMedia text.</summary>
    internal static string UnsupportedMedia => _manager.GetString(nameof(UnsupportedMedia), System.Globalization.CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized InvalidSettings text.</summary>
    internal static string InvalidSettings => _manager.GetString(nameof(InvalidSettings), System.Globalization.CultureInfo.CurrentUICulture)!;
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
