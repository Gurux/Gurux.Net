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
    private readonly Label mediaStatus = new() { AutomationId = "MediaStatus" };

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

    /// <summary>Owns the network connection used by this component.</summary>
    private readonly ExampleSession session;
    /// <summary>Coordinates sends, echo responses and periodic testing.</summary>
    private readonly NetworkExampleController controller;
    /// <summary>Edits the outgoing payload.</summary>
    private readonly Entry send = new() { AutomationId = "SendText", Placeholder = ExampleResources.SendPlaceholder, Text = "4D" };
    /// <summary>Stores the latest received payload.</summary>
    private readonly Editor received = new() { AutomationId = "ReceivedText", IsReadOnly = true, HeightRequest = 60 };
    /// <summary>Stores the bounded network error history.</summary>
    private readonly Editor errors = new() { AutomationId = "Errors", IsReadOnly = true, HeightRequest = 160 };
    /// <summary>Tracks currently connected network clients.</summary>
    private readonly CollectionView clients = new()
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
    private readonly Entry host = new() { AutomationId = "Receiver", WidthRequest = 160 };
    /// <summary>Edits the reply timeout in milliseconds.</summary>
    private readonly Entry timeout = new() { AutomationId = "ReplyTimeout", Text = "5000", WidthRequest = 76, Keyboard = Keyboard.Numeric };
    /// <summary>Edits the end-of-packet marker.</summary>
    private readonly Entry eop = new() { AutomationId = "ReplyEop", WidthRequest = 76 };
    /// <summary>Edits the minimum reply byte count.</summary>
    private readonly Entry count = new() { AutomationId = "ReplyCount", Text = "0", WidthRequest = 65, Keyboard = Keyboard.Numeric };
    /// <summary>Edits the tester delay in milliseconds.</summary>
    private readonly Entry interval = new() { AutomationId = "TestInterval", Text = "1000", WidthRequest = 90, Keyboard = Keyboard.Numeric };
    /// <summary>Displays the transmitted byte count.</summary>
    private readonly Entry sent = new() { AutomationId = "BytesSent", IsReadOnly = true, Text = "0", WidthRequest = 90 };
    /// <summary>Displays the received byte count.</summary>
    private readonly Entry receivedCount = new() { AutomationId = "BytesReceived", IsReadOnly = true, Text = "0", WidthRequest = 90 };
    /// <summary>Selects hexadecimal payload representation.</summary>
    private readonly Switch hex = new() { AutomationId = "HexMode", IsToggled = true };
    /// <summary>Selects buffered reply reception for each send.</summary>
    private readonly Switch sync = new() { AutomationId = "ReceiveReply" };
    /// <summary>Enables echoing received network packets.</summary>
    private readonly Switch echo = new() { AutomationId = "Echo" };
    /// <summary>Displays connection state.</summary>
    private readonly Label state = new() { AutomationId = "ConnectionState" };
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
    /// <summary>Displays the bounded session activity without changing transport ownership.</summary>
    private readonly Editor activity = new() { AutomationId = "ActivityLog", IsReadOnly = true, HeightRequest = 240 };
    /// <summary>Contains recent network errors.</summary>
    private readonly View errorGroup;

    /// <summary>Stores the latest connection phase independently of periodic UI refreshes.</summary>
    private int mediaPhase;
    /// <summary>Prevents callbacks and queued refreshes from touching a disposed view.</summary>
    private int shuttingDown;
    /// <summary>Marshals transport state notifications onto the native UI thread.</summary>
    private readonly Microsoft.Maui.Dispatching.IDispatcher uiDispatcher;
    /// <summary>Shares the asynchronous shutdown operation across disposal requests.</summary>
    private Task? disposal;

    /// <summary>Retains payload controls across navigation.</summary>
    private readonly VerticalStackLayout dataView = new() { AutomationId = "DataView", Spacing = 12 };
    /// <summary>Contains reply settings and testing controls.</summary>
    private readonly VerticalStackLayout communicationView = new() { AutomationId = "CommunicationView", Spacing = 12, IsVisible = false };
    /// <summary>Contains persistent read-only activity history.</summary>
    private readonly VerticalStackLayout activityView = new() { AutomationId = "ActivityView", Spacing = 12, IsVisible = false };
    /// <summary>Tracks native menu commands that mirror lifecycle availability.</summary>
    private readonly List<Command> nativeCommands = new();
    /// <summary>Prevents navigation while a settings editor owns the view.</summary>
    private bool editing;
    /// <summary>Creates Serial-style connection, payload and received cards while retaining network tools in separate sections.</summary>
    /// <param name="properties">Opens the network settings editor.</param>
    /// <param name="session">The network session owned by the calling view.</param>
    internal NetworkExamplePanel(ExampleSession session, Func<Task> properties)
    {
        this.session = session;
        controller = new(session);
        host.Text = ((GXNet)session.Media).HostName;
        var body = ExampleLayout.Body();
        body.Children.Add(ExampleLayout.Header(state));
        ExampleLayout.Activity(activity);
        ExampleLayout.Activity(received);
        if (OperatingSystem.IsWindows()) { send.FontFamily = "Consolas"; }
        ExampleLayout.Activity(errors);
        errors.HeightRequest = 120;
        open = MakeButton("Open", ExampleResources.Open, async () =>
        {
            var net = (GXNet)session.Media;
            if (!net.Server && !string.IsNullOrWhiteSpace(host.Text)) { net.HostName = host.Text; }
            await session.OpenAsync();
        });
        close = MakeButton("Close", ExampleResources.Close, async () => { var stopping = controller.StopTesterAsync(); await session.CloseAsync(); await stopping; });
        settings = MakeButton("Settings", ExampleResources.Properties, async () => { editing = true; try { await properties(); host.Text = ((GXNet)session.Media).HostName; } finally { editing = false; } });
        transmit = MakeButton("Send", ExampleResources.Send, () => controller.SendAsync(Options()));
        echo.Toggled += (_, args) => controller.Echo = args.Value;
        dataView.Children.Add(ExampleLayout.Group(LayoutResources.ConnectionGroup, ExampleLayout.Row(open, close)));
        var options = ExampleLayout.Row(ExampleLayout.Toggle(hex, ExampleResources.Hex), ExampleLayout.Toggle(echo, ExampleResources.Echo));
        var sendActions = OperatingSystem.IsWindows() ? options : ExampleLayout.Row();
        sendActions.Children.Add(transmit);
        sendActions.Children.Add(MakeButton("Clear", ExampleResources.Clear, () => { controller.ClearReceived(); return Task.CompletedTask; }));
        var data = ExampleLayout.Stack(ExampleLayout.Field(LayoutResources.PayloadLabel, send), options);
        if (!OperatingSystem.IsWindows()) { data.Children.Add(sendActions); }
        dataView.Children.Add(ExampleLayout.Group(LayoutResources.DataGroup, data));
        dataView.Children.Add(ExampleLayout.Group(ExampleResources.ReceivedDataHeading, received));
        var replies = ExampleLayout.Row(ExampleLayout.Field(LayoutResources.CountLabel, count, 164),
            ExampleLayout.Field(LayoutResources.TerminatorLabel, eop, 164), ExampleLayout.Field(LayoutResources.TimeoutLabel, timeout, 164));
        communicationView.Children.Add(ExampleLayout.Group(LayoutResources.ReplyGroup,
            ExampleLayout.Stack(ExampleLayout.Toggle(sync, ExampleResources.Sync), replies)));
        tester = MakeButton("Tester", ExampleResources.GoStop, async () =>
        {
            if (controller.IsTesting) { await controller.StopTesterAsync(); }
            else { controller.StartTester(Options(), int.Parse(interval.Text ?? "1000")); }
        });
        communicationView.Children.Add(ExampleLayout.Pair(
            ExampleLayout.Group(ExampleResources.Statistics, ExampleLayout.Row(ExampleLayout.Field(ExampleResources.Sent, sent, 164), ExampleLayout.Field(ExampleResources.ReceivedLabel, receivedCount, 164))),
            ExampleLayout.Group(ExampleResources.Tester, ExampleLayout.Row(ExampleLayout.Field(ExampleResources.Interval, interval, 164), tester))));
        communicationView.Children.Add(ExampleLayout.Group(LayoutResources.ConnectionGroup, ExampleLayout.Field(LayoutResources.HostLabel, host, 220)));
        communicationView.Children.Add(ExampleLayout.Group(ExampleResources.Clients, clients));
        errorGroup = ExampleLayout.Group(ExampleResources.Errors, errors);
        activityView.Children.Add(ExampleLayout.Row(MakeButton("ClearActivity", ExampleResources.Clear, () => { controller.ClearActivity(); return Task.CompletedTask; })));
        activityView.Children.Add(errorGroup);
        activityView.Children.Add(ExampleLayout.Group(LayoutResources.ActivityGroup, activity));
        body.Children.Add(dataView); body.Children.Add(communicationView); body.Children.Add(activityView);
        Content = ExampleLayout.WithStatus(body, mediaStatus);
        uiDispatcher = Dispatcher;
        mediaPhase = (int)(session.Media.IsOpen ? Gurux.Common.MediaState.Open : Gurux.Common.MediaState.Closed);
        session.Media.OnMediaStateChange += MediaStateChanged;
        Refresh();
        preferencesLoading = RestorePreferencesAsync();

    }
    /// <summary>Gets whether the current lifecycle state permits editing media settings.</summary>
    internal bool CanEditSettings => CanNavigate && settings.IsEnabled;
    /// <summary>Reports native navigation failures through the existing bounded error history.</summary>
    /// <param name="error">The cancellation or failure reported by native navigation.</param>
    internal void ReportNavigationError(Exception error)
    {
        if (System.Threading.Volatile.Read(ref shuttingDown) == 0)
        {
            controller.Report(error.Message);
            Refresh();
        }
    }
    /// <summary>Creates a native lifecycle command with the existing button availability.</summary>
    /// <param name="name">The lifecycle command identifier.</param>
    /// <returns>A command whose availability follows the existing controls.</returns>
    internal Command CreateCommand(string name)
    {
        Button button = name == "Open" ? open : name == "Close" ? close : settings;
        var command = new Command(() => InvokeCommand(name), () => CanNavigate && button.IsEnabled);
        nativeCommands.Add(command);
        return command;
    }
    /// <summary>Shows the existing payload controls.</summary>
    internal void ShowData() => ShowSection(dataView);
    /// <summary>Shows reply and tester settings.</summary>
    internal void ShowCommunication() => ShowSection(communicationView);
    /// <summary>Shows the bounded activity and error history.</summary>
    internal void ShowActivity() => ShowSection(activityView);
    /// <summary>Runs a lifecycle command only when its existing button permits it.</summary>
    /// <param name="name">The existing command identifier.</param>
    internal void InvokeCommand(string name)
    {
        Button button = name == "Open" ? open : name == "Close" ? close : settings;
        if (CanNavigate && button.IsEnabled) { button.SendClicked(); }
    }
    /// <summary>Gets whether navigation can safely change the current view.</summary>
    internal bool CanNavigate => preferencesReady && !editing && System.Threading.Volatile.Read(ref shuttingDown) == 0;
    /// <summary>Changes the visible section while retaining all controls.</summary>
    /// <param name="section">The section to show.</param>
    private void ShowSection(VerticalStackLayout section)
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
        return new(send.Text ?? "", hex.IsToggled, sync.IsToggled, eop.Text, int.Parse(count.Text ?? "0"),
            int.Parse(timeout.Text ?? "5000"), net.Server ? clients.SelectedItem as string : null);
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
            catch (Exception ex) { controller.Report(ex.Message); }
            finally { Refresh(); }
        };
        return button;
    }
    /// <summary>Caches connection phases and schedules immediate native control updates.</summary>
    /// <param name="sender">The media reporting its lifecycle state.</param>
    /// <param name="args">The reported lifecycle phase or settings-change notification.</param>
    private void MediaStateChanged(object? sender, Gurux.Common.MediaStateEventArgs args)
    {
        if (System.Threading.Volatile.Read(ref shuttingDown) != 0) { return; }
        if (args.State != Gurux.Common.MediaState.Changed)
        {
            System.Threading.Volatile.Write(ref mediaPhase, (int)args.State);
        }
        if (uiDispatcher.IsDispatchRequired) { uiDispatcher.Dispatch(Refresh); }
        else { Refresh(); }
    }
    /// <summary>Updates availability, counters and clients from connection state.</summary>
    internal void Refresh()
    {
        if (System.Threading.Volatile.Read(ref shuttingDown) != 0)
        {
            open.IsEnabled = false; close.IsEnabled = false; settings.IsEnabled = false;
            transmit.IsEnabled = false; tester.IsEnabled = false; host.IsEnabled = false;
            foreach (Command command in nativeCommands) { command.ChangeCanExecute(); }
            return;
        }
        var phase = (Gurux.Common.MediaState)System.Threading.Volatile.Read(ref mediaPhase);
        bool closed = phase == Gurux.Common.MediaState.Closed;
        bool running = controller.IsTesting;
        bool busy = session.IsBusy || running;
        bool connected = phase == Gurux.Common.MediaState.Open;
        open.IsEnabled = !busy && closed; settings.IsEnabled = !busy && closed;
        close.IsEnabled = connected; transmit.IsEnabled = connected && !busy;
        tester.IsEnabled = connected && (!busy || running); tester.Text = running ? ExampleResources.Stop : ExampleResources.GoStop;
        host.IsEnabled = closed && !busy && !((GXNet)session.Media).Server;
        foreach (Command command in nativeCommands) { command.ChangeCanExecute(); }
        ExampleLayout.Status(state, busy, connected);
        string latest = controller.Received(hex.IsToggled);
        if (received.Text != latest) { received.Text = latest; }
        sent.Text = session.Media.BytesSent.ToString(); receivedCount.Text = session.Media.BytesReceived.ToString();
        string messages = string.Join(Environment.NewLine, controller.Errors);
        if (errors.Text != messages) { errors.Text = messages; }
        errorGroup.IsVisible = !string.IsNullOrWhiteSpace(messages);
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
    /// <summary>Detaches transport callbacks before releasing the controller.</summary>
    /// <returns>A task completing after controller shutdown.</returns>
    private async Task DisposeCoreAsync()
    {
        System.Threading.Interlocked.Exchange(ref shuttingDown, 1);
        if (uiDispatcher.IsDispatchRequired) { uiDispatcher.Dispatch(Refresh); }
        else { Refresh(); }
        session.Media.OnMediaStateChange -= MediaStateChanged;
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
            ["Hex"] = hex.IsToggled,
            ["Echo"] = echo.IsToggled,
            ["ReceiveReply"] = sync.IsToggled,
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
        hex.IsToggled = values["Hex"]?.GetValue<bool>() ?? (hex.IsToggled);
        echo.IsToggled = values["Echo"]?.GetValue<bool>() ?? false;
        sync.IsToggled = values["ReceiveReply"]?.GetValue<bool>() ?? (sync.IsToggled);
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
        if (uiDispatcher.IsDispatchRequired)
        {
            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(SavePreferencesAsync);
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
