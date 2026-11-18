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
using Microsoft.Maui.Controls;
using UiResources = Gurux.Medias.Maui.Properties.ExampleSettingsResources;

namespace Gurux.Net.Example.Maui;

/// <summary>Displays network controls and owns their asynchronous connection lifetime.</summary>
public sealed class MediaExamplePage : ContentPage, IAsyncDisposable
{
    /// <summary>The network connection owned by this page.</summary>
    private readonly ExampleSession _session = new();
    /// <summary>The compact network control panel.</summary>
    private readonly NetworkExamplePanel _networkPanel;
    /// <summary>Cancels pending settings requests when the page is destroyed.</summary>
    private readonly CancellationTokenSource _lifetime = new();
    /// <summary>Updates the panel on the native UI dispatcher.</summary>
    private readonly Microsoft.Maui.Dispatching.IDispatcherTimer _timer;
    /// <summary>Prevents overlapping native navigation sheets.</summary>
    private bool _navigationShowing;
    /// <summary>Shares a single shutdown operation across repeated requests.</summary>
    private Task? _disposal;

    /// <summary>Creates the network panel and starts transport state updates.</summary>
    public MediaExamplePage()
    {
        Title = ExampleResources.ApplicationTitle;
        ExampleLayout.Apply(this);
        _networkPanel = new NetworkExamplePanel(_session,
            () => new MauiMediaSettingsEditor(this).ShowAsync(_session.Media, cancellationToken: _lifetime.Token));
        Content = _networkPanel;
#if WINDOWS
        var connection = new MenuBarItem { Text = UiResources.ConnectionGroup };
        connection.Add(new MenuFlyoutItem { Text = ExampleResources.Open, Command = _networkPanel.CreateCommand("Open") });
        connection.Add(new MenuFlyoutItem { Text = ExampleResources.Close, Command = _networkPanel.CreateCommand("Close") });
        var view = new MenuBarItem { Text = UiResources.ViewMenu };
        view.Add(MenuCommand(UiResources.PayloadLabel, _networkPanel.ShowData));
        view.Add(MenuCommand(UiResources.ActivityGroup, _networkPanel.ShowActivity));
        var settings = new MenuBarItem { Text = UiResources.SettingsMenu };
        settings.Add(new MenuFlyoutItem { Text = UiResources.MediaSettings, Command = _networkPanel.CreateCommand("Settings") });
        settings.Add(MenuCommand(UiResources.CommunicationSettings, _networkPanel.ShowCommunication));
        MenuBarItems.Add(connection); MenuBarItems.Add(view); MenuBarItems.Add(settings);
#else
        var navigation = new ToolbarItem { Text = UiResources.NavigationMenu, AutomationId = "NavigationMenu" };
        if (OperatingSystem.IsAndroid())
        {
            var icon = new FontImageSource { Glyph = "\u2630", FontFamily = "sans-serif", Size = 24 };
            icon.SetAppThemeColor(FontImageSource.ColorProperty, Microsoft.Maui.Graphics.Colors.Black, Microsoft.Maui.Graphics.Colors.White);
            navigation.IconImageSource = icon;
            navigation.Order = ToolbarItemOrder.Primary;
            navigation.Text = string.Empty;
            SemanticProperties.SetDescription(navigation, UiResources.NavigationMenu);
        }
        navigation.Clicked += async (_, _) => await ShowNavigationAsync();
        ToolbarItems.Add(navigation);
#endif
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(200);
        _timer.Tick += (_, _) => _networkPanel.Refresh();
        _timer.Start();
        _networkPanel.Refresh();
    }

    /// <summary>Creates a native menu command that respects view ownership.</summary>
    /// <param name="caption">The localized caption.</param>
    /// <param name="action">The guarded navigation or lifecycle action.</param>
    /// <returns>The native menu item.</returns>
    private MenuFlyoutItem MenuCommand(string caption, Action action)
    {
        return new MenuFlyoutItem { Text = caption, Command = new Command(() => { if (_disposal is null && _networkPanel.CanNavigate) { action(); } }) };
    }
    /// <summary>Displays native mobile navigation without replacing the session controls.</summary>
    /// <returns>A task completing after a selection or cancellation.</returns>
    private async Task ShowNavigationAsync()
    {
        if (_navigationShowing || _disposal is not null || !_networkPanel.CanNavigate || Navigation.ModalStack.Count != 0) { return; }
        _navigationShowing = true;
        try
        {
            var choices = new List<string> { UiResources.PayloadLabel };
            if (_networkPanel.CanEditSettings) { choices.Add(UiResources.MediaSettings); }
            choices.Add(UiResources.CommunicationSettings);
            choices.Add(UiResources.ActivityGroup);
            string? selected = await DisplayActionSheetAsync(UiResources.NavigationMenu, UiResources.CancelNavigation, null, choices.ToArray());
            if (_disposal is not null || !_networkPanel.CanNavigate || Navigation.ModalStack.Count != 0) { return; }
            if (selected == UiResources.PayloadLabel) { _networkPanel.ShowData(); }
            else if (selected == UiResources.MediaSettings && _networkPanel.CanEditSettings) { _networkPanel.InvokeCommand("Settings"); }
            else if (selected == UiResources.CommunicationSettings) { _networkPanel.ShowCommunication(); }
            else if (selected == UiResources.ActivityGroup) { _networkPanel.ShowActivity(); }
        }
        catch (OperationCanceledException ex)
        {
            if (_disposal is null) { _networkPanel.ReportNavigationError(ex); }
        }
        catch (Exception ex)
        {
            if (_disposal is null) { _networkPanel.ReportNavigationError(ex); }
        }
        finally { _navigationShowing = false; }
    }

    /// <summary>Completes cleanup when the owning native window is destroyed.</summary>
    /// <returns>A task completing after connection cleanup.</returns>
    public async Task ShutdownAsync() => await DisposeAsync();

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => new(_disposal ??= DisposeCoreAsync());

    /// <summary>Stops updates and releases network and settings resources.</summary>
    /// <returns>A task completing after all resources are released.</returns>
    private async Task DisposeCoreAsync()
    {
        _timer.Stop();
        _lifetime.Cancel();
        await _networkPanel.DisposeAsync();
        await _session.DisposeAsync();
        _lifetime.Dispose();
    }
}