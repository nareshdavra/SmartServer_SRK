namespace Controltest
{
    partial class Form2
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
            this.txtResult = new System.Windows.Forms.TextBox();
            this.ultraButton1 = new Infragistics.Win.Misc.UltraButton();
            this.ultraTextEditor1 = new Infragistics.Win.UltraWinEditors.UltraTextEditor();
            this.rfidReader1 = new ControlLib.Rfid.RfidReader();
            ((System.ComponentModel.ISupportInitialize)(this.ultraTextEditor1)).BeginInit();
            this.SuspendLayout();
            // 
            // txtResult
            // 
            this.txtResult.Location = new System.Drawing.Point(364, 12);
            this.txtResult.Name = "txtResult";
            this.txtResult.Size = new System.Drawing.Size(42, 20);
            this.txtResult.TabIndex = 1;
            this.txtResult.TextChanged += new System.EventHandler(this.txtResult_TextChanged);
            // 
            // ultraButton1
            // 
            this.ultraButton1.Location = new System.Drawing.Point(255, 12);
            this.ultraButton1.Name = "ultraButton1";
            this.ultraButton1.Size = new System.Drawing.Size(75, 23);
            this.ultraButton1.TabIndex = 9;
            this.ultraButton1.Text = "SetLed";
            this.ultraButton1.Click += new System.EventHandler(this.ultraButton1_Click);
            // 
            // ultraTextEditor1
            // 
            this.ultraTextEditor1.Location = new System.Drawing.Point(13, 62);
            this.ultraTextEditor1.Multiline = true;
            this.ultraTextEditor1.Name = "ultraTextEditor1";
            this.ultraTextEditor1.Size = new System.Drawing.Size(393, 58);
            this.ultraTextEditor1.TabIndex = 8;
            // 
            // rfidReader1
            // 
            this.rfidReader1.BackColor = System.Drawing.Color.Transparent;
            this.rfidReader1.DisableAutoStop = false;
            this.rfidReader1.Location = new System.Drawing.Point(12, 12);
            this.rfidReader1.MessageDisplayPosition = ControlLib.Rfid.RfidReader.MessagePosition.Bottom;
            this.rfidReader1.Name = "rfidReader1";
            this.rfidReader1.RFIDIPAddress = null;
            this.rfidReader1.RfidTagList = "";
            this.rfidReader1.SingleRfidTag = null;
            this.rfidReader1.Size = new System.Drawing.Size(218, 49);
            this.rfidReader1.SpaceCodeIPList = null;
            this.rfidReader1.TabIndex = 10;
            this.rfidReader1.WidthButton = 62;
            this.rfidReader1.WidthRfidCount = 85;
            this.rfidReader1.Load += new System.EventHandler(this.rfidReader1_Load);
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(418, 132);
            this.Controls.Add(this.rfidReader1);
            this.Controls.Add(this.ultraButton1);
            this.Controls.Add(this.ultraTextEditor1);
            this.Controls.Add(this.txtResult);
            this.Name = "Form2";
            this.Text = "Form2";
            this.Load += new System.EventHandler(this.Form2_Load);
            ((System.ComponentModel.ISupportInitialize)(this.ultraTextEditor1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtResult;
        private Infragistics.Win.Misc.UltraButton ultraButton1;
        private Infragistics.Win.UltraWinEditors.UltraTextEditor ultraTextEditor1;
        private ControlLib.Rfid.RfidReader rfidReader1;
    }
}