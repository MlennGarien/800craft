// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using fCraft.MapConversion;
using System.Collections.Generic;
using fCraft.Events;

namespace fCraft {
    /// <summary> Contains commands related to zone management. </summary>
    static class ZoneCommands {

        internal static void Init() {
            CommandManager.RegisterCommand( CdZoneAdd );
            CommandManager.RegisterCommand( CdZoneEdit );
            CommandManager.RegisterCommand( CdZoneInfo );
            CommandManager.RegisterCommand( CdZoneList );
            CommandManager.RegisterCommand( CdZoneMark );
            CommandManager.RegisterCommand( CdZoneRemove );
            CommandManager.RegisterCommand( CdZoneRename );
            CommandManager.RegisterCommand( CdZoneTest );

            CommandManager.RegisterCustomCommand(cdDoor);
            CommandManager.RegisterCustomCommand(cdDoorRemove);
            Player.Clicked += PlayerClickedDoor;
            openDoors = new List<Zone>();
        }

        static readonly TimeSpan DoorCloseTimer = TimeSpan.FromMilliseconds(1500);
        const int maxDoorBlocks = 30;  //change for max door area
        static List<Zone> openDoors;

        struct DoorInfo
        {
            public readonly Zone Zone;
            public readonly Block[] Buffer;
            public readonly Map WorldMap;
            public DoorInfo(Zone zone, Block[] buffer, Map worldMap)
            {
                Zone = zone;
                Buffer = buffer;
                WorldMap = worldMap;
            }
        }

        static readonly CommandDescriptor cdDoor = new CommandDescriptor
        {
            Name = "Door",
            Category = CommandCategory.Zone,
            Permissions = new[] { Permission.Build },
            Help = "Creates door zone. Left click to open doors.",
            Handler = Door
        };

        static void Door(Player player, Command cmd)
        {
            if (player.WorldMap.Zones.FindExact(player.Name + "door") != null)
            {
                player.Message("One door per person.");
                return;
            }

            Zone door = new Zone();
            door.Name = player.Name + "door";
            player.SelectionStart(2, DoorAdd, door, cdDoor.Permissions);
            player.Message("Door: Place a block or type /mark to use your location.");
        }

        static readonly CommandDescriptor cdDoorRemove = new CommandDescriptor
        {
            Name = "Removedoor",
            Aliases = new[] { "rd" },
            Category = CommandCategory.Zone,
            Permissions = new[] { Permission.Build },
            Help = "Removes door.",
            Handler = DoorRemove
        };

        static void DoorRemove(Player player, Command cmd)
        {
            Zone zone;
            string playerName = cmd.Next();
            if (playerName == null)
            {
                if ((zone = player.WorldMap.Zones.FindExact(player.Name + "door")) != null)
                {
                    player.WorldMap.Zones.Remove(zone);
                    player.Message("Door removed.");
                }
                else
                {
                    player.Message("You do not have a door on this map.");
                }
            }
            else
            {
                if (!Player.IsValidName(playerName))
                {
                    player.Message("&SInvalid name");
                    return;
                }
                Player target = Server.FindPlayerOrPrintMatches(player, playerName, true, true);
                if (target == null) return;
                if ((zone = player.WorldMap.Zones.FindExact(target.Name + "door")) != null)
                {
                    player.WorldMap.Zones.Remove(zone);
                    player.Message("Door removed for " + target.ClassyName);
                }
                else
                {
                    player.Message(target.ClassyName + "&S does not have a door on this map.");
                }
            }
        }

