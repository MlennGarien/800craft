// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using fCraft.MapConversion;
using fCraft.Events;
using System.Collections.Generic;

namespace fCraft {
    /// <summary> Contains commands related to zone management. </summary>
    static class ZoneCommands {

        internal static void Init()
        {
            CommandManager.RegisterCommand(CdZoneAdd);
            CommandManager.RegisterCommand(CdZoneEdit);
            CommandManager.RegisterCommand(CdZoneInfo);
            CommandManager.RegisterCommand(CdZoneList);
            CommandManager.RegisterCommand(CdZoneMark);
            CommandManager.RegisterCommand(CdZoneRemove);
            CommandManager.RegisterCommand(CdZoneRename);
            CommandManager.RegisterCommand(CdZoneTest);

            //CommandManager.RegisterCommand(cdDoor);
            //CommandManager.RegisterCommand(cdDoorRemove);

           //Player.Clicked += PlayerClickedDoor;
            Player.Clicked += PlayerClickedGuest;
            Player.Clicked += PlayerClickedBuilder;
            Player.Clicked += PlayerClickedAdvbuilder;
            Player.Clicked += PlayerClickedProbuilder;
            Player.Clicked += PlayerClickedOpLounge;
            Player.Clicked += PlayerClickedEngineer;
            Player.Clicked += PlayerClickedModerator;
        }
            
        
        public static void PlayerClickedDoor(object sender, PlayerClickedEventArgs e)
        {

            Zone[] allowed, denied;
            if (e.Player.World.Map.Zones.CheckDetailed(e.Coords, e.Player, out allowed, out denied))
            {
                foreach (Zone door in allowed)
                {
                    if (door.Name.Contains("door"))
                    {
                        
                        CutDoor(door, e.Player);
                        break;

                    }
                }

            }
        }

        public static void CutDoor(Zone zone, Player player)
        {

            int sx = zone.Bounds.XMin;
            int ex = zone.Bounds.XMax;
            int sy = zone.Bounds.YMin;
            int ey = zone.Bounds.YMax;
            int sh = zone.Bounds.ZMin;
            int eh = zone.Bounds.ZMax;

            string[] DL = new string[5];

            DL[0] = ""; DL[1] = ""; DL[2] = ""; DL[3] = ""; DL[4] = player.Name;

            //for each block in selection, add x, y, z coords and block type to string array
            for (int i = sx; i <= ex; i++)
            {
                for (int j = sy; j <= ey; j++)
                {
                    for (int k = sh; k <= eh; k++)
                    {
                        DL[0] += i.ToString() + " "; //x
                        DL[1] += j.ToString() + " "; //y
                        DL[2] += k.ToString() + " "; //z
                        DL[3] += player.World.Map.GetBlock(i, j, k) + " "; //block

                        //clear the door
                        player.World.Map.QueueUpdate(
                            new BlockUpdate(null, (short)i, (short)j, (short)k, Block.Air));

                    }
                }
            }

            if (!DoorLookUp.ContainsKey(zone.Name))
            {
                DoorLookUp.Add(zone.Name, DL);
            }
            else
            {
                DoorLookUp[zone.Name] = DL;
            }

            Scheduler.NewTask(DoorTimer_Elapsed).RunOnce(zone.Name, DoorCloseTimer); //timer to re-close door
        }
        static readonly TimeSpan DoorCloseTimer = TimeSpan.FromMilliseconds(1500); // time that door will stay open
        public static Dictionary<string, string[]> DoorLookUp = new Dictionary<string, string[]>();
       
        static readonly CommandDescriptor cdDoorRemove = new CommandDescriptor
        {
            Name = "Removedoor",
            Aliases = new[] { "rd" },
            Category = CommandCategory.Zone,
            Permissions = new[] { Permission.Chat },
            Help = "Removes door.",
            Handler = DoorRemove
        };

        static void DoorRemove(Player player, Command cmd)
        {
            if (player.World.Map.Zones.Find(player.Info.Name + "door") != null)
            {
                player.World.Map.Zones.Remove(player.Info.Name + "door");
                DoorLookUp.Remove(player.Info.Name + "door");
                player.Message("Door removed.");
            }

        }

        static readonly CommandDescriptor cdDoor = new CommandDescriptor
        {
            Name = "Door",
            Category = CommandCategory.Zone,
            Permissions = new[] { Permission.Chat },
            Help = "First create a door with blocks, then use /door left click 2 corners. Left click to open doors.",
            Handler = Door
        };

