using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
    public class ZombieGame
    {
        public static bool GameOn = false;
        public static bool TooLate = false;
        public static string FinalName;
        //public static List<Player> InGame = new List<Player>(); //list of all players in game, updated every 5 seconds
        public static List<Player> Humans = new List<Player>();
        public static List<Player> Zombies = new List<Player>();
        public static World WorldName; //name of the zombie game world
        public static int HumanCount = 0;
    }

}
