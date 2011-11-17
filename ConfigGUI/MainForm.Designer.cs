namespace fCraft.ConfigGUI {
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
            bold.Dispose();
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.tabs = new System.Windows.Forms.TabControl();
            this.tabGeneral = new System.Windows.Forms.TabPage();
            this.gUpdaterSettings = new System.Windows.Forms.GroupBox();
            this.bShowAdvancedUpdaterSettings = new System.Windows.Forms.Button();
            this.cUpdaterMode = new System.Windows.Forms.ComboBox();
            this.lUpdater = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.bChangelog = new System.Windows.Forms.Button();
            this.bCredits = new System.Windows.Forms.Button();
            this.bReadme = new System.Windows.Forms.Button();
            this.gHelpAndSupport = new System.Windows.Forms.GroupBox();
            this.bOpenWiki = new System.Windows.Forms.Button();
            this.bReportABug = new System.Windows.Forms.Button();
            this.gInformation = new System.Windows.Forms.GroupBox();
            this.bGreeting = new System.Windows.Forms.Button();
            this.lAnnouncementsUnits = new System.Windows.Forms.Label();
            this.nAnnouncements = new System.Windows.Forms.NumericUpDown();
            this.xAnnouncements = new System.Windows.Forms.CheckBox();
            this.bRules = new System.Windows.Forms.Button();
            this.bAnnouncements = new System.Windows.Forms.Button();
            this.gBasic = new System.Windows.Forms.GroupBox();
            this.nMaxPlayersPerWorld = new System.Windows.Forms.NumericUpDown();
            this.lMaxPlayersPerWorld = new System.Windows.Forms.Label();
            this.bPortCheck = new System.Windows.Forms.Button();
            this.lPort = new System.Windows.Forms.Label();
            this.nPort = new System.Windows.Forms.NumericUpDown();
            this.cDefaultRank = new System.Windows.Forms.ComboBox();
            this.lDefaultRank = new System.Windows.Forms.Label();
            this.lUploadBandwidth = new System.Windows.Forms.Label();
            this.bMeasure = new System.Windows.Forms.Button();
            this.tServerName = new System.Windows.Forms.TextBox();
            this.lUploadBandwidthUnits = new System.Windows.Forms.Label();
            this.lServerName = new System.Windows.Forms.Label();
            this.nUploadBandwidth = new System.Windows.Forms.NumericUpDown();
            this.tMOTD = new System.Windows.Forms.TextBox();
            this.lMOTD = new System.Windows.Forms.Label();
            this.cPublic = new System.Windows.Forms.ComboBox();
            this.nMaxPlayers = new System.Windows.Forms.NumericUpDown();
            this.lPublic = new System.Windows.Forms.Label();
            this.lMaxPlayers = new System.Windows.Forms.Label();
            this.tabChat = new System.Windows.Forms.TabPage();
            this.gChatColors = new System.Windows.Forms.GroupBox();
            this.lColorMe = new System.Windows.Forms.Label();
            this.bColorMe = new System.Windows.Forms.Button();
            this.lColorWarning = new System.Windows.Forms.Label();
            this.bColorWarning = new System.Windows.Forms.Button();
            this.bColorSys = new System.Windows.Forms.Button();
            this.lColorSys = new System.Windows.Forms.Label();
            this.bColorPM = new System.Windows.Forms.Button();
            this.lColorHelp = new System.Windows.Forms.Label();
            this.lColorPM = new System.Windows.Forms.Label();
            this.lColorSay = new System.Windows.Forms.Label();
            this.bColorAnnouncement = new System.Windows.Forms.Button();
            this.lColorAnnouncement = new System.Windows.Forms.Label();
            this.bColorHelp = new System.Windows.Forms.Button();
            this.bColorSay = new System.Windows.Forms.Button();
            this.gAppearence = new System.Windows.Forms.GroupBox();
            this.xShowConnectionMessages = new System.Windows.Forms.CheckBox();
            this.xShowJoinedWorldMessages = new System.Windows.Forms.CheckBox();
            this.xRankColorsInWorldNames = new System.Windows.Forms.CheckBox();
            this.xRankPrefixesInList = new System.Windows.Forms.CheckBox();
            this.xRankPrefixesInChat = new System.Windows.Forms.CheckBox();
            this.xRankColorsInChat = new System.Windows.Forms.CheckBox();
            this.chatPreview = new fCraft.ConfigGUI.ChatPreview();
            this.tabWorlds = new System.Windows.Forms.TabPage();
            this.xWoMEnableEnvExtensions = new System.Windows.Forms.CheckBox();
            this.bMapPath = new System.Windows.Forms.Button();
            this.xMapPath = new System.Windows.Forms.CheckBox();
            this.tMapPath = new System.Windows.Forms.TextBox();
            this.lDefaultBuildRank = new System.Windows.Forms.Label();
            this.cDefaultBuildRank = new System.Windows.Forms.ComboBox();
            this.cMainWorld = new System.Windows.Forms.ComboBox();
            this.lMainWorld = new System.Windows.Forms.Label();
            this.bWorldEdit = new System.Windows.Forms.Button();
            this.bAddWorld = new System.Windows.Forms.Button();
            this.bWorldDelete = new System.Windows.Forms.Button();
            this.dgvWorlds = new System.Windows.Forms.DataGridView();
            this.dgvcName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvcDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvcAccess = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dgvcBuild = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dgvcBackup = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dgvcHidden = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.dgvcBlockDB = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.tabRanks = new System.Windows.Forms.TabPage();
            this.gPermissionLimits = new System.Windows.Forms.GroupBox();
            this.permissionLimitBoxContainer = new System.Windows.Forms.FlowLayoutPanel();
            this.lRankList = new System.Windows.Forms.Label();
            this.bLowerRank = new System.Windows.Forms.Button();
            this.bRaiseRank = new System.Windows.Forms.Button();
            this.gRankOptions = new System.Windows.Forms.GroupBox();
            this.lFillLimitUnits = new System.Windows.Forms.Label();
            this.nFillLimit = new System.Windows.Forms.NumericUpDown();
            this.lFillLimit = new System.Windows.Forms.Label();
            this.nCopyPasteSlots = new System.Windows.Forms.NumericUpDown();
            this.lCopyPasteSlots = new System.Windows.Forms.Label();
            this.xAllowSecurityCircumvention = new System.Windows.Forms.CheckBox();
            this.lAntiGrief1 = new System.Windows.Forms.Label();
            this.lAntiGrief3 = new System.Windows.Forms.Label();
            this.nAntiGriefSeconds = new System.Windows.Forms.NumericUpDown();
            this.bColorRank = new System.Windows.Forms.Button();
            this.xDrawLimit = new System.Windows.Forms.CheckBox();
            this.lDrawLimitUnits = new System.Windows.Forms.Label();
            this.lKickIdleUnits = new System.Windows.Forms.Label();
            this.nDrawLimit = new System.Windows.Forms.NumericUpDown();
            this.nKickIdle = new System.Windows.Forms.NumericUpDown();
            this.xAntiGrief = new System.Windows.Forms.CheckBox();
            this.lAntiGrief2 = new System.Windows.Forms.Label();
            this.xKickIdle = new System.Windows.Forms.CheckBox();
            this.nAntiGriefBlocks = new System.Windows.Forms.NumericUpDown();
            this.xReserveSlot = new System.Windows.Forms.CheckBox();
            this.tPrefix = new System.Windows.Forms.TextBox();
            this.lPrefix = new System.Windows.Forms.Label();
            this.lRankColor = new System.Windows.Forms.Label();
            this.tRankName = new System.Windows.Forms.TextBox();
            this.lRankName = new System.Windows.Forms.Label();
            this.bDeleteRank = new System.Windows.Forms.Button();
            this.vPermissions = new System.Windows.Forms.ListView();
            this.chPermissions = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.bAddRank = new System.Windows.Forms.Button();
            this.lPermissions = new System.Windows.Forms.Label();
            this.vRanks = new System.Windows.Forms.ListBox();
            this.tabSecurity = new System.Windows.Forms.TabPage();
            this.gBlockDB = new System.Windows.Forms.GroupBox();
            this.cBlockDBAutoEnableRank = new System.Windows.Forms.ComboBox();
            this.xBlockDBAutoEnable = new System.Windows.Forms.CheckBox();
            this.xBlockDBEnabled = new System.Windows.Forms.CheckBox();
            this.gSecurityMisc = new System.Windows.Forms.GroupBox();
            this.xAnnounceRankChangeReasons = new System.Windows.Forms.CheckBox();
            this.xRequireKickReason = new System.Windows.Forms.CheckBox();
            this.xPaidPlayersOnly = new System.Windows.Forms.CheckBox();
            this.lPatrolledRankAndBelow = new System.Windows.Forms.Label();
            this.cPatrolledRank = new System.Windows.Forms.ComboBox();
            this.lPatrolledRank = new System.Windows.Forms.Label();
            this.xAnnounceRankChanges = new System.Windows.Forms.CheckBox();
            this.xAnnounceKickAndBanReasons = new System.Windows.Forms.CheckBox();
            this.xRequireRankChangeReason = new System.Windows.Forms.CheckBox();
            this.xRequireBanReason = new System.Windows.Forms.CheckBox();
            this.gSpamChat = new System.Windows.Forms.GroupBox();
            this.lAntispamMaxWarnings = new System.Windows.Forms.Label();
            this.nAntispamMaxWarnings = new System.Windows.Forms.NumericUpDown();
            this.xAntispamKicks = new System.Windows.Forms.CheckBox();
            this.lSpamMuteSeconds = new System.Windows.Forms.Label();
            this.lAntispamInterval = new System.Windows.Forms.Label();
            this.nSpamMute = new System.Windows.Forms.NumericUpDown();
            this.lSpamMute = new System.Windows.Forms.Label();
            this.nAntispamInterval = new System.Windows.Forms.NumericUpDown();
            this.lAntispamMessageCount = new System.Windows.Forms.Label();
            this.nAntispamMessageCount = new System.Windows.Forms.NumericUpDown();
            this.lSpamChat = new System.Windows.Forms.Label();
            this.gVerify = new System.Windows.Forms.GroupBox();
            this.nMaxConnectionsPerIP = new System.Windows.Forms.NumericUpDown();
            this.xAllowUnverifiedLAN = new System.Windows.Forms.CheckBox();
            this.xMaxConnectionsPerIP = new System.Windows.Forms.CheckBox();
            this.lVerifyNames = new System.Windows.Forms.Label();
            this.cVerifyNames = new System.Windows.Forms.ComboBox();
            this.tabSavingAndBackup = new System.Windows.Forms.TabPage();
            this.gDataBackup = new System.Windows.Forms.GroupBox();
            this.xBackupDataOnStartup = new System.Windows.Forms.CheckBox();
            this.gSaving = new System.Windows.Forms.GroupBox();
            this.nSaveInterval = new System.Windows.Forms.NumericUpDown();
            this.lSaveIntervalUnits = new System.Windows.Forms.Label();
            this.xSaveInterval = new System.Windows.Forms.CheckBox();
            this.gBackups = new System.Windows.Forms.GroupBox();
            this.xBackupOnlyWhenChanged = new System.Windows.Forms.CheckBox();
            this.lMaxBackupSize = new System.Windows.Forms.Label();
            this.xMaxBackupSize = new System.Windows.Forms.CheckBox();
            this.nMaxBackupSize = new System.Windows.Forms.NumericUpDown();
            this.xMaxBackups = new System.Windows.Forms.CheckBox();
            this.xBackupOnStartup = new System.Windows.Forms.CheckBox();
            this.lMaxBackups = new System.Windows.Forms.Label();
            this.nMaxBackups = new System.Windows.Forms.NumericUpDown();
            this.nBackupInterval = new System.Windows.Forms.NumericUpDown();
            this.lBackupIntervalUnits = new System.Windows.Forms.Label();
            this.xBackupInterval = new System.Windows.Forms.CheckBox();
            this.xBackupOnJoin = new System.Windows.Forms.CheckBox();
            this.tabLogging = new System.Windows.Forms.TabPage();
            this.gLogFile = new System.Windows.Forms.GroupBox();
            this.lLogFileOptionsDescription = new System.Windows.Forms.Label();
            this.xLogLimit = new System.Windows.Forms.CheckBox();
            this.vLogFileOptions = new System.Windows.Forms.ListView();
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lLogLimitUnits = new System.Windows.Forms.Label();
            this.nLogLimit = new System.Windows.Forms.NumericUpDown();
            this.cLogMode = new System.Windows.Forms.ComboBox();
            this.lLogMode = new System.Windows.Forms.Label();
            this.gConsole = new System.Windows.Forms.GroupBox();
            this.lLogConsoleOptionsDescription = new System.Windows.Forms.Label();
            this.vConsoleOptions = new System.Windows.Forms.ListView();
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabIRC = new System.Windows.Forms.TabPage();
            this.xIRCListShowNonEnglish = new System.Windows.Forms.CheckBox();
            this.gIRCOptions = new System.Windows.Forms.GroupBox();
            this.xIRCBotAnnounceServerEvents = new System.Windows.Forms.CheckBox();
            this.xIRCUseColor = new System.Windows.Forms.CheckBox();
            this.lIRCNoForwardingMessage = new System.Windows.Forms.Label();
            this.xIRCBotAnnounceIRCJoins = new System.Windows.Forms.CheckBox();
            this.bColorIRC = new System.Windows.Forms.Button();
            this.lColorIRC = new System.Windows.Forms.Label();
            this.xIRCBotForwardFromIRC = new System.Windows.Forms.CheckBox();
            this.xIRCBotAnnounceServerJoins = new System.Windows.Forms.CheckBox();
            this.xIRCBotForwardFromServer = new System.Windows.Forms.CheckBox();
            this.gIRCNetwork = new System.Windows.Forms.GroupBox();
            this.lIRCDelayUnits = new System.Windows.Forms.Label();
            this.xIRCRegisteredNick = new System.Windows.Forms.CheckBox();
            this.tIRCNickServMessage = new System.Windows.Forms.TextBox();
            this.lIRCNickServMessage = new System.Windows.Forms.Label();
            this.tIRCNickServ = new System.Windows.Forms.TextBox();
            this.lIRCNickServ = new System.Windows.Forms.Label();
            this.nIRCDelay = new System.Windows.Forms.NumericUpDown();
            this.lIRCDelay = new System.Windows.Forms.Label();
            this.lIRCBotChannels2 = new System.Windows.Forms.Label();
            this.lIRCBotChannels3 = new System.Windows.Forms.Label();
            this.tIRCBotChannels = new System.Windows.Forms.TextBox();
            this.lIRCBotChannels = new System.Windows.Forms.Label();
            this.nIRCBotPort = new System.Windows.Forms.NumericUpDown();
            this.lIRCBotPort = new System.Windows.Forms.Label();
            this.tIRCBotNetwork = new System.Windows.Forms.TextBox();
            this.lIRCBotNetwork = new System.Windows.Forms.Label();
            this.lIRCBotNick = new System.Windows.Forms.Label();
            this.tIRCBotNick = new System.Windows.Forms.TextBox();
            this.lIRCList = new System.Windows.Forms.Label();
            this.xIRCBotEnabled = new System.Windows.Forms.CheckBox();
            this.cIRCList = new System.Windows.Forms.ComboBox();
            this.tabAdvanced = new System.Windows.Forms.TabPage();
            this.gPerformance = new System.Windows.Forms.GroupBox();
            this.lAdvancedWarning = new System.Windows.Forms.Label();
            this.xLowLatencyMode = new System.Windows.Forms.CheckBox();
            this.lProcessPriority = new System.Windows.Forms.Label();
            this.cProcessPriority = new System.Windows.Forms.ComboBox();
            this.nTickInterval = new System.Windows.Forms.NumericUpDown();
            this.lTickIntervalUnits = new System.Windows.Forms.Label();
            this.lTickInterval = new System.Windows.Forms.Label();
            this.nThrottling = new System.Windows.Forms.NumericUpDown();
            this.lThrottling = new System.Windows.Forms.Label();
            this.lThrottlingUnits = new System.Windows.Forms.Label();
            this.gAdvancedMisc = new System.Windows.Forms.GroupBox();
            this.nMaxUndoStates = new System.Windows.Forms.NumericUpDown();
            this.lMaxUndoStates = new System.Windows.Forms.Label();
            this.xHeartbeatToWoMDirect = new System.Windows.Forms.CheckBox();
            this.lIPWarning = new System.Windows.Forms.Label();
            this.tIP = new System.Windows.Forms.TextBox();
            this.xIP = new System.Windows.Forms.CheckBox();
            this.lConsoleName = new System.Windows.Forms.Label();
            this.tConsoleName = new System.Windows.Forms.TextBox();
            this.nMaxUndo = new System.Windows.Forms.NumericUpDown();
            this.lMaxUndoUnits = new System.Windows.Forms.Label();
            this.xMaxUndo = new System.Windows.Forms.CheckBox();
            this.xRelayAllBlockUpdates = new System.Windows.Forms.CheckBox();
            this.xNoPartialPositionUpdates = new System.Windows.Forms.CheckBox();
            this.gCrashReport = new System.Windows.Forms.GroupBox();
            this.lCrashReportDisclaimer = new System.Windows.Forms.Label();
            this.xSubmitCrashReports = new System.Windows.Forms.CheckBox();
            this.bOK = new System.Windows.Forms.Button();
            this.bCancel = new System.Windows.Forms.Button();
            this.bResetTab = new System.Windows.Forms.Button();
            this.bResetAll = new System.Windows.Forms.Button();
            this.bApply = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.tabs.SuspendLayout();
            this.tabGeneral.SuspendLayout();
            this.gUpdaterSettings.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.gHelpAndSupport.SuspendLayout();
            this.gInformation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nAnnouncements)).BeginInit();
            this.gBasic.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxPlayersPerWorld)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nUploadBandwidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxPlayers)).BeginInit();
            this.tabChat.SuspendLayout();
            this.gChatColors.SuspendLayout();
            this.gAppearence.SuspendLayout();
            this.tabWorlds.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvWorlds)).BeginInit();
            this.tabRanks.SuspendLayout();
            this.gPermissionLimits.SuspendLayout();
            this.gRankOptions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nFillLimit)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nCopyPasteSlots)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nAntiGriefSeconds)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nDrawLimit)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nKickIdle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nAntiGriefBlocks)).BeginInit();
            this.tabSecurity.SuspendLayout();
            this.gBlockDB.SuspendLayout();
            this.gSecurityMisc.SuspendLayout();
            this.gSpamChat.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nAntispamMaxWarnings)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamMute)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nAntispamInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nAntispamMessageCount)).BeginInit();
            this.gVerify.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxConnectionsPerIP)).BeginInit();
            this.tabSavingAndBackup.SuspendLayout();
            this.gDataBackup.SuspendLayout();
            this.gSaving.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nSaveInterval)).BeginInit();
            this.gBackups.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxBackupSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxBackups)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nBackupInterval)).BeginInit();
            this.tabLogging.SuspendLayout();
            this.gLogFile.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nLogLimit)).BeginInit();
            this.gConsole.SuspendLayout();
            this.tabIRC.SuspendLayout();
            this.gIRCOptions.SuspendLayout();
            this.gIRCNetwork.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nIRCDelay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nIRCBotPort)).BeginInit();
            this.tabAdvanced.SuspendLayout();
            this.gPerformance.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nTickInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nThrottling)).BeginInit();
            this.gAdvancedMisc.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxUndoStates)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxUndo)).BeginInit();
            this.gCrashReport.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabs
            // 
            this.tabs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabs.Controls.Add(this.tabGeneral);
            this.tabs.Controls.Add(this.tabChat);
            this.tabs.Controls.Add(this.tabWorlds);
            this.tabs.Controls.Add(this.tabRanks);
            this.tabs.Controls.Add(this.tabSecurity);
            this.tabs.Controls.Add(this.tabSavingAndBackup);
            this.tabs.Controls.Add(this.tabLogging);
            this.tabs.Controls.Add(this.tabIRC);
            this.tabs.Controls.Add(this.tabAdvanced);
            this.tabs.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabs.Location = new System.Drawing.Point(16, 15);
            this.tabs.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabs.Name = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new System.Drawing.Size(880, 628);
            this.tabs.TabIndex = 0;
            // 
            // tabGeneral
            // 
            this.tabGeneral.Controls.Add(this.gUpdaterSettings);
            this.tabGeneral.Controls.Add(this.groupBox2);
            this.tabGeneral.Controls.Add(this.gHelpAndSupport);
            this.tabGeneral.Controls.Add(this.gInformation);
            this.tabGeneral.Controls.Add(this.gBasic);
            this.tabGeneral.Location = new System.Drawing.Point(4, 27);
            this.tabGeneral.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.Padding = new System.Windows.Forms.Padding(7, 12, 7, 12);
            this.tabGeneral.Size = new System.Drawing.Size(872, 597);
            this.tabGeneral.TabIndex = 0;
            this.tabGeneral.Text = "General";
            this.tabGeneral.UseVisualStyleBackColor = true;
            // 
            // gUpdaterSettings
            // 
            this.gUpdaterSettings.Controls.Add(this.bShowAdvancedUpdaterSettings);
            this.gUpdaterSettings.Controls.Add(this.cUpdaterMode);
            this.gUpdaterSettings.Controls.Add(this.lUpdater);
            this.gUpdaterSettings.Location = new System.Drawing.Point(11, 304);
            this.gUpdaterSettings.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gUpdaterSettings.Name = "gUpdaterSettings";
            this.gUpdaterSettings.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gUpdaterSettings.Size = new System.Drawing.Size(848, 66);
            this.gUpdaterSettings.TabIndex = 2;
            this.gUpdaterSettings.TabStop = false;
            this.gUpdaterSettings.Text = "Updater Settings";
            // 
            // bShowAdvancedUpdaterSettings
            // 
            this.bShowAdvancedUpdaterSettings.Location = new System.Drawing.Point(424, 27);
            this.bShowAdvancedUpdaterSettings.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bShowAdvancedUpdaterSettings.Name = "bShowAdvancedUpdaterSettings";
            this.bShowAdvancedUpdaterSettings.Size = new System.Drawing.Size(100, 28);
            this.bShowAdvancedUpdaterSettings.TabIndex = 2;
            this.bShowAdvancedUpdaterSettings.Text = "Advanced";
            this.bShowAdvancedUpdaterSettings.UseVisualStyleBackColor = true;
            this.bShowAdvancedUpdaterSettings.Click += new System.EventHandler(this.bShowAdvancedUpdaterSettings_Click);
            // 
            // cUpdaterMode
            // 
            this.cUpdaterMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cUpdaterMode.FormattingEnabled = true;
            this.cUpdaterMode.Items.AddRange(new object[] {
            "Disabled",
            "Notify about availability",
            "Prompt to install",
            "Fully automatic"});
            this.cUpdaterMode.Location = new System.Drawing.Point(164, 27);
            this.cUpdaterMode.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cUpdaterMode.Name = "cUpdaterMode";
            this.cUpdaterMode.Size = new System.Drawing.Size(251, 26);
            this.cUpdaterMode.TabIndex = 1;
            this.cUpdaterMode.SelectedIndexChanged += new System.EventHandler(this.cUpdaterMode_SelectedIndexChanged);
            // 
            // lUpdater
            // 
            this.lUpdater.AutoSize = true;
            this.lUpdater.Location = new System.Drawing.Point(8, 31);
            this.lUpdater.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lUpdater.Name = "lUpdater";
            this.lUpdater.Size = new System.Drawing.Size(153, 18);
            this.lUpdater.TabIndex = 0;
            this.lUpdater.Text = "800craft update check";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBox2.Controls.Add(this.bChangelog);
            this.groupBox2.Controls.Add(this.bCredits);
            this.groupBox2.Controls.Add(this.bReadme);
            this.groupBox2.Location = new System.Drawing.Point(439, 508);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox2.Size = new System.Drawing.Size(420, 68);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "About 800craft";
            // 
            // bChangelog
            // 
            this.bChangelog.Location = new System.Drawing.Point(143, 25);
            this.bChangelog.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bChangelog.Name = "bChangelog";
            this.bChangelog.Size = new System.Drawing.Size(127, 28);
            this.bChangelog.TabIndex = 2;
            this.bChangelog.Text = "Changelog";
            this.bChangelog.UseVisualStyleBackColor = true;
            this.bChangelog.Click += new System.EventHandler(this.bChangelog_Click);
            // 
            // bCredits
            // 
            this.bCredits.Location = new System.Drawing.Point(277, 25);
            this.bCredits.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bCredits.Name = "bCredits";
            this.bCredits.Size = new System.Drawing.Size(127, 28);
            this.bCredits.TabIndex = 1;
            this.bCredits.Text = "Credits";
            this.bCredits.UseVisualStyleBackColor = true;
            this.bCredits.Click += new System.EventHandler(this.bCredits_Click);
            // 
            // bReadme
            // 
            this.bReadme.Enabled = false;
            this.bReadme.Location = new System.Drawing.Point(8, 25);
            this.bReadme.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bReadme.Name = "bReadme";
            this.bReadme.Size = new System.Drawing.Size(127, 28);
            this.bReadme.TabIndex = 0;
            this.bReadme.Text = "Readme";
            this.bReadme.UseVisualStyleBackColor = true;
            this.bReadme.Click += new System.EventHandler(this.bReadme_Click);
            // 
            // gHelpAndSupport
            // 
            this.gHelpAndSupport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.gHelpAndSupport.Controls.Add(this.bOpenWiki);
            this.gHelpAndSupport.Controls.Add(this.bReportABug);
            this.gHelpAndSupport.Location = new System.Drawing.Point(11, 508);
            this.gHelpAndSupport.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gHelpAndSupport.Name = "gHelpAndSupport";
            this.gHelpAndSupport.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gHelpAndSupport.Size = new System.Drawing.Size(420, 68);
            this.gHelpAndSupport.TabIndex = 3;
            this.gHelpAndSupport.TabStop = false;
            this.gHelpAndSupport.Text = "Help and Support";
            // 
            // bOpenWiki
            // 
            this.bOpenWiki.Location = new System.Drawing.Point(12, 25);
            this.bOpenWiki.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bOpenWiki.Name = "bOpenWiki";
            this.bOpenWiki.Size = new System.Drawing.Size(187, 28);
            this.bOpenWiki.TabIndex = 0;
            this.bOpenWiki.Text = "Open fCraft Wiki";
            this.bOpenWiki.UseVisualStyleBackColor = true;
            this.bOpenWiki.Click += new System.EventHandler(this.bOpenWiki_Click);
            // 
            // bReportABug
            // 
            this.bReportABug.Location = new System.Drawing.Point(207, 25);
            this.bReportABug.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bReportABug.Name = "bReportABug";
            this.bReportABug.Size = new System.Drawing.Size(187, 28);
            this.bReportABug.TabIndex = 1;
            this.bReportABug.Text = "Report a Bug";
            this.bReportABug.UseVisualStyleBackColor = true;
            this.bReportABug.Click += new System.EventHandler(this.bReportABug_Click);
            // 
            // gInformation
            // 
            this.gInformation.Controls.Add(this.bGreeting);
            this.gInformation.Controls.Add(this.lAnnouncementsUnits);
            this.gInformation.Controls.Add(this.nAnnouncements);
            this.gInformation.Controls.Add(this.xAnnouncements);
            this.gInformation.Controls.Add(this.bRules);
            this.gInformation.Controls.Add(this.bAnnouncements);
            this.gInformation.Location = new System.Drawing.Point(11, 226);
            this.gInformation.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gInformation.Name = "gInformation";
            this.gInformation.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gInformation.Size = new System.Drawing.Size(848, 70);
            this.gInformation.TabIndex = 1;
            this.gInformation.TabStop = false;
            this.gInformation.Text = "Information";
            // 
            // bGreeting
            // 
            this.bGreeting.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bGreeting.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bGreeting.Location = new System.Drawing.Point(717, 25);
            this.bGreeting.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bGreeting.Name = "bGreeting";
            this.bGreeting.Size = new System.Drawing.Size(123, 34);
            this.bGreeting.TabIndex = 5;
            this.bGreeting.Text = "Edit Greeting";
            this.bGreeting.UseVisualStyleBackColor = true;
            this.bGreeting.Click += new System.EventHandler(this.bGreeting_Click);
            // 
            // lAnnouncementsUnits
            // 
            this.lAnnouncementsUnits.AutoSize = true;
            this.lAnnouncementsUnits.Location = new System.Drawing.Point(355, 33);
            this.lAnnouncementsUnits.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lAnnouncementsUnits.Name = "lAnnouncementsUnits";
            this.lAnnouncementsUnits.Size = new System.Drawing.Size(32, 18);
            this.lAnnouncementsUnits.TabIndex = 2;
            this.lAnnouncementsUnits.Text = "min";
            // 
            // nAnnouncements
            // 
            this.nAnnouncements.Enabled = false;
            this.nAnnouncements.Location = new System.Drawing.Point(280, 31);
            this.nAnnouncements.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nAnnouncements.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.nAnnouncements.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nAnnouncements.Name = "nAnnouncements";
            this.nAnnouncements.Size = new System.Drawing.Size(67, 24);
            this.nAnnouncements.TabIndex = 1;
            this.nAnnouncements.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // xAnnouncements
            // 
            this.xAnnouncements.AutoSize = true;
            this.xAnnouncements.Location = new System.Drawing.Point(32, 32);
            this.xAnnouncements.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xAnnouncements.Name = "xAnnouncements";
            this.xAnnouncements.Size = new System.Drawing.Size(217, 22);
            this.xAnnouncements.TabIndex = 0;
            this.xAnnouncements.Text = "Show announcements every";
            this.xAnnouncements.UseVisualStyleBackColor = true;
            this.xAnnouncements.CheckedChanged += new System.EventHandler(this.xAnnouncements_CheckedChanged);
            // 
            // bRules
            // 
            this.bRules.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bRules.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bRules.Location = new System.Drawing.Point(593, 25);
            this.bRules.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bRules.Name = "bRules";
            this.bRules.Size = new System.Drawing.Size(116, 34);
            this.bRules.TabIndex = 4;
            this.bRules.Text = "Edit Rules";
            this.bRules.UseVisualStyleBackColor = true;
            this.bRules.Click += new System.EventHandler(this.bRules_Click);
            // 
            // bAnnouncements
            // 
            this.bAnnouncements.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bAnnouncements.Enabled = false;
            this.bAnnouncements.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bAnnouncements.Location = new System.Drawing.Point(401, 25);
            this.bAnnouncements.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bAnnouncements.Name = "bAnnouncements";
            this.bAnnouncements.Size = new System.Drawing.Size(184, 34);
            this.bAnnouncements.TabIndex = 3;
            this.bAnnouncements.Text = "Edit Announcements";
            this.bAnnouncements.UseVisualStyleBackColor = true;
            this.bAnnouncements.Click += new System.EventHandler(this.bAnnouncements_Click);
            // 
            // gBasic
            // 
            this.gBasic.Controls.Add(this.nMaxPlayersPerWorld);
            this.gBasic.Controls.Add(this.lMaxPlayersPerWorld);
            this.gBasic.Controls.Add(this.bPortCheck);
            this.gBasic.Controls.Add(this.lPort);
            this.gBasic.Controls.Add(this.nPort);
            this.gBasic.Controls.Add(this.cDefaultRank);
            this.gBasic.Controls.Add(this.lDefaultRank);
            this.gBasic.Controls.Add(this.lUploadBandwidth);
            this.gBasic.Controls.Add(this.bMeasure);
            this.gBasic.Controls.Add(this.tServerName);
            this.gBasic.Controls.Add(this.lUploadBandwidthUnits);
            this.gBasic.Controls.Add(this.lServerName);
            this.gBasic.Controls.Add(this.nUploadBandwidth);
            this.gBasic.Controls.Add(this.tMOTD);
            this.gBasic.Controls.Add(this.lMOTD);
            this.gBasic.Controls.Add(this.cPublic);
            this.gBasic.Controls.Add(this.nMaxPlayers);
            this.gBasic.Controls.Add(this.lPublic);
            this.gBasic.Controls.Add(this.lMaxPlayers);
            this.gBasic.Location = new System.Drawing.Point(11, 16);
            this.gBasic.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gBasic.Name = "gBasic";
            this.gBasic.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gBasic.Size = new System.Drawing.Size(848, 203);
            this.gBasic.TabIndex = 0;
            this.gBasic.TabStop = false;
            this.gBasic.Text = "Basic Settings";
            // 
            // nMaxPlayersPerWorld
            // 
            this.nMaxPlayersPerWorld.Location = new System.Drawing.Point(587, 91);
            this.nMaxPlayersPerWorld.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nMaxPlayersPerWorld.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
            this.nMaxPlayersPerWorld.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nMaxPlayersPerWorld.Name = "nMaxPlayersPerWorld";
            this.nMaxPlayersPerWorld.Size = new System.Drawing.Size(100, 24);
            this.nMaxPlayersPerWorld.TabIndex = 12;
            this.nMaxPlayersPerWorld.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nMaxPlayersPerWorld.Validating += new System.ComponentModel.CancelEventHandler(this.nMaxPlayerPerWorld_Validating);
            // 
            // lMaxPlayersPerWorld
            // 
            this.lMaxPlayersPerWorld.AutoSize = true;
            this.lMaxPlayersPerWorld.Location = new System.Drawing.Point(399, 94);
            this.lMaxPlayersPerWorld.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lMaxPlayersPerWorld.Name = "lMaxPlayersPerWorld";
            this.lMaxPlayersPerWorld.Size = new System.Drawing.Size(162, 18);
            this.lMaxPlayersPerWorld.TabIndex = 11;
            this.lMaxPlayersPerWorld.Text = "Max players (per world)";
            // 
            // bPortCheck
            // 
            this.bPortCheck.Location = new System.Drawing.Point(272, 122);
            this.bPortCheck.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bPortCheck.Name = "bPortCheck";
            this.bPortCheck.Size = new System.Drawing.Size(91, 28);
            this.bPortCheck.TabIndex = 8;
            this.bPortCheck.Text = "Check";
            this.bPortCheck.UseVisualStyleBackColor = true;
            this.bPortCheck.Click += new System.EventHandler(this.bPortCheck_Click);
            // 
            // lPort
            // 
            this.lPort.AutoSize = true;
            this.lPort.Location = new System.Drawing.Point(56, 127);
            this.lPort.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lPort.Name = "lPort";
            this.lPort.Size = new System.Drawing.Size(90, 18);
            this.lPort.TabIndex = 6;
            this.lPort.Text = "Port number";
            // 
            // nPort
            // 
            this.nPort.Location = new System.Drawing.Point(164, 124);
            this.nPort.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nPort.Name = "nPort";
            this.nPort.Size = new System.Drawing.Size(100, 24);
            this.nPort.TabIndex = 7;
            this.nPort.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // cDefaultRank
            // 
            this.cDefaultRank.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cDefaultRank.FormattingEnabled = true;
            this.cDefaultRank.Location = new System.Drawing.Point(587, 158);
            this.cDefaultRank.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cDefaultRank.Name = "cDefaultRank";
            this.cDefaultRank.Size = new System.Drawing.Size(225, 26);
            this.cDefaultRank.TabIndex = 18;
            this.cDefaultRank.SelectedIndexChanged += new System.EventHandler(this.cDefaultRank_SelectedIndexChanged);
            // 
            // lDefaultRank
            // 
            this.lDefaultRank.AutoSize = true;
            this.lDefaultRank.Location = new System.Drawing.Point(481, 161);
            this.lDefaultRank.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lDefaultRank.Name = "lDefaultRank";
            this.lDefaultRank.Size = new System.Drawing.Size(87, 18);
            this.lDefaultRank.TabIndex = 17;
            this.lDefaultRank.Text = "Default rank";
            // 
            // lUploadBandwidth
            // 
            this.lUploadBandwidth.AutoSize = true;
            this.lUploadBandwidth.Location = new System.Drawing.Point(436, 127);
            this.lUploadBandwidth.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lUploadBandwidth.Name = "lUploadBandwidth";
            this.lUploadBandwidth.Size = new System.Drawing.Size(125, 18);
            this.lUploadBandwidth.TabIndex = 13;
            this.lUploadBandwidth.Text = "Upload bandwidth";
            // 
            // bMeasure
            // 
            this.bMeasure.Location = new System.Drawing.Point(745, 122);
            this.bMeasure.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bMeasure.Name = "bMeasure";
            this.bMeasure.Size = new System.Drawing.Size(95, 28);
            this.bMeasure.TabIndex = 16;
            this.bMeasure.Text = "Measure";
            this.bMeasure.UseVisualStyleBackColor = true;
            this.bMeasure.Click += new System.EventHandler(this.bMeasure_Click);
            // 
            // tServerName
            // 
            this.tServerName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tServerName.HideSelection = false;
            this.tServerName.Location = new System.Drawing.Point(164, 25);
            this.tServerName.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tServerName.MaxLength = 64;
            this.tServerName.Name = "tServerName";
            this.tServerName.Size = new System.Drawing.Size(675, 24);
            this.tServerName.TabIndex = 1;
            // 
            // lUploadBandwidthUnits
            // 
            this.lUploadBandwidthUnits.AutoSize = true;
            this.lUploadBandwidthUnits.Location = new System.Drawing.Point(695, 127);
            this.lUploadBandwidthUnits.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lUploadBandwidthUnits.Name = "lUploadBandwidthUnits";
            this.lUploadBandwidthUnits.Size = new System.Drawing.Size(40, 18);
            this.lUploadBandwidthUnits.TabIndex = 15;
            this.lUploadBandwidthUnits.Text = "KB/s";
            // 
            // lServerName
            // 
            this.lServerName.AutoSize = true;
            this.lServerName.Location = new System.Drawing.Point(53, 28);
            this.lServerName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lServerName.Name = "lServerName";
            this.lServerName.Size = new System.Drawing.Size(92, 18);
            this.lServerName.TabIndex = 0;
            this.lServerName.Text = "Server name";
            // 
            // nUploadBandwidth
            // 
            this.nUploadBandwidth.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nUploadBandwidth.Location = new System.Drawing.Point(587, 124);
            this.nUploadBandwidth.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nUploadBandwidth.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nUploadBandwidth.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nUploadBandwidth.Name = "nUploadBandwidth";
            this.nUploadBandwidth.Size = new System.Drawing.Size(100, 24);
            this.nUploadBandwidth.TabIndex = 14;
            this.nUploadBandwidth.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // tMOTD
            // 
            this.tMOTD.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tMOTD.Location = new System.Drawing.Point(164, 58);
            this.tMOTD.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tMOTD.MaxLength = 64;
            this.tMOTD.Name = "tMOTD";
            this.tMOTD.Size = new System.Drawing.Size(675, 24);
            this.tMOTD.TabIndex = 3;
            // 
            // lMOTD
            // 
            this.lMOTD.AutoSize = true;
            this.lMOTD.Location = new System.Drawing.Point(99, 62);
            this.lMOTD.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lMOTD.Name = "lMOTD";
            this.lMOTD.Size = new System.Drawing.Size(53, 18);
            this.lMOTD.TabIndex = 2;
            this.lMOTD.Text = "MOTD";
            // 
            // cPublic
            // 
            this.cPublic.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cPublic.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cPublic.FormattingEnabled = true;
            this.cPublic.Items.AddRange(new object[] {
            "Public",
            "Private"});
            this.cPublic.Location = new System.Drawing.Point(164, 158);
            this.cPublic.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cPublic.Name = "cPublic";
            this.cPublic.Size = new System.Drawing.Size(99, 26);
            this.cPublic.TabIndex = 10;
            // 
            // nMaxPlayers
            // 
            this.nMaxPlayers.Location = new System.Drawing.Point(164, 91);
            this.nMaxPlayers.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nMaxPlayers.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nMaxPlayers.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nMaxPlayers.Name = "nMaxPlayers";
            this.nMaxPlayers.Size = new System.Drawing.Size(100, 24);
            this.nMaxPlayers.TabIndex = 5;
            this.nMaxPlayers.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nMaxPlayers.ValueChanged += new System.EventHandler(this.nMaxPlayers_ValueChanged);
            // 
            // lPublic
            // 
            this.lPublic.AutoSize = true;
            this.lPublic.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lPublic.Location = new System.Drawing.Point(19, 161);
            this.lPublic.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lPublic.Name = "lPublic";
            this.lPublic.Size = new System.Drawing.Size(121, 18);
            this.lPublic.TabIndex = 9;
            this.lPublic.Text = "Server visibility";
            // 
            // lMaxPlayers
            // 
            this.lMaxPlayers.AutoSize = true;
            this.lMaxPlayers.Location = new System.Drawing.Point(13, 94);
            this.lMaxPlayers.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lMaxPlayers.Name = "lMaxPlayers";
            this.lMaxPlayers.Size = new System.Drawing.Size(129, 18);
            this.lMaxPlayers.TabIndex = 4;
            this.lMaxPlayers.Text = "Max players (total)";
            // 
            // tabChat
            // 
            this.tabChat.Controls.Add(this.gChatColors);
            this.tabChat.Controls.Add(this.gAppearence);
            this.tabChat.Controls.Add(this.chatPreview);
            this.tabChat.Location = new System.Drawing.Point(4, 27);
            this.tabChat.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabChat.Name = "tabChat";
            this.tabChat.Padding = new System.Windows.Forms.Padding(7, 6, 7, 12);
            this.tabChat.Size = new System.Drawing.Size(872, 597);
            this.tabChat.TabIndex = 10;
            this.tabChat.Text = "Chat";
            this.tabChat.UseVisualStyleBackColor = true;
            // 
            // gChatColors
            // 
            this.gChatColors.Controls.Add(this.lColorMe);
            this.gChatColors.Controls.Add(this.bColorMe);
            this.gChatColors.Controls.Add(this.lColorWarning);
            this.gChatColors.Controls.Add(this.bColorWarning);
            this.gChatColors.Controls.Add(this.bColorSys);
            this.gChatColors.Controls.Add(this.lColorSys);
            this.gChatColors.Controls.Add(this.bColorPM);
            this.gChatColors.Controls.Add(this.lColorHelp);
            this.gChatColors.Controls.Add(this.lColorPM);
            this.gChatColors.Controls.Add(this.lColorSay);
            this.gChatColors.Controls.Add(this.bColorAnnouncement);
            this.gChatColors.Controls.Add(this.lColorAnnouncement);
            this.gChatColors.Controls.Add(this.bColorHelp);
            this.gChatColors.Controls.Add(this.bColorSay);
            this.gChatColors.Location = new System.Drawing.Point(11, 10);
            this.gChatColors.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gChatColors.Name = "gChatColors";
            this.gChatColors.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gChatColors.Size = new System.Drawing.Size(848, 171);
            this.gChatColors.TabIndex = 0;
            this.gChatColors.TabStop = false;
            this.gChatColors.Text = "Colors";
            // 
            // lColorMe
            // 
            this.lColorMe.AutoSize = true;
            this.lColorMe.Location = new System.Drawing.Point(536, 101);
            this.lColorMe.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lColorMe.Name = "lColorMe";
            this.lColorMe.Size = new System.Drawing.Size(142, 18);
            this.lColorMe.TabIndex = 12;
            this.lColorMe.Text = "/Me command color";
            // 
            // bColorMe
            // 
            this.bColorMe.BackColor = System.Drawing.Color.White;
            this.bColorMe.Location = new System.Drawing.Point(700, 96);
            this.bColorMe.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bColorMe.Name = "bColorMe";
            this.bColorMe.Size = new System.Drawing.Size(133, 28);
            this.bColorMe.TabIndex = 13;
            this.bColorMe.UseVisualStyleBackColor = false;
            this.bColorMe.Click += new System.EventHandler(this.bColorMe_Click);
            // 
            // lColorWarning
            // 
            this.lColorWarning.AutoSize = true;
            this.lColorWarning.Location = new System.Drawing.Point(92, 65);
            this.lColorWarning.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lColorWarning.Name = "lColorWarning";
            this.lColorWarning.Size = new System.Drawing.Size(145, 18);
            this.lColorWarning.TabIndex = 2;
            this.lColorWarning.Text = "Warning / error color";
            // 
            // bColorWarning
            // 
            this.bColorWarning.BackColor = System.Drawing.Color.White;
            this.bColorWarning.Location = new System.Drawing.Point(257, 60);
            this.bColorWarning.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bColorWarning.Name = "bColorWarning";
            this.bColorWarning.Size = new System.Drawing.Size(133, 28);
            this.bColorWarning.TabIndex = 3;
            this.bColorWarning.UseVisualStyleBackColor = false;
            this.bColorWarning.Click += new System.EventHandler(this.bColorWarning_Click);
            // 
            // bColorSys
            // 
            this.bColorSys.BackColor = System.Drawing.Color.White;
            this.bColorSys.Location = new System.Drawing.Point(257, 25);
            this.bColorSys.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bColorSys.Name = "bColorSys";
            this.bColorSys.Size = new System.Drawing.Size(133, 28);
            this.bColorSys.TabIndex = 1;
            this.bColorSys.UseVisualStyleBackColor = false;
            this.bColorSys.Click += new System.EventHandler(this.bColorSys_Click);
            // 
            // lColorSys
            // 
            this.lColorSys.AutoSize = true;
            this.lColorSys.Location = new System.Drawing.Point(75, 30);
            this.lColorSys.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lColorSys.Name = "lColorSys";
            this.lColorSys.Size = new System.Drawing.Size(161, 18);
            this.lColorSys.TabIndex = 0;
            this.lColorSys.Text = "System message color";
            // 
            // bColorPM
            // 
            this.bColorPM.BackColor = System.Drawing.Color.White;
            this.bColorPM.Location = new System.Drawing.Point(257, 96);
            this.bColorPM.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bColorPM.Name = "bColorPM";
            this.bColorPM.Size = new System.Drawing.Size(133, 28);
            this.bColorPM.TabIndex = 5;
            this.bColorPM.UseVisualStyleBackColor = false;
            this.bColorPM.Click += new System.EventHandler(this.bColorPM_Click);
            // 
            // lColorHelp
            // 
            this.lColorHelp.AutoSize = true;
            this.lColorHelp.Location = new System.Drawing.Point(93, 137);
            this.lColorHelp.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lColorHelp.Name = "lColorHelp";
            this.lColorHelp.Size = new System.Drawing.Size(141, 18);
            this.lColorHelp.TabIndex = 6;
            this.lColorHelp.Text = "Help message color";
            // 
            // lColorPM
            // 
            this.lColorPM.AutoSize = true;
            this.lColorPM.Location = new System.Drawing.Point(35, 101);
            this.lColorPM.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lColorPM.Name = "lColorPM";
            this.lColorPM.Size = new System.Drawing.Size(197, 18);
            this.lColorPM.TabIndex = 4;
            this.lColorPM.Text = "Private / rank message color";
            // 
            // lColorSay
            // 
            this.lColorSay.AutoSize = true;
            this.lColorSay.Location = new System.Drawing.Point(543, 65);
            this.lColorSay.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lColorSay.Name = "lColorSay";
            this.lColorSay.Size = new System.Drawing.Size(140, 18);
            this.lColorSay.TabIndex = 10;
            this.lColorSay.Text = "/Say message color";
            // 
            // bColorAnnouncement
            // 
            this.bColorAnnouncement.BackColor = System.Drawing.Color.White;
            this.bColorAnnouncement.Location = new System.Drawing.Point(700, 25);
            this.bColorAnnouncement.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bColorAnnouncement.Name = "bColorAnnouncement";
            this.bColorAnnouncement.Size = new System.Drawing.Size(133, 28);
            this.bColorAnnouncement.TabIndex = 9;
            this.bColorAnnouncement.UseVisualStyleBackColor = false;
            this.bColorAnnouncement.Click += new System.EventHandler(this.bColorAnnouncement_Click);
            // 
            // lColorAnnouncement
            // 
            this.lColorAnnouncement.AutoSize = true;
            this.lColorAnnouncement.Location = new System.Drawing.Point(456, 30);
            this.lColorAnnouncement.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lColorAnnouncement.Name = "lColorAnnouncement";
            this.lColorAnnouncement.Size = new System.Drawing.Size(219, 18);
            this.lColorAnnouncement.TabIndex = 8;
            this.lColorAnnouncement.Text = "Announcement and /Rules color";
            // 
            // bColorHelp
            // 
            this.bColorHelp.BackColor = System.Drawing.Color.White;
            this.bColorHelp.Location = new System.Drawing.Point(257, 132);
            this.bColorHelp.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bColorHelp.Name = "bColorHelp";
            this.bColorHelp.Size = new System.Drawing.Size(133, 28);
            this.bColorHelp.TabIndex = 7;
            this.bColorHelp.UseVisualStyleBackColor = false;
            this.bColorHelp.Click += new System.EventHandler(this.bColorHelp_Click);
            // 
            // bColorSay
            // 
            this.bColorSay.BackColor = System.Drawing.Color.White;
            this.bColorSay.Location = new System.Drawing.Point(700, 60);
            this.bColorSay.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bColorSay.Name = "bColorSay";
            this.bColorSay.Size = new System.Drawing.Size(133, 28);
            this.bColorSay.TabIndex = 11;
            this.bColorSay.UseVisualStyleBackColor = false;
            this.bColorSay.Click += new System.EventHandler(this.bColorSay_Click);
            // 
            // gAppearence
            // 
            this.gAppearence.Controls.Add(this.xShowConnectionMessages);
            this.gAppearence.Controls.Add(this.xShowJoinedWorldMessages);
            this.gAppearence.Controls.Add(this.xRankColorsInWorldNames);
            this.gAppearence.Controls.Add(this.xRankPrefixesInList);
            this.gAppearence.Controls.Add(this.xRankPrefixesInChat);
            this.gAppearence.Controls.Add(this.xRankColorsInChat);
            this.gAppearence.Location = new System.Drawing.Point(9, 188);
            this.gAppearence.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gAppearence.Name = "gAppearence";
            this.gAppearence.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gAppearence.Size = new System.Drawing.Size(849, 119);
            this.gAppearence.TabIndex = 1;
            this.gAppearence.TabStop = false;
            this.gAppearence.Text = "Appearence Tweaks";
            // 
            // xShowConnectionMessages
            // 
            this.xShowConnectionMessages.AutoSize = true;
            this.xShowConnectionMessages.Location = new System.Drawing.Point(433, 55);
            this.xShowConnectionMessages.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xShowConnectionMessages.Name = "xShowConnectionMessages";
            this.xShowConnectionMessages.Size = new System.Drawing.Size(492, 27);
            this.xShowConnectionMessages.TabIndex = 4;
            this.xShowConnectionMessages.Text = "Show a message when players join/leave SERVER.";
            this.xShowConnectionMessages.UseVisualStyleBackColor = true;
            // 
            // xShowJoinedWorldMessages
            // 
            this.xShowJoinedWorldMessages.AutoSize = true;
            this.xShowJoinedWorldMessages.Location = new System.Drawing.Point(433, 25);
            this.xShowJoinedWorldMessages.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xShowJoinedWorldMessages.Name = "xShowJoinedWorldMessages";
            this.xShowJoinedWorldMessages.Size = new System.Drawing.Size(419, 27);
            this.xShowJoinedWorldMessages.TabIndex = 3;
            this.xShowJoinedWorldMessages.Text = "Show a message when players join worlds.";
            this.xShowJoinedWorldMessages.UseVisualStyleBackColor = true;
            // 
            // xRankColorsInWorldNames
            // 
            this.xRankColorsInWorldNames.AutoSize = true;
            this.xRankColorsInWorldNames.Location = new System.Drawing.Point(433, 86);
            this.xRankColorsInWorldNames.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xRankColorsInWorldNames.Name = "xRankColorsInWorldNames";
            this.xRankColorsInWorldNames.Size = new System.Drawing.Size(389, 27);
            this.xRankColorsInWorldNames.TabIndex = 5;
            this.xRankColorsInWorldNames.Text = "Color world names based on build rank.";
            this.xRankColorsInWorldNames.UseVisualStyleBackColor = true;
            // 
            // xRankPrefixesInList
            // 
            this.xRankPrefixesInList.AutoSize = true;
            this.xRankPrefixesInList.Location = new System.Drawing.Point(59, 86);
            this.xRankPrefixesInList.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xRankPrefixesInList.Name = "xRankPrefixesInList";
            this.xRankPrefixesInList.Size = new System.Drawing.Size(353, 27);
            this.xRankPrefixesInList.TabIndex = 2;
            this.xRankPrefixesInList.Text = "Prefixes in player list (breaks skins).";
            this.xRankPrefixesInList.UseVisualStyleBackColor = true;
            // 
            // xRankPrefixesInChat
            // 
            this.xRankPrefixesInChat.AutoSize = true;
            this.xRankPrefixesInChat.Location = new System.Drawing.Point(33, 55);
            this.xRankPrefixesInChat.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xRankPrefixesInChat.Name = "xRankPrefixesInChat";
            this.xRankPrefixesInChat.Size = new System.Drawing.Size(213, 27);
            this.xRankPrefixesInChat.TabIndex = 1;
            this.xRankPrefixesInChat.Text = "Show rank prefixes.";
            this.xRankPrefixesInChat.UseVisualStyleBackColor = true;
            this.xRankPrefixesInChat.CheckedChanged += new System.EventHandler(this.xRankPrefixesInChat_CheckedChanged);
            // 
            // xRankColorsInChat
            // 
            this.xRankColorsInChat.AutoSize = true;
            this.xRankColorsInChat.Location = new System.Drawing.Point(33, 25);
            this.xRankColorsInChat.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xRankColorsInChat.Name = "xRankColorsInChat";
            this.xRankColorsInChat.Size = new System.Drawing.Size(201, 27);
            this.xRankColorsInChat.TabIndex = 0;
            this.xRankColorsInChat.Text = "Show rank colors.";
            this.xRankColorsInChat.UseVisualStyleBackColor = true;
            // 
            // chatPreview
            // 
            this.chatPreview.Location = new System.Drawing.Point(9, 315);
            this.chatPreview.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.chatPreview.Name = "chatPreview";
            this.chatPreview.Size = new System.Drawing.Size(849, 297);
            this.chatPreview.TabIndex = 2;
            // 
            // tabWorlds
            // 
            this.tabWorlds.Controls.Add(this.xWoMEnableEnvExtensions);
            this.tabWorlds.Controls.Add(this.bMapPath);
            this.tabWorlds.Controls.Add(this.xMapPath);
            this.tabWorlds.Controls.Add(this.tMapPath);
            this.tabWorlds.Controls.Add(this.lDefaultBuildRank);
            this.tabWorlds.Controls.Add(this.cDefaultBuildRank);
            this.tabWorlds.Controls.Add(this.cMainWorld);
            this.tabWorlds.Controls.Add(this.lMainWorld);
            this.tabWorlds.Controls.Add(this.bWorldEdit);
            this.tabWorlds.Controls.Add(this.bAddWorld);
            this.tabWorlds.Controls.Add(this.bWorldDelete);
            this.tabWorlds.Controls.Add(this.dgvWorlds);
            this.tabWorlds.Location = new System.Drawing.Point(4, 27);
            this.tabWorlds.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabWorlds.Name = "tabWorlds";
            this.tabWorlds.Padding = new System.Windows.Forms.Padding(7, 12, 7, 12);
            this.tabWorlds.Size = new System.Drawing.Size(872, 597);
            this.tabWorlds.TabIndex = 9;
            this.tabWorlds.Text = "Worlds";
            this.tabWorlds.UseVisualStyleBackColor = true;
            // 
            // xWoMEnableEnvExtensions
            // 
            this.xWoMEnableEnvExtensions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.xWoMEnableEnvExtensions.AutoSize = true;
            this.xWoMEnableEnvExtensions.Location = new System.Drawing.Point(11, 533);
            this.xWoMEnableEnvExtensions.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xWoMEnableEnvExtensions.Name = "xWoMEnableEnvExtensions";
            this.xWoMEnableEnvExtensions.Size = new System.Drawing.Size(431, 27);
            this.xWoMEnableEnvExtensions.TabIndex = 22;
            this.xWoMEnableEnvExtensions.Text = "Enable WoM environment extensions (/Env).";
            this.xWoMEnableEnvExtensions.UseVisualStyleBackColor = true;
            // 
            // bMapPath
            // 
            this.bMapPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bMapPath.Enabled = false;
            this.bMapPath.Location = new System.Drawing.Point(783, 503);
            this.bMapPath.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bMapPath.Name = "bMapPath";
            this.bMapPath.Size = new System.Drawing.Size(76, 28);
            this.bMapPath.TabIndex = 10;
            this.bMapPath.Text = "Browse";
            this.bMapPath.UseVisualStyleBackColor = true;
            this.bMapPath.Click += new System.EventHandler(this.bMapPath_Click);
            // 
            // xMapPath
            // 
            this.xMapPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.xMapPath.AutoSize = true;
            this.xMapPath.Location = new System.Drawing.Point(11, 500);
            this.xMapPath.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xMapPath.Name = "xMapPath";
            this.xMapPath.Size = new System.Drawing.Size(308, 27);
            this.xMapPath.TabIndex = 8;
            this.xMapPath.Text = "Custom path for storing maps:";
            this.xMapPath.UseVisualStyleBackColor = true;
            this.xMapPath.CheckedChanged += new System.EventHandler(this.xMapPath_CheckedChanged);
            // 
            // tMapPath
            // 
            this.tMapPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tMapPath.Enabled = false;
            this.tMapPath.Font = new System.Drawing.Font("Lucida Console", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tMapPath.Location = new System.Drawing.Point(271, 506);
            this.tMapPath.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tMapPath.Name = "tMapPath";
            this.tMapPath.Size = new System.Drawing.Size(503, 22);
            this.tMapPath.TabIndex = 9;
            // 
            // lDefaultBuildRank
            // 
            this.lDefaultBuildRank.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lDefaultBuildRank.AutoSize = true;
            this.lDefaultBuildRank.Location = new System.Drawing.Point(32, 469);
            this.lDefaultBuildRank.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lDefaultBuildRank.Name = "lDefaultBuildRank";
            this.lDefaultBuildRank.Size = new System.Drawing.Size(407, 18);
            this.lDefaultBuildRank.TabIndex = 6;
            this.lDefaultBuildRank.Text = "Default rank requirement for building on newly-loaded worlds:";
            // 
            // cDefaultBuildRank
            // 
            this.cDefaultBuildRank.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cDefaultBuildRank.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cDefaultBuildRank.FormattingEnabled = true;
            this.cDefaultBuildRank.Location = new System.Drawing.Point(496, 465);
            this.cDefaultBuildRank.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cDefaultBuildRank.Name = "cDefaultBuildRank";
            this.cDefaultBuildRank.Size = new System.Drawing.Size(160, 26);
            this.cDefaultBuildRank.TabIndex = 7;
            this.cDefaultBuildRank.SelectedIndexChanged += new System.EventHandler(this.cDefaultBuildRank_SelectedIndexChanged);
            // 
            // cMainWorld
            // 
            this.cMainWorld.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cMainWorld.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cMainWorld.Location = new System.Drawing.Point(723, 21);
            this.cMainWorld.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cMainWorld.Name = "cMainWorld";
            this.cMainWorld.Size = new System.Drawing.Size(135, 26);
            this.cMainWorld.TabIndex = 5;
            // 
            // lMainWorld
            // 
            this.lMainWorld.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lMainWorld.AutoSize = true;
            this.lMainWorld.Location = new System.Drawing.Point(620, 25);
            this.lMainWorld.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lMainWorld.Name = "lMainWorld";
            this.lMainWorld.Size = new System.Drawing.Size(84, 18);
            this.lMainWorld.TabIndex = 4;
            this.lMainWorld.Text = "Main world:";
            // 
            // bWorldEdit
            // 
            this.bWorldEdit.Enabled = false;
            this.bWorldEdit.Location = new System.Drawing.Point(152, 16);
            this.bWorldEdit.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bWorldEdit.Name = "bWorldEdit";
            this.bWorldEdit.Size = new System.Drawing.Size(133, 34);
            this.bWorldEdit.TabIndex = 2;
            this.bWorldEdit.Text = "Edit";
            this.bWorldEdit.UseVisualStyleBackColor = true;
            this.bWorldEdit.Click += new System.EventHandler(this.bWorldEdit_Click);
            // 
            // bAddWorld
            // 
            this.bAddWorld.Location = new System.Drawing.Point(11, 16);
            this.bAddWorld.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bAddWorld.Name = "bAddWorld";
            this.bAddWorld.Size = new System.Drawing.Size(133, 34);
            this.bAddWorld.TabIndex = 1;
            this.bAddWorld.Text = "Add World";
            this.bAddWorld.UseVisualStyleBackColor = true;
            this.bAddWorld.Click += new System.EventHandler(this.bAddWorld_Click);
            // 
            // bWorldDelete
            // 
            this.bWorldDelete.Enabled = false;
            this.bWorldDelete.Location = new System.Drawing.Point(293, 16);
            this.bWorldDelete.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bWorldDelete.Name = "bWorldDelete";
            this.bWorldDelete.Size = new System.Drawing.Size(133, 34);
            this.bWorldDelete.TabIndex = 3;
            this.bWorldDelete.Text = "Delete World";
            this.bWorldDelete.UseVisualStyleBackColor = true;
            this.bWorldDelete.Click += new System.EventHandler(this.bWorldDel_Click);
            // 
            // dgvWorlds
            // 
            this.dgvWorlds.AllowUserToAddRows = false;
            this.dgvWorlds.AllowUserToDeleteRows = false;
            this.dgvWorlds.AllowUserToOrderColumns = true;
            this.dgvWorlds.AllowUserToResizeRows = false;
            this.dgvWorlds.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvWorlds.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvWorlds.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dgvcName,
            this.dgvcDescription,
            this.dgvcAccess,
            this.dgvcBuild,
            this.dgvcBackup,
            this.dgvcHidden,
            this.dgvcBlockDB});
            this.dgvWorlds.Location = new System.Drawing.Point(11, 58);
            this.dgvWorlds.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dgvWorlds.MultiSelect = false;
            this.dgvWorlds.Name = "dgvWorlds";
            this.dgvWorlds.RowHeadersVisible = false;
            dataGridViewCellStyle2.Padding = new System.Windows.Forms.Padding(0, 1, 0, 1);
            this.dgvWorlds.RowsDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvWorlds.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvWorlds.Size = new System.Drawing.Size(848, 400);
            this.dgvWorlds.TabIndex = 0;
            this.dgvWorlds.SelectionChanged += new System.EventHandler(this.dgvWorlds_Click);
            this.dgvWorlds.Click += new System.EventHandler(this.dgvWorlds_Click);
            // 
            // dgvcName
            // 
            this.dgvcName.DataPropertyName = "Name";
            this.dgvcName.HeaderText = "World Name";
            this.dgvcName.Name = "dgvcName";
            this.dgvcName.Width = 110;
            // 
            // dgvcDescription
            // 
            this.dgvcDescription.DataPropertyName = "Description";
            this.dgvcDescription.HeaderText = "";
            this.dgvcDescription.Name = "dgvcDescription";
            this.dgvcDescription.ReadOnly = true;
            this.dgvcDescription.Width = 130;
            // 
            // dgvcAccess
            // 
            this.dgvcAccess.DataPropertyName = "AccessPermission";
            this.dgvcAccess.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
            this.dgvcAccess.HeaderText = "Access";
            this.dgvcAccess.Name = "dgvcAccess";
            this.dgvcAccess.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // dgvcBuild
            // 
            this.dgvcBuild.DataPropertyName = "BuildPermission";
            this.dgvcBuild.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
            this.dgvcBuild.HeaderText = "Build";
            this.dgvcBuild.Name = "dgvcBuild";
            this.dgvcBuild.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // dgvcBackup
            // 
            this.dgvcBackup.DataPropertyName = "Backup";
            this.dgvcBackup.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
            this.dgvcBackup.HeaderText = "Backup";
            this.dgvcBackup.Name = "dgvcBackup";
            this.dgvcBackup.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.dgvcBackup.Width = 90;
            // 
            // dgvcHidden
            // 
            this.dgvcHidden.DataPropertyName = "Hidden";
            this.dgvcHidden.HeaderText = "Hide";
            this.dgvcHidden.Name = "dgvcHidden";
            this.dgvcHidden.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.dgvcHidden.Width = 40;
            // 
            // dgvcBlockDB
            // 
            this.dgvcBlockDB.DataPropertyName = "BlockDBEnabled";
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.dgvcBlockDB.DefaultCellStyle = dataGridViewCellStyle1;
            this.dgvcBlockDB.HeaderText = "BlockDB";
            this.dgvcBlockDB.Name = "dgvcBlockDB";
            this.dgvcBlockDB.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.dgvcBlockDB.ThreeState = true;
            this.dgvcBlockDB.Width = 60;
            // 
            // tabRanks
            // 
            this.tabRanks.Controls.Add(this.gPermissionLimits);
            this.tabRanks.Controls.Add(this.lRankList);
            this.tabRanks.Controls.Add(this.bLowerRank);
            this.tabRanks.Controls.Add(this.bRaiseRank);
            this.tabRanks.Controls.Add(this.gRankOptions);
            this.tabRanks.Controls.Add(this.bDeleteRank);
            this.tabRanks.Controls.Add(this.vPermissions);
            this.tabRanks.Controls.Add(this.bAddRank);
            this.tabRanks.Controls.Add(this.lPermissions);
            this.tabRanks.Controls.Add(this.vRanks);
            this.tabRanks.Location = new System.Drawing.Point(4, 27);
            this.tabRanks.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabRanks.Name = "tabRanks";
            this.tabRanks.Padding = new System.Windows.Forms.Padding(7, 12, 7, 12);
            this.tabRanks.Size = new System.Drawing.Size(872, 597);
            this.tabRanks.TabIndex = 2;
            this.tabRanks.Text = "Ranks";
            this.tabRanks.UseVisualStyleBackColor = true;
            // 
            // gPermissionLimits
            // 
            this.gPermissionLimits.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gPermissionLimits.Controls.Add(this.permissionLimitBoxContainer);
            this.gPermissionLimits.Location = new System.Drawing.Point(213, 359);
            this.gPermissionLimits.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gPermissionLimits.Name = "gPermissionLimits";
            this.gPermissionLimits.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gPermissionLimits.Size = new System.Drawing.Size(409, 229);
            this.gPermissionLimits.TabIndex = 7;
            this.gPermissionLimits.TabStop = false;
            this.gPermissionLimits.Text = "Permission Limits";
            // 
            // permissionLimitBoxContainer
            // 
            this.permissionLimitBoxContainer.AutoScroll = true;
            this.permissionLimitBoxContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.permissionLimitBoxContainer.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.permissionLimitBoxContainer.Location = new System.Drawing.Point(4, 25);
            this.permissionLimitBoxContainer.Margin = new System.Windows.Forms.Padding(0);
            this.permissionLimitBoxContainer.Name = "permissionLimitBoxContainer";
            this.permissionLimitBoxContainer.Size = new System.Drawing.Size(401, 201);
            this.permissionLimitBoxContainer.TabIndex = 0;
            this.permissionLimitBoxContainer.WrapContents = false;
            // 
            // lRankList
            // 
            this.lRankList.AutoSize = true;
            this.lRankList.Location = new System.Drawing.Point(11, 12);
            this.lRankList.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lRankList.Name = "lRankList";
            this.lRankList.Size = new System.Drawing.Size(70, 18);
            this.lRankList.TabIndex = 0;
            this.lRankList.Text = "Rank List";
            // 
            // bLowerRank
            // 
            this.bLowerRank.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bLowerRank.Location = new System.Drawing.Point(112, 560);
            this.bLowerRank.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bLowerRank.Name = "bLowerRank";
            this.bLowerRank.Size = new System.Drawing.Size(93, 28);
            this.bLowerRank.TabIndex = 5;
            this.bLowerRank.Text = "▼ Lower";
            this.bLowerRank.UseVisualStyleBackColor = true;
            this.bLowerRank.Click += new System.EventHandler(this.bLowerRank_Click);
            // 
            // bRaiseRank
            // 
            this.bRaiseRank.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bRaiseRank.Location = new System.Drawing.Point(11, 560);
            this.bRaiseRank.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bRaiseRank.Name = "bRaiseRank";
            this.bRaiseRank.Size = new System.Drawing.Size(93, 28);
            this.bRaiseRank.TabIndex = 4;
            this.bRaiseRank.Text = "▲ Raise";
            this.bRaiseRank.UseVisualStyleBackColor = true;
            this.bRaiseRank.Click += new System.EventHandler(this.bRaiseRank_Click);
            // 
            // gRankOptions
            // 
            this.gRankOptions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gRankOptions.Controls.Add(this.lFillLimitUnits);
            this.gRankOptions.Controls.Add(this.nFillLimit);
            this.gRankOptions.Controls.Add(this.lFillLimit);
            this.gRankOptions.Controls.Add(this.nCopyPasteSlots);
            this.gRankOptions.Controls.Add(this.lCopyPasteSlots);
            this.gRankOptions.Controls.Add(this.xAllowSecurityCircumvention);
            this.gRankOptions.Controls.Add(this.lAntiGrief1);
            this.gRankOptions.Controls.Add(this.lAntiGrief3);
            this.gRankOptions.Controls.Add(this.nAntiGriefSeconds);
            this.gRankOptions.Controls.Add(this.bColorRank);
            this.gRankOptions.Controls.Add(this.xDrawLimit);
            this.gRankOptions.Controls.Add(this.lDrawLimitUnits);
            this.gRankOptions.Controls.Add(this.lKickIdleUnits);
            this.gRankOptions.Controls.Add(this.nDrawLimit);
            this.gRankOptions.Controls.Add(this.nKickIdle);
            this.gRankOptions.Controls.Add(this.xAntiGrief);
            this.gRankOptions.Controls.Add(this.lAntiGrief2);
            this.gRankOptions.Controls.Add(this.xKickIdle);
            this.gRankOptions.Controls.Add(this.nAntiGriefBlocks);
            this.gRankOptions.Controls.Add(this.xReserveSlot);
            this.gRankOptions.Controls.Add(this.tPrefix);
            this.gRankOptions.Controls.Add(this.lPrefix);
            this.gRankOptions.Controls.Add(this.lRankColor);
            this.gRankOptions.Controls.Add(this.tRankName);
            this.gRankOptions.Controls.Add(this.lRankName);
            this.gRankOptions.Location = new System.Drawing.Point(213, 16);
            this.gRankOptions.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gRankOptions.Name = "gRankOptions";
            this.gRankOptions.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gRankOptions.Size = new System.Drawing.Size(409, 336);
            this.gRankOptions.TabIndex = 6;
            this.gRankOptions.TabStop = false;
            this.gRankOptions.Text = "Rank Options";
            // 
            // lFillLimitUnits
            // 
            this.lFillLimitUnits.AutoSize = true;
            this.lFillLimitUnits.Location = new System.Drawing.Point(319, 302);
            this.lFillLimitUnits.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lFillLimitUnits.Name = "lFillLimitUnits";
            this.lFillLimitUnits.Size = new System.Drawing.Size(52, 18);
            this.lFillLimitUnits.TabIndex = 24;
            this.lFillLimitUnits.Text = "blocks";
            // 
            // nFillLimit
            // 
            this.nFillLimit.Location = new System.Drawing.Point(232, 299);
            this.nFillLimit.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nFillLimit.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
            this.nFillLimit.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nFillLimit.Name = "nFillLimit";
            this.nFillLimit.Size = new System.Drawing.Size(79, 24);
            this.nFillLimit.TabIndex = 23;
            this.nFillLimit.Value = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.nFillLimit.ValueChanged += new System.EventHandler(this.nFillLimit_ValueChanged);
            // 
            // lFillLimit
            // 
            this.lFillLimit.AutoSize = true;
            this.lFillLimit.Location = new System.Drawing.Point(113, 302);
            this.lFillLimit.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lFillLimit.Name = "lFillLimit";
            this.lFillLimit.Size = new System.Drawing.Size(98, 18);
            this.lFillLimit.TabIndex = 22;
            this.lFillLimit.Text = "Flood-fill limit:";
            // 
            // nCopyPasteSlots
            // 
            this.nCopyPasteSlots.Location = new System.Drawing.Point(232, 266);
            this.nCopyPasteSlots.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nCopyPasteSlots.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nCopyPasteSlots.Name = "nCopyPasteSlots";
            this.nCopyPasteSlots.Size = new System.Drawing.Size(79, 24);
            this.nCopyPasteSlots.TabIndex = 21;
            this.nCopyPasteSlots.ValueChanged += new System.EventHandler(this.nCopyPasteSlots_ValueChanged);
            // 
            // lCopyPasteSlots
            // 
            this.lCopyPasteSlots.AutoSize = true;
            this.lCopyPasteSlots.Location = new System.Drawing.Point(67, 268);
            this.lCopyPasteSlots.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lCopyPasteSlots.Name = "lCopyPasteSlots";
            this.lCopyPasteSlots.Size = new System.Drawing.Size(145, 18);
            this.lCopyPasteSlots.TabIndex = 20;
            this.lCopyPasteSlots.Text = "Copy/paste slot limit:";
            // 
            // xAllowSecurityCircumvention
            // 
            this.xAllowSecurityCircumvention.AutoSize = true;
            this.xAllowSecurityCircumvention.Location = new System.Drawing.Point(16, 203);
            this.xAllowSecurityCircumvention.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xAllowSecurityCircumvention.Name = "xAllowSecurityCircumvention";
            this.xAllowSecurityCircumvention.Size = new System.Drawing.Size(439, 27);
            this.xAllowSecurityCircumvention.TabIndex = 16;
            this.xAllowSecurityCircumvention.Text = "Allow removing own access/build restrictions.";
            this.xAllowSecurityCircumvention.UseVisualStyleBackColor = true;
            this.xAllowSecurityCircumvention.CheckedChanged += new System.EventHandler(this.xAllowSecurityCircumvention_CheckedChanged);
            // 
            // lAntiGrief1
            // 
            this.lAntiGrief1.AutoSize = true;
            this.lAntiGrief1.Location = new System.Drawing.Point(67, 166);
            this.lAntiGrief1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lAntiGrief1.Name = "lAntiGrief1";
            this.lAntiGrief1.Size = new System.Drawing.Size(58, 18);
            this.lAntiGrief1.TabIndex = 11;
            this.lAntiGrief1.Text = "Kick on";
            // 
            // lAntiGrief3
            // 
            this.lAntiGrief3.AutoSize = true;
            this.lAntiGrief3.Location = new System.Drawing.Point(367, 166);
            this.lAntiGrief3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lAntiGrief3.Name = "lAntiGrief3";
            this.lAntiGrief3.Size = new System.Drawing.Size(32, 18);
            this.lAntiGrief3.TabIndex = 15;
            this.lAntiGrief3.Text = "sec";
            // 
            // nAntiGriefSeconds
            // 
            this.nAntiGriefSeconds.Location = new System.Drawing.Point(305, 164);
            this.nAntiGriefSeconds.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nAntiGriefSeconds.Name = "nAntiGriefSeconds";
            this.nAntiGriefSeconds.Size = new System.Drawing.Size(53, 24);
            this.nAntiGriefSeconds.TabIndex = 14;
            this.nAntiGriefSeconds.ValueChanged += new System.EventHandler(this.nAntiGriefSeconds_ValueChanged);
            // 
            // bColorRank
            // 
            this.bColorRank.BackColor = System.Drawing.Color.White;
            this.bColorRank.Location = new System.Drawing.Point(268, 58);
            this.bColorRank.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bColorRank.Name = "bColorRank";
            this.bColorRank.Size = new System.Drawing.Size(133, 30);
            this.bColorRank.TabIndex = 6;
            this.bColorRank.UseVisualStyleBackColor = false;
            this.bColorRank.Click += new System.EventHandler(this.bColorRank_Click);
            // 
            // xDrawLimit
            // 
            this.xDrawLimit.AutoSize = true;
            this.xDrawLimit.Location = new System.Drawing.Point(16, 234);
            this.xDrawLimit.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xDrawLimit.Name = "xDrawLimit";
            this.xDrawLimit.Size = new System.Drawing.Size(127, 27);
            this.xDrawLimit.TabIndex = 17;
            this.xDrawLimit.Text = "Draw limit";
            this.xDrawLimit.UseVisualStyleBackColor = true;
            this.xDrawLimit.CheckedChanged += new System.EventHandler(this.xDrawLimit_CheckedChanged);
            // 
            // lDrawLimitUnits
            // 
            this.lDrawLimitUnits.AutoSize = true;
            this.lDrawLimitUnits.Location = new System.Drawing.Point(229, 235);
            this.lDrawLimitUnits.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lDrawLimitUnits.Name = "lDrawLimitUnits";
            this.lDrawLimitUnits.Size = new System.Drawing.Size(52, 18);
            this.lDrawLimitUnits.TabIndex = 19;
            this.lDrawLimitUnits.Text = "blocks";
            // 
            // lKickIdleUnits
            // 
            this.lKickIdleUnits.AutoSize = true;
            this.lKickIdleUnits.Location = new System.Drawing.Point(241, 97);
            this.lKickIdleUnits.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lKickIdleUnits.Name = "lKickIdleUnits";
            this.lKickIdleUnits.Size = new System.Drawing.Size(60, 18);
            this.lKickIdleUnits.TabIndex = 9;
            this.lKickIdleUnits.Text = "minutes";
            // 
            // nDrawLimit
            // 
            this.nDrawLimit.Increment = new decimal(new int[] {
            32,
            0,
            0,
            0});
            this.nDrawLimit.Location = new System.Drawing.Point(132, 233);
            this.nDrawLimit.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nDrawLimit.Maximum = new decimal(new int[] {
            100000000,
            0,
            0,
            0});
            this.nDrawLimit.Name = "nDrawLimit";
            this.nDrawLimit.Size = new System.Drawing.Size(89, 24);
            this.nDrawLimit.TabIndex = 18;
            this.nDrawLimit.ValueChanged += new System.EventHandler(this.nDrawLimit_ValueChanged);
            // 
            // nKickIdle
            // 
            this.nKickIdle.Location = new System.Drawing.Point(155, 95);
            this.nKickIdle.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nKickIdle.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nKickIdle.Name = "nKickIdle";
            this.nKickIdle.Size = new System.Drawing.Size(79, 24);
            this.nKickIdle.TabIndex = 8;
            this.nKickIdle.ValueChanged += new System.EventHandler(this.nKickIdle_ValueChanged);
            // 
            // xAntiGrief
            // 
            this.xAntiGrief.AutoSize = true;
            this.xAntiGrief.Location = new System.Drawing.Point(16, 133);
            this.xAntiGrief.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xAntiGrief.Name = "xAntiGrief";
            this.xAntiGrief.Size = new System.Drawing.Size(340, 27);
            this.xAntiGrief.TabIndex = 10;
            this.xAntiGrief.Text = "Enable grief / autoclicker detection";
            this.xAntiGrief.UseVisualStyleBackColor = true;
            this.xAntiGrief.CheckedChanged += new System.EventHandler(this.xAntiGrief_CheckedChanged);
            // 
            // lAntiGrief2
            // 
            this.lAntiGrief2.AutoSize = true;
            this.lAntiGrief2.Location = new System.Drawing.Point(224, 166);
            this.lAntiGrief2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lAntiGrief2.Name = "lAntiGrief2";
            this.lAntiGrief2.Size = new System.Drawing.Size(67, 18);
            this.lAntiGrief2.TabIndex = 13;
            this.lAntiGrief2.Text = "blocks in";
            // 
            // xKickIdle
            // 
            this.xKickIdle.AutoSize = true;
            this.xKickIdle.Location = new System.Drawing.Point(16, 96);
            this.xKickIdle.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xKickIdle.Name = "xKickIdle";
            this.xKickIdle.Size = new System.Drawing.Size(157, 27);
            this.xKickIdle.TabIndex = 7;
            this.xKickIdle.Text = "Kick if idle for";
            this.xKickIdle.UseVisualStyleBackColor = true;
            this.xKickIdle.CheckedChanged += new System.EventHandler(this.xKickIdle_CheckedChanged);
            // 
            // nAntiGriefBlocks
            // 
            this.nAntiGriefBlocks.Location = new System.Drawing.Point(137, 164);
            this.nAntiGriefBlocks.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nAntiGriefBlocks.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nAntiGriefBlocks.Name = "nAntiGriefBlocks";
            this.nAntiGriefBlocks.Size = new System.Drawing.Size(79, 24);
            this.nAntiGriefBlocks.TabIndex = 12;
            this.nAntiGriefBlocks.ValueChanged += new System.EventHandler(this.nAntiGriefBlocks_ValueChanged);
            // 
            // xReserveSlot
            // 
            this.xReserveSlot.AutoSize = true;
            this.xReserveSlot.Location = new System.Drawing.Point(16, 63);
            this.xReserveSlot.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xReserveSlot.Name = "xReserveSlot";
            this.xReserveSlot.Size = new System.Drawing.Size(208, 27);
            this.xReserveSlot.TabIndex = 4;
            this.xReserveSlot.Text = "Reserve player slot";
            this.xReserveSlot.UseVisualStyleBackColor = true;
            this.xReserveSlot.CheckedChanged += new System.EventHandler(this.xReserveSlot_CheckedChanged);
            // 
            // tPrefix
            // 
            this.tPrefix.Enabled = false;
            this.tPrefix.Location = new System.Drawing.Point(372, 25);
            this.tPrefix.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tPrefix.MaxLength = 1;
            this.tPrefix.Name = "tPrefix";
            this.tPrefix.Size = new System.Drawing.Size(28, 24);
            this.tPrefix.TabIndex = 3;
            this.tPrefix.Validating += new System.ComponentModel.CancelEventHandler(this.tPrefix_Validating);
            // 
            // lPrefix
            // 
            this.lPrefix.AutoSize = true;
            this.lPrefix.Enabled = false;
            this.lPrefix.Location = new System.Drawing.Point(313, 28);
            this.lPrefix.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lPrefix.Name = "lPrefix";
            this.lPrefix.Size = new System.Drawing.Size(45, 18);
            this.lPrefix.TabIndex = 2;
            this.lPrefix.Text = "Prefix";
            // 
            // lRankColor
            // 
            this.lRankColor.AutoSize = true;
            this.lRankColor.Location = new System.Drawing.Point(212, 64);
            this.lRankColor.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lRankColor.Name = "lRankColor";
            this.lRankColor.Size = new System.Drawing.Size(45, 18);
            this.lRankColor.TabIndex = 5;
            this.lRankColor.Text = "Color";
            // 
            // tRankName
            // 
            this.tRankName.Location = new System.Drawing.Point(83, 25);
            this.tRankName.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tRankName.MaxLength = 16;
            this.tRankName.Name = "tRankName";
            this.tRankName.Size = new System.Drawing.Size(189, 24);
            this.tRankName.TabIndex = 1;
            this.tRankName.Validating += new System.ComponentModel.CancelEventHandler(this.tRankName_Validating);
            // 
            // lRankName
            // 
            this.lRankName.AutoSize = true;
            this.lRankName.Location = new System.Drawing.Point(20, 28);
            this.lRankName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lRankName.Name = "lRankName";
            this.lRankName.Size = new System.Drawing.Size(48, 18);
            this.lRankName.TabIndex = 0;
            this.lRankName.Text = "Name";
            // 
            // bDeleteRank
            // 
            this.bDeleteRank.Location = new System.Drawing.Point(112, 34);
            this.bDeleteRank.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bDeleteRank.Name = "bDeleteRank";
            this.bDeleteRank.Size = new System.Drawing.Size(93, 28);
            this.bDeleteRank.TabIndex = 3;
            this.bDeleteRank.Text = "Delete";
            this.bDeleteRank.UseVisualStyleBackColor = true;
            this.bDeleteRank.Click += new System.EventHandler(this.bDeleteRank_Click);
            // 
            // vPermissions
            // 
            this.vPermissions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.vPermissions.CheckBoxes = true;
            this.vPermissions.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chPermissions});
            this.vPermissions.GridLines = true;
            this.vPermissions.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.vPermissions.Location = new System.Drawing.Point(631, 34);
            this.vPermissions.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.vPermissions.MultiSelect = false;
            this.vPermissions.Name = "vPermissions";
            this.vPermissions.ShowGroups = false;
            this.vPermissions.ShowItemToolTips = true;
            this.vPermissions.Size = new System.Drawing.Size(227, 553);
            this.vPermissions.TabIndex = 9;
            this.vPermissions.UseCompatibleStateImageBehavior = false;
            this.vPermissions.View = System.Windows.Forms.View.Details;
            this.vPermissions.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.vPermissions_ItemChecked);
            // 
            // chPermissions
            // 
            this.chPermissions.Width = 150;
            // 
            // bAddRank
            // 
            this.bAddRank.Location = new System.Drawing.Point(11, 34);
            this.bAddRank.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bAddRank.Name = "bAddRank";
            this.bAddRank.Size = new System.Drawing.Size(93, 28);
            this.bAddRank.TabIndex = 2;
            this.bAddRank.Text = "Add Rank";
            this.bAddRank.UseVisualStyleBackColor = true;
            this.bAddRank.Click += new System.EventHandler(this.bAddRank_Click);
            // 
            // lPermissions
            // 
            this.lPermissions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lPermissions.AutoSize = true;
            this.lPermissions.Location = new System.Drawing.Point(631, 12);
            this.lPermissions.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lPermissions.Name = "lPermissions";
            this.lPermissions.Size = new System.Drawing.Size(130, 18);
            this.lPermissions.TabIndex = 8;
            this.lPermissions.Text = "Rank Permissions";
            // 
            // vRanks
            // 
            this.vRanks.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.vRanks.Font = new System.Drawing.Font("Lucida Console", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.vRanks.FormattingEnabled = true;
            this.vRanks.IntegralHeight = false;
            this.vRanks.ItemHeight = 19;
            this.vRanks.Location = new System.Drawing.Point(11, 70);
            this.vRanks.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.vRanks.Name = "vRanks";
            this.vRanks.Size = new System.Drawing.Size(193, 482);
            this.vRanks.TabIndex = 1;
            this.vRanks.SelectedIndexChanged += new System.EventHandler(this.vRanks_SelectedIndexChanged);
            // 
            // tabSecurity
            // 
            this.tabSecurity.Controls.Add(this.gBlockDB);
            this.tabSecurity.Controls.Add(this.gSecurityMisc);
            this.tabSecurity.Controls.Add(this.gSpamChat);
            this.tabSecurity.Controls.Add(this.gVerify);
            this.tabSecurity.Location = new System.Drawing.Point(4, 27);
            this.tabSecurity.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabSecurity.Name = "tabSecurity";
            this.tabSecurity.Padding = new System.Windows.Forms.Padding(7, 12, 7, 12);
            this.tabSecurity.Size = new System.Drawing.Size(872, 597);
            this.tabSecurity.TabIndex = 7;
            this.tabSecurity.Text = "Security";
            this.tabSecurity.UseVisualStyleBackColor = true;
            // 
            // gBlockDB
            // 
            this.gBlockDB.Controls.Add(this.cBlockDBAutoEnableRank);
            this.gBlockDB.Controls.Add(this.xBlockDBAutoEnable);
            this.gBlockDB.Controls.Add(this.xBlockDBEnabled);
            this.gBlockDB.Location = new System.Drawing.Point(11, 123);
            this.gBlockDB.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gBlockDB.Name = "gBlockDB";
            this.gBlockDB.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gBlockDB.Size = new System.Drawing.Size(848, 108);
            this.gBlockDB.TabIndex = 1;
            this.gBlockDB.TabStop = false;
            this.gBlockDB.Text = "BlockDB";
            // 
            // cBlockDBAutoEnableRank
            // 
            this.cBlockDBAutoEnableRank.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cBlockDBAutoEnableRank.FormattingEnabled = true;
            this.cBlockDBAutoEnableRank.Location = new System.Drawing.Point(589, 65);
            this.cBlockDBAutoEnableRank.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cBlockDBAutoEnableRank.Name = "cBlockDBAutoEnableRank";
            this.cBlockDBAutoEnableRank.Size = new System.Drawing.Size(160, 26);
            this.cBlockDBAutoEnableRank.TabIndex = 2;
            this.cBlockDBAutoEnableRank.TabStop = false;
            this.cBlockDBAutoEnableRank.SelectedIndexChanged += new System.EventHandler(this.cBlockDBAutoEnableRank_SelectedIndexChanged);
            // 
            // xBlockDBAutoEnable
            // 
            this.xBlockDBAutoEnable.AutoSize = true;
            this.xBlockDBAutoEnable.Enabled = false;
            this.xBlockDBAutoEnable.Location = new System.Drawing.Point(101, 68);
            this.xBlockDBAutoEnable.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xBlockDBAutoEnable.Name = "xBlockDBAutoEnable";
            this.xBlockDBAutoEnable.Size = new System.Drawing.Size(579, 27);
            this.xBlockDBAutoEnable.TabIndex = 1;
            this.xBlockDBAutoEnable.TabStop = false;
            this.xBlockDBAutoEnable.Text = "Automatically enable BlockDB on worlds that can be edited by";
            this.xBlockDBAutoEnable.UseVisualStyleBackColor = true;
            this.xBlockDBAutoEnable.CheckedChanged += new System.EventHandler(this.xBlockDBAutoEnable_CheckedChanged);
            // 
            // xBlockDBEnabled
            // 
            this.xBlockDBEnabled.AutoSize = true;
            this.xBlockDBEnabled.Location = new System.Drawing.Point(56, 37);
            this.xBlockDBEnabled.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xBlockDBEnabled.Name = "xBlockDBEnabled";
            this.xBlockDBEnabled.Size = new System.Drawing.Size(401, 27);
            this.xBlockDBEnabled.TabIndex = 0;
            this.xBlockDBEnabled.Text = "Enable BlockDB (per-block edit tracking).";
            this.xBlockDBEnabled.UseVisualStyleBackColor = true;
            this.xBlockDBEnabled.CheckedChanged += new System.EventHandler(this.xBlockDBEnabled_CheckedChanged);
            // 
            // gSecurityMisc
            // 
            this.gSecurityMisc.Controls.Add(this.xAnnounceRankChangeReasons);
            this.gSecurityMisc.Controls.Add(this.xRequireKickReason);
            this.gSecurityMisc.Controls.Add(this.xPaidPlayersOnly);
            this.gSecurityMisc.Controls.Add(this.lPatrolledRankAndBelow);
            this.gSecurityMisc.Controls.Add(this.cPatrolledRank);
            this.gSecurityMisc.Controls.Add(this.lPatrolledRank);
            this.gSecurityMisc.Controls.Add(this.xAnnounceRankChanges);
            this.gSecurityMisc.Controls.Add(this.xAnnounceKickAndBanReasons);
            this.gSecurityMisc.Controls.Add(this.xRequireRankChangeReason);
            this.gSecurityMisc.Controls.Add(this.xRequireBanReason);
            this.gSecurityMisc.Location = new System.Drawing.Point(11, 362);
            this.gSecurityMisc.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gSecurityMisc.Name = "gSecurityMisc";
            this.gSecurityMisc.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gSecurityMisc.Size = new System.Drawing.Size(848, 219);
            this.gSecurityMisc.TabIndex = 3;
            this.gSecurityMisc.TabStop = false;
            this.gSecurityMisc.Text = "Misc";
            // 
            // xAnnounceRankChangeReasons
            // 
            this.xAnnounceRankChangeReasons.AutoSize = true;
            this.xAnnounceRankChangeReasons.Location = new System.Drawing.Point(448, 134);
            this.xAnnounceRankChangeReasons.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xAnnounceRankChangeReasons.Name = "xAnnounceRankChangeReasons";
            this.xAnnounceRankChangeReasons.Size = new System.Drawing.Size(408, 27);
            this.xAnnounceRankChangeReasons.TabIndex = 6;
            this.xAnnounceRankChangeReasons.Text = "Announce promotion && demotion reasons";
            this.xAnnounceRankChangeReasons.UseVisualStyleBackColor = true;
            // 
            // xRequireKickReason
            // 
            this.xRequireKickReason.AutoSize = true;
            this.xRequireKickReason.Location = new System.Drawing.Point(56, 73);
            this.xRequireKickReason.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xRequireKickReason.Name = "xRequireKickReason";
            this.xRequireKickReason.Size = new System.Drawing.Size(216, 27);
            this.xRequireKickReason.TabIndex = 1;
            this.xRequireKickReason.Text = "Require kick reason";
            this.xRequireKickReason.UseVisualStyleBackColor = true;
            // 
            // xPaidPlayersOnly
            // 
            this.xPaidPlayersOnly.AutoSize = true;
            this.xPaidPlayersOnly.Location = new System.Drawing.Point(56, 25);
            this.xPaidPlayersOnly.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xPaidPlayersOnly.Name = "xPaidPlayersOnly";
            this.xPaidPlayersOnly.Size = new System.Drawing.Size(789, 27);
            this.xPaidPlayersOnly.TabIndex = 0;
            this.xPaidPlayersOnly.Text = "Only allow players with paid Minecraft accounts to join the server (not recommend" +
    "ed).";
            this.xPaidPlayersOnly.UseVisualStyleBackColor = true;
            // 
            // lPatrolledRankAndBelow
            // 
            this.lPatrolledRankAndBelow.AutoSize = true;
            this.lPatrolledRankAndBelow.Location = new System.Drawing.Point(376, 178);
            this.lPatrolledRankAndBelow.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lPatrolledRankAndBelow.Name = "lPatrolledRankAndBelow";
            this.lPatrolledRankAndBelow.Size = new System.Drawing.Size(85, 18);
            this.lPatrolledRankAndBelow.TabIndex = 9;
            this.lPatrolledRankAndBelow.Text = "(and below)";
            // 
            // cPatrolledRank
            // 
            this.cPatrolledRank.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cPatrolledRank.FormattingEnabled = true;
            this.cPatrolledRank.Location = new System.Drawing.Point(204, 175);
            this.cPatrolledRank.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cPatrolledRank.Name = "cPatrolledRank";
            this.cPatrolledRank.Size = new System.Drawing.Size(163, 26);
            this.cPatrolledRank.TabIndex = 8;
            this.cPatrolledRank.SelectedIndexChanged += new System.EventHandler(this.cPatrolledRank_SelectedIndexChanged);
            // 
            // lPatrolledRank
            // 
            this.lPatrolledRank.AutoSize = true;
            this.lPatrolledRank.Location = new System.Drawing.Point(85, 178);
            this.lPatrolledRank.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lPatrolledRank.Name = "lPatrolledRank";
            this.lPatrolledRank.Size = new System.Drawing.Size(99, 18);
            this.lPatrolledRank.TabIndex = 7;
            this.lPatrolledRank.Text = "Patrolled rank";
            // 
            // xAnnounceRankChanges
            // 
            this.xAnnounceRankChanges.AutoSize = true;
            this.xAnnounceRankChanges.Location = new System.Drawing.Point(405, 103);
            this.xAnnounceRankChanges.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xAnnounceRankChanges.Name = "xAnnounceRankChanges";
            this.xAnnounceRankChanges.Size = new System.Drawing.Size(371, 27);
            this.xAnnounceRankChanges.TabIndex = 5;
            this.xAnnounceRankChanges.Text = "Announce promotions and demotions";
            this.xAnnounceRankChanges.UseVisualStyleBackColor = true;
            this.xAnnounceRankChanges.CheckedChanged += new System.EventHandler(this.xAnnounceRankChanges_CheckedChanged);
            // 
            // xAnnounceKickAndBanReasons
            // 
            this.xAnnounceKickAndBanReasons.AutoSize = true;
            this.xAnnounceKickAndBanReasons.Location = new System.Drawing.Point(405, 73);
            this.xAnnounceKickAndBanReasons.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xAnnounceKickAndBanReasons.Name = "xAnnounceKickAndBanReasons";
            this.xAnnounceKickAndBanReasons.Size = new System.Drawing.Size(391, 27);
            this.xAnnounceKickAndBanReasons.TabIndex = 4;
            this.xAnnounceKickAndBanReasons.Text = "Announce kick, ban, and unban reasons";
            this.xAnnounceKickAndBanReasons.UseVisualStyleBackColor = true;
            // 
            // xRequireRankChangeReason
            // 
            this.xRequireRankChangeReason.AutoSize = true;
            this.xRequireRankChangeReason.Location = new System.Drawing.Point(56, 134);
            this.xRequireRankChangeReason.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xRequireRankChangeReason.Name = "xRequireRankChangeReason";
            this.xRequireRankChangeReason.Size = new System.Drawing.Size(377, 27);
            this.xRequireRankChangeReason.TabIndex = 3;
            this.xRequireRankChangeReason.Text = "Require promotion && demotion reason";
            this.xRequireRankChangeReason.UseVisualStyleBackColor = true;
            // 
            // xRequireBanReason
            // 
            this.xRequireBanReason.AutoSize = true;
            this.xRequireBanReason.Location = new System.Drawing.Point(56, 103);
            this.xRequireBanReason.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xRequireBanReason.Name = "xRequireBanReason";
            this.xRequireBanReason.Size = new System.Drawing.Size(289, 27);
            this.xRequireBanReason.TabIndex = 2;
            this.xRequireBanReason.Text = "Require ban && unban reason";
            this.xRequireBanReason.UseVisualStyleBackColor = true;
            // 
            // gSpamChat
            // 
            this.gSpamChat.Controls.Add(this.lAntispamMaxWarnings);
            this.gSpamChat.Controls.Add(this.nAntispamMaxWarnings);
            this.gSpamChat.Controls.Add(this.xAntispamKicks);
            this.gSpamChat.Controls.Add(this.lSpamMuteSeconds);
            this.gSpamChat.Controls.Add(this.lAntispamInterval);
            this.gSpamChat.Controls.Add(this.nSpamMute);
            this.gSpamChat.Controls.Add(this.lSpamMute);
            this.gSpamChat.Controls.Add(this.nAntispamInterval);
            this.gSpamChat.Controls.Add(this.lAntispamMessageCount);
            this.gSpamChat.Controls.Add(this.nAntispamMessageCount);
            this.gSpamChat.Controls.Add(this.lSpamChat);
            this.gSpamChat.Location = new System.Drawing.Point(11, 239);
            this.gSpamChat.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gSpamChat.Name = "gSpamChat";
            this.gSpamChat.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gSpamChat.Size = new System.Drawing.Size(848, 116);
            this.gSpamChat.TabIndex = 2;
            this.gSpamChat.TabStop = false;
            this.gSpamChat.Text = "Chat Spam Prevention";
            // 
            // lAntispamMaxWarnings
            // 
            this.lAntispamMaxWarnings.AutoSize = true;
            this.lAntispamMaxWarnings.Location = new System.Drawing.Point(605, 76);
            this.lAntispamMaxWarnings.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lAntispamMaxWarnings.Name = "lAntispamMaxWarnings";
            this.lAntispamMaxWarnings.Size = new System.Drawing.Size(67, 18);
            this.lAntispamMaxWarnings.TabIndex = 10;
            this.lAntispamMaxWarnings.Text = "warnings";
            // 
            // nAntispamMaxWarnings
            // 
            this.nAntispamMaxWarnings.Location = new System.Drawing.Point(515, 74);
            this.nAntispamMaxWarnings.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nAntispamMaxWarnings.Name = "nAntispamMaxWarnings";
            this.nAntispamMaxWarnings.Size = new System.Drawing.Size(83, 24);
            this.nAntispamMaxWarnings.TabIndex = 9;
            // 
            // xAntispamKicks
            // 
            this.xAntispamKicks.AutoSize = true;
            this.xAntispamKicks.Location = new System.Drawing.Point(405, 75);
            this.xAntispamKicks.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xAntispamKicks.Name = "xAntispamKicks";
            this.xAntispamKicks.Size = new System.Drawing.Size(123, 27);
            this.xAntispamKicks.TabIndex = 8;
            this.xAntispamKicks.Text = "Kick after";
            this.xAntispamKicks.UseVisualStyleBackColor = true;
            this.xAntispamKicks.CheckedChanged += new System.EventHandler(this.xSpamChatKick_CheckedChanged);
            // 
            // lSpamMuteSeconds
            // 
            this.lSpamMuteSeconds.AutoSize = true;
            this.lSpamMuteSeconds.Location = new System.Drawing.Point(295, 76);
            this.lSpamMuteSeconds.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lSpamMuteSeconds.Name = "lSpamMuteSeconds";
            this.lSpamMuteSeconds.Size = new System.Drawing.Size(65, 18);
            this.lSpamMuteSeconds.TabIndex = 7;
            this.lSpamMuteSeconds.Text = "seconds";
            // 
            // lAntispamInterval
            // 
            this.lAntispamInterval.AutoSize = true;
            this.lAntispamInterval.Location = new System.Drawing.Point(496, 33);
            this.lAntispamInterval.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lAntispamInterval.Name = "lAntispamInterval";
            this.lAntispamInterval.Size = new System.Drawing.Size(65, 18);
            this.lAntispamInterval.TabIndex = 4;
            this.lAntispamInterval.Text = "seconds";
            // 
            // nSpamMute
            // 
            this.nSpamMute.Location = new System.Drawing.Point(204, 73);
            this.nSpamMute.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nSpamMute.Name = "nSpamMute";
            this.nSpamMute.Size = new System.Drawing.Size(83, 24);
            this.nSpamMute.TabIndex = 6;
            // 
            // lSpamMute
            // 
            this.lSpamMute.AutoSize = true;
            this.lSpamMute.Location = new System.Drawing.Point(52, 76);
            this.lSpamMute.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lSpamMute.Name = "lSpamMute";
            this.lSpamMute.Size = new System.Drawing.Size(130, 18);
            this.lSpamMute.TabIndex = 5;
            this.lSpamMute.Text = "Mute spammer for";
            // 
            // nAntispamInterval
            // 
            this.nAntispamInterval.Location = new System.Drawing.Point(405, 31);
            this.nAntispamInterval.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nAntispamInterval.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nAntispamInterval.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nAntispamInterval.Name = "nAntispamInterval";
            this.nAntispamInterval.Size = new System.Drawing.Size(83, 24);
            this.nAntispamInterval.TabIndex = 3;
            this.nAntispamInterval.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lAntispamMessageCount
            // 
            this.lAntispamMessageCount.AutoSize = true;
            this.lAntispamMessageCount.Location = new System.Drawing.Point(292, 33);
            this.lAntispamMessageCount.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lAntispamMessageCount.Name = "lAntispamMessageCount";
            this.lAntispamMessageCount.Size = new System.Drawing.Size(92, 18);
            this.lAntispamMessageCount.TabIndex = 2;
            this.lAntispamMessageCount.Text = "messages in";
            // 
            // nAntispamMessageCount
            // 
            this.nAntispamMessageCount.Location = new System.Drawing.Point(204, 31);
            this.nAntispamMessageCount.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nAntispamMessageCount.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nAntispamMessageCount.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.nAntispamMessageCount.Name = "nAntispamMessageCount";
            this.nAntispamMessageCount.Size = new System.Drawing.Size(83, 24);
            this.nAntispamMessageCount.TabIndex = 1;
            this.nAntispamMessageCount.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // lSpamChat
            // 
            this.lSpamChat.AutoSize = true;
            this.lSpamChat.Location = new System.Drawing.Point(67, 33);
            this.lSpamChat.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lSpamChat.Name = "lSpamChat";
            this.lSpamChat.Size = new System.Drawing.Size(117, 18);
            this.lSpamChat.TabIndex = 0;
            this.lSpamChat.Text = "Limit chat rate to";
            // 
            // gVerify
            // 
            this.gVerify.Controls.Add(this.nMaxConnectionsPerIP);
            this.gVerify.Controls.Add(this.xAllowUnverifiedLAN);
            this.gVerify.Controls.Add(this.xMaxConnectionsPerIP);
            this.gVerify.Controls.Add(this.lVerifyNames);
            this.gVerify.Controls.Add(this.cVerifyNames);
            this.gVerify.Location = new System.Drawing.Point(11, 16);
            this.gVerify.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gVerify.Name = "gVerify";
            this.gVerify.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gVerify.Size = new System.Drawing.Size(848, 100);
            this.gVerify.TabIndex = 0;
            this.gVerify.TabStop = false;
            this.gVerify.Text = "Connection";
            // 
            // nMaxConnectionsPerIP
            // 
            this.nMaxConnectionsPerIP.Location = new System.Drawing.Point(719, 26);
            this.nMaxConnectionsPerIP.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nMaxConnectionsPerIP.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nMaxConnectionsPerIP.Name = "nMaxConnectionsPerIP";
            this.nMaxConnectionsPerIP.Size = new System.Drawing.Size(63, 24);
            this.nMaxConnectionsPerIP.TabIndex = 4;
            this.nMaxConnectionsPerIP.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // xAllowUnverifiedLAN
            // 
            this.xAllowUnverifiedLAN.AutoSize = true;
            this.xAllowUnverifiedLAN.Location = new System.Drawing.Point(56, 60);
            this.xAllowUnverifiedLAN.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xAllowUnverifiedLAN.Name = "xAllowUnverifiedLAN";
            this.xAllowUnverifiedLAN.Size = new System.Drawing.Size(785, 27);
            this.xAllowUnverifiedLAN.TabIndex = 2;
            this.xAllowUnverifiedLAN.Text = "Allow connections from LAN without name verification (192.168.0.0/16 and 10.0.0.0" +
    "/8)";
            this.xAllowUnverifiedLAN.UseVisualStyleBackColor = true;
            // 
            // xMaxConnectionsPerIP
            // 
            this.xMaxConnectionsPerIP.AutoSize = true;
            this.xMaxConnectionsPerIP.Location = new System.Drawing.Point(405, 27);
            this.xMaxConnectionsPerIP.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xMaxConnectionsPerIP.Name = "xMaxConnectionsPerIP";
            this.xMaxConnectionsPerIP.Size = new System.Drawing.Size(368, 27);
            this.xMaxConnectionsPerIP.TabIndex = 3;
            this.xMaxConnectionsPerIP.Text = "Limit number of connections per IP to";
            this.xMaxConnectionsPerIP.UseVisualStyleBackColor = true;
            this.xMaxConnectionsPerIP.CheckedChanged += new System.EventHandler(this.xMaxConnectionsPerIP_CheckedChanged);
            // 
            // lVerifyNames
            // 
            this.lVerifyNames.AutoSize = true;
            this.lVerifyNames.Location = new System.Drawing.Point(60, 28);
            this.lVerifyNames.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lVerifyNames.Name = "lVerifyNames";
            this.lVerifyNames.Size = new System.Drawing.Size(122, 18);
            this.lVerifyNames.TabIndex = 0;
            this.lVerifyNames.Text = "Name verification";
            // 
            // cVerifyNames
            // 
            this.cVerifyNames.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cVerifyNames.FormattingEnabled = true;
            this.cVerifyNames.Items.AddRange(new object[] {
            "None (Unsafe)",
            "Normal",
            "Strict"});
            this.cVerifyNames.Location = new System.Drawing.Point(204, 25);
            this.cVerifyNames.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cVerifyNames.Name = "cVerifyNames";
            this.cVerifyNames.Size = new System.Drawing.Size(159, 26);
            this.cVerifyNames.TabIndex = 1;
            this.cVerifyNames.SelectedIndexChanged += new System.EventHandler(this.cVerifyNames_SelectedIndexChanged);
            // 
            // tabSavingAndBackup
            // 
            this.tabSavingAndBackup.Controls.Add(this.gDataBackup);
            this.tabSavingAndBackup.Controls.Add(this.gSaving);
            this.tabSavingAndBackup.Controls.Add(this.gBackups);
            this.tabSavingAndBackup.Location = new System.Drawing.Point(4, 27);
            this.tabSavingAndBackup.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabSavingAndBackup.Name = "tabSavingAndBackup";
            this.tabSavingAndBackup.Padding = new System.Windows.Forms.Padding(7, 12, 7, 12);
            this.tabSavingAndBackup.Size = new System.Drawing.Size(872, 597);
            this.tabSavingAndBackup.TabIndex = 4;
            this.tabSavingAndBackup.Text = "Saving and Backup";
            this.tabSavingAndBackup.UseVisualStyleBackColor = true;
            // 
            // gDataBackup
            // 
            this.gDataBackup.Controls.Add(this.xBackupDataOnStartup);
            this.gDataBackup.Location = new System.Drawing.Point(11, 289);
            this.gDataBackup.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gDataBackup.Name = "gDataBackup";
            this.gDataBackup.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gDataBackup.Size = new System.Drawing.Size(848, 64);
            this.gDataBackup.TabIndex = 2;
            this.gDataBackup.TabStop = false;
            this.gDataBackup.Text = "Data Backup";
            // 
            // xBackupDataOnStartup
            // 
            this.xBackupDataOnStartup.AutoSize = true;
            this.xBackupDataOnStartup.Location = new System.Drawing.Point(21, 25);
            this.xBackupDataOnStartup.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xBackupDataOnStartup.Name = "xBackupDataOnStartup";
            this.xBackupDataOnStartup.Size = new System.Drawing.Size(420, 27);
            this.xBackupDataOnStartup.TabIndex = 0;
            this.xBackupDataOnStartup.Text = "Backup PlayerDB and IP ban list on startup.";
            this.xBackupDataOnStartup.UseVisualStyleBackColor = true;
            // 
            // gSaving
            // 
            this.gSaving.Controls.Add(this.nSaveInterval);
            this.gSaving.Controls.Add(this.lSaveIntervalUnits);
            this.gSaving.Controls.Add(this.xSaveInterval);
            this.gSaving.Location = new System.Drawing.Point(11, 16);
            this.gSaving.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gSaving.Name = "gSaving";
            this.gSaving.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gSaving.Size = new System.Drawing.Size(848, 64);
            this.gSaving.TabIndex = 0;
            this.gSaving.TabStop = false;
            this.gSaving.Text = "Map Saving";
            // 
            // nSaveInterval
            // 
            this.nSaveInterval.Location = new System.Drawing.Point(181, 25);
            this.nSaveInterval.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nSaveInterval.Name = "nSaveInterval";
            this.nSaveInterval.Size = new System.Drawing.Size(64, 24);
            this.nSaveInterval.TabIndex = 1;
            // 
            // lSaveIntervalUnits
            // 
            this.lSaveIntervalUnits.AutoSize = true;
            this.lSaveIntervalUnits.Location = new System.Drawing.Point(253, 27);
            this.lSaveIntervalUnits.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lSaveIntervalUnits.Name = "lSaveIntervalUnits";
            this.lSaveIntervalUnits.Size = new System.Drawing.Size(65, 18);
            this.lSaveIntervalUnits.TabIndex = 2;
            this.lSaveIntervalUnits.Text = "seconds";
            // 
            // xSaveInterval
            // 
            this.xSaveInterval.AutoSize = true;
            this.xSaveInterval.Location = new System.Drawing.Point(16, 26);
            this.xSaveInterval.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xSaveInterval.Name = "xSaveInterval";
            this.xSaveInterval.Size = new System.Drawing.Size(191, 27);
            this.xSaveInterval.TabIndex = 0;
            this.xSaveInterval.Text = "Save maps every";
            this.xSaveInterval.UseVisualStyleBackColor = true;
            this.xSaveInterval.CheckedChanged += new System.EventHandler(this.xSaveAtInterval_CheckedChanged);
            // 
            // gBackups
            // 
            this.gBackups.Controls.Add(this.xBackupOnlyWhenChanged);
            this.gBackups.Controls.Add(this.lMaxBackupSize);
            this.gBackups.Controls.Add(this.xMaxBackupSize);
            this.gBackups.Controls.Add(this.nMaxBackupSize);
            this.gBackups.Controls.Add(this.xMaxBackups);
            this.gBackups.Controls.Add(this.xBackupOnStartup);
            this.gBackups.Controls.Add(this.lMaxBackups);
            this.gBackups.Controls.Add(this.nMaxBackups);
            this.gBackups.Controls.Add(this.nBackupInterval);
            this.gBackups.Controls.Add(this.lBackupIntervalUnits);
            this.gBackups.Controls.Add(this.xBackupInterval);
            this.gBackups.Controls.Add(this.xBackupOnJoin);
            this.gBackups.Location = new System.Drawing.Point(11, 87);
            this.gBackups.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gBackups.Name = "gBackups";
            this.gBackups.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gBackups.Size = new System.Drawing.Size(848, 194);
            this.gBackups.TabIndex = 1;
            this.gBackups.TabStop = false;
            this.gBackups.Text = "Map Backups";
            // 
            // xBackupOnlyWhenChanged
            // 
            this.xBackupOnlyWhenChanged.AutoSize = true;
            this.xBackupOnlyWhenChanged.Location = new System.Drawing.Point(492, 57);
            this.xBackupOnlyWhenChanged.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xBackupOnlyWhenChanged.Name = "xBackupOnlyWhenChanged";
            this.xBackupOnlyWhenChanged.Size = new System.Drawing.Size(413, 27);
            this.xBackupOnlyWhenChanged.TabIndex = 4;
            this.xBackupOnlyWhenChanged.Text = "Skip timed backups if map hasn\'t changed.";
            this.xBackupOnlyWhenChanged.UseVisualStyleBackColor = true;
            // 
            // lMaxBackupSize
            // 
            this.lMaxBackupSize.AutoSize = true;
            this.lMaxBackupSize.Location = new System.Drawing.Point(557, 153);
            this.lMaxBackupSize.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lMaxBackupSize.Name = "lMaxBackupSize";
            this.lMaxBackupSize.Size = new System.Drawing.Size(127, 18);
            this.lMaxBackupSize.TabIndex = 11;
            this.lMaxBackupSize.Text = "MB of disk space.";
            // 
            // xMaxBackupSize
            // 
            this.xMaxBackupSize.AutoSize = true;
            this.xMaxBackupSize.Location = new System.Drawing.Point(21, 151);
            this.xMaxBackupSize.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xMaxBackupSize.Name = "xMaxBackupSize";
            this.xMaxBackupSize.Size = new System.Drawing.Size(511, 27);
            this.xMaxBackupSize.TabIndex = 9;
            this.xMaxBackupSize.Text = "Delete old backups if the directory takes up more than";
            this.xMaxBackupSize.UseVisualStyleBackColor = true;
            this.xMaxBackupSize.CheckedChanged += new System.EventHandler(this.xMaxBackupSize_CheckedChanged);
            // 
            // nMaxBackupSize
            // 
            this.nMaxBackupSize.Location = new System.Drawing.Point(452, 150);
            this.nMaxBackupSize.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nMaxBackupSize.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.nMaxBackupSize.Name = "nMaxBackupSize";
            this.nMaxBackupSize.Size = new System.Drawing.Size(97, 24);
            this.nMaxBackupSize.TabIndex = 10;
            // 
            // xMaxBackups
            // 
            this.xMaxBackups.AutoSize = true;
            this.xMaxBackups.Location = new System.Drawing.Point(21, 121);
            this.xMaxBackups.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xMaxBackups.Name = "xMaxBackups";
            this.xMaxBackups.Size = new System.Drawing.Size(400, 27);
            this.xMaxBackups.TabIndex = 6;
            this.xMaxBackups.Text = "Delete old backups if there are more than";
            this.xMaxBackups.UseVisualStyleBackColor = true;
            this.xMaxBackups.CheckedChanged += new System.EventHandler(this.xMaxBackups_CheckedChanged);
            // 
            // xBackupOnStartup
            // 
            this.xBackupOnStartup.AutoSize = true;
            this.xBackupOnStartup.Enabled = false;
            this.xBackupOnStartup.Location = new System.Drawing.Point(21, 25);
            this.xBackupOnStartup.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xBackupOnStartup.Name = "xBackupOnStartup";
            this.xBackupOnStartup.Size = new System.Drawing.Size(272, 27);
            this.xBackupOnStartup.TabIndex = 0;
            this.xBackupOnStartup.Text = "Create backups on startup";
            this.xBackupOnStartup.UseVisualStyleBackColor = true;
            // 
            // lMaxBackups
            // 
            this.lMaxBackups.AutoSize = true;
            this.lMaxBackups.Location = new System.Drawing.Point(448, 122);
            this.lMaxBackups.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lMaxBackups.Name = "lMaxBackups";
            this.lMaxBackups.Size = new System.Drawing.Size(190, 18);
            this.lMaxBackups.TabIndex = 8;
            this.lMaxBackups.Text = "files in the backup directory.";
            // 
            // nMaxBackups
            // 
            this.nMaxBackups.Location = new System.Drawing.Point(364, 119);
            this.nMaxBackups.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nMaxBackups.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.nMaxBackups.Name = "nMaxBackups";
            this.nMaxBackups.Size = new System.Drawing.Size(76, 24);
            this.nMaxBackups.TabIndex = 7;
            // 
            // nBackupInterval
            // 
            this.nBackupInterval.Location = new System.Drawing.Point(219, 55);
            this.nBackupInterval.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nBackupInterval.Name = "nBackupInterval";
            this.nBackupInterval.Size = new System.Drawing.Size(64, 24);
            this.nBackupInterval.TabIndex = 2;
            // 
            // lBackupIntervalUnits
            // 
            this.lBackupIntervalUnits.AutoSize = true;
            this.lBackupIntervalUnits.Location = new System.Drawing.Point(291, 58);
            this.lBackupIntervalUnits.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lBackupIntervalUnits.Name = "lBackupIntervalUnits";
            this.lBackupIntervalUnits.Size = new System.Drawing.Size(60, 18);
            this.lBackupIntervalUnits.TabIndex = 3;
            this.lBackupIntervalUnits.Text = "minutes";
            // 
            // xBackupInterval
            // 
            this.xBackupInterval.AutoSize = true;
            this.xBackupInterval.Location = new System.Drawing.Point(21, 57);
            this.xBackupInterval.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xBackupInterval.Name = "xBackupInterval";
            this.xBackupInterval.Size = new System.Drawing.Size(231, 27);
            this.xBackupInterval.TabIndex = 1;
            this.xBackupInterval.Text = "Create backups every";
            this.xBackupInterval.UseVisualStyleBackColor = true;
            this.xBackupInterval.CheckedChanged += new System.EventHandler(this.xBackupAtInterval_CheckedChanged);
            // 
            // xBackupOnJoin
            // 
            this.xBackupOnJoin.AutoSize = true;
            this.xBackupOnJoin.Location = new System.Drawing.Point(21, 89);
            this.xBackupOnJoin.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xBackupOnJoin.Name = "xBackupOnJoin";
            this.xBackupOnJoin.Size = new System.Drawing.Size(447, 27);
            this.xBackupOnJoin.TabIndex = 5;
            this.xBackupOnJoin.Text = "Create backup whenever a player joins a world";
            this.xBackupOnJoin.UseVisualStyleBackColor = true;
            // 
            // tabLogging
            // 
            this.tabLogging.Controls.Add(this.gLogFile);
            this.tabLogging.Controls.Add(this.gConsole);
            this.tabLogging.Location = new System.Drawing.Point(4, 27);
            this.tabLogging.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabLogging.Name = "tabLogging";
            this.tabLogging.Padding = new System.Windows.Forms.Padding(7, 12, 7, 12);
            this.tabLogging.Size = new System.Drawing.Size(872, 597);
            this.tabLogging.TabIndex = 5;
            this.tabLogging.Text = "Logging";
            this.tabLogging.UseVisualStyleBackColor = true;
            // 
            // gLogFile
            // 
            this.gLogFile.Controls.Add(this.lLogFileOptionsDescription);
            this.gLogFile.Controls.Add(this.xLogLimit);
            this.gLogFile.Controls.Add(this.vLogFileOptions);
            this.gLogFile.Controls.Add(this.lLogLimitUnits);
            this.gLogFile.Controls.Add(this.nLogLimit);
            this.gLogFile.Controls.Add(this.cLogMode);
            this.gLogFile.Controls.Add(this.lLogMode);
            this.gLogFile.Location = new System.Drawing.Point(439, 16);
            this.gLogFile.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gLogFile.Name = "gLogFile";
            this.gLogFile.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gLogFile.Size = new System.Drawing.Size(420, 521);
            this.gLogFile.TabIndex = 1;
            this.gLogFile.TabStop = false;
            this.gLogFile.Text = "Log File";
            // 
            // lLogFileOptionsDescription
            // 
            this.lLogFileOptionsDescription.AutoSize = true;
            this.lLogFileOptionsDescription.Location = new System.Drawing.Point(36, 27);
            this.lLogFileOptionsDescription.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lLogFileOptionsDescription.Name = "lLogFileOptionsDescription";
            this.lLogFileOptionsDescription.Size = new System.Drawing.Size(257, 36);
            this.lLogFileOptionsDescription.TabIndex = 0;
            this.lLogFileOptionsDescription.Text = "Types of messages that will be written\r\nto the log file on disk.";
            // 
            // xLogLimit
            // 
            this.xLogLimit.AutoSize = true;
            this.xLogLimit.Enabled = false;
            this.xLogLimit.Location = new System.Drawing.Point(24, 480);
            this.xLogLimit.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xLogLimit.Name = "xLogLimit";
            this.xLogLimit.Size = new System.Drawing.Size(128, 27);
            this.xLogLimit.TabIndex = 4;
            this.xLogLimit.Text = "Only keep";
            this.xLogLimit.UseVisualStyleBackColor = true;
            this.xLogLimit.CheckedChanged += new System.EventHandler(this.xLogLimit_CheckedChanged);
            // 
            // vLogFileOptions
            // 
            this.vLogFileOptions.CheckBoxes = true;
            this.vLogFileOptions.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader2});
            this.vLogFileOptions.GridLines = true;
            this.vLogFileOptions.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.vLogFileOptions.Location = new System.Drawing.Point(104, 73);
            this.vLogFileOptions.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.vLogFileOptions.Name = "vLogFileOptions";
            this.vLogFileOptions.ShowItemToolTips = true;
            this.vLogFileOptions.Size = new System.Drawing.Size(213, 361);
            this.vLogFileOptions.TabIndex = 1;
            this.vLogFileOptions.UseCompatibleStateImageBehavior = false;
            this.vLogFileOptions.View = System.Windows.Forms.View.Details;
            this.vLogFileOptions.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.vLogFileOptions_ItemChecked);
            // 
            // columnHeader2
            // 
            this.columnHeader2.Width = 157;
            // 
            // lLogLimitUnits
            // 
            this.lLogLimitUnits.AutoSize = true;
            this.lLogLimitUnits.Location = new System.Drawing.Point(221, 481);
            this.lLogLimitUnits.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lLogLimitUnits.Name = "lLogLimitUnits";
            this.lLogLimitUnits.Size = new System.Drawing.Size(158, 18);
            this.lLogLimitUnits.TabIndex = 6;
            this.lLogLimitUnits.Text = "of most recent log files";
            // 
            // nLogLimit
            // 
            this.nLogLimit.Enabled = false;
            this.nLogLimit.Location = new System.Drawing.Point(139, 479);
            this.nLogLimit.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nLogLimit.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nLogLimit.Name = "nLogLimit";
            this.nLogLimit.Size = new System.Drawing.Size(75, 24);
            this.nLogLimit.TabIndex = 5;
            // 
            // cLogMode
            // 
            this.cLogMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cLogMode.FormattingEnabled = true;
            this.cLogMode.Items.AddRange(new object[] {
            "One long file",
            "Multiple files, split by session",
            "Multiple files, split by day"});
            this.cLogMode.Location = new System.Drawing.Point(139, 443);
            this.cLogMode.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cLogMode.Name = "cLogMode";
            this.cLogMode.Size = new System.Drawing.Size(245, 26);
            this.cLogMode.TabIndex = 3;
            // 
            // lLogMode
            // 
            this.lLogMode.AutoSize = true;
            this.lLogMode.Location = new System.Drawing.Point(47, 447);
            this.lLogMode.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lLogMode.Name = "lLogMode";
            this.lLogMode.Size = new System.Drawing.Size(75, 18);
            this.lLogMode.TabIndex = 2;
            this.lLogMode.Text = "Log mode";
            // 
            // gConsole
            // 
            this.gConsole.Controls.Add(this.lLogConsoleOptionsDescription);
            this.gConsole.Controls.Add(this.vConsoleOptions);
            this.gConsole.Location = new System.Drawing.Point(11, 16);
            this.gConsole.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gConsole.Name = "gConsole";
            this.gConsole.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gConsole.Size = new System.Drawing.Size(420, 521);
            this.gConsole.TabIndex = 0;
            this.gConsole.TabStop = false;
            this.gConsole.Text = "Console";
            // 
            // lLogConsoleOptionsDescription
            // 
            this.lLogConsoleOptionsDescription.AutoSize = true;
            this.lLogConsoleOptionsDescription.Location = new System.Drawing.Point(12, 26);
            this.lLogConsoleOptionsDescription.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lLogConsoleOptionsDescription.Name = "lLogConsoleOptionsDescription";
            this.lLogConsoleOptionsDescription.Size = new System.Drawing.Size(257, 36);
            this.lLogConsoleOptionsDescription.TabIndex = 0;
            this.lLogConsoleOptionsDescription.Text = "Types of messages that will be written\r\ndirectly to console.";
            // 
            // vConsoleOptions
            // 
            this.vConsoleOptions.CheckBoxes = true;
            this.vConsoleOptions.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader3});
            this.vConsoleOptions.GridLines = true;
            this.vConsoleOptions.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.vConsoleOptions.Location = new System.Drawing.Point(101, 73);
            this.vConsoleOptions.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.vConsoleOptions.Name = "vConsoleOptions";
            this.vConsoleOptions.ShowItemToolTips = true;
            this.vConsoleOptions.Size = new System.Drawing.Size(213, 361);
            this.vConsoleOptions.TabIndex = 1;
            this.vConsoleOptions.UseCompatibleStateImageBehavior = false;
            this.vConsoleOptions.View = System.Windows.Forms.View.Details;
            this.vConsoleOptions.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.vConsoleOptions_ItemChecked);
            // 
            // columnHeader3
            // 
            this.columnHeader3.Width = 157;
            // 
            // tabIRC
            // 
            this.tabIRC.Controls.Add(this.xIRCListShowNonEnglish);
            this.tabIRC.Controls.Add(this.gIRCOptions);
            this.tabIRC.Controls.Add(this.gIRCNetwork);
            this.tabIRC.Controls.Add(this.lIRCList);
            this.tabIRC.Controls.Add(this.xIRCBotEnabled);
            this.tabIRC.Controls.Add(this.cIRCList);
            this.tabIRC.Location = new System.Drawing.Point(4, 27);
            this.tabIRC.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabIRC.Name = "tabIRC";
            this.tabIRC.Padding = new System.Windows.Forms.Padding(7, 12, 7, 12);
            this.tabIRC.Size = new System.Drawing.Size(872, 597);
            this.tabIRC.TabIndex = 8;
            this.tabIRC.Text = "IRC";
            this.tabIRC.UseVisualStyleBackColor = true;
            // 
            // xIRCListShowNonEnglish
            // 
            this.xIRCListShowNonEnglish.AutoSize = true;
            this.xIRCListShowNonEnglish.Enabled = false;
            this.xIRCListShowNonEnglish.Location = new System.Drawing.Point(620, 16);
            this.xIRCListShowNonEnglish.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xIRCListShowNonEnglish.Name = "xIRCListShowNonEnglish";
            this.xIRCListShowNonEnglish.Size = new System.Drawing.Size(287, 27);
            this.xIRCListShowNonEnglish.TabIndex = 3;
            this.xIRCListShowNonEnglish.Text = "Show non-English networks";
            this.xIRCListShowNonEnglish.UseVisualStyleBackColor = true;
            this.xIRCListShowNonEnglish.CheckedChanged += new System.EventHandler(this.xIRCListShowNonEnglish_CheckedChanged);
            // 
            // gIRCOptions
            // 
            this.gIRCOptions.Controls.Add(this.xIRCBotAnnounceServerEvents);
            this.gIRCOptions.Controls.Add(this.xIRCUseColor);
            this.gIRCOptions.Controls.Add(this.lIRCNoForwardingMessage);
            this.gIRCOptions.Controls.Add(this.xIRCBotAnnounceIRCJoins);
            this.gIRCOptions.Controls.Add(this.bColorIRC);
            this.gIRCOptions.Controls.Add(this.lColorIRC);
            this.gIRCOptions.Controls.Add(this.xIRCBotForwardFromIRC);
            this.gIRCOptions.Controls.Add(this.xIRCBotAnnounceServerJoins);
            this.gIRCOptions.Controls.Add(this.xIRCBotForwardFromServer);
            this.gIRCOptions.Location = new System.Drawing.Point(11, 254);
            this.gIRCOptions.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gIRCOptions.Name = "gIRCOptions";
            this.gIRCOptions.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gIRCOptions.Size = new System.Drawing.Size(848, 199);
            this.gIRCOptions.TabIndex = 5;
            this.gIRCOptions.TabStop = false;
            this.gIRCOptions.Text = "Options";
            // 
            // xIRCBotAnnounceServerEvents
            // 
            this.xIRCBotAnnounceServerEvents.AutoSize = true;
            this.xIRCBotAnnounceServerEvents.Location = new System.Drawing.Point(51, 134);
            this.xIRCBotAnnounceServerEvents.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xIRCBotAnnounceServerEvents.Name = "xIRCBotAnnounceServerEvents";
            this.xIRCBotAnnounceServerEvents.Size = new System.Drawing.Size(684, 27);
            this.xIRCBotAnnounceServerEvents.TabIndex = 7;
            this.xIRCBotAnnounceServerEvents.Text = "Announce SERVER events (kicks, bans, promotions, demotions) on IRC.";
            this.xIRCBotAnnounceServerEvents.UseVisualStyleBackColor = true;
            // 
            // xIRCUseColor
            // 
            this.xIRCUseColor.AutoSize = true;
            this.xIRCUseColor.Location = new System.Drawing.Point(433, 28);
            this.xIRCUseColor.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xIRCUseColor.Name = "xIRCUseColor";
            this.xIRCUseColor.Size = new System.Drawing.Size(245, 27);
            this.xIRCUseColor.TabIndex = 2;
            this.xIRCUseColor.Text = "Use text colors on IRC.";
            this.xIRCUseColor.UseVisualStyleBackColor = true;
            // 
            // lIRCNoForwardingMessage
            // 
            this.lIRCNoForwardingMessage.AutoSize = true;
            this.lIRCNoForwardingMessage.Location = new System.Drawing.Point(47, 169);
            this.lIRCNoForwardingMessage.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lIRCNoForwardingMessage.Name = "lIRCNoForwardingMessage";
            this.lIRCNoForwardingMessage.Size = new System.Drawing.Size(683, 18);
            this.lIRCNoForwardingMessage.TabIndex = 8;
            this.lIRCNoForwardingMessage.Text = "NOTE: If forwarding all messages is not enabled, only messages starting with a ha" +
    "sh (#) will be relayed.";
            // 
            // xIRCBotAnnounceIRCJoins
            // 
            this.xIRCBotAnnounceIRCJoins.AutoSize = true;
            this.xIRCBotAnnounceIRCJoins.Location = new System.Drawing.Point(433, 103);
            this.xIRCBotAnnounceIRCJoins.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xIRCBotAnnounceIRCJoins.Name = "xIRCBotAnnounceIRCJoins";
            this.xIRCBotAnnounceIRCJoins.Size = new System.Drawing.Size(479, 27);
            this.xIRCBotAnnounceIRCJoins.TabIndex = 6;
            this.xIRCBotAnnounceIRCJoins.Text = "Announce people joining/leaving the IRC channels.";
            this.xIRCBotAnnounceIRCJoins.UseVisualStyleBackColor = true;
            // 
            // bColorIRC
            // 
            this.bColorIRC.BackColor = System.Drawing.Color.White;
            this.bColorIRC.Location = new System.Drawing.Point(203, 25);
            this.bColorIRC.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bColorIRC.Name = "bColorIRC";
            this.bColorIRC.Size = new System.Drawing.Size(133, 28);
            this.bColorIRC.TabIndex = 1;
            this.bColorIRC.UseVisualStyleBackColor = false;
            this.bColorIRC.Click += new System.EventHandler(this.bColorIRC_Click);
            // 
            // lColorIRC
            // 
            this.lColorIRC.AutoSize = true;
            this.lColorIRC.Location = new System.Drawing.Point(47, 30);
            this.lColorIRC.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lColorIRC.Name = "lColorIRC";
            this.lColorIRC.Size = new System.Drawing.Size(136, 18);
            this.lColorIRC.TabIndex = 0;
            this.lColorIRC.Text = "IRC message color";
            // 
            // xIRCBotForwardFromIRC
            // 
            this.xIRCBotForwardFromIRC.AutoSize = true;
            this.xIRCBotForwardFromIRC.Location = new System.Drawing.Point(51, 103);
            this.xIRCBotForwardFromIRC.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xIRCBotForwardFromIRC.Name = "xIRCBotForwardFromIRC";
            this.xIRCBotForwardFromIRC.Size = new System.Drawing.Size(395, 27);
            this.xIRCBotForwardFromIRC.TabIndex = 4;
            this.xIRCBotForwardFromIRC.Text = "Forward ALL chat from IRC to SERVER.";
            this.xIRCBotForwardFromIRC.UseVisualStyleBackColor = true;
            // 
            // xIRCBotAnnounceServerJoins
            // 
            this.xIRCBotAnnounceServerJoins.AutoSize = true;
            this.xIRCBotAnnounceServerJoins.Location = new System.Drawing.Point(433, 73);
            this.xIRCBotAnnounceServerJoins.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xIRCBotAnnounceServerJoins.Name = "xIRCBotAnnounceServerJoins";
            this.xIRCBotAnnounceServerJoins.Size = new System.Drawing.Size(443, 27);
            this.xIRCBotAnnounceServerJoins.TabIndex = 5;
            this.xIRCBotAnnounceServerJoins.Text = "Announce people joining/leaving the SERVER.";
            this.xIRCBotAnnounceServerJoins.UseVisualStyleBackColor = true;
            // 
            // xIRCBotForwardFromServer
            // 
            this.xIRCBotForwardFromServer.AutoSize = true;
            this.xIRCBotForwardFromServer.Location = new System.Drawing.Point(51, 73);
            this.xIRCBotForwardFromServer.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xIRCBotForwardFromServer.Name = "xIRCBotForwardFromServer";
            this.xIRCBotForwardFromServer.Size = new System.Drawing.Size(395, 27);
            this.xIRCBotForwardFromServer.TabIndex = 3;
            this.xIRCBotForwardFromServer.Text = "Forward ALL chat from SERVER to IRC.";
            this.xIRCBotForwardFromServer.UseVisualStyleBackColor = true;
            // 
            // gIRCNetwork
            // 
            this.gIRCNetwork.Controls.Add(this.lIRCDelayUnits);
            this.gIRCNetwork.Controls.Add(this.xIRCRegisteredNick);
            this.gIRCNetwork.Controls.Add(this.tIRCNickServMessage);
            this.gIRCNetwork.Controls.Add(this.lIRCNickServMessage);
            this.gIRCNetwork.Controls.Add(this.tIRCNickServ);
            this.gIRCNetwork.Controls.Add(this.lIRCNickServ);
            this.gIRCNetwork.Controls.Add(this.nIRCDelay);
            this.gIRCNetwork.Controls.Add(this.lIRCDelay);
            this.gIRCNetwork.Controls.Add(this.lIRCBotChannels2);
            this.gIRCNetwork.Controls.Add(this.lIRCBotChannels3);
            this.gIRCNetwork.Controls.Add(this.tIRCBotChannels);
            this.gIRCNetwork.Controls.Add(this.lIRCBotChannels);
            this.gIRCNetwork.Controls.Add(this.nIRCBotPort);
            this.gIRCNetwork.Controls.Add(this.lIRCBotPort);
            this.gIRCNetwork.Controls.Add(this.tIRCBotNetwork);
            this.gIRCNetwork.Controls.Add(this.lIRCBotNetwork);
            this.gIRCNetwork.Controls.Add(this.lIRCBotNick);
            this.gIRCNetwork.Controls.Add(this.tIRCBotNick);
            this.gIRCNetwork.Location = new System.Drawing.Point(11, 49);
            this.gIRCNetwork.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gIRCNetwork.Name = "gIRCNetwork";
            this.gIRCNetwork.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gIRCNetwork.Size = new System.Drawing.Size(848, 197);
            this.gIRCNetwork.TabIndex = 4;
            this.gIRCNetwork.TabStop = false;
            this.gIRCNetwork.Text = "Network";
            // 
            // lIRCDelayUnits
            // 
            this.lIRCDelayUnits.AutoSize = true;
            this.lIRCDelayUnits.Location = new System.Drawing.Point(797, 27);
            this.lIRCDelayUnits.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lIRCDelayUnits.Name = "lIRCDelayUnits";
            this.lIRCDelayUnits.Size = new System.Drawing.Size(29, 18);
            this.lIRCDelayUnits.TabIndex = 6;
            this.lIRCDelayUnits.Text = "ms";
            // 
            // xIRCRegisteredNick
            // 
            this.xIRCRegisteredNick.AutoSize = true;
            this.xIRCRegisteredNick.Location = new System.Drawing.Point(353, 124);
            this.xIRCRegisteredNick.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xIRCRegisteredNick.Name = "xIRCRegisteredNick";
            this.xIRCRegisteredNick.Size = new System.Drawing.Size(135, 27);
            this.xIRCRegisteredNick.TabIndex = 13;
            this.xIRCRegisteredNick.Text = "Registered";
            this.xIRCRegisteredNick.UseVisualStyleBackColor = true;
            this.xIRCRegisteredNick.CheckedChanged += new System.EventHandler(this.xIRCRegisteredNick_CheckedChanged);
            // 
            // tIRCNickServMessage
            // 
            this.tIRCNickServMessage.Enabled = false;
            this.tIRCNickServMessage.Location = new System.Drawing.Point(517, 155);
            this.tIRCNickServMessage.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tIRCNickServMessage.Name = "tIRCNickServMessage";
            this.tIRCNickServMessage.Size = new System.Drawing.Size(311, 24);
            this.tIRCNickServMessage.TabIndex = 17;
            // 
            // lIRCNickServMessage
            // 
            this.lIRCNickServMessage.AutoSize = true;
            this.lIRCNickServMessage.Enabled = false;
            this.lIRCNickServMessage.Location = new System.Drawing.Point(353, 159);
            this.lIRCNickServMessage.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lIRCNickServMessage.Name = "lIRCNickServMessage";
            this.lIRCNickServMessage.Size = new System.Drawing.Size(140, 18);
            this.lIRCNickServMessage.TabIndex = 16;
            this.lIRCNickServMessage.Text = "Authentication string";
            // 
            // tIRCNickServ
            // 
            this.tIRCNickServ.Enabled = false;
            this.tIRCNickServ.Location = new System.Drawing.Point(161, 155);
            this.tIRCNickServ.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tIRCNickServ.MaxLength = 32;
            this.tIRCNickServ.Name = "tIRCNickServ";
            this.tIRCNickServ.Size = new System.Drawing.Size(183, 24);
            this.tIRCNickServ.TabIndex = 15;
            // 
            // lIRCNickServ
            // 
            this.lIRCNickServ.AutoSize = true;
            this.lIRCNickServ.Enabled = false;
            this.lIRCNickServ.Location = new System.Drawing.Point(47, 159);
            this.lIRCNickServ.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lIRCNickServ.Name = "lIRCNickServ";
            this.lIRCNickServ.Size = new System.Drawing.Size(99, 18);
            this.lIRCNickServ.TabIndex = 14;
            this.lIRCNickServ.Text = "NickServ nick";
            // 
            // nIRCDelay
            // 
            this.nIRCDelay.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nIRCDelay.Location = new System.Drawing.Point(715, 25);
            this.nIRCDelay.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nIRCDelay.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nIRCDelay.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nIRCDelay.Name = "nIRCDelay";
            this.nIRCDelay.Size = new System.Drawing.Size(75, 24);
            this.nIRCDelay.TabIndex = 5;
            this.nIRCDelay.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lIRCDelay
            // 
            this.lIRCDelay.AutoSize = true;
            this.lIRCDelay.Location = new System.Drawing.Point(555, 27);
            this.lIRCDelay.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lIRCDelay.Name = "lIRCDelay";
            this.lIRCDelay.Size = new System.Drawing.Size(135, 18);
            this.lIRCDelay.TabIndex = 4;
            this.lIRCDelay.Text = "Min message delay";
            // 
            // lIRCBotChannels2
            // 
            this.lIRCBotChannels2.AutoSize = true;
            this.lIRCBotChannels2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lIRCBotChannels2.Location = new System.Drawing.Point(20, 80);
            this.lIRCBotChannels2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lIRCBotChannels2.Name = "lIRCBotChannels2";
            this.lIRCBotChannels2.Size = new System.Drawing.Size(131, 17);
            this.lIRCBotChannels2.TabIndex = 9;
            this.lIRCBotChannels2.Text = "(comma seperated)";
            // 
            // lIRCBotChannels3
            // 
            this.lIRCBotChannels3.AutoSize = true;
            this.lIRCBotChannels3.Location = new System.Drawing.Point(157, 87);
            this.lIRCBotChannels3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lIRCBotChannels3.Name = "lIRCBotChannels3";
            this.lIRCBotChannels3.Size = new System.Drawing.Size(415, 18);
            this.lIRCBotChannels3.TabIndex = 10;
            this.lIRCBotChannels3.Text = "NOTE: Channel names are case-sensitive on some networks!";
            // 
            // tIRCBotChannels
            // 
            this.tIRCBotChannels.Location = new System.Drawing.Point(161, 58);
            this.tIRCBotChannels.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tIRCBotChannels.MaxLength = 1000;
            this.tIRCBotChannels.Name = "tIRCBotChannels";
            this.tIRCBotChannels.Size = new System.Drawing.Size(667, 24);
            this.tIRCBotChannels.TabIndex = 8;
            // 
            // lIRCBotChannels
            // 
            this.lIRCBotChannels.AutoSize = true;
            this.lIRCBotChannels.Location = new System.Drawing.Point(27, 62);
            this.lIRCBotChannels.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lIRCBotChannels.Name = "lIRCBotChannels";
            this.lIRCBotChannels.Size = new System.Drawing.Size(114, 18);
            this.lIRCBotChannels.TabIndex = 7;
            this.lIRCBotChannels.Text = "Channels to join";
            // 
            // nIRCBotPort
            // 
            this.nIRCBotPort.Location = new System.Drawing.Point(400, 25);
            this.nIRCBotPort.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nIRCBotPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nIRCBotPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nIRCBotPort.Name = "nIRCBotPort";
            this.nIRCBotPort.Size = new System.Drawing.Size(85, 24);
            this.nIRCBotPort.TabIndex = 3;
            this.nIRCBotPort.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lIRCBotPort
            // 
            this.lIRCBotPort.AutoSize = true;
            this.lIRCBotPort.Location = new System.Drawing.Point(353, 27);
            this.lIRCBotPort.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lIRCBotPort.Name = "lIRCBotPort";
            this.lIRCBotPort.Size = new System.Drawing.Size(36, 18);
            this.lIRCBotPort.TabIndex = 2;
            this.lIRCBotPort.Text = "Port";
            // 
            // tIRCBotNetwork
            // 
            this.tIRCBotNetwork.Location = new System.Drawing.Point(161, 23);
            this.tIRCBotNetwork.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tIRCBotNetwork.MaxLength = 512;
            this.tIRCBotNetwork.Name = "tIRCBotNetwork";
            this.tIRCBotNetwork.Size = new System.Drawing.Size(183, 24);
            this.tIRCBotNetwork.TabIndex = 1;
            // 
            // lIRCBotNetwork
            // 
            this.lIRCBotNetwork.AutoSize = true;
            this.lIRCBotNetwork.Location = new System.Drawing.Point(35, 27);
            this.lIRCBotNetwork.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lIRCBotNetwork.Name = "lIRCBotNetwork";
            this.lIRCBotNetwork.Size = new System.Drawing.Size(111, 18);
            this.lIRCBotNetwork.TabIndex = 0;
            this.lIRCBotNetwork.Text = "IRC server host";
            // 
            // lIRCBotNick
            // 
            this.lIRCBotNick.AutoSize = true;
            this.lIRCBotNick.Location = new System.Drawing.Point(87, 126);
            this.lIRCBotNick.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lIRCBotNick.Name = "lIRCBotNick";
            this.lIRCBotNick.Size = new System.Drawing.Size(62, 18);
            this.lIRCBotNick.TabIndex = 11;
            this.lIRCBotNick.Text = "Bot nick";
            // 
            // tIRCBotNick
            // 
            this.tIRCBotNick.Location = new System.Drawing.Point(161, 122);
            this.tIRCBotNick.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tIRCBotNick.MaxLength = 32;
            this.tIRCBotNick.Name = "tIRCBotNick";
            this.tIRCBotNick.Size = new System.Drawing.Size(183, 24);
            this.tIRCBotNick.TabIndex = 12;
            // 
            // lIRCList
            // 
            this.lIRCList.AutoSize = true;
            this.lIRCList.Enabled = false;
            this.lIRCList.Location = new System.Drawing.Point(284, 17);
            this.lIRCList.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lIRCList.Name = "lIRCList";
            this.lIRCList.Size = new System.Drawing.Size(128, 18);
            this.lIRCList.TabIndex = 1;
            this.lIRCList.Text = "Popular networks:";
            // 
            // xIRCBotEnabled
            // 
            this.xIRCBotEnabled.AutoSize = true;
            this.xIRCBotEnabled.Location = new System.Drawing.Point(19, 16);
            this.xIRCBotEnabled.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xIRCBotEnabled.Name = "xIRCBotEnabled";
            this.xIRCBotEnabled.Size = new System.Drawing.Size(235, 27);
            this.xIRCBotEnabled.TabIndex = 0;
            this.xIRCBotEnabled.Text = "Enable IRC integration";
            this.xIRCBotEnabled.UseVisualStyleBackColor = true;
            this.xIRCBotEnabled.CheckedChanged += new System.EventHandler(this.xIRC_CheckedChanged);
            // 
            // cIRCList
            // 
            this.cIRCList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cIRCList.Enabled = false;
            this.cIRCList.FormattingEnabled = true;
            this.cIRCList.Location = new System.Drawing.Point(428, 14);
            this.cIRCList.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cIRCList.Name = "cIRCList";
            this.cIRCList.Size = new System.Drawing.Size(183, 26);
            this.cIRCList.TabIndex = 2;
            this.cIRCList.SelectedIndexChanged += new System.EventHandler(this.cIRCList_SelectedIndexChanged);
            // 
            // tabAdvanced
            // 
            this.tabAdvanced.Controls.Add(this.gPerformance);
            this.tabAdvanced.Controls.Add(this.gAdvancedMisc);
            this.tabAdvanced.Controls.Add(this.gCrashReport);
            this.tabAdvanced.Location = new System.Drawing.Point(4, 27);
            this.tabAdvanced.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabAdvanced.Name = "tabAdvanced";
            this.tabAdvanced.Padding = new System.Windows.Forms.Padding(7, 12, 7, 12);
            this.tabAdvanced.Size = new System.Drawing.Size(872, 597);
            this.tabAdvanced.TabIndex = 6;
            this.tabAdvanced.Text = "Advanced";
            this.tabAdvanced.UseVisualStyleBackColor = true;
            // 
            // gPerformance
            // 
            this.gPerformance.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gPerformance.Controls.Add(this.lAdvancedWarning);
            this.gPerformance.Controls.Add(this.xLowLatencyMode);
            this.gPerformance.Controls.Add(this.lProcessPriority);
            this.gPerformance.Controls.Add(this.cProcessPriority);
            this.gPerformance.Controls.Add(this.nTickInterval);
            this.gPerformance.Controls.Add(this.lTickIntervalUnits);
            this.gPerformance.Controls.Add(this.lTickInterval);
            this.gPerformance.Controls.Add(this.nThrottling);
            this.gPerformance.Controls.Add(this.lThrottling);
            this.gPerformance.Controls.Add(this.lThrottlingUnits);
            this.gPerformance.Location = new System.Drawing.Point(11, 404);
            this.gPerformance.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gPerformance.Name = "gPerformance";
            this.gPerformance.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gPerformance.Size = new System.Drawing.Size(848, 186);
            this.gPerformance.TabIndex = 2;
            this.gPerformance.TabStop = false;
            this.gPerformance.Text = "Performance";
            // 
            // lAdvancedWarning
            // 
            this.lAdvancedWarning.AutoSize = true;
            this.lAdvancedWarning.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lAdvancedWarning.Location = new System.Drawing.Point(20, 26);
            this.lAdvancedWarning.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lAdvancedWarning.Name = "lAdvancedWarning";
            this.lAdvancedWarning.Size = new System.Drawing.Size(658, 36);
            this.lAdvancedWarning.TabIndex = 0;
            this.lAdvancedWarning.Text = "Warning: Altering these settings may decrease your server\'s stability and perform" +
    "ance.\r\nIf you\'re not sure what these settings do, you probably shouldn\'t change " +
    "them...";
            // 
            // xLowLatencyMode
            // 
            this.xLowLatencyMode.AutoSize = true;
            this.xLowLatencyMode.Location = new System.Drawing.Point(8, 79);
            this.xLowLatencyMode.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xLowLatencyMode.Name = "xLowLatencyMode";
            this.xLowLatencyMode.Size = new System.Drawing.Size(864, 27);
            this.xLowLatencyMode.TabIndex = 3;
            this.xLowLatencyMode.Text = "Low-latency mode (disables Nagle\'s algorithm, reducing latency but increasing ban" +
    "dwidth use).";
            this.xLowLatencyMode.UseVisualStyleBackColor = true;
            // 
            // lProcessPriority
            // 
            this.lProcessPriority.AutoSize = true;
            this.lProcessPriority.Location = new System.Drawing.Point(25, 116);
            this.lProcessPriority.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lProcessPriority.Name = "lProcessPriority";
            this.lProcessPriority.Size = new System.Drawing.Size(112, 18);
            this.lProcessPriority.TabIndex = 10;
            this.lProcessPriority.Text = "Process priority";
            // 
            // cProcessPriority
            // 
            this.cProcessPriority.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cProcessPriority.Items.AddRange(new object[] {
            "(system default)",
            "High",
            "Above Normal",
            "Normal",
            "Below Normal",
            "Low"});
            this.cProcessPriority.Location = new System.Drawing.Point(153, 112);
            this.cProcessPriority.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cProcessPriority.Name = "cProcessPriority";
            this.cProcessPriority.Size = new System.Drawing.Size(144, 26);
            this.cProcessPriority.TabIndex = 11;
            // 
            // nTickInterval
            // 
            this.nTickInterval.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nTickInterval.Location = new System.Drawing.Point(572, 113);
            this.nTickInterval.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nTickInterval.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nTickInterval.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nTickInterval.Name = "nTickInterval";
            this.nTickInterval.Size = new System.Drawing.Size(93, 24);
            this.nTickInterval.TabIndex = 13;
            this.nTickInterval.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // lTickIntervalUnits
            // 
            this.lTickIntervalUnits.AutoSize = true;
            this.lTickIntervalUnits.Location = new System.Drawing.Point(673, 116);
            this.lTickIntervalUnits.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lTickIntervalUnits.Name = "lTickIntervalUnits";
            this.lTickIntervalUnits.Size = new System.Drawing.Size(29, 18);
            this.lTickIntervalUnits.TabIndex = 14;
            this.lTickIntervalUnits.Text = "ms";
            // 
            // lTickInterval
            // 
            this.lTickInterval.AutoSize = true;
            this.lTickInterval.Location = new System.Drawing.Point(469, 116);
            this.lTickInterval.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lTickInterval.Name = "lTickInterval";
            this.lTickInterval.Size = new System.Drawing.Size(86, 18);
            this.lTickInterval.TabIndex = 12;
            this.lTickInterval.Text = "Tick interval";
            // 
            // nThrottling
            // 
            this.nThrottling.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nThrottling.Location = new System.Drawing.Point(153, 148);
            this.nThrottling.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nThrottling.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nThrottling.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nThrottling.Name = "nThrottling";
            this.nThrottling.Size = new System.Drawing.Size(93, 24);
            this.nThrottling.TabIndex = 16;
            this.nThrottling.Value = new decimal(new int[] {
            2048,
            0,
            0,
            0});
            // 
            // lThrottling
            // 
            this.lThrottling.AutoSize = true;
            this.lThrottling.Location = new System.Drawing.Point(29, 150);
            this.lThrottling.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lThrottling.Name = "lThrottling";
            this.lThrottling.Size = new System.Drawing.Size(106, 18);
            this.lThrottling.TabIndex = 15;
            this.lThrottling.Text = "Block throttling";
            // 
            // lThrottlingUnits
            // 
            this.lThrottlingUnits.AutoSize = true;
            this.lThrottlingUnits.Location = new System.Drawing.Point(255, 150);
            this.lThrottlingUnits.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lThrottlingUnits.Name = "lThrottlingUnits";
            this.lThrottlingUnits.Size = new System.Drawing.Size(159, 18);
            this.lThrottlingUnits.TabIndex = 17;
            this.lThrottlingUnits.Text = "blocks / second / client";
            // 
            // gAdvancedMisc
            // 
            this.gAdvancedMisc.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gAdvancedMisc.Controls.Add(this.nMaxUndoStates);
            this.gAdvancedMisc.Controls.Add(this.lMaxUndoStates);
            this.gAdvancedMisc.Controls.Add(this.xHeartbeatToWoMDirect);
            this.gAdvancedMisc.Controls.Add(this.lIPWarning);
            this.gAdvancedMisc.Controls.Add(this.tIP);
            this.gAdvancedMisc.Controls.Add(this.xIP);
            this.gAdvancedMisc.Controls.Add(this.lConsoleName);
            this.gAdvancedMisc.Controls.Add(this.tConsoleName);
            this.gAdvancedMisc.Controls.Add(this.nMaxUndo);
            this.gAdvancedMisc.Controls.Add(this.lMaxUndoUnits);
            this.gAdvancedMisc.Controls.Add(this.xMaxUndo);
            this.gAdvancedMisc.Controls.Add(this.xRelayAllBlockUpdates);
            this.gAdvancedMisc.Controls.Add(this.xNoPartialPositionUpdates);
            this.gAdvancedMisc.Location = new System.Drawing.Point(11, 145);
            this.gAdvancedMisc.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gAdvancedMisc.Name = "gAdvancedMisc";
            this.gAdvancedMisc.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gAdvancedMisc.Size = new System.Drawing.Size(848, 251);
            this.gAdvancedMisc.TabIndex = 1;
            this.gAdvancedMisc.TabStop = false;
            this.gAdvancedMisc.Text = "Miscellaneous";
            // 
            // nMaxUndoStates
            // 
            this.nMaxUndoStates.Location = new System.Drawing.Point(153, 87);
            this.nMaxUndoStates.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nMaxUndoStates.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nMaxUndoStates.Name = "nMaxUndoStates";
            this.nMaxUndoStates.Size = new System.Drawing.Size(77, 24);
            this.nMaxUndoStates.TabIndex = 23;
            this.nMaxUndoStates.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nMaxUndoStates.ValueChanged += new System.EventHandler(this.nMaxUndo_ValueChanged);
            // 
            // lMaxUndoStates
            // 
            this.lMaxUndoStates.AutoSize = true;
            this.lMaxUndoStates.Location = new System.Drawing.Point(239, 90);
            this.lMaxUndoStates.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lMaxUndoStates.Name = "lMaxUndoStates";
            this.lMaxUndoStates.Size = new System.Drawing.Size(89, 18);
            this.lMaxUndoStates.TabIndex = 22;
            this.lMaxUndoStates.Text = "states, up to";
            // 
            // xHeartbeatToWoMDirect
            // 
            this.xHeartbeatToWoMDirect.AutoSize = true;
            this.xHeartbeatToWoMDirect.Location = new System.Drawing.Point(8, 217);
            this.xHeartbeatToWoMDirect.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xHeartbeatToWoMDirect.Name = "xHeartbeatToWoMDirect";
            this.xHeartbeatToWoMDirect.Size = new System.Drawing.Size(573, 27);
            this.xHeartbeatToWoMDirect.TabIndex = 21;
            this.xHeartbeatToWoMDirect.Text = "Send heartbeats to WoM Direct (direct.worldofminecraft.net).";
            this.xHeartbeatToWoMDirect.UseVisualStyleBackColor = true;
            // 
            // lIPWarning
            // 
            this.lIPWarning.AutoSize = true;
            this.lIPWarning.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lIPWarning.Location = new System.Drawing.Point(149, 186);
            this.lIPWarning.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lIPWarning.Name = "lIPWarning";
            this.lIPWarning.Size = new System.Drawing.Size(542, 17);
            this.lIPWarning.TabIndex = 20;
            this.lIPWarning.Text = "Note: You do not need to specify an IP address unless you have multiple NICs or I" +
    "Ps.";
            // 
            // tIP
            // 
            this.tIP.Location = new System.Drawing.Point(153, 156);
            this.tIP.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tIP.MaxLength = 15;
            this.tIP.Name = "tIP";
            this.tIP.Size = new System.Drawing.Size(128, 24);
            this.tIP.TabIndex = 19;
            this.tIP.Validating += new System.ComponentModel.CancelEventHandler(this.tIP_Validating);
            // 
            // xIP
            // 
            this.xIP.AutoSize = true;
            this.xIP.Location = new System.Drawing.Point(8, 159);
            this.xIP.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xIP.Name = "xIP";
            this.xIP.Size = new System.Drawing.Size(161, 27);
            this.xIP.TabIndex = 18;
            this.xIP.Text = "Designated IP";
            this.xIP.UseVisualStyleBackColor = true;
            this.xIP.CheckedChanged += new System.EventHandler(this.xIP_CheckedChanged);
            // 
            // lConsoleName
            // 
            this.lConsoleName.AutoSize = true;
            this.lConsoleName.Location = new System.Drawing.Point(29, 127);
            this.lConsoleName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lConsoleName.Name = "lConsoleName";
            this.lConsoleName.Size = new System.Drawing.Size(105, 18);
            this.lConsoleName.TabIndex = 7;
            this.lConsoleName.Text = "Console name";
            // 
            // tConsoleName
            // 
            this.tConsoleName.Location = new System.Drawing.Point(153, 123);
            this.tConsoleName.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tConsoleName.Name = "tConsoleName";
            this.tConsoleName.Size = new System.Drawing.Size(221, 24);
            this.tConsoleName.TabIndex = 8;
            // 
            // nMaxUndo
            // 
            this.nMaxUndo.Increment = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nMaxUndo.Location = new System.Drawing.Point(343, 87);
            this.nMaxUndo.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nMaxUndo.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.nMaxUndo.Name = "nMaxUndo";
            this.nMaxUndo.Size = new System.Drawing.Size(115, 24);
            this.nMaxUndo.TabIndex = 5;
            this.nMaxUndo.Value = new decimal(new int[] {
            2000000,
            0,
            0,
            0});
            this.nMaxUndo.ValueChanged += new System.EventHandler(this.nMaxUndo_ValueChanged);
            // 
            // lMaxUndoUnits
            // 
            this.lMaxUndoUnits.AutoSize = true;
            this.lMaxUndoUnits.Location = new System.Drawing.Point(465, 90);
            this.lMaxUndoUnits.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lMaxUndoUnits.Name = "lMaxUndoUnits";
            this.lMaxUndoUnits.Size = new System.Drawing.Size(316, 18);
            this.lMaxUndoUnits.TabIndex = 6;
            this.lMaxUndoUnits.Text = "blocks each (up to 16.0 MB of RAM per player)";
            // 
            // xMaxUndo
            // 
            this.xMaxUndo.AutoSize = true;
            this.xMaxUndo.Checked = true;
            this.xMaxUndo.CheckState = System.Windows.Forms.CheckState.Checked;
            this.xMaxUndo.Location = new System.Drawing.Point(8, 89);
            this.xMaxUndo.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xMaxUndo.Name = "xMaxUndo";
            this.xMaxUndo.Size = new System.Drawing.Size(159, 27);
            this.xMaxUndo.TabIndex = 4;
            this.xMaxUndo.Text = "Limit /undo to";
            this.xMaxUndo.UseVisualStyleBackColor = true;
            this.xMaxUndo.CheckedChanged += new System.EventHandler(this.xMaxUndo_CheckedChanged);
            // 
            // xRelayAllBlockUpdates
            // 
            this.xRelayAllBlockUpdates.AutoSize = true;
            this.xRelayAllBlockUpdates.Location = new System.Drawing.Point(8, 26);
            this.xRelayAllBlockUpdates.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xRelayAllBlockUpdates.Name = "xRelayAllBlockUpdates";
            this.xRelayAllBlockUpdates.Size = new System.Drawing.Size(895, 27);
            this.xRelayAllBlockUpdates.TabIndex = 1;
            this.xRelayAllBlockUpdates.Text = "When a player changes a block, send him the redundant update packet anyway (origi" +
    "nal behavior).";
            this.xRelayAllBlockUpdates.UseVisualStyleBackColor = true;
            // 
            // xNoPartialPositionUpdates
            // 
            this.xNoPartialPositionUpdates.AutoSize = true;
            this.xNoPartialPositionUpdates.Location = new System.Drawing.Point(8, 57);
            this.xNoPartialPositionUpdates.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xNoPartialPositionUpdates.Name = "xNoPartialPositionUpdates";
            this.xNoPartialPositionUpdates.Size = new System.Drawing.Size(525, 27);
            this.xNoPartialPositionUpdates.TabIndex = 2;
            this.xNoPartialPositionUpdates.Text = "Do not use partial position updates (opcodes 9, 10, 11).";
            this.xNoPartialPositionUpdates.UseVisualStyleBackColor = true;
            // 
            // gCrashReport
            // 
            this.gCrashReport.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gCrashReport.Controls.Add(this.lCrashReportDisclaimer);
            this.gCrashReport.Controls.Add(this.xSubmitCrashReports);
            this.gCrashReport.Location = new System.Drawing.Point(11, 16);
            this.gCrashReport.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gCrashReport.Name = "gCrashReport";
            this.gCrashReport.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gCrashReport.Size = new System.Drawing.Size(848, 122);
            this.gCrashReport.TabIndex = 0;
            this.gCrashReport.TabStop = false;
            this.gCrashReport.Text = "Crash Reporting";
            // 
            // lCrashReportDisclaimer
            // 
            this.lCrashReportDisclaimer.AutoSize = true;
            this.lCrashReportDisclaimer.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lCrashReportDisclaimer.Location = new System.Drawing.Point(56, 52);
            this.lCrashReportDisclaimer.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lCrashReportDisclaimer.Name = "lCrashReportDisclaimer";
            this.lCrashReportDisclaimer.Size = new System.Drawing.Size(699, 51);
            this.lCrashReportDisclaimer.TabIndex = 1;
            this.lCrashReportDisclaimer.Text = resources.GetString("lCrashReportDisclaimer.Text");
            // 
            // xSubmitCrashReports
            // 
            this.xSubmitCrashReports.AutoSize = true;
            this.xSubmitCrashReports.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xSubmitCrashReports.Location = new System.Drawing.Point(8, 25);
            this.xSubmitCrashReports.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.xSubmitCrashReports.Name = "xSubmitCrashReports";
            this.xSubmitCrashReports.Size = new System.Drawing.Size(735, 27);
            this.xSubmitCrashReports.TabIndex = 0;
            this.xSubmitCrashReports.Text = "Automatically submit crash reports to 800craft developers (fCraft.net)";
            this.xSubmitCrashReports.UseVisualStyleBackColor = true;
            // 
            // bOK
            // 
            this.bOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bOK.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bOK.Location = new System.Drawing.Point(480, 650);
            this.bOK.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bOK.Name = "bOK";
            this.bOK.Size = new System.Drawing.Size(133, 34);
            this.bOK.TabIndex = 1;
            this.bOK.Text = "OK";
            this.bOK.Click += new System.EventHandler(this.bSave_Click);
            // 
            // bCancel
            // 
            this.bCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bCancel.Location = new System.Drawing.Point(621, 650);
            this.bCancel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size(133, 34);
            this.bCancel.TabIndex = 2;
            this.bCancel.Text = "Cancel";
            this.bCancel.Click += new System.EventHandler(this.bCancel_Click);
            // 
            // bResetTab
            // 
            this.bResetTab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bResetTab.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bResetTab.Location = new System.Drawing.Point(176, 650);
            this.bResetTab.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bResetTab.Name = "bResetTab";
            this.bResetTab.Size = new System.Drawing.Size(133, 34);
            this.bResetTab.TabIndex = 5;
            this.bResetTab.Text = "Reset Tab";
            this.bResetTab.UseVisualStyleBackColor = true;
            this.bResetTab.Click += new System.EventHandler(this.bResetTab_Click);
            // 
            // bResetAll
            // 
            this.bResetAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bResetAll.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bResetAll.Location = new System.Drawing.Point(16, 650);
            this.bResetAll.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bResetAll.Name = "bResetAll";
            this.bResetAll.Size = new System.Drawing.Size(152, 34);
            this.bResetAll.TabIndex = 4;
            this.bResetAll.Text = "Reset All Defaults";
            this.bResetAll.UseVisualStyleBackColor = true;
            this.bResetAll.Click += new System.EventHandler(this.bResetAll_Click);
            // 
            // bApply
            // 
            this.bApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bApply.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bApply.Location = new System.Drawing.Point(763, 650);
            this.bApply.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bApply.Name = "bApply";
            this.bApply.Size = new System.Drawing.Size(133, 34);
            this.bApply.TabIndex = 3;
            this.bApply.Text = "Apply";
            this.bApply.Click += new System.EventHandler(this.bApply_Click);
            // 
            // toolTip
            // 
            this.toolTip.AutoPopDelay = 11111;
            this.toolTip.InitialDelay = 500;
            this.toolTip.IsBalloon = true;
            this.toolTip.ReshowDelay = 100;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(912, 699);
            this.Controls.Add(this.bApply);
            this.Controls.Add(this.bResetAll);
            this.Controls.Add(this.bResetTab);
            this.Controls.Add(this.bCancel);
            this.Controls.Add(this.bOK);
            this.Controls.Add(this.tabs);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MinimumSize = new System.Drawing.Size(927, 663);
            this.Name = "MainForm";
            this.Text = "800craft Config Tool";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ConfigUI_FormClosing);
            this.tabs.ResumeLayout(false);
            this.tabGeneral.ResumeLayout(false);
            this.gUpdaterSettings.ResumeLayout(false);
            this.gUpdaterSettings.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.gHelpAndSupport.ResumeLayout(false);
            this.gInformation.ResumeLayout(false);
            this.gInformation.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nAnnouncements)).EndInit();
            this.gBasic.ResumeLayout(false);
            this.gBasic.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxPlayersPerWorld)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nUploadBandwidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxPlayers)).EndInit();
            this.tabChat.ResumeLayout(false);
            this.gChatColors.ResumeLayout(false);
            this.gChatColors.PerformLayout();
            this.gAppearence.ResumeLayout(false);
            this.gAppearence.PerformLayout();
            this.tabWorlds.ResumeLayout(false);
            this.tabWorlds.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvWorlds)).EndInit();
            this.tabRanks.ResumeLayout(false);
            this.tabRanks.PerformLayout();
            this.gPermissionLimits.ResumeLayout(false);
            this.gRankOptions.ResumeLayout(false);
            this.gRankOptions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nFillLimit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nCopyPasteSlots)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nAntiGriefSeconds)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nDrawLimit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nKickIdle)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nAntiGriefBlocks)).EndInit();
            this.tabSecurity.ResumeLayout(false);
            this.gBlockDB.ResumeLayout(false);
            this.gBlockDB.PerformLayout();
            this.gSecurityMisc.ResumeLayout(false);
            this.gSecurityMisc.PerformLayout();
            this.gSpamChat.ResumeLayout(false);
            this.gSpamChat.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nAntispamMaxWarnings)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamMute)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nAntispamInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nAntispamMessageCount)).EndInit();
            this.gVerify.ResumeLayout(false);
            this.gVerify.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxConnectionsPerIP)).EndInit();
            this.tabSavingAndBackup.ResumeLayout(false);
            this.gDataBackup.ResumeLayout(false);
            this.gDataBackup.PerformLayout();
            this.gSaving.ResumeLayout(false);
            this.gSaving.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nSaveInterval)).EndInit();
            this.gBackups.ResumeLayout(false);
            this.gBackups.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxBackupSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxBackups)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nBackupInterval)).EndInit();
            this.tabLogging.ResumeLayout(false);
            this.gLogFile.ResumeLayout(false);
            this.gLogFile.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nLogLimit)).EndInit();
            this.gConsole.ResumeLayout(false);
            this.gConsole.PerformLayout();
            this.tabIRC.ResumeLayout(false);
            this.tabIRC.PerformLayout();
            this.gIRCOptions.ResumeLayout(false);
            this.gIRCOptions.PerformLayout();
            this.gIRCNetwork.ResumeLayout(false);
            this.gIRCNetwork.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nIRCDelay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nIRCBotPort)).EndInit();
            this.tabAdvanced.ResumeLayout(false);
            this.gPerformance.ResumeLayout(false);
            this.gPerformance.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nTickInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nThrottling)).EndInit();
            this.gAdvancedMisc.ResumeLayout(false);
            this.gAdvancedMisc.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxUndoStates)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxUndo)).EndInit();
            this.gCrashReport.ResumeLayout(false);
            this.gCrashReport.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabs;
        private System.Windows.Forms.Button bOK;
        private System.Windows.Forms.Button bCancel;
        private System.Windows.Forms.Button bResetTab;
        private System.Windows.Forms.TabPage tabGeneral;
        private System.Windows.Forms.TabPage tabRanks;
        private System.Windows.Forms.Label lServerName;
        private System.Windows.Forms.TextBox tServerName;
        private System.Windows.Forms.Label lMOTD;
        private System.Windows.Forms.TextBox tMOTD;
        private System.Windows.Forms.Label lMaxPlayers;
        private System.Windows.Forms.NumericUpDown nMaxPlayers;
        private System.Windows.Forms.TabPage tabSavingAndBackup;
        private System.Windows.Forms.ComboBox cPublic;
        private System.Windows.Forms.Label lPublic;
        private System.Windows.Forms.Button bMeasure;
        private System.Windows.Forms.Label lUploadBandwidthUnits;
        private System.Windows.Forms.NumericUpDown nUploadBandwidth;
        private System.Windows.Forms.Label lUploadBandwidth;
        private System.Windows.Forms.TabPage tabLogging;
        private System.Windows.Forms.TabPage tabAdvanced;
        private System.Windows.Forms.Label lTickIntervalUnits;
        private System.Windows.Forms.NumericUpDown nTickInterval;
        private System.Windows.Forms.Label lTickInterval;
        private System.Windows.Forms.Label lAdvancedWarning;
        private System.Windows.Forms.ListBox vRanks;
        private System.Windows.Forms.Button bAddRank;
        private System.Windows.Forms.Label lPermissions;
        private System.Windows.Forms.ListView vPermissions;
        private System.Windows.Forms.ColumnHeader chPermissions;
        private System.Windows.Forms.GroupBox gRankOptions;
        private System.Windows.Forms.Button bDeleteRank;
        private System.Windows.Forms.Label lRankColor;
        private System.Windows.Forms.TextBox tRankName;
        private System.Windows.Forms.Label lRankName;
        private System.Windows.Forms.TextBox tPrefix;
        private System.Windows.Forms.Label lPrefix;
        private System.Windows.Forms.Label lAntiGrief2;
        private System.Windows.Forms.NumericUpDown nAntiGriefBlocks;
        private System.Windows.Forms.CheckBox xDrawLimit;
        private System.Windows.Forms.Label lDrawLimitUnits;
        private System.Windows.Forms.GroupBox gBasic;
        private System.Windows.Forms.ComboBox cDefaultRank;
        private System.Windows.Forms.Label lDefaultRank;
        private System.Windows.Forms.GroupBox gSaving;
        private System.Windows.Forms.NumericUpDown nSaveInterval;
        private System.Windows.Forms.Label lSaveIntervalUnits;
        private System.Windows.Forms.CheckBox xSaveInterval;
        private System.Windows.Forms.GroupBox gBackups;
        private System.Windows.Forms.CheckBox xBackupOnStartup;
        private System.Windows.Forms.NumericUpDown nBackupInterval;
        private System.Windows.Forms.Label lBackupIntervalUnits;
        private System.Windows.Forms.CheckBox xBackupInterval;
        private System.Windows.Forms.CheckBox xBackupOnJoin;
        private System.Windows.Forms.CheckBox xRelayAllBlockUpdates;
        private System.Windows.Forms.ComboBox cProcessPriority;
        private System.Windows.Forms.Label lProcessPriority;
        private System.Windows.Forms.Button bResetAll;
        private System.Windows.Forms.GroupBox gLogFile;
        private System.Windows.Forms.ComboBox cLogMode;
        private System.Windows.Forms.Label lLogMode;
        private System.Windows.Forms.GroupBox gConsole;
        private System.Windows.Forms.ListView vLogFileOptions;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.Label lLogLimitUnits;
        private System.Windows.Forms.NumericUpDown nLogLimit;
        private System.Windows.Forms.ListView vConsoleOptions;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.CheckBox xLogLimit;
        private System.Windows.Forms.CheckBox xReserveSlot;
        private System.Windows.Forms.NumericUpDown nDrawLimit;
        private System.Windows.Forms.Label lKickIdleUnits;
        private System.Windows.Forms.NumericUpDown nKickIdle;
        private System.Windows.Forms.CheckBox xKickIdle;
        private System.Windows.Forms.CheckBox xNoPartialPositionUpdates;
        private System.Windows.Forms.Label lMaxBackups;
        private System.Windows.Forms.NumericUpDown nMaxBackups;
        private System.Windows.Forms.Label lMaxBackupSize;
        private System.Windows.Forms.NumericUpDown nMaxBackupSize;
        private System.Windows.Forms.CheckBox xMaxBackupSize;
        private System.Windows.Forms.CheckBox xMaxBackups;
        private System.Windows.Forms.Label lThrottlingUnits;
        private System.Windows.Forms.NumericUpDown nThrottling;
        private System.Windows.Forms.Label lThrottling;
        private System.Windows.Forms.Button bApply;
        private System.Windows.Forms.Button bColorRank;
        private System.Windows.Forms.TabPage tabSecurity;
        private System.Windows.Forms.GroupBox gVerify;
        private System.Windows.Forms.Label lVerifyNames;
        private System.Windows.Forms.ComboBox cVerifyNames;
        private System.Windows.Forms.GroupBox gSpamChat;
        private System.Windows.Forms.Label lAntispamInterval;
        private System.Windows.Forms.NumericUpDown nAntispamInterval;
        private System.Windows.Forms.Label lAntispamMessageCount;
        private System.Windows.Forms.NumericUpDown nAntispamMessageCount;
        private System.Windows.Forms.Label lSpamChat;
        private System.Windows.Forms.CheckBox xLowLatencyMode;
        private System.Windows.Forms.CheckBox xAntispamKicks;
        private System.Windows.Forms.Label lSpamMuteSeconds;
        private System.Windows.Forms.NumericUpDown nSpamMute;
        private System.Windows.Forms.Label lSpamMute;
        private System.Windows.Forms.Label lAntispamMaxWarnings;
        private System.Windows.Forms.NumericUpDown nAntispamMaxWarnings;
        private System.Windows.Forms.CheckBox xBackupOnlyWhenChanged;
        private System.Windows.Forms.Label lPort;
        private System.Windows.Forms.NumericUpDown nPort;
        private System.Windows.Forms.Button bRules;
        private System.Windows.Forms.TabPage tabIRC;
        private System.Windows.Forms.GroupBox gIRCNetwork;
        private System.Windows.Forms.CheckBox xIRCBotEnabled;
        private System.Windows.Forms.CheckBox xIRCBotAnnounceServerJoins;
        private System.Windows.Forms.Label lIRCBotChannels;
        private System.Windows.Forms.NumericUpDown nIRCBotPort;
        private System.Windows.Forms.Label lIRCBotPort;
        private System.Windows.Forms.TextBox tIRCBotNetwork;
        private System.Windows.Forms.Label lIRCBotNetwork;
        private System.Windows.Forms.Label lIRCBotNick;
        private System.Windows.Forms.TextBox tIRCBotNick;
        private System.Windows.Forms.CheckBox xIRCBotForwardFromServer;
        private System.Windows.Forms.GroupBox gIRCOptions;
        private System.Windows.Forms.TextBox tIRCBotChannels;
        private System.Windows.Forms.Label lIRCBotChannels3;
        private System.Windows.Forms.Label lIRCBotChannels2;
        private System.Windows.Forms.CheckBox xIRCBotForwardFromIRC;
        private System.Windows.Forms.CheckBox xMaxConnectionsPerIP;
        private System.Windows.Forms.TabPage tabWorlds;
        private System.Windows.Forms.DataGridView dgvWorlds;
        private System.Windows.Forms.Button bWorldDelete;
        private System.Windows.Forms.Button bAddWorld;
        private System.Windows.Forms.Button bWorldEdit;
        private System.Windows.Forms.ComboBox cMainWorld;
        private System.Windows.Forms.Label lMainWorld;
        private System.Windows.Forms.GroupBox gInformation;
        private System.Windows.Forms.CheckBox xAnnouncements;
        private System.Windows.Forms.Button bAnnouncements;
        private System.Windows.Forms.Label lAnnouncementsUnits;
        private System.Windows.Forms.NumericUpDown nAnnouncements;
        private System.Windows.Forms.Label lAntiGrief3;
        private System.Windows.Forms.NumericUpDown nAntiGriefSeconds;
        private System.Windows.Forms.CheckBox xAntiGrief;
        private System.Windows.Forms.Label lAntiGrief1;
        private System.Windows.Forms.GroupBox gSecurityMisc;
        private System.Windows.Forms.CheckBox xAnnounceKickAndBanReasons;
        private System.Windows.Forms.CheckBox xRequireRankChangeReason;
        private System.Windows.Forms.CheckBox xRequireBanReason;
        private System.Windows.Forms.CheckBox xAnnounceRankChanges;
        private System.Windows.Forms.Button bPortCheck;
        private System.Windows.Forms.Button bColorIRC;
        private System.Windows.Forms.Label lColorIRC;
        private System.Windows.Forms.CheckBox xIRCBotAnnounceIRCJoins;
        private System.Windows.Forms.CheckBox xSubmitCrashReports;
        private System.Windows.Forms.GroupBox gCrashReport;
        private System.Windows.Forms.Label lCrashReportDisclaimer;
        private System.Windows.Forms.GroupBox gAdvancedMisc;
        private System.Windows.Forms.Label lIRCDelay;
        private System.Windows.Forms.NumericUpDown nIRCDelay;
        private System.Windows.Forms.ComboBox cPatrolledRank;
        private System.Windows.Forms.Label lPatrolledRank;
        private System.Windows.Forms.Label lPatrolledRankAndBelow;
        private System.Windows.Forms.Button bLowerRank;
        private System.Windows.Forms.Button bRaiseRank;
        private System.Windows.Forms.Label lRankList;
        private System.Windows.Forms.CheckBox xIRCRegisteredNick;
        private System.Windows.Forms.TextBox tIRCNickServMessage;
        private System.Windows.Forms.Label lIRCNickServMessage;
        private System.Windows.Forms.TextBox tIRCNickServ;
        private System.Windows.Forms.Label lIRCNickServ;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Label lIRCNoForwardingMessage;
        private System.Windows.Forms.Label lIRCDelayUnits;
        private System.Windows.Forms.NumericUpDown nMaxUndo;
        private System.Windows.Forms.Label lMaxUndoUnits;
        private System.Windows.Forms.CheckBox xMaxUndo;
        private System.Windows.Forms.Label lDefaultBuildRank;
        private System.Windows.Forms.ComboBox cDefaultBuildRank;
        private System.Windows.Forms.Button bGreeting;
        private System.Windows.Forms.ComboBox cIRCList;
        private System.Windows.Forms.Label lIRCList;
        private System.Windows.Forms.CheckBox xIRCListShowNonEnglish;
        private System.Windows.Forms.CheckBox xAllowUnverifiedLAN;
        private System.Windows.Forms.TabPage tabChat;
        private System.Windows.Forms.GroupBox gAppearence;
        private System.Windows.Forms.CheckBox xShowJoinedWorldMessages;
        private System.Windows.Forms.CheckBox xRankColorsInWorldNames;
        private System.Windows.Forms.Button bColorPM;
        private System.Windows.Forms.Label lColorPM;
        private System.Windows.Forms.Button bColorAnnouncement;
        private System.Windows.Forms.Label lColorAnnouncement;
        private System.Windows.Forms.Button bColorSay;
        private System.Windows.Forms.Button bColorHelp;
        private System.Windows.Forms.Button bColorSys;
        private System.Windows.Forms.CheckBox xRankPrefixesInList;
        private System.Windows.Forms.CheckBox xRankPrefixesInChat;
        private System.Windows.Forms.CheckBox xRankColorsInChat;
        private System.Windows.Forms.Label lColorSay;
        private System.Windows.Forms.Label lColorHelp;
        private System.Windows.Forms.Label lColorSys;
        private ChatPreview chatPreview;
        private System.Windows.Forms.GroupBox gChatColors;
        private System.Windows.Forms.Label lColorWarning;
        private System.Windows.Forms.Button bColorWarning;
        private System.Windows.Forms.Label lColorMe;
        private System.Windows.Forms.Button bColorMe;
        private System.Windows.Forms.Label lLogFileOptionsDescription;
        private System.Windows.Forms.Label lLogConsoleOptionsDescription;
        private System.Windows.Forms.CheckBox xIRCBotAnnounceServerEvents;
        private System.Windows.Forms.CheckBox xIRCUseColor;
        private System.Windows.Forms.CheckBox xPaidPlayersOnly;
        private System.Windows.Forms.Button bMapPath;
        private System.Windows.Forms.CheckBox xMapPath;
        private System.Windows.Forms.TextBox tMapPath;
        private System.Windows.Forms.CheckBox xAllowSecurityCircumvention;
        private System.Windows.Forms.Label lConsoleName;
        private System.Windows.Forms.TextBox tConsoleName;
        private System.Windows.Forms.CheckBox xShowConnectionMessages;
        private System.Windows.Forms.GroupBox gHelpAndSupport;
        private System.Windows.Forms.Button bReadme;
        private System.Windows.Forms.Button bReportABug;
        private System.Windows.Forms.Button bCredits;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button bOpenWiki;
        private System.Windows.Forms.ComboBox cUpdaterMode;
        private System.Windows.Forms.Label lUpdater;
        private System.Windows.Forms.GroupBox gUpdaterSettings;
        private System.Windows.Forms.Button bShowAdvancedUpdaterSettings;
        private System.Windows.Forms.Label lIPWarning;
        private System.Windows.Forms.TextBox tIP;
        private System.Windows.Forms.CheckBox xIP;
        private System.Windows.Forms.NumericUpDown nMaxConnectionsPerIP;
        private System.Windows.Forms.NumericUpDown nMaxPlayersPerWorld;
        private System.Windows.Forms.Label lMaxPlayersPerWorld;
        private System.Windows.Forms.CheckBox xAnnounceRankChangeReasons;
        private System.Windows.Forms.CheckBox xRequireKickReason;
        private System.Windows.Forms.GroupBox gPermissionLimits;
        private System.Windows.Forms.FlowLayoutPanel permissionLimitBoxContainer;
        private System.Windows.Forms.GroupBox gDataBackup;
        private System.Windows.Forms.CheckBox xBackupDataOnStartup;
        private System.Windows.Forms.GroupBox gBlockDB;
        private System.Windows.Forms.ComboBox cBlockDBAutoEnableRank;
        private System.Windows.Forms.CheckBox xBlockDBAutoEnable;
        private System.Windows.Forms.CheckBox xBlockDBEnabled;
        private System.Windows.Forms.CheckBox xWoMEnableEnvExtensions;
        private System.Windows.Forms.CheckBox xHeartbeatToWoMDirect;
        private System.Windows.Forms.NumericUpDown nCopyPasteSlots;
        private System.Windows.Forms.Label lCopyPasteSlots;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvcName;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvcDescription;
        private System.Windows.Forms.DataGridViewComboBoxColumn dgvcAccess;
        private System.Windows.Forms.DataGridViewComboBoxColumn dgvcBuild;
        private System.Windows.Forms.DataGridViewComboBoxColumn dgvcBackup;
        private System.Windows.Forms.DataGridViewCheckBoxColumn dgvcHidden;
        private System.Windows.Forms.DataGridViewCheckBoxColumn dgvcBlockDB;
        private System.Windows.Forms.Label lFillLimitUnits;
        private System.Windows.Forms.NumericUpDown nFillLimit;
        private System.Windows.Forms.Label lFillLimit;
        private System.Windows.Forms.NumericUpDown nMaxUndoStates;
        private System.Windows.Forms.Label lMaxUndoStates;
        private System.Windows.Forms.GroupBox gPerformance;
        private System.Windows.Forms.Button bChangelog;
    }
}