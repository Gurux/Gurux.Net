namespace Gurux.Net
{
partial class Settings
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

#region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
            this.ServerCB = new System.Windows.Forms.CheckBox();
            this.ServerPanel = new System.Windows.Forms.Panel();
            this.UseIPv6Panel = new System.Windows.Forms.Panel();
            this.UseIPv6CB = new System.Windows.Forms.CheckBox();
            this.ProtocolPanel = new System.Windows.Forms.Panel();
            this.ProtocolLbl = new System.Windows.Forms.Label();
            this.ProtocolCB = new System.Windows.Forms.ComboBox();
            this.PortPanel = new System.Windows.Forms.Panel();
            this.PortTB = new System.Windows.Forms.TextBox();
            this.PortLbl = new System.Windows.Forms.Label();
            this.HostPanel = new System.Windows.Forms.Panel();
            this.IPAddressTB = new System.Windows.Forms.TextBox();
            this.IPAddressLbl = new System.Windows.Forms.Label();
            this.ServerPanel.SuspendLayout();
            this.UseIPv6Panel.SuspendLayout();
            this.ProtocolPanel.SuspendLayout();
            this.PortPanel.SuspendLayout();
            this.HostPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // ServerCB
            // 
            this.ServerCB.AutoSize = true;
            this.ServerCB.Location = new System.Drawing.Point(4, 7);
            this.ServerCB.Name = "ServerCB";
            this.ServerCB.Size = new System.Drawing.Size(64, 17);
            this.ServerCB.TabIndex = 4;
            this.ServerCB.Text = "ServerX";
            this.ServerCB.UseVisualStyleBackColor = true;
            this.ServerCB.CheckedChanged += new System.EventHandler(this.ServerCB_CheckedChanged);
            // 
            // ServerPanel
            // 
            this.ServerPanel.Controls.Add(this.ServerCB);
            this.ServerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.ServerPanel.Location = new System.Drawing.Point(0, 0);
            this.ServerPanel.Name = "ServerPanel";
            this.ServerPanel.Size = new System.Drawing.Size(235, 30);
            this.ServerPanel.TabIndex = 15;
            // 
            // UseIPv6Panel
            // 
            this.UseIPv6Panel.Controls.Add(this.UseIPv6CB);
            this.UseIPv6Panel.Dock = System.Windows.Forms.DockStyle.Top;
            this.UseIPv6Panel.Location = new System.Drawing.Point(0, 30);
            this.UseIPv6Panel.Name = "UseIPv6Panel";
            this.UseIPv6Panel.Size = new System.Drawing.Size(235, 30);
            this.UseIPv6Panel.TabIndex = 20;
            // 
            // UseIPv6CB
            // 
            this.UseIPv6CB.AutoSize = true;
            this.UseIPv6CB.Enabled = false;
            this.UseIPv6CB.Location = new System.Drawing.Point(4, 7);
            this.UseIPv6CB.Name = "UseIPv6CB";
            this.UseIPv6CB.Size = new System.Drawing.Size(74, 17);
            this.UseIPv6CB.TabIndex = 4;
            this.UseIPv6CB.Text = "UseIPv6X";
            this.UseIPv6CB.UseVisualStyleBackColor = true;
            this.UseIPv6CB.CheckedChanged += new System.EventHandler(this.UseIPv6CB_CheckedChanged);
            // 
            // ProtocolPanel
            // 
            this.ProtocolPanel.Controls.Add(this.ProtocolLbl);
            this.ProtocolPanel.Controls.Add(this.ProtocolCB);
            this.ProtocolPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.ProtocolPanel.Location = new System.Drawing.Point(0, 120);
            this.ProtocolPanel.Name = "ProtocolPanel";
            this.ProtocolPanel.Size = new System.Drawing.Size(235, 30);
            this.ProtocolPanel.TabIndex = 23;
            // 
            // ProtocolLbl
            // 
            this.ProtocolLbl.AutoSize = true;
            this.ProtocolLbl.Location = new System.Drawing.Point(4, 7);
            this.ProtocolLbl.Name = "ProtocolLbl";
            this.ProtocolLbl.Size = new System.Drawing.Size(53, 13);
            this.ProtocolLbl.TabIndex = 15;
            this.ProtocolLbl.Text = "ProtocolX";
            // 
            // ProtocolCB
            // 
            this.ProtocolCB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ProtocolCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ProtocolCB.FormattingEnabled = true;
            this.ProtocolCB.Location = new System.Drawing.Point(78, 4);
            this.ProtocolCB.Name = "ProtocolCB";
            this.ProtocolCB.Size = new System.Drawing.Size(145, 21);
            this.ProtocolCB.TabIndex = 14;
            this.ProtocolCB.SelectedIndexChanged += new System.EventHandler(this.ProtocolCB_SelectedIndexChanged);
            // 
            // PortPanel
            // 
            this.PortPanel.Controls.Add(this.PortTB);
            this.PortPanel.Controls.Add(this.PortLbl);
            this.PortPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.PortPanel.Location = new System.Drawing.Point(0, 90);
            this.PortPanel.Name = "PortPanel";
            this.PortPanel.Size = new System.Drawing.Size(235, 30);
            this.PortPanel.TabIndex = 22;
            // 
            // PortTB
            // 
            this.PortTB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PortTB.Location = new System.Drawing.Point(78, 5);
            this.PortTB.Name = "PortTB";
            this.PortTB.Size = new System.Drawing.Size(145, 20);
            this.PortTB.TabIndex = 11;
            this.PortTB.TextChanged += new System.EventHandler(this.PortTB_TextChanged);
            // 
            // PortLbl
            // 
            this.PortLbl.AutoSize = true;
            this.PortLbl.Location = new System.Drawing.Point(4, 7);
            this.PortLbl.Name = "PortLbl";
            this.PortLbl.Size = new System.Drawing.Size(33, 13);
            this.PortLbl.TabIndex = 12;
            this.PortLbl.Text = "PortX";
            // 
            // HostPanel
            // 
            this.HostPanel.Controls.Add(this.IPAddressTB);
            this.HostPanel.Controls.Add(this.IPAddressLbl);
            this.HostPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.HostPanel.Location = new System.Drawing.Point(0, 60);
            this.HostPanel.Name = "HostPanel";
            this.HostPanel.Size = new System.Drawing.Size(235, 30);
            this.HostPanel.TabIndex = 21;
            // 
            // IPAddressTB
            // 
            this.IPAddressTB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.IPAddressTB.Location = new System.Drawing.Point(78, 4);
            this.IPAddressTB.Name = "IPAddressTB";
            this.IPAddressTB.Size = new System.Drawing.Size(145, 20);
            this.IPAddressTB.TabIndex = 9;
            this.IPAddressTB.TextChanged += new System.EventHandler(this.IPAddressTB_TextChanged);
            // 
            // IPAddressLbl
            // 
            this.IPAddressLbl.AutoSize = true;
            this.IPAddressLbl.Location = new System.Drawing.Point(4, 7);
            this.IPAddressLbl.Name = "IPAddressLbl";
            this.IPAddressLbl.Size = new System.Drawing.Size(65, 13);
            this.IPAddressLbl.TabIndex = 10;
            this.IPAddressLbl.Text = "IP AddressX";
            // 
            // Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(235, 152);
            this.Controls.Add(this.ProtocolPanel);
            this.Controls.Add(this.PortPanel);
            this.Controls.Add(this.HostPanel);
            this.Controls.Add(this.UseIPv6Panel);
            this.Controls.Add(this.ServerPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Settings";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Network SettingsX";
            this.ServerPanel.ResumeLayout(false);
            this.ServerPanel.PerformLayout();
            this.UseIPv6Panel.ResumeLayout(false);
            this.UseIPv6Panel.PerformLayout();
            this.ProtocolPanel.ResumeLayout(false);
            this.ProtocolPanel.PerformLayout();
            this.PortPanel.ResumeLayout(false);
            this.PortPanel.PerformLayout();
            this.HostPanel.ResumeLayout(false);
            this.HostPanel.PerformLayout();
            this.ResumeLayout(false);

    }

#endregion

    private System.Windows.Forms.CheckBox ServerCB;
    private System.Windows.Forms.Panel ServerPanel;
    private System.Windows.Forms.Panel UseIPv6Panel;
    private System.Windows.Forms.CheckBox UseIPv6CB;
    private System.Windows.Forms.Panel ProtocolPanel;
    private System.Windows.Forms.Label ProtocolLbl;
    private System.Windows.Forms.ComboBox ProtocolCB;
    private System.Windows.Forms.Panel PortPanel;
    private System.Windows.Forms.TextBox PortTB;
    private System.Windows.Forms.Label PortLbl;
    private System.Windows.Forms.Panel HostPanel;
    private System.Windows.Forms.TextBox IPAddressTB;
    private System.Windows.Forms.Label IPAddressLbl;


}
}
