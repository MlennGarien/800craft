// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;
using JetBrains.Annotations;


namespace fCraft.ConfigGUI {
    // This section handles transfer of settings from Config to the specific UI controls, and vice versa.
    // Effectively, it's an adapter between Config's and ConfigUI's representations of the settings
    partial class MainForm {
        #region Loading & Applying Config

        void LoadConfig() {
            string missingFileMsg = null;
            if( !File.Exists( Paths.WorldListFileName ) && !File.Exists( Paths.ConfigFileName ) ) {
                missingFileMsg = String.Format( "Configuration ({0}) and world list ({1}) were not found. Using defaults.",
                                                Paths.ConfigFileName,
                                                Paths.WorldListFileName );
            } else if( !File.Exists( Paths.ConfigFileName ) ) {
                missingFileMsg = String.Format( "Configuration ({0}) was not found. Using defaults.",
                                                 Paths.ConfigFileName );
            } else if( !File.Exists( Paths.WorldListFileName ) ) {
                missingFileMsg = String.Format( "World list ({0}) was not found. Assuming 0 worlds.",
                                                Paths.WorldListFileName );
            }
            if( missingFileMsg != null ) {
                MessageBox.Show( missingFileMsg );
            }

            using( LogRecorder loadLogger = new LogRecorder() ) {
                if( Config.Load( false, false ) ) {
                    if( loadLogger.HasMessages ) {
                        MessageBox.Show( loadLogger.MessageString, "Config loading warnings" );
                    }
                } else {
                    MessageBox.Show( loadLogger.MessageString, "Error occured while trying to load config" );
                }
            }

            ApplyTabGeneral();
            ApplyTabChat();
            ApplyTabWorlds(); // also reloads world list
            ApplyTabRanks();
            ApplyTabSecurity();
            ApplyTabSavingAndBackup();
            ApplyTabLogging();
            ApplyTabIRC();
            ApplyTabAdvanced();

            AddChangeHandler( tabs, SomethingChanged );
            AddChangeHandler( bResetTab, SomethingChanged );
            AddChangeHandler( bResetAll, SomethingChanged );
            dgvWorlds.CellValueChanged += delegate {
                SomethingChanged( null, null );
            };

            AddChangeHandler( tabChat, HandleTabChatChange );
            bApply.Enabled = false;
        }


        void LoadWorldList() {
            if( Worlds.Count > 0 ) Worlds.Clear();
            if( !File.Exists( Paths.WorldListFileName ) ) return;

            try {
                XDocument doc = XDocument.Load( Paths.WorldListFileName );
                XElement root = doc.Root;
                if( root == null ) {
                    MessageBox.Show( "Worlds.xml is empty or corrupted." );
                    return;
                }

                string errorLog = "";
                using( LogRecorder logRecorder = new LogRecorder() ) {
                    foreach( XElement el in root.Elements( "World" ) ) {
                        try {
                            Worlds.Add( new WorldListEntry( el ) );
                        } catch( Exception ex ) {
                            errorLog += ex + Environment.NewLine;
                        }
                    }
                    if( logRecorder.HasMessages ) {
                        MessageBox.Show( logRecorder.MessageString, "World list loading warnings." );
                    }
                }
                if( errorLog.Length > 0 ) {
                    MessageBox.Show( "Some errors occured while loading the world list:" + Environment.NewLine + errorLog, "Warning" );
                }

                FillWorldList();
                XAttribute mainWorldAttr = root.Attribute( "main" );
                if( mainWorldAttr != null ) {
                    foreach( WorldListEntry world in Worlds ) {
                        if( world.Name.ToLower() == mainWorldAttr.Value.ToLower() ) {
                            cMainWorld.SelectedItem = world.Name;
                            break;
                        }
                    }
                }

            } catch( Exception ex ) {
                MessageBox.Show( "Error occured while loading the world list: " + Environment.NewLine + ex, "Warning" );
            }

            Worlds.ListChanged += SomethingChanged;
        }


