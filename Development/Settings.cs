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
using System.Reflection;
using Gurux.Common;
using System.Windows.Forms;
using Gurux.Net.Properties;

namespace Gurux.Net
{
    partial class Settings : Form, IGXPropertyPage, INotifyPropertyChanged
    {
        GXNet target;
        PropertyChangedEventHandler propertyChanged;

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
            this.target = target;
            InitializeComponent();
        }

#region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
#if NET46 || NET6_0// !NETSTANDARD2_0 && !NETSTANDARD2_1 && !NET6_0
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
#else
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.Location);
#endif            
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }

        public bool Dirty
        {
            get;
            set;
        }
#endregion

        private void ServerCB_CheckedChanged(object sender, EventArgs e)
        {
            Dirty = true;
            UseIPv6CB.Enabled = ServerCB.Checked;
            IPAddressTB.Enabled = !ServerCB.Checked;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("Server"));
            }
        }

#region IGXPropertyPage Members

        void IGXPropertyPage.Initialize()
        {
            this.ServerCB.Text = Resources.ServerTxt;
            this.IPAddressLbl.Text = Resources.HostNameTxt;
            this.PortLbl.Text = Resources.PortTxt;
            this.ProtocolLbl.Text = Resources.ProtocolTxt;
            this.UseIPv6CB.Text = Resources.UseIPv6Txt;
            ProtocolCB.Items.Add(NetworkType.Tcp);
            ProtocolCB.Items.Add(NetworkType.Udp);
            this.ServerCB.Checked = target.Server;
            this.PortTB.Text = target.Port.ToString();
            this.IPAddressTB.Text = target.HostName;
            ProtocolCB.SelectedItem = target.Protocol;
            this.UseIPv6CB.Checked = target.UseIPv6;
            //Hide controls which user do not want to show.
            HostPanel.Enabled = (target.ConfigurableSettings & AvailableMediaSettings.Host) != 0;
            PortPanel.Enabled = (target.ConfigurableSettings & AvailableMediaSettings.Port) != 0;
            ProtocolPanel.Enabled = (target.ConfigurableSettings & AvailableMediaSettings.Protocol) != 0;
            ServerPanel.Enabled = (target.ConfigurableSettings & AvailableMediaSettings.Server) != 0;
            UseIPv6Panel.Enabled = (target.ConfigurableSettings & AvailableMediaSettings.UseIPv6) != 0;
            UpdateEditBoxSizes();
            Dirty = false;
        }

        /// <summary>
        /// Because label lenght depends from the localization string, edit box sizes must be update.
        /// </summary>
        private void UpdateEditBoxSizes()
        {
            //Find max length of the localization string.
            int maxLength = 0;
            foreach (Control it in this.Controls)
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
            foreach (Control it in this.Controls)
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
            target.Server = this.ServerCB.Checked;
            target.Port = Convert.ToInt32(this.PortTB.Text);
            target.HostName = this.IPAddressTB.Text;
            target.UseIPv6 = this.UseIPv6CB.Checked;
            target.Protocol = (NetworkType)ProtocolCB.SelectedItem;
            Dirty = false;
        }

#endregion

        private void UseIPv6CB_CheckedChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("UseIPv6"));
            }
        }

        private void IPAddressTB_TextChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("IPAddress"));
            }
        }

        private void PortTB_TextChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("Port"));
            }
        }

        private void ProtocolCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirty = true;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs("Protocol"));
            }
        }
    }
}