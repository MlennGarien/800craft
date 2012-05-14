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

using System;
using System.Linq;
using System.Text;
using fCraft.Events;
using System.Threading;
using Util = RandomMaze.MazeUtil;

namespace fCraft
{
    public class BlockSink : PhysicsTask
    {
        private const int Delay = 200;
        private Vector3I _pos; //tnt position
        private int _nextPos;
        private bool _firstMove = true;
        private Block type;
        public BlockSink(World world, Vector3I position, Block Type)
            : base(world)
        {
            _pos = position;
            _nextPos = position.Z - 1;
            type = Type;
        }

        protected override int PerformInternal()
        {
            lock (_world.SyncRoot)
            {
                if (_world.waterPhysics)
                {
                    if (_firstMove)
                    {
                        if (_world.Map.GetBlock(_pos) != type)
                        {
                            return 0;
                        }
                        if (_world.Map.GetBlock(_pos.X, _pos.Y, _nextPos) == Block.Water)
                        {
                            _world.Map.QueueUpdate(new BlockUpdate(null, _pos, Block.Water));
                            _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)_nextPos, type));
                            _nextPos--;
                            _firstMove = false;
                            return Delay;
                        }
                    }
                    if (_world.Map.GetBlock(_pos.X, _pos.Y, _nextPos + 1) != type)
                    {
                        return 0;
                    }
                    if (_world.Map.GetBlock(_pos.X, _pos.Y, _nextPos) == Block.Water)
                    {
                        _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)(_nextPos + 1), Block.Water));
                        _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)_nextPos, type));
                        _nextPos--;
                    }
                }
                return Delay;
            }
        }
    }
    public class BlockFloat : PhysicsTask
    {
        private const int Delay = 200;
        private Vector3I _pos;
        private int _nextPos;
        private bool _firstMove = true;
        private Block type;

        public BlockFloat(World world, Vector3I position, Block Type)
            : base(world)
        {
            _pos = position;
            _nextPos = position.Z + 1;
            type = Type;
        }

        protected override int PerformInternal()
        {
            lock (_world.SyncRoot)
            {
                if (_world.waterPhysics)
                {
                    if (_firstMove)
                    {
                        if (_world.Map.GetBlock(_pos) != type)
                        {
                            return 0;
                        }
                        if (_world.Map.GetBlock(_pos.X, _pos.Y, _nextPos) == Block.Water)
                        {
                            _world.Map.QueueUpdate(new BlockUpdate(null, _pos, Block.Water));
                            _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)_nextPos, type));
                            _nextPos++;
                            _firstMove = false;
                            return Delay;
                        }
                    }
                    if (_world.Map.GetBlock(_pos.X, _pos.Y, _nextPos - 1) != type)
                    {
                        return 0;
                    }
                    if (_world.Map.GetBlock(_pos.X, _pos.Y, _nextPos) == Block.Water)
                    {
                        _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)(_nextPos - 1), Block.Water));
                        _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)_nextPos, type));
                        _nextPos++;
                    }
                }
                return Delay;
            }
        }
    }
    class WaterPhysics
    {
        public static Thread waterThread;

        #region Tower
        public static void towerInit(object sender, Events.PlayerPlacedBlockEventArgs e)
        {
            World world = e.Player.World;
            if (e.Player.towerMode)
            {
                if (world.Map != null && world.IsLoaded)
                {
                    if (e.Context == BlockChangeContext.Manual)
                    {
                        if (e.NewBlock == Block.Iron)
                        {
                            waterThread = new Thread(new ThreadStart(delegate
                            {
                                if (e.Player.TowerCache != null)
                                {
                                    world.Map.QueueUpdate(new BlockUpdate(null, e.Player.towerOrigin, Block.Air));
                                    e.Player.towerOrigin = e.Coords;
                                    foreach (Vector3I block in e.Player.TowerCache.Values)
                                    {
                                        e.Player.Send(PacketWriter.MakeSetBlock(block, Block.Air));
                                    }
                                    e.Player.TowerCache.Clear();
                                }
                                e.Player.towerOrigin = e.Coords;
                                e.Player.TowerCache = new System.Collections.Concurrent.ConcurrentDictionary<string, Vector3I>();
                                for (int z = e.Coords.Z; z <= world.Map.Height; z++)
                                {
                                    Thread.Sleep(250);
                                    if (world.Map != null && world.IsLoaded)
                                    {
                                        if (world.Map.GetBlock(e.Coords.X, e.Coords.Y, z + 1) != Block.Air
                                            || world.Map.GetBlock(e.Coords) != Block.Iron
                                            || e.Player.towerOrigin != e.Coords
                                            || !e.Player.towerMode)
                                        {
                                            break;
                                        }
                                        else
                                        {
                                            Vector3I tower = new Vector3I(e.Coords.X, e.Coords.Y, z + 1);
                                            e.Player.TowerCache.TryAdd(tower.ToString(), tower);
                                            e.Player.Send(PacketWriter.MakeSetBlock(tower, Block.Water));
                                        }
                                    }
                                }
                            })); waterThread.Start();
                        }
                    }
                }
            }
        }

        public static void towerRemove(object sender, Events.PlayerClickingEventArgs e)
        {
            World world = e.Player.World;
            if (e.Action == ClickAction.Delete)
            {
                if (e.Coords == e.Player.towerOrigin)
                {
                    if (e.Player.TowerCache != null)
                    {
                        if (e.Block == Block.Iron)
                        {
                            e.Player.towerOrigin = new Vector3I();
                            foreach (Vector3I block in e.Player.TowerCache.Values)
                            {
                                e.Player.Send(PacketWriter.MakeSetBlock(block, world.Map.GetBlock(block)));
                            }
                            e.Player.TowerCache.Clear();
                        }
                    }
                }
            }
        }
        #endregion

        public static void drownCheck(SchedulerTask task)
        {
            try
            {
                foreach (Player p in Server.Players.Where(p=> !p.Immortal))
                {
                    if (p.World != null) //ignore console
                    {
                        if (p.World.waterPhysics)
                        {
                            Position pos = new Position(
                                (short)(p.Position.X / 32),
                                (short)(p.Position.Y / 32),
                                (short)((p.Position.Z + 1) / 32)
                            );
                            if (p.WorldMap.GetBlock(pos.X, pos.Y, pos.Z) == Block.Water)
                            {
                                if (p.DrownTime == null || (DateTime.Now - p.DrownTime).TotalSeconds > 33)
                                {
                                    p.DrownTime = DateTime.Now;
                                }
                                if ((DateTime.Now - p.DrownTime).TotalSeconds > 30)
                                {
                                    p.TeleportTo(p.WorldMap.Spawn);
                                    p.World.Players.Message("{0}&S drowned and died", p.ClassyName);
                                }
                            }
                            else
                            {
                                p.DrownTime = DateTime.Now;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.SeriousError, "" + ex);
            }
        }
    }
}