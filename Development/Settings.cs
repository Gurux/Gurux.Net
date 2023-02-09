//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using System;
using System.ComponentModel;
using Gurux.Common;
using System.Windows.Forms;
using Gurux.Net.Properties;

namespace Gurux.Net
{
    partial class Settings : Form, IGXPropertyPage, INotifyPropertyChanged
    {
        private bool _initialize = true;
        private GXNet _target;
        private PropertyChangedEventHandler propertyChanged;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                propertyChanged += value;
            }
            remove
            {
                propertyChanged -= value;
            }
        }

        public Settings(GXNet target)
        {
            _target = target;
            InitializeComponent();
        }

        public bool Dirty
        {
            get;
            set;
        }

        #region IGXPropertyPage Members

        void IGXPropertyPage.Initialize()
        {
            ServerCB.Text = Resources.ServerTxt;
            IPAddressLbl.Text = Resources.HostNameTxt;
            PortLbl.Text = Resources.PortTxt;
            ProtocolLbl.Text = Resources.ProtocolTxt;
            UseIPv6CB.Text = Resources.UseIPv6Txt;
            ProtocolCB.Items.Add(NetworkType.Tcp);
            ProtocolCB.Items.Add(NetworkType.Udp);
            ServerCB.Checked = _target.Server;
            PortTB.Text = _target.Port.ToString();
            IPAddressTB.Text = _target.HostName;
            ProtocolCB.SelectedItem = _target.Protocol;
            UseIPv6CB.Checked = _target.UseIPv6;
            //Hide controls which user do not want to show.
            HostPanel.Enabled = (_target.ConfigurableSettings & AvailableMediaSettings.Host) != 0;
            PortPanel.Enabled = (_target.ConfigurableSettings & AvailableMediaSettings.Port) != 0;
            ProtocolPanel.Enabled = (_target.ConfigurableSettings & AvailableMediaSettings.Protocol) != 0;
            ServerPanel.Enabled = (_target.ConfigurableSettings & AvailableMediaSettings.Server) != 0;
            UseIPv6Panel.Enabled = (_target.ConfigurableSettings & AvailableMediaSettings.UseIPv6) != 0;
            UpdateEditBoxSizes();
            Dirty = false;
            _initialize = false;
        }

        /// <summary>
        /// Because label lenght depends from the localization string, edit box sizes must be update.
        /// </summary>
        private void UpdateEditBoxSizes()
        {
            //Find max length of the localization string.
            int maxLength = 0;
            foreach (Control it in Controls)
            {
                if (it.Enabled)
                {
                    foreach (Control it2 in it.Controls)
                    {
                        if (it2 is Label && it2.Right > maxLength)
                        {
                            maxLength = it2.Right;
                        }
                    }
                }
            }
            //Increase edit control length.
            foreach (Control it in Controls)
            {
                if (it.Enabled)
                {
                    foreach (Control it2 in it.Controls)
                    {
                        if (it2 is ComboBox || it2 is TextBox)
                        {
                            it2.Width += it2.Left - maxLength - 10;
                            it2.Left = maxLength + 10;
                        }
                    }
                }
            }
        }

        void IGXPropertyPage.Apply()
        {
            _target.Server = ServerCB.Checked;
            _target.Port = Convert.ToInt32(PortTB.Text);
            _target.HostName = IPAddressTB.Text;
            _target.UseIPv6 = UseIPv6CB.Checked;
            _target.Protocol = (NetworkType)ProtocolCB.SelectedItem;
            Dirty = false;
        }

        #endregion

        private void ServerCB_CheckedChanged(object sender, EventArgs e)
        {
            Dirty = true;
            UseIPv6CB.Enabled = ServerCB.Checked;
            IPAddressTB.Enabled = !ServerCB.Checked;
            if (!_initialize)
            {
                _target.Server = ServerCB.Checked;
            }
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("Server"));
            }
        }

        private void UseIPv6CB_CheckedChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (!_initialize)
            {
                _target.UseIPv6 = UseIPv6CB.Checked;
            }
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("UseIPv6"));
            }
        }

        private void IPAddressTB_TextChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (!_initialize)
            {
                _target.HostName = IPAddressTB.Text;
            }
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("IPAddress"));
            }
        }

        private void PortTB_TextChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (!_initialize)
            {
                _target.Port = Convert.ToInt32(PortTB.Text);
            }
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("Port"));
            }
        }

        private void ProtocolCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (!_initialize)
            {
                _target.Protocol = (NetworkType)ProtocolCB.SelectedItem;
            }
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("Protocol"));
            }
        }
    }
}