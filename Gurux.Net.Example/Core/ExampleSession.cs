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

using Gurux.Net.Enums;
using Gurux.Net.Example.Properties;
using System.Text;
using Gurux.Common;
using Gurux.Net;

namespace Gurux.Net.Example;


/// <summary>Owns a network connection and serializes cancellable send and receive operations.</summary>
public sealed class ExampleSession : IAsyncDisposable
{
    /// <summary>Clears retained activity without changing received payloads, connection state or traffic counters.</summary>
    public void ClearActivity() => _log.Clear();
    /// <summary>Serializes operations that own receive buffering.</summary>
    readonly SemaphoreSlim _operations = new(1, 1);
    /// <summary>Protects shared state during concurrent transport callbacks.</summary>
    readonly object _sync = new();
    /// <summary>Stores timestamped activity history.</summary>
    readonly SessionLog _log = new();
    /// <summary>Tracks currently connected network clients.</summary>
    readonly HashSet<string> _clients = new();
    /// <summary>Retains attached-client receive subscriptions.</summary>
    readonly Dictionary<string, GXNet> _attachedClients = new();
    /// <summary>Cancels pending operations when this owner closes.</summary>
    CancellationTokenSource _lifetime = new();
    /// <summary>Records whether disposal has started.</summary>
    bool _disposed;
    /// <summary>Indicates an active serialized operation.</summary>
    int _busy;
    /// <summary>Gets the network transport owned by this session.</summary>
    public GXNet Media { get; }
    /// <summary>Gets whether a serialized network operation is active.</summary>
    public bool IsBusy => Volatile.Read(ref _busy) != 0;
    /// <summary>Gets a snapshot of connected client addresses.</summary>
    public IReadOnlyList<string> Clients { get { lock (_sync) { return _clients.ToArray(); } } }
    /// <summary>Gets a snapshot of timestamped network activity.</summary>
    public IReadOnlyList<string> Log => _log.Snapshot;

    /// <summary>Takes ownership of a network transport and subscribes to its events.</summary>
    public ExampleSession() : this(new GXNet(NetworkType.Tcp, "localhost", 4059)) { }

    /// <summary>Takes ownership of a network transport and subscribes to its events.</summary>
    /// <param name="media">The owned network transport.</param>
    /// <exception cref="ArgumentNullException">The network transport is null.</exception>
    public ExampleSession(GXNet media)
    {
        Media = media ?? throw new ArgumentNullException(nameof(media));
        Media.OnReceived += Received;
        Media.OnError += Error;
        Media.OnClientConnected += Connected;
        Media.OnClientDisconnected += Disconnected;
    }
    /// <summary>Records the received payload and sender, displaying non-ASCII bytes only as hexadecimal.</summary>
    /// <param name="args">The data or connection information delivered by the event.</param>
    /// <param name="sender">The transport that raised the event.</param>
    void Received(object? sender, ReceiveEventArgs args)
    {
        if (args.Data is byte[] bytes)
        {
            _log.Add(ExampleDataDisplay.IsAscii(bytes)
                ? string.Format(System.Globalization.CultureInfo.CurrentCulture, ExampleResources.ReceivedBytes, args.SenderInfo, Convert.ToHexString(bytes), ExampleDataDisplay.Format(bytes))
                : string.Format(System.Globalization.CultureInfo.CurrentCulture, ExampleResources.ReceivedData, args.SenderInfo, ExampleDataDisplay.Format(bytes)));
        }
        else
        {
            _log.Add(string.Format(System.Globalization.CultureInfo.CurrentCulture, ExampleResources.ReceivedData, args.SenderInfo, args.Data));
        }
    }
    /// <summary>Records an asynchronous transport failure.</summary>
    /// <param name="args">The data or connection information delivered by the event.</param>
    /// <param name="sender">The transport that raised the event.</param>
    void Error(object? sender, MediaErrorEventArgs args) => _log.Add(string.Format(System.Globalization.CultureInfo.CurrentCulture, ExampleResources.TransportError, args.Exception.Message));
    /// <summary>Tracks a newly accepted client address.</summary>
    /// <param name="args">The data or connection information delivered by the event.</param>
    /// <param name="sender">The transport that raised the event.</param>
    void Connected(object? sender, ConnectionEventArgs args) { if (args.Info is { } info) { lock (_sync) { _clients.Add(info); } } }
    /// <summary>Removes a disconnected client and its receive subscription.</summary>
    /// <param name="args">The data or connection information delivered by the event.</param>
    /// <param name="sender">The transport that raised the event.</param>
    void Disconnected(object? sender, ConnectionEventArgs args)
    {
        if (args.Info is { } info)
        {
            lock (_sync)
            {
                _clients.Remove(info);
                if (_attachedClients.Remove(info, out var attached)) { attached.OnReceived -= Received; }
            }
        }
    }

