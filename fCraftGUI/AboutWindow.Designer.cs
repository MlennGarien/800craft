namespace fCraft.GUI {
    sealed partial class AboutWindow {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutWindow));
            this.lHeader = new System.Windows.Forms.Label();
            this.lSubheader = new System.Windows.Forms.Label();
            this.linkLabel2 = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // lHeader
            // 
            this.lHeader.AutoSize = true;
            this.lHeader.Font = new System.Drawing.Font("Consolas", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lHeader.ForeColor = System.Drawing.Color.White;
            this.lHeader.Image = ((System.Drawing.Image)(resources.GetObject("lHeader.Image")));
            this.lHeader.Location = new System.Drawing.Point(109, 9);
            this.lHeader.Name = "lHeader";
            this.lHeader.Size = new System.Drawing.Size(258, 224);
            this.lHeader.TabIndex = 1;
            this.lHeader.Text = "         \r\n\r\n\r\n\r\n";
            this.lHeader.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lSubheader
            // 
            this.lSubheader.AutoSize = true;
            this.lSubheader.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lSubheader.Location = new System.Drawing.Point(82, 233);
            this.lSubheader.Name = "lSubheader";
            this.lSubheader.Size = new System.Drawing.Size(298, 70);
            this.lSubheader.TabIndex = 2;
            this.lSubheader.Text = "Version {0}\r\nBased on 800craft by Jonty800, GlennMR and LaoTszy\r\n800craft Plus de" +
    "veloped by MrBluePotato and LeChosenOne\r\n\r\nFor news, documentation, and support," +
    " visit  ww.800craft.n";
            this.lSubheader.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // linkLabel2
            // 
            this.linkLabel2.AutoSize = true;
            this.linkLabel2.Location = new System.Drawing.Point(299, 290);
            this.linkLabel2.Name = "linkLabel2";
            this.linkLabel2.Size = new System.Drawing.Size(92, 13);
            this.linkLabel2.TabIndex = 4;
            this.linkLabel2.TabStop = true;
            this.linkLabel2.Text = "www.800Craft.net";
            this.linkLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel2_LinkClicked_1);
            // 
            // AboutWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(492, 404);
            this.Controls.Add(this.linkLabel2);
            this.Controls.Add(this.lSubheader);
            this.Controls.Add(this.lHeader);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(500, 440);
            this.Name = "AboutWindow";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lHeader;
        private System.Windows.Forms.Label lSubheader;
        private System.Windows.Forms.LinkLabel linkLabel2;
    }
}