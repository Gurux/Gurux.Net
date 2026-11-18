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

namespace Gurux.Net.Example.Properties;

/// <summary>Provides localized captions and messages for the network sample.</summary>
public static class ExampleResources
{
    /// <summary>Gets the outgoing-payload input hint.</summary>
    public static string SendPlaceholder => _manager.GetString(nameof(SendPlaceholder), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the title of the dedicated received-payload card.</summary>
    public static string ReceivedDataHeading => _manager.GetString(nameof(ReceivedDataHeading), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the caption for clearing displayed history.</summary>
    public static string Clear => _manager.GetString(nameof(Clear), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Clear);
    /// <summary>Resolves resources from this application's own Core assembly.</summary>
    private static readonly ResourceManager _manager = new("Gurux.Net.Example.Properties.ExampleResources", typeof(ExampleResources).Assembly);
    /// <summary>Gets the localized text: Gurux Network example</summary>
    public static string ApplicationTitle => _manager.GetString(nameof(ApplicationTitle), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Send:</summary>
    public static string SendLabel => _manager.GetString(nameof(SendLabel), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Received:</summary>
    public static string ReceivedLabel => _manager.GetString(nameof(ReceivedLabel), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Open</summary>
    public static string Open => _manager.GetString(nameof(Open), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the caption for closing the connection or canceling pending work.</summary>
    public static string Close => _manager.GetString(nameof(Close), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Properties</summary>
    public static string Properties => _manager.GetString(nameof(Properties), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Send</summary>
    public static string Send => _manager.GetString(nameof(Send), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized caption for hexadecimal payloads.</summary>
    public static string Hex => _manager.GetString(nameof(Hex), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Sync</summary>
    public static string Sync => _manager.GetString(nameof(Sync), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized caption for echoing received data.</summary>
    public static string Echo => _manager.GetString(nameof(Echo), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Synchronous settings</summary>
    public static string SyncSettings => _manager.GetString(nameof(SyncSettings), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Wait time:</summary>
    public static string WaitTime => _manager.GetString(nameof(WaitTime), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: EOP:</summary>
    public static string Eop => _manager.GetString(nameof(Eop), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Size:</summary>
    public static string Size => _manager.GetString(nameof(Size), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Packet statistics (bytes)</summary>
    public static string Statistics => _manager.GetString(nameof(Statistics), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Sent:</summary>
    public static string Sent => _manager.GetString(nameof(Sent), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Interval (ms):</summary>
    public static string Interval => _manager.GetString(nameof(Interval), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Tester</summary>
    public static string Tester => _manager.GetString(nameof(Tester), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Go / Stop</summary>
    public static string GoStop => _manager.GetString(nameof(GoStop), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Clients</summary>
    public static string Clients => _manager.GetString(nameof(Clients), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Errors</summary>
    public static string Errors => _manager.GetString(nameof(Errors), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Stop</summary>
    public static string Stop => _manager.GetString(nameof(Stop), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Closed</summary>
    public static string Closed => _manager.GetString(nameof(Closed), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized caption for echoing received data. queue is full; packet skipped.</summary>
    public static string EchoQueueFull => _manager.GetString(nameof(EchoQueueFull), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Tester is already running.</summary>
    public static string TesterRunning => _manager.GetString(nameof(TesterRunning), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Interval must be positive.</summary>
    public static string PositiveInterval => _manager.GetString(nameof(PositiveInterval), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Open the connection first.</summary>
    public static string OpenFirst => _manager.GetString(nameof(OpenFirst), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: Select a connected client.</summary>
    public static string SelectClient => _manager.GetString(nameof(SelectClient), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized text: No reply was received before the timeout.</summary>
    public static string ReplyTimeout => _manager.GetString(nameof(ReplyTimeout), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized activity message: Opened.</summary>
    public static string Opened => _manager.GetString(nameof(Opened), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized activity message: Closed.</summary>
    public static string ClosedActivity => _manager.GetString(nameof(ClosedActivity), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized activity message: Error: {0}</summary>
    public static string TransportError => _manager.GetString(nameof(TransportError), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the localized activity message: Reply: {0}</summary>
    public static string ReplyReceived => _manager.GetString(nameof(ReplyReceived), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the activity format for received bytes and text.</summary>
    public static string ReceivedBytes => _manager.GetString(nameof(ReceivedBytes), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the activity format for received data.</summary>
    public static string ReceivedData => _manager.GetString(nameof(ReceivedData), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the activity format for sent bytes.</summary>
    public static string SentBytes => _manager.GetString(nameof(SentBytes), CultureInfo.CurrentUICulture)!;
    /// <summary>Gets the format combining a culture-aware activity timestamp and message.</summary>
    public static string TimestampedActivity => _manager.GetString(nameof(TimestampedActivity), CultureInfo.CurrentUICulture)!;
}
