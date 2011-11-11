// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>

namespace fCraft.ConfigGUI {
    partial class MainForm {

        void FillToolTipsGeneral() {

            toolTip.SetToolTip( lServerName, ConfigKey.ServerName.GetDescription() );
            toolTip.SetToolTip( tServerName, ConfigKey.ServerName.GetDescription() );

            toolTip.SetToolTip( lMOTD, ConfigKey.MOTD.GetDescription() );
            toolTip.SetToolTip( tMOTD, ConfigKey.MOTD.GetDescription() );

            toolTip.SetToolTip( lMaxPlayers, ConfigKey.MaxPlayers.GetDescription() );
            toolTip.SetToolTip( nMaxPlayers, ConfigKey.MaxPlayers.GetDescription() );

            toolTip.SetToolTip( lMaxPlayersPerWorld, ConfigKey.MaxPlayersPerWorld.GetDescription() );
            toolTip.SetToolTip( nMaxPlayersPerWorld, ConfigKey.MaxPlayersPerWorld.GetDescription() );

            toolTip.SetToolTip( lDefaultRank, ConfigKey.DefaultRank.GetDescription() );
            toolTip.SetToolTip( cDefaultRank, ConfigKey.DefaultRank.GetDescription() );

            toolTip.SetToolTip( lPublic, ConfigKey.IsPublic.GetDescription() );
            toolTip.SetToolTip( cPublic, ConfigKey.IsPublic.GetDescription() );

            toolTip.SetToolTip( nPort, ConfigKey.Port.GetDescription() );
            toolTip.SetToolTip( lPort, ConfigKey.Port.GetDescription() );

            toolTip.SetToolTip( bPortCheck,
@"Check if the selected port is connectible.
If port check fails, you may need to set up
port forwarding on your router." );

            toolTip.SetToolTip( nUploadBandwidth, ConfigKey.UploadBandwidth.GetDescription() );
            toolTip.SetToolTip( lUploadBandwidth, ConfigKey.UploadBandwidth.GetDescription() );

            toolTip.SetToolTip( bMeasure,
@"Test your connection's upload speed with speedtest.net
Note: to convert from megabits to kilobytes, multiply the
number by 128" );

            toolTip.SetToolTip( bRules,
@"Edit the list of rules displayed by the ""/Rules"" command.
This list is stored in rules.txt, and can also be edited with any text editor.
If rules.txt is missing or empty, ""/Rules"" shows this message:
""Use common sense!""" );

            const string tipAnnouncements =
@"Show a random announcement every once in a while.
Announcements are shown to all players, one line at a time, in random order.";
            toolTip.SetToolTip( xAnnouncements, tipAnnouncements );

            toolTip.SetToolTip( nAnnouncements, ConfigKey.AnnouncementInterval.GetDescription() );
            toolTip.SetToolTip( lAnnouncementsUnits, ConfigKey.AnnouncementInterval.GetDescription() );

            toolTip.SetToolTip( bAnnouncements,
@"Edit the list of announcements (announcements.txt).
One line is shown at a time, in random order.
You can include any color codes in the announcements.
You can also edit announcements.txt with any text editor." );

            toolTip.SetToolTip( bGreeting,
@"Edit a custom greeting that's shown to connecting players.
You can use any color codes, and these special variables:
    {SERVER_NAME} = server name (as defined in config)
    {RANK} = connecting player's rank" );

        }


        void FillToolTipsChat() {

            toolTip.SetToolTip( xRankColorsInChat, ConfigKey.RankColorsInChat.GetDescription() );

            toolTip.SetToolTip( xRankColorsInWorldNames, ConfigKey.RankColorsInWorldNames.GetDescription() );

            toolTip.SetToolTip( xRankPrefixesInChat, ConfigKey.RankPrefixesInChat.GetDescription() );

            toolTip.SetToolTip( xRankPrefixesInList, ConfigKey.RankPrefixesInList.GetDescription() );

            toolTip.SetToolTip( xShowConnectionMessages, ConfigKey.ShowConnectionMessages.GetDescription() );

            toolTip.SetToolTip( xShowJoinedWorldMessages, ConfigKey.ShowJoinedWorldMessages.GetDescription() );

            toolTip.SetToolTip( bColorSys, ConfigKey.SystemMessageColor.GetDescription() );
            toolTip.SetToolTip( lColorSys, ConfigKey.SystemMessageColor.GetDescription() );

            toolTip.SetToolTip( bColorHelp, ConfigKey.HelpColor.GetDescription() );
            toolTip.SetToolTip( lColorHelp, ConfigKey.HelpColor.GetDescription() );

            toolTip.SetToolTip( bColorSay, ConfigKey.SayColor.GetDescription() );
            toolTip.SetToolTip( lColorSay, ConfigKey.SayColor.GetDescription() );

            toolTip.SetToolTip( bColorAnnouncement, ConfigKey.AnnouncementColor.GetDescription() );
            toolTip.SetToolTip( lColorAnnouncement, ConfigKey.AnnouncementColor.GetDescription() );

            toolTip.SetToolTip( bColorPM, ConfigKey.PrivateMessageColor.GetDescription() );
            toolTip.SetToolTip( lColorPM, ConfigKey.PrivateMessageColor.GetDescription() );

            toolTip.SetToolTip( bColorMe, ConfigKey.MeColor.GetDescription() );
            toolTip.SetToolTip( lColorMe, ConfigKey.MeColor.GetDescription() );

            toolTip.SetToolTip( bColorWarning, ConfigKey.WarningColor.GetDescription() );
            toolTip.SetToolTip( lColorWarning, ConfigKey.WarningColor.GetDescription() );
        }


        void FillToolTipsWorlds() {
            toolTip.SetToolTip( bAddWorld, "Add a new world to the list." );
            toolTip.SetToolTip( bWorldEdit, "Edit or replace an existing world." );
            toolTip.SetToolTip( cMainWorld, "Main world is the first world that players see when they join the server." );
            toolTip.SetToolTip( bWorldDelete, "Delete a world from the list." );

            toolTip.SetToolTip( lDefaultBuildRank, ConfigKey.DefaultBuildRank.GetDescription() );
            toolTip.SetToolTip( cDefaultBuildRank, ConfigKey.DefaultBuildRank.GetDescription() );

            toolTip.SetToolTip( tMapPath, ConfigKey.MapPath.GetDescription() );
            toolTip.SetToolTip( xMapPath, ConfigKey.MapPath.GetDescription() );

            toolTip.SetToolTip( xWoMEnableEnvExtensions, ConfigKey.WoMEnableEnvExtensions.GetDescription() );
        }


        void FillToolTipsRanks() {

            toolTip.SetToolTip( xAllowSecurityCircumvention,
@"Allows players to manupulate whitelists/blacklists or rank requirements
in order to join restricted worlds, or to build in worlds/zones. Normally
players with ManageWorlds and ManageZones permissions are not allowed to do this.
Affected commands:
    /WAccess
    /WBuild
    /WMain
    /ZEdit" );

            toolTip.SetToolTip( bAddRank, "Add a new rank to the list." );
            toolTip.SetToolTip( bDeleteRank,
@"Delete a rank from the list. You will be prompted to specify a replacement
rank - to be able to convert old references to the deleted rank." );
            toolTip.SetToolTip( bRaiseRank,
@"Raise a rank (and all players of the rank) on the hierarchy.
The hierarchy is used for all permission checks." );
            toolTip.SetToolTip( bLowerRank,
@"Lower a rank (and all players of the rank) on the hierarchy.
The hierarchy is used for all permission checks." );

            const string tipRankName =
"Name of the rank - between 2 and 16 alphanumeric characters.";
            toolTip.SetToolTip( lRankName, tipRankName );
            toolTip.SetToolTip( tRankName, tipRankName );

            const string tipRankColor =
@"Color associated with this rank.
Rank colors may be applied to player and world names.";
            toolTip.SetToolTip( lRankColor, tipRankColor );
            toolTip.SetToolTip( bColorRank, tipRankColor );

            const string tipPrefix =
@"1-character prefix that may be shown above player names.
The option to show prefixes in chat is on ""General"" tab.";
            toolTip.SetToolTip( lPrefix, tipPrefix );
            toolTip.SetToolTip( tPrefix, tipPrefix );



            toolTip.SetToolTip( permissionLimitBoxes[Permission.Kick],
@"Limit on who can be kicked by players of this rank.
By default, players can only kick players of same or lower rank." );

            toolTip.SetToolTip( permissionLimitBoxes[Permission.Ban],
@"Limit on who can be banned by players of this rank.
By default, players can only ban players of same or lower rank." );

            toolTip.SetToolTip( permissionLimitBoxes[Permission.Promote],
@"Limit on how much can players of this rank promote others.
By default, players can only promote up to the same or lower rank." );

            toolTip.SetToolTip( permissionLimitBoxes[Permission.Demote],
@"Limit on whom players of this rank can demote.
By default, players can only demote players of same or lower rank." );

            toolTip.SetToolTip( permissionLimitBoxes[Permission.Hide],
@"Limit on whom can players of this rank hide from.
By default, players can only hide from players of same or lower rank." );

            toolTip.SetToolTip( permissionLimitBoxes[Permission.Freeze],
@"Limit on who can be frozen by players of this rank.
By default, players can only freeze players of same or lower rank." );

            toolTip.SetToolTip( permissionLimitBoxes[Permission.Mute],
@"Limit on who can be muted by players of this rank.
By default, players can only mute players of same or lower rank." );

            toolTip.SetToolTip( permissionLimitBoxes[Permission.Bring],
@"Limit on who can be brought (forcibly teleported) by players of this rank.
By default, players can only bring players of same or lower rank." );

            toolTip.SetToolTip( permissionLimitBoxes[Permission.Spectate],
@"Limit on who can be spectated by players of this rank.
By default, players can only bring players of same or lower rank." );

            toolTip.SetToolTip( permissionLimitBoxes[Permission.UndoOthersActions],
@"Limit on whose actions players of this rank can undo.
By default, players can only undo actions of players of same or lower rank." );



            toolTip.SetToolTip( xReserveSlot,
@"Allows players of this rank to join the server
even if it reached the maximum number of players." );

            const string tipKickIdle = "Allows kicking players who have been inactive/AFK for some time.";
            toolTip.SetToolTip( xKickIdle, tipKickIdle );
            toolTip.SetToolTip( nKickIdle, tipKickIdle );
            toolTip.SetToolTip( lKickIdleUnits, tipKickIdle );

            toolTip.SetToolTip( xAntiGrief,
@"Antigrief is an automated system for kicking players who build
or delete at abnormally high rates. This helps stop certain kinds
of malicious software (like MCTunnel) from doing large-scale damage
to server maps. False positives can sometimes occur if server or
player connection is very laggy." );

            toolTip.SetToolTip( nAntiGriefBlocks,
@"Maximum number of blocks that players of this rank are
allowed to build in a specified time period." );

            toolTip.SetToolTip( nAntiGriefBlocks,
@"Minimum time interval that players of this rank are
expected to spent to build a specified number of blocks." );

            const string tipDrawLimit =
@"Limit on the number of blocks that a player is
allowed to affect with drawing or copy/paste commands
at one time. If unchecked, there is no limit.";
            toolTip.SetToolTip( xDrawLimit, tipDrawLimit );
            toolTip.SetToolTip( nDrawLimit, tipDrawLimit );
            toolTip.SetToolTip( lDrawLimitUnits, tipDrawLimit );




            vPermissions.Items[(int)Permission.Ban].ToolTipText =
@"Ability to ban/unban other players from the server.
Affected commands:
    /Ban
    /Unban";

            vPermissions.Items[(int)Permission.BanAll].ToolTipText =
@"Ability to ban/unban a player account, his IP, and all other accounts that used the IP.
BanAll/UnbanAll commands can be used on players who keep evading bans.
Required permissions: Ban & BanIP
Affected commands:
    /BanAll
    /UnbanAll";

            vPermissions.Items[(int)Permission.BanIP].ToolTipText =
@"Ability to ban/unban players by IP.
Required permission: Ban
Affected commands:
    /BanIP
    /UnbanIP";

            vPermissions.Items[(int)Permission.Bring].ToolTipText =
@"Ability to bring/summon other players to your location.
This works a bit like reverse-teleport - other player is sent to you.
Affected commands:
    /Bring
    /BringAll";

            vPermissions.Items[(int)Permission.BringAll].ToolTipText =
@"Ability to bring/summon many players at a time to your location.
Affected command:
    /BringAll";

            vPermissions.Items[(int)Permission.Build].ToolTipText =
@"Ability to place blocks on maps. This is a baseline permission
that can be overridden by world-specific and zone-specific permissions.";

            vPermissions.Items[(int)Permission.Chat].ToolTipText =
@"Ability to chat and PM players. Note that players without this
permission can still type in commands, receive PMs, and read chat.
Affected commands:
    /Say
    @ (pm)
    @@ (rank chat)";

            vPermissions.Items[(int)Permission.CopyAndPaste].ToolTipText =
@"Ability to copy (or cut) and paste blocks. The total number of
blocks that can be copied or pasted at a time is affected by
the draw limit.
Affected commands:
    /Copy
    /Cut
    /Mirror
    /Paste, /PasteNot
    /Rotate";

            vPermissions.Items[(int)Permission.Delete].ToolTipText =
@"Ability to delete or replace blocks on maps. This is a baseline permission
that can be overridden by world-specific and zone-specific permissions.";

            vPermissions.Items[(int)Permission.DeleteAdmincrete].ToolTipText =
@"Ability to delete admincrete (aka adminium) blocks. Even if someone
has this permission, it can be overridden by world-specific and
zone-specific permissions.
Required permission: Delete";

            vPermissions.Items[(int)Permission.Demote].ToolTipText =
@"Ability to demote other players to a lower rank.
Affected commands:
    /Rank
    /MassRank";

            vPermissions.Items[(int)Permission.Draw].ToolTipText =
@"Ability to use drawing tools (commands capable of affecting many blocks
at once). This permission can be overridden by world-specific and
zone-specific permissions.
Required permission: Build, Delete
Affected commands:
    /Cuboid, /CuboidH, and /CuboidW
    /Ellipsoid and /EllipsoidH
    /Line
    /Replace and /ReplaceNot
    /Undo and /Redo";

            vPermissions.Items[(int)Permission.DrawAdvanced].ToolTipText =
@"Ability to use advanced drawing tools, such as brushes.
Required permission: Build, Delete, Draw
Affected commands:
    /Brush
    /ReplaceBrush
    /Restore
    /Sphere and /SphereH
    /Torus";

            vPermissions.Items[(int)Permission.EditPlayerDB].ToolTipText =
@"Ability to edit the player database directly. This also adds the ability to
promote/demote players by name, even if they have not visited the server yet.
Also allows to manipulate players' records, and to promote/demote players in batches.
Affected commands:
    /PruneDB
    /AutoRankAll
    /MassRank
    /SetInfo
    /InfoSwap
    /DumpStats";

            vPermissions.Items[(int)Permission.Freeze].ToolTipText =
@"Ability to freeze/unfreeze players. Frozen players cannot
move or build/delete.
Affected commands:
    /Freeze
    /Unfreeze";

            vPermissions.Items[(int)Permission.Hide].ToolTipText =
@"Ability to appear hidden from other players. You can still chat,
build/delete blocks, use all commands, and join worlds while hidden.
Hidden players are completely invisible to other players.
Affected commands:
    /Hide
    /Unhide";

            vPermissions.Items[(int)Permission.Import].ToolTipText =
@"Ability to import rank and ban lists from files. Useful if you
are switching from another server software.
Affected commands:
    /Import";

            vPermissions.Items[(int)Permission.Kick].ToolTipText =
@"Ability to kick players from the server.
Affected commands:
    /Kick";

            vPermissions.Items[(int)Permission.Lock].ToolTipText =
@"Ability to lock/unlock maps (locking puts a map into read-only state).
Affected commands:
    /WLock
    /WUnlock";

            vPermissions.Items[(int)Permission.ManageWorlds].ToolTipText =
@"Ability to manipulate the world list: adding, renaming, and deleting worlds,
loading/saving maps, change per-world permissions, and using the map generator.
Affected commands:
    /WLoad
    /WUnload
    /WRename
    /WMain
    /WAccess and /WBuild
    /WFlush
    /Gen";


            vPermissions.Items[(int)Permission.ManageBlockDB].ToolTipText =
@"Ability to enable/disable, clear, and configure BlockDB.
Affected command:
    /BlockDB";

            vPermissions.Items[(int)Permission.ManageZones].ToolTipText =
@"Ability to manipulate zones: adding, editing, renaming, and removing zones.
Affected commands:
    /ZAdd
    /ZEdit
    /ZRemove
    /ZRename";

            vPermissions.Items[(int)Permission.Mute].ToolTipText =
@"Ability to temporarily mute players. Muted players cannot write chat or 
send PMs, but they can still type in commands, receive PMs, and read chat.
Affected commands:
    /Mute
    /Unmute";

            vPermissions.Items[(int)Permission.Patrol].ToolTipText =
@"Ability to patrol lower-ranked players. ""Patrolling"" means teleporting
to other players to check on them, usually while hidden.
Required permission: Teleport
Affected commands:
    /Patrol
    /SpecPatrol";

            vPermissions.Items[(int)Permission.PlaceAdmincrete].ToolTipText =
@"Ability to place admincrete/adminium. This also affects draw commands.
Required permission: Build
Affected commands:
    /Solid
    /Bind";

            vPermissions.Items[(int)Permission.PlaceGrass].ToolTipText =
@"Ability to place grass blocks. This also affects draw commands.
Required permission: Build
Affected commands:
    /Grass
    /Bind";

            vPermissions.Items[(int)Permission.PlaceLava].ToolTipText =
@"Ability to place lava blocks. This also affects draw commands.
Required permission: Build
Affected commands:
    /Lava
    /Bind";

            vPermissions.Items[(int)Permission.PlaceWater].ToolTipText =
@"Ability to place water blocks. This also affects draw commands.
Required permission: Build
Affected commands:
    /Water
    /Bind";

            vPermissions.Items[(int)Permission.Promote].ToolTipText =
@"Ability to promote players to a higher rank.
Affected commands:
    /Rank";

            vPermissions.Items[(int)Permission.ReadStaffChat].ToolTipText =
@"Ability to read staff chat.";

            vPermissions.Items[(int)Permission.ReloadConfig].ToolTipText =
@"Ability to reload the configuration file without restarting.
Affected commands:
    /Reload";

            vPermissions.Items[(int)Permission.Say].ToolTipText =
@"Ability to use /Say command.
Required permission: Chat
Affected commands:
    /Say";

            vPermissions.Items[(int)Permission.SetSpawn].ToolTipText =
@"Ability to change the spawn point of a world or a player.
Affected commands:
    /SetSpawn";

            vPermissions.Items[(int)Permission.ShutdownServer].ToolTipText =
@"Ability to shut down or restart the server remotely.
Useful for servers that run on dedicated machines.
Affected commands:
    /Shutdown
    /Restart";

            vPermissions.Items[(int)Permission.Spectate].ToolTipText =
@"Ability to spectate/follow other players in first-person view.
Affected commands:
    /Spectate";

            vPermissions.Items[(int)Permission.Teleport].ToolTipText =
@"Ability to teleport to other players.
Affected commands:
    /TP";

            vPermissions.Items[(int)Permission.UndoOthersActions].ToolTipText =
@"Ability to undo actions of other players, using the BlockDB.
Affected commands:
    /UndoArea
    /UndoPlayer";

            vPermissions.Items[(int)Permission.UseColorCodes].ToolTipText =
@"Ability to use color codes in chat messages.";

            vPermissions.Items[(int)Permission.UseSpeedHack].ToolTipText =
@"Ability to move at a faster-than-normal rate (using hacks).
WARNING: Speedhack detection is often inaccurate, and may produce many
false positives - especially on laggy servers.";

            vPermissions.Items[(int)Permission.ViewOthersInfo].ToolTipText =
@"Ability to view extended information about other players.
Affected commands:
    /Info
    /BanInfo
    /Where";

            vPermissions.Items[(int)Permission.ViewPlayerIPs].ToolTipText =
@"Ability to view players' IP addresses.
Affected commands:
    /Info
    /BanInfo
    /BanIP, /BanAll, /UnbanIP, /UnbanAll";
        }


        void FillToolTipsSecurity() {
            toolTip.SetToolTip( lVerifyNames, ConfigKey.VerifyNames.GetDescription() );
            toolTip.SetToolTip( cVerifyNames, ConfigKey.VerifyNames.GetDescription() );

            toolTip.SetToolTip( xMaxConnectionsPerIP, ConfigKey.MaxConnectionsPerIP.GetDescription() );
            toolTip.SetToolTip( nMaxConnectionsPerIP, ConfigKey.MaxConnectionsPerIP.GetDescription() );

            toolTip.SetToolTip( xAllowUnverifiedLAN, ConfigKey.AllowUnverifiedLAN.GetDescription() );

            toolTip.SetToolTip( xRequireBanReason, ConfigKey.RequireBanReason.GetDescription() );
            toolTip.SetToolTip( xRequireKickReason, ConfigKey.RequireKickReason.GetDescription() );
            toolTip.SetToolTip( xRequireRankChangeReason, ConfigKey.RequireRankChangeReason.GetDescription() );

            toolTip.SetToolTip( xAnnounceKickAndBanReasons, ConfigKey.AnnounceKickAndBanReasons.GetDescription() );
            toolTip.SetToolTip( xAnnounceRankChanges, ConfigKey.AnnounceRankChanges.GetDescription() );
            toolTip.SetToolTip( xAnnounceRankChangeReasons, ConfigKey.AnnounceRankChanges.GetDescription() );


            toolTip.SetToolTip( lPatrolledRank, ConfigKey.PatrolledRank.GetDescription() );
            toolTip.SetToolTip( cPatrolledRank, ConfigKey.PatrolledRank.GetDescription() );
            toolTip.SetToolTip( lPatrolledRankAndBelow, ConfigKey.PatrolledRank.GetDescription() );

            toolTip.SetToolTip( nAntispamMessageCount, ConfigKey.AntispamMessageCount.GetDescription() );
            toolTip.SetToolTip( lAntispamMessageCount, ConfigKey.AntispamMessageCount.GetDescription() );
            toolTip.SetToolTip( nAntispamInterval, ConfigKey.AntispamInterval.GetDescription() );
            toolTip.SetToolTip( lAntispamInterval, ConfigKey.AntispamInterval.GetDescription() );

            toolTip.SetToolTip( xAntispamKicks, "Kick players who repeatedly trigger antispam warnings." );
            toolTip.SetToolTip( nAntispamMaxWarnings, ConfigKey.AntispamMaxWarnings.GetDescription() );
            toolTip.SetToolTip( lAntispamMaxWarnings, ConfigKey.AntispamMaxWarnings.GetDescription() );

            toolTip.SetToolTip( xPaidPlayersOnly, ConfigKey.PaidPlayersOnly.GetDescription() );

            toolTip.SetToolTip( xBlockDBEnabled, ConfigKey.BlockDBEnabled.GetDescription() );
            toolTip.SetToolTip( xBlockDBAutoEnable, ConfigKey.BlockDBAutoEnable.GetDescription() );
            toolTip.SetToolTip( cBlockDBAutoEnableRank, ConfigKey.BlockDBAutoEnableRank.GetDescription() );
        }


        void FillToolTipsSavingAndBackup() {

            toolTip.SetToolTip( xSaveInterval, ConfigKey.SaveInterval.GetDescription() );
            toolTip.SetToolTip( nSaveInterval, ConfigKey.SaveInterval.GetDescription() );
            toolTip.SetToolTip( lSaveIntervalUnits, ConfigKey.SaveInterval.GetDescription() );

            toolTip.SetToolTip( xBackupOnStartup, ConfigKey.BackupOnStartup.GetDescription() );

            toolTip.SetToolTip( xBackupOnJoin, ConfigKey.BackupOnJoin.GetDescription() );

            toolTip.SetToolTip( xBackupInterval, ConfigKey.DefaultBackupInterval.GetDescription() );
            toolTip.SetToolTip( nBackupInterval, ConfigKey.DefaultBackupInterval.GetDescription() );
            toolTip.SetToolTip( lBackupIntervalUnits, ConfigKey.DefaultBackupInterval.GetDescription() );

            toolTip.SetToolTip( xBackupOnlyWhenChanged, ConfigKey.DefaultBackupInterval.GetDescription() );

            toolTip.SetToolTip( xMaxBackups, ConfigKey.MaxBackups.GetDescription() );
            toolTip.SetToolTip( nMaxBackups, ConfigKey.MaxBackups.GetDescription() );
            toolTip.SetToolTip( lMaxBackups, ConfigKey.MaxBackups.GetDescription() );

            toolTip.SetToolTip( xMaxBackupSize, ConfigKey.MaxBackupSize.GetDescription() );
            toolTip.SetToolTip( nMaxBackupSize, ConfigKey.MaxBackupSize.GetDescription() );
            toolTip.SetToolTip( lMaxBackupSize, ConfigKey.MaxBackupSize.GetDescription() );
        }


        void FillToolTipsLogging() {
            toolTip.SetToolTip( lLogMode, ConfigKey.LogMode.GetDescription() );
            toolTip.SetToolTip( cLogMode, ConfigKey.LogMode.GetDescription() );

            toolTip.SetToolTip( xLogLimit, ConfigKey.MaxLogs.GetDescription() );
            toolTip.SetToolTip( nLogLimit, ConfigKey.MaxLogs.GetDescription() );
            toolTip.SetToolTip( lLogLimitUnits, ConfigKey.MaxLogs.GetDescription() );

            vLogFileOptions.Items[(int)LogType.ConsoleInput].ToolTipText = "Commands typed in from the server console.";
            vLogFileOptions.Items[(int)LogType.ConsoleOutput].ToolTipText =
@"Things sent directly in response to console input,
e.g. output of commands called from console.";
            vLogFileOptions.Items[(int)LogType.Debug].ToolTipText = "Technical information that may be useful to find bugs.";
            vLogFileOptions.Items[(int)LogType.Error].ToolTipText = "Major errors and problems.";
            vLogFileOptions.Items[(int)LogType.SeriousError].ToolTipText = "Errors that prevent server from starting or result in crashes.";
            vLogFileOptions.Items[(int)LogType.GlobalChat].ToolTipText = "Normal chat messages written by players.";
            vLogFileOptions.Items[(int)LogType.IRC].ToolTipText =
@"IRC-related status and error messages.
Does not include IRC chatter (see IRCChat).";
            vLogFileOptions.Items[(int)LogType.PrivateChat].ToolTipText = "PMs (Private Messages) exchanged between players (@player message).";
            vLogFileOptions.Items[(int)LogType.RankChat].ToolTipText = "Rank-wide messages (@@rank message).";
            vLogFileOptions.Items[(int)LogType.SuspiciousActivity].ToolTipText = "Suspicious activity - hack attempts, failed logins, unverified names.";
            vLogFileOptions.Items[(int)LogType.SystemActivity].ToolTipText = "Status messages regarding normal system activity.";
            vLogFileOptions.Items[(int)LogType.UserActivity].ToolTipText = "Status messages regarding players' actions.";
            vLogFileOptions.Items[(int)LogType.UserCommand].ToolTipText = "Commands types in by players.";
            vLogFileOptions.Items[(int)LogType.Warning].ToolTipText = "Minor, recoverable errors and problems.";

            for( int i = 0; i < vConsoleOptions.Items.Count; i++ ) {
                vConsoleOptions.Items[i].ToolTipText = vLogFileOptions.Items[i].ToolTipText;
            }
        }


        void FillToolTipsIRC() {
            toolTip.SetToolTip( xIRCBotEnabled, ConfigKey.IRCBotEnabled.GetDescription() );

            const string tipIRCList =
@"Choose one of these popular IRC networks,
or type in address/port manually below.";
            toolTip.SetToolTip( lIRCList, tipIRCList );
            toolTip.SetToolTip( cIRCList, tipIRCList );

            toolTip.SetToolTip( lIRCBotNick, ConfigKey.IRCBotNick.GetDescription() );
            toolTip.SetToolTip( tIRCBotNick, ConfigKey.IRCBotNick.GetDescription() );

            toolTip.SetToolTip( lIRCBotNetwork, ConfigKey.IRCBotNetwork.GetDescription() );
            toolTip.SetToolTip( tIRCBotNetwork, ConfigKey.IRCBotNetwork.GetDescription() );

            toolTip.SetToolTip( lIRCBotPort, ConfigKey.IRCBotPort.GetDescription() );
            toolTip.SetToolTip( nIRCBotPort, ConfigKey.IRCBotPort.GetDescription() );

            toolTip.SetToolTip( lIRCDelay, ConfigKey.IRCDelay.GetDescription() );
            toolTip.SetToolTip( nIRCDelay, ConfigKey.IRCDelay.GetDescription() );
            toolTip.SetToolTip( lIRCDelayUnits, ConfigKey.IRCDelay.GetDescription() );

            toolTip.SetToolTip( tIRCBotChannels, ConfigKey.IRCBotChannels.GetDescription() );

            toolTip.SetToolTip( xIRCRegisteredNick, ConfigKey.IRCRegisteredNick.GetDescription() );

            toolTip.SetToolTip( lIRCNickServ, ConfigKey.IRCNickServ.GetDescription() );
            toolTip.SetToolTip( tIRCNickServ, ConfigKey.IRCNickServ.GetDescription() );

            toolTip.SetToolTip( lIRCNickServMessage, ConfigKey.IRCNickServMessage.GetDescription() );
            toolTip.SetToolTip( tIRCNickServMessage, ConfigKey.IRCNickServMessage.GetDescription() );

            toolTip.SetToolTip( lColorIRC, ConfigKey.IRCMessageColor.GetDescription() );
            toolTip.SetToolTip( bColorIRC, ConfigKey.IRCMessageColor.GetDescription() );

            toolTip.SetToolTip( xIRCBotForwardFromIRC, ConfigKey.IRCBotForwardFromIRC.GetDescription() );
            toolTip.SetToolTip( xIRCBotAnnounceIRCJoins, ConfigKey.IRCBotAnnounceIRCJoins.GetDescription() );

            toolTip.SetToolTip( xIRCBotForwardFromServer, ConfigKey.IRCBotForwardFromServer.GetDescription() );
            toolTip.SetToolTip( xIRCBotAnnounceServerJoins, ConfigKey.IRCBotAnnounceServerJoins.GetDescription() );
            toolTip.SetToolTip( xIRCBotAnnounceServerEvents, ConfigKey.IRCBotAnnounceServerEvents.GetDescription() );

            // TODO: IRCThreads

            toolTip.SetToolTip( xIRCUseColor, ConfigKey.IRCUseColor.GetDescription() );
        }


        void FillToolTipsAdvanced() {
            toolTip.SetToolTip( xRelayAllBlockUpdates, ConfigKey.RelayAllBlockUpdates.GetDescription() );

            toolTip.SetToolTip( xNoPartialPositionUpdates, ConfigKey.NoPartialPositionUpdates.GetDescription() );

            toolTip.SetToolTip( xLowLatencyMode, ConfigKey.LowLatencyMode.GetDescription() );

            toolTip.SetToolTip( lProcessPriority, ConfigKey.ProcessPriority.GetDescription() );
            toolTip.SetToolTip( cProcessPriority, ConfigKey.ProcessPriority.GetDescription() );

            toolTip.SetToolTip( lUpdater, ConfigKey.UpdaterMode.GetDescription() );
            toolTip.SetToolTip( cUpdaterMode, ConfigKey.UpdaterMode.GetDescription() );

            toolTip.SetToolTip( lThrottling, ConfigKey.BlockUpdateThrottling.GetDescription() );
            toolTip.SetToolTip( nThrottling, ConfigKey.BlockUpdateThrottling.GetDescription() );
            toolTip.SetToolTip( lThrottlingUnits, ConfigKey.BlockUpdateThrottling.GetDescription() );

            toolTip.SetToolTip( lTickInterval, ConfigKey.TickInterval.GetDescription() );
            toolTip.SetToolTip( nTickInterval, ConfigKey.TickInterval.GetDescription() );
            toolTip.SetToolTip( lTickIntervalUnits, ConfigKey.TickInterval.GetDescription() );

            toolTip.SetToolTip( xMaxUndo, ConfigKey.MaxUndo.GetDescription() );
            toolTip.SetToolTip( nMaxUndo, ConfigKey.MaxUndo.GetDescription() );
            toolTip.SetToolTip( lMaxUndoUnits, ConfigKey.MaxUndo.GetDescription() );

            toolTip.SetToolTip( xIP, ConfigKey.IP.GetDescription() );
            toolTip.SetToolTip( tIP, ConfigKey.IP.GetDescription() );

            toolTip.SetToolTip( xHeartbeatToWoMDirect, ConfigKey.HeartbeatToWoMDirect.GetDescription() );
        }
    }
}