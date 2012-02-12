// Copyright 2009, 2010, 2011, 2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using fCraft.Events;
using JetBrains.Annotations;
using System.IO;

namespace fCraft {
    /// <summary>
    /// Most commands for server moderation - kick, ban, rank change, etc - are here.
    /// </summary>
    static class ModerationCommands {
        const string BanCommonHelp = "Ban information can be viewed with &H/BanInfo";

        internal static void Init() {
            CdBan.Help += BanCommonHelp;
            CdBanIP.Help += BanCommonHelp;
            CdBanAll.Help += BanCommonHelp;
            CdUnban.Help += BanCommonHelp;
            CdUnbanIP.Help += BanCommonHelp;
            CdUnbanAll.Help += BanCommonHelp;

            CommandManager.RegisterCommand( CdBan );
            CommandManager.RegisterCommand( CdBanIP );
            CommandManager.RegisterCommand( CdBanAll );
            CommandManager.RegisterCommand( CdUnban );
            CommandManager.RegisterCommand( CdUnbanIP );
            CommandManager.RegisterCommand( CdUnbanAll );

            CommandManager.RegisterCommand( CdBanEx );

            CommandManager.RegisterCommand( CdKick );

            CommandManager.RegisterCommand( CdRank );

            CommandManager.RegisterCommand( CdHide );
            CommandManager.RegisterCommand( CdUnhide );

            CommandManager.RegisterCommand( CdSetSpawn );

            CommandManager.RegisterCommand( CdFreeze );
            CommandManager.RegisterCommand( CdUnfreeze );

            CommandManager.RegisterCommand( CdTP );
            CommandManager.RegisterCommand( CdBring );
            CommandManager.RegisterCommand( CdWorldBring );
            CommandManager.RegisterCommand( CdBringAll );

            CommandManager.RegisterCommand( CdPatrol );
            CommandManager.RegisterCommand( CdSpecPatrol );

            CommandManager.RegisterCommand( CdMute );
            CommandManager.RegisterCommand( CdUnmute );

            CommandManager.RegisterCommand( CdSpectate );
            CommandManager.RegisterCommand( CdUnspectate );

            CommandManager.RegisterCommand(CdSlap);
            CommandManager.RegisterCommand(CdTPZone);
            CommandManager.RegisterCommand(CdBasscannon);
            CommandManager.RegisterCommand(CdKill);
            CommandManager.RegisterCommand(CdTempBan);
            //CommandManager.RegisterCommand(CdPossess);
            //CommandManager.RegisterCommand(CdUnPossess);
            CommandManager.RegisterCommand(CdWarn);
            CommandManager.RegisterCommand(CdUnWarn);
            CommandManager.RegisterCommand(CdDisconnect);
            CommandManager.RegisterCommand(CdDummy);
            Player.Moving += DummyAI.DummyFollowing;
            Player.Moving += DummyAI.DummyTurn;
            Player.Disconnected += DummyAI.Player_Disconnected;
        }
        
