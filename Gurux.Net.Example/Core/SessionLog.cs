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

/// <summary>Stores a thread-safe bounded history of timestamped network activity.</summary>
internal sealed class SessionLog
{
    /// <summary>Removes retained activity entries atomically while allowing subsequent logging.</summary>
    public void Clear()
    {
        lock (_sync)
        {
            _entries.Clear();
        }
    }

    /// <summary>Protects shared state during concurrent transport callbacks.</summary>
    readonly object _sync = new();
    /// <summary>Stores bounded activity entries in chronological order.</summary>
    readonly Queue<string> _entries = new();
    /// <summary>Gets a stable copy of the activity history.</summary>
    public IReadOnlyList<string> Snapshot { get { lock (_sync) { return _entries.ToArray(); } } }
    /// <summary>Appends a timestamped entry and trims the history to five hundred entries.</summary>
    /// <param name="text">The activity or error description to record.</param>
    public void Add(string text)
    {
        lock (_sync)
        {
            _entries.Enqueue(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.ExampleResources.TimestampedActivity, DateTime.Now, text));
            while (_entries.Count > 500) { _entries.Dequeue(); }
        }
    }
}

