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

using Gurux.Net.Example.Properties;
using LayoutResources = Gurux.Medias.Maui.Properties.ExampleSettingsResources;
using Gurux.Net;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Graphics;

using System.Text.Json.Nodes;

using StatusResources = Gurux.Medias.Maui.Properties.ExampleSettingsResources;

namespace Gurux.Net.Example.Maui;

/// <summary>Native compact network form; wraps settings rows on narrow screens.</summary>
internal sealed class NetworkExamplePanel : ContentView, IAsyncDisposable
{
    /// <summary>Displays connection state and selected endpoints in the fixed bottom status bar.</summary>
    private readonly Label _mediaStatus = new() { AutomationId = "MediaStatus" };

    /// <summary>Persists this application's media and outgoing data preferences.</summary>
    private readonly ExampleSettingsStore _preferences = new();
    /// <summary>Completes when startup preferences have been applied on the UI thread.</summary>
    private Task _preferencesLoading = Task.CompletedTask;
    /// <summary>Tracks the latest queued write so shutdown waits for the final disk update.</summary>
    private Task _preferencesSaving = Task.CompletedTask;
    /// <summary>Prevents persistence until startup restoration has completed.</summary>
    private bool _preferencesReady;
    /// <summary>Tracks the most recently requested snapshot and avoids saving unchanged controls.</summary>
    private string? _preferencesSnapshot;

    /// <summary>Owns the network connection used by this component.</summary>
    private readonly ExampleSession _session;
    /// <summary>Coordinates sends, echo responses and periodic testing.</summary>
    private readonly NetworkExampleController _controller;
    /// <summary>Edits the outgoing payload.</summary>
    private readonly Entry _send = new() { AutomationId = "SendText", Placeholder = ExampleResources.SendPlaceholder, Text = "4D" };
    /// <summary>Stores the latest received payload.</summary>
    private readonly Editor _received = new() { AutomationId = "ReceivedText", IsReadOnly = true, HeightRequest = 60 };
    /// <summary>Stores the bounded network error history.</summary>
    private readonly Editor _errors = new() { AutomationId = "Errors", IsReadOnly = true, HeightRequest = 160 };
    /// <summary>Tracks currently connected network clients.</summary>
    private readonly CollectionView _clients = new()
    {
        AutomationId = "Clients", HeightRequest = 160, SelectionMode = SelectionMode.Single,
        ItemTemplate = new DataTemplate(() =>
        {
            var label = new Label { Padding = new Thickness(4) };
            label.SetBinding(Microsoft.Maui.Controls.Label.TextProperty, ".");
            return label;
        })
    };
    /// <summary>Edits the remote host before connecting.</summary>
    private readonly Entry _host = new() { AutomationId = "Receiver", WidthRequest = 160 };
    /// <summary>Edits the reply timeout in milliseconds.</summary>
    private readonly Entry _timeout = new() { AutomationId = "ReplyTimeout", Text = "5000", WidthRequest = 76, Keyboard = Keyboard.Numeric };
    /// <summary>Edits the end-of-packet marker.</summary>
    private readonly Entry _eop = new() { AutomationId = "ReplyEop", WidthRequest = 76 };
    /// <summary>Edits the minimum reply byte count.</summary>
    private readonly Entry _count = new() { AutomationId = "ReplyCount", Text = "0", WidthRequest = 65, Keyboard = Keyboard.Numeric };
    /// <summary>Edits the tester delay in milliseconds.</summary>
    private readonly Entry _interval = new() { AutomationId = "TestInterval", Text = "1000", WidthRequest = 90, Keyboard = Keyboard.Numeric };
    /// <summary>Displays the transmitted byte count.</summary>
    private readonly Entry _sent = new() { AutomationId = "BytesSent", IsReadOnly = true, Text = "0", WidthRequest = 90 };
    /// <summary>Displays the received byte count.</summary>
    private readonly Entry _receivedCount = new() { AutomationId = "BytesReceived", IsReadOnly = true, Text = "0", WidthRequest = 90 };
    /// <summary>Selects hexadecimal payload representation.</summary>
    private readonly Switch _hex = new() { AutomationId = "HexMode", IsToggled = true };
    /// <summary>Selects buffered reply reception for each send.</summary>
    private readonly Switch _sync = new() { AutomationId = "ReceiveReply" };
    /// <summary>Enables echoing received network packets.</summary>
    private readonly Switch _echo = new() { AutomationId = "Echo" };
    /// <summary>Displays connection state.</summary>
    private readonly Label _state = new() { AutomationId = "ConnectionState" };
    /// <summary>Opens the configured connection.</summary>
    private readonly Button _open;
    /// <summary>Cancels pending operations and closes the transport.</summary>
    private readonly Button _close;
    /// <summary>Opens network settings.</summary>
    private readonly Button _settings;
    /// <summary>Sends the current payload.</summary>
    private readonly Button _transmit;
    /// <summary>Starts or stops periodic sending.</summary>
    private readonly Button _tester;
    /// <summary>Displays the bounded session activity without changing transport ownership.</summary>
    private readonly Editor _activity = new() { AutomationId = "ActivityLog", IsReadOnly = true, HeightRequest = 240 };
    /// <summary>Contains recent network errors.</summary>
    private readonly View _errorGroup;