        #region custom
        public static List<string> BassText = new List<string>();
        static readonly CommandDescriptor CdDummy = new CommandDescriptor
        {
            Name = "Dummy",
            Aliases = new[] { "makedummys", "makedummy", "bot" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = false,
            Permissions = new[] { Permission.MakeDummys },
            Help = "Makes a dummy player.",
            NotRepeatable = false,
            Usage = "/dummy Create | Undo | List | Bring | Follow",
            Handler = DummyHandler
        };

        public static void DummyHandler(Player player, Command cmd)
        {
            string option = cmd.Next();
            World world = player.World;

            switch (option)
            {
                case "create":

                    string name = cmd.Next();

                    if (name == null)
                    {
                        CdDummy.PrintUsage(player);
                        return;
                    }
                   
                    if (name.Length < 2)
                    {
                        player.Message("&WThe name you have chosen is too small");
                        return;
                    }

                    if (player.World.Name != WorldManager.MainWorld.Name)
                    {
                        player.Message("&WDummys can only be made on the main world");
                        return;
                    }
                    try
                    {
                        Player dummy = new Player(name);
                        Position pos = player.Position;
                        dummy.Info.ID = world.Map.Dummys.Count() + 1;
                        name = Color.ReplacePercentCodes(name); //classynames
                        world.Players.Send(PacketWriter.MakeAddEntity(dummy.Info.ID, name, pos)); //makes the dummy
                        world.Map.Dummys.Add(dummy); //adds the dummy to a list
                       
                        //used for reloading dummies.
                        dummy.Info.DummyID = dummy.Info.ID;
                        dummy.Info.DummyName = name;
                        dummy.Info.DummyPos = pos;
                        player.Message("&8Created dummy &F{0} &8with ID &A{1}", dummy.Info.DummyName, dummy.Info.ID);
                        
                    }

                    catch (Exception ex)
                    {
                        Logger.Log(LogType.Error, "ModerationCommands.DummyHandler: " + ex);
                    }
                    break;

                case "bring":
                case "summon":
                case "teleport":
                case "tp":
                    string ID = cmd.Next();

                    if(ID != null)

                        foreach (Player T in world.Map.Dummys)
                        {
                            double Num;
                            bool isNum = double.TryParse(ID, out Num);
                            if (isNum)
                            {
                                int ID2 = Convert.ToInt16(ID);
                                int count = 0;
                                if (T.Info.DummyID == ID2)
                                {
                                    world.Players.Send(PacketWriter.MakeRemoveEntity(ID2));
                                    world.Players.Send(PacketWriter.MakeAddEntity(ID2, T.Name, player.Position));
                                    T.Info.DummyPos = player.Position;
                                    player.Message("&8Dummy was teleported to you");
                                    count++;
                                }
                            }
                        }

                    break;

                case "follow":
                    string eNum = cmd.Next();

                    if(eNum != null)
                        foreach (Player T in world.Map.Dummys)
                        {
                            double Num;
                            bool isNum = double.TryParse(eNum, out Num);
                            if (isNum)
                            {
                                int eNum2 = Convert.ToInt16(eNum);
                                if (T.Info.DummyID == eNum2)
                                {
                                    if (T.Info.IsFollowing)
                                    {
                                        player.Message("This dummy is already following a player");
                                        return;
                                    }

                                    if (player.Info.IsFollowing)
                                    {
                                        player.Message("You already have a dummy following you");
                                        return;
                                    }
                                    T.Info.IsFollowing = true;
                                    player.Info.IsFollowing = true;
                                    player.Info.followingID = T.Info.DummyID.ToString();
                                    T.Info.Static = false;
                                    player.Message("&8Dummy {0}&8 is now following you", T.Info.ClassyName);
                                }
                            }
                        }

                    break;

                case "unfollow":
                    string eNumb = cmd.Next();

                    if (eNumb != null)
                        foreach (Player T in world.Map.Dummys)
                        {
                            double Num;
                            bool isNum = double.TryParse(eNumb, out Num);
                            if (isNum)
                            {
                                int eNumb2 = Convert.ToInt16(eNumb);
                                if (T.Info.DummyID == eNumb2)
                                {
                                    if (player.Info.followingID != T.Info.DummyID.ToString())
                                    {
                                        player.Message("This dummy is not following you");
                                        return;
                                    }

                                    if (player.Info.followingID == T.Info.DummyID.ToString())
                                    {
                                        T.Info.IsFollowing = false;
                                        player.Info.IsFollowing = false;
                                        T.Info.Static = true;
                                        player.Info.followingID = null;
                                        player.Message("&8The dummy stopped following you");
                                    }
                                }
                            }
                        }

                    break;

                case "undo":
                    string Uname = cmd.Next();

                    if (Uname == null)
                    {

                        if (world.Map.Dummys.Count == 0) //stops user from deleting dummys that doent exist
                        {
                            player.Message("The dummy list on this world is empty");
                            return;
                        }

                        player.World.Players.Send(PacketWriter.MakeRemoveEntity(world.Map.Dummys.Count())); //removes the dummy from the world
                        
                        player.Info.IsFollowing = false;

                        List<Player> toRemove = new List<Player>();
                        foreach (Player d in world.Map.Dummys)
                        {
                            if (d.Info.ID == world.Map.Dummys.Count())
                                toRemove.Add(d); //adds chosed ID to a removing list
                        }

                        foreach (Player r in toRemove)
                        {
                            world.Map.Dummys.Remove(r); //removes the dummy from the list of dummys
                            string Dname = r.ClassyName;
                            Dname = Color.ReplacePercentCodes(Dname);
                            player.Message("Dummy {0}&S has been removed", Dname);
                        }
                    }
                    else CdDummy.PrintUsage(player);
                    //yes

                    break;

                case "list":

                    if (world.Map.Dummys.Count() > 0)
                    {
                        player.Message("Dummys available on world {0}: ", world.ClassyName);
                        foreach (Player d in world.Map.Dummys)
                        {
                            string Dname = d.ClassyName;
                            Dname = Color.ReplacePercentCodes(Dname);
                            player.Message("Name: {0}&S ID: {1}", Dname, d.Info.ID);
                        }
                    }
                    else player.Message("There are no Dummys on world {0}", world.ClassyName);
                    break;

                default: CdDummy.PrintUsage(player); break;
            }
        }

        static readonly CommandDescriptor CdKill = new CommandDescriptor
        {
            Name = "Kill",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = false,
            Permissions = new[] { Permission.Kill },
            Help = "Kills a player.",
            NotRepeatable = true,
            Usage = "/Kill playername",
            Handler = KillHandler
        };

        internal static void KillHandler(Player player, Command cmd)
        {
            string name = cmd.Next();
            if (name == null)
            {
                player.Message("Please enter a name");
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches(player, name, false, true);
            if (target == null) return;

            if (target == player)
            {
                player.Message("You suicidal bro?");
                return;
            }

            if ((DateTime.Now - player.Info.LastUsedKill).TotalSeconds < 10)
            {
                player.Message("&CYou can only kill once every 10 seconds. Slow down.");
                return;
            }

            if (target == null)
            {
                player.Message("You need to enter a player name to Kill");
                return;
            }
            else
            {

                if (player.Can(Permission.Kill, target.Info.Rank))
                {
                    target.TeleportTo(player.World.Map.Spawn);
                    Server.Players.CanSee(target).Message("{0}&C was &4Killed&C by {1}", target.ClassyName, player.ClassyName);
                    player.Info.LastUsedKill = DateTime.Now;
                    return;
                }
                else
                {
                    player.Message("You can only Kill players ranked {0}&S or lower",
                                    player.Info.Rank.GetLimit(Permission.Kill).ClassyName);
                    player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
                }
            }
        }
        
        static readonly CommandDescriptor CdPossess = new CommandDescriptor
        {
            Name = "Possess",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = false,
            Permissions = new[] { Permission.Possess },
            Help = "Possess a player.",
            Usage = "/possess playername",
            Handler = PossessHandler
        };

        internal static void PossessHandler(Player player, Command cmd)
        {
            string name = cmd.Next();
            if (name == null)
            {
                CdPossess.PrintUsage(player);
                return;
            }

            if (!Player.IsValidName(name))
                return;

            else
            {

                Player target = Server.FindPlayerOrPrintMatches(player, name, true, true);

                if (target == null)
                {
                    return;
                }

                /*if (target == player)
                {
                    player.Message("You can't possess yourself.");
                    return;
                }*/

                if (player.Can(Permission.Possess, target.Info.Rank))
                {
                    Position P = target.Position;
                    player.TeleportTo(P);
                    Server.TargetName = target.Name;
                    Possess(player, cmd, target.Name);
                    player.Message("Now possessing " + target.Name);
                    return;
                }


                else
                {
                    player.Message("You can only Possess players ranked {0}&S or lower",
                                    player.Info.Rank.GetLimit(Permission.Possess).ClassyName);
                    player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
                }
            }
        }

        static void Possess(Player player, Command cmd, string toPossess)
        {
            string targetName = Server.ToPossess;
            Player target = Server.FindPlayerOrPrintMatches(player, targetName, false, true);
            if (targetName == null)
            {
                PlayerInfo lastSpec = target.LastSpectatedPlayer;
                if (lastSpec != null)
                {
                    Player spec = player.SpectatedPlayer;
                    if (spec != null)
                        player.Message("Now possessing {0}", target.ClassyName);
                }
                else
                {
                    CdPossess.PrintUsage(player);
                }
                return;
            }

            if (target == null) return;

           /*if (target == player)
            {
                player.Message("You cannot Possess yourself.");
                return;
            }*/
        }


        static readonly CommandDescriptor CdUnPossess = new CommandDescriptor
        {
            Name = "Unpossess",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = false,
            Permissions = new[] { Permission.Possess },
            Help = "Unpossess a player.",
            Usage = "/unpossess playername",
            Handler = UnPossessHandler
        };

        internal static void UnPossessHandler(Player player, Command cmd)
        {
            string name = cmd.Next();
            if (name == null)
            {
                player.Message("Please enter a name");
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches(player, name, false, true);

            if (!Player.IsValidName(name))
                return;

            if (target == null) return;

            if (target == player)
            {
                player.Message("Nope.avi");
                return;
            }

            if (player.Can(Permission.Possess, target.Info.Rank))
            {

                UnPossess(target, new Command("/unfollow " + player.Name));
                player.Message("Stopped possessing " + target.Name);
                return;
            }
            else
            {
                player.Message("Unpossess failed. Try to /hide then /unhide",
                                player.Info.Rank.GetLimit(Permission.Possess).ClassyName);
                player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
            }
        }

        static void UnPossess(Player player, Command cmd)
        {
            if (!player.StopSpectating())
            {
                player.Message("You are not currently spectating anyone.");
            }
        }

        static readonly CommandDescriptor CdSlap = new CommandDescriptor
        {
            Name = "Slap",
            IsConsoleSafe = true,
            NotRepeatable = true,
            Aliases = new[] { "sky" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Slap },
            Help = "Slaps a player to the sky.",
            Handler = Slap
        };

        static void Slap(Player player, Command cmd)
        {
            string name = cmd.Next();

            if (name == null)
            {
                player.Message("Please enter a name");
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches(player, name, false, true);

            if (target == null) return;

            if (target == player)
            {
                player.Message("&sYou can't slap yourself.... What's wrong with you???");
                return;
            }

            if ((DateTime.Now - player.Info.LastUsedSlap).TotalSeconds < 10)
            {
                player.Message("&CYou can only use /Slap once every 10 seconds. Slow down.");
                return;
            }

            if (player.Can(Permission.Slap, target.Info.Rank))
            {
                Position slap = new Position(target.Position.X, target.Position.Y, (target.World.Map.Bounds.ZMax) * 32);
                Server.Players.CanSee(target).Message("{0} &swas slapped sky high by {1}", target.ClassyName, player.ClassyName);
                target.TeleportTo(slap);
                player.Info.LastUsedSlap = DateTime.Now;
                return;
            }

            else
            {
                player.Message("&sYou can only Slap players ranked {0}&S or lower",
                               player.Info.Rank.GetLimit(Permission.Slap).ClassyName);
                player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
            }
        }

        static readonly CommandDescriptor CdTPZone = new CommandDescriptor
        {
            Name = "Tpzone",
            IsConsoleSafe = false,
            Aliases = new[] { "tpz", "zonetp" },
            Category = CommandCategory.World | CommandCategory.Zone,
            Permissions = new[] { Permission.Teleport },
            Help = "Teleports you to the centre of a Zone listed in /Zones.",
            Usage = "/tpzone ZoneName",
            Handler = TPZone
        };

        static void TPZone(Player player, Command cmd)
        {
            string zoneName = cmd.Next();
            if (zoneName == null)
            {
                player.Message("No zone name specified. See &W/Help tpzone");
                return;
            }

            else
            {
                Zone zone = player.World.Map.Zones.Find(zoneName);
                if (zone == null)
                {
                    player.MessageNoZone(zoneName);
                    return;
                }
                Position zPos = new Position((((zone.Bounds.XMin + zone.Bounds.XMax) / 2) * 32),
                    (((zone.Bounds.YMin + zone.Bounds.YMax) / 2) * 32),
                    (((zone.Bounds.ZMin + zone.Bounds.ZMax) / 2) + 2) * 32);
                player.TeleportTo((zPos));
                player.Message("&WTeleporting you to zone " + zone.ClassyName);
            }
        }


        static readonly CommandDescriptor CdTempBan = new CommandDescriptor
        {
            Name = "Tempban",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Aliases = new[] { "tban" },
            Permissions = new[] { Permission.TempBan },
            Help = "Bans a player for a selected amount of time. Example: 10s | 10 m | 10h ",
            Usage = "/Tempban Player Seconds",
            Handler = Tempban
        };

        static void Tempban(Player player, Command cmd)
        {
            string targetName = cmd.Next();
            string timeString = cmd.Next();
            TimeSpan duration;

            try
            {
                // validate command parameters
                if (targetName == null || !Player.IsValidName(targetName) ||
                    timeString == null || !timeString.TryParseMiniTimespan(out duration) ||
                    duration <= TimeSpan.Zero)
                {
                    player.Message("Invalid input");
                    return;
                }
            }
            catch (OverflowException)
            {
                player.Message("TempBan: Given duration is too long.");
                return;
            }

            // find the target
            Player target = Server.FindPlayerOrPrintMatches(player, targetName, false, true);

            if (target == null)
            {
                player.MessageNoPlayer(targetName);
                return;
            };

            if (target == player)
            {
                player.Message("Trying to T-Ban yourself? Fail!");
                return;
            }

            // check permissions
            if (!player.Can(Permission.BanIP, target.Info.Rank))
            {
                player.Message("You can only Temp-Ban players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit(Permission.BanIP).ClassyName);
                player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
                return;
            }

            // do the banning
            if (target.Info.Tempban(player.Name, duration))
            {
                PlayerInfo targets = PlayerDB.FindPlayerInfoOrPrintMatches(player, targetName);
                if (targets == null) return;
                string reason = cmd.NextAll();
                try
                {
                    Player targetPlayer = targets.PlayerObject;
                    targets.Ban(player, "You were Banned for " + timeString, false, true);
                    Server.TempBans.Add(targetPlayer);
                }
                catch (PlayerOpException ex)
                {
                    player.Message(ex.MessageColored);
                }
                Scheduler.NewTask(t => target.Info.Unban(player, "Tempban Expired", true, true)).RunOnce(duration);
                Server.Message(target,
                                "&SPlayer {0}&S was Banned by {1}&S for {2}",
                                target.ClassyName, player.ClassyName, duration.ToMiniString());
                if (reason.Length > 0) Server.Message("&Wreason: {0}", reason);
                Logger.Log(LogType.UserActivity, "Player {0} was Banned by {1} for {2}",
                            target.Name, player.Name, duration.ToMiniString());
            }

            else
            {
                player.Message("Player {0}&S is already Banned by {1}&S for {2:0} more.",
                                target.ClassyName,
                                target.Info.MutedBy,
                                target.Info.MutedUntil.Subtract(DateTime.UtcNow).ToMiniString());
            }
        }

        static readonly CommandDescriptor CdBasscannon = new CommandDescriptor
        {
            Name = "Basscannon",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Aliases = new[] { "bc" },
            IsHidden = false,
            Permissions = new[] { Permission.Basscannon },
            Usage = "Let the Basscannon 'Kick' it!",
            Help = "A classy way to kick players from the server",
            Handler = Basscannon
        };

        internal static void Basscannon(Player player, Command cmd)
        {
            string name = cmd.Next();
            string reason = cmd.NextAll();

            if (name == null)
            {
                player.Message("Please enter a player name to use the basscannon on.");
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches(player, name, false, true);

            if (target == null)
            {
                return;
            }

            if (ConfigKey.RequireKickReason.Enabled() && String.IsNullOrEmpty(reason))
            {
                player.Message("&WPlease specify a reason: &W/Basscannon PlayerName Reason");
                // freeze the target player to prevent further damage
                return;
            }

            if (player.Can(Permission.Kick, target.Info.Rank))
            {
                target.Info.IsHidden = false;

                try
                {
                    Player targetPlayer = target;
                    target.BassKick(player, reason, LeaveReason.Kick, true, true, true);
                    if (BassText.Count < 1)
                    {
                        BassText.Add("Flux Pavillion does not approve of your behavior");
                        BassText.Add("Let the Basscannon KICK IT!");
                        BassText.Add("WUB WUB WUB WUB WUB WUB!");
                        BassText.Add("Basscannon, Basscannon, Basscannon, Basscannon!");
                        BassText.Add("Pow pow POW!!!");
                    }
                    string line = BassText[new Random().Next(0, BassText.Count)].Trim();
                    if (line.Length == 0) return;
                    Server.Message("&9{0}", line);
                }
                catch (PlayerOpException ex)
                {
                    player.Message(ex.MessageColored);
                    if (ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired)
                        return;
                }
            }
            else
            {
                player.Message("You can only use /Basscannon on players ranked {0}&S or lower",
                                player.Info.Rank.GetLimit(Permission.Kick).ClassyName);
                player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
            }
        }

        static readonly CommandDescriptor CdWarn = new CommandDescriptor
        {
            Name = "Warn",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            NotRepeatable = true,
            Permissions = new[] { Permission.Warn },
            Help = "Warns a player and puts a black star next to their name for 20 minutes. During the 20 minutes, if they are warned again, they will get kicked.",
            Usage = "/Warn playername",
            Handler = Warn
        };

        internal static void Warn(Player player, Command cmd)
        {
            string name = cmd.Next();

            if (name == null)
            {
                player.Message("No player specified.");
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches(player, name, false, true);

            if (target == null)
            {
                player.MessageNoPlayer(name);
                return;
            }

            if (player.Can(Permission.Warn, target.Info.Rank))
            {
                target.Info.IsHidden = false;
                if (target.Info.Warn(player.Name))
                {
                    Server.Message("{0}&S has been Warned by {1}",
                                      target.ClassyName, player.ClassyName);
                    Scheduler.NewTask(t => target.Info.UnWarn()).RunOnce(TimeSpan.FromMinutes(15));
                }
                else
                {
                    try
                    {
                        Player targetPlayer = target;
                        target.Kick(player, "Auto Kick (2 warnings or more)", LeaveReason.Kick, true, true, true);
                    }
                    catch (PlayerOpException ex)
                    {
                        player.Message(ex.MessageColored);
                        if (ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired)
                            return;
                    }
                }
            }
            else
            {
                player.Message("You can only warn players ranked {0}&S or lower",
                                player.Info.Rank.GetLimit(Permission.Warn).ClassyName);
                player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
            }
        }

        static readonly CommandDescriptor CdUnWarn = new CommandDescriptor
        {
            Name = "Unwarn",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Warn },
            Usage = "/Unwarn PlayerName",
            Help = "Unwarns a player",
            Handler = UnWarn
        };

        internal static void UnWarn(Player player, Command cmd)
        {
            string name = cmd.Next();
            if (name == null)
            {
                player.Message("No player specified.");
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches(player, name, false, true);

            if (target == null)
            {
                player.MessageNoPlayer(name);
                return;
            }

            if (player.Can(Permission.Warn, target.Info.Rank))
            {
                if (target.Info.UnWarn())
                {
                    Server.Message("{0}&S had their warning removed by {1}.", target.ClassyName, player.ClassyName);
                }
                else
                {
                    player.Message("{0}&S does not have a warning.", target.ClassyName);
                }
            }
            else
            {
                player.Message("You can only unwarn players ranked {0}&S or lower",
                                player.Info.Rank.GetLimit(Permission.Warn).ClassyName);
                player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
            }
        }
        

        static readonly CommandDescriptor CdDisconnect = new CommandDescriptor
        {
            Name = "Disconnect",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Aliases = new[] { "gtfo" },
            IsHidden = false,
            Permissions = new[] { Permission.Gtfo },
            Usage = "/disconnect playername",
            Help = "Get rid of those annoying people without saving to PlayerDB",
            Handler = dc
        };

        internal static void dc(Player player, Command cmd)
        {
            string name = cmd.Next();
            if (name == null)
            {
                player.Message("Please enter a name");
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches(player, name, false, true);
            if (target == null) return;

            if (player.Can(Permission.Gtfo, target.Info.Rank))
            {
                try
                {
                    Player targetPlayer = target;
                    target.Kick(player, "Manually disconnected by "+player.Name, LeaveReason.Kick, false, true, false);
                    Server.Players.Message("{0} &Swas manually disconnected by {1}", target.ClassyName, player.ClassyName);
                }
                catch (PlayerOpException ex)
                {
                    player.Message(ex.MessageColored);
                    if (ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired)
                        return;
                }
            }
            else
            {
                player.Message("You can only Disconnect players ranked {0}&S or lower",
                                player.Info.Rank.GetLimit(Permission.Gtfo).ClassyName);
                player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
            }
        }
        #endregion

        #region Custom Functions

        

        
        static void TPHandler2(Player player, Command cmd)
        {
            string name = cmd.Next();
            if (name == null)
            {

                return;
            }

            if (cmd.Next() != null)
            {
                cmd.Rewind();
                int x, y, z;
                if (cmd.NextInt(out x) && cmd.NextInt(out y) && cmd.NextInt(out z))
                {

                    if (x <= -1024 || x >= 1024 || y <= -1024 || y >= 1024 || z <= -1024 || z >= 1024)
                    {
                        player.Message("Coordinates are outside the valid range!");

                    }
                    else
                    {
                        player.TeleportTo(new Position
                        {
                            X = (short)(x * 32 + 16),
                            Y = (short)(y * 32 + 16),
                            Z = (short)(z * 32 + 16),
                            R = player.Position.R,
                            L = player.Position.L
                        });
                    }
                }
                else
                {
                    return;
                }

            }
            else
            {
                Player[] matches = Server.FindPlayers(player, name, true);
                if (matches.Length == 1)
                {
                    Player target = matches[0];
                    World targetWorld = target.World;
                    if (targetWorld == null) PlayerOpException.ThrowNoWorld(target);

                    if (targetWorld == player.World)
                    {
                        player.TeleportTo(target.Position);

                    }
                    else
                    {
                        switch (targetWorld.AccessSecurity.CheckDetailed(player.Info))
                        {
                            case SecurityCheckResult.Allowed:
                            case SecurityCheckResult.WhiteListed:
                                if (targetWorld.IsFull)
                                {
                                    player.Message("Cannot teleport to {0}&S because world {1}&S is full.",
                                                    target.ClassyName,
                                                    targetWorld.ClassyName);
                                    return;
                                }
                                player.StopSpectating();
                                player.JoinWorld(targetWorld, WorldChangeReason.Tp, target.Position);
                                break;
                            case SecurityCheckResult.BlackListed:
                                player.Message("Cannot teleport to {0}&S because you are blacklisted on world {1}",
                                                target.ClassyName,
                                                targetWorld.ClassyName);
                                break;
                            case SecurityCheckResult.RankTooLow:
                                player.Message("Cannot teleport to {0}&S because world {1}&S requires {2}+&S to join.",
                                                target.ClassyName,
                                                targetWorld.ClassyName,
                                                targetWorld.AccessSecurity.MinRank.ClassyName);
                                break;
                            // TODO: case PermissionType.RankTooHigh:
                        }
                    }

                }
                else if (matches.Length > 1)
                {
                    player.MessageManyMatches("player", matches);

                }
                else
                {
                    // Try to guess if player typed "/TP" instead of "/Join"
                    World[] worlds = WorldManager.FindWorlds(player, name);

                    if (worlds.Length == 1)
                    {
                        player.LastUsedWorldName = worlds[0].Name;
                        player.StopSpectating();
                        player.ParseMessage("/Join " + worlds[0].Name, false);
                    }
                    else
                    {
                        player.MessageNoPlayer(name);
                    }
                }
            }
        }
        #endregion

        #region Ban / Unban

        static readonly CommandDescriptor CdBan = new CommandDescriptor {
            Name = "Ban",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban },
            Usage = "/Ban PlayerName [Reason]",
            Help = "Bans a specified player by name. Note: Does NOT ban IP. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = BanHandler
        };

        static void BanHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( targetName == null ) {
                CdBan.PrintUsage( player );
                return;
            }
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetName );
            if( target == null ) return;
            string reason = cmd.NextAll();
            try {
                Player targetPlayer = target.PlayerObject;
                target.Ban( player, reason, true, true );
                WarnIfOtherPlayersOnIP( player, target, targetPlayer );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
                if( ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired ) {
                    FreezeIfAllowed( player, target );
                }
            }
        }



        static readonly CommandDescriptor CdBanIP = new CommandDescriptor {
            Name = "BanIP",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP },
            Usage = "/BanIP PlayerName|IPAddress [Reason]",
            Help = "Bans the player's name and IP. If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            Handler = BanIPHandler
        };

        static void BanIPHandler( Player player, Command cmd ) {
            string targetNameOrIP = cmd.Next();
            if( targetNameOrIP == null ) {
                CdBanIP.PrintUsage( player );
                return;
            }
            string reason = cmd.NextAll();

            IPAddress targetAddress;
            if( IPAddress.TryParse( targetNameOrIP, out targetAddress ) ) {
                try {
                    targetAddress.BanIP( player, reason, true, true );
                } catch( PlayerOpException ex ) {
                    player.Message( ex.MessageColored );
                }
            } else {
                PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetNameOrIP );
                if( target == null ) return;
                try {
                    if( target.LastIP.Equals( IPAddress.Any ) || target.LastIP.Equals( IPAddress.None ) ) {
                        target.Ban( player, reason, true, true );
                    } else {
                        target.BanIP( player, reason, true, true );
                    }
                } catch( PlayerOpException ex ) {
                    player.Message( ex.MessageColored );
                    if( ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired ) {
                        FreezeIfAllowed( player, target );
                    }
                }
            }
        }



        static readonly CommandDescriptor CdBanAll = new CommandDescriptor {
            Name = "BanAll",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            Usage = "/BanAll PlayerName|IPAddress [Reason]",
            Help = "Bans the player's name, IP, and all other names associated with the IP. " +
                   "If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            Handler = BanAllHandler
        };

        static void BanAllHandler(Player player, Command cmd)
        {
            string targetNameOrIP = cmd.Next();
            if (targetNameOrIP == null)
            {
                CdBanAll.PrintUsage(player);
                return;
            }
            string reason = cmd.NextAll();

            IPAddress targetAddress;
            if (Server.IsIP(targetNameOrIP) && IPAddress.TryParse(targetNameOrIP, out targetAddress))
            {
                try
                {
                    targetAddress.BanAll(player, reason, true, true);
                }
                catch (PlayerOpException ex)
                {
                    player.Message(ex.MessageColored);
                }
            }
            else
            {
                PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player, targetNameOrIP);
                if (target == null) return;
                try
                {
                    if (target.LastIP.Equals(IPAddress.Any) || target.LastIP.Equals(IPAddress.None))
                    {
                        target.Ban(player, reason, true, true);
                    }
                    else
                    {
                        target.BanAll(player, reason, true, true);
                    }
                }
                catch (PlayerOpException ex)
                {
                    player.Message(ex.MessageColored);
                    if (ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired)
                    {
                        FreezeIfAllowed(player, target);
                    }
                }
            }
        }



        static readonly CommandDescriptor CdUnban = new CommandDescriptor {
            Name = "Unban",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban },
            Usage = "/Unban PlayerName [Reason]",
            Help = "Removes ban for a specified player. Does NOT remove associated IP bans. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = UnbanHandler
        };

        static void UnbanHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( targetName == null ) {
                CdUnban.PrintUsage( player );
                return;
            }
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetName );
            if( target == null ) return;
            string reason = cmd.NextAll();
            try {
                target.Unban( player, reason, true, true );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }



