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
    class MineField
    {
        private static World _world;
        private const int _ground = 1;
        private static Map _map;
        public static List<Player> Failed;
        public static ConcurrentDictionary<string, Vector3I> Mines;
        private static Random _rand;
        private static bool _stopped;
        private static MineField instance;

        private MineField()
        {
            // Empty, singleton
        }

        public static MineField GetInstance()
        {
            if (instance == null)
            {
                instance = new MineField();
                Failed = new List<Player>();
                Mines = new ConcurrentDictionary<string, Vector3I>();
                Player.Moving += new EventHandler<PlayerMovingEventArgs>(PlayerMoving);
                Player.PlacingBlock += new EventHandler<PlayerPlacingBlockEventArgs>(PlayerPlacing);
                _rand = new Random();
                _stopped = false;
            }
            return instance;
        }
        public static void Start(Player player)
        {
            Map map = MapGenerator.GenerateEmpty(64, 128, 16);
            map.Save("maps/minefield.fcm");
            if (_world != null)
            {
                WorldManager.RemoveWorld(_world);
            }
            WorldManager.AddWorld(Player.Console, "Minefield", map, true);
            _map = map;
            _world = WorldManager.FindWorldExact("Minefield");
            SetUpRed();
            SetUpMiddleWater();
            SetUpGreen();
            SetUpMines();
            _map.Spawn = new Position(_map.Width / 2, 5, _ground + 3).ToVector3I().ToPlayerCoords();
            _world.LoadMap();
            _world.gameMode = GameMode.MineField;
            _world.EnableTNTPhysics(Player.Console, false);
            Server.Message("{0}&S started a game of MineField on world Minefield!", player.ClassyName);
            WorldManager.SaveWorldList();
            Server.RequestGC();
        }

        public static void Stop(Player player, bool Won)
        {
            if (Failed != null && Mines != null)
            {
                Failed.Clear();

                foreach (Vector3I m in Mines.Values)
                {
                    Vector3I removed;
                    Mines.TryRemove(m.ToString(), out removed);
                }
            }
            World world = WorldManager.FindWorldOrPrintMatches(player, "Minefield");
            WorldManager.RemoveWorld(world);
            WorldManager.SaveWorldList();
            Server.RequestGC();
            instance = null;
            if (Won)
            {
                Server.Players.Message("{0}&S Won the game of MineField!", player.ClassyName);
            }
            else
            {
                Server.Players.Message("{0}&S aborted the game of MineField", player.ClassyName);
            }
        }

        private static void SetUpRed()
        {
            for (int x = 0; x <= _map.Width; x++)
            {
                for (int y = 0; y <= 10; y++)
                {
                    _map.SetBlock(x, y, _ground, Block.Red);
                    _map.SetBlock(x, y, _ground - 1, Block.Black);
                }
            }
        }

        private static void SetUpMiddleWater()
        {
            for (int x = _map.Width; x >= 0; x--)
            {
                for (int y = _map.Length -50; y >= _map.Length - 56; y--)
                {
                    _map.SetBlock(x, y, _ground, Block.Water);
                    _map.SetBlock(x, y, _ground - 1, Block.Water);
                }
            }
        }

        private static void SetUpGreen()
        {
            for (int x = _map.Width; x >= 0; x--)
            {
                for (int y = _map.Length; y >= _map.Length - 10; y--)
                {
                    _map.SetBlock(x, y, _ground, Block.Green);
                    _map.SetBlock(x, y, _ground - 1, Block.Black);
                }
            }
        }

        private static void SetUpMines()
        {
            for (short i = 0; i <= _map.Width; ++i)
            {
                for (short j = 0; j <= _map.Length; ++j)
                {
                    if (_map.GetBlock(i, j, _ground) != Block.Red &&
                        _map.GetBlock(i, j, _ground) != Block.Green &&
                        _map.GetBlock(i, j, _ground) != Block.Water)
                    {
                        _map.SetBlock(i, j, _ground, Block.Dirt);
                        _map.SetBlock(i, j, _ground - 1, Block.Dirt);
                        if (_rand.Next(1, 100) > 96)
                        {
                            Vector3I vec = new Vector3I(i, j, _ground);
                            Mines.TryAdd(vec.ToString(), vec);
                            //_map.SetBlock(vec, Block.Red);//
                        }
                    }
                }
            }
        }

        public static bool PlayerBlowUpCheck(Player player)
        {
            if (!Failed.Contains(player))
            {
                Failed.Add(player);
                return true;
            }
            return false;
        }

        private static void PlayerPlacing(object sender, PlayerPlacingBlockEventArgs e)
        {
            World world = e.Player.World;
            if (world.gameMode == GameMode.MineField)
            {
                e.Result = CanPlaceResult.Revert;
            }
        }

        private static void PlayerMoving(object sender, PlayerMovingEventArgs e)
        {
            if (_world != null && e.Player.World == _world)
            {
                if (_world.gameMode == GameMode.MineField && !Failed.Contains(e.Player))
                {
                    if (e.NewPosition != null)
                    {
                        Vector3I oldPos = new Vector3I(e.OldPosition.X / 32, e.OldPosition.Y / 32, e.OldPosition.Z / 32);
                        Vector3I newPos = new Vector3I(e.NewPosition.X / 32, e.NewPosition.Y / 32, e.NewPosition.Z / 32);

                        if (oldPos.X != newPos.X || oldPos.Y != newPos.Y || oldPos.Z != newPos.Z)
                        {
                            if (!_map.InBounds(newPos))
                            {
                                e.Player.TeleportTo(_map.Spawn);
                                newPos = (Vector3I)_map.Spawn;
                            }
                            // Check if the player jumped, flew, whatevers
                            if (newPos.Z > _ground + 2)
                            {
                                e.Player.TeleportTo(e.OldPosition);
                                newPos = oldPos;
                            }
                            foreach (Vector3I pos in Mines.Values)
                            {
                                if (newPos == new Vector3I(pos.X, pos.Y, pos.Z + 2) || 
                                    newPos == new Vector3I(pos.X, pos.Y, pos.Z + 1)|| 
                                    newPos == new Vector3I(pos.X, pos.Y, pos.Z))
                                {
                                    _world.Map.QueueUpdate(new BlockUpdate(null, pos, Block.TNT));
                                    _world.AddPhysicsTask(new TNTTask(_world, pos, null, true, false), 0);
                                    Vector3I removed;
                                    Mines.TryRemove(pos.ToString(), out removed);
                                }
                            }
                            if (_map.GetBlock(newPos.X, newPos.Y, newPos.Z - 2) == Block.Green
                                && !_stopped)
                            {
                                _stopped = true;
                                Stop(e.Player, true);
                            }
                        }
                    }
                }
            }
        }
    }
}
