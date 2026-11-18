# Gurux.Net.Example

The example owns its Core, UI/Desktop (Avalonia) and UI/Maui source beside the
network library's Development directory. It has no imports from another example or the
former GXMedias/Examples source directory.

Windows uses native MAUI, Linux Avalonia; Android/iOS hosts are included.
Media libraries and settings adapters
are referenced through published NuGet packages. Only this example's Core controller uses a local project reference.

From this folder:

```powershell
dotnet run --project Gurux.Net.Example.csproj -f net11.0-windows10.0.19041.0 -p:GuruxExamplesBuild=true
dotnet run --project Gurux.Net.Example.csproj -f net11.0 -p:BuildDesktop=true -p:GuruxExamplesBuild=true
dotnet build Gurux.Net.Example.csproj -f net11.0-android -p:BuildMobile=true -p:GuruxExamplesBuild=true
```

The Core project contains only this application's connection logic. The application project compiles the Desktop or Maui UI directly and references that local Core; no other example is referenced or selected at runtime.

## Linux and iOS icons

The desktop window and iOS app use the shared `Resources/AppIcon/appicon.png` artwork. Windows executable metadata retains `Net.ico`.

For a Linux menu and taskbar entry, publish for the target architecture, keep the output in its final location, and register it for the current user:

```sh
dotnet publish Gurux.Net.Example.csproj -c Release -f net10.0 -r linux-arm64 --self-contained false -p:GuruxExamplesBuild=true -p:GeneratePackageOnBuild=false -o publish/linux
python3 publish/linux/Linux/install-desktop.py
```

The installer writes the launcher and matching hicolor icon under `XDG_DATA_HOME` (default `~/.local/share`), using the application's window class to associate them. It requires no administrator privileges. Run it again if the application moves. Direct launches also receive the PNG window icon; desktop registration supplies the menu and taskbar identity.

iOS builds explicitly generate an Apple app icon catalog with opaque white backgrounds. Build the native application on a Mac with the required MAUI/Xcode tooling, using `BuildApple=true` and `net11.0-ios`; see `docs/media-examples.md` in GXMedias. Icon generation can be checked separately from native Apple compilation.

## Build requirements

Use .NET SDK 11.0.100 or a newer stable .NET 11 feature band selected by the repository's global.json. The example application and its Core project target both .NET 10 and .NET 11. Select the desired target with `-f`; the repository still selects the .NET 11 SDK for building, including .NET 10 outputs.

For Windows MAUI, install the matching .NET 10 or .NET 11 `maui-windows` workload. For Android, install `maui-android`; for iOS, install `maui-ios` on a Mac with matching Xcode and signing tools. Use `dotnet workload list` to check the selected SDK's workloads. Linux uses Avalonia and does not require MAUI.

Microsoft.Maui.Controls is bounded to 10.x for .NET 10 targets and 11.x for .NET 11 targets. A MAUI major-version upgrade requires matching target frameworks and workloads; updating NuGet alone is insufficient. Building all library compatibility targets also requires the .NET 10 toolchain and matching workloads.

## Linux publication on .NET 10

Publish the application `.csproj` from this directory with the repository-selected SDK. Do not publish the `.sln`: it also contains platform-specific MAUI projects, and a shared solution output directory can mix unrelated project outputs. A Linux runtime identifier selects the Avalonia desktop target automatically, including when publishing from Windows:

```sh
dotnet publish Gurux.Net.Example.csproj -c Release -f net10.0 -r linux-arm64 --self-contained false -p:GuruxExamplesBuild=true -p:GeneratePackageOnBuild=false -o publish/linux-net10
```

The target Linux machine needs the .NET 10 runtime for this framework-dependent build. Use `--self-contained true` to include the runtime, or replace `linux-arm64` with `linux-x64` for an x64 destination. .NET 11 remains available with `-f net11.0`. Publishing Linux desktop output does not require MAUI.

## Project layout

The application project stays in the example root and compiles its UI directly, selecting Avalonia sources and resources from `UI/Desktop/` for desktop targets or native MAUI sources and resources from `UI/Maui/` for Windows, Android and iOS. These UI directories have no separate library projects. Shared application logic remains in the separate `Core/` project. Avalonia entry points are in `UI/Desktop/Startup/`, with Linux desktop registration files in `UI/Desktop/Linux/`. MAUI startup is in `UI/Maui/Startup/`, with platform entry points and manifests in `UI/Maui/Platforms/`. Only the selected platform's entry points are compiled; test and generated files are excluded. Reusable media settings adapter libraries remain in the media repository's separate `UI/Gurux.*` projects.

## Platform navigation

Windows MAUI and Avalonia desktop hosts expose a top menu bar with Connection, View and Settings groups. View contains independent Data and Activity Log choices; Settings contains the media editor and Communication settings. Activity history is kept in its own view instead of Communication settings. Existing connection and data actions remain available in the Data view.

Android and iOS use a native navigation-bar Menu action and platform action sheet. Opening an editor or navigation sheet temporarily reserves input, and media settings respect connection state and active operations. Switching views preserves the media session, staged data, counters and activity history. Closing the application cancels pending operations and ignores late menu results.

## Application icons

The iOS app explicitly selects the generated `appicon` asset catalog set.
Rebuild and deploy the updated app to refresh its Home screen icon.

Linux uses the example's own icon and a matching X11 window class and desktop
launcher. After publishing, register the launcher and icon for the current user
from the published application directory:

```sh
python3 Linux/install-desktop.py
```

The installer uses `XDG_DATA_HOME`, or `~/.local/share` when it is unset, without
root access. Keep the published application in that location, or run the installer
again after moving it, because the launcher uses its absolute executable path.
## Serial-style layout

Data uses separate Connection, Data and Received data cards. The connection card
contains Open and Close / cancel; the payload row contains hexadecimal and echo
switches followed by Send and Clear. Media settings are available through Settings.
Communication settings retain buffered-reply options, the editable host, connected
clients, traffic counters and the periodic tester. Activity Log shows session
activity and diagnostic errors with its own Clear action. Native Windows menus
are hosted in a NavigationPage, and Android uses a hamburger navigation icon.
