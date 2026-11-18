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
using System.Threading.Channels;
using Gurux.Common;
using Gurux.Net;

namespace Gurux.Net.Example;


/// <summary>UI-independent state for the compact network sample.</summary>
public sealed class NetworkExampleController : IAsyncDisposable
{
    /// <summary>Clears displayed activity and diagnostic errors while preserving reception, counters and connection state.</summary>
    public void ClearActivity()
    {
        _session.ClearActivity();
        _errors.Clear();
    }
    /// <summary>Owns the network connection used by this component.</summary>
    private readonly ExampleSession _session;
    /// <summary>Stores the bounded network error history.</summary>
    private readonly SessionLog _errors = new();
    /// <summary>Cancels pending operations when this owner closes.</summary>
    private readonly CancellationTokenSource _lifetime = new();
    /// <summary>Bounds packets waiting for asynchronous echo.</summary>
    private readonly Channel<(byte[] Data, string? Sender)> _echoes = Channel.CreateBounded<(byte[], string?)>(64);
    /// <summary>Runs the cancellable packet echo loop.</summary>
    private readonly Task _echoWorker;
    /// <summary>Cancels the active periodic send loop.</summary>
    private CancellationTokenSource? _tester;
    /// <summary>Tracks the current periodic send loop.</summary>
    private Task? _testing;
    /// <summary>Stores the latest received payload.</summary>
    private byte[] _received = [];
    /// <summary>Enables echoing received network packets.</summary>
    private int _echo;
    /// <summary>Gets or sets whether received packets are echoed.</summary>
    public bool Echo { get => Volatile.Read(ref _echo) != 0; set => Volatile.Write(ref _echo, value ? 1 : 0); }
    /// <summary>Gets whether periodic sending is active.</summary>
    public bool IsTesting => _testing is { IsCompleted: false };
    /// <summary>Gets a snapshot of the bounded network error history.</summary>
    public IReadOnlyList<string> Errors => _errors.Snapshot;
    /// <summary>Clears the retained received payload without changing the connection, counters or activity history.</summary>
    public void ClearReceived() => Volatile.Write(ref _received, []);
    /// <summary>Renders the latest received bytes as ASCII text, falling back to hexadecimal for any non-ASCII byte.</summary>
    /// <param name="hex">Whether to force hexadecimal representation even for ASCII payloads.</param>
    /// <returns>The complete hexadecimal packet when requested or non-ASCII, otherwise its ASCII text.</returns>
    public string Received(bool hex)
    {
        var data = Volatile.Read(ref _received);
        return ExampleDataDisplay.Format(data, hex);
    }
    /// <summary>Subscribes to reception and starts the bounded echo worker.</summary>
    /// <param name="session">The network session owned by the calling view.</param>
    public NetworkExampleController(ExampleSession session)
    {
        this._session = session;
        session.Media.OnReceived += OnReceived;
        session.Media.OnError += OnError;
        _echoWorker = EchoAsync();
    }
    /// <summary>Captures received bytes and queues an optional echo response.</summary>
    /// <param name="args">The data or connection information delivered by the event.</param>
    /// <param name="sender">The transport that raised the event.</param>
    private void OnReceived(object? sender, ReceiveEventArgs args)
    {
        if (args.Data is not byte[] data) { return; }
        Volatile.Write(ref _received, data.ToArray());
        if (Echo && !_echoes.Writer.TryWrite((data.ToArray(), args.SenderInfo)))
        {
            Report(ExampleResources.EchoQueueFull);
        }
    }
    /// <summary>Records an asynchronous transport failure.</summary>
    /// <param name="args">The data or connection information delivered by the event.</param>
    /// <param name="sender">The transport that raised the event.</param>
    private void OnError(object? sender, MediaErrorEventArgs args) => Report(args.Exception.Message);
    /// <summary>Adds an error description to the bounded history.</summary>
    /// <param name="message">The localized error description.</param>
    public void Report(string message) => _errors.Add(message);
    /// <summary>Processes queued echo packets until cancellation.</summary>
    /// <returns>A task completing when echo processing stops.</returns>
    private async Task EchoAsync()
    {
        try
        {
            await foreach (var packet in _echoes.Reader.ReadAllAsync(_lifetime.Token).ConfigureAwait(false))
            {
                if (!Echo || !_session.Media.IsOpen) { continue; }
                try
                {
                    var net = (GXNet)_session.Media;
                    await net.SendAsync(packet.Data, net.Server || net.Protocol == NetworkType.Udp ? packet.Sender : null, _lifetime.Token).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException) { Report(ex.Message); }
            }
        }
        catch (OperationCanceledException) { }
    }
    /// <summary>Sends the configured payload and captures a requested reply.</summary>
    /// <param name="options">The payload, destination and reply settings.</param>
    /// <param name="cancellationToken">Cancels sending or waiting for the reply.</param>
    /// <returns>A task completing when the send and optional reception finish.</returns>
    public async Task SendAsync(NetworkSendOptions options, CancellationToken cancellationToken = default)
    {
        string? end = options.Eop;
        if (end is not null && end.Contains('\\'))
        {
            end = end.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t");
            if (options.Hex) { end = Convert.ToHexString(Encoding.UTF8.GetBytes(end)); }
        }
        byte[]? response = await _session.SendBytesAsync(options.Text, options.Hex, options.Sync, end,
            options.Count, options.WaitTime, options.Receiver, cancellationToken).ConfigureAwait(false);
        if (response is not null)
        {
            Volatile.Write(ref _received, response);
        }
    }
    /// <summary>Validates connection settings and starts periodic network sends.</summary>
    /// <param name="options">The payload, destination and reply settings.</param>
    /// <param name="intervalMilliseconds">The positive delay between sends in milliseconds.</param>
    /// <exception cref="InvalidOperationException">The tester is running or the connection is closed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The interval is not positive.</exception>
    public void StartTester(NetworkSendOptions options, int intervalMilliseconds)
    {
        if (IsTesting) { throw new InvalidOperationException(ExampleResources.TesterRunning); }
        if (intervalMilliseconds < 1) { throw new ArgumentOutOfRangeException(nameof(intervalMilliseconds), ExampleResources.PositiveInterval); }
        if (!_session.Media.IsOpen) { throw new InvalidOperationException(ExampleResources.OpenFirst); }
        _tester?.Dispose();
        _tester = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _testing = TestAsync(options, intervalMilliseconds, _tester.Token);
    }
    /// <summary>Sends packets until cancellation or a transport failure.</summary>
    /// <param name="interval">The delay between sends in milliseconds.</param>
    /// <param name="options">The payload, destination and reply settings.</param>
    /// <param name="token">Cancels the periodic send loop.</param>
    /// <returns>A task completing when periodic sending stops.</returns>
    private async Task TestAsync(NetworkSendOptions options, int interval, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await SendAsync(options, token).ConfigureAwait(false);
                await Task.Delay(interval, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Report(ex.Message); }
    }
    /// <summary>Cancels and waits for periodic sending to stop.</summary>
    /// <returns>A task completing after the tester stops.</returns>
    public async Task StopTesterAsync()
    {
        _tester?.Cancel();
        if (_testing is not null) { await _testing.ConfigureAwait(false); }
    }
    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _session.Media.OnReceived -= OnReceived;
        _session.Media.OnError -= OnError;
        _lifetime.Cancel();
        await StopTesterAsync().ConfigureAwait(false);
        _echoes.Writer.TryComplete();
        await _echoWorker.ConfigureAwait(false);
        _tester?.Dispose();
        _lifetime.Dispose();
    }
}
