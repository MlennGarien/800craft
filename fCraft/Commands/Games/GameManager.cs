using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
    public class GameManager
    {
        public static World GameWorld = null;
        public static bool GameIsOn = false;
        public static bool IsStopping = false;
        public static List<Player> RedTeam = new List<Player>();
        public static List<Player> BlueTeam = new List<Player>();
        public static Position RedSpawn = new Position(1,1,1);
        public static Position BlueSpawn = new Position(1, 1, 1);
        public static int RedBaseCount = 3;
        public static int BlueBaseCount = 3;
        //more shit
    }
}
