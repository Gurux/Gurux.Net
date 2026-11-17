# Gurux.Net.Avalonia

## Why use this component?

Use this optional package to add a ready-made settings editor for `GXNet` to an existing Avalonia desktop application. It supplies localized fields, input validation and a staged Save/Cancel workflow so the host does not have to implement the media settings form itself.

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
| `Gurux.Net.Avalonia` | Settings controls and editing transactions for this framework and media type. |
| Host application | Owns the media, opens/closes connections, sends/receives data, persists accepted settings and supplies platform permissions. |
| [Gurux.Net.Example](../../Gurux.Net.Example/Gurux.Net.Example.csproj) | A complete example application with connection/data controls and activity views. Its application screens are separate from this reusable settings adapter. |

This package is for configuration forms. Adding it does not supply a complete communication application, automatically connect a device or persist settings.

## Editing and ownership

Keep the supplied media closed while editing. The editor stages changes in a private, unopened draft; Save validates before applying settings to the caller-owned media. Cancel discards pending edits. Open media and conflicting external changes are rejected.

The host retains ownership of the media and disposes it when appropriate. Dispose host-owned embedded views or drafts when their containing form ends. A successful Save updates the media configuration; the application decides whether to persist it and when to open a connection.

Use AvaloniaMediaSettingsEditor for a modal editor, or MediaSettingsView for controls embedded in a host-owned form.

## Integration details

Optional Avalonia 12 settings editor for `GXNet` on .NET 10. Register `new Gurux.Net.Avalonia.AvaloniaMediaSettingsEditor(owner)` as the media settings editor and show the owner before calling `ShowAsync`. An optional parent uses `Gurux.Net.Avalonia.AvaloniaMediaWindow`.

Edits are staged on an unopened private media instance. Save validates all fields before applying settings, rejects an open transport and detects settings changed externally. Cancel, owner close and cancellation discard the draft. Cancellation completes with `OperationCanceledException`. Calls are dispatched to the UI thread and overlapping dialogs on an editor are rejected.

All labels and validation messages are in `Properties/Resources.resx`. No transport connection is opened by this library. Terminal and SMS reuse the Serial Avalonia port controls.

Settings editors honor `GXNet.ConfigurableSettings` using `AvailableMediaSettings` flags.
For example, `(int)(AvailableMediaSettings.Protocol | AvailableMediaSettings.Host | AvailableMediaSettings.Port)`
shows only client connection fields. The default flags are `Server | Protocol | Port | Host | UseIPv6`; set `AvailableMediaSettings.All` explicitly to expose all fields.
The Server switch is first when permitted. Host remains editable in both modes. In server mode, Listen address offers all network interfaces (empty address), loopback, local IP addresses with interface names, and manual entry. Choices follow IPv4/IPv6 selection, preserving IPv6 scope identifiers. Client and server addresses are staged separately while the form is open. Interface discovery uses the machine running the editor; restricted platforms retain the all-interfaces, loopback and manual choices. Maximum clients is enabled only in server mode.
Removing the `Server` flag hides both the switch and all server-specific fields.
Maximum clients also requires `MaxClientCount`. Hidden settings retain their existing values when saved.
## Embedded settings

Use `new MediaSettingsView(media)` to place the native settings controls directly
inside a host form. The caller owns the closed media and must keep it closed.
Call `view.Commit()` only from the host's Save action, then capture or persist the
accepted media settings. A successful commit can be retried after a persistence
failure; external media changes still cause a conflict. Dispose the view when the
host closes, including Cancel, to discard uncommitted controls. Dispose the
caller-owned media separately. Creating or committing the view never opens a
transport. `AvaloniaMediaSettingsEditor` remains available for modal settings.