        static readonly CommandDescriptor CdUnbanIP = new CommandDescriptor {
            Name = "UnbanIP",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP },
            Usage = "/UnbanIP PlayerName|IPaddress [Reason]",
            Help = "Removes ban for a specified player's name and last known IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = UnbanIPHandler
        };

        static void UnbanIPHandler(Player player, Command cmd)
        {
            string targetNameOrIP = cmd.Next();
            if (targetNameOrIP == null)
            {
                CdUnbanIP.PrintUsage(player);
                return;
            }
            string reason = cmd.NextAll();

            try
            {
                IPAddress targetAddress;
                if (Server.IsIP(targetNameOrIP) && IPAddress.TryParse(targetNameOrIP, out targetAddress))
                {
                    targetAddress.UnbanIP(player, reason, true, true);
                }
                else
                {
                    PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player, targetNameOrIP);
                    if (target == null) return;
                    if (target.LastIP.Equals(IPAddress.Any) || target.LastIP.Equals(IPAddress.None))
                    {
                        target.Unban(player, reason, true, true);
                    }
                    else
                    {
                        target.UnbanIP(player, reason, true, true);
                    }
                }
            }
            catch (PlayerOpException ex)
            {
                player.Message(ex.MessageColored);
            }
        }




        static readonly CommandDescriptor CdUnbanAll = new CommandDescriptor {
            Name = "UnbanAll",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            Usage = "/UnbanAll PlayerName|IPaddress [Reason]",
            Help = "Removes ban for a specified player's name, last known IP, and all other names associated with the IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = UnbanAllHandler
        };

        static void UnbanAllHandler(Player player, Command cmd)
        {
            string targetNameOrIP = cmd.Next();
            if (targetNameOrIP == null)
            {
                CdUnbanAll.PrintUsage(player);
                return;
            }
            string reason = cmd.NextAll();

            try
            {
                IPAddress targetAddress;
                if (Server.IsIP(targetNameOrIP) && IPAddress.TryParse(targetNameOrIP, out targetAddress))
                {
                    targetAddress.UnbanAll(player, reason, true, true);
                }
                else
                {
                    PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player, targetNameOrIP);
                    if (target == null) return;
                    if (target.LastIP.Equals(IPAddress.Any) || target.LastIP.Equals(IPAddress.None))
                    {
                        target.Unban(player, reason, true, true);
                    }
                    else
                    {
                        target.UnbanAll(player, reason, true, true);
                    }
                }
            }
            catch (PlayerOpException ex)
            {
                player.Message(ex.MessageColored);
            }
        }


        static readonly CommandDescriptor CdBanEx = new CommandDescriptor
        {
            Name = "BanEx",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP },
            Usage = "/BanEx +PlayerName&S or &H/BanEx -PlayerName",
            Help = "Adds or removes an IP-ban exemption for an account. " +
                   "Exempt accounts can log in from any IP, including banned ones.",
            Handler = BanExHandler
        };

        static void BanExHandler(Player player, Command cmd)
        {
            string playerName = cmd.Next();
            if (playerName == null || playerName.Length < 2 || (playerName[0] != '-' && playerName[0] != '+'))
            {
                CdBanEx.PrintUsage(player);
                return;
            }
            bool addExemption = (playerName[0] == '+');
            string targetName = playerName.Substring(1);
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player, targetName);
            if (target == null) return;

            switch (target.BanStatus)
            {
                case BanStatus.Banned:
                    if (addExemption)
                    {
                        player.Message("Player {0}&S is currently banned. Unban before adding an exemption.",
                                        target.ClassyName);
                    }
                    else
                    {
                        player.Message("Player {0}&S is already banned. There is no exemption to remove.",
                                        target.ClassyName);
                    }
                    break;
                case BanStatus.IPBanExempt:
                    if (addExemption)
                    {
                        player.Message("IP-Ban exemption already exists for player {0}", target.ClassyName);
                    }
                    else
                    {
                        player.Message("IP-Ban exemption removed for player {0}",
                                        target.ClassyName);
                        target.BanStatus = BanStatus.NotBanned;
                    }
                    break;
                case BanStatus.NotBanned:
                    if (addExemption)
                    {
                        player.Message("IP-Ban exemption added for player {0}",
                                        target.ClassyName);
                        target.BanStatus = BanStatus.IPBanExempt;
                    }
                    else
                    {
                        player.Message("No IP-Ban exemption exists for player {0}",
                                        target.ClassyName);
                    }
                    break;
            }
        }

        #endregion


        #region Kick

        static readonly CommandDescriptor CdKick = new CommandDescriptor {
            Name = "Kick",
            Aliases = new[] { "k" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Kick },
            Usage = "/Kick PlayerName [Reason]",
            Help = "Kicks the specified player from the server. " +
                   "Optional kick reason/message is shown to the kicked player and logged.",
            Handler = KickHandler
        };

        static void KickHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                player.Message( "Usage: &H/Kick PlayerName [Message]" );
                return;
            }

            // find the target
            Player target = Server.FindPlayerOrPrintMatches( player, name, false, true );
            if( target == null ) return;

            string reason = cmd.NextAll();
            DateTime previousKickDate = target.Info.LastKickDate;
            string previousKickedBy = target.Info.LastKickByClassy;
            string previousKickReason = target.Info.LastKickReason;

            // do the kick
            try {
                Player targetPlayer = target;
                target.Kick( player, reason, LeaveReason.Kick, true, true, true );
                WarnIfOtherPlayersOnIP( player, target.Info, targetPlayer );

            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
                if( ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired ) {
                    FreezeIfAllowed( player, target.Info );
                }
                return;
            }

            // warn player if target has been kicked before
            if( target.Info.TimesKicked > 1 ) {
                player.Message( "Warning: {0}&S has been kicked {1} times before.",
                                target.ClassyName, target.Info.TimesKicked - 1 );
                if( previousKickDate != DateTime.MinValue ) {
                    player.Message( "Most recent kick was {0} ago, by {1}",
                                    DateTime.UtcNow.Subtract( previousKickDate ).ToMiniString(),
                                    previousKickedBy );
                }
                if( !String.IsNullOrEmpty( previousKickReason ) ) {
                    player.Message( "Most recent kick reason was: {0}",
                                    previousKickReason );
                }
            }
        }

        #endregion


        #region Changing Rank (Promotion / Demotion)

        static readonly CommandDescriptor CdRank = new CommandDescriptor {
            Name = "Rank",
            Aliases = new[] { "user", "promote", "demote" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Promote, Permission.Demote },
            AnyPermission = true,
            IsConsoleSafe = true,
            Usage = "/Rank PlayerName RankName [Reason]",
            Help = "Changes the rank of a player to a specified rank. " +
                   "Any text specified after the RankName will be saved as a memo.",
            Handler = RankHandler
        };

        public static void RankHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            string newRankName = cmd.Next();

            // Check arguments
            if( name == null || newRankName == null ) {
                CdRank.PrintUsage( player );
                player.Message( "See &H/Ranks&S for list of ranks." );
                return;
            }

            // Parse rank name
            Rank newRank = RankManager.FindRank( newRankName );
            if( newRank == null ) {
                player.MessageNoRank( newRankName );
                return;
            }

            // Parse player name
            if( name == "-" ) {
                if( player.LastUsedPlayerName != null ) {
                    name = player.LastUsedPlayerName;
                } else {
                    player.Message( "Cannot repeat player name: you haven't used any names yet." );
                    return;
                }
            }
            PlayerInfo targetInfo = PlayerDB.FindPlayerInfoExact( name );
            player.LastUsedPlayerName = name;

            if( targetInfo == null ) {
                if( !player.Can( Permission.EditPlayerDB ) ) {
                    player.MessageNoPlayer( name );
                    return;
                }
                if( !Player.IsValidName( name ) ) {
                    player.MessageInvalidPlayerName( name );
                    CdRank.PrintUsage( player );
                    return;
                }
                if( cmd.IsConfirmed ) {
                    if( newRank > RankManager.DefaultRank ) {
                        targetInfo = PlayerDB.AddFakeEntry( name, RankChangeType.Promoted );
                    } else {
                        targetInfo = PlayerDB.AddFakeEntry( name, RankChangeType.Demoted );
                    }
                } else {
                    player.Confirm( cmd,
                                    "Warning: Player \"{0}\" is not in the database (possible typo). Type the full name or",
                                    name );
                    return;
                }
            }

            try {
                player.LastUsedPlayerName = targetInfo.Name;
                targetInfo.ChangeRank( player, newRank, cmd.NextAll(), true, true, false );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }


        #endregion


        #region Hide

        static readonly CommandDescriptor CdHide = new CommandDescriptor {
            Name = "Hide",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Hide },
            Usage = "/Hide [silent]",
            Help = "Enables invisible mode. It looks to other players like you left the server, " +
                   "but you can still do anything - chat, build, delete, type commands - as usual. " +
                   "Great way to spy on griefers and scare newbies. " +
                   "Call &H/Unhide&S to reveal yourself.",
            Handler = HideHandler
        };

        static void HideHandler( Player player, Command cmd ) {
            if( player.Info.IsHidden ) {
                player.Message( "You are already hidden." );
                return;
            }

            string silentString = cmd.Next();
            bool silent = false;
            if( silentString != null ) {
                silent = silentString.Equals( "silent", StringComparison.OrdinalIgnoreCase );
            }

            player.Info.IsHidden = true;
            player.Message( "&8You are now hidden." );

            // to make it look like player just logged out in /Info
            player.Info.LastSeen = DateTime.UtcNow;

            if( !silent ) {
                if( ConfigKey.ShowConnectionMessages.Enabled() ) {
                    Server.Players.CantSee( player ).Message( "&SPlayer {0}&S left the server.", player.ClassyName );
                }
                if( ConfigKey.IRCBotAnnounceServerJoins.Enabled() ) {
                    IRC.PlayerDisconnectedHandler( null, new PlayerDisconnectedEventArgs( player, LeaveReason.ClientQuit, true ) );
                }
            }

            // for aware players: notify
            Server.Players.CanSee( player ).Message( "&SPlayer {0}&S is now hidden.", player.ClassyName );

            Player.RaisePlayerHideChangedEvent( player );
        }



        static readonly CommandDescriptor CdUnhide = new CommandDescriptor {
            Name = "Unhide",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Hide },
            Usage = "/Unhide [silent]",
            Help = "Disables the &H/Hide&S invisible mode. " +
                   "It looks to other players like you just joined the server.",
            Handler = UnhideHandler
        };

        static void UnhideHandler( Player player, Command cmd ) {
            if( player.World == null ) PlayerOpException.ThrowNoWorld( player );

            if( !player.Info.IsHidden ) {
                player.Message( "You are not currently hidden." );
                return;
            }

            bool silent = cmd.HasNext;

            // for aware players: notify
            Server.Players.CanSee( player ).Message( "&SPlayer {0}&S is no longer hidden.",
                                                     player.ClassyName );
            player.Message( "&8You are no longer hidden." );
            player.Info.IsHidden = false;
            if( !silent ) {
                if( ConfigKey.ShowConnectionMessages.Enabled() ) {
// ReSharper disable AssignNullToNotNullAttribute
                    string msg = Server.MakePlayerConnectedMessage( player, false, player.World );
// ReSharper restore AssignNullToNotNullAttribute
                    Server.Players.CantSee( player ).Message( msg );
                }
                if( ConfigKey.IRCBotAnnounceServerJoins.Enabled() ) {
                    IRC.PlayerReadyHandler( null, new PlayerConnectedEventArgs( player, player.World ) );
                }
            }

            Player.RaisePlayerHideChangedEvent( player );
        }

        #endregion


        #region Set Spawn

        static readonly CommandDescriptor CdSetSpawn = new CommandDescriptor {
            Name = "SetSpawn",
            Category = CommandCategory.Moderation | CommandCategory.World,
            Permissions = new[] { Permission.SetSpawn },
            Help = "Assigns your current location to be the spawn point of the map/world. " +
                   "If an optional PlayerName param is given, the spawn point of only that player is changed instead.",
            Usage = "/SetSpawn [PlayerName]",
            Handler = SetSpawnHandler
        };

        static void SetSpawnHandler( Player player, Command cmd ) {
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );


            string playerName = cmd.Next();
            if( playerName == null ) {
                Map map = player.WorldMap;
                map.Spawn = player.Position;
                player.TeleportTo( map.Spawn );
                player.Send( PacketWriter.MakeAddEntity( 255, player.ListName, player.Position ) );
                player.Message( "New spawn point saved." );
                Logger.Log( LogType.UserActivity,
                            "{0} changed the spawned point.",
                            player.Name );

            } else if( player.Can( Permission.Bring ) ) {
                Player[] infos = playerWorld.FindPlayers( player, playerName );
                if( infos.Length == 1 ) {
                    Player target = infos[0];
                    player.LastUsedPlayerName = target.Name;
                    if( player.Can( Permission.Bring, target.Info.Rank ) ) {
                        target.Send( PacketWriter.MakeAddEntity( 255, target.ListName, player.Position ) );
                    } else {
                        player.Message( "You may only set spawn of players ranked {0}&S or lower.",
                                        player.Info.Rank.GetLimit( Permission.Bring ).ClassyName );
                        player.Message( "{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName );
                    }

                } else if( infos.Length > 0 ) {
                    player.MessageManyMatches( "player", infos );

                } else {
                    infos = Server.FindPlayers( player, playerName, true );
                    if( infos.Length > 0 ) {
                        player.Message( "You may only set spawn of players on the same world as you." );
                    } else {
                        player.MessageNoPlayer( playerName );
                    }
                }
            } else {
                player.MessageNoAccess( CdSetSpawn );
            }
        }

        #endregion


        #region Freeze

        static readonly CommandDescriptor CdFreeze = new CommandDescriptor {
            Name = "Freeze",
            Aliases = new[] { "f" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Freeze },
            Usage = "/Freeze PlayerName",
            Help = "Freezes the specified player in place. " +
                   "This is usually effective, but not hacking-proof. " +
                   "To release the player, use &H/unfreeze PlayerName",
            Handler = FreezeHandler
        };

        static void FreezeHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdFreeze.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, name, false, true );
            if( target == null ) return;

            try {
                target.Info.Freeze( player, true, true );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }


        static readonly CommandDescriptor CdUnfreeze = new CommandDescriptor {
            Name = "Unfreeze",
            Aliases = new[] { "uf" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Freeze },
            Usage = "/Unfreeze PlayerName",
            Help = "Releases the player from a frozen state. See &H/Help freeze&S for more information.",
            Handler = UnfreezeHandler
        };

        static void UnfreezeHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdFreeze.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, name, false, true );
            if( target == null ) return;

            try {
                target.Info.Unfreeze( player, true, true );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }

        #endregion


        #region TP

        static readonly CommandDescriptor CdTP = new CommandDescriptor {
            Name = "TP",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Teleport },
            Usage = "/TP PlayerName&S or &H/TP X Y Z",
            Help = "Teleports you to a specified player's location. " +
                   "If coordinates are given, teleports to that location.",
            Handler = TPHandler
        };

        static void TPHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdTP.PrintUsage( player );
                return;
            }

            if( cmd.Next() != null ) {
                cmd.Rewind();
                int x, y, z;
                if( cmd.NextInt( out x ) && cmd.NextInt( out y ) && cmd.NextInt( out z ) ) {

                    if( x <= -1024 || x >= 1024 || y <= -1024 || y >= 1024 || z <= -1024 || z >= 1024 ) {
                        player.Message( "Coordinates are outside the valid range!" );

                    } else {
                        player.TeleportTo( new Position {
                            X = (short)(x * 32 + 16),
                            Y = (short)(y * 32 + 16),
                            Z = (short)(z * 32 + 16),
                            R = player.Position.R,
                            L = player.Position.L
                        } );
                    }
                } else {
                    CdTP.PrintUsage( player );
                }

            } else {
                Player[] matches = Server.FindPlayers( player, name, true );
                if( matches.Length == 1 ) {
                    Player target = matches[0];
                    World targetWorld = target.World;
                    if( targetWorld == null ) PlayerOpException.ThrowNoWorld( target );

                    if( targetWorld == player.World ) {
                        player.TeleportTo( target.Position );

                    } else {
                        switch( targetWorld.AccessSecurity.CheckDetailed( player.Info ) ) {
                            case SecurityCheckResult.Allowed:
                            case SecurityCheckResult.WhiteListed:
                                if( targetWorld.IsFull ) {
                                    player.Message( "Cannot teleport to {0}&S because world {1}&S is full.",
                                                    target.ClassyName,
                                                    targetWorld.ClassyName );
                                    return;
                                }
                                player.StopSpectating();
                                player.JoinWorld( targetWorld, WorldChangeReason.Tp, target.Position );
                                break;
                            case SecurityCheckResult.BlackListed:
                                player.Message( "Cannot teleport to {0}&S because you are blacklisted on world {1}",
                                                target.ClassyName,
                                                targetWorld.ClassyName );
                                break;
                            case SecurityCheckResult.RankTooLow:
                                player.Message( "Cannot teleport to {0}&S because world {1}&S requires {2}+&S to join.",
                                                target.ClassyName,
                                                targetWorld.ClassyName,
                                                targetWorld.AccessSecurity.MinRank.ClassyName );
                                break;
                            // TODO: case PermissionType.RankTooHigh:
                        }
                    }

                } else if( matches.Length > 1 ) {
                    player.MessageManyMatches( "player", matches );

                } else {
                    // Try to guess if player typed "/TP" instead of "/Join"
                    World[] worlds = WorldManager.FindWorlds( player, name );

                    if( worlds.Length == 1 ) {
                        player.LastUsedWorldName = worlds[0].Name;
                        player.StopSpectating();
                        player.ParseMessage( "/Join " + worlds[0].Name, false );
                    } else {
                        player.MessageNoPlayer( name );
                    }
                }
            }
        }

        #endregion


        #region Bring / WorldBring / BringAll

        static readonly CommandDescriptor CdBring = new CommandDescriptor {
            Name = "Bring",
            IsConsoleSafe = true,
            Aliases = new[] { "summon", "fetch" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Bring },
            Usage = "/Bring PlayerName [ToPlayer]",
            Help = "Teleports another player to your location. " +
                   "If the optional second parameter is given, teleports player to another player.",
            Handler = BringHandler
        };

        static void BringHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdBring.PrintUsage( player );
                return;
            }

            // bringing someone to another player (instead of to self)
            string toName = cmd.Next();
            Player toPlayer = player;
            if( toName != null ) {
                toPlayer = Server.FindPlayerOrPrintMatches( player, toName, false, true );
                if( toPlayer == null ) return;
            } else if( toPlayer.World == null ) {
                player.Message( "When used from console, /Bring requires both names to be given." );
                return;
            }

            World world = toPlayer.World;
            if(world==null) PlayerOpException.ThrowNoWorld( toPlayer );

            Player target = Server.FindPlayerOrPrintMatches( player, name, false, true );
            if( target == null ) return;

            if( !player.Can( Permission.Bring, target.Info.Rank ) ) {
                player.Message( "You may only bring players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit( Permission.Bring ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            if( target.World == world ) {
                // teleport within the same world
                target.TeleportTo( toPlayer.Position );
                target.Message("&8You were summoned by {0}", player.ClassyName);

            } else {
                if( world.AccessSecurity.CheckDetailed( target.Info ) == SecurityCheckResult.RankTooLow &&
                    player.CanJoin(world) && !cmd.IsConfirmed ) {
                    player.Confirm( cmd,
                                    "Player {0}&S is ranked too low to join {1}&S. Override world permissions?",
                                    target.Name,
                                    world );
                    return;
                }
                // teleport to a different world
                BringPlayerToWorld( player, target, world, true, true );
                target.Message("&8You were summoned by {0}", player.ClassyName);
            }
        }


        static readonly CommandDescriptor CdWorldBring = new CommandDescriptor {
            Name = "WBring",
            IsConsoleSafe = true,
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Bring },
            Usage = "/WBring PlayerName WorldName",
            Help = "Teleports a player to the given world's spawn.",
            Handler = WorldBringHandler
        };

        static void WorldBringHandler( Player player, Command cmd ) {
            string playerName = cmd.Next();
            string worldName = cmd.Next();
            if( playerName == null || worldName == null ) {
                CdBring.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, playerName, false, true );
            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );

            if( target == null || world == null ) return;

            if( target == player ) {
                player.Message( "&WYou cannot &H/WBring&W yourself." );
                return;
            }

            if( !player.Can( Permission.Bring, target.Info.Rank ) ) {
                player.Message( "You may only bring players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit( Permission.Bring ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            if( world == target.World ) {
                player.Message( "Player {0}&S is already in world {1}",
                                target.ClassyName, world.ClassyName );
                return;
            }

            bool overridePermission = false;
            if( world.AccessSecurity.CheckDetailed( target.Info ) == SecurityCheckResult.RankTooLow && player.CanJoin( world ) ) {
                if( cmd.IsConfirmed ) {
                    overridePermission = true;
                } else {
                    player.Confirm( cmd,
                                    "Player {0}&S is ranked too low to join {1}&S. Override world permissions?",
                                    target.ClassyName, world.ClassyName );
                    return;
                }
            }

            BringPlayerToWorld( player, target, world, overridePermission, false );
        }


        static readonly CommandDescriptor CdBringAll = new CommandDescriptor {
            Name = "BringAll",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Bring, Permission.BringAll },
            Usage = "/BringAll [@Rank [@AnotherRank]] [*|World [AnotherWorld]]",
            Help = "Teleports all players from your world to you. " +
                   "If any world names are given, only teleports players from those worlds. " +
                   "If any rank names are given, only teleports players of those ranks.",
            Handler = BringAllHandler
        };

        static void BringAllHandler( Player player, Command cmd ) {
            if( player.World == null ) PlayerOpException.ThrowNoWorld( player );

            List<World> targetWorlds = new List<World>();
            List<Rank> targetRanks = new List<Rank>();
            bool allWorlds = false;
            bool allRanks = true;

            // Parse the list of worlds and ranks
            string arg;
            while( (arg = cmd.Next()) != null ) {
                if( arg.StartsWith( "@" ) ) {
                    Rank rank = RankManager.FindRank( arg.Substring( 1 ) );
                    if( rank == null ) {
                        player.Message( "Unknown rank: {0}", arg.Substring( 1 ) );
                        return;
                    } else {
                        if( player.Can( Permission.Bring, rank ) ) {
                            targetRanks.Add( rank );
                        } else {
                            player.Message( "&WYou are not allowed to &H/Bring&W players of rank {0}",
                                            rank.ClassyName );
                        }
                        allRanks = false;
                    }
                } else if( arg == "*" ) {
                    allWorlds = true;
                } else {
                    World world = WorldManager.FindWorldOrPrintMatches( player, arg );
                    if( world == null ) return;
                    targetWorlds.Add( world );
                }
            }

            // If no worlds were specified, use player's current world
            if( !allWorlds && targetWorlds.Count == 0 ) {
                targetWorlds.Add( player.World );
            }

            // Apply all the rank and world options
            HashSet<Player> targetPlayers;
            if( allRanks && allWorlds ) {
                targetPlayers = new HashSet<Player>( Server.Players );
            } else if( allWorlds ) {
                targetPlayers = new HashSet<Player>();
                foreach( Rank rank in targetRanks ) {
                    foreach( Player rankPlayer in Server.Players.Ranked( rank ) ) {
                        targetPlayers.Add( rankPlayer );
                    }
                }
            } else if( allRanks ) {
                targetPlayers = new HashSet<Player>();
                foreach( World world in targetWorlds ) {
                    foreach( Player worldPlayer in world.Players ) {
                        targetPlayers.Add( worldPlayer );
                    }
                }
            } else {
                targetPlayers = new HashSet<Player>();
                foreach( Rank rank in targetRanks ) {
                    foreach( World world in targetWorlds ) {
                        foreach( Player rankWorldPlayer in world.Players.Ranked( rank ) ) {
                            targetPlayers.Add( rankWorldPlayer );
                        }
                    }
                }
            }

            // Remove the player him/herself
            targetPlayers.Remove( player );

            int count = 0;

            // Actually bring all the players
            foreach( Player targetPlayer in targetPlayers.Where( player.CanSee ) ) {
                if( targetPlayer.World == player.World ) {
                    // teleport within the same world
                    targetPlayer.TeleportTo( player.Position );
                    targetPlayer.Position = player.Position;
                    if( targetPlayer.Info.IsFrozen ) {
                        targetPlayer.Position = player.Position;
                    }

                } else {
                    // teleport to a different world
// ReSharper disable AssignNullToNotNullAttribute
                    BringPlayerToWorld( player, targetPlayer, player.World, false, true );
// ReSharper restore AssignNullToNotNullAttribute
                }
                count++;
            }

            // Check if there's anyone to bring
            if( count == 0 ) {
                player.Message( "No players to bring!" );
            } else {
                player.Message( "Bringing {0} players...", count );
            }
        }



        static void BringPlayerToWorld( [NotNull] Player player, [NotNull] Player target, [NotNull] World world,
                                        bool overridePermissions, bool usePlayerPosition ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( target == null ) throw new ArgumentNullException( "target" );
            if( world == null ) throw new ArgumentNullException( "world" );
            switch( world.AccessSecurity.CheckDetailed( target.Info ) ) {
                case SecurityCheckResult.Allowed:
                case SecurityCheckResult.WhiteListed:
                    if( world.IsFull ) {
                        player.Message( "Cannot bring {0}&S because world {1}&S is full.",
                                        target.ClassyName,
                                        world.ClassyName );
                        return;
                    }
                    target.StopSpectating();
                    if( usePlayerPosition ) {
                        target.JoinWorld( world, WorldChangeReason.Bring, player.Position );
                    } else {
                        target.JoinWorld( world, WorldChangeReason.Bring );
                    }
                    break;

                case SecurityCheckResult.BlackListed:
                    player.Message( "Cannot bring {0}&S because he/she is blacklisted on world {1}",
                                    target.ClassyName,
                                    world.ClassyName );
                    break;

                case SecurityCheckResult.RankTooLow:
                    if( overridePermissions ) {
                        target.StopSpectating();
                        if( usePlayerPosition ) {
                            target.JoinWorld( world, WorldChangeReason.Bring, player.Position );
                        } else {
                            target.JoinWorld( world, WorldChangeReason.Bring );
                        }
                    } else {
                        player.Message( "Cannot bring {0}&S because world {1}&S requires {2}+&S to join.",
                                        target.ClassyName,
                                        world.ClassyName,
                                        world.AccessSecurity.MinRank.ClassyName );
                    }
                    break;
                // TODO: case PermissionType.RankTooHigh:
            }
        }

        #endregion


        #region Patrol & SpecPatrol

        static readonly CommandDescriptor CdPatrol = new CommandDescriptor {
            Name = "Patrol",
            Aliases = new[] { "pat" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Patrol },
            Help = "Teleports you to the next player in need of checking.",
            Handler = PatrolHandler
        };

        static void PatrolHandler( Player player, Command cmd ) {
            if( player.World == null ) PlayerOpException.ThrowNoWorld( player );

            Player target = player.World.GetNextPatrolTarget( player );
            if( target == null ) {
                player.Message( "Patrol: No one to patrol in this world." );
                return;
            }

            player.TeleportTo( target.Position );
            player.Message( "Patrol: Teleporting to {0}", target.ClassyName );
        }


        static readonly CommandDescriptor CdSpecPatrol = new CommandDescriptor {
            Name = "SpecPatrol",
            Aliases = new[] { "spat" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Patrol, Permission.Spectate },
            Help = "Teleports you to the next player in need of checking.",
            Handler = SpecPatrolHandler
        };

        static void SpecPatrolHandler( Player player, Command cmd ) {
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );
            Player target = playerWorld.GetNextPatrolTarget( player );
            if( target == null ) {
                player.Message( "Patrol: No one to patrol in this world." );
                return;
            }

            target.LastPatrolTime = DateTime.UtcNow;
            player.Spectate( target );
        }

        #endregion


        #region Mute / Unmute

        static readonly TimeSpan MaxMuteDuration = TimeSpan.FromDays( 700 ); // 100w0d

        static readonly CommandDescriptor CdMute = new CommandDescriptor {
            Name = "Mute",
            Aliases = new[] { "stfu" },
            Category = CommandCategory.Moderation | CommandCategory.Chat,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Mute },
            Help = "Mutes a player for a specified length of time.",
            Usage = "/Mute PlayerName Duration",
            Handler = MuteHandler
        };

        static void MuteHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            string timeString = cmd.Next();
            TimeSpan duration;

            // validate command parameters
            if( targetName == null || !Player.IsValidName( targetName ) ||
                timeString == null || !timeString.TryParseMiniTimespan( out duration ) ||
                duration <= TimeSpan.Zero ) {
                CdMute.PrintUsage( player );
                return;
            }

            // check if given time exceeds maximum (700 days)
            if( duration > MaxMuteDuration ) {
                player.Message( "Maximum mute duration is {0}.", MaxMuteDuration.ToMiniString() );
                duration = MaxMuteDuration;
            }

            // find the target
            Player target = Server.FindPlayerOrPrintMatches( player, targetName, false, true );
            if( target == null ) return;

            // actually mute
            try {
                target.Info.Mute( player, duration, true, true );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }


        static readonly CommandDescriptor CdUnmute = new CommandDescriptor {
            Name = "Unmute",
            Category = CommandCategory.Moderation | CommandCategory.Chat,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Mute },
            Help = "Unmutes a player.",
            Usage = "/Unmute PlayerName",
            Handler = UnmuteHandler
        };

        static void UnmuteHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( targetName == null || !Player.IsValidName( targetName ) ) {
                CdUnmute.PrintUsage( player );
                return;
            }

            // find target
            Player target = Server.FindPlayerOrPrintMatches( player, targetName, false, true );
            if( target == null ) return;

            try {
                target.Info.Unmute( player, true, true );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }

        #endregion


        #region Spectate / Unspectate

        static readonly CommandDescriptor CdSpectate = new CommandDescriptor {
            Name = "Spectate",
            Aliases = new[] { "follow", "spec" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Spectate },
            Usage = "/Spectate PlayerName",
            Handler = SpectateHandler
        };

        static void SpectateHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( targetName == null ) {
                PlayerInfo lastSpec = player.LastSpectatedPlayer;
                if( lastSpec != null ) {
                    Player spec = player.SpectatedPlayer;
                    if( spec != null ) {
                        player.Message( "Now spectating {0}", spec.ClassyName );
                    } else {
                        player.Message( "Last spectated {0}", lastSpec.ClassyName );
                    }
                } else {
                    CdSpectate.PrintUsage( player );
                }
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, targetName, false, true );
            if( target == null ) return;

            if( target == player ) {
                player.Message( "You cannot spectate yourself." );
                return;
            }

            if( !player.Can( Permission.Spectate, target.Info.Rank ) ) {
                player.Message( "You may only spectate players ranked {0}&S or lower.",
                player.Info.Rank.GetLimit( Permission.Spectate ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            if( !player.Spectate( target ) ) {
                player.Message( "Already spectating {0}", target.ClassyName );
            }
        }


        static readonly CommandDescriptor CdUnspectate = new CommandDescriptor {
            Name = "Unspectate",
            Aliases = new[] { "unfollow", "unspec" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Spectate },
            Handler = UnspectateHandler
        };

        static void UnspectateHandler( Player player, Command cmd ) {
            if( !player.StopSpectating() ) {
                player.Message( "You are not currently spectating anyone." );
            }
        }

        #endregion


        // freeze target if player is allowed to do so
        static void FreezeIfAllowed( Player player, PlayerInfo targetInfo ) {
            if( targetInfo.IsOnline && !targetInfo.IsFrozen && player.Can( Permission.Freeze, targetInfo.Rank ) ) {
                try {
                    targetInfo.Freeze( player, true, true );
                    player.Message( "Player {0}&S has been frozen while you retry.", targetInfo.ClassyName );
                } catch( PlayerOpException) { }
            }
        }


        // warn player if others are still online from target's IP
        static void WarnIfOtherPlayersOnIP( Player player, PlayerInfo targetInfo, Player except ) {
            Player[] otherPlayers = Server.Players.FromIP( targetInfo.LastIP )
                                                  .Except( except )
                                                  .ToArray();
            if( otherPlayers.Length > 0 ) {
                player.Message( "&WWarning: Other player(s) share IP with {0}&W: {1}",
                                targetInfo.ClassyName,
                                otherPlayers.JoinToClassyString() );
            }
        }
    }
}