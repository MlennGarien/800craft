//Zombie game for 800Craft, Copyright Jon Baker 2011. V1.0 10/12/2011

//my code sucks.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft;
using fCraft.Events;

static class GameCommands
{

    internal static void Init()
    {
        CommandManager.RegisterCommand(CdZombieGame);
        Player.Moving += new EventHandler<PlayerMovingEventArgs>(ZombieCheck);
        Player.Disconnected += new EventHandler<PlayerDisconnectedEventArgs>(Player_Disconnected);
    }

    static readonly CommandDescriptor CdZombieGame = new CommandDescriptor
        {
            Name = "Zombiegame",
            Category = CommandCategory.World,
            Permissions = new Permission[] { Permission.Chat },
            IsConsoleSafe = false,
            Usage = "/Zombiegame start | end",
            Help = "No one can help you.",
            Handler = ZGame
        };

    public static void ZGame(Player player, Command cmd)
    {
        string Option = cmd.Next();

        if (Option.Contains("start"))
        {
            ZombieGame.WorldName = player.World;
            startGame(player, ZombieGame.WorldName);

            return;
        }
        if (Option == null)
        {
            CdZombieGame.PrintUsage(player);
            return;
        }

        if (Option == "infect")
        {
            string a = cmd.Next();
            Player p = Server.FindPlayerOrPrintMatches(player, a, true, true);
            p.Info.Name = "_Infected_";
            //p.World.UpdatePlayerList();
            p.ResetVisibleEntities();
            p.Info.IsZombie = true;
            return;
        }

        else if (Option.Contains("end"))
        {
            EndGame(player, ZombieGame.WorldName);
            return;
        }

        else
        {
            player.Message("Invalid option"); return;
        }
    }


    public static void startGame(Player player, World world)
    {
        if (ZombieGame.GameOn)
        {
            player.Message("A Zombie game is already on for {0}", world.ClassyName);
            return;
        }

        if (!ZombieGame.GameOn)
        {
            /*if (world.Players.Count() == 2)
            {
                player.Message("A game cannot start unless more than 2 people are in your world");
                return;
            }*/

            ZombieGame.GameOn = true;
            world.ZombieGame = true;
            player.Message("A Zombie game has been started on {0}", world.ClassyName);
            Server.Players.Message("{0} started a Zombie Game on {1}", player.ClassyName, world.ClassyName);
            world.Players.Message("A Zombie will be picked at random in 30 seconds");
            try
            {
                if (world.ZombieGame)
                {
                    Scheduler.NewTask(t => ZombieGame.TooLate = true).RunOnce(TimeSpan.FromSeconds(30));
                    Scheduler.NewTask(t => ZombieGame.WorldName.Players.Message("An infection has started.... RUN!!!")).RunOnce(TimeSpan.FromSeconds(30));
                    Scheduler.NewTask(t => RandomPicker(player, ZombieGame.WorldName)).RunOnce(TimeSpan.FromSeconds(30));
                    Scheduler.NewTask(t => InWorldCheck(player, ZombieGame.WorldName)).RunForever(TimeSpan.FromMilliseconds(999));
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, "" + ex);
            }
            return;
        }

    }