        static void DoorAdd(Player player, Vector3I[] marks, object tag)
        {
            int sx = Math.Min(marks[0].X, marks[1].X);
            int ex = Math.Max(marks[0].X, marks[1].X);
            int sy = Math.Min(marks[0].Y, marks[1].Y);
            int ey = Math.Max(marks[0].Y, marks[1].Y);
            int sh = Math.Min(marks[0].Z, marks[1].Z);
            int eh = Math.Max(marks[0].Z, marks[1].Z);

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1);
            if (volume > maxDoorBlocks)
            {
                player.Message("Doors are only allowed to be {0} blocks", maxDoorBlocks);
                return;
            }
            if (!player.Info.Rank.AllowSecurityCircumvention)
            {
                SecurityCheckResult buildCheck = player.World.BuildSecurity.CheckDetailed(player.Info);
                switch (buildCheck)
                {
                    case SecurityCheckResult.BlackListed:
                        player.Message("Cannot add a door to world {0}&S: You are barred from building here.",
                                        player.World.ClassyName);
                        return;
                    case SecurityCheckResult.RankTooLow:
                        player.Message("Cannot add a door to world {0}&S: You are not allowed to build here.",
                                        player.World.ClassyName);
                        return;
                    //case SecurityCheckResult.RankTooHigh:
                }
            }
            for (int x = sx; x < ex; x++){
                for (int y = sy; y < ey; y++){
                    for (int z = sh; z < eh; z++){
                        if (player.CanPlace(player.World.Map, new Vector3I(x, y, z), Block.Wood, BlockChangeContext.Manual) != CanPlaceResult.Allowed){
                            player.Message("Cannot add a door to world {0}&S: Build permissions in this area replied with 'denied'.",
                                        player.World.ClassyName);
                            return;
                        }
                    }
                }
            }
            Zone door = (Zone)tag;
            door.Create(new BoundingBox(marks[0], marks[1]), player.Info);
            player.WorldMap.Zones.Add(door);
            Logger.Log(LogType.UserActivity, "{0} created door {1} (on world {2})", player.Name, door.Name, player.World.Name);
            player.Message("Door created: {0}x{1}x{2}", door.Bounds.Dimensions.X,
                                                        door.Bounds.Dimensions.Y,
                                                        door.Bounds.Dimensions.Z);
        }

        static readonly object openDoorsLock = new object();
        public static void PlayerClickedDoor(object sender, PlayerClickedEventArgs e)
        {

            Zone[] allowed, denied;
            if (e.Player.WorldMap.Zones.CheckDetailed(e.Coords, e.Player, out allowed, out denied))
            {
                foreach (Zone zone in allowed)
                {
                    if (zone.Name.EndsWith("door"))
                    {
                        Player.RaisePlayerPlacedBlockEvent(e.Player, e.Player.WorldMap, e.Coords, e.Block, e.Block, BlockChangeContext.Manual);
                        lock (openDoorsLock)
                        {
                            if (!openDoors.Contains(zone))
                            {
                                openDoor(zone, e.Player);
                                openDoors.Add(zone);
                            }
                        }
                    }
                }
            }

        }

        static void openDoor(Zone zone, Player player)
        {
            int sx = zone.Bounds.XMin;
            int ex = zone.Bounds.XMax;
            int sy = zone.Bounds.YMin;
            int ey = zone.Bounds.YMax;
            int sz = zone.Bounds.ZMin;
            int ez = zone.Bounds.ZMax;

            Block[] buffer = new Block[zone.Bounds.Volume];

            int counter = 0;
            for (int x = sx; x <= ex; x++)
            {
                for (int y = sy; y <= ey; y++)
                {
                    for (int z = sz; z <= ez; z++)
                    {
                        buffer[counter] = player.WorldMap.GetBlock(x, y, z);
                        player.WorldMap.QueueUpdate(new BlockUpdate(null, new Vector3I(x, y, z), Block.Air));
                        counter++;
                    }
                }
            }

            DoorInfo info = new DoorInfo(zone, buffer, player.WorldMap);
            //reclose door
            Scheduler.NewTask(doorTimer_Elapsed).RunOnce(info, DoorCloseTimer);

        }

        static void doorTimer_Elapsed(SchedulerTask task)
        {
            DoorInfo info = (DoorInfo)task.UserState;
            int counter = 0;
            for (int x = info.Zone.Bounds.XMin; x <= info.Zone.Bounds.XMax; x++)
            {
                for (int y = info.Zone.Bounds.YMin; y <= info.Zone.Bounds.YMax; y++)
                {
                    for (int z = info.Zone.Bounds.ZMin; z <= info.Zone.Bounds.ZMax; z++)
                    {
                        info.WorldMap.QueueUpdate(new BlockUpdate(null, new Vector3I(x, y, z), info.Buffer[counter]));
                        counter++;
                    }
                }
            }

            lock (openDoorsLock) { openDoors.Remove(info.Zone); }
        }

