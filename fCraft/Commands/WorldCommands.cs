// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using fCraft.MapConversion;
using JetBrains.Annotations;
using fCraft.Drawing;
using fCraft.Portals;
using ServiceStack.Text;
using System.Text;

namespace fCraft {
    /// <summary> Contains commands related to world management. </summary>
    static class WorldCommands {
        const int WorldNamesPerPage = 30;

        internal static void Init() {
            CommandManager.RegisterCommand( CdBlockDB );
            CommandManager.RegisterCommand( CdBlockInfo );

            CommandManager.RegisterCommand( CdEnv );

            CdGenerate.Help = "Generates a new map. If no dimensions are given, uses current world's dimensions. " +
                              "If no filename is given, loads generated world into current world.\n" +
                              "Available themes: Grass, " + Enum.GetNames( typeof( MapGenTheme ) ).JoinToString() + '\n' +
                              "Available terrain types: Empty, Ocean, " + Enum.GetNames( typeof( MapGenTemplate ) ).JoinToString() + '\n' +
                              "Note: You do not need to specify a theme with \"Empty\" and \"Ocean\" templates.";
            CommandManager.RegisterCommand( CdGenerate );

            CommandManager.RegisterCommand( CdJoin );

            CommandManager.RegisterCommand( CdWorldLock );
            CommandManager.RegisterCommand( CdWorldUnlock );

            CommandManager.RegisterCommand( CdSpawn );

            CommandManager.RegisterCommand( CdWorlds );
            CommandManager.RegisterCommand( CdWorldAccess );
            CommandManager.RegisterCommand( CdWorldBuild );
            CommandManager.RegisterCommand( CdWorldFlush );

            CommandManager.RegisterCommand( CdWorldHide );
            CommandManager.RegisterCommand( CdWorldUnhide );

            CommandManager.RegisterCommand( CdWorldInfo );
            CommandManager.RegisterCommand( CdWorldLoad );
            CommandManager.RegisterCommand( CdWorldMain );
            CommandManager.RegisterCommand( CdWorldRename );
            CommandManager.RegisterCommand( CdWorldSave );
            CommandManager.RegisterCommand( CdWorldUnload );

            CommandManager.RegisterCommand(CdRealm);
            CommandManager.RegisterCommand(CdGuestwipe);
            CommandManager.RegisterCommand(CdRankHide);
            CommandManager.RegisterCommand(CdPortal);
            CommandManager.RegisterCommand(CdWorldSearch);
            SchedulerTask TimeCheckR = Scheduler.NewTask(TimeCheck).RunForever(TimeSpan.FromSeconds(120));
            //CommandManager.RegisterCommand(CdPhysics); //coming soon
        }
        #region 800Craft
        static readonly CommandDescriptor CdPhysics = new CommandDescriptor
        {
            Name = "Physics",
            Category = CommandCategory.World,
            Permissions = new Permission[] { Permission.Physics },
            IsConsoleSafe = false,
            Usage = "/Physics <TNT | Fireworks | Water | Plant | Grass | All | Unflood> <On / Off>",
            Help = "Enables / disables a type of Physics for the current world. Physics may use more server resources.",
            HelpSections = new Dictionary<string, string>() {
                { "tnt",     "&H/Physics tnt on/off \n&S" +
                                "Turns TNT exploding physics on / off in the current world"},
                { "fireworks",     "&H/Physics fireworks on/off \n&S" +
                                "Turns firework physics on / off in the current world"},
                { "water",       "&H/Physics water on/off \n&S" +
                                "Turns water physics on / off in the current world"},
                { "plant",       "&H/Physics plant on/off \n&S" +
                                "Turns plant physics on / off in the current world"},
                { "sand",       "&H/Physics sand on/off \n&S" +
                                "Turns sand and gravel physics on / off in the current world"},
                { "grass",       "&H/Physics grass on/off \n&S" +
                                "Turns grass regrowing physics on / off in the current world"},
                { "all",     "&H/Physics all on/off \n&S" +
                                "Turns all physics on / off in the current world"},
            },
            Handler = PhysicsHandler
        };

        private static void PhysicsHandler(Player player, Command cmd)
        {
            string option = cmd.Next();
            World world = player.World;
            if (option == null)
            {
                CdPhysics.PrintUsage(player);
                return;
            }
            switch (option.ToLower())
            {
                case "tnt":
                    if (world.tntPhysics)
                    {
                        world.tntPhysics = false;
                        Server.Players.Message("{0}&S turned TNT Physics off for {1}", player.ClassyName, world.ClassyName);
                        Logger.Log(LogType.SystemActivity, "{0} turned TNT Physics off for {1}", player.Name, world.Name);
                    }
                    else
                    {
                        world.tntPhysics = true;
                        Server.Players.Message("{0}&S turned TNT Physics on for {1}", player.ClassyName, world.ClassyName);
                        Logger.Log(LogType.SystemActivity, "{0} turned TNT Physics on for {1}", player.Name, world.Name);
                    }
                    break;
                case "grass":
                    if (world.grassPhysics)
                    {
                        world.grassPhysics = false;
                        Server.Players.Message("{0}&S turned Grass Physics off for {1}", player.ClassyName, world.ClassyName);
                        Logger.Log(LogType.SystemActivity, "{0} turned Grass Physics off for {1}", player.Name, world.Name);
                    }
                    else
                    {
                        world.grassPhysics = true;
                        Server.Players.Message("{0}&S turned Grass Physics on for {1}", player.ClassyName, world.ClassyName);
                        Logger.Log(LogType.SystemActivity, "{0} turned Grass Physics on for {1}", player.Name, world.Name);
                    }
                    break;
                case "fireworks":
                case "firework":
                    if (world.fireworkPhysics)
                    {
                        world.fireworkPhysics = false;
                        Server.Players.Message("{0}&S turned Firework Physics off for {1}", player.ClassyName, world.ClassyName);
                        Logger.Log(LogType.SystemActivity, "{0} turned Firework Physics off for {1}", player.Name, world.Name);
                    }
                    else
                    {
                        world.fireworkPhysics = true;
                        Server.Players.Message("{0}&S turned Firework Physics on for {1}", player.ClassyName, world.ClassyName);
                        Logger.Log(LogType.SystemActivity, "{0} turned Firework Physics on for {1}", player.Name, world.Name);
                    }
                    break;
                case "sand":
                    if (world.sandPhysics)
                    {
                        world.sandPhysics = false;
                        Server.Players.Message("{0}&S turned Sand Physics off for {1}", player.ClassyName, world.ClassyName);
                        Logger.Log(LogType.SystemActivity, "{0} turned Sand Physics off for {1}", player.Name, world.Name);
                    }
                    else
                    {
                        world.sandPhysics = true;
                        Server.Players.Message("{0}&S turned Sand Physics on for {1}", player.ClassyName, world.ClassyName);
                        Logger.Log(LogType.SystemActivity, "{0} turned Sand Physics on for {1}", player.Name, world.Name);
                    }
                    break;
                case "water":
                    if (world.waterPhysics)
                    {
                        world.waterPhysics = false;
                        Server.Players.Message("{0}&S turned Water Physics off for {1}", player.ClassyName, world.ClassyName);
                        Logger.Log(LogType.SystemActivity, "{0} turned Water Physics off for {1}", player.Name, world.Name);
                    }
                    else
                    {
                        world.waterPhysics = true;
                        Server.Players.Message("{0}&S turned Water Physics on for {1}", player.ClassyName, world.ClassyName);
                        Logger.Log(LogType.SystemActivity, "{0} turned Water Physics on for {1}", player.Name, world.Name);
                    }
                    break;
                case "plant":
                    if (world.plantPhysics)
                    {
                        world.plantPhysics = false;
                        Server.Players.Message("{0}&S turned Plant Physics off for {1}", player.ClassyName, world.ClassyName);
                        Logger.Log(LogType.SystemActivity, "{0} turned Plant Physics off for {1}", player.Name, world.Name);
                    }
                    else
                    {
                        world.plantPhysics = true;
                        Server.Players.Message("{0}&S turned Plant Physics on for {1}", player.ClassyName, world.ClassyName);
                        Logger.Log(LogType.SystemActivity, "{0} turned Plant Physics on for {1}", player.Name, world.Name);
                    }
                    break;
                case "all":
                    string nextOp = cmd.Next();
                    if (nextOp == null)
                    {
                        CdPhysics.PrintUsage(player);
                        return;
                    }

                    if (nextOp.ToLower() == "on")
                    {
                        world.tntPhysics = true;
                        world.sandPhysics = true;
                        world.fireworkPhysics = true;
                        world.waterPhysics = true;
                        world.plantPhysics = true;
                        world.grassPhysics = true;
                        Server.Players.Message("{0}&S turned ALL Physics on for {1}", player.ClassyName, world.ClassyName);
                        Logger.Log(LogType.SystemActivity, "{0} turned ALL Physics on for {1}", player.Name, world.Name);
                    }

                    else if (nextOp.ToLower() == "off")
                    {
                        world.tntPhysics = false;
                        world.fireworkPhysics = false;
                        world.sandPhysics = false;
                        world.waterPhysics = false;
                        world.plantPhysics = false;
                        world.grassPhysics = false;
                        Server.Players.Message("{0}&S turned ALL Physics off for {1}", player.ClassyName, world.ClassyName);
                        Logger.Log(LogType.SystemActivity, "{0} turned ALL Physics off for {1}", player.Name, world.Name);
                    }
                    break;
                
                default: CdPhysics.PrintUsage(player);
                    break;
            }
        }

        #region portals

        static readonly CommandDescriptor CdPortal = new CommandDescriptor
        {
            Name = "portal",
            Category = CommandCategory.World,
            Permissions = new Permission[] { Permission.UsePortal },
            IsConsoleSafe = false,
            Usage = "/portal [create | remove | info | list | enable | disable ]",
            Help = "Controls portals, options are: create, remove, list, info, enable, disable\n&S" +
                   "See &H/Help portal <option>&S for details about each option.",
            HelpSections = new Dictionary<string, string>() {
                { "create",     "&H/portal create Guest\n&S" +
                                "Creates a basic water portal to world Guest.\n&S" +
                                "&H/portal create Guest lava test\n&S" +
                                "Creates a lava portal with name 'test' to world Guest."},
                { "remove",     "&H/portal remove Portal1\n&S" +
                                "Removes portal with name 'Portal1'."},
                { "list",       "&H/portal list\n&S" +
                                "Gives you a list of portals in the current world."},
                { "info",       "&H/portal info Portal1\n&S" +
                                "Gives you information of portal with name 'Portal1'."},
                { "enable",     "&H/portal enable\n&S" +
                                "Enables the use of portals, this is player specific."},
                { "disable",     "&H/portal disable\n&S" +
                                "Disables the use of portals, this is player specific."},
            },
            Handler = PortalH
        };

        private static void PortalH(Player player, Command command)
        {
            try
            {
                String option = command.Next();

                if (option == null)
                {
                    CdPortal.PrintUsage(player);
                }
                else if (option.ToLower().Equals("create"))
                {
                    if (player.Can(Permission.ManagePortal))
                    {
                        string world = command.Next();

                        if (world != null && WorldManager.FindWorldExact(world) != null)
                        {
                            DrawOperation operation = new CuboidDrawOperation(player);
                            NormalBrush brush = new NormalBrush(Block.Water, Block.Water);

                            string blockTypeOrName = command.Next();

                            if (blockTypeOrName != null && blockTypeOrName.ToLower().Equals("lava"))
                            {
                                brush = new NormalBrush(Block.Lava, Block.Lava);
                            }
                            else if (blockTypeOrName != null && !blockTypeOrName.ToLower().Equals("water"))
                            {
                                player.Message("Invalid block, choose between water or lava.");
                                return;
                            }

                            string portalName = command.Next();

                            if (portalName == null)
                            {
                                player.PortalName = null;
                            }
                            else
                            {
                                if (!Portal.DoesNameExist(player.World, portalName))
                                {
                                    player.PortalName = portalName;
                                }
                                else
                                {
                                    player.Message("A portal with name {0} already exists in this world.", portalName);
                                    return;
                                }
                            }

                            operation.Brush = brush;
                            player.PortalWorld = world;


                            player.SelectionStart(operation.ExpectedMarks, PortalCreateCallback, operation, Permission.Draw);
                            player.Message("Click {0} blocks or use &H/Mark&S to mark the area of the portal.", operation.ExpectedMarks);
                        }
                        else
                        {
                            if (world == null)
                            {
                                player.Message("No world specified.");
                            }
                            else
                            {
                                player.MessageNoWorld(world);
                            }
                        }
                    }
                    else
                    {
                        player.MessageNoAccess(Permission.ManagePortal);
                    }
                }
                else if (option.ToLower().Equals("remove"))
                {
                    if (player.Can(Permission.ManagePortal))
                    {
                        string portalName = command.Next();

                        if (portalName == null)
                        {
                            player.Message("No portal name specified.");
                        }
                        else
                        {
                            if (player.World.Portals != null && player.World.Portals.Count > 0)
                            {
                                bool found = false;
                                Portal portalFound = null;

                                lock (player.World.Portals.SyncRoot)
                                {
                                    foreach (Portal portal in player.World.Portals)
                                    {
                                        if (portal.Name.Equals(portalName))
                                        {
                                            portalFound = portal;
                                            found = true;
                                            break;
                                        }
                                    }

                                    if (!found)
                                    {
                                        player.Message("Could not find portal by name {0}.", portalName);
                                    }
                                    else
                                    {
                                        portalFound.Remove(player);
                                        player.Message("Portal was removed.");
                                    }
                                }
                            }
                            else
                            {
                                player.Message("Could not find portal as this world doesn't contain a portal.");
                            }
                        }
                    }
                    else
                    {
                        player.MessageNoAccess(Permission.ManagePortal);
                    }
                }
                else if (option.ToLower().Equals("info"))
                {
                    string portalName = command.Next();

                    if (portalName == null)
                    {
                        player.Message("No portal name specified.");
                    }
                    else
                    {
                        if (player.World.Portals != null && player.World.Portals.Count > 0)
                        {
                            bool found = false;

                            lock (player.World.Portals.SyncRoot)
                            {
                                foreach (Portal portal in player.World.Portals)
                                {
                                    if (portal.Name.Equals(portalName))
                                    {
                                        World portalWorld = WorldManager.FindWorldExact(portal.World);
                                        player.Message("Portal {0}&S was created by {1}&S at {2} and teleports to world {3}&S.",
                                            portal.Name, PlayerDB.FindPlayerInfoExact(portal.Creator).ClassyName, portal.Created, portalWorld.ClassyName);
                                        found = true;
                                    }
                                }
                            }

                            if (!found)
                            {
                                player.Message("Could not find portal by name {0}.", portalName);
                            }
                        }
                        else
                        {
                            player.Message("Could not find portal as this world doesn't contain a portal.");
                        }
                    }
                }
                else if (option.ToLower().Equals("list"))
                {
                    if (player.World.Portals == null || player.World.Portals.Count == 0)
                    {
                        player.Message("There are no portals in {0}&S.", player.World.ClassyName);
                    }
                    else
                    {
                        String[] portalNames = new String[player.World.Portals.Count];
                        StringBuilder output = new StringBuilder("There are " + player.World.Portals.Count + " portals in " + player.World.ClassyName + "&S: ");

                        for (int i = 0; i < player.World.Portals.Count; i++)
                        {
                            portalNames[i] = ((Portal)player.World.Portals[i]).Name;
                        }

                        output.Append(portalNames.JoinToString(", "));

                        player.Message(output.ToString());
                    }
                }
                else if (option.ToLower().Equals("enable"))
                {
                    player.PortalsEnabled = true;
                    player.Message("You enabled the use of portals.");
                }
                else if (option.ToLower().Equals("disable"))
                {
                    player.PortalsEnabled = false;
                    player.Message("You disabled the use of portals, type /portal enable to re-enable portals.");
                }
                else
                {
                    CdPortal.PrintUsage(player);
                }
            }
            catch (PortalException ex)
            {
                player.Message(ex.Message);
                Logger.Log(LogType.Error, "WorldCommands.PortalH: " + ex);
            }
            catch (Exception ex)
            {
                player.Message("Unexpected error: " + ex);
                Logger.Log(LogType.Error, "WorldCommands.PortalH: " + ex);
            }
        }