    public static void EndGame(Player player, World world)
    {
        if (!ZombieGame.GameOn)
        {
            player.Message("A Zombie game has not started on world {0}", world.ClassyName);
            return;
        }

        else
        {
            ZombieGame.GameOn = false;
            world.ZombieGame = false;
            ZombieGame.TooLate = false;

            //change all players back
            foreach (Player p in Server.Players)
            {
                if (p.Info.Name == "_Infected_")
                {
                    try
                    {
                        p.Info.Name = p.Info.OriginalName;
                        p.Info.ArrivedLate = false;
                        // world.UpdatePlayerList();
                        p.ResetVisibleEntities();
                        p.Info.IsZombie = false;
                        ZombieGame.Zombies.Remove(p);
                        p.Info.DisplayedName = p.Info.Dname;
                        p.Message("Changing you back to human {0}", p.Info.Name);
                        ZombieGame.FinalName = null;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogType.Error, "ZombieGame Ending: " + ex);
                    }
                    foreach (Player H in ZombieGame.Humans)
                    {
                        ZombieGame.Humans.Remove(p);
                    }
                }
            }
            //end everything here
        }
    }


    public static void RandomPicker(Player player, World world)
    {
        Random rand = new Random();

        int min = 1, max = ZombieGame.Humans.Count(), num;
        num = rand.Next(min, max);
        Player toZombie = ZombieGame.Humans[num];
        try
        {
            if (toZombie != null && !toZombie.Info.Name.Contains("_Infected_"))
            {
                toZombie.Info.Name = "_Infected_";
                toZombie.Info.DisplayedName = "&c" + toZombie.Info.OriginalName;
                //toZombie.World.UpdatePlayerList();
                toZombie.ResetVisibleEntities();
                toZombie.Info.IsZombie = true;
                world.Players.Message(Color.Red + toZombie.Info.OriginalName + " is first Zombie!!");
                ZombieGame.Humans.Remove(toZombie);
                ZombieGame.Zombies.Add(toZombie);

            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, "ZombiePicker: " + ex);
        }
    }

    public static void InWorldCheck(Player player, World world)
    {
        if (ZombieGame.WorldName.IsLoaded)
        {
            if (!ZombieGame.TooLate)
            {
                if (ZombieGame.GameOn)
                {
                    foreach (Player p in ZombieGame.WorldName.Players) //saves each players original name into a list
                    {
                        if (p.Info.Name != "_Infected_")
                        {
                            if (p.World.Name == ZombieGame.WorldName.Name)
                            {
                                if (!ZombieGame.Humans.Contains(p))
                                {
                                    p.Info.OriginalName = p.Info.Name;
                                    ZombieGame.Humans.Add(p);
                                }
                                //list of original names in case of server crash / powercut goes here. JSON.
                            }
                        }
                    }
                }
            }

            if (ZombieGame.Humans.Count == 1)
            {
                foreach (Player S in ZombieGame.Humans)
                    ZombieGame.FinalName = S.Info.OriginalName;
            }

            if (ZombieGame.Humans.Count == 0)
                ZombieGame.WorldName.Players.Message("The game is over!\nThe last survivor was {0}", ZombieGame.FinalName);
                EndGame(player, ZombieGame.WorldName);

            if (ZombieGame.TooLate || !ZombieGame.TooLate)
            {
                foreach (Player Q in ZombieGame.Zombies)
                {
                    if (Q.World.Name != ZombieGame.WorldName.Name)
                    {
                        Q.Info.Name = Q.Info.OriginalName;
                        Q.Info.DisplayedName = Q.Info.Dname;
                        Q.Info.ArrivedLate = false;
                        //Q.World.UpdatePlayerList();
                        Q.ResetVisibleEntities();
                        Q.Message("Changing you back to human {0}", Q.Info.OriginalName);
                        Q.Info.IsZombie = false;
                        ZombieGame.Humans.Remove(Q);
                        ZombieGame.Zombies.Remove(Q);
                    }
                }
                foreach (Player H in ZombieGame.Humans)
                {
                    if (H.World.Name != ZombieGame.WorldName.Name)
                    {
                        ZombieGame.Humans.Remove(H);
                    }
                }
            }

            if (ZombieGame.TooLate)
            {
                foreach (Player p in Server.Players)
                {
                    if (p.World == ZombieGame.WorldName && !ZombieGame.Humans.Contains(p))
                    {
                        p.Info.ArrivedLate = true;

                        if (p.Info.ArrivedLate && !p.Info.IsZombie)
                        {
                            p.Info.OriginalName = p.Info.Name;

                            p.Info.Name = "_Infected_";
                            p.Info.DisplayedName = "&C" + p.Info.OriginalName;
                            //p.World.UpdatePlayerList();
                            p.ResetVisibleEntities();
                            p.Info.IsZombie = true;
                            ZombieGame.Humans.Remove(p);
                            ZombieGame.Zombies.Add(p);
                            p.Message("You joined too late and spawned as a Zombie");
                        }
                    }
                }
            }
        }
    }
            
        
    


    static void Player_Disconnected(object sender, PlayerDisconnectedEventArgs e)
    {
        if (ZombieGame.GameOn)
        {
            try
            {
                if (e.Player.Info.IsZombie && e.Player.Info.Name == "_Infected_")
                {
                    e.Player.Info.Name = e.Player.Info.OriginalName;
                    e.Player.Info.DisplayedName = e.Player.Info.Dname;
                    ZombieGame.Zombies.Remove(e.Player);
                    e.Player.Info.IsZombie = false;
                }

                
                Server.UnregisterPlayer(e.Player);
                Logger.Log(LogType.SystemActivity, "ZombieGame: Player {0} left, reverting name back.", e.Player.Info.OriginalName);
            }

            catch (Exception ex)
            {
                Logger.Log(LogType.Error, "ZombieGame: " + ex);
            }
        }
    }


    /*static void ZombieCheck(object sender, PlayerMovingEventArgs e) //changes to zombies... maybe
    {
        if (e.Player.World.ZombieGame && ZombieGame.TooLate)
        {
            if (e.Player.Info.Name.StartsWith("&F"))
            {
                try
                {
                    for (int j = 1; j < e.Player.World.Players.Length; j++)
                    {
                        Player Human = e.Player.World.Players[j];
                        if (e.Player != Human && Human != null && !Human.Info.IsZombie)
                        {
                            Position p2 = Human.Position;
                            Position d = e.Player.Position;

                            if (d.X * d.X + d.Y * d.Y <= 1296 && Math.Abs(d.Z) <= 52)
                            {
                                if (Human.Info.Name.StartsWith("&F") && !Human.Info.IsZombie)
                                {
                                    e.Player.Info.Name = "_Infected_";
                                    e.Player.World.UpdatePlayerList();
                                    e.Player.UpdateVisibleEntities();
                                    e.Player.World.Players.Message(Color.Red + e.Player.Name + " INFECTED " + Human.Info.OriginalName);
                                }
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex) { Logger.Log(LogType.Warning, "Problem with zombie tracking: " + ex); }
            }
        }
    }
}*/

    static void ZombieCheck(object sender, PlayerMovingEventArgs e)//turns players into zombies. Maybe.
    {
        if (ZombieGame.TooLate)
        {
            for (int j = 1; j < ZombieGame.Humans.Count; j++)
            {
                Player Human = ZombieGame.Humans[j];

                if (e.Player != Human && Human != null && !Human.Info.IsZombie)
                {
                    foreach (Player p in ZombieGame.Humans)
                    {
                        try
                        {
                            Vector3I oldPos = new Vector3I(e.OldPosition.X / 32, e.OldPosition.Y / 32, e.OldPosition.Z / 32);
                            Vector3I newPos = new Vector3I(e.NewPosition.X / 32, e.NewPosition.Y / 32, e.NewPosition.Z / 32);

                            if ((oldPos.X != newPos.X) || (oldPos.Y != newPos.Y) || (oldPos.Z != newPos.Z))
                            {
                                if (Human.Position == e.Player.Position && !Human.Info.IsZombie && Human.Info.Name != "_Infected_")
                                {
                                    Human.Info.DisplayedName = "&C" + Human.Info.OriginalName;
                                    Human.Info.Name = "_Infected_";
                                    //e.Player.World.UpdatePlayerList();
                                    e.Player.ResetVisibleEntities();
                                    e.Player.World.Players.Message("{0} &cInfected {1}", e.Player.ClassyName, Human.Info.OriginalName);
                                    ZombieGame.Humans.Remove(Human);
                                    ZombieGame.Zombies.Add(Human);
                                }

                                if (e.Player.IsUsingWoM)
                                    e.Player.Kick("WOM is not allowed on Zombie games", LeaveReason.Kick);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(LogType.Error, "ZombieGame: problem with tracking zombies: " + ex);
                        }
                    }
                }
            }
        }
    }
}


    

