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
using UiResources = Gurux.Net.Example.Desktop.Properties.ExampleSettingsResources;
using Gurux.Common;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Gurux.Net;

using System.Text.Json.Nodes;

using StatusResources = Gurux.Net.Example.Desktop.Properties.ExampleSettingsResources;

namespace Gurux.Net.Example.Desktop;

/// <summary>Compact grouped network controls shared by desktop operating systems.</summary>
internal sealed class NetworkExamplePanel : UserControl, IAsyncDisposable
{
    /// <summary>Displays connection state and selected endpoints in the fixed bottom status bar.</summary>
    private readonly TextBlock _mediaStatus = new() { Name = "MediaStatus" };

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

    /// <summary>Stores the latest transport connection phase independently of command activity.</summary>
    private int _connectionPhase;
    /// <summary>Prevents callbacks from refreshing disposed resources.</summary>
    private int _shuttingDown;
    /// <summary>Shares controller cleanup between repeated disposal requests.</summary>
    private Task? _disposal;
    /// <summary>Owns the network connection used by this component.</summary>
    private readonly ExampleSession _session;
    /// <summary>Coordinates sends, echo responses and periodic testing.</summary>
    private readonly NetworkExampleController _controller;
    /// <summary>Opens the network settings editor for this view.</summary>
    private readonly Func<Task> _properties;
    /// <summary>Edits the outgoing payload.</summary>
    private readonly TextBox _send = new() { Name = "SendText", PlaceholderText = ExampleResources.SendPlaceholder, Text = "4D", AcceptsReturn = true, MinHeight = 64 };
    /// <summary>Stores the latest received payload.</summary>
    private readonly TextBox _received = new() { Name = "ReceivedText", IsReadOnly = true };
    /// <summary>Stores the bounded network error history.</summary>
    private readonly TextBox _errors = new() { Name = "Errors", IsReadOnly = true, AcceptsReturn = true, MinHeight = 140 };
    /// <summary>Displays the session activity independently of the received payload.</summary>
    private readonly TextBox _activity = new() { Name = "ActivityLog", IsReadOnly = true, AcceptsReturn = true };
    /// <summary>Contains diagnostic errors only while they have entries.</summary>
    private readonly Border _errorGroup;
    /// <summary>Tracks currently connected network clients.</summary>
    private readonly ListBox _clients = new() { Name = "Clients", MinHeight = 140 };
    /// <summary>Edits the remote host before connecting.</summary>
    private readonly TextBox _host = new() { Name = "Receiver", MinWidth = 100 };
    /// <summary>Edits the reply timeout in milliseconds.</summary>
    private readonly TextBox _timeout = new() { Name = "ReplyTimeout", Text = "5000", Width = 70 };
    /// <summary>Edits the end-of-packet marker.</summary>
    private readonly TextBox _eop = new() { Name = "ReplyEop", Text = "", Width = 70 };
    /// <summary>Edits the minimum reply byte count.</summary>
    private readonly TextBox _count = new() { Name = "ReplyCount", Text = "0", Width = 60 };
    /// <summary>Edits the tester delay in milliseconds.</summary>
    private readonly TextBox _interval = new() { Name = "TestInterval", Text = "1000", Width = 90 };
    /// <summary>Displays the transmitted byte count.</summary>
    private readonly TextBox _sent = new() { Name = "BytesSent", IsReadOnly = true, Text = "0", Width = 80 };
    /// <summary>Displays the received byte count.</summary>
    private readonly TextBox _receivedCount = new() { Name = "BytesReceived", IsReadOnly = true, Text = "0", Width = 80 };
    /// <summary>Selects hexadecimal payload representation.</summary>
    private readonly ToggleSwitch _hex = new() { Name = "HexMode", Content = ExampleResources.Hex, IsChecked = true, OnContent = null, OffContent = null };
    /// <summary>Selects buffered reply reception for each send.</summary>
    private readonly ToggleSwitch _sync = new() { Name = "ReceiveReply", Content = ExampleResources.Sync, OnContent = null, OffContent = null };
    /// <summary>Enables echoing received network packets.</summary>
    private readonly ToggleSwitch _echo = new() { Name = "Echo", Content = ExampleResources.Echo, OnContent = null, OffContent = null };
    /// <summary>Displays connection state.</summary>
    private readonly TextBlock _state = new() { Name = "ConnectionState" };
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

