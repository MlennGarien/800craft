namespace fCraft.ServerGUI {
    partial class UpdateWindow {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing ) {
            if( disposing && (components != null) ) {
                components.Dispose();
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.lHeader = new System.Windows.Forms.Label();
            this.bCancel = new System.Windows.Forms.Button();
            this.bUpdateNow = new System.Windows.Forms.Button();
            this.bUpdateLater = new System.Windows.Forms.Button();
            this.progress = new System.Windows.Forms.ProgressBar();
            this.lProgress = new System.Windows.Forms.Label();
            this.lVersion = new System.Windows.Forms.Label();
            this.tChangeLog = new System.Windows.Forms.TextBox();
            this.xShowDetails = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // lHeader
            // 
            this.lHeader.AutoSize = true;
            this.lHeader.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.lHeader.Location = new System.Drawing.Point( 12, 9 );
            this.lHeader.Name = "lHeader";
            this.lHeader.Size = new System.Drawing.Size( 187, 13 );
            this.lHeader.TabIndex = 5;
            this.lHeader.Text = "An update to fCraft is available!";
            // 
            // bCancel
            // 
            this.bCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bCancel.Location = new System.Drawing.Point( 472, 279 );
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size( 100, 23 );
            this.bCancel.TabIndex = 4;
            this.bCancel.Text = "Cancel";
            this.bCancel.UseVisualStyleBackColor = true;
            this.bCancel.Click += new System.EventHandler( this.bCancel_Click );
            // 
            // bUpdateNow
            // 
            this.bUpdateNow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bUpdateNow.Enabled = false;
            this.bUpdateNow.Location = new System.Drawing.Point( 260, 279 );
            this.bUpdateNow.Name = "bUpdateNow";
            this.bUpdateNow.Size = new System.Drawing.Size( 100, 23 );
            this.bUpdateNow.TabIndex = 2;
            this.bUpdateNow.Text = "Restart Now";
            this.bUpdateNow.UseVisualStyleBackColor = true;
            this.bUpdateNow.Click += new System.EventHandler( this.bUpdateNow_Click );
            // 
            // bUpdateLater
            // 
            this.bUpdateLater.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bUpdateLater.Enabled = false;
            this.bUpdateLater.Location = new System.Drawing.Point( 366, 279 );
            this.bUpdateLater.Name = "bUpdateLater";
            this.bUpdateLater.Size = new System.Drawing.Size( 100, 23 );
            this.bUpdateLater.TabIndex = 3;
            this.bUpdateLater.Text = "Update Later";
            this.bUpdateLater.UseVisualStyleBackColor = true;
            this.bUpdateLater.Click += new System.EventHandler( this.bUpdateLater_Click );
            // 
            // progress
            // 
            this.progress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.progress.Location = new System.Drawing.Point( 436, 12 );
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size( 136, 23 );
            this.progress.TabIndex = 7;
            // 
            // lProgress
            // 
            this.lProgress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lProgress.AutoSize = true;
            this.lProgress.Location = new System.Drawing.Point( 433, 38 );
            this.lProgress.Name = "lProgress";
            this.lProgress.Size = new System.Drawing.Size( 100, 13 );
            this.lProgress.TabIndex = 8;
            this.lProgress.Text = "Downloading ({0}%)";
            // 
            // lVersion
            // 
            this.lVersion.AutoSize = true;
            this.lVersion.Location = new System.Drawing.Point( 12, 25 );
            this.lVersion.Name = "lVersion";
            this.lVersion.Size = new System.Drawing.Size( 266, 26 );
            this.lVersion.TabIndex = 6;
            this.lVersion.Text = "Currently installed version: {0}\r\nNewest available version: {1} (released {2:0} d" +
                "ays ago)";
            // 
            // tChangeLog
            // 
            this.tChangeLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tChangeLog.Location = new System.Drawing.Point( 12, 70 );
            this.tChangeLog.Multiline = true;
            this.tChangeLog.Name = "tChangeLog";
            this.tChangeLog.ReadOnly = true;
            this.tChangeLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tChangeLog.Size = new System.Drawing.Size( 560, 203 );
            this.tChangeLog.TabIndex = 0;
            // 
            // xShowDetails
            // 
            this.xShowDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.xShowDetails.AutoSize = true;
            this.xShowDetails.Location = new System.Drawing.Point( 12, 283 );
            this.xShowDetails.Name = "xShowDetails";
            this.xShowDetails.Size = new System.Drawing.Size( 86, 17 );
            this.xShowDetails.TabIndex = 1;
            this.xShowDetails.Text = "Show details";
            this.xShowDetails.UseVisualStyleBackColor = true;
            this.xShowDetails.CheckedChanged += new System.EventHandler( this.xShowDetails_CheckedChanged );
            // 
            // UpdateWindow
            // 
            this.AcceptButton = this.bUpdateLater;
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bCancel;
            this.ClientSize = new System.Drawing.Size( 584, 314 );
            this.Controls.Add( this.xShowDetails );
            this.Controls.Add( this.tChangeLog );
            this.Controls.Add( this.lVersion );
            this.Controls.Add( this.bUpdateLater );
            this.Controls.Add( this.bUpdateNow );
            this.Controls.Add( this.bCancel );
            this.Controls.Add( this.lProgress );
            this.Controls.Add( this.lHeader );
            this.Controls.Add( this.progress );
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size( 500, 130 );
            this.Name = "UpdateWindow";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "fCraft Updater";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler( this.UpdateWindow_FormClosing );
            this.ResumeLayout( false );
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lHeader;
        private System.Windows.Forms.Button bCancel;
        private System.Windows.Forms.Button bUpdateNow;
        private System.Windows.Forms.Button bUpdateLater;
        private System.Windows.Forms.ProgressBar progress;
        private System.Windows.Forms.Label lProgress;
        private System.Windows.Forms.Label lVersion;
        private System.Windows.Forms.TextBox tChangeLog;
        private System.Windows.Forms.CheckBox xShowDetails;
    }
}