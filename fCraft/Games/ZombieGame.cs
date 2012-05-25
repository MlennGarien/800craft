using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
    public class ZombieGame
    {
        private string _zomb = "&c_Infec&8ted_";
        private World _world;
        public ZombieGame(World world)
        {
            _world = world;
        }

        public bool toZombie(Player infector, Player target)
        {
            if (target.Immortal) return false;
            if (infector.iName == _zomb && target.iName != _zomb)
            {
                target.iName = _zomb;
                target.entityChanged = true;
                _world.Players.Message("{0}&S was &cInfected&S by {1}",
                    infector.ClassyName, target.ClassyName);
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
