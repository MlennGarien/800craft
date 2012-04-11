namespace fCraft.ConfigGUI {
    partial class TextEditorPopup {
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
            this.tText = new System.Windows.Forms.TextBox();
            this.bOK = new System.Windows.Forms.Button();
            this.bCancel = new System.Windows.Forms.Button();
            this.bInsertKeyword = new System.Windows.Forms.Button();
            this.bInsertColor = new System.Windows.Forms.Button();
            this.bReset = new System.Windows.Forms.Button();
            this.lWarning = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // tText
            // 
            this.tText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tText.Font = new System.Drawing.Font( "Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.tText.Location = new System.Drawing.Point( 13, 41 );
            this.tText.Multiline = true;
            this.tText.Name = "tText";
            this.tText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tText.Size = new System.Drawing.Size( 485, 227 );
            this.tText.TabIndex = 0;
            this.tText.WordWrap = false;
            this.tText.KeyDown += new System.Windows.Forms.KeyEventHandler( this.tRules_KeyDown );
            // 
            // bOK
            // 
            this.bOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bOK.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.bOK.Location = new System.Drawing.Point( 292, 274 );
            this.bOK.Name = "bOK";
            this.bOK.Size = new System.Drawing.Size( 100, 28 );
            this.bOK.TabIndex = 1;
            this.bOK.Text = "OK";
            this.bOK.Click += new System.EventHandler( this.bOK_Click );
            // 
            // bCancel
            // 
            this.bCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bCancel.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.bCancel.Location = new System.Drawing.Point( 398, 274 );
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size( 100, 28 );
            this.bCancel.TabIndex = 2;
            this.bCancel.Text = "Cancel";
            // 
            // bInsertKeyword
            // 
            this.bInsertKeyword.Location = new System.Drawing.Point( 108, 12 );
            this.bInsertKeyword.Name = "bInsertKeyword";
            this.bInsertKeyword.Size = new System.Drawing.Size( 90, 23 );
            this.bInsertKeyword.TabIndex = 4;
            this.bInsertKeyword.Text = "Insert Keyword";
            this.bInsertKeyword.UseVisualStyleBackColor = true;
            this.bInsertKeyword.Click += new System.EventHandler( this.bInsertKeyword_Click );
            // 
            // bInsertColor
            // 
            this.bInsertColor.Location = new System.Drawing.Point( 12, 12 );
            this.bInsertColor.Name = "bInsertColor";
            this.bInsertColor.Size = new System.Drawing.Size( 90, 23 );
            this.bInsertColor.TabIndex = 3;
            this.bInsertColor.Text = "Insert Color";
            this.bInsertColor.UseVisualStyleBackColor = true;
            this.bInsertColor.Click += new System.EventHandler( this.bInsertColor_Click );
            // 
            // bReset
            // 
            this.bReset.Location = new System.Drawing.Point( 408, 12 );
            this.bReset.Name = "bReset";
            this.bReset.Size = new System.Drawing.Size( 90, 23 );
            this.bReset.TabIndex = 5;
            this.bReset.Text = "Reset";
            this.bReset.UseVisualStyleBackColor = true;
            this.bReset.Click += new System.EventHandler( this.bReset_Click );
            // 
            // lWarning
            // 
            this.lWarning.AutoSize = true;
            this.lWarning.Location = new System.Drawing.Point( 12, 283 );
            this.lWarning.Name = "lWarning";
            this.lWarning.Size = new System.Drawing.Size( 272, 13 );
            this.lWarning.TabIndex = 4;
            this.lWarning.Text = "Warning: Lines over 64 characters long will be wrapped.";
            // 
            // TextEditorPopup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size( 510, 314 );
            this.Controls.Add( this.bReset );
            this.Controls.Add( this.bInsertColor );
            this.Controls.Add( this.bInsertKeyword );
            this.Controls.Add( this.lWarning );
            this.Controls.Add( this.bCancel );
            this.Controls.Add( this.bOK );
            this.Controls.Add( this.tText );
            this.Name = "TextEditorPopup";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "TextEditorPopup";
            this.ResumeLayout( false );
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tText;
        private System.Windows.Forms.Button bOK;
        private System.Windows.Forms.Button bCancel;
        private System.Windows.Forms.Button bInsertKeyword;
        private System.Windows.Forms.Button bInsertColor;
        private System.Windows.Forms.Button bReset;
        private System.Windows.Forms.Label lWarning;
    }
}