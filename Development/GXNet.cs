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
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;
using System.Text.Json;
using Gurux.Common;

namespace Gurux.Net;

/// <summary>Cross-platform TCP and UDP communication using asynchronous sockets.</summary>
public class GXNet : GXMediaBase, IGXMediaIconProvider
{
    /// <summary>Opens the embedded PNG icon for this media without loading a UI framework.</summary>
    /// <returns>A new readable stream positioned at the beginning, or null if the embedded icon is unavailable.</returns>
    /// <remarks>The caller owns the stream and must dispose it. Each call returns an independent stream.</remarks>
    public Stream? OpenIcon()
    {
        return typeof(GXNet).Assembly.GetManifestResourceStream("Gurux.Net.MediaIcon.png");
    }

    /// <summary>Occurs when a server reports a client connection attempt.</summary>
    public event EventHandler<ConnectionEventArgs>? OnClientConnected;
    /// <summary>Occurs when a tracked server client disconnects.</summary>
    public event EventHandler<ConnectionEventArgs>? OnClientDisconnected;

    /// <summary>Raises the connection admission event and returns whether subscribers accepted the client.</summary>
    /// <param name="info">The remote endpoint identifying the connecting client.</param>
    /// <returns>Whether the client is accepted.</returns>
    private bool NotifyClientConnected(string info)
    {
        var args = new ConnectionEventArgs(info);
        OnClientConnected?.Invoke(this, args);
        return args.Accept;
    }

    /// <summary>Raises the disconnection event for a removed remote endpoint.</summary>
    /// <param name="info">The address of the disconnected client.</param>
    private void NotifyClientDisconnected(string info)
        => OnClientDisconnected?.Invoke(this, new ConnectionEventArgs(info));

    /// <summary>Owns a connected socket, its framing buffer and its serialized send operations.</summary>
    /// <param name="socket">The accepted TCP socket.</param>
    /// <param name="address">The remote endpoint identifying the client.</param>
    sealed class Peer(Socket socket, string address)
    {
        /// <summary>Stores the connected socket owned by this client.</summary>
        internal readonly Socket Socket = socket;
        /// <summary>Stores the remote endpoint used to identify this client.</summary>
        internal readonly string Address = address;
        /// <summary>Prevents concurrent sends from interleaving on this socket.</summary>
        internal readonly SemaphoreSlim SendLock = new(1);
        /// <summary>Separates this client's incoming stream framing from other clients.</summary>
        internal readonly GXSynchronousMediaBase PacketBuffer = new();
        /// <summary>References the media receiving this client's data after attachment.</summary>
        internal GXNet? Attached;
        /// <summary>Records whether removal and socket cleanup have already started.</summary>
        internal int Removed;
        /// <summary>Completes after admission and this peer's receive callbacks and removal notifications have unwound.</summary>
        internal readonly TaskCompletionSource ReceiveDone = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Completes after the single removal owner finishes child state and listener disconnect callbacks.</summary>
        internal readonly TaskCompletionSource RemovalDone = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    /// <summary>Maps connected remote endpoints to their socket state.</summary>
    readonly ConcurrentDictionary<string, Peer> clients = new();
    /// <summary>Protects connection admission and lifecycle transitions.</summary>
    readonly object admission = new();
    /// <summary>Serializes writes from an unbound client connection.</summary>
    readonly SemaphoreSlim clientSend = new(1);
    /// <summary>Tracks background operations that shutdown must await.</summary>
    readonly HashSet<Task> workers = new();
    /// <summary>Identifies callbacks whose reentrant shutdown must avoid awaiting themselves.</summary>
    static readonly AsyncLocal<GXNet?> callbackOwner = new();
    /// <summary>Tracks the current lifecycle transition under the admission lock.</summary>
    MediaState phase = MediaState.Closed;
    /// <summary>Distinguishes current connection work from earlier open attempts.</summary>
    long generation;
    /// <summary>Signals when the current opening operation has unwound.</summary>
    TaskCompletionSource? openingDone;
    /// <summary>Signals when the current closing operation has completed.</summary>
    TaskCompletionSource? closingDone;
    /// <summary>Cancels socket operations belonging to the active connection.</summary>
    CancellationTokenSource? stopping;
    /// <summary>Stores the listener or directly connected client socket.</summary>
    Socket? socket;
    /// <summary>Identifies the accepted client owned by an attached media.</summary>
    Peer? attached;
    /// <summary>References the listener that owns an attached client socket.</summary>
    GXNet? parent;
    /// <summary>Publishes whether data operations may use the connection.</summary>
    volatile bool open;

    /// <summary>Creates a closed TCP client with default transport options.</summary>
    public GXNet()
    {
        ConfigurableSettings = (int)(AvailableMediaSettings.Server | AvailableMediaSettings.Protocol | AvailableMediaSettings.Port | AvailableMediaSettings.Host | AvailableMediaSettings.UseIPv6);
    }

