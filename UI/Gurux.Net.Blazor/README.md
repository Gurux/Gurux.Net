# Gurux.Net.Blazor

## Why use this component?

Use this optional package to add a ready-made settings editor for `GXNet` to an interactive Blazor application or a Razor-based settings surface. It supplies localized fields, input validation and a staged Save/Cancel workflow so the host does not have to implement the media settings form itself.

This adapter edits TCP/UDP selection, remote host or local listen address, port, IPv6 and the enabled server/keepalive settings.

The media library can be used without this package in services, command-line tools, automated tests or applications that provide their own settings UI. Reference this adapter when users need to edit those settings interactively.

## Choose the UI for your application

| Host application | Settings adapter |
| --- | --- |
| Avalonia desktop | [Gurux.Net.Avalonia](../Gurux.Net.Avalonia/README.md) |
| Interactive Blazor / Razor UI | [Gurux.Net.Blazor](../Gurux.Net.Blazor/README.md) |
| Native .NET MAUI | [Gurux.Net.Maui](../Gurux.Net.Maui/README.md) |

Choose the adapter matching the host's UI framework; installing all three is unnecessary. A native MAUI host can use the MAUI adapter directly without a BlazorWebView. The Blazor adapter provides web components rather than native MAUI or Avalonia controls.

## How the pieces fit together

| Layer | Responsibility |
| --- | --- |
| [Gurux.Net transport](../../README.md) | Connection lifecycle, protocol operations and media settings. It remains independent of UI frameworks. |
| `Gurux.Net.Blazor` | Settings controls and editing transactions for this framework and media type. |
| Host application | Owns the media, opens/closes connections, sends/receives data, persists accepted settings and supplies platform permissions. |
| [Gurux.Net.Example](../../Gurux.Net.Example/Gurux.Net.Example.csproj) | A complete example application with connection/data controls and activity views. Its application screens are separate from this reusable settings adapter. |

This package is for configuration forms. Adding it does not supply a complete communication application, automatically connect a device or persist settings.

## Editing and ownership

Keep the supplied media closed while editing. The editor stages changes in a private, unopened draft; Save validates before applying settings to the caller-owned media. Cancel discards pending edits. Open media and conflicting external changes are rejected.

The host retains ownership of the media and disposes it when appropriate. Dispose host-owned embedded views or drafts when their containing form ends. A successful Save updates the media configuration; the application decides whether to persist it and when to open a connection.

Render MediaSettingsEditor for a standalone editing session. For a containing settings dialog, create and dispose a MediaSettingsDraft and render MediaSettingsForm with Embedded=true when that mode is available in the adapter.

Blazor Server edits media on the server: device discovery and transport access refer to that server, not the browser's computer. Browser WebAssembly cannot use these adapters to gain arbitrary serial-port or TCP-socket access. The host must select an interactive render mode and serve the required static web assets.

## Integration details

Optional .NET 10 Razor settings components for Net.
References the Gurux.UI.Components NuGet package, version 4.0.2611.1201.
MAUI keeps its native UI; Linux keeps Avalonia. Neither depends on this library.

## Host setup

Register appearance/input services in the Blazor application's Program.cs:

```csharp
builder.Services.AddGuruxMediaComponents(); // using Gurux.Net.Blazor;
```

Add the namespace to `_Imports.razor`:

```razor
@using Gurux.Net.Blazor
```

Load the package's local Bootstrap and adaptive layout CSS in the host HTML/head:

```html
<link data-gx-bootstrap rel="stylesheet" href="_content/Gurux.UI.Components/Appearance/themes/default.min.css" />
<link rel="stylesheet" href="_content/Gurux.UI.Components/Appearance/layout.css" />
```

The host must serve Razor static web assets (standard Blazor templates do).
No CDN or additional JavaScript script tag is needed: GXThemeProvider imports its
appearance module from the package. Keep the standard Blazor host script.

```razor
@if (editing)
{
    <MediaSettingsEditor Media="media" Completed="SettingsClosed" />
}
@code {
    private readonly Gurux.Net.GXNet media = new(Gurux.Net.Enums.NetworkType.Tcp, "localhost", 4059);
    private bool editing = true;
    private void SettingsClosed(bool saved) { editing = false; }
}
```

The parent owns and disposes `media`. The editor owns a private unopened draft.
Save validates and commits; Cancel, cancellation and component disposal discard
changes. Saving detects concurrent settings changes and rejects open media.
Remove/recreate the editor to start another editing session. Pass an optional
CancellationToken to close an editor when its owning operation ends.

MediaAppearance uses GXThemeProvider and GXAdaptiveLayout Auto: Windows Fluent,
Android Material, iOS Cupertino, other platforms default. Detection follows the
client WebView/browser, not the server OS. Color mode follows the client setting.

For custom hosts, MediaSettingsDraft and MediaSettingsForm are available separately;
the host must dispose its draft and may wrap the form with MediaAppearance.

## Hosting and transport limits

Use an interactive Blazor render mode. Blazor Server runs media operations on the
server; its local address choices belong to that server, not the browser device. It cannot use serial ports attached to the browser device. Browser
WebAssembly cannot open System.IO.Ports or arbitrary TCP sockets. This library
provides settings UI and does not add browser transport adapters. Native MAUI
applications continue using Gurux.Net.Maui without BlazorWebView.

## Packaging

Build this optional project independently. The network package references only network, common and Gurux.UI.Components. Terminal and SMS reuse the Serial Blazor port controls. No aggregate media UI package is required.

## Headless verification

From the GXMedias workspace, run `dotnet run --project Gurux.Net/UI/Gurux.Net.Blazor/Tests/Tests.csproj -p:GeneratePackageOnBuild=false -p:BuildInParallel=false`. The combined harness references all four libraries; production network dependencies remain isolated. These tests do not open physical transports.

Settings editors honor `GXNet.ConfigurableSettings` using `AvailableMediaSettings` flags.
For example, `(int)(AvailableMediaSettings.Protocol | AvailableMediaSettings.Host | AvailableMediaSettings.Port)`
shows only client connection fields. The default flags are `Server | Protocol | Port | Host | UseIPv6`; set `AvailableMediaSettings.All` explicitly to expose all fields.
The Server switch is first when permitted. Host remains editable in both modes. In server mode, Listen address offers all network interfaces (empty address), loopback, local IP addresses with interface names, and manual entry. Choices follow IPv4/IPv6 selection, preserving IPv6 scope identifiers. Client and server addresses are staged separately while the form is open. Interface discovery uses the machine running the editor; restricted platforms retain the all-interfaces, loopback and manual choices. Maximum clients is enabled only in server mode.
Removing the `Server` flag hides both the switch and all server-specific fields.
Maximum clients also requires `MaxClientCount`. Hidden settings retain their existing values when saved.
## Embedded controls

Create a host-owned `MediaSettingsDraft` from a closed native media and render
`<MediaSettingsForm Draft="draft" Embedded="true" />` inside the containing dialog.
Embedded mode hides the form's Save and Cancel buttons and ignores local submits.
The outer Save calls `draft.Commit()` before persisting the media settings; the
outer Cancel disposes the draft without committing. Dispose both the draft and
the host-owned media when the containing session ends. Keep separate drafts to
retain uncommitted inputs when switching media types. Repeated successful commits
are supported for persistence retries while external settings changes are rejected.
The default `Embedded="false"` retains the standalone Save/Cancel behavior.
