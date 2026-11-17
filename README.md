# Gurux.Net

Gurux.Net is a TCP and UDP communication library for .NET 10 and .NET 11. It supports asynchronous clients and servers using managed sockets, with shared Gurux receive buffering and no dependency on a UI framework.

The transport can be used on Windows, Linux, macOS, Android and iOS. Mobile applications must supply the required network permissions and respect operating-system restrictions on background execution. Platform support does not imply that every device or network scenario has been physically tested.

## Packages and requirements

| Package | Purpose |
| --- | --- |
| Gurux.Net | UI-independent TCP and UDP transport. |
| Gurux.Net.Avalonia | Optional Avalonia settings editor. |
| Gurux.Net.Blazor | Optional Blazor settings components. |
| Gurux.Net.Maui | Optional native MAUI settings editor. |

The examples below describe the **9.0 API**. Earlier 8.x packages use different APIs; use a matching 9.0 package release or build the current source.

The transport library targets `net10.0` and `net11.0`. Building from source uses the stable .NET 11 SDK selected by [global.json](global.json), including when building .NET 10 outputs. MAUI workloads are required only for the optional MAUI adapter and native example hosts.

## TCP client

This example sends three bytes and prints incoming bytes as hexadecimal. It requires a TCP server listening at the selected endpoint.

~~~csharp
using Gurux.Net;
using Gurux.Net.Enums;

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await using var media = new GXNet(NetworkType.Tcp, "127.0.0.1", 4059);

media.OnReceived += (_, args) =>
{
    if (args.Data is byte[] data)
    {
        Console.WriteLine(Convert.ToHexString(data));
    }
};

await media.OpenAsync(timeout.Token);
await media.SendAsync(new byte[] { 1, 2, 3 }, cancellationToken: timeout.Token);
await Task.Delay(TimeSpan.FromSeconds(1), timeout.Token);
await media.CloseAsync();
~~~

The delay only keeps this event-driven example alive briefly; it does not wait for a complete protocol reply. Use buffered receive for request/reply exchanges.

## Buffered request and reply

Enter buffered mode **before sending**, and keep its scope alive until the awaited receive finishes. This prevents a fast response from arriving before buffering begins.

~~~csharp
using Gurux.Common;
using Gurux.Net;
using Gurux.Net.Enums;

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await using var media = new GXNet(NetworkType.Tcp, "127.0.0.1", 4059);
await media.OpenAsync(timeout.Token);

using (media.BeginSynchronous())
{
    var reply = new ReceiveParameters<byte[]>
    {
        Count = 3,
        WaitTime = 5000
    };

    await media.SendAsync(new byte[] { 1, 2, 3 }, cancellationToken: timeout.Token);
    if (await media.ReceiveAsync(reply, timeout.Token) && reply.Reply is byte[] response)
    {
        Console.WriteLine(Convert.ToHexString(response));
    }
}
~~~

This example expects exactly three reply bytes. Replace the count with the conditions required by your protocol. `ReceiveParameters.Eop` accepts a terminator or alternative terminators and matches across TCP fragments. A completed delimiter-based reply includes the terminator.

A receive timeout or connection closure returns `false`; cancellation throws `OperationCanceledException`. With `AllData` enabled, a failed receive can still return partial data. Do not reuse the same receive parameters concurrently.

`BeginSynchronous()` enables buffering without holding a Monitor lock across `await`. Use the legacy `Synchronous` lock only for entirely blocking exchanges.

## TCP server

~~~csharp
using Gurux.Net;
using Gurux.Net.Enums;

await using var server = new GXNet(NetworkType.Tcp, 4059)
{
    HostName = "127.0.0.1",
    MaxClientCount = 10
};

server.OnReceived += (_, args) =>
{
    if (args.Data is byte[] data)
    {
        Console.WriteLine(Convert.ToHexString(data));
    }
};

await server.OpenAsync();
Console.ReadLine();
await server.CloseAsync();
~~~

Set `HostName` to the local bind address. An empty value binds all interfaces in the selected address family; `UseIPv6` selects IPv6 instead of IPv4. The example binds only the IPv4 loopback interface.

- `GetActiveClients()` returns the endpoint strings identifying tracked clients.
- Pass an endpoint to `SendAsync(data, endpoint, cancellationToken)` to send to that client.
- `DisconnectClient(endpoint)` disconnects one client while retaining the listener.
- `OnClientConnected` can reject a connection by setting its event argument's `Accept` property to `false`.
- `MaxClientCount = 0` allows unlimited clients. Capacity rejection happens before tracking and raises no client events.
- Every tracked client raises one disconnect notification when removed, including admission-callback rejection, explicit disconnection and listener shutdown.

TCP is a byte stream: one send is not guaranteed to produce one receive event. Set the media's `Eop` for event framing when required; accepted clients have independent framing buffers.

### Independent client sessions

For a server-side buffered exchange, call `Attach(endpoint)` in `OnClientConnected` before starting the request. Register the attached media's handlers there as needed. The returned `GXNet` routes that client's data into its own receive buffer.

Use the same `BeginSynchronous()` / `SendAsync()` / `ReceiveAsync()` pattern on the attached media. Listener-wide buffered receives are rejected to prevent different clients' data from being mixed.

Repeated attachment to the same active client returns the same attached media. Closing it disconnects only that client. Attached media cannot reconnect independently; the listener continues accepting connections.

## UDP

Select `NetworkType.Udp` with the same client or server constructors.

UDP clients send to their configured host and port. UDP servers receive sender endpoints in `ReceiveEventArgs.SenderInfo`; pass the endpoint to `SendAsync` to reply.

Datagrams already have message boundaries, so configuring the media's `Eop` before opening UDP is rejected. UDP has no TCP-style client connection events or attached sessions.

