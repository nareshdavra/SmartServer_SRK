namespace Controltest
{
    partial class Form1
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
            this.button1 = new System.Windows.Forms.Button();
            this.ultraTextEditor1 = new Infragistics.Win.UltraWinEditors.UltraTextEditor();
            this.ultraTextEditor2 = new Infragistics.Win.UltraWinEditors.UltraTextEditor();
            this.ultraButton1 = new Infragistics.Win.Misc.UltraButton();
            this.txtResult = new Infragistics.Win.UltraWinEditors.UltraTextEditor();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.ultraTextEditor1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ultraTextEditor2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtResult)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(302, 227);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // ultraTextEditor1
            // 
            this.ultraTextEditor1.Location = new System.Drawing.Point(277, 12);
            this.ultraTextEditor1.Name = "ultraTextEditor1";
            this.ultraTextEditor1.Size = new System.Drawing.Size(100, 21);
            this.ultraTextEditor1.TabIndex = 5;
            this.ultraTextEditor1.Text = "3006716417,3006720353,3006721537,3006734472,3006742664";
            this.ultraTextEditor1.ValueChanged += new System.EventHandler(this.ultraTextEditor1_ValueChanged);
            // 
            // ultraTextEditor2
            // 
            this.ultraTextEditor2.Location = new System.Drawing.Point(277, 177);
            this.ultraTextEditor2.Name = "ultraTextEditor2";
            this.ultraTextEditor2.Size = new System.Drawing.Size(100, 21);
            this.ultraTextEditor2.TabIndex = 6;
            this.ultraTextEditor2.Text = "100715721245,110207182319";
            // 
            // ultraButton1
            // 
            this.ultraButton1.Location = new System.Drawing.Point(277, 39);
            this.ultraButton1.Name = "ultraButton1";
            this.ultraButton1.Size = new System.Drawing.Size(75, 23);
            this.ultraButton1.TabIndex = 7;
            this.ultraButton1.Text = "ultraButton1";
            this.ultraButton1.Click += new System.EventHandler(this.ultraButton1_Click);
            // 
            // txtResult
            // 
            this.txtResult.Location = new System.Drawing.Point(29, 76);
            this.txtResult.Name = "txtResult";
            this.txtResult.Size = new System.Drawing.Size(323, 21);
            this.txtResult.TabIndex = 8;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(159, 294);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "label1";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(410, 332);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtResult);
            this.Controls.Add(this.ultraButton1);
            this.Controls.Add(this.ultraTextEditor2);
            this.Controls.Add(this.ultraTextEditor1);
            this.Controls.Add(this.button1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.ultraTextEditor1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ultraTextEditor2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtResult)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ControlLib.Rfid.RfidReader rfidReader1;
        private ControlLib.Rfid.RfidReader rfidReader2;
        private System.Windows.Forms.Button button1;
        private Infragistics.Win.UltraWinEditors.UltraTextEditor ultraTextEditor1;
        private Infragistics.Win.UltraWinEditors.UltraTextEditor ultraTextEditor2;
        private Infragistics.Win.Misc.UltraButton ultraButton1;
        private Infragistics.Win.UltraWinEditors.UltraTextEditor txtResult;
        private System.Windows.Forms.Label label1;
    }
}

