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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Gurux.Common;
using System.Windows.Forms;

namespace Gurux.Net
{
    partial class Settings : Form, IGXPropertyPage
    {
        GXNet Target;
        public Settings(GXNet target)
        {
            Target = target;
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
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
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
        #endregion

        private void ServerCB_CheckedChanged(object sender, EventArgs e)
        {
            UseIPv6CB.Enabled = ServerCB.Checked;
            IPAddressTB.Enabled = !ServerCB.Checked;
        }
        
        #region IGXPropertyPage Members

        void IGXPropertyPage.Initialize()
        {
            this.ServerCB.Text = Gurux.Net.Resources.ServerTxt;
            this.IPAddressLbl.Text = Gurux.Net.Resources.HostNameTxt;
            this.PortLbl.Text = Gurux.Net.Resources.PortTxt;
            this.ProtocolLbl.Text = Gurux.Net.Resources.ProtocolTxt;
			this.UseIPv6CB.Text = Gurux.Net.Resources.UseIPv6Txt;
            ProtocolCB.Items.Add(NetworkType.Tcp);
            ProtocolCB.Items.Add(NetworkType.Udp);
            this.ServerCB.Checked = Target.Server;
            this.PortTB.Text = Target.Port.ToString();
            this.IPAddressTB.Text = Target.HostName;
            ProtocolCB.SelectedItem = Target.Protocol;
			this.UseIPv6CB.Checked = Target.UseIPv6;
            //Hide controls which user do not want to show.
            HostPanel.Enabled = (Target.ConfigurableSettings & AvailableMediaSettings.Host) != 0;
            PortPanel.Enabled = (Target.ConfigurableSettings & AvailableMediaSettings.Port) != 0;
            ProtocolPanel.Enabled = (Target.ConfigurableSettings & AvailableMediaSettings.Protocol) != 0;
            ServerPanel.Enabled = (Target.ConfigurableSettings & AvailableMediaSettings.Server) != 0;
			UseIPv6Panel.Enabled = (Target.ConfigurableSettings & AvailableMediaSettings.UseIPv6) != 0;   
        }

        void IGXPropertyPage.Apply()
        {
            Target.Server = this.ServerCB.Checked;
            Target.Port = Convert.ToInt32(this.PortTB.Text);
            Target.HostName = this.IPAddressTB.Text;
			Target.UseIPv6 = this.UseIPv6CB.Checked;
        }

        #endregion
    }
}
