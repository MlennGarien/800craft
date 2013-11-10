// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using fCraft.Drawing;
using fCraft.MapConversion;
using fCraft.Portals;
using JetBrains.Annotations;

namespace fCraft {

    /// <summary> Contains commands related to world management. </summary>
    internal static class WorldCommands {
        private const int WorldNamesPerPage = 30;

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

            CommandManager.RegisterCommand( CdWorldInfo );
            CommandManager.RegisterCommand( CdWorldLoad );
            CommandManager.RegisterCommand( CdWorldMain );
            CommandManager.RegisterCommand( CdWorldRename );
            CommandManager.RegisterCommand( CdWorldSave );
            CommandManager.RegisterCommand( CdWorldUnload );

            CommandManager.RegisterCommand( CdRealm );
            CommandManager.RegisterCommand( CdPortal );
            CommandManager.RegisterCommand( CdWorldSearch );
            SchedulerTask timeCheckR = Scheduler.NewTask( TimeCheck ).RunForever( TimeSpan.FromSeconds( 120 ) );
            CommandManager.RegisterCommand( CdPhysics );
            CommandManager.RegisterCommand( CdWorldSet );
            CommandManager.RegisterCommand( CdMessageBlock );
            //CommandManager.RegisterCommand( CdFeed );
            Player.JoinedWorld += FeedSettings.PlayerJoiningWorld;
            Player.PlacingBlock += FeedSettings.PlayerPlacingBlock;
        }

        #region 800Craft

        /*        ----
        Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com>
        All rights reserved.

        Redistribution and use in source and binary forms, with or without
        modification, are permitted provided that the following conditions are met:
         * Redistributions of source code must retain the above copyright
              notice, this list of conditions and the following disclaimer.
            * Redistributions in binary form must reproduce the above copyright
             notice, this list of conditions and the following disclaimer in the
             documentation and/or other materials provided with the distribution.
            * Neither the name of 800Craft or the names of its
             contributors may be used to endorse or promote products derived from this
             software without specific prior written permission.

        THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
        ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
        WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
        DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
        DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
        (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
        LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
        ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
        (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
        SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
        ----*/

        private static readonly CommandDescriptor CdFeed = new CommandDescriptor {
            Name = "Feed",
            Category = CommandCategory.Building,
            Permissions = new Permission[] { Permission.EditPlayerDB },
            RepeatableSelection = true,
            IsConsoleSafe = false,
            Help = "/Feed then click two positions. Will create a 60 block feed in the direction of the 2nd block",
            Handler = FeedHandler,
        };

        private static void FeedHandler( Player player, Command cmd ) {
            string Option = cmd.Next();
            if ( Option == null ) {
                player.Message( "Argument cannot be null, try /Feed <create | remove | list>" );
                return;
            }

            if ( Option.ToLower() == "create" ) {
                player.Message( "Feed: Click 2 blocks or use &H/Mark&S to set direction." );
                player.SelectionStart( 2, FeedCallback, Option, Permission.Draw );
                return;
            }
            if ( Option.ToLower() == "list" ) {
                FeedData[] list = player.World.Feeds.Values.OrderBy( feed => feed.Id ).ToArray();
                if ( list.Length == 0 ) {
                    player.Message( "No feeds running." );
                } else {
                    player.Message( "There are {0} feeds running:", list.Length );
                    foreach ( FeedData data in list ) {
                        player.Message( "  #{0} ({1},{2},{3}) World: {4}",
                                        data.Id, data.StartPos.X, data.StartPos.Y, data.StartPos.Z, data.world.ClassyName );
                    }
                }
                return;
            }

            if ( Option.ToLower() == "remove" || Option.ToLower() == "stop" ) {
                int Id;
                if ( cmd.NextInt( out Id ) ) {
                    FeedData data = FeedData.FindFeedById( Id, player.World );
                    if ( data == null ) {
                        player.Message( "Given feed (#{0}) does not exist.", Id );
                    } else {
                        data.started = false;
                        FeedData.RemoveFeedFromList( data, player.World );
                        player.Message( "Feed #" + Id + " has been removed" );
                    }
                } else {
                    player.Message( "&WUnable to remove any feeds. Try /Feed remove <ID>" );
                }
                return;
            } else {
                player.Message( "&WUnknown argument. Check /Help Feed" );
                return;
            }
        }

        private static void FeedCallback( Player player, Vector3I[] marks, object tag ) {
            Direction direction = DirectionFinder.GetDirection( marks );
            if ( Math.Abs( marks[1].X - marks[0].X ) > Math.Abs( marks[1].Y - marks[0].Y ) ) {
                if ( marks[0].X < marks[1].X ) {
                    direction = Direction.one;
                } else {
                    direction = Direction.two;
                }
            } else if ( Math.Abs( marks[1].X - marks[0].X ) < Math.Abs( marks[1].Y - marks[0].Y ) ) {
                if ( marks[0].Y < marks[1].Y ) {
                    direction = Direction.three;
                } else {
                    direction = Direction.four;
                }
            } else
                return;
            System.Drawing.Bitmap bmp = fCraft.Properties.Resources.font;
            var data = new FeedData( Block.Lava, marks[0], bmp, player.World, direction, player ) {StartPos = marks[0]};
            int x1 = 0, y1 = 0, z1 = 0;
            switch ( direction ) {
                case Direction.one:
                    for ( int x = data.StartPos.X; x < data.StartPos.X + 60; x++ ) {
                        for ( int z = data.StartPos.Z; z < data.StartPos.Z + 9; z++ ) {
                            player.World.Map.QueueUpdate( new BlockUpdate( null, ( short )x, ( short )data.StartPos.Y, ( short )z, Block.Black ) );
                            x1 = x;
                            z1 = z;
                        }
                    }
                    data.EndPos = new Vector3I( x1, marks[0].Y, z1 );
                    data.FinishPos = new Vector3I( x1, marks[0].Y, z1 );

                    break;

                case Direction.two:
                    for ( int x = data.StartPos.X; x > data.StartPos.X - 60; x-- ) {
                        for ( int z = data.StartPos.Z; z < data.StartPos.Z + 9; z++ ) {
                            player.World.Map.QueueUpdate( new BlockUpdate( null, ( short )x, ( short )data.StartPos.Y, ( short )z, Block.Black ) );
                            x1 = x;
                            z1 = z;
                        }
                    }
                    data.EndPos = new Vector3I( x1, marks[0].Y, z1 );
                    data.FinishPos = new Vector3I( x1, marks[0].Y, z1 );
                    break;

                case Direction.three:
                    for ( int y = data.StartPos.Y; y < data.StartPos.Y + 60; y++ ) {
                        for ( int z = data.StartPos.Z; z < data.StartPos.Z + 9; z++ ) {
                            player.World.Map.QueueUpdate( new BlockUpdate( null, ( short )data.StartPos.X, ( short )y, ( short )z, Block.Black ) );
                            y1 = y;
                            z1 = z;
                        }
                    }
                    data.EndPos = new Vector3I( marks[0].X, y1, z1 );
                    data.FinishPos = new Vector3I( marks[0].X, y1, z1 );
                    break;

                case Direction.four:
                    for ( int y = data.StartPos.Y; y > data.StartPos.Y - 60; y-- ) {
                        for ( int z = data.StartPos.Z; z < data.StartPos.Z + 9; z++ ) {
                            player.World.Map.QueueUpdate( new BlockUpdate( null, ( short )data.StartPos.X, ( short )y, ( short )z, Block.Black ) );
                            y1 = y;
                            z1 = z;
                        }
                    }
                    data.EndPos = new Vector3I( marks[0].X, y1, z1 );
                    data.FinishPos = new Vector3I( marks[0].X, y1, z1 );
                    break;
            }
        }

        #region Physics