        static void PortalCreateCallback(Player player, Vector3I[] marks, object tag)
        {
            try
            {
                World world = WorldManager.FindWorldExact(player.PortalWorld);

                if (world != null)
                {
                    DrawOperation op = (DrawOperation)tag;
                    if (!op.Prepare(marks)) return;
                    if (!player.CanDraw(op.BlocksTotalEstimate))
                    {
                        player.MessageNow("You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                           player.Info.Rank.DrawLimit,
                                           op.Bounds.Volume);
                        op.Cancel();
                        return;
                    }

                    int Xmin = Math.Min(marks[0].X, marks[1].X);
                    int Xmax = Math.Max(marks[0].X, marks[1].X);
                    int Ymin = Math.Min(marks[0].Y, marks[1].Y);
                    int Ymax = Math.Max(marks[0].Y, marks[1].Y);
                    int Zmin = Math.Min(marks[0].Z, marks[1].Z);
                    int Zmax = Math.Max(marks[0].Z, marks[1].Z);

                    for (int x = Xmin; x <= Xmax; x++)
                    {
                        for (int y = Ymin; y <= Ymax; y++)
                        {
                            for (int z = Zmin; z <= Zmax; z++)
                            {
                                if (PortalHandler.IsInRangeOfSpawnpoint(player.World, new Vector3I(x, y, z)))
                                {
                                    player.Message("You can not build a portal near a spawnpoint.");
                                    return;
                                }

                                if (PortalHandler.GetInstance().GetPortal(player.World, new Vector3I(x, y, z)) != null)
                                {
                                    player.Message("You can not build a portal inside a portal, U MAD BRO?");
                                    return;
                                }
                            }
                        }
                    }

                    if (player.PortalName == null)
                    {
                        player.PortalName = Portal.GenerateName(player.World);
                    }

                    Portal portal = new Portal(player.PortalWorld, marks, player.PortalName, player.Name, player.World.Name);
                    PortalHandler.CreatePortal(portal, player.World);
                    op.AnnounceCompletion = false;
                    op.Context = BlockChangeContext.Portal;
                    op.Begin();

                    player.Message("Successfully created portal with name " + portal.Name + ".");
                }
                else
                {
                    player.MessageInvalidWorldName(player.PortalWorld);
                }
            }
            catch (Exception ex)
            {
                player.Message("Failed to create portal.");
                Logger.Log(LogType.Error, "WorldCommands.PortalCreateCallback: " + ex);
            }
        }
        #endregion

        static readonly CommandDescriptor CdWorldSearch = new CommandDescriptor
        {
            Name = "Worldsearch",
            Aliases = new[] { "ws" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Chat },
            Usage = "/Worldsearch WorldName",
            Help = "An easy way to search through a big list of worlds",
            Handler = WorldSearchHandler
        };

        static void WorldSearchHandler(Player player, Command cmd)
        {
            string worldName = cmd.Next();
            if (worldName == null)
            {
                CdWorldSearch.PrintUsage(player);
                return;
            }

            if (worldName.Length < 2)
            {
                CdWorldSearch.PrintUsage(player);
                return;
            }

            else
            {
                worldName = worldName.ToLower();
                player.Message("Worlds found: ");
                foreach (World w in WorldManager.Worlds)
                {
                    if (w.Name.ToLower().Contains(worldName))
                        player.Message("{0} ", w.ClassyName);
                }
                return;
            }
        }



        static readonly CommandDescriptor CdRankHide = new CommandDescriptor
        {
            Name = "Rankhide",
            Aliases = new[] { "rhide" },
            Category = CommandCategory.Maintenance,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.HideRanks },
            Usage = "/rhide rankname",
            Handler = RankHideHandler
        };

        static void RankHideHandler(Player player, Command cmd)
        {
            string worldName = cmd.Next();
            if (worldName == null)
            {
                CdRankHide.PrintUsage(player);
                return;
            }

            Rank rank = RankManager.FindRank(worldName);
            if (rank == null) return;

            if (rank.IsHidden)
            {
                player.Message("Rank \"{0}&S\" is no longer hidden.", rank.ClassyName);
                rank.IsHidden = false;
                return;
            }
            else
            {
                player.Message("Rank \"{0}&S\" is now hidden.", rank.ClassyName);
                rank.IsHidden = true;

            }
        }


        static readonly CommandDescriptor CdRealm = new CommandDescriptor
        {
            Name = "Realm",
            Category = CommandCategory.World,
            Permissions = new[] { Permission.Realm },
            IsConsoleSafe = false,
            Usage = "/Realm &A| Help | Join | Like | Home | Flush | Spawn " +
            "| Review | Create | Allow | Unallow | Ban | Unban | Activate | Invite",
            Help = "/Realm &A| Help | Join | Like | Home | Flush | Spawn " +
            "| Review | Create | Allow | Unallow | Ban | Unban | Activate | Invite",
            Handler = Realm,
        };

        internal static void Realm(Player player, Command cmd)
        {
            string Choice = cmd.Next();
            if (Choice == null)
            {
                CdRealm.PrintUsage(player);
                return;
            }
            switch (Choice.ToLower())
            {
                default:
                    CdRealm.PrintUsage(player);
                    break;

                case "review":

                    if (player.World.Name == player.Name)
                    {
                        var recepientList = Server.Players.Can(Permission.ReadStaffChat)
                                              .NotIgnoring(player)
                                              .Union(player);
                        string message = String.Format("{0}&C would like staff to review their realm", player.ClassyName);
                        recepientList.Message(message);
                    }

                    else
                        player.Message("You are not in your Realm");

                    break;

                case "like":

                    Choice = player.World.Name;
                    World world = WorldManager.FindWorldOrPrintMatches(player, Choice);
                    if (world == null) player.Message("You need to enter a realm name");

                    if (world.IsRealm)
                    {
                        Server.Players.Message("{0}&S likes realm {1}.",
                                               player.ClassyName, world.ClassyName);
                        return;
                    }
                    else player.Message("You are not in a Realm");

                    break;

                case "flush":

                    WorldFlushHandler(player, new Command("/wflush " + player.Name));
                    break;

                case "create":

                    string create = cmd.Next();
                    if (player.World.Name == player.Name)
                    {
                        player.Message("You cannot create a new Realm when you are inside your Realm");
                        return;
                    }

                    if (create == null)
                    {
                        player.Message("Realm create. Use /realm create [ThemeType]" +
                            " Theme types include | flat | hills | hell | island | swamp | desert | arctic | forest | ");
                    }

                    if (create == "flat")
                    {
                        RealmHandler.RealmCreate(player, cmd, "grass", "flat");
                        player.Message("You have created a Realm. Activate it with /realm activate");
                    }

                    if (create == "hills")
                    {
                        RealmHandler.RealmCreate(player, cmd, "grass", "hills");
                        player.Message("You have created a Realm. Activate it with /realm activate");
                    }

                    if (create == "island")
                    {
                        RealmHandler.RealmCreate(player, cmd, "desert", "island");
                        player.Message("You have created a Realm. Activate it with /realm activate");
                    }

                    if (create == "hell")
                    {
                        RealmHandler.RealmCreate(player, cmd, "hell", "streams");
                        player.Message("You have created a Realm. Activate it with /realm activate");
                    }

                    if (create == "swamp")
                    {
                        RealmHandler.RealmCreate(player, cmd, "swamp", "river");
                        player.Message("You have created a Realm. Activate it with /realm activate");
                    }

                    if (create == "desert")
                    {
                        RealmHandler.RealmCreate(player, cmd, "desert", "flat");
                        player.Message("You have created a Realm. Activate it with /realm activate");
                    }

                    if (create == "arctic")
                    {
                        RealmHandler.RealmCreate(player, cmd, "arctic", "ice");
                        player.Message("You have created a Realm. Activate it with /realm activate");
                    }

                    if (create == "forest")
                    {
                        RealmHandler.RealmCreate(player, cmd, "forest", "hills");
                        player.Message("You have created a Realm. Activate it with /realm activate");
                    }

                    break;

                case "home":
                    JoinHandler(player, new Command("/join " + player.Name));
                    break;

                case "help":

                    player.Message("To build a realm, use /realm create. To activate it so you can build, use /realm activate. " +
                    "If you find yourself unable to build in your Realm, use /realm activate again. " +
                    "If there are any Bugs, report them to Jonty800@gmail.com.");
                    break;

                case "activate":
                    {
                        if (player.World.Name == player.Name)
                        {
                            player.Message("You cannot use /Realm activate when you are in your Realm");
                            return;
                        }
                        RealmHandler.RealmLoad(player, cmd, player.Name + ".fcm", player.Name);
                        RealmHandler.RealmBuild(player, cmd, player.Name, RankManager.HighestRank.Name, null);
                        RealmHandler.RealmBuild(player, cmd, player.Name, "+" + player.Name, null);
                        WorldManager.SaveWorldList();
                        break;
                    }

                case "spawn":

                    if (player.World.Name == player.Name)
                    {
                        ModerationCommands.SetSpawnHandler(player, new Command("/setspawn"));
                        return;
                    }
                    else
                    {
                        player.Message("You can only change the Spawn on your own realm");
                        return;
                    }

                case "invite":

                    string invite = cmd.Next();

                    if (invite == null)
                    {
                        player.Message("Invite a player to see your Realm. Useage: /Realm invite playername.");
                        return;
                    }

                    Player targetInvite = Server.FindPlayerOrPrintMatches(player, invite, false, true);

                    if (targetInvite == null)
                    {
                        player.Message("Please enter the name of the player you want to invite into your Realm.");
                        return;
                    }

                    if (!Player.IsValidName(targetInvite.Name))
                    {
                        player.Message("Player not found. Please specify valid name.");
                        return;
                    }
                    else
                    {
                        targetInvite.Confirm(cmd, "{0}&S Has invited you to join their Realm \"{1}\".", player.ClassyName, player.Name);
                        World JoinName = WorldManager.FindWorldOrPrintMatches(targetInvite, player.Name);
                        targetInvite.JoinWorld(JoinName, WorldChangeReason.ManualJoin);
                    }
                    break;

                case "join":

                    string JoinCmd = cmd.Next();
                    if (JoinCmd == null)
                    {
                        player.Message("Derp. Invalid Realm.");
                        return;
                    }

                    else
                    {
                        Player target = Server.FindPlayerOrPrintMatches(player, Choice, false, true);
                        JoinHandler(player, new Command("/goto " + JoinCmd));
                        return;
                    }

                case "allow":

                    string toAllow = cmd.Next();

                    if (toAllow == null)
                    {
                        player.Message("Allows a player to build in your world. useage: /realm allow playername.");
                        return;
                    }

                    PlayerInfo targetAllow = PlayerDB.FindPlayerInfoOrPrintMatches(player, toAllow);

                    if (targetAllow == null)
                    {
                        player.Message("Please enter the name of the player you want to allow to build in your Realm.");
                        return;
                    }

                    if (!Player.IsValidName(targetAllow.Name))
                    {
                        player.Message("Player not found. Please specify valid name.");
                        return;
                    }

                    else
                    {
                        RealmHandler.RealmBuild(player, cmd, player.Name, "+" + targetAllow.Name, null);
                        if (!Player.IsValidName(targetAllow.Name))
                        {
                            player.Message("Player not found. Please specify valid name.");
                            return;
                        }
                    }
                    break;

                case "unallow":

                    string Unallow = cmd.Next();

                    if (Unallow == null)
                    {
                        player.Message("Stops a player from building in your world. usage: /realm unallow playername.");
                        return;
                    }
                    PlayerInfo targetUnallow = PlayerDB.FindPlayerInfoOrPrintMatches(player, Unallow);


                    if (targetUnallow == null)
                    {
                        player.Message("Please enter the name of the player you want to stop building in your Realm.");
                        return;
                    }

                    if (!Player.IsValidName(targetUnallow.Name))
                    {
                        player.Message("Player not found. Please specify valid name.");
                        return;
                    }

                    else
                    {
                        RealmHandler.RealmBuild(player, cmd, player.Name, "-" + targetUnallow.Name, null);
                        if (!Player.IsValidName(targetUnallow.Name))
                        {
                            player.Message("Player not found. Please specify valid name.");
                            return;
                        }
                    }
                    break;

                case "ban":

                    string Ban = cmd.Next();

                    if (Ban == null)
                    {
                        player.Message("Bans a player from accessing your Realm. Useage: /Realm ban playername.");
                        return;
                    }
                    Player targetBan = Server.FindPlayerOrPrintMatches(player, Ban, false, true);


                    if (targetBan == null)
                    {
                        player.Message("Please enter the name of the player you want to ban from your Realm.");
                        return;
                    }

                    if (!Player.IsValidName(targetBan.Name))
                    {
                        player.Message("Player not found. Please specify valid name.");
                        return;
                    }

                    else
                    {
                        RealmHandler.RealmAccess(player, cmd, player.Name, "-" + targetBan.Name);
                        if (!Player.IsValidName(targetBan.Name))
                        {
                            player.Message("Player not found. Please specify valid name.");
                            return;
                        }
                    }

                    break;

                case "unban":

                    string UnBan = cmd.Next();

                    if (UnBan == null)
                    {
                        player.Message("Unbans a player from your Realm. Useage: /Realm unban playername.");
                        return;
                    }
                    PlayerInfo targetUnBan = PlayerDB.FindPlayerInfoOrPrintMatches(player, UnBan);

                    if (targetUnBan == null)
                    {
                        player.Message("Please enter the name of the player you want to unban from your Realm.");
                        return;
                    }

                    if (!Player.IsValidName(targetUnBan.Name))
                    {
                        player.Message("Player not found. Please specify valid name.");
                        return;
                    }

                    else
                    {
                        RealmHandler.RealmAccess(player, cmd, player.Name, "+" + targetUnBan.Name);
                        if (!Player.IsValidName(targetUnBan.Name))
                        {
                            player.Message("Player not found. Please specify valid name.");
                            return;
                        }
                        break;
                    }
            }
        }

        static readonly CommandDescriptor CdGuestwipe = new CommandDescriptor
        {
            Name = "Guestwipe",

            Category = CommandCategory.World,
            Permissions = new[] { Permission.ManageWorlds },
            IsConsoleSafe = true,
            Usage = "/guestwipe",
            Help = "Wipes a map with the name 'Guest'.",
            Handler = Guestwipe
        };

        internal static void Guestwipe(Player player, Command cmd)
        {
            Scheduler.NewTask(t => Server.Players.Message("&9Warning! The Guest world will be wiped in 30 seconds.")).RunOnce(TimeSpan.FromSeconds(1));
            Scheduler.NewTask(t => Server.Players.Message("&9Warning! The Guest world will be wiped in 15 seconds.")).RunOnce(TimeSpan.FromSeconds(16));
            Scheduler.NewTask(t => player.Message("&4Prepare to use /ok when notified.")).RunOnce(TimeSpan.FromSeconds(25));
            Scheduler.NewTask(t => WorldLoadHandler(player, new Command("/wload guestwipe guest"))).RunOnce(TimeSpan.FromSeconds(27));
            return;
        }
        #endregion

        #region BlockDB

