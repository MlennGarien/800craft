using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;

namespace fCraft
{
    public class ZombieGame
    {
        private const string _zomb = "&8_Infec&Cted_";
        private World _world;
        private int _humanCount;
        private SchedulerTask _task;

        public ZombieGame(World world){
            _world = world;
            _humanCount = _world.Players.Count();
            Player.Moved += OnPlayerMoved;
            _world.positions = new Player[_world.Map.Width,
                    _world.Map.Length, _world.Map.Height];
            _task = new SchedulerTask(Interval, true).RunForever(TimeSpan.FromSeconds(45));
        }

        public void Start(){
            _world.gameMode = World.GameMode.ZombieSurvival; //set the game mode
            _humanCount = _world.Players.Where(p => p.iName != _zomb).Count(); //count all players
            _world.Players.Message("&WThe game has begun! Run!!!");
        }

        public void RandomPick(){
            Random rand = new Random();
            int pick = rand.Next(0, _world.Players.Count() + 1);
            Player picked = _world.Players[pick];
            toZombie(null, picked);
        }

        public void Interval(SchedulerTask task){
            if (_world.gameMode != World.GameMode.ZombieSurvival){
                task.Stop();
                return;
            }
            _humanCount = _world.Players.Where(p => p.iName != _zomb).Count(); //refresh incase new players joined
            _world.Players.Message("&WThere are {0} humans", _humanCount.ToString());
            foreach(Player p in _world.Players){
                if(p.iName == _zomb) p.Message("&8You are " + _zomb);
                else p.Message("&8You are a Human");
            }
        }

        void OnPlayerMoved(object sender, PlayerMovedEventArgs e){
            if (e.Player.World.Name == _world.Name){
                
               // _world.positions[e.OldPosition.X, e.OldPosition.Y, e.OldPosition.Z] = null; //remove player from prev position
                //_world.positions[e.NewPosition.X, e.NewPosition.Y, e.NewPosition.Z] = e.Player; //move player to new position

                /*foreach (Player neighbor in _world.positions){
                    Player smone = _world.positions[neighbor.Position.X, neighbor.Position.Y, neighbor.Position.Z];
                    if (null != smone){
                        toZombie(smone, e.Player); //contains all the checks (bool)
                    }
                }*/
            }
        }

        public bool toZombie(Player infector, Player target){
            if (infector.iName == _zomb && target.iName != _zomb){
                target.iName = _zomb;
                target.entityChanged = true;
                if (infector != null){
                    _world.Players.Message("{0}&S was &cInfected&S by {1}",
                        target.ClassyName, infector.ClassyName);
                    target.Message("&WYou are now a Zombie!");
                }else{
                    _world.Players.Message("{0}&S has been the first to get &cInfected. &CPanick!!!!!",
                        infector.ClassyName);
                }
                return true;
            }
            return false;
        }

        public void RevertNames(){
            foreach (Player p in _world.Players){
                RevertPlayerName(p);
            }
        }

        public void RevertPlayerName(Player p){
            if (p.iName == _zomb){
                p.iName = null;
                p.entityChanged = true;
                p.Message("You are being disinfected");
            }
        }
    }
}
