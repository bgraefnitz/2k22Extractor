namespace _2k21Extractor
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
            this.btnExport = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.txtFolder = new System.Windows.Forms.TextBox();
            this.btnFolder = new System.Windows.Forms.Button();
            this.chkAutoOpen = new System.Windows.Forms.CheckBox();
            this.chkAutoClose = new System.Windows.Forms.CheckBox();
            this.btnGameInput = new System.Windows.Forms.Button();
            this.chkAutoReplay = new System.Windows.Forms.CheckBox();
            this.cboAwayTeam = new System.Windows.Forms.ComboBox();
            this.cboHomeTeam = new System.Windows.Forms.ComboBox();
            this.lblAwayTeam = new System.Windows.Forms.Label();
            this.lblHomeTeam = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(154, 38);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(75, 23);
            this.btnExport.TabIndex = 0;
            this.btnExport.Text = "Export";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // txtFolder
            // 
            this.txtFolder.Location = new System.Drawing.Point(12, 12);
            this.txtFolder.Name = "txtFolder";
            this.txtFolder.Size = new System.Drawing.Size(186, 20);
            this.txtFolder.TabIndex = 1;
            this.txtFolder.TextChanged += new System.EventHandler(this.txtFolder_TextChanged);
            // 
            // btnFolder
            // 
            this.btnFolder.Location = new System.Drawing.Point(204, 9);
            this.btnFolder.Name = "btnFolder";
            this.btnFolder.Size = new System.Drawing.Size(25, 23);
            this.btnFolder.TabIndex = 2;
            this.btnFolder.Text = "...";
            this.btnFolder.UseVisualStyleBackColor = true;
            this.btnFolder.Click += new System.EventHandler(this.btnFolder_Click);
            // 
            // chkAutoOpen
            // 
            this.chkAutoOpen.AutoSize = true;
            this.chkAutoOpen.Location = new System.Drawing.Point(12, 72);
            this.chkAutoOpen.Name = "chkAutoOpen";
            this.chkAutoOpen.Size = new System.Drawing.Size(127, 17);
            this.chkAutoOpen.TabIndex = 6;
            this.chkAutoOpen.Text = "Auto-Open Live Feed";
            this.chkAutoOpen.UseVisualStyleBackColor = true;
            this.chkAutoOpen.CheckedChanged += new System.EventHandler(this.chkAutoOpen_CheckedChanged);
            // 
            // chkAutoClose
            // 
            this.chkAutoClose.AutoSize = true;
            this.chkAutoClose.Location = new System.Drawing.Point(12, 95);
            this.chkAutoClose.Name = "chkAutoClose";
            this.chkAutoClose.Size = new System.Drawing.Size(214, 17);
            this.chkAutoClose.TabIndex = 7;
            this.chkAutoClose.Text = "Auto-Close 2k and Extractor After Game";
            this.chkAutoClose.UseVisualStyleBackColor = true;
            this.chkAutoClose.CheckedChanged += new System.EventHandler(this.chkAutoClose_CheckedChanged);
            // 
            // btnGameInput
            // 
            this.btnGameInput.Location = new System.Drawing.Point(154, 172);
            this.btnGameInput.Name = "btnGameInput";
            this.btnGameInput.Size = new System.Drawing.Size(75, 23);
            this.btnGameInput.TabIndex = 8;
            this.btnGameInput.Text = "Auto Start";
            this.btnGameInput.UseVisualStyleBackColor = true;
            this.btnGameInput.Click += new System.EventHandler(this.btnGameInput_Click);
            // 
            // chkAutoReplay
            // 
            this.chkAutoReplay.AutoSize = true;
            this.chkAutoReplay.Location = new System.Drawing.Point(12, 181);
            this.chkAutoReplay.Name = "chkAutoReplay";
            this.chkAutoReplay.Size = new System.Drawing.Size(84, 17);
            this.chkAutoReplay.TabIndex = 9;
            this.chkAutoReplay.Text = "Auto Replay";
            this.chkAutoReplay.UseVisualStyleBackColor = true;
            this.chkAutoReplay.CheckedChanged += new System.EventHandler(this.chkAutoReplay_CheckedChanged);
            // 
            // cboAwayTeam
            // 
            this.cboAwayTeam.FormattingEnabled = true;
            this.cboAwayTeam.Location = new System.Drawing.Point(85, 118);
            this.cboAwayTeam.Name = "cboAwayTeam";
            this.cboAwayTeam.Size = new System.Drawing.Size(141, 21);
            this.cboAwayTeam.TabIndex = 10;
            // 
            // cboHomeTeam
            // 
            this.cboHomeTeam.FormattingEnabled = true;
            this.cboHomeTeam.Location = new System.Drawing.Point(87, 145);
            this.cboHomeTeam.Name = "cboHomeTeam";
            this.cboHomeTeam.Size = new System.Drawing.Size(139, 21);
            this.cboHomeTeam.TabIndex = 11;
            // 
            // lblAwayTeam
            // 
            this.lblAwayTeam.AutoSize = true;
            this.lblAwayTeam.Location = new System.Drawing.Point(13, 125);
            this.lblAwayTeam.Name = "lblAwayTeam";
            this.lblAwayTeam.Size = new System.Drawing.Size(66, 13);
            this.lblAwayTeam.TabIndex = 12;
            this.lblAwayTeam.Text = "Away Team:";
            // 
            // lblHomeTeam
            // 
            this.lblHomeTeam.AutoSize = true;
            this.lblHomeTeam.Location = new System.Drawing.Point(13, 153);
            this.lblHomeTeam.Name = "lblHomeTeam";
            this.lblHomeTeam.Size = new System.Drawing.Size(68, 13);
            this.lblHomeTeam.TabIndex = 13;
            this.lblHomeTeam.Text = "Home Team:";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(238, 228);
            this.Controls.Add(this.lblHomeTeam);
            this.Controls.Add(this.lblAwayTeam);
            this.Controls.Add(this.cboHomeTeam);
            this.Controls.Add(this.cboAwayTeam);
            this.Controls.Add(this.chkAutoReplay);
            this.Controls.Add(this.btnGameInput);
            this.Controls.Add(this.chkAutoClose);
            this.Controls.Add(this.chkAutoOpen);
            this.Controls.Add(this.btnFolder);
            this.Controls.Add(this.txtFolder);
            this.Controls.Add(this.btnExport);
            this.Name = "Form1";
            this.Text = "NBA 2k21 Stats Extractor";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.TextBox txtFolder;
        private System.Windows.Forms.Button btnFolder;
        private System.Windows.Forms.CheckBox chkAutoOpen;
        private System.Windows.Forms.CheckBox chkAutoClose;
        private System.Windows.Forms.Button btnGameInput;
        private System.Windows.Forms.CheckBox chkAutoReplay;
        private System.Windows.Forms.ComboBox cboAwayTeam;
        private System.Windows.Forms.ComboBox cboHomeTeam;
        private System.Windows.Forms.Label lblAwayTeam;
        private System.Windows.Forms.Label lblHomeTeam;
    }
}

