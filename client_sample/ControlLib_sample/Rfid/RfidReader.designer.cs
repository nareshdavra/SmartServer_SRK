namespace ControlLib.Rfid
{
    partial class RfidReader
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                releaseDevice();
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            Infragistics.Win.Appearance appearance1 = new Infragistics.Win.Appearance();
            Infragistics.Win.Appearance appearance2 = new Infragistics.Win.Appearance();
            Infragistics.Win.Appearance appearance3 = new Infragistics.Win.Appearance();
            Infragistics.Win.Appearance appearance4 = new Infragistics.Win.Appearance();
            Infragistics.Win.Appearance appearance5 = new Infragistics.Win.Appearance();
            Infragistics.Win.Appearance appearance6 = new Infragistics.Win.Appearance();
            Infragistics.Win.Appearance appearance7 = new Infragistics.Win.Appearance();
            Infragistics.Win.Appearance appearance8 = new Infragistics.Win.Appearance();
            Infragistics.Win.Appearance appearance9 = new Infragistics.Win.Appearance();
            this.PnlRfidSelection = new System.Windows.Forms.Panel();
            this.RadEth = new ModCont.cRadioButton();
            this.RadIpRfid = new ModCont.cRadioButton();
            this.RadNewRfid = new ModCont.cRadioButton();
            this.RadOldRfid = new ModCont.cRadioButton();
            this.lblMessageDisplay = new ModCont.cValueLabel();
            this.lblRfidCount = new ModCont.cValueLabel();
            this.BtnStop = new ModCont.CButton();
            this.BtnStart = new ModCont.CButton();
            this.txtRfidTagList = new ModCont.cTextBox();
            this.MnRfidCntxMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.rfidTagListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.chkSpeedRead = new System.Windows.Forms.CheckBox();
            this.txtCurrentRfidTag = new ModCont.cTextBox();
            this.lbladdress = new ModCont.cValueLabel();
            this.btnLedOff = new ModCont.CButton();
            this.PnlRfidSelection.SuspendLayout();
            this.MnRfidCntxMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // PnlRfidSelection
            // 
            this.PnlRfidSelection.Controls.Add(this.RadEth);
            this.PnlRfidSelection.Controls.Add(this.RadIpRfid);
            this.PnlRfidSelection.Controls.Add(this.RadNewRfid);
            this.PnlRfidSelection.Controls.Add(this.RadOldRfid);
            this.PnlRfidSelection.Location = new System.Drawing.Point(219, 2);
            this.PnlRfidSelection.Name = "PnlRfidSelection";
            this.PnlRfidSelection.Size = new System.Drawing.Size(96, 21);
            this.PnlRfidSelection.TabIndex = 5;
            // 
            // RadEth
            // 
            this.RadEth.AutoSize = true;
            this.RadEth.Checked = true;
            this.RadEth.ForeColor = System.Drawing.Color.SlateBlue;
            this.RadEth.ForeColorDeselect = System.Drawing.Color.Black;
            this.RadEth.ForeColorSelect = System.Drawing.Color.SlateBlue;
            this.RadEth.Location = new System.Drawing.Point(48, 2);
            this.RadEth.Name = "RadEth";
            this.RadEth.ResetProperty = false;
            this.RadEth.Size = new System.Drawing.Size(47, 17);
            this.RadEth.TabIndex = 3;
            this.RadEth.TabStop = true;
            this.RadEth.Text = "New";
            this.RadEth.UseVisualStyleBackColor = true;
            this.RadEth.Visible = false;
            this.RadEth.CheckedChanged += new System.EventHandler(this.RadEth_CheckedChanged);
            // 
            // RadIpRfid
            // 
            this.RadIpRfid.AutoSize = true;
            this.RadIpRfid.ForeColorDeselect = System.Drawing.Color.Black;
            this.RadIpRfid.ForeColorSelect = System.Drawing.Color.SlateBlue;
            this.RadIpRfid.Location = new System.Drawing.Point(113, 3);
            this.RadIpRfid.Name = "RadIpRfid";
            this.RadIpRfid.ResetProperty = false;
            this.RadIpRfid.Size = new System.Drawing.Size(47, 17);
            this.RadIpRfid.TabIndex = 2;
            this.RadIpRfid.Text = "New";
            this.RadIpRfid.UseVisualStyleBackColor = true;
            this.RadIpRfid.Visible = false;
            this.RadIpRfid.CheckedChanged += new System.EventHandler(this.RfidCheckedChanged);
            // 
            // RadNewRfid
            // 
            this.RadNewRfid.AutoSize = true;
            this.RadNewRfid.ForeColorDeselect = System.Drawing.Color.Black;
            this.RadNewRfid.ForeColorSelect = System.Drawing.Color.SlateBlue;
            this.RadNewRfid.Location = new System.Drawing.Point(48, 23);
            this.RadNewRfid.Name = "RadNewRfid";
            this.RadNewRfid.ResetProperty = false;
            this.RadNewRfid.Size = new System.Drawing.Size(47, 17);
            this.RadNewRfid.TabIndex = 1;
            this.RadNewRfid.Text = "New";
            this.RadNewRfid.UseVisualStyleBackColor = true;
            this.RadNewRfid.Visible = false;
            this.RadNewRfid.CheckedChanged += new System.EventHandler(this.RfidCheckedChanged);
            // 
            // RadOldRfid
            // 
            this.RadOldRfid.AutoSize = true;
            this.RadOldRfid.ForeColor = System.Drawing.Color.Black;
            this.RadOldRfid.ForeColorDeselect = System.Drawing.Color.Black;
            this.RadOldRfid.ForeColorSelect = System.Drawing.Color.SlateBlue;
            this.RadOldRfid.Location = new System.Drawing.Point(5, 2);
            this.RadOldRfid.Name = "RadOldRfid";
            this.RadOldRfid.ResetProperty = false;
            this.RadOldRfid.Size = new System.Drawing.Size(41, 17);
            this.RadOldRfid.TabIndex = 0;
            this.RadOldRfid.Text = "Old";
            this.RadOldRfid.UseVisualStyleBackColor = true;
            this.RadOldRfid.Visible = false;
            this.RadOldRfid.CheckedChanged += new System.EventHandler(this.RfidCheckedChanged);
            // 
            // lblMessageDisplay
            // 
            this.lblMessageDisplay.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblMessageDisplay.AutoSize = true;
            this.lblMessageDisplay.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMessageDisplay.ForeColor = System.Drawing.Color.Red;
            this.lblMessageDisplay.Format = null;
            this.lblMessageDisplay.Location = new System.Drawing.Point(3, 60);
            this.lblMessageDisplay.Name = "lblMessageDisplay";
            this.lblMessageDisplay.ResetProperty = false;
            this.lblMessageDisplay.Size = new System.Drawing.Size(134, 13);
            this.lblMessageDisplay.TabIndex = 3;
            this.lblMessageDisplay.Text = "Rfid Message Display Here";
            this.lblMessageDisplay.Visible = false;
            this.lblMessageDisplay.SizeChanged += new System.EventHandler(this.lblMessageDisplay_SizeChanged);
            // 
            // lblRfidCount
            // 
            this.lblRfidCount.BackColor = System.Drawing.Color.Gainsboro;
            this.lblRfidCount.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblRfidCount.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRfidCount.ForeColor = System.Drawing.Color.DarkGreen;
            this.lblRfidCount.Format = null;
            this.lblRfidCount.Location = new System.Drawing.Point(66, 0);
            this.lblRfidCount.Name = "lblRfidCount";
            this.lblRfidCount.ResetProperty = false;
            this.lblRfidCount.Size = new System.Drawing.Size(85, 23);
            this.lblRfidCount.TabIndex = 2;
            this.lblRfidCount.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblRfidCount.DoubleClick += new System.EventHandler(this.lblRfidCount_DoubleClick);
            // 
            // BtnStop
            // 
            appearance1.BackColor = System.Drawing.Color.LightSteelBlue;
            appearance1.FontData.BoldAsString = "True";
            appearance1.FontData.Name = "Arial";
            appearance1.FontData.SizeInPoints = 8F;
            appearance1.ForeColor = System.Drawing.Color.Black;
            appearance1.TextHAlignAsString = "Center";
            appearance1.TextVAlignAsString = "Middle";
            this.BtnStop.Appearance = appearance1;
            this.BtnStop.ButtonStyle = Infragistics.Win.UIElementButtonStyle.WindowsVistaToolbarButton;
            this.BtnStop.Enabled = false;
            this.BtnStop.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            appearance2.BorderColor = System.Drawing.Color.Transparent;
            appearance2.ForeColor = System.Drawing.Color.Black;
            this.BtnStop.HotTrackAppearance = appearance2;
            this.BtnStop.Location = new System.Drawing.Point(154, 0);
            this.BtnStop.Name = "BtnStop";
            appearance3.BackColor = System.Drawing.Color.SteelBlue;
            appearance3.BorderColor = System.Drawing.Color.White;
            appearance3.FontData.BoldAsString = "True";
            appearance3.ForeColor = System.Drawing.Color.White;
            this.BtnStop.PressedAppearance = appearance3;
            this.BtnStop.PropertyList = null;
            this.BtnStop.ResetProperty = true;
            this.BtnStop.ShowFocusRect = false;
            this.BtnStop.ShowOutline = false;
            this.BtnStop.Size = new System.Drawing.Size(62, 23);
            this.BtnStop.TabIndex = 1;
            this.BtnStop.TableName = null;
            this.BtnStop.Text = "Stop";
            this.BtnStop.UseFlatMode = Infragistics.Win.DefaultableBoolean.False;
            this.BtnStop.UseOsThemes = Infragistics.Win.DefaultableBoolean.False;
            this.BtnStop.Click += new System.EventHandler(this.BtnStop_Click);
            // 
            // BtnStart
            // 
            appearance4.BackColor = System.Drawing.Color.LightSteelBlue;
            appearance4.FontData.BoldAsString = "True";
            appearance4.FontData.Name = "Arial";
            appearance4.FontData.SizeInPoints = 8F;
            appearance4.ForeColor = System.Drawing.Color.Black;
            appearance4.TextHAlignAsString = "Center";
            appearance4.TextVAlignAsString = "Middle";
            this.BtnStart.Appearance = appearance4;
            this.BtnStart.ButtonStyle = Infragistics.Win.UIElementButtonStyle.WindowsVistaToolbarButton;
            this.BtnStart.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            appearance5.BorderColor = System.Drawing.Color.Transparent;
            appearance5.ForeColor = System.Drawing.Color.Black;
            this.BtnStart.HotTrackAppearance = appearance5;
            this.BtnStart.Location = new System.Drawing.Point(1, 0);
            this.BtnStart.Name = "BtnStart";
            appearance6.BackColor = System.Drawing.Color.SteelBlue;
            appearance6.BorderColor = System.Drawing.Color.White;
            appearance6.FontData.BoldAsString = "True";
            appearance6.ForeColor = System.Drawing.Color.White;
            this.BtnStart.PressedAppearance = appearance6;
            this.BtnStart.PropertyList = null;
            this.BtnStart.ResetProperty = true;
            this.BtnStart.ShowFocusRect = false;
            this.BtnStart.ShowOutline = false;
            this.BtnStart.Size = new System.Drawing.Size(62, 23);
            this.BtnStart.TabIndex = 0;
            this.BtnStart.TableName = null;
            this.BtnStart.Text = "Start";
            this.BtnStart.UseFlatMode = Infragistics.Win.DefaultableBoolean.False;
            this.BtnStart.UseOsThemes = Infragistics.Win.DefaultableBoolean.False;
            this.BtnStart.Click += new System.EventHandler(this.BtnStart_Click);
            // 
            // txtRfidTagList
            // 
            this.txtRfidTagList.ActivationColor = false;
            this.txtRfidTagList.Format = "";
            this.txtRfidTagList.HintColor = System.Drawing.Color.Gray;
            this.txtRfidTagList.InternalText = null;
            this.txtRfidTagList.IsPassword = false;
            this.txtRfidTagList.Location = new System.Drawing.Point(75, 2);
            this.txtRfidTagList.Multiline = true;
            this.txtRfidTagList.Name = "txtRfidTagList";
            this.txtRfidTagList.OldTextColor = System.Drawing.Color.Empty;
            this.txtRfidTagList.OriginalText = "";
            this.txtRfidTagList.RequiredChars = "";
            this.txtRfidTagList.ResetProperty = false;
            this.txtRfidTagList.Size = new System.Drawing.Size(33, 20);
            this.txtRfidTagList.TabIndex = 4;
            this.txtRfidTagList.Visible = false;
            this.txtRfidTagList.WaterText = null;
            this.txtRfidTagList.TextChanged += new System.EventHandler(this.txtRfidTagList_TextChanged);
            // 
            // MnRfidCntxMenu
            // 
            this.MnRfidCntxMenu.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.MnRfidCntxMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.rfidTagListToolStripMenuItem,
            this.clearToolStripMenuItem});
            this.MnRfidCntxMenu.Name = "MnRfidCntxMenu";
            this.MnRfidCntxMenu.Size = new System.Drawing.Size(144, 48);
            // 
            // rfidTagListToolStripMenuItem
            // 
            this.rfidTagListToolStripMenuItem.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.rfidTagListToolStripMenuItem.Name = "rfidTagListToolStripMenuItem";
            this.rfidTagListToolStripMenuItem.Size = new System.Drawing.Size(143, 22);
            this.rfidTagListToolStripMenuItem.Text = "Rfid Tag List";
            this.rfidTagListToolStripMenuItem.Click += new System.EventHandler(this.rfidTagListToolStripMenuItem_Click);
            // 
            // clearToolStripMenuItem
            // 
            this.clearToolStripMenuItem.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            this.clearToolStripMenuItem.Size = new System.Drawing.Size(143, 22);
            this.clearToolStripMenuItem.Text = "Clear";
            this.clearToolStripMenuItem.Click += new System.EventHandler(this.clearToolStripMenuItem_Click);
            // 
            // chkSpeedRead
            // 
            this.chkSpeedRead.AutoSize = true;
            this.chkSpeedRead.Location = new System.Drawing.Point(155, 26);
            this.chkSpeedRead.Name = "chkSpeedRead";
            this.chkSpeedRead.Size = new System.Drawing.Size(81, 17);
            this.chkSpeedRead.TabIndex = 6;
            this.chkSpeedRead.Text = "Speed read";
            this.chkSpeedRead.UseVisualStyleBackColor = true;
            this.chkSpeedRead.Visible = false;
            this.chkSpeedRead.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // txtCurrentRfidTag
            // 
            this.txtCurrentRfidTag.ActivationColor = false;
            this.txtCurrentRfidTag.BackColor = System.Drawing.Color.White;
            this.txtCurrentRfidTag.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtCurrentRfidTag.Font = new System.Drawing.Font("Arial", 8F);
            this.txtCurrentRfidTag.ForeColor = System.Drawing.Color.Black;
            this.txtCurrentRfidTag.Format = "";
            this.txtCurrentRfidTag.HintColor = System.Drawing.Color.Gray;
            this.txtCurrentRfidTag.InternalText = null;
            this.txtCurrentRfidTag.IsPassword = false;
            this.txtCurrentRfidTag.Location = new System.Drawing.Point(239, 28);
            this.txtCurrentRfidTag.Multiline = true;
            this.txtCurrentRfidTag.Name = "txtCurrentRfidTag";
            this.txtCurrentRfidTag.OldTextColor = System.Drawing.Color.Black;
            this.txtCurrentRfidTag.OriginalText = "";
            this.txtCurrentRfidTag.RequiredChars = "";
            this.txtCurrentRfidTag.ResetProperty = true;
            this.txtCurrentRfidTag.Size = new System.Drawing.Size(60, 20);
            this.txtCurrentRfidTag.TabIndex = 63;
            this.txtCurrentRfidTag.Visible = false;
            this.txtCurrentRfidTag.WaterText = "";
            // 
            // lbladdress
            // 
            this.lbladdress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbladdress.AutoSize = true;
            this.lbladdress.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbladdress.ForeColor = System.Drawing.Color.Red;
            this.lbladdress.Format = null;
            this.lbladdress.Location = new System.Drawing.Point(3, 30);
            this.lbladdress.Name = "lbladdress";
            this.lbladdress.ResetProperty = false;
            this.lbladdress.Size = new System.Drawing.Size(45, 13);
            this.lbladdress.TabIndex = 64;
            this.lbladdress.Text = "address";
            // 
            // btnLedOff
            // 
            appearance7.BackColor = System.Drawing.Color.LightSteelBlue;
            appearance7.FontData.BoldAsString = "True";
            appearance7.FontData.Name = "Arial";
            appearance7.FontData.SizeInPoints = 8F;
            appearance7.ForeColor = System.Drawing.Color.Black;
            appearance7.TextHAlignAsString = "Center";
            appearance7.TextVAlignAsString = "Middle";
            this.btnLedOff.Appearance = appearance7;
            this.btnLedOff.ButtonStyle = Infragistics.Win.UIElementButtonStyle.WindowsVistaToolbarButton;
            this.btnLedOff.Enabled = false;
            this.btnLedOff.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            appearance8.BorderColor = System.Drawing.Color.Transparent;
            appearance8.ForeColor = System.Drawing.Color.Black;
            this.btnLedOff.HotTrackAppearance = appearance8;
            this.btnLedOff.Location = new System.Drawing.Point(154, 26);
            this.btnLedOff.Name = "btnLedOff";
            appearance9.BackColor = System.Drawing.Color.SteelBlue;
            appearance9.BorderColor = System.Drawing.Color.White;
            appearance9.FontData.BoldAsString = "True";
            appearance9.ForeColor = System.Drawing.Color.White;
            this.btnLedOff.PressedAppearance = appearance9;
            this.btnLedOff.PropertyList = null;
            this.btnLedOff.ResetProperty = true;
            this.btnLedOff.ShowFocusRect = false;
            this.btnLedOff.ShowOutline = false;
            this.btnLedOff.Size = new System.Drawing.Size(62, 23);
            this.btnLedOff.TabIndex = 65;
            this.btnLedOff.TableName = null;
            this.btnLedOff.Text = "off";
            this.btnLedOff.UseFlatMode = Infragistics.Win.DefaultableBoolean.False;
            this.btnLedOff.UseOsThemes = Infragistics.Win.DefaultableBoolean.False;
            this.btnLedOff.Click += new System.EventHandler(this.btnLedOff_Click);
            // 
            // RfidReader
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.btnLedOff);
            this.Controls.Add(this.lbladdress);
            this.Controls.Add(this.txtCurrentRfidTag);
            this.Controls.Add(this.chkSpeedRead);
            this.Controls.Add(this.lblMessageDisplay);
            this.Controls.Add(this.lblRfidCount);
            this.Controls.Add(this.BtnStop);
            this.Controls.Add(this.BtnStart);
            this.Controls.Add(this.txtRfidTagList);
            this.Controls.Add(this.PnlRfidSelection);
            this.Name = "RfidReader";
            this.Size = new System.Drawing.Size(315, 50);
            this.Load += new System.EventHandler(this.RfidReader_Load);
            this.Click += new System.EventHandler(this.RfidReader_Click);
            this.PnlRfidSelection.ResumeLayout(false);
            this.PnlRfidSelection.PerformLayout();
            this.MnRfidCntxMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ModCont.CButton BtnStop;
        private ModCont.cValueLabel lblRfidCount;
        public ModCont.CButton BtnStart;
        private System.Windows.Forms.Panel PnlRfidSelection;
        private ModCont.cRadioButton RadNewRfid;
        private ModCont.cRadioButton RadOldRfid;
        private ModCont.cRadioButton RadIpRfid;
        public ModCont.cTextBox txtRfidTagList;
        private System.Windows.Forms.ContextMenuStrip MnRfidCntxMenu;
        private System.Windows.Forms.ToolStripMenuItem rfidTagListToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
        private ModCont.cRadioButton RadEth;
        private System.Windows.Forms.CheckBox chkSpeedRead;
        public ModCont.cTextBox txtCurrentRfidTag;
        public ModCont.cValueLabel lblMessageDisplay;
        public ModCont.cValueLabel lbladdress;
        public ModCont.CButton btnLedOff;


    }
}
