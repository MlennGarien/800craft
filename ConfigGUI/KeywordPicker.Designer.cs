namespace fCraft.ConfigGUI {
    sealed partial class KeywordPicker {
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
            this.pFlow = new System.Windows.Forms.FlowLayoutPanel();
            this.bCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // pFlow
            // 
            this.pFlow.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pFlow.Location = new System.Drawing.Point( 13, 13 );
            this.pFlow.Name = "pFlow";
            this.pFlow.Size = new System.Drawing.Size( 159, 318 );
            this.pFlow.TabIndex = 0;
            // 
            // bCancel
            // 
            this.bCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bCancel.Location = new System.Drawing.Point( 62, 337 );
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size( 60, 23 );
            this.bCancel.TabIndex = 1;
            this.bCancel.Text = "Cancel";
            this.bCancel.UseVisualStyleBackColor = true;
            // 
            // KeywordPicker
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bCancel;
            this.ClientSize = new System.Drawing.Size( 184, 372 );
            this.Controls.Add( this.bCancel );
            this.Controls.Add( this.pFlow );
            this.Name = "KeywordPicker";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "KeywordPicker";
            this.ResumeLayout( false );

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel pFlow;
        private System.Windows.Forms.Button bCancel;
    }
}