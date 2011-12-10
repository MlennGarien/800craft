//Zombie game for 800Craft, Copyright Jon Baker 2011. V1.0 10/12/2011

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
        public static List<Player> InGame = new List<Player>();
        public static List<string> OriginalNames = new List<string>(); //for loading names if a server crash happens

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
                GameOn = true;
                player.Message("A Zombie game is already on for {0}", world.ClassyName);
                Server.Players.Message("{0} started a Zombie Game on {1}", player.ClassyName, world.ClassyName);
                return;
            }


            foreach (Player p in world.Players) //saves each players original name into a list
            {
                OriginalNames.Add(p.Info.Name); //saves the name
                p.Info.OriginalName = p.Info.Name;
                InGame.Add(p); //saves the players who need to be reverted
                //do displayedNames need to be stored?
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
                    if (p.Info.Name.Contains("_Infected_"))
                        p.Info.Name = p.Info.OriginalName;
                }

                //end everything here
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
                if (p.Info.Name.Contains("_Infected_"))
                {
                    p.Info.Name = p.Info.OriginalName;
                    InGame.Remove(p);
                }


            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, "ZombieGame: Player left: " + ex);
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
                        if (e.Player.Position == p.Position && !e.Player.Info.Name.Contains("_Infected_"))
                        {
                            e.Player.Info.Name = "&c_Infected_";
                            //add p infected e here
                        }
                    }
                }
            }
        }
    }
}

    

