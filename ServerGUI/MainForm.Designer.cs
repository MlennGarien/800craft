namespace fCraft.ServerGUI {
    partial class MainForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing ) {
            if( disposing && ( components != null ) ) {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.logBox = new System.Windows.Forms.RichTextBox();
            this.uriDisplay = new System.Windows.Forms.TextBox();
            this.URLLabel = new System.Windows.Forms.Label();
            this.playerList = new System.Windows.Forms.ListBox();
            this.playerListLabel = new System.Windows.Forms.Label();
            this.bPlay = new System.Windows.Forms.Button();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.InvertCheckBox = new System.Windows.Forms.CheckBox();
            this.console = new fCraft.ServerGUI.ConsoleBox();
            this.SuspendLayout();
            // 
            // logBox
            // 
            this.logBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logBox.BackColor = System.Drawing.Color.Black;
            this.logBox.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.logBox.HideSelection = false;
            this.logBox.Location = new System.Drawing.Point(12, 39);
            this.logBox.Name = "logBox";
            this.logBox.ReadOnly = true;
            this.logBox.Size = new System.Drawing.Size(610, 387);
            this.logBox.TabIndex = 7;
            this.logBox.Text = "";
            this.logBox.TextChanged += new System.EventHandler(this.logBox_TextChanged);
            // 
            // uriDisplay
            // 
            this.uriDisplay.Location = new System.Drawing.Point(92, 12);
            this.uriDisplay.Name = "uriDisplay";
            this.uriDisplay.Size = new System.Drawing.Size(476, 20);
            this.uriDisplay.TabIndex = 7;
            // 
            // URLLabel
            // 
            this.URLLabel.AutoSize = true;
            this.URLLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.URLLabel.Location = new System.Drawing.Point(9, 15);
            this.URLLabel.Name = "URLLabel";
            this.URLLabel.Size = new System.Drawing.Size(77, 13);
            this.URLLabel.TabIndex = 5;
            this.URLLabel.Text = "Server URL:";
            // 
            // playerList
            // 
            this.playerList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.playerList.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.playerList.FormattingEnabled = true;
            this.playerList.IntegralHeight = false;
            this.playerList.Location = new System.Drawing.Point(628, 58);
            this.playerList.Name = "playerList";
            this.playerList.Size = new System.Drawing.Size(144, 368);
            this.playerList.TabIndex = 4;
            // 
            // playerListLabel
            // 
            this.playerListLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.playerListLabel.AutoSize = true;
            this.playerListLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.playerListLabel.Location = new System.Drawing.Point(628, 42);
            this.playerListLabel.Name = "playerListLabel";
            this.playerListLabel.Size = new System.Drawing.Size(62, 13);
            this.playerListLabel.TabIndex = 6;
            this.playerListLabel.Text = "Player list";
            // 
            // bPlay
            // 
            this.bPlay.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bPlay.Enabled = false;
            this.bPlay.Location = new System.Drawing.Point(574, 10);
            this.bPlay.Name = "bPlay";
            this.bPlay.Size = new System.Drawing.Size(48, 23);
            this.bPlay.TabIndex = 2;
            this.bPlay.Text = "Play";
            this.bPlay.UseVisualStyleBackColor = true;
            this.bPlay.Click += new System.EventHandler(this.bPlay_Click);
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "Normal",
            "Big",
            "Large"});
            this.comboBox1.Location = new System.Drawing.Point(628, 10);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(56, 21);
            this.comboBox1.TabIndex = 8;
            this.comboBox1.Text = "Size";
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // InvertCheckBox
            // 
            this.InvertCheckBox.AutoSize = true;
            this.InvertCheckBox.Location = new System.Drawing.Point(690, 14);
            this.InvertCheckBox.Name = "InvertCheckBox";
            this.InvertCheckBox.Size = new System.Drawing.Size(85, 17);
            this.InvertCheckBox.TabIndex = 9;
            this.InvertCheckBox.Text = "Invert Colors";
            this.InvertCheckBox.UseVisualStyleBackColor = true;
            this.InvertCheckBox.CheckedChanged += new System.EventHandler(this.InvertCheckBox_CheckedChanged);
            // 
            // console
            // 
            this.console.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.console.Enabled = false;
            this.console.Location = new System.Drawing.Point(13, 433);
            this.console.Name = "console";
            this.console.Size = new System.Drawing.Size(759, 20);
            this.console.TabIndex = 0;
            this.console.Text = "Please wait, starting server...";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.ClientSize = new System.Drawing.Size(784, 464);
            this.Controls.Add(this.InvertCheckBox);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.bPlay);
            this.Controls.Add(this.console);
            this.Controls.Add(this.playerListLabel);
            this.Controls.Add(this.playerList);
            this.Controls.Add(this.URLLabel);
            this.Controls.Add(this.uriDisplay);
            this.Controls.Add(this.logBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(500, 150);
            this.Name = "MainForm";
            this.Text = "800Craft";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox logBox;
        private System.Windows.Forms.TextBox uriDisplay;
        private System.Windows.Forms.Label URLLabel;
        private System.Windows.Forms.ListBox playerList;
        private System.Windows.Forms.Label playerListLabel;
        private ConsoleBox console;
        private System.Windows.Forms.Button bPlay;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.CheckBox InvertCheckBox;
    }
}

