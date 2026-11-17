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
using Avalonia;
using Avalonia.Controls;
using SettingsResources = Gurux.Net.Avalonia.Properties.Resources;
using Gurux.Net.UI;

namespace Gurux.Net.Avalonia;

/// <summary>Edits a remote host or selects a local bind address without losing either value.</summary>
internal sealed class HostAddressControl : StackPanel
{
    /// <summary>Owns the staged addresses and interface snapshot.</summary>
    private readonly HostAddressState _state;
    /// <summary>Describes the address according to the selected connection mode.</summary>
    private readonly TextBlock _label = new();
    /// <summary>Lists all interfaces, loopback, local addresses and manual entry.</summary>
    private readonly ComboBox _choices = new() { Name = "ListenAddressChoices", HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch };
    /// <summary>Accepts a remote host or manually entered local address.</summary>
    private readonly TextBox _text = new() { Name = "HostName" };
    /// <summary>Stores the values corresponding to the visible choices.</summary>
    private List<(string? Address, string Caption)> _options = [];
    /// <summary>Prevents programmatic refreshes from being treated as user changes.</summary>
    private bool _refreshing;

    /// <summary>Creates the native address editor.</summary>
    /// <param name="state">The staged address values and IP family.</param>
    internal HostAddressControl(HostAddressState state)
    {
        _state = state;
        Spacing = 8;
        Children.Add(_label);
        Children.Add(_choices);
        Children.Add(_text);
        _choices.SelectionChanged += (_, _) =>
        {
            if (_refreshing || _choices.SelectedIndex < 0) { return; }
            var address = _options[_choices.SelectedIndex].Address;
            if (address is not null) { _state.Host = address; Refresh(); }
            else { _text.Focus(); }
        };
        _text.PropertyChanged += (_, args) =>
        {
            if (!_refreshing && args.Property == TextBox.TextProperty)
            {
                _state.Host = _text.Text ?? "";
                Refresh();
            }
        };
        Refresh();
    }

    /// <summary>Updates labels and available IP addresses after server or IPv6 changes.</summary>
    internal void Refresh()
    {
        _refreshing = true;
        try
        {
            _label.Text = SettingsResources.Get(_state.Server ? "ListenAddress" : "Host");
            global::Avalonia.Automation.AutomationProperties.SetName(_text, _label.Text);
            global::Avalonia.Automation.AutomationProperties.SetName(_choices, SettingsResources.Get("ListenAddress"));
            _choices.IsVisible = _state.Server;
            if (_state.Server)
            {
                _options = _state.GetOptions(SettingsResources.Get("AllInterfaces"), SettingsResources.Get("LoopbackOnly"), SettingsResources.Get("CustomAddress"), SettingsResources.Get("InterfaceAddress"));
                _choices.ItemsSource = _options.Select(option => option.Caption).ToArray();
                int selected = _options.FindIndex(option => option.Address == _state.Host);
                _choices.SelectedIndex = selected >= 0 ? selected : _options.Count - 1;
            }
            _text.Text = _state.Host;
        }
        finally { _refreshing = false; }
    }
}