        void ApplyTabGeneral() {
            tServerName.Text = ConfigKey.ServerName.GetString();
            tMOTD.Text = ConfigKey.MOTD.GetString();
            SwearBox.Text = ConfigKey.SwearReplace.GetString();

            nMaxPlayers.Value = ConfigKey.MaxPlayers.GetInt();
            xMineQuery.Checked = ConfigKey.MineQuery.Enabled();
            nMineQueryPort.Value = ConfigKey.MineQueryPort.GetInt();
            CheckMaxPlayersPerWorldValue();
            nMaxPlayersPerWorld.Value = ConfigKey.MaxPlayersPerWorld.GetInt();

            FillRankList( cDefaultRank, "(lowest rank)" );
            if( ConfigKey.DefaultRank.IsBlank() ) {
                cDefaultRank.SelectedIndex = 0;
            } else {
                RankManager.DefaultRank = Rank.Parse( ConfigKey.DefaultRank.GetString() );
                cDefaultRank.SelectedIndex = RankManager.GetIndex( RankManager.DefaultRank );
            }

            cPublic.SelectedIndex = ConfigKey.IsPublic.Enabled() ? 0 : 1;
            nPort.Value = ConfigKey.Port.GetInt();
            nUploadBandwidth.Value = ConfigKey.UploadBandwidth.GetInt();

            xAnnouncements.Checked = (ConfigKey.AnnouncementInterval.GetInt() > 0);
            if( xAnnouncements.Checked ) {
                nAnnouncements.Value = ConfigKey.AnnouncementInterval.GetInt();
            } else {
                nAnnouncements.Value = 1;
            }

            // UpdaterSettingsWindow
            updaterWindow.BackupBeforeUpdate = ConfigKey.BackupBeforeUpdate.Enabled();
            updaterWindow.RunBeforeUpdate = ConfigKey.RunBeforeUpdate.GetString();
            updaterWindow.RunAfterUpdate = ConfigKey.RunAfterUpdate.GetString();
            updaterWindow.UpdaterMode = ConfigKey.UpdaterMode.GetEnum<UpdaterMode>();
        }


        void ApplyTabChat() {
            xRankColorsInChat.Checked = ConfigKey.RankColorsInChat.Enabled();
            xRankPrefixesInChat.Checked = ConfigKey.RankPrefixesInChat.Enabled();
            xRankPrefixesInList.Checked = ConfigKey.RankPrefixesInList.Enabled();
            xRankColorsInWorldNames.Checked = ConfigKey.RankColorsInWorldNames.Enabled();
            xShowJoinedWorldMessages.Checked = ConfigKey.ShowJoinedWorldMessages.Enabled();
            xShowConnectionMessages.Checked = ConfigKey.ShowConnectionMessages.Enabled();
            tChatChannel.Text = ConfigKey.CustomChatChannel.GetString();
            colorSys = Color.ParseToIndex( ConfigKey.SystemMessageColor.GetString() );
            ApplyColor( bColorSys, colorSys );
            Color.Sys = Color.Parse( colorSys );
            colorCustom = Color.ParseToIndex(ConfigKey.CustomMessageColor.GetString());
            ApplyColor(bColorCustom, colorCustom);
            Color.Custom = Color.Parse(colorCustom);

            colorHelp = Color.ParseToIndex( ConfigKey.HelpColor.GetString() );
            ApplyColor( bColorHelp, colorHelp );
            Color.Help = Color.Parse( colorHelp );

            colorSay = Color.ParseToIndex( ConfigKey.SayColor.GetString() );
            ApplyColor( bColorSay, colorSay );
            Color.Say = Color.Parse( colorSay );

            colorAnnouncement = Color.ParseToIndex( ConfigKey.AnnouncementColor.GetString() );
            ApplyColor( bColorAnnouncement, colorAnnouncement );
            Color.Announcement = Color.Parse( colorAnnouncement );

            colorPM = Color.ParseToIndex( ConfigKey.PrivateMessageColor.GetString() );
            ApplyColor( bColorPM, colorPM );
            Color.PM = Color.Parse( colorPM );

            colorWarning = Color.ParseToIndex( ConfigKey.WarningColor.GetString() );
            ApplyColor( bColorWarning, colorWarning );
            Color.Warning = Color.Parse( colorWarning );

            colorMe = Color.ParseToIndex( ConfigKey.MeColor.GetString() );
            ApplyColor( bColorMe, colorMe );
            Color.Me = Color.Parse( colorMe );

            UpdateChatPreview();
        }