        static void Door(Player player, Command cmd)
        {
            if (player.World.Map.Zones.Find(player.Info.Name + "door") != null)
            {
                player.Message("One door per person.");
                return;
            }



            Zone door = new Zone();
            door.Name = player.Info.Name + "door";

            player.SelectionStart(2, DoorAddCallback, door, cdDoor.Permissions);
            player.Message("Door: Left click two corners of a door you have made with blocks.");

        }

        static void DoorTimer_Elapsed(SchedulerTask task)
        {
            string door = (string)task.UserState;
            string[] doorVars = DoorLookUp[door];
            string x, y, z, b;

            Player player = Server.FindPlayerExact(doorVars[4]);
            

            //parse values from DoorLookUp
            x = doorVars[0].TrimEnd(' '); y = doorVars[1].TrimEnd(' '); z = doorVars[2].TrimEnd(' '); b = doorVars[3].TrimEnd(' ');
            string[] xs = x.Split(' '), ys = y.Split(' '), zs = z.Split(' '), bs = b.Split(' ');

            int i = 0;

            foreach (string arg in xs)
            {
                player.World.Map.QueueUpdate(new BlockUpdate(null, (short)Int32.Parse(xs[i]), (short)Int32.Parse(ys[i]), (short)Int32.Parse(zs[i]), (Block)Byte.Parse(bs[i])));
                i++;
            }


        }
        public static void PlayerClickedGuest(object sender, PlayerClickedEventArgs e)
        {
            
            Zone[] allowed, denied;
            if (e.Player.World.Map.Zones.CheckDetailed(e.Coords, e.Player, out allowed, out denied))
            {
                foreach (Zone zone in denied)
                {
                    if (zone.Name.Contains("guest"))
                    {
                        GuestZone(zone, e.Player);
                        

                        break;

                    }
                }

            }
        }

        public static void GuestZone(Zone zone, Player player)
        {

            JoinHandler(player, new Command("/goto guest"));

        }


        public static void PlayerClickedBuilder(object sender, PlayerClickedEventArgs e)
        {

            Zone[] allowed, denied;
            if (e.Player.World.Map.Zones.CheckDetailed(e.Coords, e.Player, out allowed, out denied))
            {
                foreach (Zone zone in denied)
                {
                    if (zone.Name.Contains("builder"))
                    {
                        

                        BuilderZone(zone, e.Player);

                    }
                }

            }
        }

        public static void BuilderZone(Zone zone, Player player)
        {

            JoinHandler(player, new Command("/goto builder"));
        }

        static void DoorAddCallback(Player player, Vector3I[] marks, object tag)
        {
            if (player.World == null) PlayerOpException.ThrowNoWorld(player);
            if (!player.Info.Rank.AllowSecurityCircumvention)
            {
                SecurityCheckResult buildCheck = player.World.BuildSecurity.CheckDetailed(player.Info);
                switch (buildCheck)
                {
                    case SecurityCheckResult.BlackListed:
                        player.Message("Cannot add doors to world {0}&S: You are barred from building here.",
                                        player.World.ClassyName);
                        return;
                    case SecurityCheckResult.RankTooLow:
                        player.Message("Cannot add doors to world {0}&S: You are not allowed to build here.",
                                        player.World.ClassyName);
                        return;
                    //case SecurityCheckResult.RankTooHigh:
                }
            }
            int maxBlocks = 20;  //max door size, change as needed... should add config key

            int sx = Math.Min(marks[0].X, marks[1].X);
            int ex = Math.Max(marks[0].X, marks[1].X);
            int sy = Math.Min(marks[0].Y, marks[1].Y);
            int ey = Math.Max(marks[0].Y, marks[1].Y);
            int sh = Math.Min(marks[0].Z, marks[1].Z);
            int eh = Math.Max(marks[0].Z, marks[1].Z);

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1);
            if (volume > maxBlocks)
            {
                player.Message("Doors are only allowed to be {0} blocks", maxBlocks);
                return;
            }
            Zone door = (Zone)tag;
            var doors = player.World.LoadMap().Zones;
            lock (doors.SyncRoot)
            {
                Zone dupeZone = doors.FindExact(door.Name);
                if (dupeZone != null)
                {
                    player.Message("A door named \"{0}\" has just been created by {1}",
                                    dupeZone.Name, dupeZone.CreatedBy);
                    return;
                }

                door.Create(new BoundingBox(marks[0], marks[1]), player.Info);

                player.Message("Door \"{0}\" created, {1} blocks total.",
                                door.Name,
                                door.Bounds.Volume);
                Logger.Log(LogType.UserActivity, "Player {0} created a new door \"{1}\" containing {2} blocks.",
                            player.Name,
                            door.Name,
                            door.Bounds.Volume);

                doors.Add(door);
            }
        }

