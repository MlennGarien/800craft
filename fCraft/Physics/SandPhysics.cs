using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;

namespace fCraft
{
    public class SandTask : PhysicsTask
    {
        private const int Delay = 200;
        private Vector3I _pos;
        private int _nextPos;
        private bool _firstMove = true;
        private Block _type;
        public SandTask(World world, Vector3I position, Block Type)
            : base(world)
        {
            _pos = position;
            _nextPos = position.Z - 1;
            _type = Type;
        }

        protected override int PerformInternal()
        {
            lock (_world.SyncRoot)
            {
                if (_world.sandPhysics)
                {
                    Block nblock = _world.Map.GetBlock(_pos.X, _pos.Y, _nextPos);
                    if (_firstMove)
                    {
                        if (_world.Map.GetBlock(_pos) != _type)
                        {
                            return 0;
                        }
                        if (_world.Map.GetBlock(_pos.X, _pos.Y, _nextPos) == Block.Air)
                        {
                            _world.Map.QueueUpdate(new BlockUpdate(null, _pos, Block.Air));
                            _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)_nextPos, _type));
                            _nextPos--;
                            _firstMove = false;
                            return Delay;
                        }
                    }
                    if (_world.Map.GetBlock(_pos.X, _pos.Y, _nextPos) != Block.Air)
                    {
                        return 0;
                    }
                    if (Physics.Physics.BlockThrough(nblock))
                    {
                        _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)(_nextPos + 1), Block.Air));
                        _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)_nextPos, _type));
                        _nextPos--;
                    }
                }
                return Delay;
            }
        }
    }
}