    /// <summary>Retains payload controls across navigation.</summary>
    private readonly StackPanel _dataView = new() { Name = "DataView", Spacing = 12 };
    /// <summary>Contains reply settings and testing controls.</summary>
    private readonly StackPanel _communicationView = new() { Name = "CommunicationView", Spacing = 12, IsVisible = false };
    /// <summary>Contains persistent read-only activity history.</summary>
    private readonly StackPanel _activityView = new() { Name = "ActivityView", Spacing = 12, IsVisible = false };
    /// <summary>Prevents navigation while a settings editor owns the view.</summary>
    private bool _editing;
    /// <summary>Creates Serial-style connection, payload and received cards while retaining network tools in separate sections.</summary>
    /// <param name="properties">Opens the network settings editor.</param>
    /// <param name="session">The network session owned by the calling view.</param>
    internal NetworkExamplePanel(ExampleSession session, Func<Task> properties)
    {
        this._session = session;
        this._properties = properties;
        _controller = new(session);
        _host.Text = ((GXNet)session.Media).HostName;
        var body = ExampleLayout.CreateBody(_state);
        var actions = ExampleLayout.Toolbar(_dataView);
        _open = Button(actions, "Open", ExampleResources.Open, async () =>
        {
            var net = (GXNet)session.Media;
            if (!net.Server && !string.IsNullOrWhiteSpace(_host.Text)) { net.HostName = _host.Text; }
            await session.OpenAsync();
        });
        _close = Button(actions, "Close", ExampleResources.Close, async () => { var stopping = _controller.StopTesterAsync(); await session.CloseAsync(); await stopping; });
        _settings = Button(null, "Settings", ExampleResources.Properties, async () => { _editing = true; try { await this._properties(); _host.Text = ((GXNet)session.Media).HostName; } finally { _editing = false; } });
        _send.AcceptsReturn = false;
        _send.MinHeight = 0;
        _send.FontFamily = new global::Avalonia.Media.FontFamily("Cascadia Mono, DejaVu Sans Mono, Consolas, Menlo, monospace");
        var payload = ExampleLayout.Field(UiResources.DataGroup, _send);
        payload.MaxWidth = double.PositiveInfinity;
        payload.HorizontalAlignment = HorizontalAlignment.Stretch;
        var data = new StackPanel { Spacing = 10 };
        data.Children.Add(payload);
        var sendActions = ExampleLayout.Row(_hex, _echo);
        _transmit = Button(sendActions, "Send", ExampleResources.Send, () => _controller.SendAsync(Options()));
        Button(sendActions, "Clear", ExampleResources.Clear, () => { _controller.ClearReceived(); return Task.CompletedTask; });
        data.Children.Add(sendActions);
        _echo.IsCheckedChanged += (_, _) => _controller.Echo = _echo.IsChecked == true;
        _dataView.Children.Add(ExampleLayout.Group(UiResources.DataGroup, data));
        ExampleLayout.ConfigureLog(_received);
        _dataView.Children.Add(ExampleLayout.Group(ExampleResources.ReceivedDataHeading, _received));
        _timeout.Width = double.NaN; _eop.Width = double.NaN; _count.Width = double.NaN;
        var replyOptions = ExampleLayout.Group(UiResources.ReplyGroup, new StackPanel { Spacing = 10, Children = { _sync, ExampleLayout.Row(
            ExampleLayout.Field(UiResources.CountLabel, _count), ExampleLayout.Field(UiResources.TerminatorLabel, _eop), ExampleLayout.Field(UiResources.TimeoutLabel, _timeout)) } });
        var tools = new StackPanel { Spacing = 8 };
        tools.Children.Add(ExampleLayout.Row(Label(ExampleResources.Sent), _sent, Label(ExampleResources.ReceivedLabel), _receivedCount));
        var testRow = ExampleLayout.Row(Label(ExampleResources.Interval), _interval);
        _tester = Button(testRow, "Tester", ExampleResources.GoStop, async () =>
        {
            if (_controller.IsTesting) { await _controller.StopTesterAsync(); }
            else { _controller.StartTester(Options(), int.Parse(_interval.Text ?? "1000")); }
        });
        tools.Children.Add(testRow);
        _communicationView.Children.Add(ExampleLayout.TwoColumns(replyOptions, ExampleLayout.Group(UiResources.ToolsGroup, tools)));
        _clients.Height = 140; _clients.MinHeight = 120;
        _errors.Height = 140; _errors.MinHeight = 120; _errors.FontFamily = _send.FontFamily;
        _communicationView.Children.Add(ExampleLayout.Group(UiResources.ConnectionGroup, ExampleLayout.Field(UiResources.HostLabel, _host)));
        _communicationView.Children.Add(ExampleLayout.Group(ExampleResources.Clients, _clients));
        var activityActions = ExampleLayout.Row();
        Button(activityActions, "ClearActivity", ExampleResources.Clear, () => { _controller.ClearActivity(); return Task.CompletedTask; });
        _activityView.Children.Add(activityActions);
        _errorGroup = ExampleLayout.Group(ExampleResources.Errors, _errors);
        _activityView.Children.Add(_errorGroup);
        ExampleLayout.ConfigureLog(_activity);
        _activityView.Children.Add(ExampleLayout.Group(UiResources.ActivityGroup, _activity));
        body.Children.Add(_dataView); body.Children.Add(_communicationView); body.Children.Add(_activityView);
        var menu = new Menu { Name = "ApplicationMenu" };
        var connection = new MenuItem { Name = "ConnectionMenu", Header = UiResources.ConnectionGroup };
        connection.Items.Add(Command("OpenMenu", ExampleResources.Open, _open));
        connection.Items.Add(Command("CloseMenu", ExampleResources.Close, _close));
        var view = new MenuItem { Name = "ViewMenu", Header = UiResources.ViewMenu };
        view.Items.Add(Navigation("DataNavigation", UiResources.DataGroup, _dataView));
        view.Items.Add(Navigation("ActivityNavigation", UiResources.ActivityGroup, _activityView));
        var configuration = new MenuItem { Name = "SettingsMenu", Header = UiResources.SettingsMenu };
        configuration.Items.Add(Command("Settings", UiResources.MediaSettings, _settings));
        configuration.Items.Add(Navigation("CommunicationNavigation", UiResources.CommunicationSettings, _communicationView));
        menu.Items.Add(connection); menu.Items.Add(view); menu.Items.Add(configuration);
        var frame = new DockPanel();
        DockPanel.SetDock(menu, Dock.Top);
        frame.Children.Add(menu);
        frame.Children.Add(ExampleLayout.WithStatus(body, _mediaStatus));
        Content = frame;
        Volatile.Write(ref _connectionPhase, (int)(session.Media.IsOpen ? MediaState.Open : MediaState.Closed));
        session.Media.OnMediaStateChange += MediaStateChanged;
        Refresh();
        _preferencesLoading = RestorePreferencesAsync();

    }
    /// <summary>Records transport lifecycle events and refreshes controls on the UI thread.</summary>
    /// <param name="sender">The transport publishing the event.</param>
    /// <param name="args">The new media state.</param>
    private void MediaStateChanged(object? sender, MediaStateEventArgs args)
    {
        if (Volatile.Read(ref _shuttingDown) != 0 || args.State == MediaState.Changed)
        {
            return;
        }
        Volatile.Write(ref _connectionPhase, (int)args.State);
        if (Dispatcher.UIThread.CheckAccess())
        {
            RefreshMediaState();
        }
        else
        {
            Dispatcher.UIThread.Post(RefreshMediaState);
        }
    }
    /// <summary>Ignores queued transport notifications after shutdown begins.</summary>
    private void RefreshMediaState()
    {
        if (Volatile.Read(ref _shuttingDown) == 0)
        {
            Refresh();
        }
    }
    /// <summary>Creates a menu command using the existing button availability.</summary>
    /// <param name="name">The automation identifier.</param>
    /// <param name="caption">The localized caption.</param>
    /// <param name="button">The existing lifecycle command.</param>
    /// <returns>The menu command.</returns>
    private MenuItem Command(string name, string caption, Button button)
    {
        var item = new MenuItem { Name = name, Header = caption };
        item.Bind(MenuItem.IsEnabledProperty, button.GetObservable(global::Avalonia.Controls.Button.IsEnabledProperty));
        item.Click += (_, _) =>
        {
            if (CanNavigate && button.IsEnabled)
            {
                button.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(global::Avalonia.Controls.Button.ClickEvent));
            }
        };
        return item;
    }
    /// <summary>Creates a persistent section navigation command.</summary>
    /// <param name="name">The automation identifier.</param>
    /// <param name="caption">The localized caption.</param>
    /// <param name="section">The section to display.</param>
    /// <returns>The navigation menu item.</returns>
    private MenuItem Navigation(string name, string caption, StackPanel section)
    {
        var item = new MenuItem { Name = name, Header = caption };
        item.Click += (_, _) => ShowSection(section);
        return item;
    }
    /// <summary>Gets whether navigation can safely change the current view.</summary>
    internal bool CanNavigate => _preferencesReady && !_editing && System.Threading.Volatile.Read(ref _shuttingDown) == 0;
    /// <summary>Changes the visible section while retaining all controls.</summary>
    /// <param name="section">The section to show.</param>
    private void ShowSection(StackPanel section)
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
        return new(_send.Text ?? "", _hex.IsChecked == true, _sync.IsChecked == true, _eop.Text,
            int.Parse(_count.Text ?? "0"), int.Parse(_timeout.Text ?? "5000"), net.Server ? _clients.SelectedItem as string : null);
    }
    /// <summary>Creates a vertically centered caption.</summary>
    /// <param name="text">The payload or caption to use.</param>
    /// <returns>The caption control.</returns>
    private static TextBlock Label(string text) => new() { Text = text, VerticalAlignment = VerticalAlignment.Center };
    /// <summary>Creates an asynchronous action button that reports failures in the error view.</summary>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <param name="name">The invariant automation identifier.</param>
    /// <param name="parent">The optional action row; null retains a command for the settings menu only.</param>
    /// <param name="title">The localized caption.</param>
    /// <returns>The configured action button.</returns>
    private Button Button(Panel? parent, string name, string title, Func<Task> action)
    {
        var button = new Button { Name = name, Content = title };
        ExampleLayout.ConfigureAction(button, name is "Open" or "Send");
        button.Click += async (_, _) =>
        {
            try { Task task = action(); Refresh(); await task; }
            catch (OperationCanceledException) { }
            catch (Exception ex) { _controller.Report(ex.Message); }
            finally { Refresh(); }
        };
        parent?.Children.Add(button);
        return button;
    }
    /// <summary>Updates availability, counters and clients from connection state.</summary>
    internal void Refresh()
    {
        if (Volatile.Read(ref _shuttingDown) != 0)
        {
            _open.IsEnabled = false;
            _close.IsEnabled = false;
            _settings.IsEnabled = false;
            _transmit.IsEnabled = false;
            _tester.IsEnabled = false;
            _host.IsEnabled = false;
            return;
        }
        MediaState phase = (MediaState)Volatile.Read(ref _connectionPhase);
        bool closed = phase == MediaState.Closed;
        bool running = _controller.IsTesting;
        bool busy = _session.IsBusy || running;
        bool connected = phase == MediaState.Open;
        _open.IsEnabled = !busy && closed;
        _settings.IsEnabled = !busy && closed;
        _close.IsEnabled = connected;
        _transmit.IsEnabled = connected && !busy;
        _tester.IsEnabled = connected && (!busy || running);
        _tester.Content = running ? ExampleResources.Stop : ExampleResources.GoStop;
        _host.IsEnabled = closed && !busy && !((GXNet)_session.Media).Server;
        ExampleLayout.UpdateStatus(_state, connected, busy);
        _received.Text = _controller.Received(_hex.IsChecked == true);
        _sent.Text = _session.Media.BytesSent.ToString(); _receivedCount.Text = _session.Media.BytesReceived.ToString();
        _errors.Text = string.Join(Environment.NewLine, _controller.Errors);
        _errorGroup.IsVisible = _controller.Errors.Count != 0;
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
    /// <summary>Detaches lifecycle callbacks before releasing controller resources.</summary>
    /// <returns>A task completing after the controller has stopped.</returns>
    private async Task DisposeCoreAsync()
    {
        Volatile.Write(ref _shuttingDown, 1);
        _session.Media.OnMediaStateChange -= MediaStateChanged;
        if (Dispatcher.UIThread.CheckAccess()) { Refresh(); }
        else { Dispatcher.UIThread.Post(Refresh); }
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
            ["Hex"] = _hex.IsChecked == true,
            ["Echo"] = _echo.IsChecked == true,
            ["ReceiveReply"] = _sync.IsChecked == true,
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
        _hex.IsChecked = values["Hex"]?.GetValue<bool>() ?? (_hex.IsChecked == true);
        _echo.IsChecked = values["Echo"]?.GetValue<bool>() ?? false;
        _sync.IsChecked = values["ReceiveReply"]?.GetValue<bool>() ?? (_sync.IsChecked == true);
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
        if (!Dispatcher.UIThread.CheckAccess())
        {
            await Dispatcher.UIThread.InvokeAsync(SavePreferencesAsync);
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