        public static void PlayerClickedAdvbuilder(object sender, PlayerClickedEventArgs e)
        {

            Zone[] allowed, denied;
            if (e.Player.World.Map.Zones.CheckDetailed(e.Coords, e.Player, out allowed, out denied))
            {
                foreach (Zone zone in denied)
                {
                    if (zone.Name.Contains("advportal"))
                    {
                        
                        AdvBuilderZone(zone, e.Player);

                    }
                }

            }
        }

        public static void AdvBuilderZone(Zone zone, Player player)
        {

            JoinHandler(player, new Command("/goto advbuilder"));
        }

        public static void PlayerClickedProbuilder(object sender, PlayerClickedEventArgs e)
        {

            Zone[] allowed, denied;
            if (e.Player.World.Map.Zones.CheckDetailed(e.Coords, e.Player, out allowed, out denied))
            {
                foreach (Zone zone in denied)
                {
                    if (zone.Name.Contains("proportal"))
                    {
                        
                        ProZone(zone, e.Player);

                    }
                }

            }
        }

        public static void ProZone(Zone zone, Player player)
        {

            JoinHandler(player, new Command("/goto probuilder"));
        }

        public static void PlayerClickedEngineer(object sender, PlayerClickedEventArgs e)
        {

            Zone[] allowed, denied;
            if (e.Player.World.Map.Zones.CheckDetailed(e.Coords, e.Player, out allowed, out denied))
            {
                foreach (Zone zone in denied)
                {
                    if (zone.Name.Contains("engineer"))
                    {
                        EngZone(zone, e.Player);
                        
                        break;


                    }
                }

            }
        }

        public static void EngZone(Zone zone, Player player)
        {

            JoinHandler(player, new Command("/goto engineer"));
        }

        public static void PlayerClickedOpLounge(object sender, PlayerClickedEventArgs e)
        {

            Zone[] allowed, denied;
            if (e.Player.World.Map.Zones.CheckDetailed(e.Coords, e.Player, out allowed, out denied))
            {
                foreach (Zone zone in denied)
                {
                    if (zone.Name.Contains("operator"))
                    {
                        
                        OpZone(zone, e.Player);

                    }
                }

            }
        }

        public static void OpZone(Zone zone, Player player)
        {

            JoinHandler(player, new Command("/goto operator"));
        }

        public static void PlayerClickedModerator(object sender, PlayerClickedEventArgs e)
        {

            Zone[] allowed, denied;
            if (e.Player.World.Map.Zones.CheckDetailed(e.Coords, e.Player, out allowed, out denied))
            {
                foreach (Zone zone in denied)
                {
                    if (zone.Name.Contains("moderator"))
                    {
                       
                        ModZone(zone, e.Player);

                    }
                }

            }
        }

        public static void ModZone(Zone zone, Player player)
        {

            JoinHandler(player, new Command("/goto moderator"));
        }
        

        #region joinhandler
        static void JoinHandler(Player player, Command cmd)
        {
            string worldName = cmd.Next();


            World[] worlds = WorldManager.FindWorlds(player, worldName);

            if (worlds.Length > 1)
            {
                player.MessageManyMatches("world", worlds);

            }
            else if (worlds.Length == 1)
            {
                World world = worlds[0];
                switch (world.AccessSecurity.CheckDetailed(player.Info))
                {
                    case SecurityCheckResult.Allowed:
                    case SecurityCheckResult.WhiteListed:
                        if (world.IsFull)
                        {
                            player.Message("Cannot join {0}&S: world is full.", world.ClassyName);
                            return;
                        }
                        player.StopSpectating();
                        if (!player.JoinWorldNow(world, true, WorldChangeReason.ManualJoin))
                        {
                            player.Message("ERROR: Failed to join world. See log for details.");
                        }
                        break;
                    case SecurityCheckResult.BlackListed:
                        player.Message("Cannot join world {0}&S: you are blacklisted",
                                        world.ClassyName, world.AccessSecurity.MinRank.ClassyName);
                        break;
                    case SecurityCheckResult.RankTooLow:
                        player.Message("Cannot join world {0}&S: must be {1}+",
                                        world.ClassyName, world.AccessSecurity.MinRank.ClassyName);
                        break;
                }

            }
            else
            {
                // no worlds found - see if player meant to type in "/join" and not "/tp"
                Player[] players = Server.FindPlayers(player, worldName, true);
                if (players.Length == 1)
                {
                    player.StopSpectating();
                    player.ParseMessage("/tp " + players[0].Name, false);
                }
                else
                {
                    player.MessageNoWorld(worldName);
                }
            }
        }

        #endregion


        #region ZoneAdd

