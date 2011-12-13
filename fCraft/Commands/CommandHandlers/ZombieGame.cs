//Zombie game for 800Craft, Copyright Jon Baker 2011. V1.0 10/12/2011

//my code sucks.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft;
using fCraft.Events;

namespace fCraft
{
    public class ZombieGame
    {
        public static bool GameOn = false;
        public static bool TooLate = false;
        public static List<Player> InGame = new List<Player>(); //list of all players in game, updated every 5 seconds
        private static ZombieGame instance;

        private ZombieGame()
        {
            //Single, just like me
        }

        public static ZombieGame GetInstance()
        {
            if (instance == null)
            {
                instance = new ZombieGame();
                Player.Moving += new EventHandler<Events.PlayerMovingEventArgs>(ZombieCheck);
                Player.Disconnected += new EventHandler<Events.PlayerDisconnectedEventArgs>(Player_Disconnected);
            }

            return instance;
        }

        public static void startGame(Player player, World world)
        {
            if (GameOn)
            {
                player.Message("A Zombie game is already on for {0}", world.ClassyName);
                return;
            }

            if (!GameOn)
            {
                if (InGame.Count > 2)
                {
                    player.Message("A game cannot start unless more than 2 people are in your world");
                    return;
                }

                GameOn = true;
                player.Message("A Zombie game has been started on {0}", world.ClassyName);
                Server.Players.Message("{0} started a Zombie Game on {1}", player.ClassyName, world.ClassyName);
                world.Players.Message("A Zombie will be picked at random in 30 seconds");
                Scheduler.NewTask(t => TooLate = true).RunOnce(TimeSpan.FromSeconds(30));
                Scheduler.NewTask(t => world.Players.Message("An infection has started.... RUN!!!")).RunOnce(TimeSpan.FromSeconds(30));
                Scheduler.NewTask(t => RandomPicker(player, world)).RunOnce(TimeSpan.FromSeconds(40));
                Scheduler.NewTask(t => InWorldCheck(player, world)).RunForever(TimeSpan.FromMilliseconds(4999));
                return;
            }
            
        }

        public static void EndGame(Player player, World world)
        {
            if (!GameOn)
            {
                player.Message("A Zombie game is has not started on world {0}", world.ClassyName);
                return;
            }

            else
            {
                GameOn = false;

                //change all players back
                foreach (Player p in Server.Players)
                {
                    if (p.Info.IsZombie)
                    {
                        InGame.Remove(p);
                        Player.VisibleEntity entity = new Player.VisibleEntity(p.Position, (sbyte)p.Info.ID, null);//zombie changer?
                        entity.IsZombie = true;
                        p.UpdateVisibleEntities();
                        p.Info.IsZombie = false;
                    }
                }

                //end everything here
                
            }

        }

        public static void RandomPicker(Player player, World world)
        {
            InGame.Count();
            Player zombie = new Player(InGame.Count.ToString());
            zombie.Info.Name = "_Infected_";
            world.Players.Message("{0} is the first to be infected... run away!", zombie.Info.OriginalName);
        }

        public static void InWorldCheck(Player player, World world)
        {
            if (!TooLate)
            {
                foreach (Player p in world.Players) //saves each players original name into a list
                {
                    InGame.Add(p);
                }
            }

            if (TooLate)
            {
                foreach (Player p in world.Players)
                {
                    if (p.Info.ArrivedLate && !p.Info.IsZombie)
                    {
                        InGame.Add(player); //adds player to InGame

                        Player.VisibleEntity entity = new Player.VisibleEntity(p.Position, (sbyte)p.Info.ID, null);//zombie changer?
                        entity.IsZombie = true;
                        p.UpdateVisibleEntities();
                        p.Info.IsZombie = true;
                        player.Message("You joined too late and spawned as a Zombie");
                    }
                }
            }
        }

        static void Player_Disconnected(object sender, Events.PlayerDisconnectedEventArgs e)
        {
            if (GameOn)
            {
                ZombieGame.GetInstance().UnregisterPlayer(e.Player);
            }
        }

        public void UnregisterPlayer(Player p)
        {
            try
            {
                if (p.Info.IsZombie)
                {
                    InGame.Remove(p);
                    p.Info.IsZombie = false;
                }
            }

            catch (Exception ex)
            {
                Logger.Log(LogType.Error, "ZombieGame: " + ex);
            }
        }

        static void ZombieCheck(object sender, Events.PlayerMovingEventArgs e)//turns players into zombies. Maybe.
        {
            if (GameOn)
            {
                foreach (Player p in e.Player.World.Players)
                {
                    Vector3I oldPos = new Vector3I(e.OldPosition.X / 32, e.OldPosition.Y / 32, e.OldPosition.Z / 32);
                    Vector3I newPos = new Vector3I(e.NewPosition.X / 32, e.NewPosition.Y / 32, e.NewPosition.Z / 32);

                    if ((oldPos.X != newPos.X) || (oldPos.Y != newPos.Y) || (oldPos.Z != newPos.Z))
                    {
                        if (e.Player.Position == p.Position && !e.Player.Info.IsZombie)
                        {
                            Player.VisibleEntity entity = new Player.VisibleEntity(e.Player.Position, (sbyte)e.Player.Info.ID, null);//zombie changer?
                            
                            entity.IsZombie = true;
                            p.UpdateVisibleEntities();
                            e.Player.Info.IsZombie = true;
                            //p.World.Message("{0} &cInfected {1}", p.ClassyName, e.Player.ClassyName);
                        }

                        if (e.Player.IsUsingWoM)
                            e.Player.Kick("WOM is not allowed on Zombie games", LeaveReason.Kick);
                    }
                }
            }
        }
    }
}

    