    /// <summary>Creates a closed client for a remote network endpoint.</summary>
    /// <param name="protocol">The transport protocol.</param>
    /// <param name="hostName">The remote host name or IP address.</param>
    /// <param name="connectionPort">The remote port, validated when opening.</param>
    public GXNet(NetworkType protocol, string hostName, int connectionPort) : this()
    {
        Protocol = protocol;
        HostName = hostName;
        Port = connectionPort;
    }

    /// <summary>Creates a closed server listening on the selected protocol and port.</summary>
    /// <param name="protocol">The transport protocol.</param>
    /// <param name="listeningPort">The local port, validated when opening.</param>
    public GXNet(NetworkType protocol, int listeningPort) : this()
    {
        Protocol = protocol;
        Port = listeningPort;
        Server = true;
    }

    /// <summary>Gets or sets whether communication uses TCP streams or UDP datagrams.</summary>
    public NetworkType Protocol { get; set; } = NetworkType.Tcp;

    /// <summary>Gets or sets the remote client host or the local server bind address.</summary>
    public string HostName { get; set; } = "";
    /// <summary>Gets or sets the remote client port or local listening port in the range 1 through 65535.</summary>
    public int Port { get; set; }
    /// <summary>Gets or sets whether this instance listens for remote clients.</summary>
    public bool Server { get; set; }
    /// <summary>Gets or sets whether name resolution and wildcard binding use IPv6.</summary>
    public bool UseIPv6 { get; set; }
    /// <summary>Gets or sets the opening timeout in milliseconds; zero and minus one disable the deadline.</summary>
    public int WaitTime { get; set; } = 60000;
    /// <summary>Gets or sets the maximum admitted TCP clients; zero allows an unlimited number.</summary>
    public int MaxClientCount { get; set; }
    /// <summary>Gets or sets whether operating-system TCP keepalive probes are enabled on clients and accepted connections.</summary>
    public bool EnableTcpKeepAlive { get; set; } = true;
    /// <summary>Gets or sets the positive idle time in seconds before the first TCP keepalive probe, where supported.</summary>
    public int TcpKeepAliveIdleSeconds { get; set; } = 60;
    /// <summary>Gets or sets the positive interval in seconds between unanswered TCP keepalive probes, where supported.</summary>
    public int TcpKeepAliveIntervalSeconds { get; set; } = 10;
    /// <summary>Gets or sets the positive number of unanswered TCP keepalive probes before OS failure detection, where supported.</summary>
    public int TcpKeepAliveRetryCount { get; set; } = 3;
    /// <inheritdoc/>
    public override bool IsOpen => open;
    /// <inheritdoc/>
    public override string Name => $"{HostName}:{Port}";
    /// <inheritdoc/>
    public override string MediaType => "Net";
    /// <summary>Returns a snapshot of the endpoints currently tracked by a TCP server.</summary>
    /// <returns>The connected client endpoint strings.</returns>
    public string[] GetActiveClients() => clients.Keys.ToArray();
    /// <summary>Reports all invalid network settings without opening a connection.</summary>
    /// <returns>Localized, property-specific errors, or an empty list when the settings are valid.</returns>
    public override IReadOnlyList<GXValidationError> Validate()
    {
        List<GXValidationError> errors = new();
        if (Port <= 0 || Port > 65535)
        {
            errors.Add(new(nameof(Port), "OutOfRange", global::Gurux.Net.Properties.Resources.PortOutOfRange));
        }
        if (!Server && string.IsNullOrWhiteSpace(HostName))
        {
            errors.Add(new(nameof(HostName), "Required", global::Gurux.Net.Properties.Resources.RemoteHostRequired));
        }
        if (!Enum.IsDefined(Protocol))
        {
            errors.Add(new(nameof(Protocol), "InvalidValue", global::Gurux.Net.Properties.Resources.InvalidProtocol));
        }
        if (MaxClientCount < 0)
        {
            errors.Add(new(nameof(MaxClientCount), "OutOfRange", global::Gurux.Net.Properties.Resources.InvalidMaxClientCount));
        }
        if (WaitTime < -1)
        {
            errors.Add(new(nameof(WaitTime), "OutOfRange", global::Gurux.Net.Properties.Resources.InvalidWaitTime));
        }
        if (TcpKeepAliveIdleSeconds <= 0)
        {
            errors.Add(new(nameof(TcpKeepAliveIdleSeconds), "OutOfRange", global::Gurux.Net.Properties.Resources.InvalidKeepAliveIdle));
        }
        if (TcpKeepAliveIntervalSeconds <= 0)
        {
            errors.Add(new(nameof(TcpKeepAliveIntervalSeconds), "OutOfRange", global::Gurux.Net.Properties.Resources.InvalidKeepAliveInterval));
        }
        if (TcpKeepAliveRetryCount <= 0)
        {
            errors.Add(new(nameof(TcpKeepAliveRetryCount), "OutOfRange", global::Gurux.Net.Properties.Resources.InvalidKeepAliveRetryCount));
        }
        if (Protocol == NetworkType.Udp && Eop != null)
        {
            errors.Add(new(nameof(Eop), "InvalidCombination", global::Gurux.Net.Properties.Resources.UdpFramingUnsupported));
        }
        return errors;
    }
    /// <summary>Rejects invalid settings before starting a connection attempt.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A numeric setting is outside its supported range or the protocol is undefined.</exception>
    /// <exception cref="ArgumentException">A client has no remote host.</exception>
    /// <exception cref="InvalidOperationException">UDP is configured with TCP packet framing.</exception>
    private void ValidateForOpen()
    {
        IReadOnlyList<GXValidationError> errors = Validate();
        if (errors.Count != 0)
        {
            GXValidationError error = errors[0];
            if (error.Code == "InvalidCombination")
            {
                throw new InvalidOperationException(error.Message);
            }
            if (error.Code == "OutOfRange" || error.PropertyName == nameof(Protocol))
            {
                throw new ArgumentOutOfRangeException(error.PropertyName, error.Message);
            }
            throw new ArgumentException(error.Message, error.PropertyName);
        }
    }
    /// <inheritdoc/>
    public override async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        CancellationTokenSource attempt;
        TaskCompletionSource completion;
        long currentGeneration;
        lock (admission)
        {
            if (phase == MediaState.Open) { return; }
            if (parent != null) { throw new InvalidOperationException(global::Gurux.Net.Properties.Resources.AttachedReconnect); }
            if (phase != MediaState.Closed) { throw new InvalidOperationException(global::Gurux.Net.Properties.Resources.LifecycleInProgress); }
            ValidateForOpen();
            phase = MediaState.Opening;
            currentGeneration = ++generation;
            attempt = new CancellationTokenSource();
            if (WaitTime > 0) { attempt.CancelAfter(WaitTime); }
            stopping = attempt;
            openingDone = completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        GXNet? previousOwner = callbackOwner.Value;
        callbackOwner.Value = this;
        var cancellationRegistration = cancellationToken.Register(() => attempt.Cancel());
        Socket? pending = null;
        try
        {
            SetState(MediaState.Opening);
            attempt.Token.ThrowIfCancellationRequested();
            var address = string.IsNullOrWhiteSpace(HostName) ? (UseIPv6 ? IPAddress.IPv6Any : IPAddress.Any) : (await Dns.GetHostAddressesAsync(HostName, attempt.Token).ConfigureAwait(false)).First(a => a.AddressFamily == (UseIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork));
            pending = new Socket(address.AddressFamily, Protocol == NetworkType.Tcp ? SocketType.Stream : SocketType.Dgram, Protocol == NetworkType.Tcp ? ProtocolType.Tcp : ProtocolType.Udp);
            if (Protocol == NetworkType.Tcp && !Server) { ConfigureTcpKeepAlive(pending); }
            if (Server)
            {
                pending.Bind(new IPEndPoint(address, Port));
                if (Protocol == NetworkType.Tcp) { pending.Listen(128); }
            }
            else { await pending.ConnectAsync(new IPEndPoint(address, Port), attempt.Token).ConfigureAwait(false); }
            lock (admission)
            {
                attempt.Token.ThrowIfCancellationRequested();
                if (generation != currentGeneration || phase != MediaState.Opening) { throw new OperationCanceledException(attempt.Token); }
                socket = pending;
                attempt.CancelAfter(Timeout.Infinite);
                if (Server) { Port = ((IPEndPoint)pending.LocalEndPoint!).Port; }
                ReceiveBuffer.Open(); open = true; phase = MediaState.Open;
            }
            SetState(MediaState.Open);
            lock (admission)
            {
                if (generation == currentGeneration && phase == MediaState.Open)
                {
                    var activeSocket = pending;
                    StartWorker(() => Server && Protocol == NetworkType.Tcp ? AcceptLoop(activeSocket, attempt.Token) : ReceiveLoop(activeSocket, null, attempt.Token, currentGeneration));
                }
            }
        }
        catch
        {
            pending?.Dispose();
            bool reportClosed = false;
            lock (admission)
            {
                if (generation == currentGeneration && (phase == MediaState.Opening || (phase == MediaState.Open && ReferenceEquals(socket, pending))))
                {
                    socket = null; open = false; phase = MediaState.Closed; stopping = null;
                    attempt.Cancel(); ReceiveBuffer.Close();
                    reportClosed = true;
                }
            }
            if (reportClosed) { SetState(MediaState.Closed); }
            throw;
        }
        finally
        {
            cancellationRegistration.Dispose();
            callbackOwner.Value = previousOwner;
            completion.TrySetResult();
            lock (admission) { if (openingDone == completion) { openingDone = null; } }
            // The active connection owns the token source until shutdown.
            if (!open || generation != currentGeneration) { attempt.Dispose(); }
        }
    }
    /// <summary>Tracks and starts an asynchronous socket worker whose errors are reported through media events.</summary>
    /// <param name="action">The asynchronous operation to run.</param>
    void StartWorker(Func<Task> action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (admission)
        {
            workers.Add(completion.Task);
        }
        _ = Task.Run(Run);
        // Runs the registered worker, reports failures and signals cleanup completion.
        // A task completing after the worker and tracking cleanup finish.
        async Task Run()
        {
            var previous = callbackOwner.Value;
            callbackOwner.Value = this;
            try { await action().ConfigureAwait(false); }
            catch (Exception error) { ReportBackgroundError(error); }
            finally
            {
                callbackOwner.Value = previous;
                lock (admission)
                {
                    workers.Remove(completion.Task);
                }
                completion.TrySetResult();
            }
        }
    }
    /// <summary>Accepts TCP clients, applies admission limits and starts independent receive operations.</summary>
    /// <param name="listener">The listening TCP socket.</param>
    /// <param name="token">The connection lifetime cancellation token.</param>
    /// <returns>A task completing when client acceptance stops.</returns>
    async Task AcceptLoop(Socket listener, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Socket? accepted = null;
            Peer? admitted = null;
            bool receiveStarted = false;
            try
            {
                accepted = await listener.AcceptAsync(token).ConfigureAwait(false);
                ConfigureTcpKeepAlive(accepted);
                var peer = new Peer(accepted, accepted.RemoteEndPoint!.ToString()!);
                lock (admission)
                {
                    if (!open || token.IsCancellationRequested || (MaxClientCount != 0 && clients.Count >= MaxClientCount)) { accepted.Dispose(); continue; }
                    clients[peer.Address] = peer;
                    admitted = peer;
                }
                if (!NotifyClientConnected(peer.Address)) { RemovePeer(peer); continue; }
                bool startReceive;
                lock (admission)
                {
                    startReceive = open && !token.IsCancellationRequested && Volatile.Read(ref peer.Removed) == 0;
                    if (startReceive)
                    {
                        StartWorker(() => ReceiveLoop(accepted, peer, token, generation));
                        receiveStarted = true;
                    }
                }
                if (!startReceive) { RemovePeer(peer); }
            }
            catch (Exception) when (token.IsCancellationRequested) { accepted?.Dispose(); break; }
            catch (Exception ex)
            {
                accepted?.Dispose();
                if (admitted != null) { try { RemovePeer(admitted); } catch (Exception cleanupError) { ReportBackgroundError(cleanupError); } }
                ReportBackgroundError(ex);
            }
            finally
            {
                if (admitted != null && !receiveStarted)
                {
                    await admitted.RemovalDone.Task.ConfigureAwait(false);
                    admitted.ReceiveDone.TrySetResult();
                }
            }
        }
    }
    /// <summary>Publishes socket input while preserving datagram and per-client stream boundaries.</summary>
    /// <param name="source">The socket to read.</param>
    /// <param name="peer">The accepted client state, or null for a direct connection.</param>
    /// <param name="token">The connection lifetime cancellation token.</param>
    /// <param name="currentGeneration">The opening generation that owns this worker.</param>
    /// <returns>A task completing after receive and cleanup finish.</returns>
    async Task ReceiveLoop(Socket source, Peer? peer, CancellationToken token, long currentGeneration)
    {
        byte[] buffer = new byte[65536];
        try
        {
            while (!token.IsCancellationRequested)
            {
                int count; string? sender;
                if (Protocol == NetworkType.Udp && Server)
                {
                    var result = await source.ReceiveFromAsync(buffer, SocketFlags.None, new IPEndPoint(UseIPv6 ? IPAddress.IPv6Any : IPAddress.Any, 0), token).ConfigureAwait(false);
                    count = result.ReceivedBytes; sender = result.RemoteEndPoint.ToString();
                }
                else { count = await source.ReceiveAsync(buffer, SocketFlags.None, token).ConfigureAwait(false); sender = peer?.Address ?? source.RemoteEndPoint?.ToString(); if (count == 0 && Protocol == NetworkType.Tcp) { break; } }
                byte[] data = buffer.AsSpan(0, count).ToArray();
                if (peer != null && Volatile.Read(ref peer.Removed) != 0) { break; }
                if (peer != null && Volatile.Read(ref peer.Attached) is GXNet child) { AddBytesReceived(count); child.PublishReceived(data, sender); }
                else if (Protocol == NetworkType.Udp)
                {
                    // A fresh framing buffer prevents Eop changes after opening from
                    // ever combining datagrams belonging to different senders.
                    var datagram = new GXSynchronousMediaBase();
                    PublishReceived(data, sender, datagram);
                    datagram.Close();
                }
                else { PublishReceived(data, sender, peer?.PacketBuffer); }
            }
        }
        catch (Exception ex) when (token.IsCancellationRequested || ex is ObjectDisposedException || ex is SocketException { SocketErrorCode: SocketError.ConnectionReset or SocketError.ConnectionAborted or SocketError.OperationAborted }) { }
        catch (Exception ex) { (peer?.Attached ?? this).ReportBackgroundError(ex); }
        finally
        {
            if (peer != null)
            {
                try { RemovePeer(peer); }
                catch (Exception ex) { (peer?.Attached ?? this).ReportBackgroundError(ex); }
                finally
                {
                    await peer.RemovalDone.Task.ConfigureAwait(false);
                    peer.ReceiveDone.TrySetResult();
                }
            }
            else if (!token.IsCancellationRequested)
            {
                bool current;
                lock (admission)
                {
                    current = generation == currentGeneration && phase == MediaState.Open;
                }
                if (current) { await CloseAsync().ConfigureAwait(false); }
            }
        }
    }
    /// <summary>Reports a worker failure while allowing cleanup to continue if an error subscriber throws.</summary>
    /// <param name="error">The worker or cleanup failure.</param>
    void ReportBackgroundError(Exception error)
    {
        try { ReportError(error); }
        catch { /* A throwing error subscriber must not terminate transport cleanup. */ }
    }
    /// <summary>Applies keepalive settings and retains operating-system defaults when optional tuning is unsupported.</summary>
    /// <param name="target">The connected TCP socket to configure.</param>
    void ConfigureTcpKeepAlive(Socket target)
    {
        // Use the managed options so .NET maps them to the platform TCP stack.
        // Some mobile/older OS stacks support SO_KEEPALIVE but not every tuning
        // option. Unsupported tuning keeps basic keepalive and the OS defaults.
        try { target.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, EnableTcpKeepAlive); }
        catch (Exception error) when (UnsupportedKeepAliveOption(error)) { return; }
        if (!EnableTcpKeepAlive) { return; }
        SetOptional(SocketOptionName.TcpKeepAliveTime, TcpKeepAliveIdleSeconds);
        SetOptional(SocketOptionName.TcpKeepAliveInterval, TcpKeepAliveIntervalSeconds);
        SetOptional(SocketOptionName.TcpKeepAliveRetryCount, TcpKeepAliveRetryCount);
        // Applies an optional TCP tuning value while retaining defaults on unsupported platforms.
        // option: The TCP keepalive option to configure.
        // value: The configured idle time, interval or probe count.
        void SetOptional(SocketOptionName option, int value)
        {
            try { target.SetSocketOption(SocketOptionLevel.Tcp, option, value); }
            catch (Exception error) when (UnsupportedKeepAliveOption(error)) { }
        }
    }
    /// <summary>Determines whether a socket option failure permits fallback to operating-system defaults.</summary>
    /// <param name="error">The socket configuration failure.</param>
    /// <returns>Whether the failure indicates an unsupported or invalid platform option.</returns>
    static bool UnsupportedKeepAliveOption(Exception error) => error is NotSupportedException ||
        error is SocketException { SocketErrorCode: SocketError.ProtocolOption or SocketError.OperationNotSupported or SocketError.ProtocolNotSupported or SocketError.SocketNotSupported or SocketError.InvalidArgument };
    /// <summary>Removes and disposes an accepted client once, then closes its attached media and reports disconnection.</summary>
    /// <param name="peer">The accepted client to remove.</param>
    void RemovePeer(Peer peer)
    {
        GXNet? child;
        lock (peer)
        {
            if (peer.Removed != 0) { return; }
            peer.Removed = 1;
            clients.TryRemove(peer.Address, out _); peer.Socket.Dispose();
            peer.PacketBuffer.Close();
            child = peer.Attached;
        }
        var previous = callbackOwner.Value;
        callbackOwner.Value = this;
        try
        {
            if (child != null)
            {
                bool notify;
                lock (child.admission) { notify = child.phase != MediaState.Closing; child.open = false; child.phase = MediaState.Closed; }
                child.ReceiveBuffer.Close();
                if (notify) { child.SetState(MediaState.Closed); }
            }
        }
        finally
        {
            try { NotifyClientDisconnected(peer.Address); }
            finally
            {
                callbackOwner.Value = previous;
                peer.RemovalDone.TrySetResult();
            }
        }
    }
    /// <summary>Disconnects a currently tracked client; unknown addresses are ignored.</summary>
    /// <param name="address">The remote endpoint returned by <see cref="GetActiveClients"/>.</param>
    public void DisconnectClient(string address) { if (clients.TryGetValue(address, out var peer)) { RemovePeer(peer); } }
    /// <summary>Routes one accepted TCP client's data into an independent media and synchronous receive buffer.</summary>
    /// <param name="info">The endpoint of a currently connected TCP client.</param>
    /// <returns>The existing or newly created media attached to this client.</returns>
    /// <exception cref="ArgumentException">The endpoint is no longer connected.</exception>
    public GXNet Attach(object info)
    {
        if (!clients.TryGetValue(Convert.ToString(info) ?? "", out var peer)) { throw new ArgumentException(global::Gurux.Net.Properties.Resources.ClientNotConnected, nameof(info)); }
        lock (peer)
        {
            if (peer.Removed != 0 || !clients.TryGetValue(peer.Address, out var current) || !ReferenceEquals(peer, current)) { throw new ArgumentException(global::Gurux.Net.Properties.Resources.ClientDisconnected, nameof(info)); }
            if (peer.Attached != null) { return peer.Attached; }
            var child = new GXNet(Protocol, HostName, Port) { parent = this, attached = peer, open = true, phase = MediaState.Open };
            child.EnableTcpKeepAlive = EnableTcpKeepAlive; child.TcpKeepAliveIdleSeconds = TcpKeepAliveIdleSeconds;
            child.TcpKeepAliveIntervalSeconds = TcpKeepAliveIntervalSeconds; child.TcpKeepAliveRetryCount = TcpKeepAliveRetryCount;
            child.ReceiveBuffer.Open(); peer.Attached = child; return child;
        }
    }
    /// <inheritdoc/>
    public override async ValueTask SendAsync(ReadOnlyMemory<byte> data, string? receiver = null, CancellationToken cancellationToken = default)
    {
        if (!open) { throw new InvalidOperationException(global::Gurux.Net.Properties.Resources.MediaClosed); }
        if (attached != null) { await SendPeer(attached, data, cancellationToken).ConfigureAwait(false); return; }
        if (Server && Protocol == NetworkType.Tcp)
        {
            if (receiver == null) { throw new ArgumentException(global::Gurux.Net.Properties.Resources.ClientAddressRequired, nameof(receiver)); }
            if (!clients.TryGetValue(receiver, out var peer)) { throw new ArgumentException(global::Gurux.Net.Properties.Resources.ClientNotConnected, nameof(receiver)); }
            await SendPeer(peer, data, cancellationToken).ConfigureAwait(false); return;
        }
        var target = socket ?? throw new InvalidOperationException(global::Gurux.Net.Properties.Resources.MediaClosed);
        if (Protocol == NetworkType.Udp && Server)
        {
            if (receiver == null) { throw new ArgumentException(global::Gurux.Net.Properties.Resources.DestinationRequired, nameof(receiver)); }
            AddBytesSent(await target.SendToAsync(data, SocketFlags.None, IPEndPoint.Parse(receiver), cancellationToken).ConfigureAwait(false));
        }
        else
        {
            await clientSend.WaitAsync(cancellationToken).ConfigureAwait(false);
            try { await SendAll(target, data, cancellationToken).ConfigureAwait(false); }
            finally { clientSend.Release(); }
        }
    }
    /// <summary>Serializes a complete write to an accepted client socket.</summary>
    /// <param name="peer">The connected client receiving the data.</param>
    /// <param name="data">The bytes to send.</param>
    /// <param name="token">The cancellation token for waiting and writing.</param>
    /// <returns>An operation completing when all bytes are sent.</returns>
    async ValueTask SendPeer(Peer peer, ReadOnlyMemory<byte> data, CancellationToken token)
    {
        await peer.SendLock.WaitAsync(token).ConfigureAwait(false);
        try { await SendAll(peer.Socket, data, token).ConfigureAwait(false); }
        finally { peer.SendLock.Release(); }
    }
    /// <summary>Writes a complete TCP payload or one UDP datagram and updates byte counters.</summary>
    /// <param name="target">The destination socket.</param>
    /// <param name="data">The payload bytes.</param>
    /// <param name="token">The cancellation token for socket writes.</param>
    /// <returns>An operation completing when the payload has been sent.</returns>
    async ValueTask SendAll(Socket target, ReadOnlyMemory<byte> data, CancellationToken token)
    {
        if (Protocol == NetworkType.Udp) { AddBytesSent(await target.SendAsync(data, SocketFlags.None, token).ConfigureAwait(false)); return; }
        while (!data.IsEmpty) { int count = await target.SendAsync(data, SocketFlags.None, token).ConfigureAwait(false); if (count == 0) { throw new IOException(global::Gurux.Net.Properties.Resources.SocketClosedDuringSend); } AddBytesSent(count); parent?.AddBytesSent(count); data = data[count..]; }
    }
    /// <summary>Closes this connection and awaits its receive callbacks unless called reentrantly from their owner.</summary>
    /// <remarks>Closing an attached media drains only its peer; the listening server remains open.</remarks>
    /// <inheritdoc/>
    public override async Task CloseAsync()
    {
        Peer[] removed = [];
        Task[] background = [];
        Socket? closingSocket = null;
        CancellationTokenSource? cancellation = null;
        Task? opening = null;
        Task? existingClose = null;
        TaskCompletionSource? completion = null;
        long closingGeneration = 0;
        bool reentrant = callbackOwner.Value == this
            || (parent != null && callbackOwner.Value == parent)
            || callbackOwner.Value?.parent == this;
        lock (admission)
        {
            if (phase == MediaState.Closed && !open)
            { existingClose = Task.WhenAll(workers.Concat(openingDone == null ? [] : new[] { openingDone.Task })); }
            else if (phase == MediaState.Closing)
            { existingClose = Task.WhenAll(workers.Concat(openingDone == null ? [] : new[] { openingDone.Task }).Append(closingDone?.Task ?? Task.CompletedTask)); }
            else
            {
                phase = MediaState.Closing; open = false; closingGeneration = generation;
                closingDone = completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
                removed = clients.Values.ToArray();
                background = workers.ToArray();
                opening = openingDone?.Task;
                closingSocket = socket; socket = null;
                cancellation = stopping; stopping = null;
            }
        }
        if (attached != null)
        {
            background = background.Append(attached.ReceiveDone.Task).ToArray();
            if (completion == null)
            {
                existingClose = Task.WhenAll(existingClose ?? Task.CompletedTask, attached.ReceiveDone.Task, closingDone?.Task ?? Task.CompletedTask);
            }
        }
        if (completion == null)
        {
            if (!reentrant && existingClose != null) { await existingClose.ConfigureAwait(false); }
            return;
        }
        var previous = callbackOwner.Value;
        callbackOwner.Value = this;
        Exception? callbackError = null;
        // Runs a shutdown step and retains its first failure so remaining cleanup can continue.
        // action: The shutdown operation to attempt.
        void Cleanup(Action action) { try { action(); } catch (Exception ex) { callbackError ??= ex; } }
        try
        {
            Cleanup(() => cancellation?.Cancel()); Cleanup(() => closingSocket?.Dispose()); Cleanup(() => ReceiveBuffer.Close());
            Cleanup(() => SetState(MediaState.Closing));
            if (parent != null && attached != null) { Cleanup(() => parent.RemovePeer(attached)); }
            foreach (var peer in removed) { Cleanup(() => RemovePeer(peer)); }
            // A synchronous event handler cannot await its own worker/open operation.
            // External callers wait for every tracked operation to unwind.
            if (!reentrant)
            {
                if (opening != null) { await opening.ConfigureAwait(false); }
                await Task.WhenAll(background).ConfigureAwait(false);
            }
            lock (admission) { if (generation == closingGeneration) { phase = MediaState.Closed; } }
            Cleanup(() => SetState(MediaState.Closed));
        }
        finally
        {
            callbackOwner.Value = previous;
            completion.TrySetResult();
            if (opening is { IsCompleted: false }) { _ = opening.ContinueWith(_ => cancellation?.Dispose(), TaskScheduler.Default); }
            else { cancellation?.Dispose(); }
        }
        if (callbackError != null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(callbackError).Throw(); }
    }
    /// <inheritdoc/>
    public override Task<bool> ReceiveAsync<T>(ReceiveParameters<T> args, CancellationToken cancellationToken = default)
    {
        if (Server && Protocol == NetworkType.Tcp) { throw new InvalidOperationException(global::Gurux.Net.Properties.Resources.AttachBeforeReceive); }
        return base.ReceiveAsync(args, cancellationToken);
    }
    /// <inheritdoc/>
    public override void Copy(object target)
    {
        if (target is not GXNet other) { throw new ArgumentException(global::Gurux.Net.Properties.Resources.ExpectedNetworkMedia, nameof(target)); }
        other.Protocol = Protocol; other.HostName = HostName; other.Port = Port; other.Server = Server; other.UseIPv6 = UseIPv6; other.MaxClientCount = MaxClientCount; other.WaitTime = WaitTime;
        other.EnableTcpKeepAlive = EnableTcpKeepAlive; other.TcpKeepAliveIdleSeconds = TcpKeepAliveIdleSeconds;
        other.TcpKeepAliveIntervalSeconds = TcpKeepAliveIntervalSeconds; other.TcpKeepAliveRetryCount = TcpKeepAliveRetryCount;
    }
    /// <summary>Gets or replaces network settings as legacy XML fragments using IP, Port, Protocol, Server and IPv6.</summary>
    /// <remarks>Also accepts the previous Net wrapper and HostName/UseIPv6 element names.
    /// Null or empty input restores defaults, including the unconfigured zero port.</remarks>
    /// <exception cref="InvalidOperationException">The media is open or a lifecycle transition is in progress.</exception>
    /// <exception cref="System.Xml.XmlException">The supplied XML is malformed.</exception>
    /// <exception cref="ArgumentException">The protocol cannot be parsed.</exception>
    /// <exception cref="FormatException">A numeric or Boolean option is malformed.</exception>
    public override string? Settings
    {
        get
        {
            var xml = new XElement("Net",
                new XElement("IP", HostName), new XElement("Port", Port),
                new XElement("Protocol", (int)Protocol), new XElement("Server", Server ? 1 : 0),
                new XElement("IPv6", UseIPv6 ? 1 : 0),
                new XElement("MaxClientCount", MaxClientCount),
                new XElement("WaitTime", WaitTime),
                new XElement("EnableTcpKeepAlive", EnableTcpKeepAlive),
                new XElement("TcpKeepAliveIdleSeconds", TcpKeepAliveIdleSeconds),
                new XElement("TcpKeepAliveIntervalSeconds", TcpKeepAliveIntervalSeconds),
                new XElement("TcpKeepAliveRetryCount", TcpKeepAliveRetryCount));
            return string.Concat(xml.Elements().Select(element => element.ToString(SaveOptions.DisableFormatting)));
        }
        set
        {
            var root = XElement.Parse("<Settings>" + value + "</Settings>");
            var xml = root.Element("Net") ?? root;
            var settings = new NetSettings
            {
                Protocol = Enum.Parse<NetworkType>((string?)xml.Element("Protocol") ?? "Tcp"),
                HostName = (string?)xml.Element("IP") ?? (string?)xml.Element("HostName") ?? string.Empty,
                Port = (int?)xml.Element("Port") ?? 0,
                Server = (bool?)xml.Element("Server") ?? false,
                UseIPv6 = (bool?)xml.Element("IPv6") ?? (bool?)xml.Element("UseIPv6") ?? false,
                MaxClientCount = (int?)xml.Element("MaxClientCount") ?? 0,
                WaitTime = (int?)xml.Element("WaitTime") ?? 60000,
                EnableTcpKeepAlive = (bool?)xml.Element("EnableTcpKeepAlive") ?? true,
                TcpKeepAliveIdleSeconds = (int?)xml.Element("TcpKeepAliveIdleSeconds") ?? 60,
                TcpKeepAliveIntervalSeconds = (int?)xml.Element("TcpKeepAliveIntervalSeconds") ?? 10,
                TcpKeepAliveRetryCount = (int?)xml.Element("TcpKeepAliveRetryCount") ?? 3,
            };
            ApplySettings(settings);
        }
    }

    /// <summary>Gets or replaces the same network settings using JSON with source-generated metadata.</summary>
    /// <remarks>Null restores defaults. Settings changes require closed media.</remarks>
    /// <exception cref="JsonException">The supplied JSON is malformed or is the JSON null value.</exception>
    /// <exception cref="InvalidOperationException">The media is open or a lifecycle transition is in progress.</exception>
    public override string? JsonSettings
    {
        get => JsonSerializer.Serialize(new NetSettings
        {
            Protocol = Protocol,
            HostName = HostName,
            Port = Port,
            Server = Server,
            UseIPv6 = UseIPv6,
            MaxClientCount = MaxClientCount,
            WaitTime = WaitTime,
            EnableTcpKeepAlive = EnableTcpKeepAlive,
            TcpKeepAliveIdleSeconds = TcpKeepAliveIdleSeconds,
            TcpKeepAliveIntervalSeconds = TcpKeepAliveIntervalSeconds,
            TcpKeepAliveRetryCount = TcpKeepAliveRetryCount,
        }, NetJsonContext.Default.NetSettings);
        set => ApplySettings(value == null ? new NetSettings()
            : JsonSerializer.Deserialize(value, NetJsonContext.Default.NetSettings)
                ?? throw new JsonException());
    }

    /// <summary>Applies a completely parsed settings snapshot without exposing partial parsing changes.</summary>
    /// <param name="settings">The network configuration to apply while closed.</param>
    /// <exception cref="InvalidOperationException">The media is open or a lifecycle transition is in progress.</exception>
    private void ApplySettings(NetSettings settings)
    {
        lock (admission)
        {
            if (phase != MediaState.Closed || open)
            {
                throw new InvalidOperationException(global::Gurux.Net.Properties.Resources.CloseBeforeSettings);
            }
            Protocol = settings.Protocol;
            HostName = settings.HostName;
            Port = settings.Port;
            Server = settings.Server;
            UseIPv6 = settings.UseIPv6;
            MaxClientCount = settings.MaxClientCount;
            WaitTime = settings.WaitTime;
            EnableTcpKeepAlive = settings.EnableTcpKeepAlive;
            TcpKeepAliveIdleSeconds = settings.TcpKeepAliveIdleSeconds;
            TcpKeepAliveIntervalSeconds = settings.TcpKeepAliveIntervalSeconds;
            TcpKeepAliveRetryCount = settings.TcpKeepAliveRetryCount;
        }
    }
}
