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

namespace Gurux.Net.Example;

/// <summary>Describes one TCP or UDP send and optional synchronous reply.</summary>
/// <param name="Text">Text or hexadecimal bytes to transmit.</param>
/// <param name="Hex">Whether text, end marker and reply use hexadecimal representation.</param>
/// <param name="Sync">Whether the send waits for a buffered response.</param>
/// <param name="Eop">The optional end-of-packet marker.</param>
/// <param name="Count">The minimum reply byte count, or zero for marker-based reception.</param>
/// <param name="WaitTime">The receive timeout in milliseconds, or minus one to wait indefinitely.</param>
/// <param name="Receiver">The selected server client or datagram destination.</param>
public sealed record NetworkSendOptions(string Text, bool Hex, bool Sync, string? Eop, int Count, int WaitTime, string? Receiver);