        void ApplyTabWorlds() {
            if( rankNameList == null ) {
                rankNameList = new BindingList<string> {
                    WorldListEntry.DefaultRankOption
                };
                foreach( Rank rank in RankManager.Ranks ) {
                    rankNameList.Add( MainForm.ToComboBoxOption(rank) );
                }
                dgvcAccess.DataSource = rankNameList;
                dgvcBuild.DataSource = rankNameList;
                dgvcBackup.DataSource = WorldListEntry.BackupEnumNames;

                LoadWorldList();
                dgvWorlds.DataSource = Worlds;

            } else {
                //dgvWorlds.DataSource = null;
                rankNameList.Clear();
                rankNameList.Add( WorldListEntry.DefaultRankOption );
                foreach( Rank rank in RankManager.Ranks ) {
                    rankNameList.Add( MainForm.ToComboBoxOption(rank) );
                }
                foreach( WorldListEntry world in Worlds ) {
                    world.ReparseRanks();
                }
                Worlds.ResetBindings();
                //dgvWorlds.DataSource = worlds;
            }

            FillRankList( cDefaultBuildRank, "(default rank)" );
            if( ConfigKey.DefaultBuildRank.IsBlank() ) {
                cDefaultBuildRank.SelectedIndex = 0;
            } else {
                RankManager.DefaultBuildRank = Rank.Parse( ConfigKey.DefaultBuildRank.GetString() );
                cDefaultBuildRank.SelectedIndex = RankManager.GetIndex( RankManager.DefaultBuildRank );
            }

            if( Paths.IsDefaultMapPath( ConfigKey.MapPath.GetString() ) ) {
                tMapPath.Text = Paths.MapPathDefault;
                xMapPath.Checked = false;
            } else {
                tMapPath.Text = ConfigKey.MapPath.GetString();
                xMapPath.Checked = true;
            }

            xWoMEnableEnvExtensions.Checked = ConfigKey.WoMEnableEnvExtensions.Enabled();
        }


        void ApplyTabRanks() {
            selectedRank = null;
            RebuildRankList();
            DisableRankOptions();
        }


        void ApplyTabSecurity() {
            ApplyEnum( cVerifyNames, ConfigKey.VerifyNames, NameVerificationMode.Balanced );

            nMaxConnectionsPerIP.Value = ConfigKey.MaxConnectionsPerIP.GetInt();
            xMaxConnectionsPerIP.Checked = (nMaxConnectionsPerIP.Value > 0);
            xAllowUnverifiedLAN.Checked = ConfigKey.AllowUnverifiedLAN.Enabled();

            nAntispamMessageCount.Value = ConfigKey.AntispamMessageCount.GetInt();
            nAntispamInterval.Value = ConfigKey.AntispamInterval.GetInt();
            nSpamMute.Value = ConfigKey.AntispamMuteDuration.GetInt();

            xAntispamKicks.Checked = (ConfigKey.AntispamMaxWarnings.GetInt() > 0);
            nAntispamMaxWarnings.Value = ConfigKey.AntispamMaxWarnings.GetInt();
            if( !xAntispamKicks.Checked ) nAntispamMaxWarnings.Enabled = false;

            xRequireKickReason.Checked = ConfigKey.RequireKickReason.Enabled();
            xRequireBanReason.Checked = ConfigKey.RequireBanReason.Enabled();
            xRequireRankChangeReason.Checked = ConfigKey.RequireRankChangeReason.Enabled();
            xAnnounceKickAndBanReasons.Checked = ConfigKey.AnnounceKickAndBanReasons.Enabled();
            xAnnounceRankChanges.Checked = ConfigKey.AnnounceRankChanges.Enabled();
            xAnnounceRankChangeReasons.Checked = ConfigKey.AnnounceRankChangeReasons.Enabled();
            xAnnounceRankChangeReasons.Enabled = xAnnounceRankChanges.Checked;

            FillRankList( cPatrolledRank, "(default rank)" );
            if( ConfigKey.PatrolledRank.IsBlank() ) {
                cPatrolledRank.SelectedIndex = 0;
            } else {
                RankManager.PatrolledRank = Rank.Parse( ConfigKey.PatrolledRank.GetString() );
                cPatrolledRank.SelectedIndex = RankManager.GetIndex( RankManager.PatrolledRank );
            }

            xPaidPlayersOnly.Checked = ConfigKey.PaidPlayersOnly.Enabled();


            xBlockDBEnabled.Checked = ConfigKey.BlockDBEnabled.Enabled();
            xBlockDBAutoEnable.Checked = ConfigKey.BlockDBAutoEnable.Enabled();

            FillRankList( cBlockDBAutoEnableRank, "(default rank)" );
            if( ConfigKey.BlockDBAutoEnableRank.IsBlank() ) {
                cBlockDBAutoEnableRank.SelectedIndex = 0;
            } else {
                RankManager.BlockDBAutoEnableRank = Rank.Parse( ConfigKey.BlockDBAutoEnableRank.GetString() );
                cBlockDBAutoEnableRank.SelectedIndex = RankManager.GetIndex( RankManager.BlockDBAutoEnableRank );
            }
        }


