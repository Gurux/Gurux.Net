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
    private readonly TextBlock mediaStatus = new() { Name = "MediaStatus" };

    /// <summary>Persists this application's media and outgoing data preferences.</summary>
    private readonly ExampleSettingsStore preferences = new();
    /// <summary>Completes when startup preferences have been applied on the UI thread.</summary>
    private Task preferencesLoading = Task.CompletedTask;
    /// <summary>Tracks the latest queued write so shutdown waits for the final disk update.</summary>
    private Task preferencesSaving = Task.CompletedTask;
    /// <summary>Prevents persistence until startup restoration has completed.</summary>
    private bool preferencesReady;
    /// <summary>Tracks the most recently requested snapshot and avoids saving unchanged controls.</summary>
    private string? preferencesSnapshot;

    /// <summary>Stores the latest transport connection phase independently of command activity.</summary>
    private int connectionPhase;
    /// <summary>Prevents callbacks from refreshing disposed resources.</summary>
    private int shuttingDown;
    /// <summary>Shares controller cleanup between repeated disposal requests.</summary>
    private Task? disposal;
    /// <summary>Owns the network connection used by this component.</summary>
    private readonly ExampleSession session;
    /// <summary>Coordinates sends, echo responses and periodic testing.</summary>
    private readonly NetworkExampleController controller;
    /// <summary>Opens the network settings editor for this view.</summary>
    private readonly Func<Task> properties;
    /// <summary>Edits the outgoing payload.</summary>
    private readonly TextBox send = new() { Name = "SendText", PlaceholderText = ExampleResources.SendPlaceholder, Text = "4D", AcceptsReturn = true, MinHeight = 64 };
    /// <summary>Stores the latest received payload.</summary>
    private readonly TextBox received = new() { Name = "ReceivedText", IsReadOnly = true };
    /// <summary>Stores the bounded network error history.</summary>
    private readonly TextBox errors = new() { Name = "Errors", IsReadOnly = true, AcceptsReturn = true, MinHeight = 140 };
    /// <summary>Displays the session activity independently of the received payload.</summary>
    private readonly TextBox activity = new() { Name = "ActivityLog", IsReadOnly = true, AcceptsReturn = true };
    /// <summary>Contains diagnostic errors only while they have entries.</summary>
    private readonly Border errorGroup;
    /// <summary>Tracks currently connected network clients.</summary>
    private readonly ListBox clients = new() { Name = "Clients", MinHeight = 140 };
    /// <summary>Edits the remote host before connecting.</summary>
    private readonly TextBox host = new() { Name = "Receiver", MinWidth = 100 };
    /// <summary>Edits the reply timeout in milliseconds.</summary>
    private readonly TextBox timeout = new() { Name = "ReplyTimeout", Text = "5000", Width = 70 };
    /// <summary>Edits the end-of-packet marker.</summary>
    private readonly TextBox eop = new() { Name = "ReplyEop", Text = "", Width = 70 };
    /// <summary>Edits the minimum reply byte count.</summary>
    private readonly TextBox count = new() { Name = "ReplyCount", Text = "0", Width = 60 };
    /// <summary>Edits the tester delay in milliseconds.</summary>
    private readonly TextBox interval = new() { Name = "TestInterval", Text = "1000", Width = 90 };
    /// <summary>Displays the transmitted byte count.</summary>
    private readonly TextBox sent = new() { Name = "BytesSent", IsReadOnly = true, Text = "0", Width = 80 };
    /// <summary>Displays the received byte count.</summary>
    private readonly TextBox receivedCount = new() { Name = "BytesReceived", IsReadOnly = true, Text = "0", Width = 80 };
    /// <summary>Selects hexadecimal payload representation.</summary>
    private readonly ToggleSwitch hex = new() { Name = "HexMode", Content = ExampleResources.Hex, IsChecked = true, OnContent = null, OffContent = null };
    /// <summary>Selects buffered reply reception for each send.</summary>
    private readonly ToggleSwitch sync = new() { Name = "ReceiveReply", Content = ExampleResources.Sync, OnContent = null, OffContent = null };
    /// <summary>Enables echoing received network packets.</summary>
    private readonly ToggleSwitch echo = new() { Name = "Echo", Content = ExampleResources.Echo, OnContent = null, OffContent = null };
    /// <summary>Displays connection state.</summary>
    private readonly TextBlock state = new() { Name = "ConnectionState" };
    /// <summary>Opens the configured connection.</summary>
    private readonly Button open;
    /// <summary>Cancels pending operations and closes the transport.</summary>
    private readonly Button close;
    /// <summary>Opens network settings.</summary>
    private readonly Button settings;
    /// <summary>Sends the current payload.</summary>
    private readonly Button transmit;
    /// <summary>Starts or stops periodic sending.</summary>
    private readonly Button tester;

    /// <summary>Retains payload controls across navigation.</summary>
    private readonly StackPanel dataView = new() { Name = "DataView", Spacing = 12 };
    /// <summary>Contains reply settings and testing controls.</summary>
    private readonly StackPanel communicationView = new() { Name = "CommunicationView", Spacing = 12, IsVisible = false };
    /// <summary>Contains persistent read-only activity history.</summary>
    private readonly StackPanel activityView = new() { Name = "ActivityView", Spacing = 12, IsVisible = false };
    /// <summary>Prevents navigation while a settings editor owns the view.</summary>
    private bool editing;
    /// <summary>Creates Serial-style connection, payload and received cards while retaining network tools in separate sections.</summary>
    /// <param name="properties">Opens the network settings editor.</param>
    /// <param name="session">The network session owned by the calling view.</param>
    internal NetworkExamplePanel(ExampleSession session, Func<Task> properties)
    {
        this.session = session;
        this.properties = properties;
        controller = new(session);
        host.Text = ((GXNet)session.Media).HostName;
        var body = ExampleLayout.CreateBody(state);
        var actions = ExampleLayout.Toolbar(dataView);
        open = Button(actions, "Open", ExampleResources.Open, async () =>
        {
            var net = (GXNet)session.Media;
            if (!net.Server && !string.IsNullOrWhiteSpace(host.Text)) { net.HostName = host.Text; }
            await session.OpenAsync();
        });
        close = Button(actions, "Close", ExampleResources.Close, async () => { var stopping = controller.StopTesterAsync(); await session.CloseAsync(); await stopping; });
        settings = Button(null, "Settings", ExampleResources.Properties, async () => { editing = true; try { await this.properties(); host.Text = ((GXNet)session.Media).HostName; } finally { editing = false; } });
        send.AcceptsReturn = false;
        send.MinHeight = 0;
        send.FontFamily = new global::Avalonia.Media.FontFamily("Cascadia Mono, DejaVu Sans Mono, Consolas, Menlo, monospace");
        var payload = ExampleLayout.Field(UiResources.DataGroup, send);
        payload.MaxWidth = double.PositiveInfinity;
        payload.HorizontalAlignment = HorizontalAlignment.Stretch;
        var data = new StackPanel { Spacing = 10 };
        data.Children.Add(payload);
        var sendActions = ExampleLayout.Row(hex, echo);
        transmit = Button(sendActions, "Send", ExampleResources.Send, () => controller.SendAsync(Options()));
        Button(sendActions, "Clear", ExampleResources.Clear, () => { controller.ClearReceived(); return Task.CompletedTask; });
        data.Children.Add(sendActions);
        echo.IsCheckedChanged += (_, _) => controller.Echo = echo.IsChecked == true;
        dataView.Children.Add(ExampleLayout.Group(UiResources.DataGroup, data));
        ExampleLayout.ConfigureLog(received);
        dataView.Children.Add(ExampleLayout.Group(ExampleResources.ReceivedDataHeading, received));
        timeout.Width = double.NaN; eop.Width = double.NaN; count.Width = double.NaN;
        var replyOptions = ExampleLayout.Group(UiResources.ReplyGroup, new StackPanel { Spacing = 10, Children = { sync, ExampleLayout.Row(
            ExampleLayout.Field(UiResources.CountLabel, count), ExampleLayout.Field(UiResources.TerminatorLabel, eop), ExampleLayout.Field(UiResources.TimeoutLabel, timeout)) } });
        var tools = new StackPanel { Spacing = 8 };
        tools.Children.Add(ExampleLayout.Row(Label(ExampleResources.Sent), sent, Label(ExampleResources.ReceivedLabel), receivedCount));
        var testRow = ExampleLayout.Row(Label(ExampleResources.Interval), interval);
        tester = Button(testRow, "Tester", ExampleResources.GoStop, async () =>
        {
            if (controller.IsTesting) { await controller.StopTesterAsync(); }
            else { controller.StartTester(Options(), int.Parse(interval.Text ?? "1000")); }
        });
        tools.Children.Add(testRow);
        communicationView.Children.Add(ExampleLayout.TwoColumns(replyOptions, ExampleLayout.Group(UiResources.ToolsGroup, tools)));
        clients.Height = 140; clients.MinHeight = 120;
        errors.Height = 140; errors.MinHeight = 120; errors.FontFamily = send.FontFamily;
        communicationView.Children.Add(ExampleLayout.Group(UiResources.ConnectionGroup, ExampleLayout.Field(UiResources.HostLabel, host)));
        communicationView.Children.Add(ExampleLayout.Group(ExampleResources.Clients, clients));
        var activityActions = ExampleLayout.Row();
        Button(activityActions, "ClearActivity", ExampleResources.Clear, () => { controller.ClearActivity(); return Task.CompletedTask; });
        activityView.Children.Add(activityActions);
        errorGroup = ExampleLayout.Group(ExampleResources.Errors, errors);
        activityView.Children.Add(errorGroup);
        ExampleLayout.ConfigureLog(activity);
        activityView.Children.Add(ExampleLayout.Group(UiResources.ActivityGroup, activity));
        body.Children.Add(dataView); body.Children.Add(communicationView); body.Children.Add(activityView);
        var menu = new Menu { Name = "ApplicationMenu" };
        var connection = new MenuItem { Name = "ConnectionMenu", Header = UiResources.ConnectionGroup };
        connection.Items.Add(Command("OpenMenu", ExampleResources.Open, open));
        connection.Items.Add(Command("CloseMenu", ExampleResources.Close, close));
        var view = new MenuItem { Name = "ViewMenu", Header = UiResources.ViewMenu };
        view.Items.Add(Navigation("DataNavigation", UiResources.DataGroup, dataView));
        view.Items.Add(Navigation("ActivityNavigation", UiResources.ActivityGroup, activityView));
        var configuration = new MenuItem { Name = "SettingsMenu", Header = UiResources.SettingsMenu };
        configuration.Items.Add(Command("Settings", UiResources.MediaSettings, settings));
        configuration.Items.Add(Navigation("CommunicationNavigation", UiResources.CommunicationSettings, communicationView));
        menu.Items.Add(connection); menu.Items.Add(view); menu.Items.Add(configuration);
        var frame = new DockPanel();
        DockPanel.SetDock(menu, Dock.Top);
        frame.Children.Add(menu);
        frame.Children.Add(ExampleLayout.WithStatus(body, mediaStatus));
        Content = frame;
        Volatile.Write(ref connectionPhase, (int)(session.Media.IsOpen ? MediaState.Open : MediaState.Closed));
        session.Media.OnMediaStateChange += MediaStateChanged;
        Refresh();
        preferencesLoading = RestorePreferencesAsync();

    }
    /// <summary>Records transport lifecycle events and refreshes controls on the UI thread.</summary>
    /// <param name="sender">The transport publishing the event.</param>
    /// <param name="args">The new media state.</param>
    private void MediaStateChanged(object? sender, MediaStateEventArgs args)
    {
        if (Volatile.Read(ref shuttingDown) != 0 || args.State == MediaState.Changed)
        {
            return;
        }
        Volatile.Write(ref connectionPhase, (int)args.State);
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
        if (Volatile.Read(ref shuttingDown) == 0)
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
    internal bool CanNavigate => preferencesReady && !editing && System.Threading.Volatile.Read(ref shuttingDown) == 0;
    /// <summary>Changes the visible section while retaining all controls.</summary>
    /// <param name="section">The section to show.</param>
    private void ShowSection(StackPanel section)
    {
        if (CanNavigate)
        {
            dataView.IsVisible = ReferenceEquals(section, dataView);
            communicationView.IsVisible = ReferenceEquals(section, communicationView);
            activityView.IsVisible = ReferenceEquals(section, activityView);
        }
    }
    /// <summary>Collects current payload and receive settings from the controls.</summary>
    /// <returns>The selected network send and receive settings.</returns>
    private NetworkSendOptions Options()
    {
        var net = (GXNet)session.Media;
        return new(send.Text ?? "", hex.IsChecked == true, sync.IsChecked == true, eop.Text,
            int.Parse(count.Text ?? "0"), int.Parse(timeout.Text ?? "5000"), net.Server ? clients.SelectedItem as string : null);
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
            catch (Exception ex) { controller.Report(ex.Message); }
            finally { Refresh(); }
        };
        parent?.Children.Add(button);
        return button;
    }
    /// <summary>Updates availability, counters and clients from connection state.</summary>
    internal void Refresh()
    {
        if (Volatile.Read(ref shuttingDown) != 0)
        {
            open.IsEnabled = false;
            close.IsEnabled = false;
            settings.IsEnabled = false;
            transmit.IsEnabled = false;
            tester.IsEnabled = false;
            host.IsEnabled = false;
            return;
        }
        MediaState phase = (MediaState)Volatile.Read(ref connectionPhase);
        bool closed = phase == MediaState.Closed;
        bool running = controller.IsTesting;
        bool busy = session.IsBusy || running;
        bool connected = phase == MediaState.Open;
        open.IsEnabled = !busy && closed;
        settings.IsEnabled = !busy && closed;
        close.IsEnabled = connected;
        transmit.IsEnabled = connected && !busy;
        tester.IsEnabled = connected && (!busy || running);
        tester.Content = running ? ExampleResources.Stop : ExampleResources.GoStop;
        host.IsEnabled = closed && !busy && !((GXNet)session.Media).Server;
        ExampleLayout.UpdateStatus(state, connected, busy);
        received.Text = controller.Received(hex.IsChecked == true);
        sent.Text = session.Media.BytesSent.ToString(); receivedCount.Text = session.Media.BytesReceived.ToString();
        errors.Text = string.Join(Environment.NewLine, controller.Errors);
        errorGroup.IsVisible = controller.Errors.Count != 0;
        string snapshot = string.Join(Environment.NewLine, session.Log);
        if (activity.Text != snapshot) { activity.Text = snapshot; }
        var current = session.Clients.ToArray();
        if (!(clients.ItemsSource as string[] ?? []).SequenceEqual(current))
        {
            var selected = clients.SelectedItem;
            clients.ItemsSource = current;
            clients.SelectedItem = current.Contains(selected as string) ? selected : null;
        }
        _ = SavePreferencesAsync();
        var statusNetwork = (GXNet)session.Media;
        string statusAddress = string.IsNullOrWhiteSpace(statusNetwork.HostName)
            ? (statusNetwork.Server ? (statusNetwork.UseIPv6 ? "::" : "0.0.0.0") : StatusResources.StatusNotConfigured)
            : statusNetwork.HostName;
        if (statusAddress.Contains(':') && !statusAddress.StartsWith("[", StringComparison.Ordinal))
        {
            statusAddress = string.Concat("[", statusAddress, "]");
        }
        mediaStatus.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, StatusResources.StatusNetwork,
            session.Media.IsOpen ? StatusResources.StatusOpen : StatusResources.StatusClosed,
            statusNetwork.Protocol.ToString().ToUpperInvariant(), statusAddress, statusNetwork.Port);

    }
    /// <inheritdoc/>
    public ValueTask DisposeAsync() => new(disposal ??= DisposeCoreAsync());
    /// <summary>Detaches lifecycle callbacks before releasing controller resources.</summary>
    /// <returns>A task completing after the controller has stopped.</returns>
    private async Task DisposeCoreAsync()
    {
        Volatile.Write(ref shuttingDown, 1);
        session.Media.OnMediaStateChange -= MediaStateChanged;
        if (Dispatcher.UIThread.CheckAccess()) { Refresh(); }
        else { Dispatcher.UIThread.Post(Refresh); }
        await preferencesLoading;
        await SavePreferencesAsync();
        await controller.DisposeAsync();
    }
    /// <summary>Captures media configuration and outgoing controls without opening a connection.</summary>
    /// <returns>A detached JSON object containing this view's persistent preferences.</returns>
    private JsonObject CapturePreferences()
    {
        var values = new JsonObject
        {
            ["Media"] = JsonNode.Parse(session.Media.JsonSettings ?? "{}"),
            ["SendText"] = send.Text ?? "",
            ["Hex"] = hex.IsChecked == true,
            ["Echo"] = echo.IsChecked == true,
            ["ReceiveReply"] = sync.IsChecked == true,
            ["ReplyEop"] = eop.Text ?? "",
            ["ReplyCount"] = count.Text ?? "",
            ["ReplyTimeout"] = timeout.Text ?? "",
            ["Host"] = host.Text ?? ""
        };
        return values;
    }

    /// <summary>Applies stored media configuration and interface values while the transport remains closed.</summary>
    /// <param name="values">The saved preferences; missing entries retain their current defaults.</param>
    private void ApplyPreferences(JsonObject values)
    {
        if (values["Media"] is JsonObject media) { session.Media.JsonSettings = media.ToJsonString(); }
        send.Text = values["SendText"]?.GetValue<string>() ?? send.Text;
        hex.IsChecked = values["Hex"]?.GetValue<bool>() ?? (hex.IsChecked == true);
        echo.IsChecked = values["Echo"]?.GetValue<bool>() ?? false;
        sync.IsChecked = values["ReceiveReply"]?.GetValue<bool>() ?? (sync.IsChecked == true);
        eop.Text = values["ReplyEop"]?.GetValue<string>() ?? eop.Text;
        count.Text = values["ReplyCount"]?.GetValue<string>() ?? count.Text;
        timeout.Text = values["ReplyTimeout"]?.GetValue<string>() ?? timeout.Text;
        host.Text = values["Host"]?.GetValue<string>() ?? host.Text;
    }

    /// <summary>Restores preferences asynchronously before enabling interaction and keeps defaults if loading fails.</summary>
    /// <returns>A task completing after the saved values or original defaults have been applied.</returns>
    private async Task RestorePreferencesAsync()
    {
        IsEnabled = false;
        JsonObject defaults = CapturePreferences();
        try
        {
            JsonObject? values = await preferences.LoadAsync();
            if (System.Threading.Volatile.Read(ref shuttingDown) != 0) { return; }
            if (values is not null) { ApplyPreferences(values); }
        }
        catch (Exception ex)
        {
            if (System.Threading.Volatile.Read(ref shuttingDown) != 0) { return; }
            ApplyPreferences(defaults);
            controller.Report(ex.Message);
        }
        preferencesSnapshot = CapturePreferences().ToJsonString();
        preferencesReady = true;
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
        if (!preferencesReady) { return; }
        string? snapshot = null;
        try
        {
            JsonObject values = CapturePreferences();
            snapshot = values.ToJsonString();
            if (snapshot == preferencesSnapshot) { await preferencesSaving; return; }
            preferencesSnapshot = snapshot;
            preferencesSaving = preferences.SaveAsync(values);
            await preferencesSaving;
        }
        catch (Exception ex)
        {
            if (preferencesSnapshot == snapshot) { preferencesSnapshot = null; }
            controller.Report(ex.Message);
        }
    }

}