    /// <summary>Serializes an operation and links cancellation to connection shutdown.</summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <param name="cancellationToken">Cancels the pending operation.</param>
    /// <returns>The result produced by the operation.</returns>
    async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        CancellationToken token;
        lock (_sync) { ObjectDisposedException.ThrowIf(_disposed, this); token = _lifetime.Token; }
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, cancellationToken);
        await _operations.WaitAsync(linked.Token).ConfigureAwait(false);
        Interlocked.Exchange(ref _busy, 1);
        try { linked.Token.ThrowIfCancellationRequested(); return await action(linked.Token).ConfigureAwait(false); }
        finally { Interlocked.Exchange(ref _busy, 0); _operations.Release(); }
    }

    /// <summary>Opens the configured network transport.</summary>
    /// <param name="cancellationToken">Cancels opening the connection.</param>
    /// <returns>A task completing when the connection opens.</returns>
    public Task OpenAsync(CancellationToken cancellationToken = default) => RunAsync(async token =>
    {
        await Media.OpenAsync(token).ConfigureAwait(false);
        _log.Add(ExampleResources.Opened);
        return true;
    }, cancellationToken);

    /// <summary>Cancels pending operations and clears connection state.</summary>
    /// <returns>A task completing after connection cleanup.</returns>
    public async Task CloseAsync()
    {
        lock (_sync) { _lifetime.Cancel(); }
        var closing = Media.CloseAsync();
        await _operations.WaitAsync().ConfigureAwait(false);
        try
        {
            await closing.ConfigureAwait(false);
            lock (_sync)
            {
                foreach (var attached in _attachedClients.Values) { attached.OnReceived -= Received; }
                _attachedClients.Clear();
            }
            lock (_sync) { _clients.Clear(); _lifetime.Dispose(); _lifetime = new(); }
            _log.Add(ExampleResources.ClosedActivity);
        }
        finally { _operations.Release(); }
    }

    /// <summary>Sends UTF-8 text or hexadecimal bytes and optionally receives a buffered response.</summary>
    /// <param name="text">The payload to send; in text mode, literal \r and \n sequences become carriage returns and line feeds.</param>
    /// <param name="hex">Whether payloads and the end marker use hexadecimal representation.</param>
    /// <param name="receiveReply">Whether to wait for a response.</param>
    /// <param name="eop">The optional end-of-packet marker.</param>
    /// <param name="count">The minimum reply byte count, or zero for marker-based reception.</param>
    /// <param name="waitTime">The reply timeout in milliseconds; minus one waits indefinitely.</param>
    /// <param name="receiver">The accepted client or datagram recipient.</param>
    /// <param name="cancellationToken">Cancels sending or receiving.</param>
    /// <returns>The reply as ASCII text or hexadecimal when selected or non-ASCII, or null when no reply was requested.</returns>
    /// <exception cref="ArgumentException">A server send has no selected client.</exception>
    /// <exception cref="TimeoutException">The reply deadline expires.</exception>
    /// <exception cref="FormatException">The hexadecimal payload or marker is malformed.</exception>
    public async Task<string?> SendAsync(string text, bool hex = false, bool receiveReply = false, string? eop = null, int count = 0, int waitTime = 5000, string? receiver = null, CancellationToken cancellationToken = default)
    {
        byte[]? reply = await SendBytesAsync(text, hex, receiveReply, eop, count, waitTime, receiver, cancellationToken).ConfigureAwait(false);
        return reply is null ? null : ExampleDataDisplay.Format(reply, hex);
    }

    /// <summary>Sends the selected representation and retains the original reply bytes for display and controller processing.</summary>
    /// <param name="text">The payload to send; in text mode, literal \r and \n sequences become carriage returns and line feeds.</param>
    /// <param name="hex">Whether payloads and the end marker use hexadecimal representation.</param>
    /// <param name="receiveReply">Whether to wait for a response.</param>
    /// <param name="eop">The optional end-of-packet marker.</param>
    /// <param name="count">The minimum reply byte count, or zero for marker-based reception.</param>
    /// <param name="waitTime">The reply timeout in milliseconds; minus one waits indefinitely.</param>
    /// <param name="receiver">The accepted client or datagram recipient.</param>
    /// <param name="cancellationToken">Cancels sending or receiving.</param>
    /// <returns>The original reply bytes, or null when no reply was requested.</returns>
    /// <exception cref="ArgumentException">A server send has no selected client.</exception>
    /// <exception cref="TimeoutException">The reply deadline expires.</exception>
    /// <exception cref="FormatException">The hexadecimal payload or marker is malformed.</exception>
    internal Task<byte[]?> SendBytesAsync(string text, bool hex = false, bool receiveReply = false, string? eop = null, int count = 0, int waitTime = 5000, string? receiver = null, CancellationToken cancellationToken = default)
        => RunAsync<byte[]?>(async token =>
        {
            var data = hex ? Convert.FromHexString(string.Concat(text.Where(c => !char.IsWhiteSpace(c)))) : Encoding.UTF8.GetBytes(text.Replace(@"\r", "\r", StringComparison.Ordinal).Replace(@"\n", "\n", StringComparison.Ordinal));
            GXMediaBase target = Media;
            GXNet? attached = null;
            if (Media is GXNet { Server: true } net)
            {
                if (string.IsNullOrWhiteSpace(receiver)) { throw new ArgumentException(ExampleResources.SelectClient, nameof(receiver)); }
                if (receiveReply) { attached = net.Attach(receiver); target = attached; lock (_sync) { if (_attachedClients.TryAdd(receiver, attached)) { attached.OnReceived += Received; } } }
            }
            try
            {
                using var scope = receiveReply ? target.BeginSynchronous() : null;
                if (receiveReply) { target.ResetSynchronousBuffer(); }
                await target.SendAsync(data, attached is null ? receiver : null, token).ConfigureAwait(false);
                _log.Add(string.Format(System.Globalization.CultureInfo.CurrentCulture, ExampleResources.SentBytes, ExampleDataDisplay.Format(data, hex)));
                if (!receiveReply) { return null; }
                var parameters = new ReceiveParameters<byte[]> { Count = count, WaitTime = waitTime, Eop = string.IsNullOrEmpty(eop) ? null : (hex ? Convert.FromHexString(string.Concat(eop.Where(c => !char.IsWhiteSpace(c)))) : Encoding.UTF8.GetBytes(eop)), AllData = count == 0 && string.IsNullOrEmpty(eop) };
                if (!await target.ReceiveAsync(parameters, token).ConfigureAwait(false)) { throw new TimeoutException(ExampleResources.ReplyTimeout); }
                var reply = parameters.Reply ?? [];
                var result = ExampleDataDisplay.Format(reply, hex);
                _log.Add(string.Format(System.Globalization.CultureInfo.CurrentCulture, ExampleResources.ReplyReceived, result));
                return reply;
            }
            finally { /* Attached TCP media remains owned by the server until the client disconnects. */ }
        }, cancellationToken);

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        lock (_sync) { if (_disposed) { return; } _disposed = true; }
        await CloseAsync().ConfigureAwait(false);
        Media.OnReceived -= Received;
        Media.OnError -= Error;
        if (Media is GXNet net) { net.OnClientConnected -= Connected; net.OnClientDisconnected -= Disconnected; }
        await Media.DisposeAsync().ConfigureAwait(false);
    }
}