        void ApplyTabSavingAndBackup() {
            xSaveInterval.Checked = (ConfigKey.SaveInterval.GetInt() > 0);
            nSaveInterval.Value = ConfigKey.SaveInterval.GetInt();
            if( !xSaveInterval.Checked ) nSaveInterval.Enabled = false;

            xBackupOnStartup.Checked = ConfigKey.BackupOnStartup.Enabled();
            xBackupOnJoin.Checked = ConfigKey.BackupOnJoin.Enabled();
            xBackupOnlyWhenChanged.Checked = ConfigKey.BackupOnlyWhenChanged.Enabled();

            xBackupInterval.Checked = (ConfigKey.DefaultBackupInterval.GetInt() > 0);
            nBackupInterval.Value = ConfigKey.DefaultBackupInterval.GetInt();
            if( !xBackupInterval.Checked ) nBackupInterval.Enabled = false;

            xMaxBackups.Checked = (ConfigKey.MaxBackups.GetInt() > 0);
            nMaxBackups.Value = ConfigKey.MaxBackups.GetInt();
            if( !xMaxBackups.Checked ) nMaxBackups.Enabled = false;

            xMaxBackupSize.Checked = (ConfigKey.MaxBackupSize.GetInt() > 0);
            nMaxBackupSize.Value = ConfigKey.MaxBackupSize.GetInt();
            if( !xMaxBackupSize.Checked ) nMaxBackupSize.Enabled = false;

            xBackupDataOnStartup.Checked = ConfigKey.BackupDataOnStartup.Enabled();
        }


        void ApplyTabLogging() {
            foreach( ListViewItem item in vConsoleOptions.Items ) {
                item.Checked = Logger.ConsoleOptions[item.Index];
            }
            foreach( ListViewItem item in vLogFileOptions.Items ) {
                item.Checked = Logger.LogFileOptions[item.Index];
            }

            ApplyEnum( cLogMode, ConfigKey.LogMode, LogSplittingType.OneFile );

            xLogLimit.Checked = (ConfigKey.MaxLogs.GetInt() > 0);
            nLogLimit.Value = ConfigKey.MaxLogs.GetInt();
            if( !xLogLimit.Checked ) nLogLimit.Enabled = false;
        }


        void ApplyTabIRC() {
            xIRCBotEnabled.Checked = ConfigKey.IRCBotEnabled.Enabled();
            gIRCNetwork.Enabled = xIRCBotEnabled.Checked;
            gIRCOptions.Enabled = xIRCBotEnabled.Checked;

            tIRCBotNetwork.Text = ConfigKey.IRCBotNetwork.GetString();
            nIRCBotPort.Value = ConfigKey.IRCBotPort.GetInt();
            nIRCDelay.Value = ConfigKey.IRCDelay.GetInt();

            tIRCBotChannels.Text = ConfigKey.IRCBotChannels.GetString();

            tIRCBotNick.Text = ConfigKey.IRCBotNick.GetString();
            xIRCRegisteredNick.Checked = ConfigKey.IRCRegisteredNick.Enabled();

            tIRCNickServ.Text = ConfigKey.IRCNickServ.GetString();
            tIRCNickServMessage.Text = ConfigKey.IRCNickServMessage.GetString();

            xIRCBotAnnounceIRCJoins.Checked = ConfigKey.IRCBotAnnounceIRCJoins.Enabled();
            xIRCBotAnnounceServerJoins.Checked = ConfigKey.IRCBotAnnounceServerJoins.Enabled();
            xIRCBotForwardFromIRC.Checked = ConfigKey.IRCBotForwardFromIRC.Enabled();
            xIRCBotForwardFromServer.Checked = ConfigKey.IRCBotForwardFromServer.Enabled();


            colorIRC = Color.ParseToIndex( ConfigKey.IRCMessageColor.GetString() );
            ApplyColor( bColorIRC, colorIRC );
            Color.IRC = Color.Parse( colorIRC );

            xIRCUseColor.Checked = ConfigKey.IRCUseColor.Enabled();
            xIRCBotAnnounceServerEvents.Checked = ConfigKey.IRCBotAnnounceServerEvents.Enabled();
        }


