//Copyright (C) <2012>  <Jon Baker, Glenn Mariën and Lao Tszy>

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.

//Copyright (C) <2012> Jon Baker(http://au70.net)
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using fCraft.MapConversion;
using fCraft.Events;

namespace fCraft
{
    public class MineField
    {
        private World _world;
        private const int _ground = 15; 
        private Map _map;
        public List<Player> Failed; //public so the Mines class can access it
        public ConcurrentDictionary<string, Vector3I> Mines;
        private Random _rand;
        public MineField()
        {
            Failed = new List<Player>();
            Mines = new ConcurrentDictionary<string, Vector3I>();
            Player.Moving += PlayerMoving;
            _rand = new Random();
        }
        public void Start()
        {
            Map map = MapGenerator.GenerateFlatgrass(64, 128, 32);
            map.Save("maps/minefield.fcm");
            if (_world != null){
                WorldManager.RemoveWorld(_world);
            }
            WorldManager.AddWorld(Player.Console, "Minefield", map, true);
            _map = map;
            _world = WorldManager.FindWorldExact("Minefield");
            SetUpRed();
            SetUpGreen();
            SetUpMines();
            _map.Spawn = new Position(_map.Width / 2, 5, _ground + 3).ToVector3I().ToPlayerCoords();
            _world.LoadMap();
            _world.gameMode = World.GameMode.MineField;
        }

        private void SetUpRed(){
            for (int x = 1; x <= _map.Width; x++){
                for (int y = 1; y <= 10; y++){
                    _map.SetBlock(x, y, _ground, Block.Red);
                }
            }
        }

        private void SetUpGreen(){
            for (int x = _map.Width; x >= 1; x--){
                for (int y = _map.Length; y >= _map.Length - 10; y--){
                    _map.SetBlock(x, y, _ground, Block.Green);
                }
            }
        }

        private void SetUpMines(){
            for (short i = 0; i < _map.Width; ++i){
                for (short j = 0; j < _map.Length; ++j){
                    if (_rand.Next(1, 100) > 95){
                        if (_map.GetBlock(i, j, _ground) != Block.Red && 
                            _map.GetBlock(i, j, _ground) != Block.Green){
                            Vector3I vec = new Vector3I(i, j, _ground);
                            Mines.TryAdd(vec.ToString(), vec);
                            _map.SetBlock(vec, Block.Red);//
                        }
                    }
                }
            }
        }

        private bool PlayerBlowUpCheck(Player player)
        {
            if (!Failed.Contains(player))
            {
                Failed.Add(player);
                return true;
            }
            return false;
        }

        private void PlayerMoving(object sender, PlayerMovingEventArgs e)
        {
            if (e.Player.World.gameMode == World.GameMode.MineField && !Failed.Contains(e.Player)){
                Vector3I oldPos = new Vector3I(e.OldPosition.X / 32, e.OldPosition.Y / 32, e.OldPosition.Z / 32);
                Vector3I newPos = new Vector3I(e.NewPosition.X / 32, e.NewPosition.Y / 32, e.NewPosition.Z / 32);

                // Check if the player jumped, flew, whatevers
                if (oldPos.Z != newPos.Z){
                    if (newPos.Z > _ground + 2){
                        e.Player.TeleportTo(e.OldPosition);
                        newPos = oldPos;
                    }
                }
                foreach (Vector3I pos in Mines.Values){
                    Vector3I checkPos = new Vector3I(pos.X, pos.Y, pos.Z + 2);
                    if (newPos == checkPos){
                        e.Player.TeleportTo(_map.Spawn);
                        Failed.Add(e.Player);
                        Vector3I removed;
                        Mines.TryRemove(pos.ToString(), out removed);
                    }
                }
            }
        }
    }
}
