using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
    public class ZombieGame
    {
        private const string _zomb = "&c_Infec&8ted_";
        private World _world;
        private int _humanCount;
        public ZombieGame(World world)
        {
            _world = world;
            _humanCount = _world.Players.Count();
        }

        public void Start()
        {
            _world.gameMode = World.GameMode.ZombieSurvival; //set the game mode
            _humanCount = _world.Players.Where(p => p.iName != _zomb).Count(); //count all players
            _world.Players.Message("&WThe game has begun! Run!!!");
        }

        public void RandomPick()
        {
            Random rand = new Random();
            int pick = rand.Next(0, _world.Players.Count() + 1);
            Player picked = _world.Players[pick];
            toZombie(null, picked);
        }

        public void Interval()
        {
            if (_world.gameMode != World.GameMode.ZombieSurvival){
                return;
            }
            _humanCount = _world.Players.Where(p => p.iName != _zomb).Count(); //refresh incase new players joined
            _world.Players.Message("&WThere are {0} humans left", _humanCount.ToString());
        }

        public bool toZombie(Player infector, Player target)
        {
            if (infector.iName == _zomb && target.iName != _zomb)
            {
                target.iName = _zomb;
                target.entityChanged = true;
                if (infector != null)
                {
                    _world.Players.Message("{0}&S was &cInfected&S by {1}",
                        target.ClassyName, infector.ClassyName);
                }else{
                    _world.Players.Message("{0}&S has been the first to get &cInfected. &CPanick!!!!!",
                        infector.ClassyName);
                }
                return true;
            }
            return false;
        }

        public void RevertNames()
        {
            foreach (Player p in _world.Players)
            {
                if (p.iName == _zomb){
                    p.iName = null;
                    p.entityChanged = true;
                    p.Message("You are being disinfected");
                }
            }
        }

        public void RevertPlayerName(Player p)
        {
            if (p.iName == _zomb)
            {
                p.iName = null;
                p.entityChanged = true;
                p.Message("You are being disinfected");
            }
        }
    }
}
