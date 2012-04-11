namespace fCraft.ConfigGUI {
    partial class DeleteRankPopup {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( DeleteRankPopup ) );
            this.lWarning = new System.Windows.Forms.Label();
            this.lSubstitute = new System.Windows.Forms.Label();
            this.cSubstitute = new System.Windows.Forms.ComboBox();
            this.bDelete = new System.Windows.Forms.Button();
            this.bCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lWarning
            // 
            this.lWarning.AutoSize = true;
            this.lWarning.Location = new System.Drawing.Point( 12, 9 );
            this.lWarning.Name = "lWarning";
            this.lWarning.Size = new System.Drawing.Size( 393, 39 );
            this.lWarning.TabIndex = 0;
            this.lWarning.Text = resources.GetString( "lWarning.Text" );
            // 
            // lSubstitute
            // 
            this.lSubstitute.AutoSize = true;
            this.lSubstitute.Location = new System.Drawing.Point( 12, 69 );
            this.lSubstitute.Name = "lSubstitute";
            this.lSubstitute.Size = new System.Drawing.Size( 81, 13 );
            this.lSubstitute.TabIndex = 1;
            this.lSubstitute.Text = "Substitute rank:";
            // 
            // cSubstitute
            // 
            this.cSubstitute.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cSubstitute.FormattingEnabled = true;
            this.cSubstitute.Location = new System.Drawing.Point( 102, 66 );
            this.cSubstitute.Name = "cSubstitute";
            this.cSubstitute.Size = new System.Drawing.Size( 121, 21 );
            this.cSubstitute.TabIndex = 2;
            this.cSubstitute.SelectedIndexChanged += new System.EventHandler( this.cSubstitute_SelectedIndexChanged );
            // 
            // bDelete
            // 
            this.bDelete.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.bDelete.Enabled = false;
            this.bDelete.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.bDelete.Location = new System.Drawing.Point( 203, 112 );
            this.bDelete.Name = "bDelete";
            this.bDelete.Size = new System.Drawing.Size( 100, 25 );
            this.bDelete.TabIndex = 3;
            this.bDelete.Text = "Delete";
            this.bDelete.UseVisualStyleBackColor = true;
            // 
            // bCancel
            // 
            this.bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bCancel.Location = new System.Drawing.Point( 309, 112 );
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size( 100, 25 );
            this.bCancel.TabIndex = 4;
            this.bCancel.Text = "Cancel";
            this.bCancel.UseVisualStyleBackColor = true;
            // 
            // DeleteRankPopup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size( 421, 149 );
            this.Controls.Add( this.bCancel );
            this.Controls.Add( this.bDelete );
            this.Controls.Add( this.cSubstitute );
            this.Controls.Add( this.lSubstitute );
            this.Controls.Add( this.lWarning );
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DeleteRankPopup";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Deleting a Rank";
            this.ResumeLayout( false );
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lWarning;
        private System.Windows.Forms.Label lSubstitute;
        private System.Windows.Forms.Button bDelete;
        private System.Windows.Forms.Button bCancel;
        private System.Windows.Forms.ComboBox cSubstitute;
    }
}