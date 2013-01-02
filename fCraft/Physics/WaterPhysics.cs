//Copyright (C) <2011 - 2013>  <Jon Baker, Glenn Mariën and Lao Tszy>

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
                                if (p.DrownTime == null || (DateTime.UtcNow - p.DrownTime).TotalSeconds > 33)
                                {
                                    p.DrownTime = DateTime.UtcNow;
                                }
                                if ((DateTime.UtcNow - p.DrownTime).TotalSeconds > 30)
                                {
                                    p.TeleportTo(p.WorldMap.Spawn);
                                    p.World.Players.Message("{0}&S drowned and died", p.ClassyName);
                                }
                            }
                            else
                            {
                                p.DrownTime = DateTime.UtcNow;
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