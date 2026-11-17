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
using Microsoft.Maui.Controls;
using SettingsResources = Gurux.Net.Maui.Properties.Resources;
using Gurux.Net.UI;

namespace Gurux.Net.Maui;

/// <summary>Edits remote and local addresses using the same native controls on desktop and mobile.</summary>
internal sealed class HostAddressControl : VerticalStackLayout
{
    /// <summary>Owns the staged addresses and interface snapshot.</summary>
    private readonly HostAddressState _state;
    /// <summary>Describes the address for the selected connection mode.</summary>
    private readonly Label _label = new();
    /// <summary>Lists local bind choices for the selected IP family.</summary>
    private readonly Picker _choices = new() { AutomationId = "ListenAddressChoices" };
    /// <summary>Accepts a remote host or manually entered local address.</summary>
    private readonly Entry _text = new() { AutomationId = "HostName" };
    /// <summary>Stores the addresses corresponding to the visible choices.</summary>
    private List<(string? Address, string Caption)> _options = [];
    /// <summary>Prevents programmatic refreshes from overwriting staged values.</summary>
    private bool _refreshing;

    /// <summary>Creates the native address editor.</summary>
    /// <param name="state">The staged addresses and IP family.</param>
    internal HostAddressControl(HostAddressState state)
    {
        _state = state;
        Spacing = 8;
        Children.Add(_label);
        Children.Add(_choices);
        Children.Add(_text);
        _choices.SelectedIndexChanged += (_, _) =>
        {
            if (_refreshing || _choices.SelectedIndex < 0) { return; }
            var address = _options[_choices.SelectedIndex].Address;
            if (address is not null) { _state.Host = address; Refresh(); }
            else { _text.Focus(); }
        };
        _text.TextChanged += (_, args) =>
        {
            if (!_refreshing) { _state.Host = args.NewTextValue ?? ""; Refresh(); }
        };
        Refresh();
    }

    /// <summary>Updates captions and local addresses after a server or IP family change.</summary>
    internal void Refresh()
    {
        _refreshing = true;
        try
        {
            _label.Text = _state.Server ? SettingsResources.ListenAddress : SettingsResources.Host;
            SemanticProperties.SetDescription(_text, _label.Text);
            SemanticProperties.SetDescription(_choices, SettingsResources.ListenAddress);
            _choices.IsVisible = _state.Server;
            if (_state.Server)
            {
                _options = _state.GetOptions(SettingsResources.AllInterfaces, SettingsResources.LoopbackOnly, SettingsResources.CustomAddress, SettingsResources.InterfaceAddress);
                _choices.ItemsSource = _options.Select(option => option.Caption).ToArray();
                int selected = _options.FindIndex(option => option.Address == _state.Host);
                _choices.SelectedIndex = selected >= 0 ? selected : _options.Count - 1;
            }
            _text.Text = _state.Host;
        }
        finally { _refreshing = false; }
    }
}