        static readonly CommandDescriptor CdBlockDB = new CommandDescriptor {
            Name = "BlockDB",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageBlockDB },
            Usage = "/BlockDB <WorldName> <Operation>",
            Help = "Manages BlockDB on a given world. " +
                   "Operations are: On, Off, Clear, Limit, TimeLimit, Preload. " +
                   "See &H/Help BlockDB <Operation>&S for operation-specific help. " +
                   "If no operation is given, world's BlockDB status is shown. " +
                   "If no WorldName is given, prints status of all worlds.",
            HelpSections = new Dictionary<string, string>{
                { "auto",       "/BlockDB <WorldName> Auto\n&S" +
                                "Allows BlockDB to decide whether it should be enabled or disabled based on each world's permissions (default)." },
                { "on",         "/BlockDB <WorldName> On\n&S" +
                                "Enables block tracking. Information will only be available for blocks that changed while BlockDB was enabled." },
                { "off",        "/BlockDB <WorldName> Off\n&S" +
                                "Disables block tracking. Block changes will NOT be recorded while BlockDB is disabled. " +
                                "Note that disabling BlockDB does not delete the existing data. Use &Hclear&S for that." },
                { "clear",      "/BlockDB <WorldName> Clear\n&S" +
                                "Clears all recorded data from the BlockDB. Erases all changes from memory and deletes the .fbdb file." },
                { "limit",      "/BlockDB <WorldName> Limit <#>|None\n&S" +
                                "Sets the limit on the maximum number of changes to store for a given world. " +
                                "Oldest changes will be deleted once the limit is reached. " +
                                "Put \"None\" to disable limiting. " +
                                "Unless a Limit or a TimeLimit it specified, all changes will be stored indefinitely." },
                { "timelimit",  "/BlockDB <WorldName> TimeLimit <Time>/None\n&S" +
                                "Sets the age limit for stored changes. " +
                                "Oldest changes will be deleted once the limit is reached. " +
                                "Use \"None\" to disable time limiting. " +
                                "Unless a Limit or a TimeLimit it specified, all changes will be stored indefinitely." },
                { "preload",    "/BlockDB <WorldName> Preload On/Off\n&S" +
                                "Enabled or disables preloading. When BlockDB is preloaded, all changes are stored in memory as well as in a file. " +
                                "This reduces CPU and disk use for busy maps, but may not be suitable for large maps due to increased memory use." },
            },
            Handler = BlockDBHandler
        };

        static void BlockDBHandler( Player player, Command cmd ) {
            if( !BlockDB.IsEnabledGlobally ) {
                player.Message( "&WBlockDB is disabled on this server." );
                return;
            }

            string worldName = cmd.Next();
            if( worldName == null ) {
                int total = 0;
                World[] autoEnabledWorlds = WorldManager.Worlds.Where( w => (w.BlockDB.EnabledState == YesNoAuto.Auto) && w.BlockDB.IsEnabled ).ToArray();
                if( autoEnabledWorlds.Length > 0 ) {
                    total += autoEnabledWorlds.Length;
                    player.Message( "BlockDB is auto-enabled on: {0}",
                                    autoEnabledWorlds.JoinToClassyString() );
                }

                World[] manuallyEnabledWorlds = WorldManager.Worlds.Where( w => w.BlockDB.EnabledState == YesNoAuto.Yes ).ToArray();
                if( manuallyEnabledWorlds.Length > 0 ) {
                    total += manuallyEnabledWorlds.Length;
                    player.Message( "BlockDB is manually enabled on: {0}",
                                    manuallyEnabledWorlds.JoinToClassyString() );
                }

                World[] manuallyDisabledWorlds = WorldManager.Worlds.Where( w => w.BlockDB.EnabledState == YesNoAuto.No ).ToArray();
                if( manuallyDisabledWorlds.Length > 0 ) {
                    player.Message( "BlockDB is manually disabled on: {0}",
                                    manuallyDisabledWorlds.JoinToClassyString() );
                }

                if( total == 0 ) {
                    player.Message( "BlockDB is not enabled on any world." );
                }
                return;
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;
            BlockDB db = world.BlockDB;

            lock( db.SyncRoot ) {
                string op = cmd.Next();
                if( op == null ) {
                    if( !db.IsEnabled ) {
                        if( db.EnabledState == YesNoAuto.Auto ) {
                            player.Message( "BlockDB is disabled (auto) on world {0}", world.ClassyName );
                        } else {
                            player.Message( "BlockDB is disabled on world {0}", world.ClassyName );
                        }
                    } else {
                        if( db.IsPreloaded ) {
                            if( db.EnabledState == YesNoAuto.Auto ) {
                                player.Message( "BlockDB is enabled (auto) and preloaded on world {0}", world.ClassyName );
                            } else {
                                player.Message( "BlockDB is enabled and preloaded on world {0}", world.ClassyName );
                            }
                        } else {
                            if( db.EnabledState == YesNoAuto.Auto ) {
                                player.Message( "BlockDB is enabled (auto) on world {0}", world.ClassyName );
                            } else {
                                player.Message( "BlockDB is enabled on world {0}", world.ClassyName );
                            }
                        }
                        player.Message( "    Change limit: {0}    Time limit: {1}",
                                        db.Limit == 0 ? "none" : db.Limit.ToString(),
                                        db.TimeLimit == TimeSpan.Zero ? "none" : db.TimeLimit.ToMiniString() );
                    }
                    return;
                }

                switch( op.ToLower() ) {
                    case "on":
                        // enables BlockDB
                        if( db.EnabledState == YesNoAuto.Yes ) {
                            player.Message( "BlockDB is already manually enabled on world {0}", world.ClassyName );

                        } else if( db.EnabledState == YesNoAuto.Auto && db.IsEnabled ) {
                            db.EnabledState = YesNoAuto.Yes;
                            WorldManager.SaveWorldList();
                            player.Message( "BlockDB was auto-enabled, and is now manually enabled on world {0}", world.ClassyName );

                        } else {
                            db.EnabledState = YesNoAuto.Yes;
                            WorldManager.SaveWorldList();
                            player.Message( "BlockDB is now manually enabled on world {0}", world.ClassyName );
                        }
                        break;

                    case "off":
                        // disables BlockDB
                        if( db.EnabledState == YesNoAuto.No ) {
                            player.Message( "BlockDB is already manually disabled on world {0}", world.ClassyName );

                        } else if( db.IsEnabled ) {
                            if( cmd.IsConfirmed ) {
                                db.EnabledState = YesNoAuto.No;
                                WorldManager.SaveWorldList();
                                player.Message( "BlockDB is now manually disabled on world {0}&S. Use &H/BlockDB {1} clear&S to delete all the data.",
                                                world.ClassyName, world.Name );
                            } else {
                                player.Confirm( cmd,
                                                "Disable BlockDB on world {0}&S? Block changes will stop being recorded.",
                                                world.ClassyName );
                            }
                        } else {
                            db.EnabledState = YesNoAuto.No;
                            WorldManager.SaveWorldList();
                            player.Message( "BlockDB was auto-disabled, and is now manually disabled on world {0}&S.",
                                            world.ClassyName );
                        }
                        break;

                    case "auto":
                        if( db.EnabledState == YesNoAuto.Auto ) {
                            player.Message( "BlockDB is already set to automatically enable/disable itself on world {0}", world.ClassyName );
                        } else {
                            db.EnabledState = YesNoAuto.Auto;
                            WorldManager.SaveWorldList();
                            if( db.IsEnabled ) {
                                player.Message( "BlockDB is now auto-enabled on world {0}",
                                                world.ClassyName );
                            } else {
                                player.Message( "BlockDB is now auto-disabled on world {0}",
                                                world.ClassyName );
                            }
                        }
                        break;

                    case "limit":
                        // sets or resets limit on the number of changes to store
                        if( db.IsEnabled ) {
                            string limitString = cmd.Next();
                            int limitNumber;

                            if( limitString == null ) {
                                player.Message( "BlockDB: Limit for world {0}&S is {1}",
                                                world.ClassyName,
                                                (db.Limit == 0 ? "none" : db.Limit.ToString()) );
                                return;
                            }

                            if( limitString.Equals( "none", StringComparison.OrdinalIgnoreCase ) ) {
                                limitNumber = 0;

                            } else if( !Int32.TryParse( limitString, out limitNumber ) ) {
                                CdBlockDB.PrintUsage( player );
                                return;

                            } else if( limitNumber < 0 ) {
                                player.Message( "BlockDB: Limit must be non-negative." );
                                return;
                            }

                            if( !cmd.IsConfirmed && limitNumber != 0 ) {
                                player.Confirm( cmd, "BlockDB: Change limit? Some old data for world {0}&S may be discarded.", world.ClassyName );

                            } else {
                                string limitDisplayString = (limitNumber == 0 ? "none" : limitNumber.ToString());
                                if( db.Limit == limitNumber ) {
                                    player.Message( "BlockDB: Limit for world {0}&S is already set to {1}",
                                                   world.ClassyName, limitDisplayString );

                                } else {
                                    db.Limit = limitNumber;
                                    WorldManager.SaveWorldList();
                                    player.Message( "BlockDB: Limit for world {0}&S set to {1}",
                                                   world.ClassyName, limitDisplayString );
                                }
                            }

                        } else {
                            player.Message( "Block tracking is disabled on world {0}", world.ClassyName );
                        }
                        break;

                    case "timelimit":
                        // sets or resets limit on the age of changes to store
                        if( db.IsEnabled ) {
                            string limitString = cmd.Next();

                            if( limitString == null ) {
                                if( db.TimeLimit == TimeSpan.Zero ) {
                                    player.Message( "BlockDB: There is no time limit for world {0}",
                                                    world.ClassyName );
                                } else {
                                    player.Message( "BlockDB: Time limit for world {0}&S is {1}",
                                                    world.ClassyName, db.TimeLimit.ToMiniString() );
                                }
                                return;
                            }

                            TimeSpan limit;
                            if( limitString.Equals( "none", StringComparison.OrdinalIgnoreCase ) ) {
                                limit = TimeSpan.Zero;

                            } else if( !limitString.TryParseMiniTimespan( out limit ) ) {
                                CdBlockDB.PrintUsage( player );
                                return;
                            }
                            if( limit > DateTimeUtil.MaxTimeSpan ) {
                                player.MessageMaxTimeSpan();
                                return;
                            }

                            if( !cmd.IsConfirmed && limit != TimeSpan.Zero ) {
                                player.Confirm( cmd, "BlockDB: Change time limit? Some old data for world {0}&S may be discarded.", world.ClassyName );

                            } else {

                                if( db.TimeLimit == limit ) {
                                    if( db.TimeLimit == TimeSpan.Zero ) {
                                        player.Message( "BlockDB: There is already no time limit for world {0}",
                                                        world.ClassyName );
                                    } else {
                                        player.Message( "BlockDB: Time limit for world {0}&S is already set to {1}",
                                                        world.ClassyName, db.TimeLimit.ToMiniString() );
                                    }
                                } else {
                                    db.TimeLimit = limit;
                                    WorldManager.SaveWorldList();
                                    if( db.TimeLimit == TimeSpan.Zero ) {
                                        player.Message( "BlockDB: Time limit removed for world {0}",
                                                        world.ClassyName );
                                    } else {
                                        player.Message( "BlockDB: Time limit for world {0}&S set to {1}",
                                                        world.ClassyName, db.TimeLimit.ToMiniString() );
                                    }
                                }
                            }

                        } else {
                            player.Message( "Block tracking is disabled on world {0}", world.ClassyName );
                        }
                        break;

                    case "clear":
                        // wipes BlockDB data
                        bool hasData = (db.IsEnabled || File.Exists( db.FileName ));
                        if( hasData ) {
                            if( cmd.IsConfirmed ) {
                                db.Clear();
                                player.Message( "BlockDB: Cleared all data for {0}", world.ClassyName );
                            } else {
                                player.Confirm( cmd, "Clear BlockDB data for world {0}&S? This cannot be undone.",
                                                world.ClassyName );
                            }
                        } else {
                            player.Message( "BlockDB: No data to clear for world {0}", world.ClassyName );
                        }
                        break;

                    case "preload":
                        // enables/disables BlockDB preloading
                        if( db.IsEnabled ) {
                            string param = cmd.Next();
                            if( param == null ) {
                                // shows current preload setting
                                player.Message( "BlockDB preloading is {0} for world {1}",
                                                (db.IsPreloaded ? "ON" : "OFF"),
                                                world.ClassyName );

                            } else if( param.Equals( "on", StringComparison.OrdinalIgnoreCase ) ) {
                                // turns preload on
                                if( db.IsPreloaded ) {
                                    player.Message( "BlockDB preloading is already enabled on world {0}", world.ClassyName );
                                } else {
                                    db.IsPreloaded = true;
                                    WorldManager.SaveWorldList();
                                    player.Message( "BlockDB preloading is now enabled on world {0}", world.ClassyName );
                                }

                            } else if( param.Equals( "off", StringComparison.OrdinalIgnoreCase ) ) {
                                // turns preload off
                                if( !db.IsPreloaded ) {
                                    player.Message( "BlockDB preloading is already disabled on world {0}", world.ClassyName );
                                } else {
                                    db.IsPreloaded = false;
                                    WorldManager.SaveWorldList();
                                    player.Message( "BlockDB preloading is now disabled on world {0}", world.ClassyName );
                                }

                            } else {
                                CdBlockDB.PrintUsage( player );
                            }
                        } else {
                            player.Message( "Block tracking is disabled on world {0}", world.ClassyName );
                        }
                        break;

                    default:
                        // unknown operand
                        CdBlockDB.PrintUsage( player );
                        return;
                }
            }
        }

        #endregion


        #region BlockInfo

        static readonly CommandDescriptor CdBlockInfo = new CommandDescriptor {
            Name = "BInfo",
            Category = CommandCategory.World,
            Aliases = new[] { "b", "bi", "whodid", "about" },
            Permissions = new[] { Permission.ViewOthersInfo },
            RepeatableSelection = true,
            Usage = "/BInfo [X Y Z]",
            Help = "Checks edit history for a given block.",
            Handler = BlockInfoHandler
        };

        static void BlockInfoHandler( Player player, Command cmd ) {
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );

            // Make sure BlockDB is usable
            if( !BlockDB.IsEnabledGlobally ) {
                player.Message( "&WBlockDB is disabled on this server." );
                return;
            }
            if( !playerWorld.BlockDB.IsEnabled ) {
                player.Message( "&WBlockDB is disabled in this world." );
                return;
            }

            int x, y, z;
            if( cmd.NextInt( out x ) && cmd.NextInt( out y ) && cmd.NextInt( out z ) ) {
                // If block coordinates are given, run the BlockDB query right away
                if( cmd.HasNext ) {
                    CdBlockInfo.PrintUsage( player );
                    return;
                }
                Vector3I coords = new Vector3I( x, y, z );
                Map map = player.WorldMap;
                coords.X = Math.Min( map.Width - 1, Math.Max( 0, coords.X ) );
                coords.Y = Math.Min( map.Length - 1, Math.Max( 0, coords.Y ) );
                coords.Z = Math.Min( map.Height - 1, Math.Max( 0, coords.Z ) );
                BlockInfoSelectionCallback( player, new[] { coords }, null );

            } else {
                // Otherwise, start a selection
                player.Message( "BInfo: Click a block to look it up." );
                player.SelectionStart( 1, BlockInfoSelectionCallback, null, CdBlockInfo.Permissions );
            }
        }

        static void BlockInfoSelectionCallback( Player player, Vector3I[] marks, object tag ) {
            var args = new BlockInfoLookupArgs {
                Player = player,
                World = player.World,
                Coordinate = marks[0]
            };

            Scheduler.NewBackgroundTask( BlockInfoSchedulerCallback, args ).RunOnce();
        }


        sealed class BlockInfoLookupArgs {
            public Player Player;
            public World World;
            public Vector3I Coordinate;
        }

        const int MaxBlockChangesToList = 15;
        static void BlockInfoSchedulerCallback( SchedulerTask task ) {
            BlockInfoLookupArgs args = (BlockInfoLookupArgs)task.UserState;
            if( !args.World.BlockDB.IsEnabled ) {
                args.Player.Message( "&WBlockDB is disabled in this world." );
                return;
            }
            BlockDBEntry[] results = args.World.BlockDB.Lookup( args.Coordinate );
            if( results.Length > 0 ) {
                int startIndex = Math.Max( 0, results.Length - MaxBlockChangesToList );
                for( int i = startIndex; i < results.Length; i++ ) {
                    BlockDBEntry entry = results[i];
                    string date = DateTime.UtcNow.Subtract( DateTimeUtil.ToDateTime( entry.Timestamp ) ).ToMiniString();

                    PlayerInfo info = PlayerDB.FindPlayerInfoByID( entry.PlayerID );
                    string playerName;
                    if( info == null ) {
                        playerName = "?";
                    } else {
                        Player target = info.PlayerObject;
                        if( target != null && args.Player.CanSee( target ) ) {
                            playerName = info.ClassyName;
                        } else {
                            playerName = info.ClassyName + "&S (offline)";
                        }
                    }
                    string contextString;
                    if( entry.Context == BlockChangeContext.Manual ) {
                        contextString = "";
                    } else if( (entry.Context & BlockChangeContext.Drawn) == BlockChangeContext.Drawn &&
                        entry.Context != BlockChangeContext.Drawn ) {
                        contextString = " (" + (entry.Context & ~BlockChangeContext.Drawn) + ")";
                    } else {
                        contextString = " (" + entry.Context + ")";
                    }

                    if( entry.OldBlock == (byte)Block.Air ) {
                        args.Player.Message( "&S  {0} ago: {1}&S placed {2}{3}",
                                             date, playerName, entry.NewBlock, contextString );
                    } else if( entry.NewBlock == (byte)Block.Air ) {
                        args.Player.Message( "&S  {0} ago: {1}&S deleted {2}{3}",
                                             date, playerName, entry.OldBlock, contextString );
                    } else {
                        args.Player.Message( "&S  {0} ago: {1}&S replaced {2} with {3}{4}",
                                             date, playerName, entry.OldBlock, entry.NewBlock, contextString );
                    }
                }
            } else {
                args.Player.Message( "BlockInfo: No results for {0}",
                                     args.Coordinate );
            }
        }

        #endregion


        #region Env

        static readonly CommandDescriptor CdEnv = new CommandDescriptor
        {
            Name = "Env",
            Category = CommandCategory.World,
            Permissions = new[] { Permission.ManageWorlds },
            Help = "Prints or changes the environmental variables for a given world. " +
                   "Variables are: clouds, fog, sky, level, edge, terrain, realistic " +
                   "See &H/Help env <Variable>&S for details about each variable. " +
                   "Type &H/Env <WorldName> normal&S to reset everything for a world.",
            HelpSections = new Dictionary<string, string>{
                { "normal",     "&H/Env <WorldName> normal\n&S" +
                                "Resets all environment settings to their defaults for the given world." },
               { "terrain",     "&H/Env terrain terrainType. Leave blank for a list\n&S" +
                                "Changes the blockset for a given world ." },
               { "realistic",     "&H/Env realistic. Toggles realistic mode on or off\n&S" +
                                "Changes the environment according to the server time for a chosen world" },
                { "clouds",     "&H/Env <WorldName> clouds <Color>\n&S" +
                                "Sets color of the clouds. Use \"normal\" instead of color to reset." },
                { "fog",        "&H/Env <WorldName> fog <Color>\n&S" +
                                "Sets color of the fog. Sky color blends with fog color in the distance. " +
                                "Use \"normal\" instead of color to reset." },
                { "sky",        "&H/Env <WorldName> sky <Color>\n&S" +
                                "Sets color of the sky. Sky color blends with fog color in the distance. " +
                                "Use \"normal\" instead of color to reset." },
                { "level",      "&H/Env <WorldName> level <#>\n&S" +
                                "Sets height of the map edges/water level, in terms of blocks from the bottom of the map. " +
                                "Use \"normal\" instead of a number to reset to default (middle of the map)." },
                { "edge",       "&H/Env <WorldName> edge <BlockType>\n&S" +
                                "Changes the type of block that's visible beyond the map boundaries. "+
                                "Use \"normal\" instead of a number to reset to default (water)." }
            },
            Usage = "/Env <WorldName> <Variable>",
            IsConsoleSafe = true,
            Handler = EnvHandler
        };

        static void EnvHandler( Player player, Command cmd ) {
            if( !ConfigKey.WoMEnableEnvExtensions.Enabled() ) {
                player.Message( "Env command is disabled on this server." );
                return;
            }
            string worldName = cmd.Next();
            World world;
            if( worldName == null ) {
                world = player.World;
                if( world == null ) {
                    player.Message( "When used from console, /Env requires a world name." );
                    return;
                }
            } else {
                world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                if( world == null ) return;
            }

            string variable = cmd.Next();
            string valueText = cmd.Next();
            if( variable == null ) {
                player.Message( "Environment settings for world {0}&S:", world.ClassyName );
                player.Message( "  Cloud: {0}   Fog: {1}   Sky: {2}",
                                world.CloudColor == -1 ? "normal" : '#' + world.CloudColor.ToString( "X6" ),
                                world.FogColor == -1 ? "normal" : '#' + world.FogColor.ToString( "X6" ),
                                world.SkyColor == -1 ? "normal" : '#' + world.SkyColor.ToString( "X6" ) );
                player.Message( "  Edge level: {0}  Edge texture: {1}",
                                world.EdgeLevel == -1 ? "normal" : world.EdgeLevel + " blocks",
                                world.EdgeBlock );
                if( !player.IsUsingWoM ) {
                    player.Message( "  You need WoM client to see the changes." );
                }
                return;
            }

            if (variable.ToLower() == "terrain")
            {
                string option = cmd.Next();
                switch (option)
                {
                    case "normal":
                        world.Terrain = "bc4acee575474f5266105430c3cc628b8b3948a2";
                        player.Message("Terrain Changed for {0}", world.ClassyName);
                        WorldManager.SaveWorldList();
                        break;
                    case "simple":
                        world.Terrain = "85f783c3a70c0c9d523eb39e080c2ed95f45bfc2";
                        player.Message("Terrain Changed for {0}", world.ClassyName);
                        WorldManager.SaveWorldList();
                        break;
                    case "highres":
                        world.Terrain = "f3dac271d7bce9954baad46e183a6a910a30d13b";
                        player.Message("Terrain Changed for {0}", world.ClassyName);
                        WorldManager.SaveWorldList();
                        break;
                    case "tron":
                        world.Terrain = "ba851c9544ba5e4eed3a8fc9b8b5bf25a4dd45e0";
                        player.Message("Terrain Changed for {0}", world.ClassyName);
                        WorldManager.SaveWorldList();
                        break;
                    case "8bit":
                        world.Terrain = "5a3fb1994e2ae526815ceaaca3a4dac0051aa890";
                        player.Message("Terrain Changed for {0}", world.ClassyName);
                        WorldManager.SaveWorldList();
                        break;
                    case "mario":
                        world.Terrain = "e98a37ddccbc6144306bd08f41248324965c4e5a";
                        player.Message("Terrain Changed for {0}", world.ClassyName);
                        WorldManager.SaveWorldList();
                        break;
                    case "fall":
                        world.Terrain = "b7c6dcb7a858639077f95ef94e8e2d51bedc3307";
                        player.Message("Terrain Changed for {0}", world.ClassyName);
                        WorldManager.SaveWorldList();
                        break;
                    case "indev":
                        world.Terrain = "73d1ef4441725bdcc9ac3616205faa3dff46e12a";
                        player.Message("Terrain Changed Rejoin world to see changes");
                        WorldManager.SaveWorldList();
                        break;
                    case "messa":
                        world.Terrain = "db0feeac8702704a3146a71365622db55fb5a4c4";
                        player.Message("Terrain Changed Rejoin world to see changes");
                        WorldManager.SaveWorldList();
                        break;
                    case "portal":
                        world.Terrain = "d4b455134394763296994d0c819b0ac0ea338457";
                        player.Message("Terrain Changed for {0}", world.ClassyName);
                        WorldManager.SaveWorldList();
                        break;
                    case "winter":
                        world.Terrain = "3d22ed0ab311e003ed4e3ba17c3cf455019e7f35";
                        player.Message("Terrain Changed for {0}", world.ClassyName);
                        WorldManager.SaveWorldList();
                        break;
                    case "zelda":
                        world.Terrain = "b25e3bffe57c4f6a35ae42bb6116fcb21c50fa6f";
                        player.Message("Terrain Changed for {0}", world.ClassyName);
                        WorldManager.SaveWorldList();
                        break;
                    default: player.Message("&A/terrain Normal | fall | winter | tron | mario | highres | 8bit | simple |" +
                             " indev | messa | portal | zelda ");
                        break;
                }
                return;
            }

            if (variable.ToLower() == "realistic")
            {
                if (!world.RealisticEnv)
                {
                    world.RealisticEnv = true;
                    player.Message("Realistic Environment has been turned ON for world {0}", world.ClassyName);
                    return;
                }

                if (world.RealisticEnv)
                {
                    world.RealisticEnv = false;
                    player.Message("Realistic Environment has been turned OFF for world {0}", player.World.ClassyName);
                    return;
                }
            }

            if( variable.Equals( "normal", StringComparison.OrdinalIgnoreCase ) ) {
                if( cmd.IsConfirmed ) {
                    world.FogColor = -1;
                    world.CloudColor = -1;
                    world.SkyColor = -1;
                    world.EdgeLevel = -1;
                    world.EdgeBlock = Block.Water;
                    player.Message( "Reset enviroment settings for world {0}", world.ClassyName );
                    WorldManager.SaveWorldList();
                } else {
                    player.Confirm( cmd, "Reset enviroment settings for world {0}&S?", world.ClassyName );
                }
                return;
            }

            if( valueText == null ) {
                CdEnv.PrintUsage( player );
                return;
            }

            int value = 0;
            if( valueText.Equals( "normal", StringComparison.OrdinalIgnoreCase ) ) {
                value = -1;
            }

            switch( variable.ToLower() ) {
                case "fog":
                    if( value == -1 ) {
                        player.Message( "Reset fog color for {0}&S to normal", world.ClassyName );
                    } else {
                        try {
                            value = ParseHexColor( valueText );
                        } catch( FormatException ) {
                            CdEnv.PrintUsage( player );
                            return;
                        }
                        player.Message( "Set fog color for {0}&S to #{1:X6}", world.ClassyName, value );
                    }
                    world.FogColor = value;
                    break;

                case "cloud":
                case "clouds":
                    if( value == -1 ) {
                        player.Message( "Reset cloud color for {0}&S to normal", world.ClassyName );
                    } else {
                        try {
                            value = ParseHexColor( valueText );
                        } catch( FormatException ) {
                            CdEnv.PrintUsage( player );
                            return;
                        }
                        player.Message( "Set cloud color for {0}&S to #{1:X6}", world.ClassyName, value );
                    }
                    world.CloudColor = value;
                    break;

                case "sky":
                    if( value == -1 ) {
                        player.Message( "Reset sky color for {0}&S to normal", world.ClassyName );
                    } else {
                        try {
                            value = ParseHexColor( valueText );
                        } catch( FormatException ) {
                            CdEnv.PrintUsage( player );
                            return;
                        }
                        player.Message( "Set sky color for {0}&S to #{1:X6}", world.ClassyName, value );
                    }
                    world.SkyColor = value;
                    break;

                case "level":
                    if( value == -1 ) {
                        player.Message( "Reset edge level for {0}&S to normal", world.ClassyName );
                    } else {
                        try {
                            value = UInt16.Parse( valueText );
                        } catch( OverflowException ) {
                            CdEnv.PrintUsage( player );
                            return;
                        } catch( FormatException ) {
                            CdEnv.PrintUsage( player );
                            return;
                        }
                        player.Message( "Set edge level for {0}&S to {1}", world.ClassyName, value );
                    }
                    world.EdgeLevel = value;
                    break;

                case "edge":
                    Block block = Map.GetBlockByName( valueText );
                    if( block == Block.Undefined ) {
                        CdEnv.PrintUsage( player );
                        return;
                    }
                    if( block == Block.Water || valueText.Equals( "normal", StringComparison.OrdinalIgnoreCase ) ) {
                        player.Message( "Reset edge block for {0}&S to normal (water)", world.ClassyName );
                        world.EdgeBlock = Block.Water;
                    } else {
                        string textName = Map.GetEdgeTexture( block );
                        if( textName == null ) {
                            player.Message( "Cannot use {0} for edge textures.", block );
                            return;
                        } else {
                            world.EdgeBlock = block;
                        }
                    }
                    break;

                default:
                    CdEnv.PrintUsage( player );
                    return;
            }

            WorldManager.SaveWorldList();
            if( player.World == world ) {
                if( player.IsUsingWoM ) {
                    player.Message( "Rejoin the world to see the changes." );
                } else {
                    player.Message( "You need WoM client to see the changes." );
                }
            }
        }

        static int ParseHexColor( string text ) {
            byte red, green, blue;
            switch( text.Length ) {
                case 3:
                    red = (byte)(HexToValue( text[0] ) * 16 + HexToValue( text[0] ));
                    green = (byte)(HexToValue( text[1] ) * 16 + HexToValue( text[1] ));
                    blue = (byte)(HexToValue( text[2] ) * 16 + HexToValue( text[2] ));
                    break;
                case 4:
                    if( text[0] != '#' ) throw new FormatException();
                    red = (byte)(HexToValue( text[1] ) * 16 + HexToValue( text[1] ));
                    green = (byte)(HexToValue( text[2] ) * 16 + HexToValue( text[2] ));
                    blue = (byte)(HexToValue( text[3] ) * 16 + HexToValue( text[3] ));
                    break;
                case 6:
                    red = (byte)(HexToValue( text[0] ) * 16 + HexToValue( text[1] ));
                    green = (byte)(HexToValue( text[2] ) * 16 + HexToValue( text[3] ));
                    blue = (byte)(HexToValue( text[4] ) * 16 + HexToValue( text[5] ));
                    break;
                case 7:
                    if( text[0] != '#' ) throw new FormatException();
                    red = (byte)(HexToValue( text[1] ) * 16 + HexToValue( text[2] ));
                    green = (byte)(HexToValue( text[3] ) * 16 + HexToValue( text[4] ));
                    blue = (byte)(HexToValue( text[5] ) * 16 + HexToValue( text[6] ));
                    break;
                default:
                    throw new FormatException();
            }
            return red * 256 * 256 + green * 256 + blue;
        }

        static byte HexToValue( char c ) {
            if( c >= '0' && c <= '9' ) {
                return (byte)(c - '0');
            } else if( c >= 'A' && c <= 'F' ) {
                return (byte)(c - 'A' + 10);
            } else if( c >= 'a' && c <= 'f' ) {
                return (byte)(c - 'a' + 10);
            } else {
                throw new FormatException();
            }
        }

        static void TimeCheck(SchedulerTask task)
        {
            foreach (World world in WorldManager.Worlds)
            {
                if (world.RealisticEnv)
                {
                    int sky;
                    int clouds;
                    int fog;
                    DateTime now = DateTime.Now;
                    var SunriseStart = new TimeSpan(6, 30, 0);
                    var SunriseEnd = new TimeSpan(7, 29, 59);
                    var MorningStart = new TimeSpan(7, 30, 0);
                    var MorningEnd = new TimeSpan(11, 59, 59);
                    var NormalStart = new TimeSpan(12, 0, 0);
                    var NormalEnd = new TimeSpan(16, 59, 59);
                    var EveningStart = new TimeSpan(17, 0, 0);
                    var EveningEnd = new TimeSpan(18, 59, 59);
                    var SunsetStart = new TimeSpan(19, 0, 0);
                    var SunsetEnd = new TimeSpan(19, 29, 59);
                    var NightaStart = new TimeSpan(19, 30, 0);
                    var NightaEnd = new TimeSpan(1, 0, 1);
                    var NightbStart = new TimeSpan(1, 0, 2);
                    var NightbEnd = new TimeSpan(6, 29, 59);

                    if (now.TimeOfDay > SunriseStart && now.TimeOfDay < SunriseEnd) //sunrise
                    {
                        sky = ParseHexColor("ffff33");
                        clouds = ParseHexColor("ff0033");
                        fog = ParseHexColor("ff3333");
                        world.SkyColor = sky;
                        world.CloudColor = clouds;
                        world.FogColor = fog;
                        world.EdgeBlock = Block.Water;
                        WorldManager.SaveWorldList();
                        return;
                    }

                    if (now.TimeOfDay > MorningStart && now.TimeOfDay < MorningEnd) //end of sunrise
                    {
                        sky = -1;
                        clouds = ParseHexColor("ff0033");
                        fog = ParseHexColor("fffff0");
                        world.SkyColor = sky;
                        world.CloudColor = clouds;
                        world.FogColor = fog;
                        world.EdgeBlock = Block.Water;
                        WorldManager.SaveWorldList();
                        return;
                    }

                    if (now.TimeOfDay > NormalStart && now.TimeOfDay < NormalEnd)//env normal
                    {
                        sky = -1;
                        clouds = -1;
                        fog = -1;
                        world.SkyColor = sky;
                        world.CloudColor = clouds;
                        world.FogColor = fog;
                        world.EdgeBlock = Block.Water;
                        WorldManager.SaveWorldList();
                        return;
                    }

                    if (now.TimeOfDay > EveningStart && now.TimeOfDay < EveningEnd) //evening
                    {
                        sky = ParseHexColor("99cccc");
                        clouds = -1;
                        fog = ParseHexColor("99ccff");
                        world.SkyColor = sky;
                        world.CloudColor = clouds;
                        world.FogColor = fog;
                        world.EdgeBlock = Block.Water;
                        WorldManager.SaveWorldList();
                        return;
                    }

                    if (now.TimeOfDay > SunsetStart && now.TimeOfDay < SunsetEnd) //sunset
                    {
                        sky = ParseHexColor("9999cc");
                        clouds = ParseHexColor("000033");
                        fog = ParseHexColor("cc9966");
                        world.SkyColor = sky;
                        world.CloudColor = clouds;
                        world.FogColor = fog;
                        world.EdgeBlock = Block.Water;
                        WorldManager.SaveWorldList();
                        return;
                    }

                    if (now.TimeOfDay > NightaStart && now.TimeOfDay < NightaEnd) //end of sunset
                    {
                        sky = ParseHexColor("003366");
                        clouds = ParseHexColor("000033");
                        fog = ParseHexColor("000033");
                        world.SkyColor = sky;
                        world.CloudColor = clouds;
                        world.FogColor = fog;
                        world.EdgeBlock = Block.Black;
                        WorldManager.SaveWorldList();
                        return;
                    }

                    if (now.TimeOfDay > NightbStart && now.TimeOfDay < NightbEnd) //black
                    {
                        sky = ParseHexColor("000000");
                        clouds = ParseHexColor("000033");
                        fog = ParseHexColor("000033");
                        world.SkyColor = sky;
                        world.CloudColor = clouds;
                        world.FogColor = fog;
                        world.EdgeBlock = Block.Obsidian;
                        WorldManager.SaveWorldList();
                    }
                }
            }
        }

        #endregion


        #region Gen

        static readonly CommandDescriptor CdGenerate = new CommandDescriptor {
            Name = "Gen",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/Gen Theme Template [Width Length Height] [FileName]",
            //Help is assigned by WorldCommands.Init
            Handler = GenHandler
        };

        static void GenHandler( Player player, Command cmd ) {
            World playerWorld = player.World;
            string themeName = cmd.Next();
            string templateName;
            bool genOcean = false;
            bool genEmpty = false;
            bool noTrees = false;

            if( themeName == null ) {
                CdGenerate.PrintUsage( player );
                return;
            }
            MapGenTheme theme = MapGenTheme.Forest;
            MapGenTemplate template = MapGenTemplate.Flat;

            // parse special template names (which do not need a theme)
            if( themeName.Equals( "ocean" ) ) {
                genOcean = true;

            } else if( themeName.Equals( "empty" ) ) {
                genEmpty = true;

            } else {
                templateName = cmd.Next();
                if( templateName == null ) {
                    CdGenerate.PrintUsage( player );
                    return;
                }

                // parse theme
                bool swapThemeAndTemplate = false;
                if( themeName.Equals( "grass", StringComparison.OrdinalIgnoreCase ) ) {
                    theme = MapGenTheme.Forest;
                    noTrees = true;

                } else if( templateName.Equals( "grass", StringComparison.OrdinalIgnoreCase ) ) {
                    theme = MapGenTheme.Forest;
                    noTrees = true;
                    swapThemeAndTemplate = true;

                } else if( EnumUtil.TryParse( themeName, out theme, true ) ) {
                    noTrees = (theme != MapGenTheme.Forest);

                } else if( EnumUtil.TryParse( templateName, out theme, true ) ) {
                    noTrees = (theme != MapGenTheme.Forest);
                    swapThemeAndTemplate = true;

                } else {
                    player.Message( "Gen: Unrecognized theme \"{0}\". Available themes are: {1}",
                                    themeName,
                                    Enum.GetNames( typeof( MapGenTheme ) ).JoinToString() );
                    return;
                }

                // parse template
                if( swapThemeAndTemplate ) {
                    if( !EnumUtil.TryParse( themeName, out template, true ) ) {
                        player.Message( "Unrecognized template \"{0}\". Available templates are: {1}",
                                        themeName,
                                        Enum.GetNames( typeof( MapGenTemplate ) ).JoinToString() );
                        return;
                    }
                } else {
                    if( !EnumUtil.TryParse( templateName, out template, true ) ) {
                        player.Message( "Unrecognized template \"{0}\". Available templates are: {1}",
                                        templateName,
                                        Enum.GetNames( typeof( MapGenTemplate ) ).JoinToString() );
                        return;
                    }
                }
            }

            // parse map dimensions
            int mapWidth, mapLength, mapHeight;
            if( cmd.HasNext ) {
                int offset = cmd.Offset;
                if( !(cmd.NextInt( out mapWidth ) && cmd.NextInt( out mapLength ) && cmd.NextInt( out mapHeight )) ) {
                    if( playerWorld != null ) {
                        Map oldMap = player.WorldMap;
                        // If map dimensions were not given, use current map's dimensions
                        mapWidth = oldMap.Width;
                        mapLength = oldMap.Length;
                        mapHeight = oldMap.Height;
                    } else {
                        player.Message( "When used from console, /Gen requires map dimensions." );
                        CdGenerate.PrintUsage( player );
                        return;
                    }
                    cmd.Offset = offset;
                }
            } else if( playerWorld != null ) {
                Map oldMap = player.WorldMap;
                // If map dimensions were not given, use current map's dimensions
                mapWidth = oldMap.Width;
                mapLength = oldMap.Length;
                mapHeight = oldMap.Height;
            } else {
                player.Message( "When used from console, /Gen requires map dimensions." );
                CdGenerate.PrintUsage( player );
                return;
            }

            // Check map dimensions
            const string dimensionRecommendation = "Dimensions must be between 16 and 2047. " +
                                                   "Recommended values: 16, 32, 64, 128, 256, 512, and 1024.";
            if( !Map.IsValidDimension( mapWidth ) ) {
                player.Message( "Cannot make map with width {0}. {1}", mapWidth, dimensionRecommendation );
                return;
            } else if( !Map.IsValidDimension( mapLength ) ) {
                player.Message( "Cannot make map with length {0}. {1}", mapLength, dimensionRecommendation );
                return;
            } else if( !Map.IsValidDimension( mapHeight ) ) {
                player.Message( "Cannot make map with height {0}. {1}", mapHeight, dimensionRecommendation );
                return;
            }
            long volume = (long)mapWidth * (long)mapLength * (long)mapHeight;
            if( volume > Int32.MaxValue ) {
                player.Message( "Map volume may not exceed {0}", Int32.MaxValue );
                return;
            }

            if( !cmd.IsConfirmed && (!Map.IsRecommendedDimension( mapWidth ) || !Map.IsRecommendedDimension( mapLength )) ) {
                player.Message( "&WThe map will have non-standard dimensions. " +
                                "You may see glitched blocks or visual artifacts. " +
                                "The only recommended map dimensions are: 16, 32, 64, 128, 256, 512, and 1024." );
            }

            // figure out full template name
            bool genFlatgrass = (theme == MapGenTheme.Forest && noTrees && template == MapGenTemplate.Flat);
            string templateFullName;
            if( genEmpty ) {
                templateFullName = "Empty";
            } else if( genOcean ) {
                templateFullName = "Ocean";
            } else if( genFlatgrass ) {
                templateFullName = "Flatgrass";
            } else {
                if( theme == MapGenTheme.Forest && noTrees ) {
                    templateFullName = "Grass " + template;
                } else {
                    templateFullName = theme + " " + template;
                }
            }

            // check file/world name
            string fileName = cmd.Next();
            string fullFileName = null;
            if( fileName == null ) {
                // replacing current world
                if( playerWorld == null ) {
                    player.Message( "When used from console, /Gen requires FileName." );
                    CdGenerate.PrintUsage( player );
                    return;
                }
                if( !cmd.IsConfirmed ) {
                    player.Confirm( cmd, "Replace THIS MAP with a generated one ({0})?", templateFullName );
                    return;
                }

            } else {
                if( cmd.HasNext ) {
                    CdGenerate.PrintUsage( player );
                    return;
                }
                // saving to file
                fileName = fileName.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar );
                if( !fileName.EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase ) ) {
                    fileName += ".fcm";
                }
                if( !Paths.IsValidPath( fileName ) ) {
                    player.Message( "Invalid filename." );
                    return;
                }
                fullFileName = Path.Combine( Paths.MapPath, fileName );
                if( !Paths.Contains( Paths.MapPath, fullFileName ) ) {
                    player.MessageUnsafePath();
                    return;
                }
                string dirName = fullFileName.Substring( 0, fullFileName.LastIndexOf( Path.DirectorySeparatorChar ) );
                if( !Directory.Exists( dirName ) ) {
                    Directory.CreateDirectory( dirName );
                }
                if( !cmd.IsConfirmed && File.Exists( fullFileName ) ) {
                    player.Confirm( cmd, "The mapfile \"{0}\" already exists. Overwrite?", fileName );
                    return;
                }
            }

            // generate the map
            Map map;
            player.MessageNow( "Generating {0}...", templateFullName );

            if( genEmpty ) {
                map = MapGenerator.GenerateEmpty( mapWidth, mapLength, mapHeight );

            } else if( genOcean ) {
                map = MapGenerator.GenerateOcean( mapWidth, mapLength, mapHeight );

            } else if( genFlatgrass ) {
                map = MapGenerator.GenerateFlatgrass( mapWidth, mapLength, mapHeight );

            } else {
                MapGeneratorArgs args = MapGenerator.MakeTemplate( template );
                if( theme == MapGenTheme.Desert ) {
                    args.AddWater = false;
                }
                float ratio = mapHeight / (float)args.MapHeight;
                args.MapWidth = mapWidth;
                args.MapLength = mapLength;
                args.MapHeight = mapHeight;
                args.MaxHeight = (int)Math.Round( args.MaxHeight * ratio );
                args.MaxDepth = (int)Math.Round( args.MaxDepth * ratio );
                args.SnowAltitude = (int)Math.Round( args.SnowAltitude * ratio );
                args.Theme = theme;
                args.AddTrees = !noTrees;

                MapGenerator generator = new MapGenerator( args );
                map = generator.Generate();
            }

            // save map to file, or load it into a world
            if( fileName != null ) {
                if( map.Save( fullFileName ) ) {
                    player.Message( "Generation done. Saved to {0}", fileName );
                } else {
                    player.Message( "&WAn error occured while saving generated map to {0}", fileName );
                }
            } else {
                if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );
                player.MessageNow( "Generation done. Changing map..." );
                playerWorld.MapChangedBy = player.Name;
                playerWorld.ChangeMap( map );
            }
        }

        #endregion


        #region Join

        static readonly CommandDescriptor CdJoin = new CommandDescriptor {
            Name = "Join",
            Aliases = new[] { "j", "load", "goto", "map" },
            Category = CommandCategory.World,
            Usage = "/Join WorldName",
            Help = "Teleports the player to a specified world. You can see the list of available worlds by using &H/Worlds",
            Handler = JoinHandler
        };

        static void JoinHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                CdJoin.PrintUsage( player );
                return;
            } 
            
            if( worldName == "-" ) {
                if( player.LastUsedWorldName != null ) {
                    worldName = player.LastUsedWorldName;
                } else {
                    player.Message( "Cannot repeat world name: you haven't used any names yet." );
                    return;
                }
            }

            World[] worlds = WorldManager.FindWorlds( player, worldName );

            if( worlds.Length > 1 ) {
                player.MessageManyMatches( "world", worlds );

            } else if( worlds.Length == 1 ) {
                World world = worlds[0];
                player.LastUsedWorldName = world.Name;
                switch( world.AccessSecurity.CheckDetailed( player.Info ) ) {
                    case SecurityCheckResult.Allowed:
                    case SecurityCheckResult.WhiteListed:
                        if( world.IsFull ) {
                            player.Message( "Cannot join {0}&S: world is full.", world.ClassyName );
                            return;
                        }
                        player.StopSpectating();
                        if( !player.JoinWorldNow( world, true, WorldChangeReason.ManualJoin ) ) {
                            player.Message( "ERROR: Failed to join world. See log for details." );
                        }
                        break;
                    case SecurityCheckResult.BlackListed:
                        player.Message( "Cannot join world {0}&S: you are blacklisted.",
                                        world.ClassyName );
                        break;
                    case SecurityCheckResult.RankTooLow:
                        player.Message( "Cannot join world {0}&S: must be {1}+",
                                        world.ClassyName, world.AccessSecurity.MinRank.ClassyName );
                        break;
                }

            } else {
                // no worlds found - see if player meant to type in "/Join" and not "/TP"
                Player[] players = Server.FindPlayers( player, worldName, true );
                if( players.Length == 1 ) {
                    player.LastUsedPlayerName = players[0].Name;
                    player.StopSpectating();
                    player.ParseMessage( "/TP " + players[0].Name, false );
                } else {
                    player.MessageNoWorld( worldName );
                }
            }
        }

        #endregion


        #region WLock, WUnlock

        static readonly CommandDescriptor CdWorldLock = new CommandDescriptor {
            Name = "WLock",
            Aliases = new[] { "lock" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Lock },
            Usage = "/WLock [*|WorldName]",
            Help = "Puts the world into a locked, read-only mode. " +
                   "No one can place or delete blocks during lockdown. " +
                   "By default this locks the world you're on, but you can also lock any world by name. " +
                   "Put an asterisk (*) for world name to lock ALL worlds at once. " +
                   "Call &H/WUnlock&S to release lock on a world.",
            Handler = WorldLockHandler
        };

        static void WorldLockHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();

            World world;
            if( worldName != null ) {
                if( worldName == "*" ) {
                    int locked = 0;
                    World[] worldListCache = WorldManager.Worlds;
                    for( int i = 0; i < worldListCache.Length; i++ ) {
                        if( !worldListCache[i].IsLocked ) {
                            worldListCache[i].Lock( player );
                            locked++;
                        }
                    }
                    player.Message( "Unlocked {0} worlds.", locked );
                    return;
                } else {
                    world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                    if( world == null ) return;
                }

            } else if( player.World != null ) {
                world = player.World;

            } else {
                player.Message( "When called from console, /WLock requires a world name." );
                return;
            }

            if( !world.Lock( player ) ) {
                player.Message( "The world is already locked." );
            } else if( player.World != world ) {
                player.Message( "Locked world {0}", world );
            }
        }


        static readonly CommandDescriptor CdWorldUnlock = new CommandDescriptor {
            Name = "WUnlock",
            Aliases = new[] { "unlock" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Lock },
            Usage = "/WUnlock [*|WorldName]",
            Help = "Removes the lockdown set by &H/WLock&S. See &H/Help WLock&S for more information.",
            Handler = WorldUnlockHandler
        };

        static void WorldUnlockHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();

            World world;
            if( worldName != null ) {
                if( worldName == "*" ) {
                    World[] worldListCache = WorldManager.Worlds;
                    int unlocked = 0;
                    for( int i = 0; i < worldListCache.Length; i++ ) {
                        if( worldListCache[i].IsLocked ) {
                            worldListCache[i].Unlock( player );
                            unlocked++;
                        }
                    }
                    player.Message( "Unlocked {0} worlds.", unlocked );
                    return;
                } else {
                    world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                    if( world == null ) return;
                }

            } else if( player.World != null ) {
                world = player.World;

            } else {
                player.Message( "When called from console, /WLock requires a world name." );
                return;
            }

            if( !world.Unlock( player ) ) {
                player.Message( "The world is already unlocked." );
            } else if( player.World != world ) {
                player.Message( "Unlocked world {0}", world );
            }
        }

        #endregion


        #region Spawn

        static readonly CommandDescriptor CdSpawn = new CommandDescriptor {
            Name = "Spawn",
            Category = CommandCategory.World,
            Help = "Teleports you to the current map's spawn.",
            Handler = SpawnHandler
        };

        static void SpawnHandler( Player player, Command cmd ) {
            if( player.World == null ) PlayerOpException.ThrowNoWorld( player );
            player.TeleportTo( player.World.LoadMap().Spawn );
        }

        #endregion


        #region Worlds

        static readonly CommandDescriptor CdWorlds = new CommandDescriptor {
            Name = "Worlds",
            Category = CommandCategory.World | CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Aliases = new[] { "maps", "levels" },
            Usage = "/Worlds [all/hidden/realms/populated/@Rank]",
            Help = "Shows a list of available worlds. To join a world, type &H/Join WorldName&S. " +
                   "If the optional \"all\" is added, also shows inaccessible or hidden worlds. " +
                   "If \"hidden\" is added, shows only inaccessible and hidden worlds. " +
                   "If \"populated\" is added, shows only worlds with players online. " +
                   "If a rank name is given, shows only worlds where players of that rank can build.",
            Handler = WorldsHandler
        };

        static void WorldsHandler( Player player, Command cmd ) {
            string param = cmd.Next();
            World[] worlds;

            string listName;
            string extraParam;
            int offset = 0;

            if( param == null || Int32.TryParse( param, out offset ) ) {
                listName = "available worlds";
                extraParam = "";
                worlds = WorldManager.Worlds.Where( player.CanSee ).ToArray();

            } else {
                switch( Char.ToLower( param[0] ) ) {
                    case 'a':
                        listName = "worlds";
                        extraParam = "all ";
                        worlds = WorldManager.Worlds;
                        break;
                    case 'h':
                        listName = "hidden worlds";
                        extraParam = "hidden ";
                        worlds = WorldManager.Worlds.Where( w => !player.CanSee( w ) ).ToArray();
                        break;
                    case 'r':
                        listName = "Available Realms";
                        extraParam = "realms";
                        worlds = WorldManager.Worlds.Where(w => w.IsRealm).ToArray();
                        break;
                    case 'p':
                        listName = "populated worlds";
                        extraParam = "populated ";
                        worlds = WorldManager.Worlds.Where( w => w.Players.Any( player.CanSee ) ).ToArray();
                        break;
                    case '@':
                        if( param.Length == 1 ) {
                            CdWorlds.PrintUsage( player );
                            return;
                        }
                        string rankName = param.Substring( 1 );
                        Rank rank = RankManager.FindRank( rankName );
                        if( rank == null ) {
                            player.MessageNoRank( rankName );
                            return;
                        }
                        listName = String.Format( "worlds where {0}&S+ can build", rank.ClassyName );
                        extraParam = "@" + rank.Name + " ";
                        worlds = WorldManager.Worlds.Where( w => (w.BuildSecurity.MinRank <= rank) && player.CanSee( w ) )
                                                    .ToArray();
                        break;
                    default:
                        CdWorlds.PrintUsage( player );
                        return;
                }
                if( cmd.HasNext && !cmd.NextInt( out offset ) ) {
                    CdWorlds.PrintUsage( player );
                    return;
                }
            }

            if( worlds.Length == 0 ) {
                player.Message( "There are no {0}.", listName );

            } else if( worlds.Length <= WorldNamesPerPage || player.IsSuper ) {
                player.MessagePrefixed( "&S  ", "&SThere are {0} {1}: {2}",
                                        worlds.Length, listName, worlds.JoinToClassyString() );

            } else {
                if( offset >= worlds.Length ) {
                    offset = Math.Max( 0, worlds.Length - WorldNamesPerPage );
                }
                World[] worldsPart = worlds.Skip( offset ).Take( WorldNamesPerPage ).ToArray();
                player.MessagePrefixed( "&S   ", "&S{0}: {1}",
                                        listName.UppercaseFirst(), worldsPart.JoinToClassyString() );

                if( offset + worldsPart.Length < worlds.Length ) {
                    player.Message( "Showing {0}-{1} (out of {2}). Next: &H/Worlds {3}{1}",
                                    offset + 1, offset + worldsPart.Length, worlds.Length, extraParam );
                } else {
                    player.Message( "Showing worlds {0}-{1} (out of {2}).",
                                    offset + 1, offset + worldsPart.Length, worlds.Length );
                }
            }
        }

        #endregion


        #region WorldAccess

        static readonly CommandDescriptor CdWorldAccess = new CommandDescriptor {
            Name = "WAccess",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WAccess [WorldName [RankName]]",
            Help = "Shows access permission for player's current world. " +
                   "If optional WorldName parameter is given, shows access permission for another world. " +
                   "If RankName parameter is also given, sets access permission for specified world.",
            Handler = WorldAccessHandler
        };

        static void WorldAccessHandler( [NotNull] Player player, Command cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            string worldName = cmd.Next();

            // Print information about the current world
            if( worldName == null ) {
                if( player.World == null ) {
                    player.Message( "When calling /WAccess from console, you must specify a world name." );
                } else {
                    player.Message( player.World.AccessSecurity.GetDescription( player.World, "world", "accessed" ) );
                }
                return;
            }

            // Find a world by name
            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;


            string name = cmd.Next();
            if( name == null ) {
                player.Message( world.AccessSecurity.GetDescription( world, "world", "accessed" ) );
                return;
            }
            if( world == WorldManager.MainWorld ) {
                player.Message( "The main world cannot have access restrictions." );
                return;
            }

            bool changesWereMade = false;
            do {
                // Whitelisting individuals
                if( name.StartsWith( "+" ) ) {
                    if( name.Length == 1 ) {
                        CdWorldAccess.PrintUsage( player );
                        break;
                    }
                    PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches( player, name.Substring( 1 ) );
                    if( info == null ) return;

                    // prevent players from whitelisting themselves to bypass protection
                    if( player.Info == info && !player.Info.Rank.AllowSecurityCircumvention ) {
                        switch( world.AccessSecurity.CheckDetailed( player.Info ) ) {
                            case SecurityCheckResult.RankTooLow:
                                player.Message( "&WYou must be {0}&W+ to add yourself to the access whitelist of {1}",
                                                world.AccessSecurity.MinRank.ClassyName,
                                                world.ClassyName );
                                continue;
                            // TODO: RankTooHigh
                            case SecurityCheckResult.BlackListed:
                                player.Message( "&WYou cannot remove yourself from the access blacklist of {0}",
                                                world.ClassyName );
                                continue;
                        }
                    }

                    if( world.AccessSecurity.CheckDetailed( info ) == SecurityCheckResult.Allowed ) {
                        player.Message( "{0}&S is already allowed to access {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName );
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.AccessSecurity.Include( info ) ) {
                        case PermissionOverride.Deny:
                            if( world.AccessSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer barred from accessing {1}",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "You can now access world {0}&S (removed from blacklist by {1}&S).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            } else {
                                player.Message( "{0}&S was removed from the access blacklist of {1}&S. " +
                                                "Player is still NOT allowed to join (by rank).",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "You were removed from the access blacklist of world {0}&S by {1}&S. " +
                                                    "You are still NOT allowed to join (by rank).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            }
                            Logger.Log( LogType.UserActivity,
                                        "{0} removed {1} from the access blacklist of {2}",
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.None:
                            player.Message( "{0}&S is now allowed to access {1}",
                                            info.ClassyName, world.ClassyName );
                            if( target != null ) {
                                target.Message( "You can now access world {0}&S (whitelisted by {1}&S).",
                                                world.ClassyName, player.ClassyName );
                            }
                            Logger.Log( LogType.UserActivity,
                                        "{0} added {1} to the access whitelist on world {2}",
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            player.Message( "{0}&S is already on the access whitelist of {1}",
                                            info.ClassyName, world.ClassyName );
                            break;
                    }

                    // Blacklisting individuals
                } else if( name.StartsWith( "-" ) ) {
                    if( name.Length == 1 ) {
                        CdWorldAccess.PrintUsage( player );
                        break;
                    }
                    PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches( player, name.Substring( 1 ) );
                    if( info == null ) return;

                    if( world.AccessSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooHigh ||
                        world.AccessSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooLow ) {
                        player.Message( "{0}&S is already barred from accessing {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName );
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.AccessSecurity.Exclude( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is already on access blacklist of {1}",
                                            info.ClassyName, world.ClassyName );
                            break;

                        case PermissionOverride.None:
                            player.Message( "{0}&S is now barred from accessing {1}",
                                            info.ClassyName, world.ClassyName );
                            if( target != null ) {
                                target.Message( "&WYou were barred by {0}&W from accessing world {1}",
                                                player.ClassyName, world.ClassyName );
                            }
                            Logger.Log( LogType.UserActivity,
                                        "{0} added {1} to the access blacklist on world {2}",
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            if( world.AccessSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer on the access whitelist of {1}&S. " +
                                                "Player is still allowed to join (by rank).",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "You were removed from the access whitelist of world {0}&S by {1}&S. " +
                                                    "You are still allowed to join (by rank).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            } else {
                                player.Message( "{0}&S is no longer allowed to access {1}",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "&WYou can no longer access world {0}&W (removed from whitelist by {1}&W).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            }
                            Logger.Log( LogType.UserActivity,
                                        "{0} removed {1} from the access whitelist on world {2}",
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;
                    }

                    // Setting minimum rank
                } else {
                    Rank rank = RankManager.FindRank( name );
                    if( rank == null ) {
                        player.MessageNoRank( name );

                    } else if( !player.Info.Rank.AllowSecurityCircumvention &&
                               world.AccessSecurity.MinRank > rank &&
                               world.AccessSecurity.MinRank > player.Info.Rank ) {
                        player.Message( "&WYou must be ranked {0}&W+ to lower the access rank for world {1}",
                                        world.AccessSecurity.MinRank.ClassyName, world.ClassyName );

                    } else {
                        // list players who are redundantly blacklisted
                        var exceptionList = world.AccessSecurity.ExceptionList;
                        PlayerInfo[] noLongerExcluded = exceptionList.Excluded.Where( excludedPlayer => excludedPlayer.Rank < rank ).ToArray();
                        if( noLongerExcluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be blacklisted to be barred from {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerExcluded.JoinToClassyString() );
                        }

                        // list players who are redundantly whitelisted
                        PlayerInfo[] noLongerIncluded = exceptionList.Included.Where( includedPlayer => includedPlayer.Rank >= rank ).ToArray();
                        if( noLongerIncluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be whitelisted to access {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerIncluded.JoinToClassyString() );
                        }

                        // apply changes
                        world.AccessSecurity.MinRank = rank;
                        changesWereMade = true;
                        if( world.AccessSecurity.MinRank == RankManager.LowestRank ) {
                            Server.Message( "{0}&S made the world {1}&S accessible to everyone.",
                                              player.ClassyName, world.ClassyName );
                        } else {
                            Server.Message( "{0}&S made the world {1}&S accessible only by {2}+",
                                              player.ClassyName, world.ClassyName,
                                              world.AccessSecurity.MinRank.ClassyName );
                        }
                        Logger.Log( LogType.UserActivity,
                                    "{0} set access rank for world {1} to {2}+",
                                    player.Name, world.Name, world.AccessSecurity.MinRank.Name );
                    }
                }
            } while( (name = cmd.Next()) != null );

            if( changesWereMade ) {
                var playersWhoCantStay = world.Players.Where( p => !p.CanJoin( world ) );
                foreach( Player p in playersWhoCantStay ) {
                    p.Message( "&WYou are no longer allowed to join world {0}", world.ClassyName );
                    p.JoinWorld( WorldManager.MainWorld, WorldChangeReason.PermissionChanged );
                }
                WorldManager.SaveWorldList();
            }
        }

        #endregion


        #region WorldBuild

        static readonly CommandDescriptor CdWorldBuild = new CommandDescriptor {
            Name = "WBuild",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WBuild [WorldName [RankName]]",
            Help = "Shows build permissions for player's current world. " +
                   "If optional WorldName parameter is given, shows build permission for another world. " +
                   "If RankName parameter is also given, sets build permission for specified world.",
            Handler = WorldBuildHandler
        };

        static void WorldBuildHandler( [NotNull] Player player, Command cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            string worldName = cmd.Next();

            // Print information about the current world
            if( worldName == null ) {
                if( player.World == null ) {
                    player.Message( "When calling /WBuild from console, you must specify a world name." );
                } else {
                    player.Message( player.World.BuildSecurity.GetDescription( player.World, "world", "modified" ) );
                }
                return;
            }

            // Find a world by name
            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;


            string name = cmd.Next();
            if( name == null ) {
                player.Message( world.BuildSecurity.GetDescription( world, "world", "modified" ) );
                return;
            }

            bool changesWereMade = false;
            do {
                // Whitelisting individuals
                if( name.StartsWith( "+" ) ) {
                    if( name.Length == 1 ) {
                        CdWorldBuild.PrintUsage( player );
                        break;
                    }
                    PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches( player, name.Substring( 1 ) );
                    if( info == null ) return;

                    // prevent players from whitelisting themselves to bypass protection
                    if( player.Info == info && !player.Info.Rank.AllowSecurityCircumvention ) {
                        switch( world.BuildSecurity.CheckDetailed( player.Info ) ) {
                            case SecurityCheckResult.RankTooLow:
                                player.Message( "&WYou must be {0}&W+ to add yourself to the build whitelist of {1}",
                                                world.BuildSecurity.MinRank.ClassyName,
                                                world.ClassyName );
                                continue;
                            // TODO: RankTooHigh
                            case SecurityCheckResult.BlackListed:
                                player.Message( "&WYou cannot remove yourself from the build blacklist of {0}",
                                                world.ClassyName );
                                continue;
                        }
                    }

                    if( world.BuildSecurity.CheckDetailed( info ) == SecurityCheckResult.Allowed ) {
                        player.Message( "{0}&S is already allowed to build in {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName );
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.BuildSecurity.Include( info ) ) {
                        case PermissionOverride.Deny:
                            if( world.BuildSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer barred from building in {1}",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "You can now build in world {0}&S (removed from blacklist by {1}&S).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            } else {
                                player.Message( "{0}&S was removed from the build blacklist of {1}&S. " +
                                                "Player is still NOT allowed to build (by rank).",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "You were removed from the build blacklist of world {0}&S by {1}&S. " +
                                                    "You are still NOT allowed to build (by rank).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            }
                            Logger.Log( LogType.UserActivity,
                                        "{0} removed {1} from the build blacklist of {2}",
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.None:
                            player.Message( "{0}&S is now allowed to build in {1}",
                                            info.ClassyName, world.ClassyName );
                            if( target != null ) {
                                target.Message( "You can now build in world {0}&S (whitelisted by {1}&S).",
                                                world.ClassyName, player.ClassyName );
                            }
                            Logger.Log( LogType.UserActivity,
                                        "{0} added {1} to the build whitelist on world {2}",
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            player.Message( "{0}&S is already on the build whitelist of {1}",
                                            info.ClassyName, world.ClassyName );
                            break;
                    }

                    // Blacklisting individuals
                } else if( name.StartsWith( "-" ) ) {
                    if( name.Length == 1 ) {
                        CdWorldBuild.PrintUsage( player );
                        break;
                    }
                    PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches( player, name.Substring( 1 ) );
                    if( info == null ) return;

                    if( world.BuildSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooHigh ||
                        world.BuildSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooLow ) {
                        player.Message( "{0}&S is already barred from building in {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName );
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.BuildSecurity.Exclude( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is already on build blacklist of {1}",
                                            info.ClassyName, world.ClassyName );
                            break;

                        case PermissionOverride.None:
                            player.Message( "{0}&S is now barred from building in {1}",
                                            info.ClassyName, world.ClassyName );
                            if( target != null ) {
                                target.Message( "&WYou were barred by {0}&W from building in world {1}",
                                                player.ClassyName, world.ClassyName );
                            }
                            Logger.Log( LogType.UserActivity,
                                        "{0} added {1} to the build blacklist on world {2}",
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            if( world.BuildSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer on the build whitelist of {1}&S. " +
                                                "Player is still allowed to build (by rank).",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "You were removed from the build whitelist of world {0}&S by {1}&S. " +
                                                    "You are still allowed to build (by rank).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            } else {
                                player.Message( "{0}&S is no longer allowed to build in {1}",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "&WYou can no longer build in world {0}&W (removed from whitelist by {1}&W).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            }
                            Logger.Log( LogType.UserActivity,
                                        "{0} removed {1} from the build whitelist on world {2}",
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;
                    }

                    // Setting minimum rank
                } else {
                    Rank rank = RankManager.FindRank( name );
                    if( rank == null ) {
                        player.MessageNoRank( name );
                    } else if( !player.Info.Rank.AllowSecurityCircumvention &&
                               world.BuildSecurity.MinRank > rank &&
                               world.BuildSecurity.MinRank > player.Info.Rank ) {
                        player.Message( "&WYou must be ranked {0}&W+ to lower build restrictions for world {1}",
                                        world.BuildSecurity.MinRank.ClassyName, world.ClassyName );
                    } else {
                        // list players who are redundantly blacklisted
                        var exceptionList = world.BuildSecurity.ExceptionList;
                        PlayerInfo[] noLongerExcluded = exceptionList.Excluded.Where( excludedPlayer => excludedPlayer.Rank < rank ).ToArray();
                        if( noLongerExcluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be blacklisted on world {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerExcluded.JoinToClassyString() );
                        }

                        // list players who are redundantly whitelisted
                        PlayerInfo[] noLongerIncluded = exceptionList.Included.Where( includedPlayer => includedPlayer.Rank >= rank ).ToArray();
                        if( noLongerIncluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be whitelisted on world {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerIncluded.JoinToClassyString() );
                        }

                        // apply changes
                        world.BuildSecurity.MinRank = rank;
                        if( BlockDB.IsEnabledGlobally && world.BlockDB.AutoToggleIfNeeded() ) {
                            if( world.BlockDB.IsEnabled ) {
                                player.Message( "BlockDB is now auto-enabled on world {0}",
                                                world.ClassyName );
                            } else {
                                player.Message( "BlockDB is now auto-disabled on world {0}",
                                                world.ClassyName );
                            }
                        }
                        changesWereMade = true;
                        if( world.BuildSecurity.MinRank == RankManager.LowestRank ) {
                            Server.Message( "{0}&S allowed anyone to build on world {1}",
                                              player.ClassyName, world.ClassyName );
                        } else {
                            Server.Message( "{0}&S allowed only {1}+&S to build in world {2}",
                                              player.ClassyName, world.BuildSecurity.MinRank.ClassyName, world.ClassyName );
                        }
                        Logger.Log( LogType.UserActivity,
                                    "{0} set build rank for world {1} to {2}+",
                                    player.Name, world.Name, world.BuildSecurity.MinRank.Name );
                    }
                }
            } while( (name = cmd.Next()) != null );

            if( changesWereMade ) {
                WorldManager.SaveWorldList();
            }
        }

        #endregion


        #region WorldFlush

        static readonly CommandDescriptor CdWorldFlush = new CommandDescriptor {
            Name = "WFlush",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WFlush [WorldName]",
            Help = "Flushes the update buffer on specified map by causing players to rejoin. " +
                   "Makes cuboids and other draw commands finish REALLY fast.",
            Handler = WorldFlushHandler
        };

        static void WorldFlushHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();
            World world = player.World;

            if( worldName != null ) {
                world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                if( world == null ) return;

            } else if( world == null ) {
                player.Message( "When using /WFlush from console, you must specify a world name." );
                return;
            }

            Map map = world.Map;
            if( map == null ) {
                player.MessageNow( "WFlush: {0}&S has no updates to process.",
                                   world.ClassyName );
            } else {
                player.MessageNow( "WFlush: Flushing {0}&S ({1} blocks)...",
                                   world.ClassyName,
                                   map.UpdateQueueLength + map.DrawQueueBlockCount );
                world.Flush();
            }
        }

        #endregion


        #region WorldHide / WorldUnhide

        static readonly CommandDescriptor CdWorldHide = new CommandDescriptor {
            Name = "WHide",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WHide WorldName",
            Help = "Hides the specified world from the &H/Worlds&S list. " +
                   "Hidden worlds can be seen by typing &H/Worlds all",
            Handler = WorldHideHandler
        };

        static void WorldHideHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                CdWorldHide.PrintUsage( player );
                return;
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

            if( world.IsHidden ) {
                player.Message( "World \"{0}&S\" is already hidden.", world.ClassyName );
            } else {
                player.Message( "World \"{0}&S\" is now hidden.", world.ClassyName );
                world.IsHidden = true;
                WorldManager.SaveWorldList();
            }
        }


        static readonly CommandDescriptor CdWorldUnhide = new CommandDescriptor {
            Name = "WUnhide",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WUnhide WorldName",
            Help = "Unhides the specified world from the &H/Worlds&S list. " +
                   "Hidden worlds can be listed by typing &H/Worlds all",
            Handler = WorldUnhideHandler
        };

        static void WorldUnhideHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                CdWorldUnhide.PrintUsage( player );
                return;
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

            if( world.IsHidden ) {
                player.Message( "World \"{0}&S\" is no longer hidden.", world.ClassyName );
                world.IsHidden = false;
                WorldManager.SaveWorldList();
            } else {
                player.Message( "World \"{0}&S\" is not hidden.", world.ClassyName );
            }
        }

        #endregion


        #region WorldInfo

        static readonly CommandDescriptor CdWorldInfo = new CommandDescriptor {
            Name = "WInfo",
            Aliases = new[] { "mapinfo" },
            Category = CommandCategory.World | CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/WInfo [WorldName]",
            Help = "Shows information about a world: player count, map dimensions, permissions, etc." +
                   "If no WorldName is given, shows info for current world.",
            Handler = WorldInfoHandler
        };

        static void WorldInfoHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                if( player.World == null ) {
                    player.Message( "Please specify a world name when calling /WInfo from console." );
                    return;
                } else {
                    worldName = player.World.Name;
                }
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

            player.Message( "World {0}&S has {1} player(s) on.",
                            world.ClassyName,
                            world.CountVisiblePlayers( player ) );

            Map map = world.Map;

            // If map is not currently loaded, grab its header from disk
            if( map == null ) {
                try {
                    map = MapUtility.LoadHeader( Path.Combine( Paths.MapPath, world.MapFileName ) );
                } catch( Exception ex ) {
                    player.Message( "  Map information could not be loaded: {0}: {1}",
                                    ex.GetType().Name, ex.Message );
                }
            }

            if( map != null ) {
                player.Message( "  Map dimensions are {0} x {1} x {2}",
                                map.Width, map.Length, map.Height );
            }

            // Print access/build limits
            player.Message( "  " + world.AccessSecurity.GetDescription( world, "world", "accessed" ) );
            player.Message( "  " + world.BuildSecurity.GetDescription( world, "world", "modified" ) );

            // Print lock/unlock information
            if( world.IsLocked ) {
                player.Message( "  {0}&S was locked {1} ago by {2}",
                                world.ClassyName,
                                DateTime.UtcNow.Subtract( world.LockedDate ).ToMiniString(),
                                world.LockedBy );
            } else if( world.UnlockedBy != null ) {
                player.Message( "  {0}&S was unlocked {1} ago by {2}",
                                world.ClassyName,
                                DateTime.UtcNow.Subtract( world.UnlockedDate ).ToMiniString(),
                                world.UnlockedBy );
            }

            if( !String.IsNullOrEmpty( world.LoadedBy ) && world.LoadedOn != DateTime.MinValue ) {
                player.Message( "  {0}&S was created/loaded {1} ago by {2}",
                                world.ClassyName,
                                DateTime.UtcNow.Subtract( world.LoadedOn ).ToMiniString(),
                                world.LoadedByClassy );
            }

            if( !String.IsNullOrEmpty( world.MapChangedBy ) && world.MapChangedOn != DateTime.MinValue ) {
                player.Message( "  Map was last changed {0} ago by {1}",
                                DateTime.UtcNow.Subtract( world.MapChangedOn ).ToMiniString(),
                                world.MapChangedByClassy );
            }

            if( world.BlockDB.IsEnabled ) {
                if( world.BlockDB.EnabledState == YesNoAuto.Auto ) {
                    player.Message( "  BlockDB is enabled (auto) on {0}", world.ClassyName );
                } else {
                    player.Message( "  BlockDB is enabled on {0}", world.ClassyName );
                }
            } else {
                player.Message( "  BlockDB is disabled on {0}", world.ClassyName );
            }

            if( world.BackupInterval == TimeSpan.Zero ) {
                if( WorldManager.DefaultBackupInterval != TimeSpan.Zero ) {
                    player.Message( "  Periodic backups are disabled on {0}", world.ClassyName );
                }
            } else {
                player.Message( "  Periodic backups every {0}", world.BackupInterval.ToMiniString() );
            }
            if (world.RealisticEnv)
                player.Message("  Realistic ENV is enabled on this world");
        }

        #endregion


        #region WorldLoad

        static readonly CommandDescriptor CdWorldLoad = new CommandDescriptor {
            Name = "WLoad",
            Aliases = new[] { "wadd" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WLoad FileName [WorldName [BuildRank [AccessRank]]]",
            Help = "If WorldName parameter is not given, replaces the current world's map with the specified map. The old map is overwritten. " +
                   "If the world with the specified name exists, its map is replaced with the specified map file. " +
                   "Otherwise, a new world is created using the given name and map file. " +
                   "NOTE: For security reasons, you may only load files from the map folder. " +
                   "For a list of supported formats, see &H/Help WLoad Formats",
            HelpSections = new Dictionary<string, string>{
                { "formats",    "WLoad supported formats: fCraft FCM (versions 2, 3, and 4), MCSharp/MCZall/MCLawl (.lvl), " +
                                "D3 (.map), Classic (.dat), InDev (.mclevel), MinerCPP/LuaCraft (.dat), " +
                                "JTE (.gz), iCraft/Myne (directory-based), Opticraft (.save)." }
            },
            Handler = WorldLoadHandler
        };


        static void WorldLoadHandler( Player player, Command cmd ) {
            string fileName = cmd.Next();
            string worldName = cmd.Next();

            if( worldName == null && player.World == null ) {
                player.Message( "When using /WLoad from console, you must specify the world name." );
                return;
            }

            if( fileName == null ) {
                // No params given at all
                CdWorldLoad.PrintUsage( player );
                return;
            }

            string fullFileName = WorldManager.FindMapFile( player, fileName );
            if( fullFileName == null ) return;

            // Loading map into current world
            if( worldName == null ) {
                if( !cmd.IsConfirmed ) {
                    player.Confirm( cmd, "Replace THIS MAP with \"{0}\"?", fileName );
                    return;
                }
                Map map;
                try {
                    map = MapUtility.Load( fullFileName );
                } catch( Exception ex ) {
                    player.MessageNow( "Could not load specified file: {0}: {1}", ex.GetType().Name, ex.Message );
                    return;
                }
                World world = player.World;

                // Loading to current world
                world.MapChangedBy = player.Name;
                world.ChangeMap( map );

                world.Players.Message( player, "{0}&S loaded a new map for this world.",
                                              player.ClassyName );
                player.MessageNow( "New map loaded for the world {0}", world.ClassyName );

                Logger.Log( LogType.UserActivity,
                            "{0} loaded new map for world \"{1}\" from {2}",
                            player.Name, world.Name, fileName );


            } else {
                // Loading to some other (or new) world
                if( !World.IsValidName( worldName ) ) {
                    player.MessageInvalidWorldName( worldName );
                    return;
                }

                string buildRankName = cmd.Next();
                string accessRankName = cmd.Next();
                Rank buildRank = RankManager.DefaultBuildRank;
                Rank accessRank = null;
                if( buildRankName != null ) {
                    buildRank = RankManager.FindRank( buildRankName );
                    if( buildRank == null ) {
                        player.MessageNoRank( buildRankName );
                        return;
                    }
                    if( accessRankName != null ) {
                        accessRank = RankManager.FindRank( accessRankName );
                        if( accessRank == null ) {
                            player.MessageNoRank( accessRankName );
                            return;
                        }
                    }
                }

                // Retype world name, if needed
                if( worldName == "-" ) {
                    if( player.LastUsedWorldName != null ) {
                        worldName = player.LastUsedWorldName;
                    } else {
                        player.Message( "Cannot repeat world name: you haven't used any names yet." );
                        return;
                    }
                }

                lock( WorldManager.SyncRoot ) {
                    World world = WorldManager.FindWorldExact( worldName );
                    if( world != null ) {
                        player.LastUsedWorldName = world.Name;
                        // Replacing existing world's map
                        if( !cmd.IsConfirmed ) {
                            player.Confirm( cmd, "Replace map for {0}&S with \"{1}\"?",
                                            world.ClassyName, fileName );
                            return;
                        }

                        Map map;
                        try {
                            map = MapUtility.Load( fullFileName );
                        } catch( Exception ex ) {
                            player.MessageNow( "Could not load specified file: {0}: {1}", ex.GetType().Name, ex.Message );
                            return;
                        }

                        try {
                            world.MapChangedBy = player.Name;
                            world.ChangeMap( map );
                        } catch( WorldOpException ex ) {
                            Logger.Log( LogType.Error,
                                        "Could not complete WorldLoad operation: {0}", ex.Message );
                            player.Message( "&WWLoad: {0}", ex.Message );
                            return;
                        }

                        world.Players.Message( player, "{0}&S loaded a new map for the world {1}",
                                               player.ClassyName, world.ClassyName );
                        player.MessageNow( "New map for the world {0}&S has been loaded.", world.ClassyName );
                        Logger.Log( LogType.UserActivity,
                                    "{0} loaded new map for world \"{1}\" from {2}",
                                    player.Name, world.Name, fullFileName );

                    } else {
                        // Adding a new world
                        string targetFullFileName = Path.Combine( Paths.MapPath, worldName + ".fcm" );
                        if( !cmd.IsConfirmed &&
                            File.Exists( targetFullFileName ) && // target file already exists
                            !Paths.Compare( targetFullFileName, fullFileName ) ) {
                            // and is different from sourceFile
                            player.Confirm( cmd,
                                            "A map named \"{0}\" already exists, and will be overwritten with \"{1}\".",
                                            Path.GetFileName( targetFullFileName ), Path.GetFileName( fullFileName ) );
                            return;
                        }

                        Map map;
                        try {
                            map = MapUtility.Load( fullFileName );
                        } catch( Exception ex ) {
                            player.MessageNow( "Could not load \"{0}\": {1}: {2}",
                                               fileName, ex.GetType().Name, ex.Message );
                            return;
                        }

                        World newWorld;
                        try {
                            newWorld = WorldManager.AddWorld( player, worldName, map, false );
                        } catch( WorldOpException ex ) {
                            player.Message( "WLoad: {0}", ex.Message );
                            return;
                        }

                        if( newWorld == null ) {
                            player.MessageNow( "Failed to create a new world." );
                            return;
                        }

                        player.LastUsedWorldName = worldName;
                        newWorld.BuildSecurity.MinRank = buildRank;
                        if( accessRank == null ) {
                            newWorld.AccessSecurity.ResetMinRank();
                        } else {
                            newWorld.AccessSecurity.MinRank = accessRank;
                        }
                        newWorld.BlockDB.AutoToggleIfNeeded();
                        if( BlockDB.IsEnabledGlobally && newWorld.BlockDB.IsEnabled ) {
                            player.Message( "BlockDB is now auto-enabled on world {0}", newWorld.ClassyName );
                        }
                        newWorld.LoadedBy = player.Name;
                        newWorld.LoadedOn = DateTime.UtcNow;
                        Server.Message( "{0}&S created a new world named {1}",
                                        player.ClassyName, newWorld.ClassyName );
                        Logger.Log( LogType.UserActivity,
                                    "{0} created a new world named \"{1}\" (loaded from \"{2}\")",
                                    player.Name, worldName, fileName );
                        WorldManager.SaveWorldList();
                        player.MessageNow( "Access permission is {0}+&S, and build permission is {1}+",
                                           newWorld.AccessSecurity.MinRank.ClassyName,
                                           newWorld.BuildSecurity.MinRank.ClassyName );
                    }
                }
            }

            Server.RequestGC();
        }

        #endregion


        #region WorldMain

        static readonly CommandDescriptor CdWorldMain = new CommandDescriptor {
            Name = "WMain",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WMain [@RankName] [WorldName]",
            Help = "Sets the specified world as the new main world. " +
                   "Main world is what newly-connected players join first. " +
                   "You can specify a rank name to set a different starting world for that particular rank.",
            Handler = WorldMainHandler
        };

        static void WorldMainHandler( Player player, Command cmd ) {
            string param = cmd.Next();
            if( param == null ) {
                player.Message( "Main world is {0}", WorldManager.MainWorld.ClassyName );
                var mainedRanks = RankManager.Ranks
                                             .Where( r => r.MainWorld != null && r.MainWorld != WorldManager.MainWorld );
                if( mainedRanks.Count() > 0 ) {
                    player.Message( "Rank mains: {0}",
                                    mainedRanks.JoinToString( r => String.Format( "{0}&S for {1}&S",
                                        // ReSharper disable PossibleNullReferenceException
                                                                                  r.MainWorld.ClassyName,
                                        // ReSharper restore PossibleNullReferenceException
                                                                                  r.ClassyName ) ) );
                }
                return;
            }

            if( param.StartsWith( "@" ) ) {
                string rankName = param.Substring( 1 );
                Rank rank = RankManager.FindRank( rankName );
                if( rank == null ) {
                    player.MessageNoRank( rankName );
                    return;
                }
                string worldName = cmd.Next();
                if( worldName == null ) {
                    if( rank.MainWorld != null ) {
                        player.Message( "Main world for rank {0}&S is {1}",
                                        rank.ClassyName,
                                        rank.MainWorld.ClassyName );
                    } else {
                        player.Message( "Main world for rank {0}&S is {1}&S (default)",
                                        rank.ClassyName,
                                        WorldManager.MainWorld.ClassyName );
                    }
                } else {
                    World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                    if( world != null ) {
                        SetRankMainWorld( player, rank, world );
                    }
                }

            } else {
                World world = WorldManager.FindWorldOrPrintMatches( player, param );
                if( world != null ) {
                    SetMainWorld( player, world );
                }
            }
        }


        static void SetRankMainWorld( Player player, Rank rank, World world ) {
            if( world == rank.MainWorld ) {
                player.Message( "World {0}&S is already set as main for {1}&S.",
                                world.ClassyName, rank.ClassyName );
                return;
            }

            if( world == WorldManager.MainWorld ) {
                if( rank.MainWorld == null ) {
                    player.Message( "The main world for rank {0}&S is already {1}&S (default).",
                                    rank.ClassyName, world.ClassyName );
                } else {
                    rank.MainWorld = null;
                    WorldManager.SaveWorldList();
                    Server.Message( "&SPlayer {0}&S has reset the main world for rank {1}&S.",
                                    player.ClassyName, rank.ClassyName );
                    Logger.Log( LogType.UserActivity,
                                "{0} reset the main world for rank {1}.",
                                player.Name, rank.Name );
                }
                return;
            }

            if( world.AccessSecurity.MinRank > rank ) {
                player.Message( "World {0}&S requires {1}+&S to join, so it cannot be used as the main world for rank {2}&S.",
                                world.ClassyName, world.AccessSecurity.MinRank, rank.ClassyName );
                return;
            }

            rank.MainWorld = world;
            WorldManager.SaveWorldList();
            Server.Message( "&SPlayer {0}&S designated {1}&S to be the main world for rank {2}",
                            player.ClassyName, world.ClassyName, rank.ClassyName );
            Logger.Log( LogType.UserActivity,
                        "{0} set {1} to be the main world for rank {2}.",
                        player.Name, world.Name, rank.Name );
        }


        static void SetMainWorld( Player player, World world ) {
            if( world == WorldManager.MainWorld ) {
                player.Message( "World {0}&S is already set as main.", world.ClassyName );

            } else if( !player.Info.Rank.AllowSecurityCircumvention && !player.CanJoin( world ) ) {
                // Prevent players from exploiting /WMain to gain access to restricted maps
                switch( world.AccessSecurity.CheckDetailed( player.Info ) ) {
                    case SecurityCheckResult.RankTooHigh:
                    case SecurityCheckResult.RankTooLow:
                        player.Message( "You are not allowed to set {0}&S as the main world (by rank).", world.ClassyName );
                        return;
                    case SecurityCheckResult.BlackListed:
                        player.Message( "You are not allowed to set {0}&S as the main world (blacklisted).", world.ClassyName );
                        return;
                }

            } else {
                if( world.AccessSecurity.HasRestrictions ) {
                    world.AccessSecurity.Reset();
                    player.Message( "The main world cannot have access restrictions. " +
                                    "All access restrictions were removed from world {0}",
                                    world.ClassyName );
                }

                try {
                    WorldManager.MainWorld = world;
                } catch( WorldOpException ex ) {
                    player.Message( ex.Message );
                    return;
                }

                WorldManager.SaveWorldList();

                Server.Message( "{0}&S set {1}&S to be the main world.",
                                  player.ClassyName, world.ClassyName );
                Logger.Log( LogType.UserActivity,
                            "{0} set {1} to be the main world.",
                            player.Name, world.Name );
            }
        }

        #endregion


        #region WorldRename

        static readonly CommandDescriptor CdWorldRename = new CommandDescriptor {
            Name = "WRename",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WRename OldName NewName",
            Help = "Changes the name of a world. Does not require any reloading.",
            Handler = WorldRenameHandler
        };

        static void WorldRenameHandler( Player player, Command cmd ) {
            string oldName = cmd.Next();
            string newName = cmd.Next();
            if( oldName == null || newName == null ) {
                CdWorldRename.PrintUsage( player );
                return;
            }

            World oldWorld = WorldManager.FindWorldOrPrintMatches( player, oldName );
            if( oldWorld == null ) return;
            oldName = oldWorld.Name;

            if( !World.IsValidName( newName ) ) {
                player.MessageInvalidWorldName( newName );
                return;
            }

            World newWorld = WorldManager.FindWorldExact( newName );
            if( !cmd.IsConfirmed && newWorld != null && newWorld != oldWorld ) {
                player.Confirm( cmd, "A world named {0}&S already exists. Replace it?", newWorld.ClassyName );
                return;
            }

            if( !cmd.IsConfirmed && File.Exists( Path.Combine( Paths.MapPath, newName + ".fcm" ) ) ) {
                player.Confirm( cmd, "Renaming this world will overwrite an existing map file \"{0}.fcm\".", newName );
                return;
            }

            try {
                WorldManager.RenameWorld( oldWorld, newName, true, true );
            } catch( WorldOpException ex ) {
                switch( ex.ErrorCode ) {
                    case WorldOpExceptionCode.NoChangeNeeded:
                        player.MessageNow( "WRename: World is already named \"{0}\"", oldName );
                        return;
                    case WorldOpExceptionCode.DuplicateWorldName:
                        player.MessageNow( "WRename: Another world named \"{0}\" already exists.", newName );
                        return;
                    case WorldOpExceptionCode.InvalidWorldName:
                        player.MessageNow( "WRename: Invalid world name: \"{0}\"", newName );
                        return;
                    case WorldOpExceptionCode.MapMoveError:
                        player.MessageNow( "WRename: World \"{0}\" was renamed to \"{1}\", but the map file could not be moved due to an error: {2}",
                                            oldName, newName, ex.InnerException );
                        return;
                    default:
                        player.MessageNow( "&WWRename: Unexpected error renaming world \"{0}\": {1}", oldName, ex.Message );
                        Logger.Log( LogType.Error,
                                    "WorldCommands.Rename: Unexpected error while renaming world {0} to {1}: {2}",
                                    oldWorld.Name, newName, ex );
                        return;
                }
            }

            player.LastUsedWorldName = newName;
            WorldManager.SaveWorldList();
            Logger.Log( LogType.UserActivity,
                        "{0} renamed the world \"{1}\" to \"{2}\".",
                        player.Name, oldName, newName );
            Server.Message( "{0}&S renamed the world \"{1}\" to \"{2}\"",
                              player.ClassyName, oldName, newName );
        }

        #endregion


        #region WorldSave

        static readonly CommandDescriptor CdWorldSave = new CommandDescriptor {
            Name = "WSave",
            Aliases = new[] { "save" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WSave FileName &Sor&H /WSave WorldName FileName",
            Help = "Saves a map copy to a file with the specified name. " +
                   "The \".fcm\" file extension can be omitted. " +
                   "If a file with the same name already exists, it will be overwritten.",
            Handler = WorldSaveHandler
        };

        static void WorldSaveHandler( Player player, Command cmd ) {
            string p1 = cmd.Next(), p2 = cmd.Next();
            if( p1 == null ) {
                CdWorldSave.PrintUsage( player );
                return;
            }

            World world = player.World;
            string fileName;
            if( p2 == null ) {
                fileName = p1;
                if( world == null ) {
                    player.Message( "When called from console, /wsave requires WorldName. See \"/Help save\" for details." );
                    return;
                }
            } else {
                world = WorldManager.FindWorldOrPrintMatches( player, p1 );
                if( world == null ) return;
                fileName = p2;
            }

            // normalize the path
            fileName = fileName.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar );
            if( fileName.EndsWith( "/" ) && fileName.EndsWith( @"\" ) ) {
                fileName += world.Name + ".fcm";
            } else if( !fileName.ToLower().EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase ) ) {
                fileName += ".fcm";
            }
            if( !Paths.IsValidPath( fileName ) ) {
                player.Message( "Invalid filename." );
                return;
            }
            string fullFileName = Path.Combine( Paths.MapPath, fileName );
            if( !Paths.Contains( Paths.MapPath, fullFileName ) ) {
                player.MessageUnsafePath();
                return;
            }

            // Ask for confirmation if overwriting
            if( File.Exists( fullFileName ) ) {
                FileInfo targetFile = new FileInfo( fullFileName );
                FileInfo sourceFile = new FileInfo( world.MapFileName );
                if( !targetFile.FullName.Equals( sourceFile.FullName, StringComparison.OrdinalIgnoreCase ) ) {
                    if( !cmd.IsConfirmed ) {
                        player.Confirm( cmd, "Target file \"{0}\" already exists, and will be overwritten.", targetFile.Name );
                        return;
                    }
                }
            }

            // Create the target directory if it does not exist
            string dirName = fullFileName.Substring( 0, fullFileName.LastIndexOf( Path.DirectorySeparatorChar ) );
            if( !Directory.Exists( dirName ) ) {
                Directory.CreateDirectory( dirName );
            }

            player.MessageNow( "Saving map to {0}", fileName );

            const string mapSavingErrorMessage = "Map saving failed. See server logs for details.";
            Map map = world.Map;
            if( map == null ) {
                if( File.Exists( world.MapFileName ) ) {
                    try {
                        File.Copy( world.MapFileName, fullFileName, true );
                    } catch( Exception ex ) {
                        Logger.Log( LogType.Error,
                                    "WorldCommands.WorldSave: Error occured while trying to copy an unloaded map: {0}", ex );
                        player.Message( mapSavingErrorMessage );
                    }
                } else {
                    Logger.Log( LogType.Error,
                                "WorldCommands.WorldSave: Map for world \"{0}\" is unloaded, and file does not exist.",
                                world.Name );
                    player.Message( mapSavingErrorMessage );
                }
            } else if( map.Save( fullFileName ) ) {
                player.Message( "Map saved succesfully." );
            } else {
                Logger.Log( LogType.Error,
                            "WorldCommands.WorldSave: Saving world \"{0}\" failed.", world.Name );
                player.Message( mapSavingErrorMessage );
            }
        }

        #endregion


        #region WorldUnload

        static readonly CommandDescriptor CdWorldUnload = new CommandDescriptor {
            Name = "WUnload",
            Aliases = new[] { "wremove", "wdelete" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WUnload WorldName",
            Help = "Removes the specified world from the world list, and moves all players from it to the main world. " +
                   "The main world itself cannot be removed with this command. You will need to delete the map file manually.",
            Handler = WorldUnloadHandler
        };

        static void WorldUnloadHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                CdWorldUnload.PrintUsage( player );
                return;
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

            try {
                WorldManager.RemoveWorld( world );
            } catch( WorldOpException ex ) {
                switch( ex.ErrorCode ) {
                    case WorldOpExceptionCode.CannotDoThatToMainWorld:
                        player.MessageNow( "&WWorld {0}&W is set as the main world. " +
                                           "Assign a new main world before deleting this one.",
                                           world.ClassyName );
                        return;
                    case WorldOpExceptionCode.WorldNotFound:
                        player.MessageNow( "&WWorld {0}&W is already unloaded.",
                                           world.ClassyName );
                        return;
                    default:
                        player.MessageNow( "&WUnexpected error occured while unloading world {0}&W: {1}",
                                           world.ClassyName, ex.GetType().Name );
                        Logger.Log( LogType.Error,
                                    "WorldCommands.WorldUnload: Unexpected error while unloading world {0}: {1}",
                                    world.Name, ex );
                        return;
                }
            }

            WorldManager.SaveWorldList();
            Server.Message( player,
                            "{0}&S removed {1}&S from the world list.",
                            player.ClassyName, world.ClassyName );
            player.Message( "Removed {0}&S from the world list. You can now delete the map file ({1}.fcm) manually.",
                            world.ClassyName, world.Name );
            Logger.Log( LogType.UserActivity,
                        "{0} removed \"{1}\" from the world list.",
                        player.Name, worldName );

            Server.RequestGC();
        }

        #endregion
    }
}