    /// <summary>Stores the latest connection phase independently of periodic UI refreshes.</summary>
    private int _mediaPhase;
    /// <summary>Prevents callbacks and queued refreshes from touching a disposed view.</summary>
    private int _shuttingDown;
    /// <summary>Marshals transport state notifications onto the native UI thread.</summary>
    private readonly Microsoft.Maui.Dispatching.IDispatcher _uiDispatcher;
    /// <summary>Shares the asynchronous shutdown operation across disposal requests.</summary>
    private Task? _disposal;

    /// <summary>Retains payload controls across navigation.</summary>
    private readonly VerticalStackLayout _dataView = new() { AutomationId = "DataView", Spacing = 12 };
    /// <summary>Contains reply settings and testing controls.</summary>
    private readonly VerticalStackLayout _communicationView = new() { AutomationId = "CommunicationView", Spacing = 12, IsVisible = false };
    /// <summary>Contains persistent read-only activity history.</summary>
    private readonly VerticalStackLayout _activityView = new() { AutomationId = "ActivityView", Spacing = 12, IsVisible = false };
    /// <summary>Tracks native menu commands that mirror lifecycle availability.</summary>
    private readonly List<Command> _nativeCommands = new();
    /// <summary>Prevents navigation while a settings editor owns the view.</summary>
    private bool _editing;
    /// <summary>Creates Serial-style connection, payload and received cards while retaining network tools in separate sections.</summary>
    /// <param name="properties">Opens the network settings editor.</param>
    /// <param name="session">The network session owned by the calling view.</param>
    internal NetworkExamplePanel(ExampleSession session, Func<Task> properties)
    {
        this._session = session;
        _controller = new(session);
        _host.Text = ((GXNet)session.Media).HostName;
        var body = ExampleLayout.Body();
        body.Children.Add(ExampleLayout.Header(_state));
        ExampleLayout.Activity(_activity);
        ExampleLayout.Activity(_received);
        if (OperatingSystem.IsWindows()) { _send.FontFamily = "Consolas"; }
        ExampleLayout.Activity(_errors);
        _errors.HeightRequest = 120;
        _open = MakeButton("Open", ExampleResources.Open, async () =>
        {
            var net = (GXNet)session.Media;
            if (!net.Server && !string.IsNullOrWhiteSpace(_host.Text)) { net.HostName = _host.Text; }
            await session.OpenAsync();
        });
        _close = MakeButton("Close", ExampleResources.Close, async () => { var stopping = _controller.StopTesterAsync(); await session.CloseAsync(); await stopping; });
        _settings = MakeButton("Settings", ExampleResources.Properties, async () => { _editing = true; try { await properties(); _host.Text = ((GXNet)session.Media).HostName; } finally { _editing = false; } });
        _transmit = MakeButton("Send", ExampleResources.Send, () => _controller.SendAsync(Options()));
        _echo.Toggled += (_, args) => _controller.Echo = args.Value;
        _dataView.Children.Add(ExampleLayout.Group(LayoutResources.ConnectionGroup, ExampleLayout.Row(_open, _close)));
        var options = ExampleLayout.Row(ExampleLayout.Toggle(_hex, ExampleResources.Hex), ExampleLayout.Toggle(_echo, ExampleResources.Echo));
        var sendActions = OperatingSystem.IsWindows() ? options : ExampleLayout.Row();
        sendActions.Children.Add(_transmit);
        sendActions.Children.Add(MakeButton("Clear", ExampleResources.Clear, () => { _controller.ClearReceived(); return Task.CompletedTask; }));
        var data = ExampleLayout.Stack(ExampleLayout.Field(LayoutResources.PayloadLabel, _send), options);
        if (!OperatingSystem.IsWindows()) { data.Children.Add(sendActions); }
        _dataView.Children.Add(ExampleLayout.Group(LayoutResources.DataGroup, data));
        _dataView.Children.Add(ExampleLayout.Group(ExampleResources.ReceivedDataHeading, _received));
        var replies = ExampleLayout.Row(ExampleLayout.Field(LayoutResources.CountLabel, _count, 164),
            ExampleLayout.Field(LayoutResources.TerminatorLabel, _eop, 164), ExampleLayout.Field(LayoutResources.TimeoutLabel, _timeout, 164));
        _communicationView.Children.Add(ExampleLayout.Group(LayoutResources.ReplyGroup,
            ExampleLayout.Stack(ExampleLayout.Toggle(_sync, ExampleResources.Sync), replies)));
        _tester = MakeButton("Tester", ExampleResources.GoStop, async () =>
        {
            if (_controller.IsTesting) { await _controller.StopTesterAsync(); }
            else { _controller.StartTester(Options(), int.Parse(_interval.Text ?? "1000")); }
        });
        _communicationView.Children.Add(ExampleLayout.Pair(
            ExampleLayout.Group(ExampleResources.Statistics, ExampleLayout.Row(ExampleLayout.Field(ExampleResources.Sent, _sent, 164), ExampleLayout.Field(ExampleResources.ReceivedLabel, _receivedCount, 164))),
            ExampleLayout.Group(ExampleResources.Tester, ExampleLayout.Row(ExampleLayout.Field(ExampleResources.Interval, _interval, 164), _tester))));
        _communicationView.Children.Add(ExampleLayout.Group(LayoutResources.ConnectionGroup, ExampleLayout.Field(LayoutResources.HostLabel, _host, 220)));
        _communicationView.Children.Add(ExampleLayout.Group(ExampleResources.Clients, _clients));
        _errorGroup = ExampleLayout.Group(ExampleResources.Errors, _errors);
        _activityView.Children.Add(ExampleLayout.Row(MakeButton("ClearActivity", ExampleResources.Clear, () => { _controller.ClearActivity(); return Task.CompletedTask; })));
        _activityView.Children.Add(_errorGroup);
        _activityView.Children.Add(ExampleLayout.Group(LayoutResources.ActivityGroup, _activity));
        body.Children.Add(_dataView); body.Children.Add(_communicationView); body.Children.Add(_activityView);
        Content = ExampleLayout.WithStatus(body, _mediaStatus);
        _uiDispatcher = Dispatcher;
        _mediaPhase = (int)(session.Media.IsOpen ? Gurux.Common.MediaState.Open : Gurux.Common.MediaState.Closed);
        session.Media.OnMediaStateChange += MediaStateChanged;
        Refresh();
        _preferencesLoading = RestorePreferencesAsync();

    }
    /// <summary>Gets whether the current lifecycle state permits editing media settings.</summary>
    internal bool CanEditSettings => CanNavigate && _settings.IsEnabled;
    /// <summary>Reports native navigation failures through the existing bounded error history.</summary>
    /// <param name="error">The cancellation or failure reported by native navigation.</param>
    internal void ReportNavigationError(Exception error)
    {
        if (System.Threading.Volatile.Read(ref _shuttingDown) == 0)
        {
            _controller.Report(error.Message);
            Refresh();
        }
    }
    /// <summary>Creates a native lifecycle command with the existing button availability.</summary>
    /// <param name="name">The lifecycle command identifier.</param>
    /// <returns>A command whose availability follows the existing controls.</returns>
    internal Command CreateCommand(string name)
    {
        Button button = name == "Open" ? _open : name == "Close" ? _close : _settings;
        var command = new Command(() => InvokeCommand(name), () => CanNavigate && button.IsEnabled);
        _nativeCommands.Add(command);
        return command;
    }
    /// <summary>Shows the existing payload controls.</summary>
    internal void ShowData() => ShowSection(_dataView);
    /// <summary>Shows reply and tester settings.</summary>
    internal void ShowCommunication() => ShowSection(_communicationView);
    /// <summary>Shows the bounded activity and error history.</summary>
    internal void ShowActivity() => ShowSection(_activityView);
    /// <summary>Runs a lifecycle command only when its existing button permits it.</summary>
    /// <param name="name">The existing command identifier.</param>
    internal void InvokeCommand(string name)
    {
        Button button = name == "Open" ? _open : name == "Close" ? _close : _settings;
        if (CanNavigate && button.IsEnabled) { button.SendClicked(); }
    }
    /// <summary>Gets whether navigation can safely change the current view.</summary>
    internal bool CanNavigate => _preferencesReady && !_editing && System.Threading.Volatile.Read(ref _shuttingDown) == 0;
    /// <summary>Changes the visible section while retaining all controls.</summary>
    /// <param name="section">The section to show.</param>
    private void ShowSection(VerticalStackLayout section)
    {
        if (CanNavigate)
        {
            _dataView.IsVisible = ReferenceEquals(section, _dataView);
            _communicationView.IsVisible = ReferenceEquals(section, _communicationView);
            _activityView.IsVisible = ReferenceEquals(section, _activityView);
        }
    }
    /// <summary>Collects current payload and receive settings from the controls.</summary>
    /// <returns>The selected network send and receive settings.</returns>
    private NetworkSendOptions Options()
    {
        var net = (GXNet)_session.Media;
        return new(_send.Text ?? "", _hex.IsToggled, _sync.IsToggled, _eop.Text, int.Parse(_count.Text ?? "0"),
            int.Parse(_timeout.Text ?? "5000"), net.Server ? _clients.SelectedItem as string : null);
    }
    /// <summary>Creates an asynchronous action button that reports failures in the error view.</summary>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <param name="name">The invariant automation identifier.</param>
    /// <param name="caption">The localized caption.</param>
    /// <returns>The configured action button.</returns>
    private Button MakeButton(string name, string caption, Func<Task> action)
    {
        var button = new Button { AutomationId = name, Text = caption };
        ExampleLayout.StyleButton(button, name is "Open" or "Send");
        button.Margin = new Thickness(0, 0, 8, 8);
        button.Clicked += async (_, _) =>
        {
            try { Task task = action(); Refresh(); await task; }
            catch (OperationCanceledException) { }
            catch (Exception ex) { _controller.Report(ex.Message); }
            finally { Refresh(); }
        };
        return button;
    }
    /// <summary>Caches connection phases and schedules immediate native control updates.</summary>
    /// <param name="sender">The media reporting its lifecycle state.</param>
    /// <param name="args">The reported lifecycle phase or settings-change notification.</param>
    private void MediaStateChanged(object? sender, Gurux.Common.MediaStateEventArgs args)
    {
        if (System.Threading.Volatile.Read(ref _shuttingDown) != 0) { return; }
        if (args.State != Gurux.Common.MediaState.Changed)
        {
            System.Threading.Volatile.Write(ref _mediaPhase, (int)args.State);
        }
        if (_uiDispatcher.IsDispatchRequired) { _uiDispatcher.Dispatch(Refresh); }
        else { Refresh(); }
    }
    /// <summary>Updates availability, counters and clients from connection state.</summary>
    internal void Refresh()
    {
        if (System.Threading.Volatile.Read(ref _shuttingDown) != 0)
        {
            _open.IsEnabled = false; _close.IsEnabled = false; _settings.IsEnabled = false;
            _transmit.IsEnabled = false; _tester.IsEnabled = false; _host.IsEnabled = false;
            foreach (Command command in _nativeCommands) { command.ChangeCanExecute(); }
            return;
        }
        var phase = (Gurux.Common.MediaState)System.Threading.Volatile.Read(ref _mediaPhase);
        bool closed = phase == Gurux.Common.MediaState.Closed;
        bool running = _controller.IsTesting;
        bool busy = _session.IsBusy || running;
        bool connected = phase == Gurux.Common.MediaState.Open;
        _open.IsEnabled = !busy && closed; _settings.IsEnabled = !busy && closed;
        _close.IsEnabled = connected; _transmit.IsEnabled = connected && !busy;
        _tester.IsEnabled = connected && (!busy || running); _tester.Text = running ? ExampleResources.Stop : ExampleResources.GoStop;
        _host.IsEnabled = closed && !busy && !((GXNet)_session.Media).Server;
        foreach (Command command in _nativeCommands) { command.ChangeCanExecute(); }
        ExampleLayout.Status(_state, busy, connected);
        string latest = _controller.Received(_hex.IsToggled);
        if (_received.Text != latest) { _received.Text = latest; }
        _sent.Text = _session.Media.BytesSent.ToString(); _receivedCount.Text = _session.Media.BytesReceived.ToString();
        string messages = string.Join(Environment.NewLine, _controller.Errors);
        if (_errors.Text != messages) { _errors.Text = messages; }
        _errorGroup.IsVisible = !string.IsNullOrWhiteSpace(messages);
        string snapshot = string.Join(Environment.NewLine, _session.Log);
        if (_activity.Text != snapshot) { _activity.Text = snapshot; }
        var current = _session.Clients.ToArray();
        if (!(_clients.ItemsSource as string[] ?? []).SequenceEqual(current))
        {
            var selected = _clients.SelectedItem;
            _clients.ItemsSource = current;
            _clients.SelectedItem = current.Contains(selected as string) ? selected : null;
        }
        _ = SavePreferencesAsync();
        var statusNetwork = (GXNet)_session.Media;
        string statusAddress = string.IsNullOrWhiteSpace(statusNetwork.HostName)
            ? (statusNetwork.Server ? (statusNetwork.UseIPv6 ? "::" : "0.0.0.0") : StatusResources.StatusNotConfigured)
            : statusNetwork.HostName;
        if (statusAddress.Contains(':') && !statusAddress.StartsWith("[", StringComparison.Ordinal))
        {
            statusAddress = string.Concat("[", statusAddress, "]");
        }
        _mediaStatus.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, StatusResources.StatusNetwork,
            _session.Media.IsOpen ? StatusResources.StatusOpen : StatusResources.StatusClosed,
            statusNetwork.Protocol.ToString().ToUpperInvariant(), statusAddress, statusNetwork.Port);

    }
    /// <inheritdoc/>
    public ValueTask DisposeAsync() => new(_disposal ??= DisposeCoreAsync());
    /// <summary>Detaches transport callbacks before releasing the controller.</summary>
    /// <returns>A task completing after controller shutdown.</returns>
    private async Task DisposeCoreAsync()
    {
        System.Threading.Interlocked.Exchange(ref _shuttingDown, 1);
        if (_uiDispatcher.IsDispatchRequired) { _uiDispatcher.Dispatch(Refresh); }
        else { Refresh(); }
        _session.Media.OnMediaStateChange -= MediaStateChanged;
        await _preferencesLoading;
        await SavePreferencesAsync();
        await _controller.DisposeAsync();
    }
    /// <summary>Captures media configuration and outgoing controls without opening a connection.</summary>
    /// <returns>A detached JSON object containing this view's persistent preferences.</returns>
    private JsonObject CapturePreferences()
    {
        var values = new JsonObject
        {
            ["Media"] = JsonNode.Parse(_session.Media.JsonSettings ?? "{}"),
            ["SendText"] = _send.Text ?? "",
            ["Hex"] = _hex.IsToggled,
            ["Echo"] = _echo.IsToggled,
            ["ReceiveReply"] = _sync.IsToggled,
            ["ReplyEop"] = _eop.Text ?? "",
            ["ReplyCount"] = _count.Text ?? "",
            ["ReplyTimeout"] = _timeout.Text ?? "",
            ["Host"] = _host.Text ?? ""
        };
        return values;
    }

    /// <summary>Applies stored media configuration and interface values while the transport remains closed.</summary>
    /// <param name="values">The saved preferences; missing entries retain their current defaults.</param>
    private void ApplyPreferences(JsonObject values)
    {
        if (values["Media"] is JsonObject media) { _session.Media.JsonSettings = media.ToJsonString(); }
        _send.Text = values["SendText"]?.GetValue<string>() ?? _send.Text;
        _hex.IsToggled = values["Hex"]?.GetValue<bool>() ?? (_hex.IsToggled);
        _echo.IsToggled = values["Echo"]?.GetValue<bool>() ?? false;
        _sync.IsToggled = values["ReceiveReply"]?.GetValue<bool>() ?? (_sync.IsToggled);
        _eop.Text = values["ReplyEop"]?.GetValue<string>() ?? _eop.Text;
        _count.Text = values["ReplyCount"]?.GetValue<string>() ?? _count.Text;
        _timeout.Text = values["ReplyTimeout"]?.GetValue<string>() ?? _timeout.Text;
        _host.Text = values["Host"]?.GetValue<string>() ?? _host.Text;
    }

    /// <summary>Restores preferences asynchronously before enabling interaction and keeps defaults if loading fails.</summary>
    /// <returns>A task completing after the saved values or original defaults have been applied.</returns>
    private async Task RestorePreferencesAsync()
    {
        IsEnabled = false;
        JsonObject defaults = CapturePreferences();
        try
        {
            JsonObject? values = await _preferences.LoadAsync();
            if (System.Threading.Volatile.Read(ref _shuttingDown) != 0) { return; }
            if (values is not null) { ApplyPreferences(values); }
        }
        catch (Exception ex)
        {
            if (System.Threading.Volatile.Read(ref _shuttingDown) != 0) { return; }
            ApplyPreferences(defaults);
            _controller.Report(ex.Message);
        }
        _preferencesSnapshot = CapturePreferences().ToJsonString();
        _preferencesReady = true;
        IsEnabled = true;
        Refresh();
    }

    /// <summary>Persists changed controls and reports disk failures without interrupting transport cleanup.</summary>
    /// <returns>A task completing after the current snapshot has been saved or skipped.</returns>
    private async Task SavePreferencesAsync()
    {
        if (_uiDispatcher.IsDispatchRequired)
        {
            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(SavePreferencesAsync);
            return;
        }
        if (!_preferencesReady) { return; }
        string? snapshot = null;
        try
        {
            JsonObject values = CapturePreferences();
            snapshot = values.ToJsonString();
            if (snapshot == _preferencesSnapshot) { await _preferencesSaving; return; }
            _preferencesSnapshot = snapshot;
            _preferencesSaving = _preferences.SaveAsync(values);
            await _preferencesSaving;
        }
        catch (Exception ex)
        {
            if (_preferencesSnapshot == snapshot) { _preferencesSnapshot = null; }
            _controller.Report(ex.Message);
        }
    }

}
