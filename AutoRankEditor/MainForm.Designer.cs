namespace AutoRankEditor {
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
            this.components = new System.ComponentModel.Container();
            this.treeData = new System.Windows.Forms.TreeView();
            this.bAddGroup = new System.Windows.Forms.Button();
            this.cmAddGroup = new System.Windows.Forms.ContextMenuStrip( this.components );
            this.aNDToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.oRToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nANDToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nORToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bAddCondition = new System.Windows.Forms.Button();
            this.cmAddCondition = new System.Windows.Forms.ContextMenuStrip( this.components );
            this.timeSinceFirstLoginToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lastSeenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.firstJoinToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mostRecentJoinToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mostRecentKickToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mostRecentPromotionDemotionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.blocksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.builtToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deletedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.builtDeletedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.drawnToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.messagesWrittenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.totalTimeSpentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.numberOfTimesVisitedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.numberOfTimesKickedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bDeleteGroup = new System.Windows.Forms.Button();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.gEditGroup = new System.Windows.Forms.GroupBox();
            this.lGroupOp = new System.Windows.Forms.Label();
            this.cGroupOp = new System.Windows.Forms.ComboBox();
            this.gEditCondition = new System.Windows.Forms.GroupBox();
            this.nConditionValue = new System.Windows.Forms.NumericUpDown();
            this.lConditionValue = new System.Windows.Forms.Label();
            this.cConditionOp = new System.Windows.Forms.ComboBox();
            this.lConditionOp = new System.Windows.Forms.Label();
            this.cConditionField = new System.Windows.Forms.ComboBox();
            this.lConditionField = new System.Windows.Forms.Label();
            this.bDeleteCondition = new System.Windows.Forms.Button();
            this.gEditAction = new System.Windows.Forms.GroupBox();
            this.lActionConnective = new System.Windows.Forms.Label();
            this.cActionConnective = new System.Windows.Forms.ComboBox();
            this.cToRank = new System.Windows.Forms.ComboBox();
            this.cFromRank = new System.Windows.Forms.ComboBox();
            this.lToRank = new System.Windows.Forms.Label();
            this.lFromRank = new System.Windows.Forms.Label();
            this.lActionType = new System.Windows.Forms.Label();
            this.cActionType = new System.Windows.Forms.ComboBox();
            this.bDeleteAction = new System.Windows.Forms.Button();
            this.tHelp = new System.Windows.Forms.TextBox();
            this.bAction = new System.Windows.Forms.Button();
            this.bOK = new System.Windows.Forms.Button();
            this.bCancel = new System.Windows.Forms.Button();
            this.cmAddGroup.SuspendLayout();
            this.cmAddCondition.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.gEditGroup.SuspendLayout();
            this.gEditCondition.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nConditionValue)).BeginInit();
            this.gEditAction.SuspendLayout();
            this.SuspendLayout();
            // 
            // treeData
            // 
            this.treeData.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.treeData.HideSelection = false;
            this.treeData.Location = new System.Drawing.Point( 12, 41 );
            this.treeData.Name = "treeData";
            this.treeData.PathSeparator = ".";
            this.treeData.Size = new System.Drawing.Size( 267, 282 );
            this.treeData.TabIndex = 0;
            this.treeData.AfterSelect += new System.Windows.Forms.TreeViewEventHandler( this.treeData_AfterSelect );
            // 
            // bAddGroup
            // 
            this.bAddGroup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bAddGroup.Location = new System.Drawing.Point( 12, 329 );
            this.bAddGroup.Name = "bAddGroup";
            this.bAddGroup.Size = new System.Drawing.Size( 85, 23 );
            this.bAddGroup.TabIndex = 1;
            this.bAddGroup.Text = "Add Group";
            this.bAddGroup.UseVisualStyleBackColor = true;
            this.bAddGroup.Click += new System.EventHandler( this.bAddGroup_Click );
            // 
            // cmAddGroup
            // 
            this.cmAddGroup.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.aNDToolStripMenuItem,
            this.oRToolStripMenuItem,
            this.nANDToolStripMenuItem,
            this.nORToolStripMenuItem} );
            this.cmAddGroup.Name = "cmAddGroup";
            this.cmAddGroup.Size = new System.Drawing.Size( 109, 92 );
            this.cmAddGroup.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler( this.cmAddGroup_ItemClicked );
            // 
            // aNDToolStripMenuItem
            // 
            this.aNDToolStripMenuItem.Name = "aNDToolStripMenuItem";
            this.aNDToolStripMenuItem.Size = new System.Drawing.Size( 108, 22 );
            this.aNDToolStripMenuItem.Text = "AND";
            // 
            // oRToolStripMenuItem
            // 
            this.oRToolStripMenuItem.Name = "oRToolStripMenuItem";
            this.oRToolStripMenuItem.Size = new System.Drawing.Size( 108, 22 );
            this.oRToolStripMenuItem.Text = "OR";
            // 
            // nANDToolStripMenuItem
            // 
            this.nANDToolStripMenuItem.Name = "nANDToolStripMenuItem";
            this.nANDToolStripMenuItem.Size = new System.Drawing.Size( 108, 22 );
            this.nANDToolStripMenuItem.Text = "NAND";
            // 
            // nORToolStripMenuItem
            // 
            this.nORToolStripMenuItem.Name = "nORToolStripMenuItem";
            this.nORToolStripMenuItem.Size = new System.Drawing.Size( 108, 22 );
            this.nORToolStripMenuItem.Text = "NOR";
            // 
            // bAddCondition
            // 
            this.bAddCondition.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bAddCondition.Location = new System.Drawing.Point( 103, 329 );
            this.bAddCondition.Name = "bAddCondition";
            this.bAddCondition.Size = new System.Drawing.Size( 85, 23 );
            this.bAddCondition.TabIndex = 3;
            this.bAddCondition.Text = "Add Condition";
            this.bAddCondition.UseVisualStyleBackColor = true;
            this.bAddCondition.Click += new System.EventHandler( this.bAddCondition_Click );
            // 
            // cmAddCondition
            // 
            this.cmAddCondition.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.timeSinceFirstLoginToolStripMenuItem,
            this.blocksToolStripMenuItem,
            this.messagesWrittenToolStripMenuItem,
            this.totalTimeSpentToolStripMenuItem,
            this.numberOfTimesVisitedToolStripMenuItem,
            this.numberOfTimesKickedToolStripMenuItem} );
            this.cmAddCondition.Name = "cmAddCondition";
            this.cmAddCondition.Size = new System.Drawing.Size( 206, 136 );
            // 
            // timeSinceFirstLoginToolStripMenuItem
            // 
            this.timeSinceFirstLoginToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.lastSeenToolStripMenuItem,
            this.firstJoinToolStripMenuItem,
            this.mostRecentJoinToolStripMenuItem,
            this.mostRecentKickToolStripMenuItem,
            this.mostRecentPromotionDemotionToolStripMenuItem} );
            this.timeSinceFirstLoginToolStripMenuItem.Name = "timeSinceFirstLoginToolStripMenuItem";
            this.timeSinceFirstLoginToolStripMenuItem.Size = new System.Drawing.Size( 205, 22 );
            this.timeSinceFirstLoginToolStripMenuItem.Text = "Time Since";
            // 
            // lastSeenToolStripMenuItem
            // 
            this.lastSeenToolStripMenuItem.Name = "lastSeenToolStripMenuItem";
            this.lastSeenToolStripMenuItem.Size = new System.Drawing.Size( 258, 22 );
            this.lastSeenToolStripMenuItem.Text = "Last Seen";
            // 
            // firstJoinToolStripMenuItem
            // 
            this.firstJoinToolStripMenuItem.Name = "firstJoinToolStripMenuItem";
            this.firstJoinToolStripMenuItem.Size = new System.Drawing.Size( 258, 22 );
            this.firstJoinToolStripMenuItem.Text = "First Join";
            // 
            // mostRecentJoinToolStripMenuItem
            // 
            this.mostRecentJoinToolStripMenuItem.Name = "mostRecentJoinToolStripMenuItem";
            this.mostRecentJoinToolStripMenuItem.Size = new System.Drawing.Size( 258, 22 );
            this.mostRecentJoinToolStripMenuItem.Text = "Most Recent Join";
            // 
            // mostRecentKickToolStripMenuItem
            // 
            this.mostRecentKickToolStripMenuItem.Name = "mostRecentKickToolStripMenuItem";
            this.mostRecentKickToolStripMenuItem.Size = new System.Drawing.Size( 258, 22 );
            this.mostRecentKickToolStripMenuItem.Text = "Most Recent Kick";
            // 
            // mostRecentPromotionDemotionToolStripMenuItem
            // 
            this.mostRecentPromotionDemotionToolStripMenuItem.Name = "mostRecentPromotionDemotionToolStripMenuItem";
            this.mostRecentPromotionDemotionToolStripMenuItem.Size = new System.Drawing.Size( 258, 22 );
            this.mostRecentPromotionDemotionToolStripMenuItem.Text = "Most Recent Promotion/Demotion";
            // 
            // blocksToolStripMenuItem
            // 
            this.blocksToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.builtToolStripMenuItem,
            this.deletedToolStripMenuItem,
            this.builtDeletedToolStripMenuItem,
            this.drawnToolStripMenuItem} );
            this.blocksToolStripMenuItem.Name = "blocksToolStripMenuItem";
            this.blocksToolStripMenuItem.Size = new System.Drawing.Size( 205, 22 );
            this.blocksToolStripMenuItem.Text = "Blocks";
            // 
            // builtToolStripMenuItem
            // 
            this.builtToolStripMenuItem.Name = "builtToolStripMenuItem";
            this.builtToolStripMenuItem.Size = new System.Drawing.Size( 152, 22 );
            this.builtToolStripMenuItem.Text = "Built";
            // 
            // deletedToolStripMenuItem
            // 
            this.deletedToolStripMenuItem.Name = "deletedToolStripMenuItem";
            this.deletedToolStripMenuItem.Size = new System.Drawing.Size( 152, 22 );
            this.deletedToolStripMenuItem.Text = "Deleted";
            // 
            // builtDeletedToolStripMenuItem
            // 
            this.builtDeletedToolStripMenuItem.Name = "builtDeletedToolStripMenuItem";
            this.builtDeletedToolStripMenuItem.Size = new System.Drawing.Size( 152, 22 );
            this.builtDeletedToolStripMenuItem.Text = "Built + Deleted";
            // 
            // drawnToolStripMenuItem
            // 
            this.drawnToolStripMenuItem.Name = "drawnToolStripMenuItem";
            this.drawnToolStripMenuItem.Size = new System.Drawing.Size( 152, 22 );
            this.drawnToolStripMenuItem.Text = "Drawn";
            // 
            // messagesWrittenToolStripMenuItem
            // 
            this.messagesWrittenToolStripMenuItem.Name = "messagesWrittenToolStripMenuItem";
            this.messagesWrittenToolStripMenuItem.Size = new System.Drawing.Size( 205, 22 );
            this.messagesWrittenToolStripMenuItem.Text = "Messages Written";
            // 
            // totalTimeSpentToolStripMenuItem
            // 
            this.totalTimeSpentToolStripMenuItem.Name = "totalTimeSpentToolStripMenuItem";
            this.totalTimeSpentToolStripMenuItem.Size = new System.Drawing.Size( 205, 22 );
            this.totalTimeSpentToolStripMenuItem.Text = "Total Time Spent";
            // 
            // numberOfTimesVisitedToolStripMenuItem
            // 
            this.numberOfTimesVisitedToolStripMenuItem.Name = "numberOfTimesVisitedToolStripMenuItem";
            this.numberOfTimesVisitedToolStripMenuItem.Size = new System.Drawing.Size( 205, 22 );
            this.numberOfTimesVisitedToolStripMenuItem.Text = "Number of Visits";
            // 
            // numberOfTimesKickedToolStripMenuItem
            // 
            this.numberOfTimesKickedToolStripMenuItem.Name = "numberOfTimesKickedToolStripMenuItem";
            this.numberOfTimesKickedToolStripMenuItem.Size = new System.Drawing.Size( 205, 22 );
            this.numberOfTimesKickedToolStripMenuItem.Text = "Number of Times Kicked";
            // 
            // bDeleteGroup
            // 
            this.bDeleteGroup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bDeleteGroup.Location = new System.Drawing.Point( 160, 59 );
            this.bDeleteGroup.Name = "bDeleteGroup";
            this.bDeleteGroup.Size = new System.Drawing.Size( 56, 23 );
            this.bDeleteGroup.TabIndex = 4;
            this.bDeleteGroup.Text = "Delete";
            this.bDeleteGroup.UseVisualStyleBackColor = true;
            this.bDeleteGroup.Click += new System.EventHandler( this.bDelete_Click );
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel1.Controls.Add( this.gEditGroup );
            this.flowLayoutPanel1.Controls.Add( this.gEditCondition );
            this.flowLayoutPanel1.Controls.Add( this.gEditAction );
            this.flowLayoutPanel1.Controls.Add( this.tHelp );
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point( 283, 12 );
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size( 229, 311 );
            this.flowLayoutPanel1.TabIndex = 5;
            this.flowLayoutPanel1.WrapContents = false;
            // 
            // gEditGroup
            // 
            this.gEditGroup.Controls.Add( this.lGroupOp );
            this.gEditGroup.Controls.Add( this.cGroupOp );
            this.gEditGroup.Controls.Add( this.bDeleteGroup );
            this.gEditGroup.Location = new System.Drawing.Point( 3, 3 );
            this.gEditGroup.Name = "gEditGroup";
            this.gEditGroup.Size = new System.Drawing.Size( 222, 88 );
            this.gEditGroup.TabIndex = 0;
            this.gEditGroup.TabStop = false;
            this.gEditGroup.Text = "Edit Group";
            this.gEditGroup.Visible = false;
            // 
            // lGroupOp
            // 
            this.lGroupOp.AutoSize = true;
            this.lGroupOp.Location = new System.Drawing.Point( 8, 27 );
            this.lGroupOp.Name = "lGroupOp";
            this.lGroupOp.Size = new System.Drawing.Size( 61, 13 );
            this.lGroupOp.TabIndex = 5;
            this.lGroupOp.Text = "Connective";
            // 
            // cGroupOp
            // 
            this.cGroupOp.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cGroupOp.FormattingEnabled = true;
            this.cGroupOp.Location = new System.Drawing.Point( 75, 24 );
            this.cGroupOp.Name = "cGroupOp";
            this.cGroupOp.Size = new System.Drawing.Size( 83, 21 );
            this.cGroupOp.TabIndex = 0;
            this.cGroupOp.SelectedIndexChanged += new System.EventHandler( this.cGroupOp_SelectedIndexChanged );
            // 
            // gEditCondition
            // 
            this.gEditCondition.Controls.Add( this.nConditionValue );
            this.gEditCondition.Controls.Add( this.lConditionValue );
            this.gEditCondition.Controls.Add( this.cConditionOp );
            this.gEditCondition.Controls.Add( this.lConditionOp );
            this.gEditCondition.Controls.Add( this.cConditionField );
            this.gEditCondition.Controls.Add( this.lConditionField );
            this.gEditCondition.Controls.Add( this.bDeleteCondition );
            this.gEditCondition.Location = new System.Drawing.Point( 3, 97 );
            this.gEditCondition.Name = "gEditCondition";
            this.gEditCondition.Size = new System.Drawing.Size( 222, 147 );
            this.gEditCondition.TabIndex = 1;
            this.gEditCondition.TabStop = false;
            this.gEditCondition.Text = "Edit Condition";
            this.gEditCondition.Visible = false;
            // 
            // nConditionValue
            // 
            this.nConditionValue.Location = new System.Drawing.Point( 75, 90 );
            this.nConditionValue.Maximum = new decimal( new int[] {
            999999999,
            0,
            0,
            0} );
            this.nConditionValue.Minimum = new decimal( new int[] {
            999999999,
            0,
            0,
            -2147483648} );
            this.nConditionValue.Name = "nConditionValue";
            this.nConditionValue.Size = new System.Drawing.Size( 120, 20 );
            this.nConditionValue.TabIndex = 11;
            this.nConditionValue.ValueChanged += new System.EventHandler( this.nConditionValue_ValueChanged );
            // 
            // lConditionValue
            // 
            this.lConditionValue.AutoSize = true;
            this.lConditionValue.Location = new System.Drawing.Point( 35, 92 );
            this.lConditionValue.Name = "lConditionValue";
            this.lConditionValue.Size = new System.Drawing.Size( 34, 13 );
            this.lConditionValue.TabIndex = 10;
            this.lConditionValue.Text = "Value";
            // 
            // cConditionOp
            // 
            this.cConditionOp.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cConditionOp.FormattingEnabled = true;
            this.cConditionOp.Items.AddRange( new object[] {
            "Equal (=)",
            "Not Equal (!=)",
            "Greater Than (>)",
            "Greater Than Or Equal (>=)",
            "Less Than (<)",
            "Less Than Or Equal (<=)"} );
            this.cConditionOp.Location = new System.Drawing.Point( 75, 56 );
            this.cConditionOp.Name = "cConditionOp";
            this.cConditionOp.Size = new System.Drawing.Size( 141, 21 );
            this.cConditionOp.TabIndex = 9;
            this.cConditionOp.SelectedIndexChanged += new System.EventHandler( this.cConditionOp_SelectedIndexChanged );
            // 
            // lConditionOp
            // 
            this.lConditionOp.AutoSize = true;
            this.lConditionOp.Location = new System.Drawing.Point( 21, 59 );
            this.lConditionOp.Name = "lConditionOp";
            this.lConditionOp.Size = new System.Drawing.Size( 48, 13 );
            this.lConditionOp.TabIndex = 8;
            this.lConditionOp.Text = "Operator";
            // 
            // cConditionField
            // 
            this.cConditionField.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cConditionField.FormattingEnabled = true;
            this.cConditionField.Location = new System.Drawing.Point( 75, 26 );
            this.cConditionField.Name = "cConditionField";
            this.cConditionField.Size = new System.Drawing.Size( 141, 21 );
            this.cConditionField.TabIndex = 7;
            this.cConditionField.SelectedIndexChanged += new System.EventHandler( this.cConditionField_SelectedIndexChanged );
            // 
            // lConditionField
            // 
            this.lConditionField.AutoSize = true;
            this.lConditionField.Location = new System.Drawing.Point( 40, 29 );
            this.lConditionField.Name = "lConditionField";
            this.lConditionField.Size = new System.Drawing.Size( 29, 13 );
            this.lConditionField.TabIndex = 6;
            this.lConditionField.Text = "Field";
            // 
            // bDeleteCondition
            // 
            this.bDeleteCondition.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bDeleteCondition.Location = new System.Drawing.Point( 160, 118 );
            this.bDeleteCondition.Name = "bDeleteCondition";
            this.bDeleteCondition.Size = new System.Drawing.Size( 56, 23 );
            this.bDeleteCondition.TabIndex = 5;
            this.bDeleteCondition.Text = "Delete";
            this.bDeleteCondition.UseVisualStyleBackColor = true;
            this.bDeleteCondition.Click += new System.EventHandler( this.bDelete_Click );
            // 
            // gEditAction
            // 
            this.gEditAction.Controls.Add( this.lActionConnective );
            this.gEditAction.Controls.Add( this.cActionConnective );
            this.gEditAction.Controls.Add( this.cToRank );
            this.gEditAction.Controls.Add( this.cFromRank );
            this.gEditAction.Controls.Add( this.lToRank );
            this.gEditAction.Controls.Add( this.lFromRank );
            this.gEditAction.Controls.Add( this.lActionType );
            this.gEditAction.Controls.Add( this.cActionType );
            this.gEditAction.Controls.Add( this.bDeleteAction );
            this.gEditAction.Location = new System.Drawing.Point( 3, 250 );
            this.gEditAction.Name = "gEditAction";
            this.gEditAction.Size = new System.Drawing.Size( 222, 159 );
            this.gEditAction.TabIndex = 2;
            this.gEditAction.TabStop = false;
            this.gEditAction.Text = "Edit Action";
            this.gEditAction.Visible = false;
            // 
            // lActionConnective
            // 
            this.lActionConnective.AutoSize = true;
            this.lActionConnective.Location = new System.Drawing.Point( 21, 103 );
            this.lActionConnective.Name = "lActionConnective";
            this.lActionConnective.Size = new System.Drawing.Size( 61, 13 );
            this.lActionConnective.TabIndex = 15;
            this.lActionConnective.Text = "Connective";
            // 
            // cActionConnective
            // 
            this.cActionConnective.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cActionConnective.FormattingEnabled = true;
            this.cActionConnective.Location = new System.Drawing.Point( 88, 100 );
            this.cActionConnective.Name = "cActionConnective";
            this.cActionConnective.Size = new System.Drawing.Size( 83, 21 );
            this.cActionConnective.TabIndex = 14;
            this.cActionConnective.SelectedIndexChanged += new System.EventHandler( this.cActionConnective_SelectedIndexChanged );
            // 
            // cToRank
            // 
            this.cToRank.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cToRank.FormattingEnabled = true;
            this.cToRank.Location = new System.Drawing.Point( 88, 73 );
            this.cToRank.Name = "cToRank";
            this.cToRank.Size = new System.Drawing.Size( 128, 21 );
            this.cToRank.TabIndex = 13;
            this.cToRank.SelectedIndexChanged += new System.EventHandler( this.cToRank_SelectedIndexChanged );
            // 
            // cFromRank
            // 
            this.cFromRank.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cFromRank.FormattingEnabled = true;
            this.cFromRank.Location = new System.Drawing.Point( 88, 46 );
            this.cFromRank.Name = "cFromRank";
            this.cFromRank.Size = new System.Drawing.Size( 128, 21 );
            this.cFromRank.TabIndex = 12;
            this.cFromRank.SelectedIndexChanged += new System.EventHandler( this.cFromRank_SelectedIndexChanged );
            // 
            // lToRank
            // 
            this.lToRank.AutoSize = true;
            this.lToRank.Location = new System.Drawing.Point( 33, 76 );
            this.lToRank.Name = "lToRank";
            this.lToRank.Size = new System.Drawing.Size( 49, 13 );
            this.lToRank.TabIndex = 10;
            this.lToRank.Text = "To Rank";
            // 
            // lFromRank
            // 
            this.lFromRank.AutoSize = true;
            this.lFromRank.Location = new System.Drawing.Point( 23, 49 );
            this.lFromRank.Name = "lFromRank";
            this.lFromRank.Size = new System.Drawing.Size( 59, 13 );
            this.lFromRank.TabIndex = 8;
            this.lFromRank.Text = "From Rank";
            // 
            // lActionType
            // 
            this.lActionType.AutoSize = true;
            this.lActionType.Location = new System.Drawing.Point( 18, 22 );
            this.lActionType.Name = "lActionType";
            this.lActionType.Size = new System.Drawing.Size( 64, 13 );
            this.lActionType.TabIndex = 7;
            this.lActionType.Text = "Action Type";
            // 
            // cActionType
            // 
            this.cActionType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cActionType.FormattingEnabled = true;
            this.cActionType.Location = new System.Drawing.Point( 88, 19 );
            this.cActionType.Name = "cActionType";
            this.cActionType.Size = new System.Drawing.Size( 128, 21 );
            this.cActionType.TabIndex = 6;
            this.cActionType.SelectedIndexChanged += new System.EventHandler( this.cActionType_SelectedIndexChanged );
            // 
            // bDeleteAction
            // 
            this.bDeleteAction.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bDeleteAction.Location = new System.Drawing.Point( 160, 130 );
            this.bDeleteAction.Name = "bDeleteAction";
            this.bDeleteAction.Size = new System.Drawing.Size( 56, 23 );
            this.bDeleteAction.TabIndex = 5;
            this.bDeleteAction.Text = "Delete";
            this.bDeleteAction.UseVisualStyleBackColor = true;
            this.bDeleteAction.Click += new System.EventHandler( this.bDelete_Click );
            // 
            // tHelp
            // 
            this.tHelp.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tHelp.Location = new System.Drawing.Point( 3, 415 );
            this.tHelp.Multiline = true;
            this.tHelp.Name = "tHelp";
            this.tHelp.ReadOnly = true;
            this.tHelp.Size = new System.Drawing.Size( 222, 87 );
            this.tHelp.TabIndex = 7;
            // 
            // bAction
            // 
            this.bAction.Location = new System.Drawing.Point( 12, 12 );
            this.bAction.Name = "bAction";
            this.bAction.Size = new System.Drawing.Size( 85, 23 );
            this.bAction.TabIndex = 6;
            this.bAction.Text = "New Action";
            this.bAction.UseVisualStyleBackColor = true;
            this.bAction.Click += new System.EventHandler( this.bAction_Click );
            // 
            // bOK
            // 
            this.bOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.bOK.Location = new System.Drawing.Point( 437, 329 );
            this.bOK.Name = "bOK";
            this.bOK.Size = new System.Drawing.Size( 75, 23 );
            this.bOK.TabIndex = 7;
            this.bOK.Text = "OK";
            this.bOK.UseVisualStyleBackColor = true;
            this.bOK.Click += new System.EventHandler( this.bOK_Click );
            // 
            // bCancel
            // 
            this.bCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bCancel.Location = new System.Drawing.Point( 356, 329 );
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size( 75, 23 );
            this.bCancel.TabIndex = 8;
            this.bCancel.Text = "Cancel";
            this.bCancel.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AcceptButton = this.bOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bCancel;
            this.ClientSize = new System.Drawing.Size( 524, 364 );
            this.Controls.Add( this.treeData );
            this.Controls.Add( this.flowLayoutPanel1 );
            this.Controls.Add( this.bCancel );
            this.Controls.Add( this.bOK );
            this.Controls.Add( this.bAction );
            this.Controls.Add( this.bAddGroup );
            this.Controls.Add( this.bAddCondition );
            this.MinimumSize = new System.Drawing.Size( 540, 300 );
            this.Name = "MainForm";
            this.Text = "Tree Test";
            this.cmAddGroup.ResumeLayout( false );
            this.cmAddCondition.ResumeLayout( false );
            this.flowLayoutPanel1.ResumeLayout( false );
            this.flowLayoutPanel1.PerformLayout();
            this.gEditGroup.ResumeLayout( false );
            this.gEditGroup.PerformLayout();
            this.gEditCondition.ResumeLayout( false );
            this.gEditCondition.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nConditionValue)).EndInit();
            this.gEditAction.ResumeLayout( false );
            this.gEditAction.PerformLayout();
            this.ResumeLayout( false );

        }

        #endregion

        private System.Windows.Forms.TreeView treeData;
        private System.Windows.Forms.Button bAddGroup;
        private System.Windows.Forms.ContextMenuStrip cmAddGroup;
        private System.Windows.Forms.ToolStripMenuItem aNDToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem oRToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem nANDToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem nORToolStripMenuItem;
        private System.Windows.Forms.Button bAddCondition;
        private System.Windows.Forms.ContextMenuStrip cmAddCondition;
        private System.Windows.Forms.ToolStripMenuItem timeSinceFirstLoginToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lastSeenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem firstJoinToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mostRecentJoinToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mostRecentKickToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mostRecentPromotionDemotionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem blocksToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem builtToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deletedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem builtDeletedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem drawnToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem messagesWrittenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem totalTimeSpentToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem numberOfTimesVisitedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem numberOfTimesKickedToolStripMenuItem;
        private System.Windows.Forms.Button bDeleteGroup;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.GroupBox gEditGroup;
        private System.Windows.Forms.GroupBox gEditCondition;
        private System.Windows.Forms.ComboBox cGroupOp;
        private System.Windows.Forms.Button bDeleteCondition;
        private System.Windows.Forms.Label lGroupOp;
        private System.Windows.Forms.ComboBox cConditionField;
        private System.Windows.Forms.Label lConditionField;
        private System.Windows.Forms.Label lConditionValue;
        private System.Windows.Forms.ComboBox cConditionOp;
        private System.Windows.Forms.Label lConditionOp;
        private System.Windows.Forms.NumericUpDown nConditionValue;
        private System.Windows.Forms.Button bAction;
        private System.Windows.Forms.GroupBox gEditAction;
        private System.Windows.Forms.Button bDeleteAction;
        private System.Windows.Forms.Label lToRank;
        private System.Windows.Forms.Label lFromRank;
        private System.Windows.Forms.Label lActionType;
        private System.Windows.Forms.ComboBox cActionType;
        private System.Windows.Forms.TextBox tHelp;
        private System.Windows.Forms.ComboBox cFromRank;
        private System.Windows.Forms.ComboBox cToRank;
        private System.Windows.Forms.Button bOK;
        private System.Windows.Forms.Button bCancel;
        private System.Windows.Forms.Label lActionConnective;
        private System.Windows.Forms.ComboBox cActionConnective;
    }
}