        #region ZoneAdd

        static readonly CommandDescriptor CdZoneAdd = new CommandDescriptor {
            Name = "ZAdd",
            Category = CommandCategory.Zone,
            Aliases = new[] { "zone" },
            Permissions = new[] { Permission.ManageZones },
            Usage = "/ZAdd ZoneName RankName [{+|-}PlayerName] msg=CustomDenyMessage",
            Help = "Create a zone that overrides build permissions. " +
                   "This can be used to restrict access to an area (by setting RankName to a high rank) " +
                   "or to designate a guest area (by lowering RankName).",
            Handler = ZoneAddHandler
        };

        static void ZoneAddHandler( Player player, Command cmd ) {
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );

            string givenZoneName = cmd.Next();
            if( givenZoneName == null ) {
                CdZoneAdd.PrintUsage( player );
                return;
            }

            if( !player.Info.Rank.AllowSecurityCircumvention ) {
                SecurityCheckResult buildCheck = playerWorld.BuildSecurity.CheckDetailed( player.Info );
                switch( buildCheck ) {
                    case SecurityCheckResult.BlackListed:
                        player.Message( "Cannot add zones to world {0}&S: You are barred from building here.",
                                        playerWorld.ClassyName );
                        return;
                    case SecurityCheckResult.RankTooLow:
                        player.Message( "Cannot add zones to world {0}&S: You are not allowed to build here.",
                                        playerWorld.ClassyName );
                        return;
                    //case SecurityCheckResult.RankTooHigh:
                }
            }

            Zone newZone = new Zone();
            ZoneCollection zoneCollection = player.WorldMap.Zones;