        void ApplyTabAdvanced() {
            xRelayAllBlockUpdates.Checked = ConfigKey.RelayAllBlockUpdates.Enabled();
            xNoPartialPositionUpdates.Checked = ConfigKey.NoPartialPositionUpdates.Enabled();
            nTickInterval.Value = ConfigKey.TickInterval.GetInt();

            if( ConfigKey.ProcessPriority.IsBlank() ) {
                cProcessPriority.SelectedIndex = 0; // Default
            } else {
                switch( ConfigKey.ProcessPriority.GetEnum<ProcessPriorityClass>() ) {
                    case ProcessPriorityClass.High:
                        cProcessPriority.SelectedIndex = 1; break;
                    case ProcessPriorityClass.AboveNormal:
                        cProcessPriority.SelectedIndex = 2; break;
                    case ProcessPriorityClass.Normal:
                        cProcessPriority.SelectedIndex = 3; break;
                    case ProcessPriorityClass.BelowNormal:
                        cProcessPriority.SelectedIndex = 4; break;
                    case ProcessPriorityClass.Idle:
                        cProcessPriority.SelectedIndex = 5; break;
                }
            }

            ApplyEnum( cUpdaterMode, ConfigKey.UpdaterMode, UpdaterMode.Prompt );

            nThrottling.Value = ConfigKey.BlockUpdateThrottling.GetInt();
            xLowLatencyMode.Checked = ConfigKey.LowLatencyMode.Enabled();
            xSubmitCrashReports.Checked = ConfigKey.SubmitCrashReports.Enabled();

            if( ConfigKey.MaxUndo.GetInt() > 0 ) {
                xMaxUndo.Checked = true;
                nMaxUndo.Value = ConfigKey.MaxUndo.GetInt();
            } else {
                xMaxUndo.Checked = false;
                nMaxUndo.Value = (int)ConfigKey.MaxUndo.GetDefault();
            }
            nMaxUndoStates.Value = ConfigKey.MaxUndoStates.GetInt();

            tConsoleName.Text = ConfigKey.ConsoleName.GetString();

            tIP.Text = ConfigKey.IP.GetString();
            if( ConfigKey.IP.IsBlank() || ConfigKey.IP.IsDefault() ) {
                tIP.Enabled = false;
                xIP.Checked = false;
            } else {
                tIP.Enabled = true;
                xIP.Checked = true;
            }

            xHeartbeatToWoMDirect.Checked = ConfigKey.HeartbeatToWoMDirect.Enabled();
        }


        static void ApplyEnum<TEnum>( [NotNull] ComboBox box, ConfigKey key, TEnum def ) where TEnum : struct {
            if( box == null ) throw new ArgumentNullException( "box" );
            if( !typeof( TEnum ).IsEnum ) throw new ArgumentException( "Enum type required" );
            try {
                if( key.IsBlank() ) {
                    box.SelectedIndex = (int)(object)def;
                } else {
                    box.SelectedIndex = (int)Enum.Parse( typeof( TEnum ), key.GetString(), true );
                }
            } catch( ArgumentException ) {
                box.SelectedIndex = (int)(object)def;
            }
        }

        #endregion


        #region Saving Config

