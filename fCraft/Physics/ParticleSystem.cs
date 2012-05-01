using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft;

namespace fCraft
{
    public interface IParticleBehavior
    {
        int ProcessingStepsPerSecond { get; }
        int MaxDistance { get; }
        int MovesPerProcessingStep { get; }

        void ModifyDirection(ref Vector3F direction, Block currentBlock /*etc*/);
        bool VisitBlock(World world, Vector3I pos, Block block, Player owner, ref int restDistance, IList<BlockUpdate> updates);
        bool CanKillPlayer { get; }
        void HitPlayer(World world, Player hitted, Player by, ref int restDistance);
    }

    public class FireworkParticle : PhysicsTask
    {
        private Vector3I _startingPos;
        private int _nextZ;
        private Block _block;
        private bool _first = true;
        private int _maxFall = new Random().Next(2, 4);
        private int Count = 0;

        public FireworkParticle(World world, Vector3I pos, Block block)
            : base(world)
        {
            _startingPos = pos;
            _nextZ = pos.Z - 1;
            _block = block;
        }

        protected override int PerformInternal()
        {
            Random _rand = new Random();
            if (_first){
                if (_world.Map.GetBlock(_startingPos.X, _startingPos.Y, _nextZ) != Block.Air || 
                    Count > _maxFall){
                    return 0;
                }
                _world.Map.QueueUpdate(new BlockUpdate(null, (short)_startingPos.X, (short)_startingPos.Y, (short)_nextZ, _block));
                _first = false;
                return _rand.Next(100, 401);
            }
            _world.Map.QueueUpdate(new BlockUpdate(null, (short)_startingPos.X, (short)_startingPos.Y, (short)_nextZ, Block.Air));
            Count++;
            _nextZ--;
            if (_world.Map.GetBlock(_startingPos.X, _startingPos.Y, _nextZ) != Block.Air || 
                Count > _maxFall){
                return 0;
            } 
            _world.Map.QueueUpdate(new BlockUpdate(null, (short)_startingPos.X, (short)_startingPos.Y, (short)_nextZ, _block));
            return _rand.Next(100, 401);
        }


    }

    public class Particle : PhysicsTask
    {
        private int _stepDelay;
        private Vector3I _startingPos;
        private Vector3I _pos;
        private Vector3F _direction;
        private Player _owner;
        private Block _block;
        private Block _prevBlock;
        private int _restDistance;
        private int _currentStep;
        private IParticleBehavior _behavior;

        /// <summary>
        /// Initializes a new instance of the <see cref="Particle"/> class.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="pos">The initial position.</param>
        /// <param name="direction">The direction in which the particle is flying.</param>
        /// <param name="owner">The owner of the particle.</param>
        /// <param name="block">The block type of the particle.</param>
        /// <param name="behavior">The partcle behavior. Includes how fare and fast it moves, what happens if it hits a player an obstacle etc.</param>
        public Particle(World world, Vector3I pos, Vector3F direction, Player owner,
            Block block, IParticleBehavior behavior)
            : base(world)
        {
            _direction = direction.Normalize();
            if (_direction == Vector3F.Zero)
                throw new ArgumentException("direction vector cannot be zero");

            _stepDelay = behavior.ProcessingStepsPerSecond <= 0 || behavior.ProcessingStepsPerSecond > 20
                            ? 50
                            : 1000 / behavior.ProcessingStepsPerSecond;

            _pos = pos;
            _startingPos = pos;
            _currentStep = 0;
            _owner = owner;
            _block = block;
            _restDistance = behavior.MaxDistance;
            _behavior = behavior;
            lock (_world.SyncRoot)
            {
                //draw it once right now
                if (null != _map && ReferenceEquals(_map, _world.Map))
                {
                    _prevBlock = _map.GetBlock(pos);
                    if (Block.Undefined != _prevBlock) //out of bounds!
                        UpdateMap(new BlockUpdate(null, pos, block));
                }
            }
        }

        protected override int PerformInternal()
        {
            //lock is done already
            if (Block.Undefined == _prevBlock) //created out of bounds, dont continue
                return 0;


            //delete at the previous position, restore water unconditionally, lava only when bullet is still there to prevent restoration of explosion lava
            if (_prevBlock != Block.Water)
                _prevBlock = _map.GetBlock(_pos) == _block ? //is the bullet still here
                    (_prevBlock == Block.Lava ? Block.Lava : Block.Air)  //yes, then either restore lava or set air
                    : Block.Undefined; //no, it was removed by some other process, do nothing then

            if (Block.Undefined != _prevBlock)
                UpdateMap(new BlockUpdate(null, _pos, _prevBlock));

            List<BlockUpdate> updates = new List<BlockUpdate>();
            for (int i = 0; i < _behavior.MovesPerProcessingStep && _restDistance > 0; ++i)
            {
                _pos = Move();
                _prevBlock = _map.GetBlock(_pos);

                if (Block.Undefined == _prevBlock)
                {
                    _restDistance = 0;
                    break;
                }

                _behavior.ModifyDirection(ref _direction, _prevBlock); //e.g. if you want it to be dependent on gravity or change direction depending on current block etc

                if (_behavior.VisitBlock(_world, _pos, _prevBlock, _owner, ref _restDistance, updates) && _behavior.CanKillPlayer)
                    CheckHitPlayers();
            }

            bool cont = _restDistance > 0;
            if (cont) //check if the last update was for the current position and replace it with the particle
            {
                int idx = updates.Count - 1;
                if (idx >= 0 && updates[idx].X == _pos.X
                    && updates[idx].Y == _pos.Y
                    && updates[idx].Z == _pos.Z)
                {
                    updates[idx] = new BlockUpdate(null, _pos, _block);
                }
                else
					updates.Add(new BlockUpdate(null, _pos, _block));
            }

            for (int i = 0; i < updates.Count; ++i)
                UpdateMap(updates[i]);

            return cont ? _stepDelay : 0;
        }

        private Vector3I Move()
        {
            ++_currentStep;
            --_restDistance;
            return (_startingPos + _currentStep * _direction).Round();
        }

        private void CheckHitPlayers()
        {
            foreach (Player p in _world.Players)
            {
                if (p.CanBeKilled() && p.Position.DistanceSquaredTo(_pos.ToPlayerCoords()) <= 33 * 33) //less or equal than a block
                    _behavior.HitPlayer(_world, p, _owner, ref _restDistance);
            }
        }
    }

    internal class ExplosionParticleBehavior : IParticleBehavior
    {
		private static Random _r=new Random();

        public int ProcessingStepsPerSecond
        {
            get { return 15; }
        }

        public int MaxDistance
        {
            get { return _r.Next(6, 10); }
        }

        public int MovesPerProcessingStep
        {
            get { return 1; }
        }

        public void ModifyDirection(ref Vector3F direction, Block currentBlock)
        {

        }

        public bool VisitBlock(World world, Vector3I pos, Block block, Player owner, ref int restDistance, IList<BlockUpdate> updates)
        {
            if (Block.TNT == block) //explode it
            {
                world.AddTask(new TNTTask(world, pos, owner, false), 0);
                return true;
            }
            if (Block.Air != block && Block.Water != block && Block.Lava != block)
                updates.Add(new BlockUpdate(null, pos, Block.Air));
            return true;
        }

        public bool CanKillPlayer
        {
            get { return true; }
        }

        public void HitPlayer(World world, Player hitted, Player by, ref int restDistance)
        {
			hitted.Kill(world, String.Format("{0}&S was blown up by {1}", hitted.ClassyName, hitted.ClassyName == by.ClassyName ? "self" : by.ClassyName));
        }
    }
}