            if( givenZoneName.StartsWith( "+" ) ) {
                // personal zone (/ZAdd +Name)
                givenZoneName = givenZoneName.Substring( 1 );

                // Find the target player
                PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches( player, givenZoneName );
                if( info == null ) return;

                // Make sure that the name is not taken already.
                // If a zone named after the player already exists, try adding a number after the name (e.g. "Notch2")
                newZone.Name = info.Name;
                for( int i = 2; zoneCollection.Contains( newZone.Name ); i++ ) {
                    newZone.Name = givenZoneName + i;
                }

                newZone.Controller.MinRank = info.Rank.NextRankUp ?? info.Rank;
                newZone.Controller.Include( info );
                player.Message( "Zone: Creating a {0}+&S zone for player {1}&S.",
                                newZone.Controller.MinRank.ClassyName, info.ClassyName );
            } else {
                // Adding an ordinary, rank-restricted zone.
                if( !World.IsValidName( givenZoneName ) ) {
                    player.Message( "\"{0}\" is not a valid zone name", givenZoneName );
                    return;
                }

                if( zoneCollection.Contains( givenZoneName ) ) {
                    player.Message( "A zone with this name already exists. Use &H/ZEdit&S to edit." );
                    return;
                }

                newZone.Name = givenZoneName;

                string rankName = cmd.Next();
                if( rankName == null ) {
                    player.Message( "No rank was specified. See &H/Help zone" );
                    return;
                }
                Rank minRank = RankManager.FindRank( rankName );

                if( minRank != null ) {
                    string name;
                    while( (name = cmd.Next()) != null ) {

                        if( name.Length == 0 ) continue;

						if (name.ToLower().StartsWith("msg="))
						{
							newZone.Message = name.Substring(4) + " " + (cmd.NextAll() ?? "");
                            player.Message("Zone: Custom denied messaged changed to '" + newZone.Message + "'");
							break;
						}

                        PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches( player, name.Substring( 1 ) );
                        if( info == null ) return;

                        if( name.StartsWith( "+" ) ) {
                            newZone.Controller.Include( info );
                        } else if( name.StartsWith( "-" ) ) {
                            newZone.Controller.Exclude( info );
                        }
                    }

                    newZone.Controller.MinRank = minRank;
                } else {
                    player.MessageNoRank( rankName );
                	return;
                }
            }
			player.Message("Zone "+ newZone.ClassyName + "&S: Place a block or type &H/Mark&S to use your location.");
			player.SelectionStart(2, ZoneAddCallback, newZone, CdZoneAdd.Permissions);
        }

        static void ZoneAddCallback( Player player, Vector3I[] marks, object tag ) {
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );

            if( !player.Info.Rank.AllowSecurityCircumvention ) {
                SecurityCheckResult buildCheck = playerWorld.BuildSecurity.CheckDetailed( player.Info );
                switch( buildCheck ) {
                    case SecurityCheckResult.BlackListed:
                        player.Message( "Cannot add zones to world {0}&S: You are barred from building here.",
                                        playerWorld.ClassyName );
                        return;
                    case SecurityCheckResult.RankTooLow:
                        player.Message( "Cannot add zones to world {0}&S: You are not allowed to build here.",
                                        playerWorld.ClassyName );
                        return;
                    //case SecurityCheckResult.RankTooHigh:
                }
            }

            Zone zone = (Zone)tag;
            var zones = player.WorldMap.Zones;
            lock( zones.SyncRoot ) {
                Zone dupeZone = zones.FindExact( zone.Name );
                if( dupeZone != null ) {
                    player.Message( "A zone named \"{0}\" has just been created by {1}",
                                    dupeZone.Name, dupeZone.CreatedBy );
                    return;
                }

                zone.Create( new BoundingBox( marks[0], marks[1] ), player.Info );

                player.Message( "Zone \"{0}\" created, {1} blocks total.",
                                zone.Name, zone.Bounds.Volume );
                Logger.Log( LogType.UserActivity,
                            "Player {0} created a new zone \"{1}\" containing {2} blocks.",
                            player.Name,
                            zone.Name,
                            zone.Bounds.Volume );

                zones.Add( zone );
            }
        }

        #endregion


        #region ZoneEdit

        static readonly CommandDescriptor CdZoneEdit = new CommandDescriptor {
            Name = "ZEdit",
            Category = CommandCategory.Zone,
            Permissions = new[] { Permission.ManageZones },
            Usage = "/ZEdit ZoneName [RankName] [+IncludedName] [-ExcludedName] [msg=CustomDenyMessage]",
            Help = "&HAllows editing the zone permissions after creation. " +
                   "You can change the rank restrictions, and include or exclude individual players."+
				   " The custom deny message must be the last pararameter since the rest of the command past the 'msg=' will be considered as the new message",
            Handler = ZoneEditHandler
        };

        static void ZoneEditHandler( Player player, Command cmd ) {
            bool changesWereMade = false;
            string zoneName = cmd.Next();
            if( zoneName == null ) {
                player.Message( "No zone name specified. See &H/Help ZEdit" );
                return;
            }

            Zone zone = player.WorldMap.Zones.Find( zoneName );
            if( zone == null ) {
                player.MessageNoZone( zoneName );
                return;
            }

            string name;
            while( (name = cmd.Next()) != null ) {
                if( name.StartsWith( "+" ) ) {
                    if( name.Length == 1 ) {
                        CdZoneEdit.PrintUsage( player );
                        break;
                    }
                    PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches( player, name.Substring( 1 ) );
                    if( info == null ) return;

                    // prevent players from whitelisting themselves to bypass protection
                    if( !player.Info.Rank.AllowSecurityCircumvention && player.Info == info ) {
                        if( !zone.Controller.Check( info ) ) {
                            player.Message( "You must be {0}+&S to add yourself to this zone's whitelist.",
                                            zone.Controller.MinRank.ClassyName );
                            continue;
                        }
                    }

                    switch( zone.Controller.Include( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is no longer excluded from zone {1}",
                                            info.ClassyName, zone.ClassyName );
                            changesWereMade = true;
                            break;
                        case PermissionOverride.None:
                            player.Message( "{0}&S is now included in zone {1}",
                                            info.ClassyName, zone.ClassyName );
                            changesWereMade = true;
                            break;
                        case PermissionOverride.Allow:
                            player.Message( "{0}&S is already included in zone {1}",
                                            info.ClassyName, zone.ClassyName );
                            break;
                    }

                } else if( name.StartsWith( "-" ) ) {
                    if( name.Length == 1 ) {
                        CdZoneEdit.PrintUsage( player );
                        break;
                    }
                    PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches( player, name.Substring( 1 ) );
                    if( info == null ) return;

                    switch( zone.Controller.Exclude( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is already excluded from zone {1}",
                                            info.ClassyName, zone.ClassyName );
                            break;
                        case PermissionOverride.None:
                            player.Message( "{0}&S is now excluded from zone {1}",
                                            info.ClassyName, zone.ClassyName );
                            changesWereMade = true;
                            break;
                        case PermissionOverride.Allow:
                            player.Message( "{0}&S is no longer included in zone {1}",
                                            info.ClassyName, zone.ClassyName );
                            changesWereMade = true;
                            break;
                    }

                } 
				else if (name.ToLower().StartsWith("msg="))
				{
					zone.Message = name.Substring(4) + " " + (cmd.NextAll() ?? "");
					changesWereMade = true;
                    player.Message("Zedit: Custom denied messaged changed to '" + zone.Message + "'");
					break;
				}
				else {
                    Rank minRank = RankManager.FindRank( name );

                    if( minRank != null ) {
                        // prevent players from lowering rank so bypass protection
                        if( !player.Info.Rank.AllowSecurityCircumvention &&
                            zone.Controller.MinRank > player.Info.Rank && minRank <= player.Info.Rank ) {
                            player.Message( "You are not allowed to lower the zone's rank." );
                            continue;
                        }

                        if( zone.Controller.MinRank != minRank ) {
                            zone.Controller.MinRank = minRank;
                            player.Message( "Permission for zone \"{0}\" changed to {1}+",
                                            zone.Name,
                                            minRank.ClassyName );
                            changesWereMade = true;
                        }
                    } else {
                        player.MessageNoRank( name );
                    }
                }
            }
			if (changesWereMade)
				zone.Edit(player.Info);
			else
				player.Message("No changes were made to the zone.");
        }

        #endregion ZoneEdit


        #region ZoneInfo

        static readonly CommandDescriptor CdZoneInfo = new CommandDescriptor {
            Name = "ZInfo",
            Aliases = new[] { "ZoneInfo" },
            Category = CommandCategory.Zone | CommandCategory.Info,
            Help = "Shows detailed information about a zone.",
            Usage = "/ZInfo ZoneName",
            UsableByFrozenPlayers = true,
            Handler = ZoneInfoHandler
        };

        static void ZoneInfoHandler( Player player, Command cmd ) {
            string zoneName = cmd.Next();
            if( zoneName == null ) {
                player.Message( "No zone name specified. See &H/Help ZInfo" );
                return;
            }

            Zone zone = player.WorldMap.Zones.Find( zoneName );
            if( zone == null ) {
                player.MessageNoZone( zoneName );
                return;
            }

            player.Message( "About zone \"{0}\": size {1} x {2} x {3}, contains {4} blocks, editable by {5}+.",
                            zone.Name,
                            zone.Bounds.Width, zone.Bounds.Length, zone.Bounds.Height,
                            zone.Bounds.Volume,
                            zone.Controller.MinRank.ClassyName );

            player.Message( "  Zone center is at ({0},{1},{2}).",
                            (zone.Bounds.XMin + zone.Bounds.XMax) / 2,
                            (zone.Bounds.YMin + zone.Bounds.YMax) / 2,
                            (zone.Bounds.ZMin + zone.Bounds.ZMax) / 2 );

            if( zone.CreatedBy != null ) {
                player.Message( "  Zone created by {0}&S on {1:MMM d} at {1:h:mm} ({2} ago).",
                                zone.CreatedByClassy,
                                zone.CreatedDate,
                                DateTime.UtcNow.Subtract( zone.CreatedDate ).ToMiniString() );
            }

            if( zone.EditedBy != null ) {
                player.Message( "  Zone last edited by {0}&S on {1:MMM d} at {1:h:mm} ({2}d {3}h ago).",
                zone.EditedByClassy,
                zone.EditedDate,
                DateTime.UtcNow.Subtract( zone.EditedDate ).Days,
                DateTime.UtcNow.Subtract( zone.EditedDate ).Hours );
            }

            PlayerExceptions zoneExceptions = zone.ExceptionList;

            if( zoneExceptions.Included.Length > 0 ) {
                player.Message( "  Zone whitelist includes: {0}",
                                zoneExceptions.Included.JoinToClassyString() );
            }

            if( zoneExceptions.Excluded.Length > 0 ) {
                player.Message( "  Zone blacklist excludes: {0}",
                                zoneExceptions.Excluded.JoinToClassyString() );
            }

			if (null != zone.Message)
				player.Message("  Zone has custom deny build message: " + zone.Message);
			else
				player.Message("  Zone has no custom deny build message");
		}

        #endregion


        #region ZoneList

        static readonly CommandDescriptor CdZoneList = new CommandDescriptor {
            Name = "Zones",
            Category = CommandCategory.Zone | CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/Zones [WorldName]",
            Help = "&HLists all zones defined on the current map/world.",
            Handler = ZoneListHandler
        };

        static void ZoneListHandler( Player player, Command cmd ) {
            World world = player.World;
            string worldName = cmd.Next();
            if( worldName != null ) {
                world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                if( world == null ) return;
                player.Message( "List of zones on {0}&S:",
                                world.ClassyName );

            } else if( world != null ) {
                player.Message( "List of zones on this world:" );

            } else {
                player.Message( "When used from console, &H/Zones&S command requires a world name." );
                return;
            }

            Map map = world.Map;
            if( map == null ) {
                if( !MapUtility.TryLoadHeader( world.MapFileName, out map ) ) {
                    player.Message( "&WERROR:Could not load mapfile for world {0}.",
                                    world.ClassyName );
                    return;
                }
            }

            Zone[] zones = map.Zones.Cache;
            if( zones.Length > 0 ) {
                foreach( Zone zone in zones ) {
                    player.Message( "   {0} ({1}&S) - {2} x {3} x {4}",
                                    zone.Name,
                                    zone.Controller.MinRank.ClassyName,
                                    zone.Bounds.Width,
                                    zone.Bounds.Length,
                                    zone.Bounds.Height );
                }
                player.Message( "   Type &H/ZInfo ZoneName&S for details." );
            } else {
                player.Message( "   No zones defined." );
            }
        }

        #endregion


        #region ZoneMark

        static readonly CommandDescriptor CdZoneMark = new CommandDescriptor {
            Name = "ZMark",
            Category = CommandCategory.Zone | CommandCategory.Building,
            Usage = "/ZMark ZoneName",
            Help = "&HUses zone boundaries to make a selection.",
            Handler = ZoneMarkHandler
        };

        static void ZoneMarkHandler( Player player, Command cmd ) {
            if( player.SelectionMarksExpected == 0 ) {
                player.MessageNow( "Cannot use ZMark - no selection in progress." );
            } else if( player.SelectionMarksExpected == 2 ) {
                string zoneName = cmd.Next();
                if( zoneName == null ) {
                    CdZoneMark.PrintUsage( player );
                    return;
                }

                Zone zone = player.WorldMap.Zones.Find( zoneName );
                if( zone == null ) {
                    player.MessageNoZone( zoneName );
                    return;
                }

                player.SelectionResetMarks();
                player.SelectionAddMark( zone.Bounds.MinVertex, false );
                player.SelectionAddMark( zone.Bounds.MaxVertex, true );
            } else {
                player.MessageNow( "ZMark can only be used for 2-block selection." );
            }
        }

        #endregion


        #region ZoneRemove

        static readonly CommandDescriptor CdZoneRemove = new CommandDescriptor {
            Name = "ZRemove",
            Aliases = new[] { "zdelete" },
            Category = CommandCategory.Zone,
            Permissions = new[] { Permission.ManageZones },
            Usage = "/ZRemove ZoneName",
            Help = "Removes a zone with the specified name from the map.",
            Handler = ZoneRemoveHandler
        };

        static void ZoneRemoveHandler( Player player, Command cmd ) {
            string zoneName = cmd.Next();
            if( zoneName == null ) {
                CdZoneRemove.PrintUsage( player );
                return;
            }

            ZoneCollection zones = player.WorldMap.Zones;
            Zone zone = zones.Find( zoneName );
            if( zone != null ) {
                if( !player.Info.Rank.AllowSecurityCircumvention ) {
                    switch( zone.Controller.CheckDetailed( player.Info ) ) {
                        case SecurityCheckResult.BlackListed:
                            player.Message( "You are not allowed to remove zone {0}: you are blacklisted.", zone.ClassyName );
                            return;
                        case SecurityCheckResult.RankTooLow:
                            player.Message( "You are not allowed to remove zone {0}.", zone.ClassyName );
                            return;
                    }
                }
                if( !cmd.IsConfirmed ) {
                    player.Confirm( cmd, "Remove zone {0}&S?", zone.ClassyName );
                    return;
                }

                if( zones.Remove( zone.Name ) ) {
                    player.Message( "Zone \"{0}\" removed.", zone.Name );
                }

            } else {
                player.MessageNoZone( zoneName );
            }
        }

        #endregion


        #region ZoneRename

        static readonly CommandDescriptor CdZoneRename = new CommandDescriptor {
            Name = "ZRename",
            Category = CommandCategory.Zone,
            Permissions = new[] { Permission.ManageZones },
            Help = "&HRenames a zone",
            Usage = "/ZRename OldName NewName",
            Handler = ZoneRenameHandler
        };

        static void ZoneRenameHandler( Player player, Command cmd ) {
            World playerWorld = player.World;
            if(playerWorld==null)PlayerOpException.ThrowNoWorld( player );

            // make sure that both parameters are given
            string oldName = cmd.Next();
            string newName = cmd.Next();
            if( oldName == null || newName == null ) {
                CdZoneRename.PrintUsage( player );
                return;
            }

            // make sure that the new name is valid
            if( !World.IsValidName( newName ) ) {
                player.Message( "\"{0}\" is not a valid zone name", newName );
                return;
            }

            // find the old zone
            var zones = player.WorldMap.Zones;
            Zone oldZone = zones.Find( oldName );
            if( oldZone == null ) {
                player.MessageNoZone( oldName );
                return;
            }

            // Check if a zone with "newName" name already exists
            Zone newZone = zones.FindExact( newName );
            if( newZone != null && newZone != oldZone ) {
                player.Message( "A zone with the name \"{0}\" already exists.", newName );
                return;
            }

            // check if any change is needed
            string fullOldName = oldZone.Name;
            if( fullOldName == newName ) {
                player.Message( "The zone is already named \"{0}\"", fullOldName );
                return;
            }

            // actually rename the zone
            zones.Rename( oldZone, newName );

            // announce the rename
            playerWorld.Players.Message( "&SZone \"{0}\" was renamed to \"{1}&S\" by {2}",
                                         fullOldName, oldZone.ClassyName, player.ClassyName );
            Logger.Log( LogType.UserActivity,
                        "Player {0} renamed zone \"{1}\" to \"{2}\" on world {3}",
                        player.Name, fullOldName, newName, playerWorld.Name );
        }

        #endregion


        #region ZoneTest

        static readonly CommandDescriptor CdZoneTest = new CommandDescriptor {
            Name = "ZTest",
            Category = CommandCategory.Zone | CommandCategory.Info,
            RepeatableSelection = true,
            Help = "&HAllows to test exactly which zones affect a particular block. Can be used to find and resolve zone overlaps.",
            Handler = ZoneTestHandler
        };

        static void ZoneTestHandler( Player player, Command cmd ) {
            player.SelectionStart( 1, ZoneTestCallback, null );
            player.Message( "Click the block that you would like to test." );
        }

        static void ZoneTestCallback( Player player, Vector3I[] marks, object tag ) {
            Zone[] allowed, denied;
            if( player.WorldMap.Zones.CheckDetailed( marks[0], player, out allowed, out denied ) ) {
                foreach( Zone zone in allowed ) {
                    SecurityCheckResult status = zone.Controller.CheckDetailed( player.Info );
                    player.Message( "> Zone {0}&S: {1}{2}", zone.ClassyName, Color.Lime, status );
                }
                foreach( Zone zone in denied ) {
                    SecurityCheckResult status = zone.Controller.CheckDetailed( player.Info );
                    player.Message( "> Zone {0}&S: {1}{2}", zone.ClassyName, Color.Red, status );
                }
            } else {
                player.Message( "No zones affect this block." );
            }
        }

        #endregion
    }
}