        void SaveConfig() {
            // General
            ConfigKey.ServerName.TrySetValue( tServerName.Text );
            ConfigKey.SwearReplace.TrySetValue(SwearBox.Text);
            ConfigKey.MOTD.TrySetValue( tMOTD.Text );
            ConfigKey.MaxPlayers.TrySetValue( nMaxPlayers.Value );
            ConfigKey.MaxPlayersPerWorld.TrySetValue( nMaxPlayersPerWorld.Value );
            if( cDefaultRank.SelectedIndex == 0 ) {
                ConfigKey.DefaultRank.TrySetValue( "" );
            } else {
                ConfigKey.DefaultRank.TrySetValue( RankManager.DefaultRank.FullName );
            }
            ConfigKey.IsPublic.TrySetValue( cPublic.SelectedIndex == 0 );

            // MineQuery
            ConfigKey.MineQuery.TrySetValue(xMineQuery.Checked);
            ConfigKey.MineQueryPort.TrySetValue(nMineQueryPort.Value);

            ConfigKey.Port.TrySetValue( nPort.Value );
            if( xIP.Checked ) {
                ConfigKey.IP.TrySetValue( tIP.Text );
            } else {
                ConfigKey.IP.ResetValue();
            }

            ConfigKey.UploadBandwidth.TrySetValue( nUploadBandwidth.Value );

            if( xAnnouncements.Checked ) ConfigKey.AnnouncementInterval.TrySetValue( nAnnouncements.Value );
            else ConfigKey.AnnouncementInterval.TrySetValue( 0 );

            // UpdaterSettingsWindow
            ConfigKey.UpdaterMode.TrySetValue( updaterWindow.UpdaterMode );
            ConfigKey.BackupBeforeUpdate.TrySetValue( updaterWindow.BackupBeforeUpdate );
            ConfigKey.RunBeforeUpdate.TrySetValue( updaterWindow.RunBeforeUpdate );
            ConfigKey.RunAfterUpdate.TrySetValue( updaterWindow.RunAfterUpdate );


            // Chat
            ConfigKey.SystemMessageColor.TrySetValue( Color.GetName( colorSys ) );
            ConfigKey.CustomMessageColor.TrySetValue(Color.GetName(colorCustom));
            ConfigKey.HelpColor.TrySetValue( Color.GetName( colorHelp ) );
            ConfigKey.SayColor.TrySetValue( Color.GetName( colorSay ) );
            ConfigKey.AnnouncementColor.TrySetValue( Color.GetName( colorAnnouncement ) );
            ConfigKey.PrivateMessageColor.TrySetValue( Color.GetName( colorPM ) );
            ConfigKey.WarningColor.TrySetValue( Color.GetName( colorWarning ) );
            ConfigKey.MeColor.TrySetValue( Color.GetName( colorMe ) );
            ConfigKey.ShowJoinedWorldMessages.TrySetValue( xShowJoinedWorldMessages.Checked );
            ConfigKey.RankColorsInWorldNames.TrySetValue( xRankColorsInWorldNames.Checked );
            ConfigKey.RankColorsInChat.TrySetValue( xRankColorsInChat.Checked );
            ConfigKey.RankPrefixesInChat.TrySetValue( xRankPrefixesInChat.Checked );
            ConfigKey.RankPrefixesInList.TrySetValue( xRankPrefixesInList.Checked );
            ConfigKey.CustomChatChannel.TrySetValue(tChatChannel.Text);


            // Worlds
            if( cDefaultBuildRank.SelectedIndex == 0 ) {
                ConfigKey.DefaultBuildRank.TrySetValue( "" );
            } else {
                ConfigKey.DefaultBuildRank.TrySetValue( RankManager.DefaultBuildRank.FullName );
            }

            if( xMapPath.Checked ) ConfigKey.MapPath.TrySetValue( tMapPath.Text );
            else ConfigKey.MapPath.TrySetValue( ConfigKey.MapPath.GetDefault() );

            ConfigKey.WoMEnableEnvExtensions.TrySetValue( xWoMEnableEnvExtensions.Checked );


            // Security
            WriteEnum<NameVerificationMode>( cVerifyNames, ConfigKey.VerifyNames );

            if( xMaxConnectionsPerIP.Checked ) {
                ConfigKey.MaxConnectionsPerIP.TrySetValue( nMaxConnectionsPerIP.Value );
            } else {
                ConfigKey.MaxConnectionsPerIP.TrySetValue( 0 );
            }
            ConfigKey.AllowUnverifiedLAN.TrySetValue( xAllowUnverifiedLAN.Checked );

            ConfigKey.AntispamMessageCount.TrySetValue( nAntispamMessageCount.Value );
            ConfigKey.AntispamInterval.TrySetValue( nAntispamInterval.Value );
            ConfigKey.AntispamMuteDuration.TrySetValue( nSpamMute.Value );

            if( xAntispamKicks.Checked ) ConfigKey.AntispamMaxWarnings.TrySetValue( nAntispamMaxWarnings.Value );
            else ConfigKey.AntispamMaxWarnings.TrySetValue( 0 );

            ConfigKey.RequireKickReason.TrySetValue( xRequireKickReason.Checked );
            ConfigKey.RequireBanReason.TrySetValue( xRequireBanReason.Checked );
            ConfigKey.RequireRankChangeReason.TrySetValue( xRequireRankChangeReason.Checked );
            ConfigKey.AnnounceKickAndBanReasons.TrySetValue( xAnnounceKickAndBanReasons.Checked );
            ConfigKey.AnnounceRankChanges.TrySetValue( xAnnounceRankChanges.Checked );
            ConfigKey.AnnounceRankChangeReasons.TrySetValue( xAnnounceRankChangeReasons.Checked );

            if( cPatrolledRank.SelectedIndex == 0 ) {
                ConfigKey.PatrolledRank.TrySetValue( "" );
            } else {
                ConfigKey.PatrolledRank.TrySetValue( RankManager.PatrolledRank.FullName );
            }
            ConfigKey.PaidPlayersOnly.TrySetValue( xPaidPlayersOnly.Checked );

            ConfigKey.BlockDBEnabled.TrySetValue( xBlockDBEnabled.Checked );
            ConfigKey.BlockDBAutoEnable.TrySetValue( xBlockDBAutoEnable.Checked );
            if( cBlockDBAutoEnableRank.SelectedIndex == 0 ) {
                ConfigKey.BlockDBAutoEnableRank.TrySetValue( "" );
            } else {
                ConfigKey.BlockDBAutoEnableRank.TrySetValue( RankManager.BlockDBAutoEnableRank.FullName );
            }


            // Saving & Backups
            if( xSaveInterval.Checked ) ConfigKey.SaveInterval.TrySetValue( nSaveInterval.Value );
            else ConfigKey.SaveInterval.TrySetValue( 0 );
            ConfigKey.BackupOnStartup.TrySetValue( xBackupOnStartup.Checked );
            ConfigKey.BackupOnJoin.TrySetValue( xBackupOnJoin.Checked );
            ConfigKey.BackupOnlyWhenChanged.TrySetValue( xBackupOnlyWhenChanged.Checked );

            if( xBackupInterval.Checked ) ConfigKey.DefaultBackupInterval.TrySetValue( nBackupInterval.Value );
            else ConfigKey.DefaultBackupInterval.TrySetValue( 0 );
            if( xMaxBackups.Checked ) ConfigKey.MaxBackups.TrySetValue( nMaxBackups.Value );
            else ConfigKey.MaxBackups.TrySetValue( 0 );
            if( xMaxBackupSize.Checked ) ConfigKey.MaxBackupSize.TrySetValue( nMaxBackupSize.Value );
            else ConfigKey.MaxBackupSize.TrySetValue( 0 );

            ConfigKey.BackupDataOnStartup.TrySetValue( xBackupDataOnStartup.Checked );


            // Logging
            WriteEnum<LogSplittingType>( cLogMode, ConfigKey.LogMode );
            if( xLogLimit.Checked ) ConfigKey.MaxLogs.TrySetValue( nLogLimit.Value );
            else ConfigKey.MaxLogs.TrySetValue( "0" );
            foreach( ListViewItem item in vConsoleOptions.Items ) {
                Logger.ConsoleOptions[item.Index] = item.Checked;
            }
            foreach( ListViewItem item in vLogFileOptions.Items ) {
                Logger.LogFileOptions[item.Index] = item.Checked;
            }


            // IRC
            ConfigKey.IRCBotEnabled.TrySetValue( xIRCBotEnabled.Checked );

            ConfigKey.IRCBotNetwork.TrySetValue( tIRCBotNetwork.Text );
            ConfigKey.IRCBotPort.TrySetValue( nIRCBotPort.Value );
            ConfigKey.IRCDelay.TrySetValue( nIRCDelay.Value );

            ConfigKey.IRCBotChannels.TrySetValue( tIRCBotChannels.Text );

            ConfigKey.IRCBotNick.TrySetValue( tIRCBotNick.Text );
            ConfigKey.IRCRegisteredNick.TrySetValue( xIRCRegisteredNick.Checked );
            ConfigKey.IRCNickServ.TrySetValue( tIRCNickServ.Text );
            ConfigKey.IRCNickServMessage.TrySetValue( tIRCNickServMessage.Text );

            ConfigKey.IRCBotAnnounceIRCJoins.TrySetValue( xIRCBotAnnounceIRCJoins.Checked );
            ConfigKey.IRCBotAnnounceServerJoins.TrySetValue( xIRCBotAnnounceServerJoins.Checked );
            ConfigKey.IRCBotAnnounceServerEvents.TrySetValue( xIRCBotAnnounceServerEvents.Checked );
            ConfigKey.IRCBotForwardFromIRC.TrySetValue( xIRCBotForwardFromIRC.Checked );
            ConfigKey.IRCBotForwardFromServer.TrySetValue( xIRCBotForwardFromServer.Checked );

            ConfigKey.IRCMessageColor.TrySetValue( Color.GetName( colorIRC ) );
            ConfigKey.IRCUseColor.TrySetValue( xIRCUseColor.Checked );


            // advanced
            ConfigKey.SubmitCrashReports.TrySetValue( xSubmitCrashReports.Checked );

            ConfigKey.RelayAllBlockUpdates.TrySetValue( xRelayAllBlockUpdates.Checked );
            ConfigKey.NoPartialPositionUpdates.TrySetValue( xNoPartialPositionUpdates.Checked );
            ConfigKey.TickInterval.TrySetValue( Convert.ToInt32( nTickInterval.Value ) );

            switch( cProcessPriority.SelectedIndex ) {
                case 0:
                    ConfigKey.ProcessPriority.ResetValue(); break;
                case 1:
                    ConfigKey.ProcessPriority.TrySetValue( ProcessPriorityClass.High ); break;
                case 2:
                    ConfigKey.ProcessPriority.TrySetValue( ProcessPriorityClass.AboveNormal ); break;
                case 3:
                    ConfigKey.ProcessPriority.TrySetValue( ProcessPriorityClass.Normal ); break;
                case 4:
                    ConfigKey.ProcessPriority.TrySetValue( ProcessPriorityClass.BelowNormal ); break;
                case 5:
                    ConfigKey.ProcessPriority.TrySetValue( ProcessPriorityClass.Idle ); break;
            }

            ConfigKey.BlockUpdateThrottling.TrySetValue( Convert.ToInt32( nThrottling.Value ) );

            ConfigKey.LowLatencyMode.TrySetValue( xLowLatencyMode.Checked );

            if( xMaxUndo.Checked ) ConfigKey.MaxUndo.TrySetValue( Convert.ToInt32( nMaxUndo.Value ) );
            else ConfigKey.MaxUndo.TrySetValue( 0 );
            ConfigKey.MaxUndoStates.TrySetValue( Convert.ToInt32( nMaxUndoStates.Value ) );

            ConfigKey.ConsoleName.TrySetValue( tConsoleName.Text );

            ConfigKey.HeartbeatToWoMDirect.TrySetValue( xHeartbeatToWoMDirect.Checked );

            SaveWorldList();
        }


