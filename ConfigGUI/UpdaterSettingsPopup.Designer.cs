namespace fCraft.ConfigGUI {
    partial class UpdaterSettingsPopup {
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
            this.gMode = new System.Windows.Forms.GroupBox();
            this.rAutomatic = new System.Windows.Forms.RadioButton();
            this.rPrompt = new System.Windows.Forms.RadioButton();
            this.rNotify = new System.Windows.Forms.RadioButton();
            this.rDisabled = new System.Windows.Forms.RadioButton();
            this.gOptions = new System.Windows.Forms.GroupBox();
            this.tRunAfterUpdate = new System.Windows.Forms.TextBox();
            this.xRunAfterUpdate = new System.Windows.Forms.CheckBox();
            this.tRunBeforeUpdate = new System.Windows.Forms.TextBox();
            this.xRunBeforeUpdate = new System.Windows.Forms.CheckBox();
            this.xBackupBeforeUpdating = new System.Windows.Forms.CheckBox();
            this.bOK = new System.Windows.Forms.Button();
            this.bCancel = new System.Windows.Forms.Button();
            this.gMode.SuspendLayout();
            this.gOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // gMode
            // 
            this.gMode.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.gMode.Controls.Add( this.rAutomatic );
            this.gMode.Controls.Add( this.rPrompt );
            this.gMode.Controls.Add( this.rNotify );
            this.gMode.Controls.Add( this.rDisabled );
            this.gMode.Location = new System.Drawing.Point( 13, 13 );
            this.gMode.Name = "gMode";
            this.gMode.Size = new System.Drawing.Size( 322, 119 );
            this.gMode.TabIndex = 0;
            this.gMode.TabStop = false;
            this.gMode.Text = "Updater preference";
            // 
            // rAutomatic
            // 
            this.rAutomatic.AutoSize = true;
            this.rAutomatic.Location = new System.Drawing.Point( 15, 92 );
            this.rAutomatic.Name = "rAutomatic";
            this.rAutomatic.Size = new System.Drawing.Size( 274, 17 );
            this.rAutomatic.TabIndex = 3;
            this.rAutomatic.TabStop = true;
            this.rAutomatic.Text = "Automatic - Download and apply all updates at once.";
            this.rAutomatic.UseVisualStyleBackColor = true;
            // 
            // rPrompt
            // 
            this.rPrompt.AutoSize = true;
            this.rPrompt.Location = new System.Drawing.Point( 15, 69 );
            this.rPrompt.Name = "rPrompt";
            this.rPrompt.Size = new System.Drawing.Size( 282, 17 );
            this.rPrompt.TabIndex = 2;
            this.rPrompt.TabStop = true;
            this.rPrompt.Text = "Prompt - Prepare updates to install, and ask to confirm.";
            this.rPrompt.UseVisualStyleBackColor = true;
            // 
            // rNotify
            // 
            this.rNotify.AutoSize = true;
            this.rNotify.Location = new System.Drawing.Point( 15, 46 );
            this.rNotify.Name = "rNotify";
            this.rNotify.Size = new System.Drawing.Size( 278, 17 );
            this.rNotify.TabIndex = 1;
            this.rNotify.TabStop = true;
            this.rNotify.Text = "Notify - Show a message when updates are available.";
            this.rNotify.UseVisualStyleBackColor = true;
            // 
            // rDisabled
            // 
            this.rDisabled.AutoSize = true;
            this.rDisabled.Location = new System.Drawing.Point( 15, 23 );
            this.rDisabled.Name = "rDisabled";
            this.rDisabled.Size = new System.Drawing.Size( 199, 17 );
            this.rDisabled.TabIndex = 0;
            this.rDisabled.TabStop = true;
            this.rDisabled.Text = "Disabled - Do not check for updates.";
            this.rDisabled.UseVisualStyleBackColor = true;
            this.rDisabled.CheckedChanged += new System.EventHandler( this.rDisabled_CheckedChanged );
            // 
            // gOptions
            // 
            this.gOptions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.gOptions.Controls.Add( this.tRunAfterUpdate );
            this.gOptions.Controls.Add( this.xRunAfterUpdate );
            this.gOptions.Controls.Add( this.tRunBeforeUpdate );
            this.gOptions.Controls.Add( this.xRunBeforeUpdate );
            this.gOptions.Controls.Add( this.xBackupBeforeUpdating );
            this.gOptions.Location = new System.Drawing.Point( 13, 138 );
            this.gOptions.Name = "gOptions";
            this.gOptions.Size = new System.Drawing.Size( 322, 147 );
            this.gOptions.TabIndex = 1;
            this.gOptions.TabStop = false;
            this.gOptions.Text = "Optional features";
            // 
            // tRunAfterUpdate
            // 
            this.tRunAfterUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tRunAfterUpdate.Enabled = false;
            this.tRunAfterUpdate.Font = new System.Drawing.Font( "Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.tRunAfterUpdate.Location = new System.Drawing.Point( 43, 118 );
            this.tRunAfterUpdate.Name = "tRunAfterUpdate";
            this.tRunAfterUpdate.Size = new System.Drawing.Size( 273, 20 );
            this.tRunAfterUpdate.TabIndex = 4;
            this.tRunAfterUpdate.TextChanged += new System.EventHandler( this.tRunAfterUpdate_TextChanged );
            // 
            // xRunAfterUpdate
            // 
            this.xRunAfterUpdate.AutoSize = true;
            this.xRunAfterUpdate.Location = new System.Drawing.Point( 15, 95 );
            this.xRunAfterUpdate.Name = "xRunAfterUpdate";
            this.xRunAfterUpdate.Size = new System.Drawing.Size( 215, 17 );
            this.xRunAfterUpdate.TabIndex = 3;
            this.xRunAfterUpdate.Text = "Run a script or command after updating:";
            this.xRunAfterUpdate.UseVisualStyleBackColor = true;
            this.xRunAfterUpdate.CheckedChanged += new System.EventHandler( this.xRunAfterUpdate_CheckedChanged );
            // 
            // tRunBeforeUpdate
            // 
            this.tRunBeforeUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tRunBeforeUpdate.Enabled = false;
            this.tRunBeforeUpdate.Font = new System.Drawing.Font( "Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.tRunBeforeUpdate.Location = new System.Drawing.Point( 43, 69 );
            this.tRunBeforeUpdate.Name = "tRunBeforeUpdate";
            this.tRunBeforeUpdate.Size = new System.Drawing.Size( 273, 20 );
            this.tRunBeforeUpdate.TabIndex = 2;
            this.tRunBeforeUpdate.TextChanged += new System.EventHandler( this.tRunBeforeUpdate_TextChanged );
            // 
            // xRunBeforeUpdate
            // 
            this.xRunBeforeUpdate.AutoSize = true;
            this.xRunBeforeUpdate.Location = new System.Drawing.Point( 15, 46 );
            this.xRunBeforeUpdate.Name = "xRunBeforeUpdate";
            this.xRunBeforeUpdate.Size = new System.Drawing.Size( 224, 17 );
            this.xRunBeforeUpdate.TabIndex = 1;
            this.xRunBeforeUpdate.Text = "Run a script or command before updating:";
            this.xRunBeforeUpdate.UseVisualStyleBackColor = true;
            this.xRunBeforeUpdate.CheckedChanged += new System.EventHandler( this.xRunBeforeUpdate_CheckedChanged );
            // 
            // xBackupBeforeUpdating
            // 
            this.xBackupBeforeUpdating.AutoSize = true;
            this.xBackupBeforeUpdating.Location = new System.Drawing.Point( 15, 23 );
            this.xBackupBeforeUpdating.Name = "xBackupBeforeUpdating";
            this.xBackupBeforeUpdating.Size = new System.Drawing.Size( 284, 17 );
            this.xBackupBeforeUpdating.TabIndex = 0;
            this.xBackupBeforeUpdating.Text = "Back up server data and configuration before updating";
            this.xBackupBeforeUpdating.UseVisualStyleBackColor = true;
            // 
            // bOK
            // 
            this.bOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.bOK.Location = new System.Drawing.Point( 179, 291 );
            this.bOK.Name = "bOK";
            this.bOK.Size = new System.Drawing.Size( 75, 23 );
            this.bOK.TabIndex = 2;
            this.bOK.Text = "OK";
            this.bOK.UseVisualStyleBackColor = true;
            // 
            // bCancel
            // 
            this.bCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bCancel.Location = new System.Drawing.Point( 260, 291 );
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size( 75, 23 );
            this.bCancel.TabIndex = 3;
            this.bCancel.Text = "Cancel";
            this.bCancel.UseVisualStyleBackColor = true;
            // 
            // UpdaterSettingsWindow
            // 
            this.AcceptButton = this.bOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bCancel;
            this.ClientSize = new System.Drawing.Size( 347, 326 );
            this.Controls.Add( this.bCancel );
            this.Controls.Add( this.bOK );
            this.Controls.Add( this.gOptions );
            this.Controls.Add( this.gMode );
            this.Name = "UpdaterSettingsWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Advanced updater settings";
            this.gMode.ResumeLayout( false );
            this.gMode.PerformLayout();
            this.gOptions.ResumeLayout( false );
            this.gOptions.PerformLayout();
            this.ResumeLayout( false );

        }

        #endregion

        private System.Windows.Forms.GroupBox gMode;
        private System.Windows.Forms.RadioButton rDisabled;
        private System.Windows.Forms.RadioButton rAutomatic;
        private System.Windows.Forms.RadioButton rPrompt;
        private System.Windows.Forms.RadioButton rNotify;
        private System.Windows.Forms.GroupBox gOptions;
        private System.Windows.Forms.TextBox tRunAfterUpdate;
        private System.Windows.Forms.CheckBox xRunAfterUpdate;
        private System.Windows.Forms.TextBox tRunBeforeUpdate;
        private System.Windows.Forms.CheckBox xRunBeforeUpdate;
        private System.Windows.Forms.CheckBox xBackupBeforeUpdating;
        private System.Windows.Forms.Button bOK;
        private System.Windows.Forms.Button bCancel;
    }
}