## Settings

Configure connection settings before opening the media.

| Property | Default | Behavior |
| --- | --- | --- |
| `Protocol` | `NetworkType.Tcp` | Selects TCP or UDP. |
| `HostName` | Empty | Remote host for clients; local bind address for servers. |
| `Port` | `0` | Must be explicitly set to 1–65535 for clients and servers. |
| `Server` | `false` | Enables listening/server mode. |
| `UseIPv6` | `false` | Selects IPv6 name resolution and binding. |
| `MaxClientCount` | `0` | Maximum tracked TCP clients; zero means unlimited. |
| `WaitTime` | `60000` | Opening timeout in milliseconds, including DNS and connection acquisition. Zero and -1 disable the deadline. |

`Validate()` returns localized, property-specific errors without opening a socket. `OpenAsync()` rejects invalid settings before connecting.

`Settings` stores XML configuration; `JsonSettings` stores the same network configuration using source-generated JSON serialization. Replacing either requires closed media. `Copy(target)` copies network settings, including TCP keepalive settings, into another `GXNet`.

### TCP keepalive

Keepalive is enabled by default on outgoing TCP clients and accepted TCP sockets.

| Property | Default |
| --- | --- |
| `EnableTcpKeepAlive` | `true` |
| `TcpKeepAliveIdleSeconds` | `60` |
| `TcpKeepAliveIntervalSeconds` | `10` |
| `TcpKeepAliveRetryCount` | `3` |

Set `EnableTcpKeepAlive = false` before opening to opt out. All three tuning values must be positive.

These are operating-system TCP probes, not an application inactivity timeout. Healthy peers can remain idle and continue answering probes. Detection of unreachable peers depends on OS behavior, retransmissions, power management and network conditions; cable removal is not detected immediately.

Unsupported tuning options fall back to operating-system defaults. If basic keepalive is unavailable, the connection still opens with ordinary TCP failure detection. UDP is unaffected. XML, JSON and `Copy` preserve the keepalive configuration.

## Lifecycle and threading

Prefer `OpenAsync`, `SendAsync`, `ReceiveAsync` and `CloseAsync` in asynchronous applications. Blocking `Open`, `Send`, `Receive` and `Close` remain available for compatibility.

`CloseAsync` cancels an in-progress open and waits for background work. Closing from inside a media callback avoids waiting for that callback itself; a later external close drains the remaining work. Disposing the media asynchronously closes its owned connection.

Events run on transport threads. Marshal UI updates to your application's dispatcher and keep handlers short. Never hold a Monitor lock across `await`.

The optional `IGXMediaIconProvider` contract exposes the embedded PNG icon through `OpenIcon()`. Each call returns an independent stream owned by the caller.

## Optional settings editors

The optional UI packages use `GXNet.ConfigurableSettings` and `AvailableMediaSettings` to control which fields are offered.

The default flags are `Server | Protocol | Port | Host | UseIPv6`. Set `AvailableMediaSettings.All` explicitly to expose every supported field, or choose a smaller set:

~~~csharp
media.ConfigurableSettings = (int)(
    AvailableMediaSettings.Protocol |
    AvailableMediaSettings.Host |
    AvailableMediaSettings.Port);
~~~

The Host control edits the remote address in client mode and the local bind address in server mode. Server-specific fields require the `Server` flag; the maximum-client field also requires `MaxClientCount`. Hidden settings retain their values when saved.

UI belongs to these adapters and the example application; the transport library remains UI-independent.

## Example application

[Gurux.Net.Example](Gurux.Net.Example/README.md) contains the example's own connection logic and native MAUI/Avalonia views. It includes Windows, Linux, Android and iOS hosts, with detailed build and deployment instructions in its README.

From this repository directory, run the Avalonia desktop example:

~~~powershell
dotnet run --project Gurux.Net.Example/Gurux.Net.Example.csproj -f net11.0 -p:BuildDesktop=true -p:GuruxExamplesBuild=true -p:GeneratePackageOnBuild=false
~~~

## Build and test

Run from this repository directory:

~~~powershell
dotnet build Development/Gurux.Net.csproj -p:GeneratePackageOnBuild=false
dotnet run --project tests/Gurux.Net.Tests.csproj -p:GeneratePackageOnBuild=false
~~~

Run only the attached TCP session regressions:

~~~powershell
dotnet run --project tests/Gurux.Net.Tests.csproj -p:GeneratePackageOnBuild=false -- --attached
~~~

The regression executable uses actual loopback sockets without physical device requirements. Coverage includes TCP/UDP traffic, admission limits and rejection, listener survival, EOF/reset, large payloads, send serialization, keepalive configuration, and accept/shutdown races.

Attached-session tests additionally cover binary and UTF-8 payloads, fragmented alternative terminators, peek/minimum counts, partial-data recovery, full-duplex transfers, peer isolation, counters, repeated/stale attachment, reentrant callbacks and close/reopen cleanup. These tests run as part of the normal suite.

Loopback tests do not validate physical cable removal, black-holed networks or mobile background behavior. Validate those scenarios on the target operating system and hardware.

## Migrating from 8.x

The 9.0 implementation uses the current Gurux.Common event argument types, asynchronous lifecycle APIs and shared receive buffering. Legacy virtual-media simulation and bundled WinForms dialogs are no longer part of the transport library.

Import `Gurux.Net.Enums` for `NetworkType`. Use the optional UI adapters for settings dialogs and attached media for independent server-side buffered exchanges.

## License

Gurux.Net is available under either [GPL-2.0-only](LICENSE) or a commercial license from Gurux Ltd. See [commercial licensing information](LICENSES/LicenseRef-Gurux-Commercial) or contact [Gurux Ltd](https://www.gurux.fi/).