        void SaveWorldList() {
            const string worldListTempFileName = Paths.WorldListFileName + ".tmp";
            try {
                XDocument doc = new XDocument();
                XElement root = new XElement( "fCraftWorldList" );
                foreach( WorldListEntry world in Worlds ) {
                    root.Add( world.Serialize() );
                }
                if( cMainWorld.SelectedItem != null ) {
                    root.Add( new XAttribute( "main", cMainWorld.SelectedItem ) );
                }
                doc.Add( root );
                doc.Save( worldListTempFileName );
                Paths.MoveOrReplace( worldListTempFileName, Paths.WorldListFileName );
            } catch( Exception ex ) {
                MessageBox.Show( String.Format( "An error occured while trying to save world list ({0}): {1}{2}",
                                                Paths.WorldListFileName,
                                                Environment.NewLine,
                                                ex ) );
            }
        }


        static void WriteEnum<TEnum>( [NotNull] ComboBox box, ConfigKey key ) where TEnum : struct {
            if( box == null ) throw new ArgumentNullException( "box" );
            if( !typeof( TEnum ).IsEnum ) throw new ArgumentException( "Enum type required" );
            try {
                TEnum val = (TEnum)Enum.Parse( typeof( TEnum ), box.SelectedIndex.ToString(), true );
                key.TrySetValue( val );
            } catch( ArgumentException ) {
                Logger.Log( LogType.Error,
                            "ConfigUI.WriteEnum<{0}>: Could not parse value for {1}. Using default ({2}).",
                            typeof( TEnum ).Name, key, key.GetString() );
            }
        }

        #endregion
    }
}