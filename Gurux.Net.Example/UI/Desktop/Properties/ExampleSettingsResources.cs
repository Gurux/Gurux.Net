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

using System.Resources;

namespace Gurux.Net.Example.Desktop.Properties;

/// <summary>Localized messages for this example's settings dispatcher.</summary>
internal static class ExampleSettingsResources
{
    /// <summary>Reads this assembly's embedded settings messages.</summary>
    private static readonly ResourceManager Manager = new("Gurux.Net.Example.Desktop.Properties.ExampleSettingsResources", typeof(ExampleSettingsResources).Assembly);

    /// <summary>Explains that a settings operation is already in progress.</summary>
    internal static string EditorAlreadyShowing => Manager.GetString(nameof(EditorAlreadyShowing), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(EditorAlreadyShowing);
    /// <summary>Gets the heading for connection settings and lifecycle commands.</summary>
    internal static string ConnectionGroup => Manager.GetString(nameof(ConnectionGroup), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ConnectionGroup);
    /// <summary>Gets the badge caption for an established connection.</summary>
    internal static string ConnectionActive => Manager.GetString(nameof(ConnectionActive), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ConnectionActive);
    /// <summary>Gets the badge caption for an idle closed connection.</summary>
    internal static string ConnectionInactive => Manager.GetString(nameof(ConnectionInactive), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ConnectionInactive);
    /// <summary>Gets the badge caption while a connection operation is running.</summary>
    internal static string ConnectionBusy => Manager.GetString(nameof(ConnectionBusy), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ConnectionBusy);
    /// <summary>Gets the heading for the bounded technical activity output.</summary>
    internal static string ActivityGroup => Manager.GetString(nameof(ActivityGroup), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ActivityGroup);
    /// <summary>Gets the title of the data view and outgoing payload card.</summary>
    internal static string DataGroup => Manager.GetString(nameof(DataGroup), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(DataGroup);
    /// <summary>Gets the heading for synchronous reply conditions.</summary>
    internal static string ReplyGroup => Manager.GetString(nameof(ReplyGroup), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ReplyGroup);
    /// <summary>Gets the caption identifying the remote host field.</summary>
    internal static string HostLabel => Manager.GetString(nameof(HostLabel), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(HostLabel);
    /// <summary>Gets the caption identifying the minimum reply byte count.</summary>
    internal static string CountLabel => Manager.GetString(nameof(CountLabel), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(CountLabel);
    /// <summary>Gets the caption identifying the reply packet terminator.</summary>
    internal static string TerminatorLabel => Manager.GetString(nameof(TerminatorLabel), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(TerminatorLabel);
    /// <summary>Gets the reply timeout caption including millisecond units.</summary>
    internal static string TimeoutLabel => Manager.GetString(nameof(TimeoutLabel), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(TimeoutLabel);
    /// <summary>Gets the heading for byte counters and periodic transmission controls.</summary>
    internal static string ToolsGroup => Manager.GetString(nameof(ToolsGroup), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ToolsGroup);
    /// <summary>Gets the empty-state text for the network error history.</summary>
    internal static string NoErrors => Manager.GetString(nameof(NoErrors), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(NoErrors);
    /// <summary>Gets the localized state caption for an open media connection.</summary>
    internal static string StatusOpen => Manager.GetString(nameof(StatusOpen), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(StatusOpen);

    /// <summary>Gets the localized state caption for a closed media connection.</summary>
    internal static string StatusClosed => Manager.GetString(nameof(StatusClosed), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(StatusClosed);

    /// <summary>Gets the localized placeholder for an unselected endpoint.</summary>
    internal static string StatusNotConfigured => Manager.GetString(nameof(StatusNotConfigured), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(StatusNotConfigured);

    /// <summary>Gets the localized format for state, network protocol, address and port.</summary>
    internal static string StatusNetwork => Manager.GetString(nameof(StatusNetwork), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(StatusNetwork);

    /// <summary>Gets the localized view navigation caption.</summary>
    internal static string ViewMenu => Manager.GetString(nameof(ViewMenu), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ViewMenu);
    /// <summary>Gets the localized settings navigation caption.</summary>
    internal static string SettingsMenu => Manager.GetString(nameof(SettingsMenu), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(SettingsMenu);
    /// <summary>Gets the localized communication settings navigation caption.</summary>
    internal static string CommunicationSettings => Manager.GetString(nameof(CommunicationSettings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(CommunicationSettings);
    /// <summary>Gets the localized media settings navigation caption.</summary>
    internal static string MediaSettings => Manager.GetString(nameof(MediaSettings), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(MediaSettings);
    /// <summary>Gets the localized menu navigation caption.</summary>
    internal static string NavigationMenu => Manager.GetString(nameof(NavigationMenu), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(NavigationMenu);
    /// <summary>Gets the localized cancel navigation caption.</summary>
    internal static string CancelNavigation => Manager.GetString(nameof(CancelNavigation), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(CancelNavigation);
}
