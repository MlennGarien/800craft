using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;

namespace fCraft
{
    class ZombieGame
    {
        private const string _zomb = "&8_Infec&Cted_";
        private static World _world;
        private static int _humanCount = 0;
        private static SchedulerTask _task;
        public static ZombieGame instance;
        public static DateTime startTime;
        public static DateTime lastChecked;
        private static bool _started = false;

        private ZombieGame()
        {
          
        }

        public static ZombieGame GetInstance(World world)
        {
            if (instance == null)
            {
                _world = world;
                instance = new ZombieGame();
                startTime = DateTime.Now;
                _humanCount = _world.Players.Length;
                _world.Players.Message(_humanCount.ToString());
                Player.Moved += OnPlayerMoved;
                _task = new SchedulerTask(Interval, true).RunForever(TimeSpan.FromSeconds(1));
                //_world.positions = new Player[_world.Map.Width,
                       // _world.Map.Length, _world.Map.Height];
                _world.gameMode = GameMode.ZombieSurvival;
            }
            return instance;
        }

        public static void Start(){
            _world.gameMode = GameMode.ZombieSurvival; //set the game mode
            _humanCount = _world.Players.Where(p => p.iName != _zomb).Count(); //count all players
            Scheduler.NewTask(t => _world.Players.Message("&WThe will be starting soon..."))
                .RunRepeating(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(20), 2);
        }

        public static void Stop(Player player)
        {
            if (player != null){
                _world.Players.Message("{0}&S stopped the game of Infection on world {1}", 
                    player.ClassyName, _world.ClassyName);
            }
            RevertNames();
            _world.gameMode = GameMode.NULL;
            _humanCount = 0;
            _task = null;
            instance = null;
            _started = false;
        }

        public static void RandomPick(){
            Random rand = new Random();
            int min = 1, max = _world.Players.Length, num;
            num = rand.Next(min, max + 1);
            _world.Players.Message(num.ToString());
            Player p = _world.Players[num - 1];
            toZombie(null, p);
        }
        public static Random rand = new Random();
        public static void Interval(SchedulerTask task)
        {
            //check to stop Interval
            if (_world.gameMode != GameMode.ZombieSurvival || _world == null)
            {
                _world = null;
                task.Stop();
                return;
            }
            if (!_started)
            {
                if (startTime != null && (DateTime.Now - startTime).TotalMinutes > 1)
                {
                    /*if (_world.Players.Length < 3){
                        _world.Players.Message("&WThe game failed to start: 2 or more players need to be in the world");
                        Stop(null);
                        return;
                    }*/
                    foreach (Player p in _world.Players)
                    {
                        int x = rand.Next(2, _world.Map.Width);
                        int y = rand.Next(2, _world.Map.Length);
                        int z1 = 0;
                        for (int z = _world.Map.Height - 1; z > 0; z--)
                        {
                            if (_world.Map.GetBlock(x, y, z) != Block.Air)
                            {
                                z1 = z + 3;
                                break;
                            }
                        }
                        p.TeleportTo(new Position(x, y, z1 + 2).ToVector3I().ToPlayerCoords());
                    }
                    _started = true;
                    RandomPick();
                    lastChecked = DateTime.Now;
                    return;
                }
            }
            //calculate humans
            _humanCount = _world.Players.Where(p => p.iName != _zomb).Count();
            //check if zombies have won already
            if (_humanCount == 0)
            {
                _world.Players.Message("&WThe Humans have failed to survive... &9ZOMBIES WIN!");
                Stop(null);
                return;
            }
            //check if 5mins is up and all zombies have failed
            if (_started && startTime != null && (DateTime.Now - startTime).TotalMinutes > 6)
            {
                _world.Players.Message("&WThe Zombies have failed to infect everyone... &9HUMANS WIN!");
                Stop(null);
                return;
            }
            //if no one has won, notify players of their status every 31s
            if (lastChecked != null && (DateTime.Now - lastChecked).TotalSeconds > 30)
            {
                _world.Players.Message("&WThere are {0} humans", _humanCount.ToString());
                foreach (Player p in _world.Players)
                {
                    if (p.iName == _zomb) p.Message("&8You are " + _zomb);
                    else p.Message("&8You are a Human");
                }
                lastChecked = DateTime.Now;
            }
        }

        static void OnPlayerMoved(object sender, PlayerMovedEventArgs e){
            if (e.Player.World.gameMode == GameMode.ZombieSurvival){
                if (e.Player.World.Name == _world.Name && _world != null){
                    if (e.NewPosition != null){
                        Vector3I oldPos = new Vector3I(e.OldPosition.X / 32, e.OldPosition.Y / 32, e.OldPosition.Z / 32);
                        Vector3I newPos = new Vector3I(e.NewPosition.X / 32, e.NewPosition.Y / 32, e.NewPosition.Z / 32);
                        if (_world.Map.InBounds(newPos)){
                            if (oldPos.X != newPos.X || oldPos.Y != newPos.Y || oldPos.Z != newPos.Z){
                                foreach (Player p in _world.Players){
                                    Vector3I pos = p.Position.ToBlockCoords();
                                    if (e.NewPosition.DistanceSquaredTo(pos.ToPlayerCoords()) <= 33 * 33){
                                        toZombie(e.Player, p);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        static public void toZombie(Player infector, Player target){
            if (infector == null){
                _world.Players.Message("{0}&S has been the first to get &cInfected. &9Panic!!!!!",
                        target.ClassyName);
                target.iName = _zomb;
                target.entityChanged = true;
                return;
            }
            if (infector.iName == _zomb && target.iName != _zomb){
                target.iName = _zomb;
                target.entityChanged = true;
                _world.Players.Message("{0}&S was &cInfected&S by {1}",
                    target.ClassyName, infector.ClassyName);
                target.Message("&WYou are now a Zombie!");
                return;
            }
            else if (infector.iName != _zomb && target.iName == _zomb)
            {
                infector.iName = _zomb;
                infector.entityChanged = true;
                _world.Players.Message("{0}&S was &cInfected&S by {1}",
                    infector.ClassyName, target.ClassyName);
                infector.Message("&WYou are now a Zombie!");
            }
        }

        public static void RevertNames(){
            foreach (Player p in _world.Players){
                RevertPlayerName(p);
            }
        }

        public static void RevertPlayerName(Player p){
            if (p.iName == _zomb){
                p.iName = null;
                p.entityChanged = true;
                p.Message("You are being disinfected");
            }
        }
    }
}