        private static readonly CommandDescriptor CdPhysics = new CommandDescriptor {
            Name = "Physics",
            Category = CommandCategory.World,
            Permissions = new Permission[] { Permission.Physics },
            IsConsoleSafe = false,
            Usage = "/Physics <TNT | Fireworks | Water | Plant | Sand | Gun | All> <On / Off>",
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
                { "gun",       "&H/Physics gun on/off \n&S" +
                                "Turns gun physics on / off in the current world"},
                { "all",     "&H/Physics all on/off \n&S" +
                                "Turns all physics on / off in the current world"},
            },
            Handler = PhysicsHandler
        };

        private static void PhysicsHandler( Player player, Command cmd ) {
            string option = cmd.Next();
            World world = player.World;
            if ( option == null ) {
                CdPhysics.PrintUsage( player );
                return;
            }
            string NextOp = cmd.Next();
            if ( NextOp == null ) {
                CdPhysics.PrintUsage( player );
                return;
            }
            switch ( option.ToLower() ) {
                case "tnt":
                    if ( NextOp.ToLower() == "on" ) {
                        world.EnableTNTPhysics( player, true );
                        return;
                    }
                    if ( NextOp.ToLower() == "off" ) {
                        world.DisableTNTPhysics( player, true );
                        return;
                    }
                    break;

                case "gun":
                    if ( NextOp.ToLower() == "on" ) {
                        world.EnableGunPhysics( player, true );
                        return;
                    }
                    if ( NextOp.ToLower() == "off" ) {
                        world.DisableGunPhysics( player, true );
                        return;
                    }
                    break;

                case "plant":
                    if ( NextOp.ToLower() == "off" ) {
                        world.DisablePlantPhysics( player, true );
                        return;
                    }
                    if ( NextOp.ToLower() == "on" ) {
                        world.EnablePlantPhysics( player, true );
                        return;
                    }
                    break;

                case "fireworks":
                case "firework":
                    if ( NextOp.ToLower() == "on" ) {
                        world.EnableFireworkPhysics( player, true );
                        return;
                    }
                    if ( NextOp.ToLower() == "off" ) {
                        world.DisableFireworkPhysics( player, true );
                        return;
                    }
                    break;

                case "sand":
                    if ( NextOp.ToLower() == "on" ) {
                        world.EnableSandPhysics( player, true );
                        return;
                    }
                    if ( NextOp.ToLower() == "off" ) {
                        world.DisableSandPhysics( player, true );
                        return;
                    }
                    break;

                case "water":
                    if ( NextOp.ToLower() == "on" ) {
                        world.EnableWaterPhysics( player, true );
                        return;
                    }
                    if ( NextOp.ToLower() == "off" ) {
                        world.DisableWaterPhysics( player, true );
                        return;
                    }
                    break;

                case "all":
                    switch (NextOp.ToLower())
                    {
                        case "on":
                            if ( !world.tntPhysics ) {
                                world.EnableTNTPhysics( player, false );
                            }
                            if ( !world.sandPhysics ) {
                                world.EnableSandPhysics( player, false );
                            }
                            if ( !world.fireworkPhysics ) {
                                world.EnableFireworkPhysics( player, false );
                            }
                            if ( !world.waterPhysics ) {
                                world.EnableWaterPhysics( player, false );
                            }
                            if ( !world.plantPhysics ) {
                                world.EnablePlantPhysics( player, false );
                            }
                            if ( !world.gunPhysics ) {
                                world.EnableGunPhysics( player, false );
                            }
                            Server.Players.Message( "{0}&S turned ALL Physics on for {1}", player.ClassyName, world.ClassyName );
                            Logger.Log( LogType.SystemActivity, "{0} turned ALL Physics on for {1}", player.Name, world.Name );
                            break;
                        case "off":
                            if ( world.tntPhysics ) {
                                world.DisableTNTPhysics( player, false );
                            }
                            if ( world.sandPhysics ) {
                                world.DisableSandPhysics( player, false );
                            }
                            if ( world.fireworkPhysics ) {
                                world.DisableFireworkPhysics( player, false );
                            }
                            if ( world.waterPhysics ) {
                                world.DisableWaterPhysics( player, false );
                            }
                            if ( world.plantPhysics ) {
                                world.DisablePlantPhysics( player, false );
                            }
                            if ( world.gunPhysics ) {
                                world.DisableGunPhysics( player, false );
                            }
                            Server.Players.Message( "{0}&S turned ALL Physics off for {1}", player.ClassyName, world.ClassyName );
                            Logger.Log( LogType.SystemActivity, "{0} turned ALL Physics off for {1}", player.Name, world.Name );
                            break;
                    }
                    break;

                default:
                    CdPhysics.PrintUsage( player );
                    break;
            }
            WorldManager.SaveWorldList();
        }

        #endregion Physics

        #region MessageBlocks

        private static readonly CommandDescriptor CdMessageBlock = new CommandDescriptor {
            Name = "MessageBlock",
            Aliases = new[] { "mb" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.ManageMessageBlocks },
            IsConsoleSafe = false,
            Usage = "/MessageBlock [add | remove | info | list | test]",
            Help = "Create and controls a MessageBlock, options are: add, remove, list, info, test\n&S" +
                   "See &H/Help MessageBlock <option>&S for details about each option.",
            HelpSections = new Dictionary<string, string>() {
                { "add",     "&H/MessageBlock add [Your Message Goes here]\n&S" +
                                "Adds a MessageBlock with a custom message. When a player walks in" +
                                 " radius of a message block the message inside is shown to them."},
                { "remove",     "&H/MessageBlock remove MessageBlock1\n&S" +
                                "Removes MessageBlock with name 'MessageBlock1'."},
                { "list",       "&H/MessageBlock list\n&S" +
                                "Gives you a list of MessageBlocks in the current world."},
                { "info",       "&H/MessageBlock info MB1\n&S" +
                                "Gives you information of MessageBlock with name 'MB1'."},
                {"test",        "&HTests if a block is a message block"},
            },
            Handler = MessageBlock
        };

        private static void MessageBlock( Player player, Command cmd ) {
            string option = cmd.Next();
            if ( option == null ) {
                CdMessageBlock.PrintUsage( player );
                return;
            } else if ( option.ToLower() == "add" || option.ToLower() == "create" ) {
                string Message = cmd.NextAll();
                player.SelectionStart( 1, MessageBlockAdd, Message, CdMessageBlock.Permissions );
                player.Message( "MessageBlock: Place a block or type /mark to use your location." );
                return;
            } else if ( option.ToLower().Equals( "remove" ) || option.ToLower().Equals( "rd" ) ) {
                string MessageBlockName = cmd.Next();

                if ( MessageBlockName == null ) {
                    player.Message( "No MessageBlock name specified." );
                } else {
                    if ( player.World.Map.MessageBlocks != null && player.World.Map.MessageBlocks.Count > 0 ) {
                        bool found = false;
                        MessageBlock MessageBlockFound = null;

                        lock ( player.World.Map.MessageBlocks.SyncRoot ) {
                            foreach ( MessageBlock MessageBlock in player.World.Map.MessageBlocks ) {
                                if ( MessageBlock.Name.Equals( MessageBlockName, StringComparison.OrdinalIgnoreCase ) ) {
                                    MessageBlockFound = MessageBlock;
                                    found = true;
                                    break;
                                }
                            }

                            if ( !found ) {
                                player.Message( "Could not find MessageBlock by name {0}.", MessageBlockName );
                            } else {
                                MessageBlockFound.Remove( player );
                                player.Message( "MessageBlock was removed." );
                            }
                        }
                    } else {
                        player.Message( "Could not find MessageBlock as this world doesn't contain a MessageBlock." );
                    }
                }
            } else if ( option.Equals( "info", StringComparison.OrdinalIgnoreCase ) ) {
                string MessageBlockName = cmd.Next();

                if ( MessageBlockName == null ) {
                    player.Message( "No MessageBlock name specified." );
                } else {
                    if ( player.World.Map.MessageBlocks != null && player.World.Map.MessageBlocks.Count > 0 ) {
                        bool found = false;

                        lock ( player.World.Map.MessageBlocks.SyncRoot ) {
                            for (int index = 0; index < player.World.Map.MessageBlocks.Count; index++)
                            {
                                MessageBlock MessageBlock = (MessageBlock) player.World.Map.MessageBlocks[index];
                                if (MessageBlock.Name.Equals(MessageBlockName, StringComparison.OrdinalIgnoreCase))
                                {
                                    World MessageBlockWorld = WorldManager.FindWorldExact(MessageBlock.World);
                                    player.Message("MessageBlock '{0}&S' was created by {1}&S at {2}",
                                        MessageBlock.Name, MessageBlock.Creator, MessageBlock.Created);
                                    found = true;
                                }
                            }
                        }
                        if ( !found ) {
                            player.Message( "Could not find MessageBlock by name {0}.", MessageBlockName );
                        }
                    } else {
                        player.Message( "Could not find MessageBlock as this world doesn't contain a MessageBlock." );
                    }
                }
            } else if ( option.ToLower().Equals( "test" ) ) {
                player.SelectionStart( 1, MessageBlockTestCallback, null, CdMessageBlock.Permissions );
                player.Message( "MessageBlockTest: Click a block or type /mark to use your location." );
            } else if ( option.ToLower().Equals( "list" ) ) {
                if ( player.World.Map.MessageBlocks == null || player.World.Map.MessageBlocks.Count == 0 ) {
                    player.Message( "There are no MessageBlocks in {0}&S.", player.World.ClassyName );
                } else {
                    String[] MessageBlockNames = new String[player.World.Map.MessageBlocks.Count];
                    System.Text.StringBuilder output = new System.Text.StringBuilder( "There are " + player.World.Map.MessageBlocks.Count + " MessageBlocks in " + player.World.ClassyName + "&S: " );

                    for ( int i = 0; i < player.World.Map.MessageBlocks.Count; i++ ) {
                        MessageBlockNames[i] = ( ( MessageBlock )player.World.Map.MessageBlocks[i] ).Name;
                    }
                    output.Append( MessageBlockNames.JoinToString( ", " ) );
                    player.Message( output.ToString() );
                }
            } else {
                CdMessageBlock.PrintUsage( player );
            }
        }

        private static void MessageBlockTestCallback( Player player, Vector3I[] marks, object tag ) {
            Vector3I Pos = marks[0];
            MessageBlock messageblock = MessageBlockHandler.GetMessageBlock( player.World, Pos );
            if ( messageblock == null ) {
                player.Message( "MessageBlockTest: There is no door at this position" );
            } else {
                player.Message( "MessageBlockTest: This position contains door '" + messageblock.Name + "'" );
            }
        }

        private static void MessageBlockAdd( Player player, Vector3I[] marks, object tag ) {
            string Message = ( string )tag;
            Vector3I mark = marks[0];
            if ( !player.Info.Rank.AllowSecurityCircumvention ) {
                SecurityCheckResult buildCheck = player.World.BuildSecurity.CheckDetailed( player.Info );
                switch ( buildCheck ) {
                    case SecurityCheckResult.BlackListed:
                        player.Message( "Cannot add a MessageBlock to world {0}&S: You are barred from building here.",
                                        player.World.ClassyName );
                        return;

                    case SecurityCheckResult.RankTooLow:
                        player.Message( "Cannot add a MessageBlock to world {0}&S: You are not allowed to build here.",
                                        player.World.ClassyName );
                        return;
                    //case SecurityCheckResult.RankTooHigh:
                }
            }
            if ( player.LastUsedBlockType != Block.Undefined ) {
                Vector3I Pos = mark;

                if ( player.CanPlace( player.World.Map, Pos, player.LastUsedBlockType, BlockChangeContext.Manual ) != CanPlaceResult.Allowed ) {
                    player.Message( "&WYou are not allowed to build here" );
                    return;
                }

                Player.RaisePlayerPlacedBlockEvent( player, player.WorldMap, Pos, player.WorldMap.GetBlock( Pos ), player.LastUsedBlockType, BlockChangeContext.Manual );
                BlockUpdate blockUpdate = new BlockUpdate( null, Pos, player.LastUsedBlockType );
                player.World.Map.QueueUpdate( blockUpdate );
            } else {
                player.Message( "&WError: No last used blocktype was found" );
                return;
            }
            MessageBlock MessageBlock;
            MessageBlock = new MessageBlock( player.World.Name, mark,
                MessageBlock.GenerateName( player.World ),
                player.ClassyName, Message )
            {
                Range = new MessageBlockRange(mark.X, mark.X, mark.Y, mark.Y, mark.Z, mark.Z)
            };

            MessageBlockHandler.CreateMessageBlock( MessageBlock, player.World );
            NormalBrush brush = new NormalBrush( Block.Air );
            Logger.Log( LogType.UserActivity, "{0} created MessageBlock {1} (on world {2})", player.Name, MessageBlock.Name, player.World.Name );
            player.Message( "MessageBlock created on world {0}&S with name {1}", player.World.ClassyName, MessageBlock.Name );
        }

        #endregion MessageBlocks

        #region portals

        private static readonly CommandDescriptor CdPortal = new CommandDescriptor {
            Name = "Portal",
            Category = CommandCategory.World,
            Permissions = new Permission[] { Permission.UsePortal },
            IsConsoleSafe = false,
            Usage = "/portal [create | remove | info | list | enable | disable ]",
            Help = "Controls portals, options are: create, remove, list, info, enable, disable\n&S" +
                   "See &H/Help portal <option>&S for details about each option.",
            HelpSections = new Dictionary<string, string>() {
                { "create",     "&H/portal create water Guest\n&S" +
                                "Creates a basic water portal to world Guest.\n&S" +
                                "&H/portal create Guest lava test\n&S" +
                                "Creates a lava portal with name 'test' to world Guest.\n" +
                                "&H/Portal create will create a portal where the output can be the position of your next red block."},
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

        private struct PDATA {
            public bool custom;
            public DrawOperation op;
        }

        private static void PortalH( Player player, Command command ) {
            try {
                String option = command.Next();

                if ( option == null ) {
                    CdPortal.PrintUsage( player );
                } else if ( option.ToLower().Equals( "create" ) ) {
                    if ( player.Can( Permission.ManagePortal ) ) {
                        string blockTypeOrName = command.Next();
                        DrawOperation operation = new CuboidDrawOperation( player );
                        NormalBrush brush = new NormalBrush( Block.Water, Block.Water );
                        if ( blockTypeOrName == null )
                            blockTypeOrName = "water";
                        if ( blockTypeOrName.ToLower().Equals( "lava" ) ) {
                            brush = new NormalBrush( Block.Lava, Block.Lava );
                        } else if ( !blockTypeOrName.ToLower().Equals( "water" ) ) {
                            player.Message( "Invalid block, choose between water or lava." );
                            return;
                        }

                        string world = command.Next();
                        bool CustomOutput = ( world == null );
                        operation.Brush = brush;
                        if ( world != null && WorldManager.FindWorldExact( world ) != null ) {
                            player.PortalWorld = world;
                        } else
                            player.PortalWorld = player.World.Name;
                        player.SelectionStart( operation.ExpectedMarks, PortalCreateCallback, new PDATA() { op = operation, custom = CustomOutput }, Permission.Draw );
                        player.Message( "Click {0} blocks or use &H/Mark&S to mark the area of the portal.", operation.ExpectedMarks );

                        if ( world == null ) {
                            return; //custom, continue in Selectstart
                        }
                        if ( world != null && WorldManager.FindWorldExact( world ) != null ) {
                            string portalName = command.Next();

                            if ( portalName == null ) {
                                player.PortalName = null;
                            } else {
                                if ( !Portal.DoesNameExist( player.World, portalName ) ) {
                                    player.PortalName = portalName;
                                } else {
                                    player.Message( "A portal with name {0} already exists in this world.", portalName );
                                    return;
                                }
                            }
                        } else {
                            if ( world == null ) {
                                player.Message( "No world specified." );
                            } else {
                                player.MessageNoWorld( world );
                            }
                        }
                    } else {
                        player.MessageNoAccess( Permission.ManagePortal );
                    }
                } else if ( option.ToLower().Equals( "remove" ) ) {
                    if ( player.Can( Permission.ManagePortal ) ) {
                        string portalName = command.Next();

                        if ( portalName == null ) {
                            player.Message( "No portal name specified." );
                        } else {
                            if ( player.World.Map.Portals != null && player.World.Map.Portals.Count > 0 ) {
                                bool found = false;
                                Portal portalFound = null;

                                lock ( player.World.Map.Portals.SyncRoot ) {
                                    foreach ( Portal portal in player.World.Map.Portals ) {
                                        if ( portal.Name.Equals( portalName ) ) {
                                            portalFound = portal;
                                            found = true;
                                            break;
                                        }
                                    }

                                    if ( !found ) {
                                        player.Message( "Could not find portal by name {0}.", portalName );
                                    } else {
                                        portalFound.Remove( player );
                                        player.Message( "Portal was removed." );
                                    }
                                }
                            } else {
                                player.Message( "Could not find portal as this world doesn't contain a portal." );
                            }
                        }
                    } else {
                        player.MessageNoAccess( Permission.ManagePortal );
                    }
                } else if ( option.ToLower().Equals( "info" ) ) {
                    string portalName = command.Next();

                    if ( portalName == null ) {
                        player.Message( "No portal name specified." );
                    } else {
                        if ( player.World.Map.Portals != null && player.World.Map.Portals.Count > 0 ) {
                            bool found = false;

                            lock ( player.World.Map.Portals.SyncRoot ) {
                                foreach ( Portal portal in player.World.Map.Portals ) {
                                    if ( portal.Name.Equals( portalName ) ) {
                                        World portalWorld = WorldManager.FindWorldExact( portal.World );
                                        player.Message( "Portal {0}&S was created by {1}&S at {2} and teleports to world {3}&S.",
                                            portal.Name, PlayerDB.FindPlayerInfoExact( portal.Creator ).ClassyName, portal.Created, portalWorld.ClassyName );
                                        found = true;
                                    }
                                }
                            }

                            if ( !found ) {
                                player.Message( "Could not find portal by name {0}.", portalName );
                            }
                        } else {
                            player.Message( "Could not find portal as this world doesn't contain a portal." );
                        }
                    }
                } else if ( option.ToLower().Equals( "list" ) ) {
                    if ( player.World.Map.Portals == null || player.World.Map.Portals.Count == 0 ) {
                        player.Message( "There are no portals in {0}&S.", player.World.ClassyName );
                    } else {
                        String[] portalNames = new String[player.World.Map.Portals.Count];
                        StringBuilder output = new StringBuilder( "There are " + player.World.Map.Portals.Count + " portals in " + player.World.ClassyName + "&S: " );

                        for ( int i = 0; i < player.World.Map.Portals.Count; i++ ) {
                            portalNames[i] = ( ( Portal )player.World.Map.Portals[i] ).Name;
                        }

                        output.Append( portalNames.JoinToString( ", " ) );

                        player.Message( output.ToString() );
                    }
                } else if ( option.ToLower().Equals( "enable" ) ) {
                    player.PortalsEnabled = true;
                    player.Message( "You enabled the use of portals." );
                } else if ( option.ToLower().Equals( "disable" ) ) {
                    player.PortalsEnabled = false;
                    player.Message( "You disabled the use of portals, type /portal enable to re-enable portals." );
                } else {
                    CdPortal.PrintUsage( player );
                }
            } catch ( PortalException ex ) {
                player.Message( ex.Message );
                Logger.Log( LogType.Error, "WorldCommands.PortalH: " + ex );
            } catch ( Exception ex ) {
                player.Message( "Unexpected error: " + ex );
                Logger.Log( LogType.Error, "WorldCommands.PortalH: " + ex );
            }
        }

        private static void PortalCreateCallback( Player player, Vector3I[] marks, object tag ) {
            try {
                World world = WorldManager.FindWorldExact( player.PortalWorld );

                if ( world != null ) {
                    PDATA pd = ( PDATA )tag;
                    DrawOperation op = pd.op;
                    bool CustomOutput = pd.custom;
                    if ( !op.Prepare( marks ) )
                        return;
                    if ( !player.CanDraw( op.BlocksTotalEstimate ) ) {
                        player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                           player.Info.Rank.DrawLimit,
                                           op.Bounds.Volume );
                        op.Cancel();
                        return;
                    }

                    int Xmin = Math.Min( marks[0].X, marks[1].X );
                    int Xmax = Math.Max( marks[0].X, marks[1].X );
                    int Ymin = Math.Min( marks[0].Y, marks[1].Y );
                    int Ymax = Math.Max( marks[0].Y, marks[1].Y );
                    int Zmin = Math.Min( marks[0].Z, marks[1].Z );
                    int Zmax = Math.Max( marks[0].Z, marks[1].Z );

                    for ( int x = Xmin; x <= Xmax; x++ ) {
                        for ( int y = Ymin; y <= Ymax; y++ ) {
                            for ( int z = Zmin; z <= Zmax; z++ ) {
                                if ( PortalHandler.IsInRangeOfSpawnpoint( player.World, new Vector3I( x, y, z ) ) ) {
                                    player.Message( "You can not build a portal near a spawnpoint." );
                                    return;
                                }

                                if ( PortalHandler.GetInstance().GetPortal( player.World, new Vector3I( x, y, z ) ) != null ) {
                                    player.Message( "You can not build a portal inside a portal, U MAD BRO?" );
                                    return;
                                }
                            }
                        }
                    }

                    if ( player.PortalName == null ) {
                        player.PortalName = Portal.GenerateName( player.World.Name, false );
                    }

                    if ( !CustomOutput ) {
                        Portal portal = new Portal( player.PortalWorld, marks, player.PortalName, player.Name, player.World.Name, false );
                        PortalHandler.CreatePortal( portal, player.World, false );
                        player.Message( "Successfully created portal with name " + portal.Name + "." );
                    } else {
                        player.PortalCache.World = player.World.Name;
                        player.PortalCache = new Portal( player.PortalWorld, marks, player.PortalName, player.Name, player.World.Name, true );
                        player.Message( "  &SPortal started, place a red block for the desired output (can be multiworld)" );
                    }
                    op.AnnounceCompletion = false;
                    op.Context = BlockChangeContext.Portal;
                    op.Begin();
                } else {
                    player.MessageInvalidWorldName( player.PortalWorld );
                }
            } catch ( Exception ex ) {
                player.Message( "Failed to create portal." );
                Logger.Log( LogType.Error, "WorldCommands.PortalCreateCallback: " + ex );
            }
        }

        #endregion portals

        private static readonly CommandDescriptor CdWorldSearch = new CommandDescriptor {
            Name = "Worldsearch",
            Aliases = new[] { "ws" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Chat },
            Usage = "/Worldsearch WorldName",
            Help = "An easy way to search through a big list of worlds",
            Handler = WorldSearchHandler
        };

        private static void WorldSearchHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if ( worldName == null ) {
                CdWorldSearch.PrintUsage( player );
                return;
            }
            if ( worldName.Length < 2 ) {
                CdWorldSearch.PrintUsage( player );
                return;
            } else {
                worldName = worldName.ToLower();
                var WorldNames = WorldManager.Worlds
                                         .Where( w => w.Name.ToLower().Contains( worldName ) ).ToArray();

                if ( WorldNames.Length <= 30 ) {
                    player.MessageManyMatches( "worlds", WorldNames );
                } else {
                    int offset;
                    if ( !cmd.NextInt( out offset ) )
                        offset = 0;

                    if ( offset >= WorldNames.Count() )
                        offset = Math.Max( 0, WorldNames.Length - 30 );

                    World[] WorldPart = WorldNames.Skip( offset ).Take( 30 ).ToArray();
                    player.MessageManyMatches( "worlds", WorldPart );

                    if ( offset + WorldNames.Length < WorldNames.Length )
                        player.Message( "Showing {0}-{1} (out of {2}). Next: &H/List {3} {4}",
                                        offset + 1, offset + WorldPart.Length, WorldNames.Length,
                                        "worldsearch", offset + WorldPart.Length );
                    else
                        player.Message( "Showing matches {0}-{1} (out of {2}).",
                                        offset + 1, offset + WorldPart.Length, WorldNames.Length );
                    return;
                }
            }
        }

        private static readonly CommandDescriptor CdRealm = new CommandDescriptor {
            Name = "Realm",
            Category = CommandCategory.World,
            Permissions = new[] { Permission.Realm },
            IsConsoleSafe = false,
            Usage = "/Realm <Option>. /Help Realm for a list of commands.",
            Help = "/Realm &A| Help | Join | Like | Home | Flush | Spawn " +
            "| Review | Create | Allow | Unallow | Ban | Unban | Activate | Physics | Env | Control",
            Handler = Realm,
        };

        internal static void Realm( Player player, Command cmd ) {
            string playerName = player.Name.Replace( ".", "-" );

            string Choice = cmd.Next();
            if ( Choice == null ) {
                CdRealm.PrintUsage( player );
                return;
            }
            switch ( Choice.ToLower() ) {
                default:
                    CdRealm.PrintUsage( player );
                    break;

                case "env":
                    if ( player.World.Name == playerName ) {
                        string variable = cmd.Next();
                        string valueText = cmd.Next();
                        EnvHandler( player, new Command( "/Env " + player.World.Name + " " + variable + " " + valueText ) );
                    } else {
                        player.Message( "&WYou need to be in your realm" );
                        return;
                    }
                    break;

                case "review":

                    if ( player.World.Name == playerName ) {
                        var recepientList = Server.Players.Can( Permission.ReadStaffChat )
                                              .NotIgnoring( player )
                                              .Union( player );
                        string message = String.Format( "{0}&C would like staff to review their realm", player.ClassyName );
                        recepientList.Message( message );
                    } else
                        player.Message( "You are not in your Realm" );

                    break;

                case "like":

                    Choice = player.World.Name;
                    World world = WorldManager.FindWorldOrPrintMatches( player, Choice );
                    if ( world == null )
                        player.Message( "You need to enter a realm name" );

                    if ( world.IsRealm ) {
                        Server.Players.Message( "{0}&S likes realm {1}.",
                                               player.ClassyName, world.ClassyName );
                        return;
                    } else
                        player.Message( "You are not in a Realm" );

                    break;

                case "flush":

                    WorldFlushHandler( player, new Command( "/wflush " + playerName ) );
                    break;

                case "create":

                    string Theme = cmd.Next();
                    string Template = cmd.Next();
                    if ( player.World.Name == playerName ) {
                        player.Message( "You cannot create a new Realm when you are inside your Realm" );
                        return;
                    }

                    if ( Theme == null || Template == null ) {
                        player.Message( "&HUse /Realm create [ThemeType] [TerrainType]\n" +
                            "&9Available themes: Grass, " + Enum.GetNames( typeof( MapGenTheme ) ).JoinToString() + '\n' +
                              "&EAvailable terrain types: Empty, Ocean, " + Enum.GetNames( typeof( MapGenTemplate ) ).JoinToString() + '\n' );
                        return;
                    }
                    RealmHandler.RealmCreate( player, cmd, Theme, Template );
                    player.Message( "&9You can now activate your realm by typing /Realm Activate" );
                    break;

                case "home":
                    JoinHandler( player, new Command( "/join " + playerName ) );
                    break;

                case "help":
                    player.Message( "To build a realm, use /realm create. To activate it so you can build, use /realm activate. " +
                    "If you find yourself unable to build in your Realm, use /realm activate again." );
                    break;

                case "activate": {
                        if ( player.World.Name == playerName ) {
                            player.Message( "You cannot use /Realm activate when you are in your Realm" );
                            return;
                        }
                        RealmHandler.RealmLoad( player, cmd, playerName + ".fcm", playerName, RankManager.HighestRank.Name, RankManager.DefaultBuildRank.Name );
                        World realmworld = WorldManager.FindWorldExact( playerName );
                        if ( realmworld == null )
                            return;
                        if ( !realmworld.AccessSecurity.Check( player.Info ) ) {
                            realmworld.AccessSecurity.Include( player.Info );
                        }
                        if ( !realmworld.BuildSecurity.Check( player.Info ) ) {
                            realmworld.BuildSecurity.Include( player.Info );
                        }
                        WorldManager.SaveWorldList();
                        break;
                    }

                case "control":
                    World w1 = WorldManager.FindWorldExact( playerName );
                    if ( w1 == null )
                        return;
                    if ( !w1.AccessSecurity.Check( player.Info ) ) {
                        w1.AccessSecurity.Include( player.Info );
                        player.Message( "You have regained access control of your realm" );
                    }
                    if ( !w1.BuildSecurity.Check( player.Info ) ) {
                        w1.BuildSecurity.Include( player.Info );
                        player.Message( "You have regained building control of your realm" );
                    }
                    w1.IsRealm = true;
                    break;

                case "spawn":

                    if ( player.World.Name == playerName ) {
                        ModerationCommands.SetSpawnHandler( player, new Command( "/setspawn" ) );
                        return;
                    } else {
                        player.Message( "You can only change the Spawn on your own realm" );
                        return;
                    }

                case "physics":

                    string phyOption = cmd.Next();
                    string onOff = cmd.Next();
                    world = player.World;

                    if ( phyOption == null ) {
                        player.Message( "Turn physics on in your realm. Useage: /Realm physics [Plant|Water|Gun|Fireworks] On/Off." );
                        return;
                    }
                    if ( onOff == null ) {
                        player.Message( "&WInvalid option: /Realm Physics [Type] [On/Off]" );
                        return;
                    }
                    if ( world.Name != playerName ) {
                        player.Message( "&WYou can only turn physics on in your realm" );
                        return;
                    }

                    switch ( phyOption.ToLower() ) {
                        case "water":
                            if ( onOff.ToLower() == "on" ) {
                                world.EnableWaterPhysics( player, true );
                            }
                            if ( onOff.ToLower() == "off" ) {
                                world.DisableWaterPhysics( player, true );
                            } else
                                player.Message( "&WInvalid option: /Realm Physics [Type] [On/Off]" );
                            break;

                        case "plant":
                            if ( onOff.ToLower() == "on" ) {
                                world.EnablePlantPhysics( player, true );
                            }
                            if ( onOff.ToLower() == "off" ) {
                                world.DisablePlantPhysics( player, true );
                            } else
                                player.Message( "&WInvalid option: /Realm Physics [Type] [On/Off]" );
                            break;

                        case "gun":
                            if ( onOff.ToLower() == "on" ) {
                                world.EnableGunPhysics( player, true );
                            }
                            if ( onOff.ToLower() == "off" ) {
                                world.DisableGunPhysics( player, true );
                            } else
                                player.Message( "&WInvalid option: /Realm Physics [Type] [On/Off]" );
                            break;

                        case "firework":
                        case "fireworks":
                            if ( onOff.ToLower() == "on" ) {
                                world.EnableFireworkPhysics( player, true );
                            }
                            if ( onOff.ToLower() == "off" ) {
                                world.DisableFireworkPhysics( player, true );
                            } else
                                player.Message( "&WInvalid option: /Realm Physics [Type] [On/Off]" );
                            break;

                        default:
                            player.Message( "&WInvalid option: /Realm physics [Plant|Water|Gun|Fireworks] On/Off" );
                            break;
                    }
                    break;

                case "join":

                    string JoinCmd = cmd.Next();
                    if ( JoinCmd == null ) {
                        player.Message( "Derp. Invalid Realm." );
                        return;
                    } else {
                        Player target = Server.FindPlayerOrPrintMatches( player, Choice, false, true );
                        JoinHandler( player, new Command( "/goto " + JoinCmd ) );
                        return;
                    }

                case "allow":
                    string toAllow = cmd.Next();

                    if ( toAllow == null ) {
                        player.Message( "Allows a player to build in your world. useage: /realm allow playername." );
                        return;
                    }

                    PlayerInfo targetAllow = PlayerDB.FindPlayerInfoOrPrintMatches( player, toAllow );

                    if ( targetAllow == null ) {
                        player.Message( "Please enter the name of the player you want to allow to build in your Realm." );
                        return;
                    }

                    if ( !Player.IsValidName( targetAllow.Name ) ) {
                        player.Message( "Player not found. Please specify valid name." );
                        return;
                    } else {
                        RealmHandler.RealmBuild( player, cmd, player.Name, "+" + targetAllow.Name, null );
                        if ( !Player.IsValidName( targetAllow.Name ) ) {
                            player.Message( "Player not found. Please specify valid name." );
                            return;
                        }
                    }
                    break;

                case "unallow":

                    string Unallow = cmd.Next();

                    if ( Unallow == null ) {
                        player.Message( "Stops a player from building in your world. usage: /realm unallow playername." );
                        return;
                    }
                    PlayerInfo targetUnallow = PlayerDB.FindPlayerInfoOrPrintMatches( player, Unallow );

                    if ( targetUnallow == null ) {
                        player.Message( "Please enter the name of the player you want to stop building in your Realm." );
                        return;
                    }

                    if ( !Player.IsValidName( targetUnallow.Name ) ) {
                        player.Message( "Player not found. Please specify valid name." );
                        return;
                    } else {
                        RealmHandler.RealmBuild( player, cmd, player.Name, "-" + targetUnallow.Name, null );
                        if ( !Player.IsValidName( targetUnallow.Name ) ) {
                            player.Message( "Player not found. Please specify valid name." );
                            return;
                        }
                    }
                    break;

                case "ban":
                    string Ban = cmd.Next();
                    if ( Ban == null ) {
                        player.Message( "Bans a player from accessing your Realm. Useage: /Realm ban playername." );
                        return;
                    }
                    Player targetBan = Server.FindPlayerOrPrintMatches( player, Ban, false, true );

                    if ( targetBan == null ) {
                        player.Message( "Please enter the name of the player you want to ban from your Realm." );
                        return;
                    }

                    if ( !Player.IsValidName( targetBan.Name ) ) {
                        player.Message( "Player not found. Please specify valid name." );
                        return;
                    } else {
                        RealmHandler.RealmAccess( player, cmd, player.Name, "-" + targetBan.Name );
                        if ( !Player.IsValidName( targetBan.Name ) ) {
                            player.Message( "Player not found. Please specify valid name." );
                            return;
                        }
                    }
                    break;

                case "unban":
                    string UnBan = cmd.Next();

                    if ( UnBan == null ) {
                        player.Message( "Unbans a player from your Realm. Useage: /Realm unban playername." );
                        return;
                    }
                    PlayerInfo targetUnBan = PlayerDB.FindPlayerInfoOrPrintMatches( player, UnBan );

                    if ( targetUnBan == null ) {
                        player.Message( "Please enter the name of the player you want to unban from your Realm." );
                        return;
                    }

                    if ( !Player.IsValidName( targetUnBan.Name ) ) {
                        player.Message( "Player not found. Please specify valid name." );
                        return;
                    } else {
                        RealmHandler.RealmAccess( player, cmd, player.Name, "+" + targetUnBan.Name );
                        if ( !Player.IsValidName( targetUnBan.Name ) ) {
                            player.Message( "Player not found. Please specify valid name." );
                            return;
                        }
                        break;
                    }
            }
        }

        #region BlockDB

        private static readonly CommandDescriptor CdBlockDB = new CommandDescriptor {
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

        private static void BlockDBHandler( Player player, Command cmd ) {
            if ( !BlockDB.IsEnabledGlobally ) {
                player.Message( "&WBlockDB is disabled on this server." );
                return;
            }

            string worldName = cmd.Next();
            if ( worldName == null ) {
                int total = 0;
                World[] autoEnabledWorlds = WorldManager.Worlds.Where( w => ( w.BlockDB.EnabledState == YesNoAuto.Auto ) && w.BlockDB.IsEnabled ).ToArray();
                if ( autoEnabledWorlds.Length > 0 ) {
                    total += autoEnabledWorlds.Length;
                    player.Message( "BlockDB is auto-enabled on: {0}",
                                    autoEnabledWorlds.JoinToClassyString() );
                }

                World[] manuallyEnabledWorlds = WorldManager.Worlds.Where( w => w.BlockDB.EnabledState == YesNoAuto.Yes ).ToArray();
                if ( manuallyEnabledWorlds.Length > 0 ) {
                    total += manuallyEnabledWorlds.Length;
                    player.Message( "BlockDB is manually enabled on: {0}",
                                    manuallyEnabledWorlds.JoinToClassyString() );
                }

                World[] manuallyDisabledWorlds = WorldManager.Worlds.Where( w => w.BlockDB.EnabledState == YesNoAuto.No ).ToArray();
                if ( manuallyDisabledWorlds.Length > 0 ) {
                    player.Message( "BlockDB is manually disabled on: {0}",
                                    manuallyDisabledWorlds.JoinToClassyString() );
                }

                if ( total == 0 ) {
                    player.Message( "BlockDB is not enabled on any world." );
                }
                return;
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if ( world == null )
                return;
            BlockDB db = world.BlockDB;

            using ( db.GetWriteLock() ) {
                string op = cmd.Next();
                if ( op == null ) {
                    if ( !db.IsEnabled ) {
                        if ( db.EnabledState == YesNoAuto.Auto ) {
                            player.Message( "BlockDB is disabled (auto) on world {0}", world.ClassyName );
                        } else {
                            player.Message( "BlockDB is disabled on world {0}", world.ClassyName );
                        }
                    } else {
                        if ( db.IsPreloaded ) {
                            if ( db.EnabledState == YesNoAuto.Auto ) {
                                player.Message( "BlockDB is enabled (auto) and preloaded on world {0}", world.ClassyName );
                            } else {
                                player.Message( "BlockDB is enabled and preloaded on world {0}", world.ClassyName );
                            }
                        } else {
                            if ( db.EnabledState == YesNoAuto.Auto ) {
                                player.Message( "BlockDB is enabled (auto) on world {0}", world.ClassyName );
                            } else {
                                player.Message( "BlockDB is enabled on world {0}", world.ClassyName );
                            }
                        }
                        player.Message( "    Change limit: {0}    Time limit: {1}",
                                        db.Limit == 0 ? "none" : db.Limit.ToStringInvariant(),
                                        db.TimeLimit == TimeSpan.Zero ? "none" : db.TimeLimit.ToMiniString() );
                    }
                    return;
                }

                switch ( op.ToLower() ) {
                    case "on":
                        // enables BlockDB
                        if ( db.EnabledState == YesNoAuto.Yes ) {
                            player.Message( "BlockDB is already manually enabled on world {0}", world.ClassyName );
                        } else if ( db.EnabledState == YesNoAuto.Auto && db.IsEnabled ) {
                            db.EnabledState = YesNoAuto.Yes;
                            WorldManager.SaveWorldList();
                            player.Message( "BlockDB was auto-enabled, and is now manually enabled on world {0}", world.ClassyName );
                        } else {
                            Logger.Log( LogType.UserActivity,
                                        "BlockDB: Player {0} enabled BlockDB on world {1} (was {2})",
                                        player.Name, world.Name, db.EnabledState );
                            db.EnabledState = YesNoAuto.Yes;
                            WorldManager.SaveWorldList();
                            player.Message( "BlockDB is now manually enabled on world {0}", world.ClassyName );
                        }
                        break;

                    case "off":
                        // disables BlockDB
                        if ( db.EnabledState == YesNoAuto.No ) {
                            player.Message( "BlockDB is already manually disabled on world {0}", world.ClassyName );
                        } else if ( db.IsEnabled ) {
                            if ( cmd.IsConfirmed ) {
                                db.EnabledState = YesNoAuto.No;
                                WorldManager.SaveWorldList();
                                player.Message( "BlockDB is now manually disabled on world {0}&S. Use &H/BlockDB {1} clear&S to delete all the data.",
                                                world.ClassyName, world.Name );
                            } else {
                                Logger.Log( LogType.UserActivity,
                                            "BlockDB: Asked {0} to confirm disabling BlockDB on world {1}",
                                            player.Name, world.Name );
                                player.Confirm( cmd,
                                                "Disable BlockDB on world {0}&S? Block changes will stop being recorded.",
                                                world.ClassyName );
                            }
                        } else {
                            Logger.Log( LogType.UserActivity,
                                        "BlockDB: Player {0} disabled BlockDB on world {1} (was {2})",
                                        player.Name, world.Name, db.EnabledState );
                            db.EnabledState = YesNoAuto.No;
                            WorldManager.SaveWorldList();
                            player.Message( "BlockDB was auto-disabled, and is now manually disabled on world {0}&S.",
                                            world.ClassyName );
                        }
                        break;

                    case "auto":
                        if ( db.EnabledState == YesNoAuto.Auto ) {
                            player.Message( "BlockDB is already set to automatically enable/disable itself on world {0}", world.ClassyName );
                        } else {
                            Logger.Log( LogType.UserActivity,
                                        "BlockDB: Player {0} set BlockDB state on world {1} to Auto (was {2})",
                                        player.Name, world.Name, db.EnabledState );
                            db.EnabledState = YesNoAuto.Auto;
                            WorldManager.SaveWorldList();
                            if ( db.IsEnabled ) {
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
                        if ( db.IsEnabled ) {
                            string limitString = cmd.Next();
                            int limitNumber;

                            if ( limitString == null ) {
                                player.Message( "BlockDB: Limit for world {0}&S is {1}",
                                                world.ClassyName,
                                                ( db.Limit == 0 ? "none" : db.Limit.ToStringInvariant() ) );
                                return;
                            }

                            if ( limitString.Equals( "none", StringComparison.OrdinalIgnoreCase ) ) {
                                limitNumber = 0;
                            } else if ( !Int32.TryParse( limitString, out limitNumber ) ) {
                                CdBlockDB.PrintUsage( player );
                                return;
                            } else if ( limitNumber < 0 ) {
                                player.Message( "BlockDB: Limit must be non-negative." );
                                return;
                            }

                            string limitDisplayString = ( limitNumber == 0 ? "none" : limitNumber.ToStringInvariant() );
                            if ( db.Limit == limitNumber ) {
                                player.Message( "BlockDB: Limit for world {0}&S is already set to {1}",
                                               world.ClassyName, limitDisplayString );
                            } else if ( !cmd.IsConfirmed && limitNumber != 0 ) {
                                Logger.Log( LogType.UserActivity,
                                            "BlockDB: Asked {0} to confirm changing BlockDB limit on world {1}",
                                            player.Name, world.Name );
                                player.Confirm( cmd, "BlockDB: Change limit? Some old data for world {0}&S may be discarded.", world.ClassyName );
                            } else {
                                db.Limit = limitNumber;
                                WorldManager.SaveWorldList();
                                player.Message( "BlockDB: Limit for world {0}&S set to {1}",
                                               world.ClassyName, limitDisplayString );
                            }
                        } else {
                            player.Message( "Block tracking is disabled on world {0}", world.ClassyName );
                        }
                        break;

                    case "timelimit":
                        // sets or resets limit on the age of changes to store
                        if ( db.IsEnabled ) {
                            string limitString = cmd.Next();

                            if ( limitString == null ) {
                                if ( db.TimeLimit == TimeSpan.Zero ) {
                                    player.Message( "BlockDB: There is no time limit for world {0}",
                                                    world.ClassyName );
                                } else {
                                    player.Message( "BlockDB: Time limit for world {0}&S is {1}",
                                                    world.ClassyName, db.TimeLimit.ToMiniString() );
                                }
                                return;
                            }

                            TimeSpan limit;
                            if ( limitString.Equals( "none", StringComparison.OrdinalIgnoreCase ) ) {
                                limit = TimeSpan.Zero;
                            } else if ( !limitString.TryParseMiniTimespan( out limit ) ) {
                                CdBlockDB.PrintUsage( player );
                                return;
                            }
                            if ( limit > DateTimeUtil.MaxTimeSpan ) {
                                player.MessageMaxTimeSpan();
                                return;
                            }

                            if ( db.TimeLimit == limit ) {
                                if ( db.TimeLimit == TimeSpan.Zero ) {
                                    player.Message( "BlockDB: There is already no time limit for world {0}",
                                                    world.ClassyName );
                                } else {
                                    player.Message( "BlockDB: Time limit for world {0}&S is already set to {1}",
                                                    world.ClassyName, db.TimeLimit.ToMiniString() );
                                }
                            } else if ( !cmd.IsConfirmed && limit != TimeSpan.Zero ) {
                                Logger.Log( LogType.UserActivity,
                                            "BlockDB: Asked {0} to confirm changing BlockDB time limit on world {1}",
                                            player.Name, world.Name );
                                player.Confirm( cmd, "BlockDB: Change time limit? Some old data for world {0}&S may be discarded.", world.ClassyName );
                            } else {
                                db.TimeLimit = limit;
                                WorldManager.SaveWorldList();
                                if ( db.TimeLimit == TimeSpan.Zero ) {
                                    player.Message( "BlockDB: Time limit removed for world {0}",
                                                    world.ClassyName );
                                } else {
                                    player.Message( "BlockDB: Time limit for world {0}&S set to {1}",
                                                    world.ClassyName, db.TimeLimit.ToMiniString() );
                                }
                            }
                        } else {
                            player.Message( "Block tracking is disabled on world {0}", world.ClassyName );
                        }
                        break;

                    case "clear":
                        // wipes BlockDB data
                        bool hasData = ( db.IsEnabled || File.Exists( db.FileName ) );
                        if ( hasData ) {
                            if ( cmd.IsConfirmed ) {
                                db.Clear();
                                Logger.Log( LogType.UserActivity,
                                            "BlockDB: Player {0} cleared BlockDB data world {1}",
                                            player.Name, world.Name );
                                player.Message( "BlockDB: Cleared all data for {0}", world.ClassyName );
                            } else {
                                Logger.Log( LogType.UserActivity,
                                            "BlockDB: Asked {0} to confirm clearing BlockDB data world {1}",
                                            player.Name, world.Name );
                                player.Confirm( cmd, "Clear BlockDB data for world {0}&S? This cannot be undone.",
                                                world.ClassyName );
                            }
                        } else {
                            player.Message( "BlockDB: No data to clear for world {0}", world.ClassyName );
                        }
                        break;

                    case "preload":
                        // enables/disables BlockDB preloading
                        if ( db.IsEnabled ) {
                            string param = cmd.Next();
                            if ( param == null ) {
                                // shows current preload setting
                                player.Message( "BlockDB preloading is {0} for world {1}",
                                                ( db.IsPreloaded ? "ON" : "OFF" ),
                                                world.ClassyName );
                            } else if ( param.Equals( "on", StringComparison.OrdinalIgnoreCase ) ) {
                                // turns preload on
                                if ( db.IsPreloaded ) {
                                    player.Message( "BlockDB preloading is already enabled on world {0}", world.ClassyName );
                                } else {
                                    db.IsPreloaded = true;
                                    WorldManager.SaveWorldList();
                                    player.Message( "BlockDB preloading is now enabled on world {0}", world.ClassyName );
                                }
                            } else if ( param.Equals( "off", StringComparison.OrdinalIgnoreCase ) ) {
                                // turns preload off
                                if ( !db.IsPreloaded ) {
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

        #endregion BlockDB

        #region BlockInfo

        private static readonly CommandDescriptor CdBlockInfo = new CommandDescriptor {
            Name = "BInfo",
            Category = CommandCategory.World,
            Aliases = new[] { "b", "bi", "whodid", "about" },
            Permissions = new[] { Permission.ViewOthersInfo },
            RepeatableSelection = true,
            Usage = "/BInfo [X Y Z]",
            Help = "Checks edit history for a given block.",
            Handler = BlockInfoHandler
        };

        private static void BlockInfoHandler( Player player, Command cmd ) {
            World playerWorld = player.World;
            if ( playerWorld == null )
                PlayerOpException.ThrowNoWorld( player );

            // Make sure BlockDB is usable
            if ( !BlockDB.IsEnabledGlobally ) {
                player.Message( "&WBlockDB is disabled on this server." );
                return;
            }
            if ( !playerWorld.BlockDB.IsEnabled ) {
                player.Message( "&WBlockDB is disabled in this world." );
                return;
            }

            int x, y, z;
            if ( cmd.NextInt( out x ) && cmd.NextInt( out y ) && cmd.NextInt( out z ) ) {
                // If block coordinates are given, run the BlockDB query right away
                if ( cmd.HasNext ) {
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

        private static void BlockInfoSelectionCallback( Player player, Vector3I[] marks, object tag ) {
            var args = new BlockInfoLookupArgs {
                Player = player,
                World = player.World,
                Coordinate = marks[0]
            };

            Scheduler.NewBackgroundTask( BlockInfoSchedulerCallback, args ).RunOnce();
        }

        private sealed class BlockInfoLookupArgs {
            public Player Player;
            public World World;
            public Vector3I Coordinate;
        }

        private const int MaxBlockChangesToList = 15;

        private static void BlockInfoSchedulerCallback( SchedulerTask task ) {
            BlockInfoLookupArgs args = ( BlockInfoLookupArgs )task.UserState;
            if ( !args.World.BlockDB.IsEnabled ) {
                args.Player.Message( "&WBlockDB is disabled in this world." );
                return;
            }
            BlockDBEntry[] results = args.World.BlockDB.Lookup( MaxBlockChangesToList, args.Coordinate );
            if ( results.Length > 0 ) {
                Array.Reverse( results );
                foreach ( BlockDBEntry entry in results ) {
                    string date = DateTime.UtcNow.Subtract( DateTimeUtil.TryParseDateTime( entry.Timestamp ) ).ToMiniString();

                    PlayerInfo info = PlayerDB.FindPlayerInfoByID( entry.PlayerID );
                    string playerName;
                    if ( info == null ) {
                        playerName = "?";
                    } else {
                        Player target = info.PlayerObject;
                        if ( target != null && args.Player.CanSee( target ) ) {
                            playerName = info.ClassyName;
                        } else {
                            playerName = info.ClassyName + "&S (offline)";
                        }
                    }
                    string contextString;
                    switch ( entry.Context ) {
                        case BlockChangeContext.Manual:
                            contextString = "";
                            break;

                        case BlockChangeContext.PaintedCombo:
                            contextString = " (Painted)";
                            break;

                        case BlockChangeContext.RedoneCombo:
                            contextString = " (Redone)";
                            break;

                        default:
                            if ( ( entry.Context & BlockChangeContext.Drawn ) == BlockChangeContext.Drawn &&
                                entry.Context != BlockChangeContext.Drawn ) {
                                contextString = " (" + ( entry.Context & ~BlockChangeContext.Drawn ) + ")";
                            } else {
                                contextString = " (" + entry.Context + ")";
                            }
                            break;
                    }

                    if ( entry.OldBlock == ( byte )Block.Air ) {
                        args.Player.Message( "&S  {0} ago: {1}&S placed {2}{3}",
                                             date, playerName, entry.NewBlock, contextString );
                    } else if ( entry.NewBlock == ( byte )Block.Air ) {
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

        #endregion BlockInfo

        #endregion 800Craft

        #region Env

        private static readonly CommandDescriptor CdEnv = new CommandDescriptor {
            Name = "Env",
            Category = CommandCategory.World,
            Permissions = new[] { Permission.ManageWorlds },
            Help = "&HPrints or changes the environmental variables for a given world. " +
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

        private static void EnvHandler( Player player, Command cmd ) {
            if ( !ConfigKey.WoMEnableEnvExtensions.Enabled() ) {
                player.Message( "Env command is disabled on this server." );
                return;
            }
            string worldName = cmd.Next();
            World world;
            if ( worldName == null ) {
                CdEnv.PrintUsage( player );
                return;
            } else {
                world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                if ( world == null )
                    return;
            }

            string variable = cmd.Next();
            string valueText = cmd.Next();
            if ( variable == null ) {
                player.Message( "Environment settings for {0}&S:", world.ClassyName );
                player.Message( "  Cloud: {0}   Fog: {1}   Sky: {2}",
                                world.CloudColor == -1 ? "normal" : '#' + world.CloudColor.ToString( "X6" ),
                                world.FogColor == -1 ? "normal" : '#' + world.FogColor.ToString( "X6" ),
                                world.SkyColor == -1 ? "normal" : '#' + world.SkyColor.ToString( "X6" ) );
                player.Message( "  Edge level: {0}  Edge texture: {1}",
                                world.EdgeLevel == -1 ? "normal" : world.EdgeLevel + " blocks",
                                world.EdgeBlock );
                if ( !player.IsUsingWoM ) {
                    player.Message( "  You need WoM client to see the changes." );
                }
                return;
            }

            #region 800Craft

            //Copyright (C) <2011 - 2013> <Jon Baker> using open source texture packs from various sources
            if ( variable.ToLower() == "terrain" ) {
                if ( valueText == null ) {
                    player.Message( "&A/Env [WorldName] terrain [Normal, arbot, cool, deadly, shroom, prometheus, woodpunk, fall, snow, tron, " +
                    "mario, highres, 8bit, simple, indev, messa, portal, dokucraft, doomcraft, hexeretic, zelda " );
                    return;
                }
                switch ( valueText.ToLower() ) {
                    case "normal":
                        world.Terrain = "bc4acee575474f5266105430c3cc628b8b3948a2";
                        break;

                    case "arbot":
                        world.Terrain = "1e3eb03d8efaa862679d36c9044ce47e861ea25e";
                        break;

                    case "cool":
                        world.Terrain = "165917066357092a2e7f6b0ec358c05b36b0efa7";
                        break;

                    case "deadly":
                        world.Terrain = "cb45307db4addbaac1504529fef79d773a6e31f5";
                        break;

                    case "shroom":
                        world.Terrain = "f31b086dbae92cc1741476a3697506192b8f5814";
                        break;

                    case "prometheus":
                        world.Terrain = "f66479f2d6c812806c3e768442d45a08a868ad16";
                        break;

                    case "woodpunk":
                        world.Terrain = "dff99c37e4a792e10c3b775e6bded725f18ed6fe";
                        break;

                    case "simple":
                        world.Terrain = "85f783c3a70c0c9d523eb39e080c2ed95f45bfc2";
                        break;

                    case "highres":
                        world.Terrain = "f3dac271d7bce9954baad46e183a6a910a30d13b";
                        break;

                    case "hexeretic":
                        world.Terrain = "d8e75476281087c8482ac636a8b8e4a59fadd525";
                        break;

                    case "tron":
                        world.Terrain = "ba851c9544ba5e4eed3a8fc9b8b5bf25a4dd45e0";
                        break;

                    case "8bit":
                        world.Terrain = "5a3fb1994e2ae526815ceaaca3a4dac0051aa890";
                        break;

                    case "mario":
                        world.Terrain = "e98a37ddccbc6144306bd08f41248324965c4e5a";
                        break;

                    case "fall":
                        world.Terrain = "b7c6dcb7a858639077f95ef94e8e2d51bedc3307";
                        break;

                    case "dokucraft":
                        world.Terrain = "a101cadafd02019e14d727d3329a923a40ef040b";
                        break;

                    case "indev":
                        world.Terrain = "73d1ef4441725bdcc9ac3616205faa3dff46e12a";
                        break;

                    case "doomcraft":
                        world.Terrain = "8b72beb6fea6ed1e01c1e32e08edf5f784bc919c";
                        break;

                    case "messa":
                        world.Terrain = "db0feeac8702704a3146a71365622db55fb5a4c4";
                        break;

                    case "portal":
                        world.Terrain = "d4b455134394763296994d0c819b0ac0ea338457";
                        break;

                    case "snow":
                        world.Terrain = "0b18fb3b41874ac5fbcb43532d62e6b742adc25e";
                        break;

                    case "zelda":
                        world.Terrain = "b25e3bffe57c4f6a35ae42bb6116fcb21c50fa6f";
                        break;

                    default:
                        player.Message( "&A/Env [WorldName] terrain [Normal, arbot, cool, deadly, shroom, prometheus, woodpunk, fall, snow, tron, " +
                    "mario, highres, 8bit, simple, indev, messa, portal, dokucraft, doomcraft, hexeretic, zelda " );
                        return;
                }
                player.Message( "Terrain Changed for {0}", world.ClassyName );
                WorldManager.UpdateWorldList();
                return;
            }

            if ( variable.ToLower() == "realistic" ) {
                if ( !world.RealisticEnv ) {
                    world.RealisticEnv = true;
                    player.Message( "Realistic Environment has been turned ON for world {0}", world.ClassyName );
                    return;
                } else {
                    world.RealisticEnv = false;
                    player.Message( "Realistic Environment has been turned OFF for world {0}", player.World.ClassyName );
                    return;
                }
            }

            #endregion 800Craft

            if ( variable.Equals( "normal", StringComparison.OrdinalIgnoreCase ) ) {
                if ( cmd.IsConfirmed ) {
                    world.FogColor = -1;
                    world.CloudColor = -1;
                    world.SkyColor = -1;
                    world.EdgeLevel = -1;
                    world.EdgeBlock = Block.Water;
                    player.Message( "Reset enviroment settings for {0}", world.ClassyName );
                    WorldManager.SaveWorldList();
                } else {
                    player.Confirm( cmd, "Reset enviroment settings for {0}&S?", world.ClassyName );
                }
                return;
            }

            if ( valueText == null ) {
                CdEnv.PrintUsage( player );
                return;
            }

            int value = 0;
            if ( valueText.Equals( "normal", StringComparison.OrdinalIgnoreCase ) ) {
                value = -1;
            }

            switch ( variable.ToLower() ) {
                case "fog":
                    if ( value == -1 ) {
                        player.Message( "Reset fog color for {0}&S to normal", world.ClassyName );
                    } else {
                        try {
                            value = ParseHexColor( valueText );
                        } catch ( FormatException ) {
                            CdEnv.PrintUsage( player );
                            return;
                        }
                        player.Message( "Set fog color for {0}&S to #{1:X6}", world.ClassyName, value );
                    }
                    world.FogColor = value;
                    break;

                case "cloud":
                case "clouds":
                    if ( value == -1 ) {
                        player.Message( "Reset cloud color for {0}&S to normal", world.ClassyName );
                    } else {
                        try {
                            value = ParseHexColor( valueText );
                        } catch ( FormatException ) {
                            CdEnv.PrintUsage( player );
                            return;
                        }
                        player.Message( "Set cloud color for {0}&S to #{1:X6}", world.ClassyName, value );
                    }
                    world.CloudColor = value;
                    break;

                case "sky":
                    if ( value == -1 ) {
                        player.Message( "Reset sky color for {0}&S to normal", world.ClassyName );
                    } else {
                        try {
                            value = ParseHexColor( valueText );
                        } catch ( FormatException ) {
                            CdEnv.PrintUsage( player );
                            return;
                        }
                        player.Message( "Set sky color for {0}&S to #{1:X6}", world.ClassyName, value );
                    }
                    world.SkyColor = value;
                    break;

                case "level":
                    if ( value == -1 ) {
                        player.Message( "Reset edge level for {0}&S to normal", world.ClassyName );
                    } else {
                        try {
                            value = UInt16.Parse( valueText );
                        } catch ( OverflowException ) {
                            CdEnv.PrintUsage( player );
                            return;
                        } catch ( FormatException ) {
                            CdEnv.PrintUsage( player );
                            return;
                        }
                        player.Message( "Set edge level for {0}&S to {1}", world.ClassyName, value );
                    }
                    world.EdgeLevel = value;
                    break;

                case "edge":
                    Block block = Map.GetBlockByName( valueText );
                    if ( block == Block.Undefined ) {
                        CdEnv.PrintUsage( player );
                        return;
                    }
                    if ( block == Block.Water || valueText.Equals( "normal", StringComparison.OrdinalIgnoreCase ) ) {
                        player.Message( "Reset edge block for {0}&S to normal (water)", world.ClassyName );
                        world.EdgeBlock = Block.Water;
                    } else {
                        string textName = Map.GetEdgeTexture( block );
                        if ( textName == null ) {
                            player.Message( "Cannot use {0} for edge textures.", block );
                            return;
                        } else {
                            world.EdgeBlock = block;
                        }
                    }
                    break;

                case "side":
                case "sides":
                    switch (valueText.ToLower())
                    {
                        case "on":
                            if ( world.SideBlock != Block.Admincrete ) {
                                world.SideBlock = Block.Admincrete;
                                player.Message( "The sides of the world have been restored" );
                            }
                            break;
                        case "off":
                            world.SideBlock = Block.Air;
                            player.Message( "The sides of the world have been removed" );
                            break;
                    }
                    break;

                default:
                    CdEnv.PrintUsage( player );
                    return;
            }

            WorldManager.SaveWorldList();
            if ( player.World == world ) {
                if ( player.IsUsingWoM ) {
                    player.Message( "Rejoin the world to see the changes." );
                } else {
                    player.Message( "You need WoM client to see the changes." );
                }
            }
        }

        private static int ParseHexColor( string text ) {
            byte red, green, blue;
            switch ( text.Length ) {
                case 3:
                    red = ( byte )( HexToValue( text[0] ) * 16 + HexToValue( text[0] ) );
                    green = ( byte )( HexToValue( text[1] ) * 16 + HexToValue( text[1] ) );
                    blue = ( byte )( HexToValue( text[2] ) * 16 + HexToValue( text[2] ) );
                    break;

                case 4:
                    if ( text[0] != '#' )
                        throw new FormatException();
                    red = ( byte )( HexToValue( text[1] ) * 16 + HexToValue( text[1] ) );
                    green = ( byte )( HexToValue( text[2] ) * 16 + HexToValue( text[2] ) );
                    blue = ( byte )( HexToValue( text[3] ) * 16 + HexToValue( text[3] ) );
                    break;

                case 6:
                    red = ( byte )( HexToValue( text[0] ) * 16 + HexToValue( text[1] ) );
                    green = ( byte )( HexToValue( text[2] ) * 16 + HexToValue( text[3] ) );
                    blue = ( byte )( HexToValue( text[4] ) * 16 + HexToValue( text[5] ) );
                    break;

                case 7:
                    if ( text[0] != '#' )
                        throw new FormatException();
                    red = ( byte )( HexToValue( text[1] ) * 16 + HexToValue( text[2] ) );
                    green = ( byte )( HexToValue( text[3] ) * 16 + HexToValue( text[4] ) );
                    blue = ( byte )( HexToValue( text[5] ) * 16 + HexToValue( text[6] ) );
                    break;

                default:
                    throw new FormatException();
            }
            return red * 256 * 256 + green * 256 + blue;
        }

        private static byte HexToValue( char c ) {
            if ( c >= '0' && c <= '9' ) {
                return ( byte )( c - '0' );
            } else if ( c >= 'A' && c <= 'F' ) {
                return ( byte )( c - 'A' + 10 );
            } else if ( c >= 'a' && c <= 'f' ) {
                return ( byte )( c - 'a' + 10 );
            } else {
                throw new FormatException();
            }
        }

        private static void TimeCheck( SchedulerTask task ) {
            foreach ( World world in WorldManager.Worlds ) {
                if ( world.RealisticEnv ) {
                    int sky;
                    int clouds;
                    int fog;
                    DateTime now = DateTime.UtcNow;
                    var SunriseStart = new TimeSpan( 6, 30, 0 );
                    var SunriseEnd = new TimeSpan( 7, 29, 59 );
                    var MorningStart = new TimeSpan( 7, 30, 0 );
                    var MorningEnd = new TimeSpan( 11, 59, 59 );
                    var NormalStart = new TimeSpan( 12, 0, 0 );
                    var NormalEnd = new TimeSpan( 16, 59, 59 );
                    var EveningStart = new TimeSpan( 17, 0, 0 );
                    var EveningEnd = new TimeSpan( 18, 59, 59 );
                    var SunsetStart = new TimeSpan( 19, 0, 0 );
                    var SunsetEnd = new TimeSpan( 19, 29, 59 );
                    var NightaStart = new TimeSpan( 19, 30, 0 );
                    var NightaEnd = new TimeSpan( 1, 0, 1 );
                    var NightbStart = new TimeSpan( 1, 0, 2 );
                    var NightbEnd = new TimeSpan( 6, 29, 59 );

                    if ( now.TimeOfDay > SunriseStart && now.TimeOfDay < SunriseEnd ) //sunrise
                    {
                        sky = ParseHexColor( "ffff33" );
                        clouds = ParseHexColor( "ff0033" );
                        fog = ParseHexColor( "ff3333" );
                        world.SkyColor = sky;
                        world.CloudColor = clouds;
                        world.FogColor = fog;
                        world.EdgeBlock = Block.Water;
                        WorldManager.SaveWorldList();
                        return;
                    }

                    if ( now.TimeOfDay > MorningStart && now.TimeOfDay < MorningEnd ) //end of sunrise
                    {
                        sky = -1;
                        clouds = ParseHexColor( "ff0033" );
                        fog = ParseHexColor( "fffff0" );
                        world.SkyColor = sky;
                        world.CloudColor = clouds;
                        world.FogColor = fog;
                        world.EdgeBlock = Block.Water;
                        WorldManager.SaveWorldList();
                        return;
                    }

                    if ( now.TimeOfDay > NormalStart && now.TimeOfDay < NormalEnd )//env normal
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

                    if ( now.TimeOfDay > EveningStart && now.TimeOfDay < EveningEnd ) //evening
                    {
                        sky = ParseHexColor( "99cccc" );
                        clouds = -1;
                        fog = ParseHexColor( "99ccff" );
                        world.SkyColor = sky;
                        world.CloudColor = clouds;
                        world.FogColor = fog;
                        world.EdgeBlock = Block.Water;
                        WorldManager.SaveWorldList();
                        return;
                    }

                    if ( now.TimeOfDay > SunsetStart && now.TimeOfDay < SunsetEnd ) //sunset
                    {
                        sky = ParseHexColor( "9999cc" );
                        clouds = ParseHexColor( "000033" );
                        fog = ParseHexColor( "cc9966" );
                        world.SkyColor = sky;
                        world.CloudColor = clouds;
                        world.FogColor = fog;
                        world.EdgeBlock = Block.Water;
                        WorldManager.SaveWorldList();
                        return;
                    }

                    if ( now.TimeOfDay > NightaStart && now.TimeOfDay < NightaEnd ) //end of sunset
                    {
                        sky = ParseHexColor( "003366" );
                        clouds = ParseHexColor( "000033" );
                        fog = ParseHexColor( "000033" );
                        world.SkyColor = sky;
                        world.CloudColor = clouds;
                        world.FogColor = fog;
                        world.EdgeBlock = Block.Black;
                        WorldManager.SaveWorldList();
                        return;
                    }

                    if ( now.TimeOfDay > NightbStart && now.TimeOfDay < NightbEnd ) //black
                    {
                        sky = ParseHexColor( "000000" );
                        clouds = ParseHexColor( "000033" );
                        fog = ParseHexColor( "000033" );
                        world.SkyColor = sky;
                        world.CloudColor = clouds;
                        world.FogColor = fog;
                        world.EdgeBlock = Block.Obsidian;
                        WorldManager.SaveWorldList();
                    }
                }
            }
        }

        #endregion Env

        #region Gen

        private static readonly CommandDescriptor CdGenerate = new CommandDescriptor {
            Name = "Gen",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/Gen Theme Template [Width Length Height] [FileName]",
            //Help is assigned by WorldCommands.Init
            Handler = GenHandler
        };

        private static void GenHandler( Player player, Command cmd ) {
            World playerWorld = player.World;
            string themeName = cmd.Next();
            bool genOcean = false;
            bool genEmpty = false;
            bool noTrees = false;

            if ( themeName == null ) {
                CdGenerate.PrintUsage( player );
                return;
            }
            MapGenTheme theme = MapGenTheme.Forest;
            MapGenTemplate template = MapGenTemplate.Flat;

            // parse special template names (which do not need a theme)
            if ( themeName.Equals( "ocean" ) ) {
                genOcean = true;
            } else if ( themeName.Equals( "empty" ) ) {
                genEmpty = true;
            } else {
                string templateName = cmd.Next();
                if ( templateName == null ) {
                    CdGenerate.PrintUsage( player );
                    return;
                }

                // parse theme
                bool swapThemeAndTemplate = false;
                if ( themeName.Equals( "grass", StringComparison.OrdinalIgnoreCase ) ) {
                    theme = MapGenTheme.Forest;
                    noTrees = true;
                } else if ( templateName.Equals( "grass", StringComparison.OrdinalIgnoreCase ) ) {
                    theme = MapGenTheme.Forest;
                    noTrees = true;
                    swapThemeAndTemplate = true;
                } else if ( EnumUtil.TryParse( themeName, out theme, true ) ) {
                    noTrees = ( theme != MapGenTheme.Forest );
                } else if ( EnumUtil.TryParse( templateName, out theme, true ) ) {
                    noTrees = ( theme != MapGenTheme.Forest );
                    swapThemeAndTemplate = true;
                } else {
                    player.Message( "Gen: Unrecognized theme \"{0}\". Available themes are: Grass, {1}",
                                    themeName,
                                    Enum.GetNames( typeof( MapGenTheme ) ).JoinToString() );
                    return;
                }

                // parse template
                if ( swapThemeAndTemplate ) {
                    if ( !EnumUtil.TryParse( themeName, out template, true ) ) {
                        player.Message( "Unrecognized template \"{0}\". Available terrain types: Empty, Ocean, {1}",
                                        themeName,
                                        Enum.GetNames( typeof( MapGenTemplate ) ).JoinToString() );
                        return;
                    }
                } else {
                    if ( !EnumUtil.TryParse( templateName, out template, true ) ) {
                        player.Message( "Unrecognized template \"{0}\". Available terrain types: Empty, Ocean, {1}",
                                        templateName,
                                        Enum.GetNames( typeof( MapGenTemplate ) ).JoinToString() );
                        return;
                    }
                }
            }

            // parse map dimensions
            int mapWidth, mapLength, mapHeight;
            if ( cmd.HasNext ) {
                int offset = cmd.Offset;
                if ( !( cmd.NextInt( out mapWidth ) && cmd.NextInt( out mapLength ) && cmd.NextInt( out mapHeight ) ) ) {
                    if ( playerWorld != null ) {
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
            } else if ( playerWorld != null ) {
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
            if ( !Map.IsValidDimension( mapWidth ) ) {
                player.Message( "Cannot make map with width {0}. {1}", mapWidth, dimensionRecommendation );
                return;
            } else if ( !Map.IsValidDimension( mapLength ) ) {
                player.Message( "Cannot make map with length {0}. {1}", mapLength, dimensionRecommendation );
                return;
            } else if ( !Map.IsValidDimension( mapHeight ) ) {
                player.Message( "Cannot make map with height {0}. {1}", mapHeight, dimensionRecommendation );
                return;
            }
            long volume = ( long )mapWidth * ( long )mapLength * ( long )mapHeight;
            if ( volume > Int32.MaxValue ) {
                player.Message( "Map volume may not exceed {0}", Int32.MaxValue );
                return;
            }

            if ( !cmd.IsConfirmed && ( !Map.IsRecommendedDimension( mapWidth ) || !Map.IsRecommendedDimension( mapLength ) || !Map.IsRecommendedDimension( mapHeight ) ) ) {
                player.Message( "&WThe map will have non-standard dimensions. " +
                                "You may see glitched blocks or visual artifacts. " +
                                "The only recommended map dimensions are: 16, 32, 64, 128, 256, 512, and 1024." );
            }

            // figure out full template name
            bool genFlatgrass = ( theme == MapGenTheme.Forest && noTrees && template == MapGenTemplate.Flat );
            string templateFullName;
            if ( genEmpty ) {
                templateFullName = "Empty";
            } else if ( genOcean ) {
                templateFullName = "Ocean";
            } else if ( genFlatgrass ) {
                templateFullName = "Flatgrass";
            } else {
                if ( theme == MapGenTheme.Forest && noTrees ) {
                    templateFullName = "Grass " + template;
                } else {
                    templateFullName = theme + " " + template;
                }
            }

            // check file/world name
            string fileName = cmd.Next();
            string fullFileName = null;
            if ( fileName == null ) {
                // replacing current world
                if ( playerWorld == null ) {
                    player.Message( "When used from console, /Gen requires FileName." );
                    CdGenerate.PrintUsage( player );
                    return;
                }
                if ( !cmd.IsConfirmed ) {
                    player.Confirm( cmd, "Replace THIS MAP with a generated one ({0})?", templateFullName );
                    return;
                }
            } else {
                if ( cmd.HasNext ) {
                    CdGenerate.PrintUsage( player );
                    return;
                }
                // saving to file
                fileName = fileName.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar );
                if ( !fileName.EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase ) ) {
                    fileName += ".fcm";
                }
                if ( !Paths.IsValidPath( fileName ) ) {
                    player.Message( "Invalid filename." );
                    return;
                }
                fullFileName = Path.Combine( Paths.MapPath, fileName );
                if ( !Paths.Contains( Paths.MapPath, fullFileName ) ) {
                    player.MessageUnsafePath();
                    return;
                }
                string dirName = fullFileName.Substring( 0, fullFileName.LastIndexOf( Path.DirectorySeparatorChar ) );
                if ( !Directory.Exists( dirName ) ) {
                    Directory.CreateDirectory( dirName );
                }
                if ( !cmd.IsConfirmed && File.Exists( fullFileName ) ) {
                    player.Confirm( cmd, "The mapfile \"{0}\" already exists. Overwrite?", fileName );
                    return;
                }
            }

            // generate the map
            Map map;
            player.MessageNow( "Generating {0}...", templateFullName );

            if ( genEmpty ) {
                map = MapGeneratorOld.GenerateEmpty( mapWidth, mapLength, mapHeight );
            } else if ( genOcean ) {
                map = MapGeneratorOld.GenerateOcean( mapWidth, mapLength, mapHeight );
            } else if ( genFlatgrass ) {
                map = MapGeneratorOld.GenerateFlatgrass( mapWidth, mapLength, mapHeight );
            } else {
                MapGeneratorArgs args = MapGeneratorOld.MakeTemplate( template );
                if ( theme == MapGenTheme.Desert ) {
                    args.AddWater = false;
                }
                float ratio = mapHeight / ( float )args.MapHeight;
                args.MapWidth = mapWidth;
                args.MapLength = mapLength;
                args.MapHeight = mapHeight;
                args.MaxHeight = ( int )Math.Round( args.MaxHeight * ratio );
                args.MaxDepth = ( int )Math.Round( args.MaxDepth * ratio );
                args.SnowAltitude = ( int )Math.Round( args.SnowAltitude * ratio );
                args.Theme = theme;
                args.AddTrees = !noTrees;

                MapGeneratorOld generator = new MapGeneratorOld( args );
                map = generator.Generate();
            }

            // save map to file, or load it into a world
            if ( fileName != null ) {
                if ( map.Save( fullFileName ) ) {
                    player.Message( "Generation done. Saved to {0}", fileName );
                } else {
                    player.Message( "&WAn error occured while saving generated map to {0}", fileName );
                }
            } else {
                if ( playerWorld == null )
                    PlayerOpException.ThrowNoWorld( player );
                player.MessageNow( "Generation done. Changing map..." );
                playerWorld.MapChangedBy = player.Name;
                playerWorld.ChangeMap( map );
            }
            Server.RequestGC();
        }

        #endregion Gen

        #region Join

        private static readonly CommandDescriptor CdJoin = new CommandDescriptor {
            Name = "Join",
            Aliases = new[] { "j", "load", "goto", "map" },
            Category = CommandCategory.World,
            Usage = "/Join WorldName",
            Help = "Teleports the player to a specified world. You can see the list of available worlds by using &H/Worlds",
            Handler = JoinHandler
        };

        private static void JoinHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if ( worldName == null ) {
                CdJoin.PrintUsage( player );
                return;
            }

            if ( worldName == "-" ) {
                if ( player.LastUsedWorldName != null ) {
                    worldName = player.LastUsedWorldName;
                } else {
                    player.Message( "Cannot repeat world name: you haven't used any names yet." );
                    return;
                }
            }

            World[] worlds = WorldManager.FindWorlds( player, worldName );

            if ( worlds.Length > 1 ) {
                player.MessageManyMatches( "world", worlds );
            } else if ( worlds.Length == 1 ) {
                World world = worlds[0];
                player.LastUsedWorldName = world.Name;
                switch ( world.AccessSecurity.CheckDetailed( player.Info ) ) {
                    case SecurityCheckResult.Allowed:
                    case SecurityCheckResult.WhiteListed:
                        if ( world.IsFull ) {
                            player.Message( "Cannot join {0}&S: world is full.", world.ClassyName );
                            return;
                        }
                        player.StopSpectating();
                        if ( !player.JoinWorldNow( world, true, WorldChangeReason.ManualJoin ) ) {
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
                if ( players.Length == 1 ) {
                    player.LastUsedPlayerName = players[0].Name;
                    player.StopSpectating();
                    player.ParseMessage( "/TP " + players[0].Name, false );
                } else {
                    player.MessageNoWorld( worldName );
                }
            }
        }

        #endregion Join

        #region WLock, WUnlock

        private static readonly CommandDescriptor CdWorldLock = new CommandDescriptor {
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

        private static void WorldLockHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();

            World world;
            if ( worldName != null ) {
                if ( worldName == "*" ) {
                    int locked = 0;
                    World[] worldListCache = WorldManager.Worlds;
                    foreach (World t in worldListCache)
                    {
                        if ( !t.IsLocked ) {
                            t.Lock( player );
                            locked++;
                        }
                    }
                    player.Message( "Unlocked {0} worlds.", locked );
                    return;
                } else {
                    world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                    if ( world == null )
                        return;
                }
            } else if ( player.World != null ) {
                world = player.World;
            } else {
                player.Message( "When called from console, /WLock requires a world name." );
                return;
            }

            if ( !world.Lock( player ) ) {
                player.Message( "The world is already locked." );
            } else if ( player.World != world ) {
                player.Message( "Locked world {0}", world );
            }
        }

        private static readonly CommandDescriptor CdWorldUnlock = new CommandDescriptor {
            Name = "WUnlock",
            Aliases = new[] { "unlock" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Lock },
            Usage = "/WUnlock [*|WorldName]",
            Help = "Removes the lockdown set by &H/WLock&S. See &H/Help WLock&S for more information.",
            Handler = WorldUnlockHandler
        };

        private static void WorldUnlockHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();

            World world;
            if ( worldName != null ) {
                if ( worldName == "*" ) {
                    World[] worldListCache = WorldManager.Worlds;
                    int unlocked = 0;
                    foreach (World t in worldListCache)
                    {
                        if ( t.IsLocked ) {
                            t.Unlock( player );
                            unlocked++;
                        }
                    }
                    player.Message( "Unlocked {0} worlds.", unlocked );
                    return;
                } else {
                    world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                    if ( world == null )
                        return;
                }
            } else if ( player.World != null ) {
                world = player.World;
            } else {
                player.Message( "When called from console, /WLock requires a world name." );
                return;
            }

            if ( !world.Unlock( player ) ) {
                player.Message( "The world is already unlocked." );
            } else if ( player.World != world ) {
                player.Message( "Unlocked world {0}", world );
            }
        }

        #endregion WLock, WUnlock

        #region Spawn

        private static readonly CommandDescriptor CdSpawn = new CommandDescriptor {
            Name = "Spawn",
            Category = CommandCategory.World,
            Help = "Teleports you to the current map's spawn.",
            Handler = SpawnHandler
        };

        private static void SpawnHandler( Player player, Command cmd ) {
            if ( player.World == null )
                PlayerOpException.ThrowNoWorld( player );
            player.TeleportTo( player.World.LoadMap().Spawn );
        }

        #endregion Spawn

        private static readonly CommandDescriptor CdWorldSet = new CommandDescriptor {
            Name = "WSet",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WSet <World> <Variable> <Value>",
            Help = "Sets a world variable. Variables are: hide, backups, greeting",
            HelpSections = new Dictionary<string, string>{
                { "hide",       "&H/WSet <WorldName> Hide On/Off\n&S" +
                                "When a world is hidden, it does not show up on the &H/Worlds&S list. It can still be joined normally." },
                { "backups",    "&H/WSet <World> Backups Off&S, &H/WSet <World> Backups Default&S, or &H/WSet <World> Backups <Time>\n&S" +
                                "Enables or disables periodic backups. Time is given in the compact format." },
                { "greeting",   "&H/WSet <WorldName> Greeting <Text>\n&S" +
                                "Sets a greeting message. Message is shown whenever someone joins the map, and can also be viewed in &H/WInfo" }
            },
            Handler = WorldSetHandler
        };

        private static void WorldSetHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();
            string varName = cmd.Next();
            string value = cmd.NextAll();
            if ( worldName == null || varName == null ) {
                CdWorldSet.PrintUsage( player );
                return;
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if ( world == null )
                return;

            switch ( varName.ToLower() ) {
                case "hide":
                case "hidden":
                    if ( String.IsNullOrEmpty( value ) ) {
                        player.Message( "World {0}&S is current {1}hidden.",
                                        world.ClassyName,
                                        world.IsHidden ? "" : "NOT " );
                    } else if ( value.Equals( "on", StringComparison.OrdinalIgnoreCase ) ||
                               value.Equals( "true", StringComparison.OrdinalIgnoreCase ) ||
                               value == "1" ) {
                        if ( world.IsHidden ) {
                            player.Message( "World {0}&S is already hidden.", world.ClassyName );
                        } else {
                            player.Message( "World {0}&S is now hidden.", world.ClassyName );
                            world.IsHidden = true;
                            WorldManager.SaveWorldList();
                        }
                    } else if ( value.Equals( "off", StringComparison.OrdinalIgnoreCase ) ||
                               value.Equals( "false", StringComparison.OrdinalIgnoreCase ) ||
                               value == "0" ) {
                        if ( world.IsHidden ) {
                            player.Message( "World {0}&S is no longer hidden.", world.ClassyName );
                            world.IsHidden = false;
                            WorldManager.SaveWorldList();
                        } else {
                            player.Message( "World {0}&S is not hidden.", world.ClassyName );
                        }
                    } else {
                        CdWorldSet.PrintHelpSection( player, "hidden" );
                    }
                    break;

                case "backup":
                case "backups":
                    TimeSpan backupInterval;
                    string oldDescription = world.BackupSettingDescription;
                    if ( String.IsNullOrEmpty( value ) ) {
                        player.Message( GetBackupSettingsString( world ) );
                        return;
                    } else if ( value.Equals( "off", StringComparison.OrdinalIgnoreCase ) ||
                               value.StartsWith( "disable", StringComparison.OrdinalIgnoreCase ) ) {
                        // Disable backups on the world
                        if ( world.BackupEnabledState == YesNoAuto.No ) {
                            MessageSameBackupSettings( player, world );
                            return;
                        } else {
                            world.BackupEnabledState = YesNoAuto.No;
                        }
                    } else if ( value.Equals( "default", StringComparison.OrdinalIgnoreCase ) ||
                               value.Equals( "auto", StringComparison.OrdinalIgnoreCase ) ) {
                        // Set world to use default settings
                        if ( world.BackupEnabledState == YesNoAuto.Auto ) {
                            MessageSameBackupSettings( player, world );
                            return;
                        } else {
                            world.BackupEnabledState = YesNoAuto.Auto;
                        }
                    } else if ( value.TryParseMiniTimespan( out backupInterval ) ) {
                        if ( backupInterval == TimeSpan.Zero ) {
                            // Set world's backup interval to 0, which is equivalent to disabled
                            if ( world.BackupEnabledState == YesNoAuto.No ) {
                                MessageSameBackupSettings( player, world );
                                return;
                            } else {
                                world.BackupEnabledState = YesNoAuto.No;
                            }
                        } else if ( world.BackupEnabledState != YesNoAuto.Yes ||
                                   world.BackupInterval != backupInterval ) {
                            // Alter world's backup interval
                            world.BackupInterval = backupInterval;
                        } else {
                            MessageSameBackupSettings( player, world );
                            return;
                        }
                    } else {
                        CdWorldSet.PrintHelpSection( player, "backups" );
                        return;
                    }
                    player.Message( "Backup setting for world {0}&S changed from \"{1}\" to \"{2}\"",
                                    world.ClassyName, oldDescription, world.BackupSettingDescription );
                    WorldManager.SaveWorldList();
                    break;

                case "description":
                case "greeting":
                    if ( String.IsNullOrEmpty( value ) ) {
                        if ( world.Greeting == null ) {
                            player.Message( "No greeting message is set for world {0}", world.ClassyName );
                        } else {
                            player.Message( "Greeting message removed for world {0}", world.ClassyName );
                            world.Greeting = null;
                        }
                    } else {
                        world.Greeting = value;
                        player.Message( "Greeting message for world {0}&S set to: &R{1}", world.ClassyName, value );
                    }
                    WorldManager.SaveWorldList();
                    break;

                default:
                    CdWorldSet.PrintUsage( player );
                    player.Message( "&S   Variables include: Hidden, Backups and Greeting" );
                    break;
            }
        }

        private static void MessageSameBackupSettings( Player player, World world ) {
            player.Message( "Backup settings for {0}&S are already \"{1}\"",
                            world.ClassyName, world.BackupSettingDescription );
        }

        private static string GetBackupSettingsString( World world ) {
            switch ( world.BackupEnabledState ) {
                case YesNoAuto.Yes:
                    return String.Format( "World {0}&S is backed up every {1}",
                                          world.ClassyName,
                                          world.BackupInterval.ToMiniString() );
                case YesNoAuto.No:
                    return String.Format( "Backups are manually disabled on {0}&S",
                                          world.ClassyName );
                case YesNoAuto.Auto:
                    if ( World.DefaultBackupsEnabled ) {
                        return String.Format( "World {0}&S is backed up every {1} (default)",
                                              world.ClassyName,
                                              WorldManager.DefaultBackupInterval.ToMiniString() );
                    } else {
                        return String.Format( "Backups are disabled on {0}&S (default)",
                                              world.ClassyName );
                    }
                default:
                    // never happens
                    throw new Exception( "Unexpected BackupEnabledState value: " + world.BackupEnabledState );
            }
        }

        #region Worlds

        private static readonly CommandDescriptor CdWorlds = new CommandDescriptor {
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

        private static void WorldsHandler( Player player, Command cmd ) {
            string param = cmd.Next();
            World[] worlds;

            string listName;
            string extraParam;
            int offset = 0;

            if ( param == null || Int32.TryParse( param, out offset ) ) {
                listName = "available worlds";
                extraParam = "";
                worlds = WorldManager.Worlds.Where( w => !w.IsRealm ).Where( player.CanSee ).ToArray();
            } else {
                switch ( Char.ToLower( param[0] ) ) {
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
                        worlds = WorldManager.Worlds.Where( w => w.IsRealm ).ToArray();
                        break;

                    case 'p':
                        listName = "populated worlds";
                        extraParam = "populated ";
                        worlds = WorldManager.Worlds.Where( w => w.Players.Any( player.CanSee ) ).ToArray();
                        break;

                    case '@':
                        if ( param.Length == 1 ) {
                            CdWorlds.PrintUsage( player );
                            return;
                        }
                        string rankName = param.Substring( 1 );
                        Rank rank = RankManager.FindRank( rankName );
                        if ( rank == null ) {
                            player.MessageNoRank( rankName );
                            return;
                        }
                        listName = String.Format( "worlds where {0}&S+ can build", rank.ClassyName );
                        extraParam = "@" + rank.Name + " ";
                        worlds = WorldManager.Worlds.Where( w => ( w.BuildSecurity.MinRank <= rank ) && player.CanSee( w ) )
                                                    .ToArray();
                        break;

                    default:
                        CdWorlds.PrintUsage( player );
                        return;
                }
                if ( cmd.HasNext && !cmd.NextInt( out offset ) ) {
                    CdWorlds.PrintUsage( player );
                    return;
                }
            }

            if ( worlds.Length == 0 ) {
                player.Message( "There are no {0}.", listName );
            } else if ( worlds.Length <= WorldNamesPerPage || player.IsSuper ) {
                player.MessagePrefixed( "&S  ", "&SThere are {0} {1}: {2}",
                                        worlds.Length, listName, worlds.JoinToClassyString() );
            } else {
                if ( offset >= worlds.Length ) {
                    offset = Math.Max( 0, worlds.Length - WorldNamesPerPage );
                }
                World[] worldsPart = worlds.Skip( offset ).Take( WorldNamesPerPage ).ToArray();
                player.MessagePrefixed( "&S   ", "&S{0}: {1}",
                                        listName.UppercaseFirst(), worldsPart.JoinToClassyString() );

                if ( offset + worldsPart.Length < worlds.Length ) {
                    player.Message( "Showing {0}-{1} (out of {2}). Next: &H/Worlds {3}{1}",
                                    offset + 1, offset + worldsPart.Length, worlds.Length, extraParam );
                } else {
                    player.Message( "Showing worlds {0}-{1} (out of {2}).",
                                    offset + 1, offset + worldsPart.Length, worlds.Length );
                }
            }
        }

        #endregion Worlds

        #region WorldAccess

        private static readonly CommandDescriptor CdWorldAccess = new CommandDescriptor {
            Name = "WAccess",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WAccess [WorldName [RankName]]",
            Help = "&HShows access permission for player's current world. " +
                   "If optional WorldName parameter is given, shows access permission for another world. " +
                   "If RankName parameter is also given, sets access permission for specified world.",
            Handler = WorldAccessHandler
        };

        private static void WorldAccessHandler( [NotNull] Player player, Command cmd ) {
            if ( player == null )
                throw new ArgumentNullException( "player" );
            string worldName = cmd.Next();

            // Print information about the current world
            if ( worldName == null ) {
                if ( player.World == null ) {
                    player.Message( "When calling /WAccess from console, you must specify a world name." );
                } else {
                    player.Message( player.World.AccessSecurity.GetDescription( player.World, "world", "accessed" ) );
                }
                return;
            }

            // Find a world by name
            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if ( world == null )
                return;

            string name = cmd.Next();
            if ( name == null ) {
                player.Message( world.AccessSecurity.GetDescription( world, "world", "accessed" ) );
                return;
            }
            if ( world == WorldManager.MainWorld ) {
                player.Message( "The main world cannot have access restrictions." );
                return;
            }

            bool changesWereMade = false;
            do {
                // Whitelisting individuals
                if ( name.StartsWith( "+" ) ) {
                    if ( name.Length == 1 ) {
                        CdWorldAccess.PrintUsage( player );
                        break;
                    }
                    PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches( player, name.Substring( 1 ) );
                    if ( info == null )
                        return;

                    // prevent players from whitelisting themselves to bypass protection
                    if ( player.Info == info && !player.Info.Rank.AllowSecurityCircumvention ) {
                        switch ( world.AccessSecurity.CheckDetailed( player.Info ) ) {
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

                    if ( world.AccessSecurity.CheckDetailed( info ) == SecurityCheckResult.Allowed ) {
                        player.Message( "{0}&S is already allowed to access {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName );
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if ( target == player )
                        target = null; // to avoid duplicate messages

                    switch ( world.AccessSecurity.Include( info ) ) {
                        case PermissionOverride.Deny:
                            if ( world.AccessSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer barred from accessing {1}",
                                                info.ClassyName, world.ClassyName );
                                if ( target != null ) {
                                    target.Message( "You can now access world {0}&S (removed from blacklist by {1}&S).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            } else {
                                player.Message( "{0}&S was removed from the access blacklist of {1}&S. " +
                                                "Player is still NOT allowed to join (by rank).",
                                                info.ClassyName, world.ClassyName );
                                if ( target != null ) {
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
                            if ( target != null ) {
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
                } else if ( name.StartsWith( "-" ) ) {
                    if ( name.Length == 1 ) {
                        CdWorldAccess.PrintUsage( player );
                        break;
                    }
                    PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches( player, name.Substring( 1 ) );
                    if ( info == null )
                        return;

                    if ( world.AccessSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooHigh ||
                        world.AccessSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooLow ) {
                        player.Message( "{0}&S is already barred from accessing {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName );
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if ( target == player )
                        target = null; // to avoid duplicate messages

                    switch ( world.AccessSecurity.Exclude( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is already on access blacklist of {1}",
                                            info.ClassyName, world.ClassyName );
                            break;

                        case PermissionOverride.None:
                            player.Message( "{0}&S is now barred from accessing {1}",
                                            info.ClassyName, world.ClassyName );
                            if ( target != null ) {
                                target.Message( "&WYou were barred by {0}&W from accessing world {1}",
                                                player.ClassyName, world.ClassyName );
                            }
                            Logger.Log( LogType.UserActivity,
                                        "{0} added {1} to the access blacklist on world {2}",
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            if ( world.AccessSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer on the access whitelist of {1}&S. " +
                                                "Player is still allowed to join (by rank).",
                                                info.ClassyName, world.ClassyName );
                                if ( target != null ) {
                                    target.Message( "You were removed from the access whitelist of world {0}&S by {1}&S. " +
                                                    "You are still allowed to join (by rank).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            } else {
                                player.Message( "{0}&S is no longer allowed to access {1}",
                                                info.ClassyName, world.ClassyName );
                                if ( target != null ) {
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
                    if ( rank == null ) {
                        player.MessageNoRank( name );
                    } else if ( !player.Info.Rank.AllowSecurityCircumvention &&
                               world.AccessSecurity.MinRank > rank &&
                               world.AccessSecurity.MinRank > player.Info.Rank ) {
                        player.Message( "&WYou must be ranked {0}&W+ to lower the access rank for world {1}",
                                        world.AccessSecurity.MinRank.ClassyName, world.ClassyName );
                    } else {
                        // list players who are redundantly blacklisted
                        var exceptionList = world.AccessSecurity.ExceptionList;
                        PlayerInfo[] noLongerExcluded = exceptionList.Excluded.Where( excludedPlayer => excludedPlayer.Rank < rank ).ToArray();
                        if ( noLongerExcluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be blacklisted to be barred from {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerExcluded.JoinToClassyString() );
                        }

                        // list players who are redundantly whitelisted
                        PlayerInfo[] noLongerIncluded = exceptionList.Included.Where( includedPlayer => includedPlayer.Rank >= rank ).ToArray();
                        if ( noLongerIncluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be whitelisted to access {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerIncluded.JoinToClassyString() );
                        }

                        // apply changes
                        world.AccessSecurity.MinRank = rank;
                        changesWereMade = true;
                        if ( world.AccessSecurity.MinRank == RankManager.LowestRank ) {
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
            } while ( ( name = cmd.Next() ) != null );

            if ( changesWereMade ) {
                var playersWhoCantStay = world.Players.Where( p => !p.CanJoin( world ) );
                foreach ( Player p in playersWhoCantStay ) {
                    p.Message( "&WYou are no longer allowed to join world {0}", world.ClassyName );
                    p.JoinWorld( WorldManager.MainWorld, WorldChangeReason.PermissionChanged );
                }
                WorldManager.SaveWorldList();
            }
        }

        #endregion WorldAccess

        #region WorldBuild

        private static readonly CommandDescriptor CdWorldBuild = new CommandDescriptor {
            Name = "WBuild",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WBuild [WorldName [RankName]]",
            Help = "&HShows build permissions for player's current world. " +
                   "If optional WorldName parameter is given, shows build permission for another world. " +
                   "If RankName parameter is also given, sets build permission for specified world.",
            Handler = WorldBuildHandler
        };

        private static void WorldBuildHandler( [NotNull] Player player, Command cmd ) {
            if ( player == null )
                throw new ArgumentNullException( "player" );
            string worldName = cmd.Next();

            // Print information about the current world
            if ( worldName == null ) {
                if ( player.World == null ) {
                    player.Message( "When calling /WBuild from console, you must specify a world name." );
                } else {
                    player.Message( player.World.BuildSecurity.GetDescription( player.World, "world", "modified" ) );
                }
                return;
            }

            // Find a world by name
            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if ( world == null )
                return;

            string name = cmd.Next();
            if ( name == null ) {
                player.Message( world.BuildSecurity.GetDescription( world, "world", "modified" ) );
                return;
            }

            bool changesWereMade = false;
            do {
                // Whitelisting individuals
                if ( name.StartsWith( "+" ) ) {
                    if ( name.Length == 1 ) {
                        CdWorldBuild.PrintUsage( player );
                        break;
                    }
                    PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches( player, name.Substring( 1 ) );
                    if ( info == null )
                        return;

                    // prevent players from whitelisting themselves to bypass protection
                    if ( player.Info == info && !player.Info.Rank.AllowSecurityCircumvention ) {
                        switch ( world.BuildSecurity.CheckDetailed( player.Info ) ) {
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

                    if ( world.BuildSecurity.CheckDetailed( info ) == SecurityCheckResult.Allowed ) {
                        player.Message( "{0}&S is already allowed to build in {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName );
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if ( target == player )
                        target = null; // to avoid duplicate messages

                    switch ( world.BuildSecurity.Include( info ) ) {
                        case PermissionOverride.Deny:
                            if ( world.BuildSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer barred from building in {1}",
                                                info.ClassyName, world.ClassyName );
                                if ( target != null ) {
                                    target.Message( "You can now build in world {0}&S (removed from blacklist by {1}&S).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            } else {
                                player.Message( "{0}&S was removed from the build blacklist of {1}&S. " +
                                                "Player is still NOT allowed to build (by rank).",
                                                info.ClassyName, world.ClassyName );
                                if ( target != null ) {
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
                            if ( target != null ) {
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
                } else if ( name.StartsWith( "-" ) ) {
                    if ( name.Length == 1 ) {
                        CdWorldBuild.PrintUsage( player );
                        break;
                    }
                    PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches( player, name.Substring( 1 ) );
                    if ( info == null )
                        return;

                    if ( world.BuildSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooHigh ||
                        world.BuildSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooLow ) {
                        player.Message( "{0}&S is already barred from building in {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName );
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if ( target == player )
                        target = null; // to avoid duplicate messages

                    switch ( world.BuildSecurity.Exclude( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is already on build blacklist of {1}",
                                            info.ClassyName, world.ClassyName );
                            break;

                        case PermissionOverride.None:
                            player.Message( "{0}&S is now barred from building in {1}",
                                            info.ClassyName, world.ClassyName );
                            if ( target != null ) {
                                target.Message( "&WYou were barred by {0}&W from building in world {1}",
                                                player.ClassyName, world.ClassyName );
                            }
                            Logger.Log( LogType.UserActivity,
                                        "{0} added {1} to the build blacklist on world {2}",
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            if ( world.BuildSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer on the build whitelist of {1}&S. " +
                                                "Player is still allowed to build (by rank).",
                                                info.ClassyName, world.ClassyName );
                                if ( target != null ) {
                                    target.Message( "You were removed from the build whitelist of world {0}&S by {1}&S. " +
                                                    "You are still allowed to build (by rank).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            } else {
                                player.Message( "{0}&S is no longer allowed to build in {1}",
                                                info.ClassyName, world.ClassyName );
                                if ( target != null ) {
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
                    if ( rank == null ) {
                        player.MessageNoRank( name );
                    } else if ( !player.Info.Rank.AllowSecurityCircumvention &&
                               world.BuildSecurity.MinRank > rank &&
                               world.BuildSecurity.MinRank > player.Info.Rank ) {
                        player.Message( "&WYou must be ranked {0}&W+ to lower build restrictions for world {1}",
                                        world.BuildSecurity.MinRank.ClassyName, world.ClassyName );
                    } else {
                        // list players who are redundantly blacklisted
                        var exceptionList = world.BuildSecurity.ExceptionList;
                        PlayerInfo[] noLongerExcluded = exceptionList.Excluded.Where( excludedPlayer => excludedPlayer.Rank < rank ).ToArray();
                        if ( noLongerExcluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be blacklisted on world {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerExcluded.JoinToClassyString() );
                        }

                        // list players who are redundantly whitelisted
                        PlayerInfo[] noLongerIncluded = exceptionList.Included.Where( includedPlayer => includedPlayer.Rank >= rank ).ToArray();
                        if ( noLongerIncluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be whitelisted on world {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerIncluded.JoinToClassyString() );
                        }

                        // apply changes
                        world.BuildSecurity.MinRank = rank;
                        if ( BlockDB.IsEnabledGlobally && world.BlockDB.AutoToggleIfNeeded() ) {
                            if ( world.BlockDB.IsEnabled ) {
                                player.Message( "BlockDB is now auto-enabled on world {0}",
                                                world.ClassyName );
                            } else {
                                player.Message( "BlockDB is now auto-disabled on world {0}",
                                                world.ClassyName );
                            }
                        }
                        changesWereMade = true;
                        if ( world.BuildSecurity.MinRank == RankManager.LowestRank ) {
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
            } while ( ( name = cmd.Next() ) != null );

            if ( changesWereMade ) {
                WorldManager.SaveWorldList();
            }
        }

        #endregion WorldBuild

        #region WorldFlush

        private static readonly CommandDescriptor CdWorldFlush = new CommandDescriptor {
            Name = "WFlush",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WFlush [WorldName]",
            Help = "&HFlushes the update buffer on specified map by causing players to rejoin. " +
                   "Makes cuboids and other draw commands finish REALLY fast.",
            Handler = WorldFlushHandler
        };

        private static void WorldFlushHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();
            World world = player.World;

            if ( worldName != null ) {
                world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                if ( world == null )
                    return;
            } else if ( world == null ) {
                player.Message( "When using /WFlush from console, you must specify a world name." );
                return;
            }

            Map map = world.Map;
            if ( map == null ) {
                player.MessageNow( "WFlush: {0}&S has no updates to process.",
                                   world.ClassyName );
            } else {
                player.MessageNow( "WFlush: Flushing {0}&S ({1} blocks)...",
                                   world.ClassyName,
                                   map.UpdateQueueLength + map.DrawQueueBlockCount );
                world.Flush();
            }
        }

        #endregion WorldFlush

        #region WorldInfo

        private static readonly CommandDescriptor CdWorldInfo = new CommandDescriptor {
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

        private static void WorldInfoHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if ( worldName == null ) {
                if ( player.World == null ) {
                    player.Message( "Please specify a world name when calling /WInfo from console." );
                    return;
                } else {
                    worldName = player.World.Name;
                }
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if ( world == null )
                return;

            player.Message( "World {0}&S has {1} player(s) on.",
                            world.ClassyName,
                            world.CountVisiblePlayers( player ) );

            Map map = world.Map;

            // If map is not currently loaded, grab its header from disk
            if ( map == null ) {
                try {
                    map = MapUtility.LoadHeader( Path.Combine( Paths.MapPath, world.MapFileName ) );
                } catch ( Exception ex ) {
                    player.Message( "  Map information could not be loaded: {0}: {1}",
                                    ex.GetType().Name, ex.Message );
                }
            }

            if ( map != null ) {
                player.Message( "  Map dimensions are {0} x {1} x {2}",
                                map.Width, map.Length, map.Height );
            }

            // Print access/build limits
            player.Message( "  " + world.AccessSecurity.GetDescription( world, "world", "accessed" ) );
            player.Message( "  " + world.BuildSecurity.GetDescription( world, "world", "modified" ) );

            // Print lock/unlock information
            if ( world.IsLocked ) {
                player.Message( "  {0}&S was locked {1} ago by {2}",
                                world.ClassyName,
                                DateTime.UtcNow.Subtract( world.LockedDate ).ToMiniString(),
                                world.LockedBy );
            } else if ( world.UnlockedBy != null ) {
                player.Message( "  {0}&S was unlocked {1} ago by {2}",
                                world.ClassyName,
                                DateTime.UtcNow.Subtract( world.UnlockedDate ).ToMiniString(),
                                world.UnlockedBy );
            }

            if ( !String.IsNullOrEmpty( world.LoadedBy ) && world.LoadedOn != DateTime.MinValue ) {
                player.Message( "  {0}&S was created/loaded {1} ago by {2}",
                                world.ClassyName,
                                DateTime.UtcNow.Subtract( world.LoadedOn ).ToMiniString(),
                                world.LoadedByClassy );
            }

            if ( !String.IsNullOrEmpty( world.MapChangedBy ) && world.MapChangedOn != DateTime.MinValue ) {
                player.Message( "  Map was last changed {0} ago by {1}",
                                DateTime.UtcNow.Subtract( world.MapChangedOn ).ToMiniString(),
                                world.MapChangedByClassy );
            }

            if ( world.BlockDB.IsEnabled ) {
                if ( world.BlockDB.EnabledState == YesNoAuto.Auto ) {
                    player.Message( "  BlockDB is enabled (auto) on {0}", world.ClassyName );
                } else {
                    player.Message( "  BlockDB is enabled on {0}", world.ClassyName );
                }
            } else {
                player.Message( "  BlockDB is disabled on {0}", world.ClassyName );
            }

            if ( world.BackupInterval == TimeSpan.Zero ) {
                if ( WorldManager.DefaultBackupInterval != TimeSpan.Zero ) {
                    player.Message( "  Periodic backups are disabled on {0}", world.ClassyName );
                }
            } else {
                player.Message( "  Periodic backups every {0}", world.BackupInterval.ToMiniString() );
            }
            if ( world.VisitCount > 0 ) {
                player.Message( "  This world has been visited {0} times", world.VisitCount );
            }
        }

        #endregion WorldInfo

        #region WorldLoad

        private static readonly CommandDescriptor CdWorldLoad = new CommandDescriptor {
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

        private static void WorldLoadHandler( Player player, Command cmd ) {
            string fileName = cmd.Next();
            string worldName = cmd.Next();

            if ( worldName == null && player.World == null ) {
                player.Message( "When using /WLoad from console, you must specify the world name." );
                return;
            }

            if ( fileName == null ) {
                // No params given at all
                CdWorldLoad.PrintUsage( player );
                return;
            }

            string fullFileName = WorldManager.FindMapFile( player, fileName );
            if ( fullFileName == null )
                return;

            // Loading map into current world
            if ( worldName == null ) {
                if ( !cmd.IsConfirmed ) {
                    player.Confirm( cmd, "Replace THIS MAP with \"{0}\"?", fileName );
                    return;
                }
                Map map;
                try {
                    map = MapUtility.Load( fullFileName );
                } catch ( Exception ex ) {
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
                if ( !World.IsValidName( worldName ) ) {
                    player.MessageInvalidWorldName( worldName );
                    return;
                }

                string buildRankName = cmd.Next();
                string accessRankName = cmd.Next();
                Rank buildRank = RankManager.DefaultBuildRank;
                Rank accessRank = null;
                if ( buildRankName != null ) {
                    buildRank = RankManager.FindRank( buildRankName );
                    if ( buildRank == null ) {
                        player.MessageNoRank( buildRankName );
                        return;
                    }
                    if ( accessRankName != null ) {
                        accessRank = RankManager.FindRank( accessRankName );
                        if ( accessRank == null ) {
                            player.MessageNoRank( accessRankName );
                            return;
                        }
                    }
                }

                // Retype world name, if needed
                if ( worldName == "-" ) {
                    if ( player.LastUsedWorldName != null ) {
                        worldName = player.LastUsedWorldName;
                    } else {
                        player.Message( "Cannot repeat world name: you haven't used any names yet." );
                        return;
                    }
                }

                lock ( WorldManager.SyncRoot ) {
                    World world = WorldManager.FindWorldExact( worldName );
                    if ( world != null ) {
                        player.LastUsedWorldName = world.Name;
                        // Replacing existing world's map
                        if ( !cmd.IsConfirmed ) {
                            player.Confirm( cmd, "Replace map for {0}&S with \"{1}\"?",
                                            world.ClassyName, fileName );
                            return;
                        }

                        Map map;
                        try {
                            map = MapUtility.Load( fullFileName );
                        } catch ( Exception ex ) {
                            player.MessageNow( "Could not load specified file: {0}: {1}", ex.GetType().Name, ex.Message );
                            return;
                        }

                        try {
                            world.MapChangedBy = player.Name;
                            world.ChangeMap( map );
                        } catch ( WorldOpException ex ) {
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
                        if ( !cmd.IsConfirmed &&
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
                        } catch ( Exception ex ) {
                            player.MessageNow( "Could not load \"{0}\": {1}: {2}",
                                               fileName, ex.GetType().Name, ex.Message );
                            return;
                        }

                        World newWorld;
                        try {
                            newWorld = WorldManager.AddWorld( player, worldName, map, false );
                        } catch ( WorldOpException ex ) {
                            player.Message( "WLoad: {0}", ex.Message );
                            return;
                        }

                        if ( newWorld == null ) {
                            player.MessageNow( "Failed to create a new world." );
                            return;
                        }

                        player.LastUsedWorldName = worldName;
                        newWorld.BuildSecurity.MinRank = buildRank;
                        if ( accessRank == null ) {
                            newWorld.AccessSecurity.ResetMinRank();
                        } else {
                            newWorld.AccessSecurity.MinRank = accessRank;
                        }
                        newWorld.BlockDB.AutoToggleIfNeeded();
                        if ( BlockDB.IsEnabledGlobally && newWorld.BlockDB.IsEnabled ) {
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

        #endregion WorldLoad

        #region WorldMain

        private static readonly CommandDescriptor CdWorldMain = new CommandDescriptor {
            Name = "WMain",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WMain [@RankName] [WorldName]",
            Help = "&HSets the specified world as the new main world. " +
                   "Main world is what newly-connected players join first. " +
                   "You can specify a rank name to set a different starting world for that particular rank.",
            Handler = WorldMainHandler
        };

        private static void WorldMainHandler( Player player, Command cmd ) {
            string param = cmd.Next();
            if ( param == null ) {
                player.Message( "Main world is {0}", WorldManager.MainWorld.ClassyName );
                var mainedRanks = RankManager.Ranks
                                             .Where( r => r.MainWorld != null && r.MainWorld != WorldManager.MainWorld );
                if ( mainedRanks.Count() > 0 ) {
                    player.Message( "Rank mains: {0}",
                                    mainedRanks.JoinToString( r => String.Format( "{0}&S for {1}&S",
                                        // ReSharper disable PossibleNullReferenceException
                                                                                  r.MainWorld.ClassyName,
                                        // ReSharper restore PossibleNullReferenceException
                                                                                  r.ClassyName ) ) );
                }
                return;
            }

            if ( param.StartsWith( "@" ) ) {
                string rankName = param.Substring( 1 );
                Rank rank = RankManager.FindRank( rankName );
                if ( rank == null ) {
                    player.MessageNoRank( rankName );
                    return;
                }
                string worldName = cmd.Next();
                if ( worldName == null ) {
                    if ( rank.MainWorld != null ) {
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
                    if ( world != null ) {
                        SetRankMainWorld( player, rank, world );
                    }
                }
            } else {
                World world = WorldManager.FindWorldOrPrintMatches( player, param );
                if ( world != null ) {
                    SetMainWorld( player, world );
                }
            }
        }

        private static void SetRankMainWorld( Player player, Rank rank, World world ) {
            if ( world == rank.MainWorld ) {
                player.Message( "World {0}&S is already set as main for {1}&S.",
                                world.ClassyName, rank.ClassyName );
                return;
            }

            if ( world == WorldManager.MainWorld ) {
                if ( rank.MainWorld == null ) {
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

            if ( world.AccessSecurity.MinRank > rank ) {
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

        private static void SetMainWorld( Player player, World world ) {
            if ( world == WorldManager.MainWorld ) {
                player.Message( "World {0}&S is already set as main.", world.ClassyName );
            } else if ( !player.Info.Rank.AllowSecurityCircumvention && !player.CanJoin( world ) ) {
                // Prevent players from exploiting /WMain to gain access to restricted maps
                switch ( world.AccessSecurity.CheckDetailed( player.Info ) ) {
                    case SecurityCheckResult.RankTooHigh:
                    case SecurityCheckResult.RankTooLow:
                        player.Message( "You are not allowed to set {0}&S as the main world (by rank).", world.ClassyName );
                        return;

                    case SecurityCheckResult.BlackListed:
                        player.Message( "You are not allowed to set {0}&S as the main world (blacklisted).", world.ClassyName );
                        return;
                }
            } else {
                if ( world.AccessSecurity.HasRestrictions ) {
                    world.AccessSecurity.Reset();
                    player.Message( "The main world cannot have access restrictions. " +
                                    "All access restrictions were removed from world {0}",
                                    world.ClassyName );
                }

                try {
                    WorldManager.MainWorld = world;
                } catch ( WorldOpException ex ) {
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

        #endregion WorldMain

        #region WorldRename

        private static readonly CommandDescriptor CdWorldRename = new CommandDescriptor {
            Name = "WRename",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WRename OldName NewName",
            Help = "&HChanges the name of a world. Does not require any reloading.",
            Handler = WorldRenameHandler
        };

        private static void WorldRenameHandler( Player player, Command cmd ) {
            string oldName = cmd.Next();
            string newName = cmd.Next();
            if ( oldName == null || newName == null ) {
                CdWorldRename.PrintUsage( player );
                return;
            }

            World oldWorld = WorldManager.FindWorldOrPrintMatches( player, oldName );
            if ( oldWorld == null )
                return;
            oldName = oldWorld.Name;

            if ( !World.IsValidName( newName ) ) {
                player.MessageInvalidWorldName( newName );
                return;
            }

            World newWorld = WorldManager.FindWorldExact( newName );
            if ( !cmd.IsConfirmed && newWorld != null && newWorld != oldWorld ) {
                player.Confirm( cmd, "A world named {0}&S already exists. Replace it?", newWorld.ClassyName );
                return;
            }

            if ( !cmd.IsConfirmed && File.Exists( Path.Combine( Paths.MapPath, newName + ".fcm" ) ) ) {
                player.Confirm( cmd, "Renaming this world will overwrite an existing map file \"{0}.fcm\".", newName );
                return;
            }

            try {
                WorldManager.RenameWorld( oldWorld, newName, true, true );
            } catch ( WorldOpException ex ) {
                switch ( ex.ErrorCode ) {
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

        #endregion WorldRename

        #region WorldSave

        private static readonly CommandDescriptor CdWorldSave = new CommandDescriptor {
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

        private static void WorldSaveHandler( Player player, Command cmd ) {
            string p1 = cmd.Next(), p2 = cmd.Next();
            if ( p1 == null ) {
                CdWorldSave.PrintUsage( player );
                return;
            }

            World world = player.World;
            string fileName;
            if ( p2 == null ) {
                fileName = p1;
                if ( world == null ) {
                    player.Message( "When called from console, /wsave requires WorldName. See \"/Help save\" for details." );
                    return;
                }
            } else {
                world = WorldManager.FindWorldOrPrintMatches( player, p1 );
                if ( world == null )
                    return;
                fileName = p2;
            }

            // normalize the path
            fileName = fileName.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar );
            if ( fileName.EndsWith( "/" ) && fileName.EndsWith( @"\" ) ) {
                fileName += world.Name + ".fcm";
            } else if ( !fileName.ToLower().EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase ) ) {
                fileName += ".fcm";
            }
            if ( !Paths.IsValidPath( fileName ) ) {
                player.Message( "Invalid filename." );
                return;
            }
            string fullFileName = Path.Combine( Paths.MapPath, fileName );
            if ( !Paths.Contains( Paths.MapPath, fullFileName ) ) {
                player.MessageUnsafePath();
                return;
            }

            // Ask for confirmation if overwriting
            if ( File.Exists( fullFileName ) ) {
                FileInfo targetFile = new FileInfo( fullFileName );
                FileInfo sourceFile = new FileInfo( world.MapFileName );
                if ( !targetFile.FullName.Equals( sourceFile.FullName, StringComparison.OrdinalIgnoreCase ) ) {
                    if ( !cmd.IsConfirmed ) {
                        player.Confirm( cmd, "Target file \"{0}\" already exists, and will be overwritten.", targetFile.Name );
                        return;
                    }
                }
            }

            // Create the target directory if it does not exist
            string dirName = fullFileName.Substring( 0, fullFileName.LastIndexOf( Path.DirectorySeparatorChar ) );
            if ( !Directory.Exists( dirName ) ) {
                Directory.CreateDirectory( dirName );
            }

            player.MessageNow( "Saving map to {0}", fileName );

            const string mapSavingErrorMessage = "Map saving failed. See server logs for details.";
            Map map = world.Map;
            if ( map == null ) {
                if ( File.Exists( world.MapFileName ) ) {
                    try {
                        File.Copy( world.MapFileName, fullFileName, true );
                    } catch ( Exception ex ) {
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
            } else if ( map.Save( fullFileName ) ) {
                player.Message( "Map saved succesfully." );
            } else {
                Logger.Log( LogType.Error,
                            "WorldCommands.WorldSave: Saving world \"{0}\" failed.", world.Name );
                player.Message( mapSavingErrorMessage );
            }
        }

        #endregion WorldSave

        #region WorldUnload

        private static readonly CommandDescriptor CdWorldUnload = new CommandDescriptor {
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

        private static void WorldUnloadHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if ( worldName == null ) {
                CdWorldUnload.PrintUsage( player );
                return;
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if ( world == null )
                return;

            try {
                WorldManager.RemoveWorld( world );
            } catch ( WorldOpException ex ) {
                switch ( ex.ErrorCode ) {
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

        #endregion WorldUnload
    }
}