        static readonly CommandDescriptor CdZoneAdd = new CommandDescriptor {
            Name = "ZAdd",
            Category = CommandCategory.Zone,
            Aliases = new[] { "zone" },
            Permissions = new[] { Permission.ManageZones },
            Usage = "/ZAdd ZoneName RankName",
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

            if (givenZoneName.Contains("door"))
            {
                player.Message("&SA Zone name cannot contain 'door'");
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
                newZone.Name = givenZoneName;
                for( int i = 2; zoneCollection.Contains( newZone.Name ); i++ ) {
                    newZone.Name = givenZoneName + i;
                }

                newZone.Controller.MinRank = info.Rank.NextRankUp ?? info.Rank;
                newZone.Controller.Include( info );
                player.Message( "Zone: Creating a {0}+&S zone for player {1}&S. Place a block or type /Mark to use your location.",
                                newZone.Controller.MinRank.ClassyName, info.ClassyName );
                player.SelectionStart( 2, ZoneAddCallback, newZone, CdZoneAdd.Permissions );

            } else {
                // Adding an ordinary, rank-restricted zone.
                if( !World.IsValidName( givenZoneName ) ) {
                    player.Message( "\"{0}\" is not a valid zone name", givenZoneName );
                    return;
                }

                if( zoneCollection.Contains( givenZoneName ) ) {
                    player.Message( "A zone with this name already exists. Use &Z/ZEdit&S to edit." );
                    return;
                }

                newZone.Name = givenZoneName;

                string rankName = cmd.Next();
                if( rankName == null ) {
                    player.Message( "No rank was specified. See &Z/Help zone" );
                    return;
                }
                Rank minRank = RankManager.FindRank( rankName );

                if( minRank != null ) {
                    string name;
                    while( (name = cmd.Next()) != null ) {

                        if( name.Length == 0 ) continue;

                        PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches( player, name.Substring( 1 ) );
                        if( info == null ) return;

                        if( name.StartsWith( "+" ) ) {
                            newZone.Controller.Include( info );
                        } else if( name.StartsWith( "-" ) ) {
                            newZone.Controller.Exclude( info );
                        }
                    }

                    newZone.Controller.MinRank = minRank;
                    player.SelectionStart( 2, ZoneAddCallback, newZone, CdZoneAdd.Permissions );
                    player.Message( "Zone: Place a block or type &Z/Mark&S to use your location." );

                } else {
                    player.MessageNoRank( rankName );
                }
            }
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
            Usage = "/ZEdit ZoneName [RankName] [+IncludedName] [-ExcludedName]",
            Help = "Allows editing the zone permissions after creation. " +
                   "You can change the rank restrictions, and include or exclude individual players.",
            Handler = ZoneEditHandler
        };

        static void ZoneEditHandler( Player player, Command cmd ) {
            bool changesWereMade = false;
            string zoneName = cmd.Next();
            if( zoneName == null ) {
                player.Message( "No zone name specified. See &Z/Help ZEdit" );
                return;
            }

            Zone zone = player.WorldMap.Zones.Find( zoneName );
            if( zone == null ) {
                player.MessageNoZone( zoneName );
                return;
            }

            string name;
            while( (name = cmd.Next()) != null ) {
                if( name.Length < 2 ) continue;

                if( name.StartsWith( "+" ) ) {
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

                } else {
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

                if( changesWereMade ) {
                    zone.Edit( player.Info );
                } else {
                    player.Message( "No changes were made to the zone." );
                }
            }
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
                player.Message( "No zone name specified. See &Z/Help ZInfo" );
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
                                zone.CreatedBy.ClassyName,
                                zone.CreatedDate,
                                DateTime.UtcNow.Subtract( zone.CreatedDate ).ToMiniString() );
            }

            if( zone.EditedBy != null ) {
                player.Message( "  Zone last edited by {0}&S on {1:MMM d} at {1:h:mm} ({2}d {3}h ago).",
                zone.EditedBy.ClassyName,
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
        }

        #endregion


        #region ZoneList

        static readonly CommandDescriptor CdZoneList = new CommandDescriptor {
            Name = "Zones",
            Category = CommandCategory.Zone | CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/Zones [WorldName]",
            Help = "Lists all zones defined on the current map/world.",
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
                player.Message( "When used from console, &Z/Zones&S command requires a world name." );
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
                player.Message( "   Type &Z/ZInfo ZoneName&S for details." );
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
            Help = "Uses zone boundaries to make a selection.",
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
                    player.Confirm( cmd, "You are about to remove zone {0}&S.", zone.ClassyName );
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
            Help = "Renames a zone",
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
            Help = "Allows to test exactly which zones affect a particular block. Can be used to find and resolve zone overlaps.",
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