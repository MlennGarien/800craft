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

		//false if stopped
        bool VisitBlock(World world, Vector3I pos, Block block, Player owner, ref int restDistance, IList<BlockUpdate> updates, Block Sending);
        bool CanKillPlayer { get; }
		void HitPlayer(World world, Vector3I pos, Player hitted, Player by, ref int restDistance, IList<BlockUpdate> updates);
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
            //fix for portal gun
            if (_owner.orangePortal.Count > 0){
                if (_pos == _owner.orangePortal[0] || _pos == _owner.orangePortal[1])
                    return 0;
            }if (_owner.bluePortal.Count > 0){
                if (_pos == _owner.bluePortal[0] || _pos == _owner.bluePortal[1])
                    return 0;
            }

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

                if (_behavior.VisitBlock(_world, _pos, _prevBlock, _owner, ref _restDistance, updates, _block) && _behavior.CanKillPlayer)
                    CheckHitPlayers(updates);
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

        private void CheckHitPlayers(List<BlockUpdate> updates)
        {
            foreach (Player p in _world.Players)
            {
                if (ReferenceEquals(p, _owner) && (_startingPos - _pos).LengthSquared <= 2 * 2) //do not react on owner within 2 blocks of the starting position
                    continue;
                if (p.CanBeKilled() && p.Position.DistanceSquaredTo(_pos.ToPlayerCoords()) <= 33 * 33) //less or equal than a block
                    _behavior.HitPlayer(_world, _pos, p, _owner, ref _restDistance, updates);
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

		//true if not stopped
        public bool VisitBlock(World world, Vector3I pos, Block block, Player owner, ref int restDistance, IList<BlockUpdate> updates, Block sending)
        {
			if (Block.TNT == block) //explode it
			{
				world.AddPhysicsTask(new TNTTask(world, pos, owner, false, true), _r.Next(150, 300));
			}
			if (Block.Air != block && Block.Water != block && Block.Lava != block)
				updates.Add(new BlockUpdate(null, pos, Block.Air));
			return true;
        }

        public bool CanKillPlayer
        {
            get { return true; }
        }

		public void HitPlayer(World world, Vector3I pos, Player hitted, Player by, ref int restDistance, IList<BlockUpdate> updates)
        {
			hitted.Kill(world, String.Format("{0}&S was blown up by {1}", hitted.ClassyName, hitted.ClassyName == by.ClassyName ? "theirself" : by.ClassyName));
        }
    }

	internal class BulletBehavior : IParticleBehavior
	{

		public int ProcessingStepsPerSecond
		{
			get { return 20; }
		}

		public int MaxDistance
		{
			get { return 100; }
		}

		public int MovesPerProcessingStep
		{
			get { return 1; }
		}


		private const double G = 10; //blocks per second per second
		public void ModifyDirection(ref Vector3F direction, Block currentBlock)
		{

		}
        public bool VisitBlock(World world, Vector3I pos, Block block, Player owner, ref int restDistance, IList<BlockUpdate> updates, Block sending)
        {
            if (Block.Air != block) //hit a building
            {
                if (owner.bluePortal.Count > 0)
                {
                    if (pos == owner.bluePortal[0] || pos == owner.bluePortal[1])
                    {
                        return false;
                    }
                }
                if (owner.orangePortal.Count > 0)
                {
                    if (pos == owner.orangePortal[0] || pos == owner.orangePortal[1])
                    {
                        return false;
                    }
                }
                //blue portal
                if (sending == Block.Water)
                {
                    if (CanPlacePortal(pos.X, pos.Y, pos.Z, world.Map))
                    {
                        if (owner.bluePortal.Count > 0)
                        {
                            int i = 0;
                            foreach (Vector3I b in owner.bluePortal)
                            {
                                world.Map.QueueUpdate(new BlockUpdate(null, b, owner.blueOld[i]));
                                i++;
                            }
                            owner.blueOld.Clear();
                            owner.bluePortal.Clear();
                        }

                        owner.blueOld.Add(world.Map.GetBlock(pos));
                        owner.blueOld.Add(world.Map.GetBlock(pos.X, pos.Y, pos.Z + 1));
                        owner.orangeOut = owner.Position.R;
                        for (double z = pos.Z; z < pos.Z + 2; z++)
                        {
                            world.Map.QueueUpdate(new BlockUpdate(null, (short)(pos.X), (short)(pos.Y), (short)z, Block.Water));
                            owner.bluePortal.Add(new Vector3I((int)pos.X, (int)pos.Y, (int)z));
                        }
                        return false;
                    }
                    else
                    {
                        return false;
                    }
                }

                    //orange portal
                else if (sending == Block.Lava)
                {
                    if (CanPlacePortal(pos.X, pos.Y, pos.Z, world.Map))
                    {
                        if (owner.orangePortal.Count > 0)
                        {
                            int i = 0;
                            foreach (Vector3I b in owner.orangePortal)
                            {
                                world.Map.QueueUpdate(new BlockUpdate(null, b, owner.orangeOld[i]));
                                i++;
                            }
                            owner.orangeOld.Clear();
                            owner.orangePortal.Clear();
                        }
                        owner.orangeOld.Add(world.Map.GetBlock(pos));
                        owner.orangeOld.Add(world.Map.GetBlock(pos.X, pos.Y, pos.Z + 1));
                        owner.blueOut = owner.Position.R;
                        for (double z = pos.Z; z < pos.Z + 2; z++)
                        {
                            world.Map.QueueUpdate(new BlockUpdate(null, (short)(pos.X), (short)(pos.Y), (short)z, Block.Lava));
                            owner.orangePortal.Add(new Vector3I((int)pos.X, (int)pos.Y, (int)z));
                        }
                        return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                updates.Add(new BlockUpdate(null, pos, world.Map.GetBlock(pos))); //restore
                restDistance = 0;
                return false;
            }
            return true;
        }
        

    public static bool CanPlacePortal(int x, int y, int z, Map map)
        {
            int Count = 0;
            for (int Z = z; Z < z + 2; Z++)
            {
                Block check = map.GetBlock(x, y, Z);
                if (check != Block.Air && check != Block.Water && check != Block.Lava)
                {
                    Count++;
                }
            }
            if (Count == 2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

		public bool CanKillPlayer
		{
			get { return true; }
		}

		public void HitPlayer(World world, Vector3I pos, Player hitted, Player by, ref int restDistance, IList<BlockUpdate> updates)
		{
            hitted.Kill(world, String.Format("{0}&S was shot by {1}", hitted.ClassyName, hitted.ClassyName == by.ClassyName ? "theirself" : by.ClassyName));
			updates.Add(new BlockUpdate(null, pos, Block.Air));
			restDistance = 0;
		}
	}

    internal class SpellStartBehavior : IParticleBehavior
    {
        private static Random _r = new Random();

        public int ProcessingStepsPerSecond
        {
            get { return 10; }
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
        public bool VisitBlock(World world, Vector3I pos, Block block, Player owner, ref int restDistance, IList<BlockUpdate> updates, Block sending)
        {
            if (Block.Air != block && Block.Water != block && Block.Lava != block)
            {
                updates.Add(new BlockUpdate(null, pos, Block.Air));
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool CanKillPlayer
        {
            get { return false; }
        }

        public void HitPlayer(World world, Vector3I pos, Player hitted, Player by, ref int restDistance, IList<BlockUpdate> updates)
        {
            
        }
    }

    internal class TntBulletBehavior : IParticleBehavior
    {
        private static Random _r = new Random();

        public int ProcessingStepsPerSecond
        {
            get { return 20; }
        }

        public int MaxDistance
        {
            get { return 1024; }
        }

        public int MovesPerProcessingStep
        {
            get { return 1; }
        }


        private const double G = 10; //blocks per second per second
        public void ModifyDirection(ref Vector3F direction, Block currentBlock)
        {
            double t = 1.0 / (ProcessingStepsPerSecond * MovesPerProcessingStep);
            Vector3F v = direction * (ProcessingStepsPerSecond * MovesPerProcessingStep);
            Vector3F dv = new Vector3F(0, 0, (float)(-G * t));
            direction = (v + dv).Normalize();
        }

        public bool VisitBlock(World world, Vector3I pos, Block block, Player owner, ref int restDistance, IList<BlockUpdate> updates, Block sending)
        {
            if (Block.Air != block && Block.Water != block) //explode it
            {
                updates.Add(new BlockUpdate(null, pos, Block.TNT));
                world.AddPhysicsTask(new TNTTask(world, pos, owner, false, true), 0);
                restDistance = 0;
                return false;
            }
            return true;
        }

        public bool CanKillPlayer
        {
            get { return true; }
        }

        public void HitPlayer(World world, Vector3I pos, Player hitted, Player by, ref int restDistance, IList<BlockUpdate> updates)
        {
            hitted.Kill(world, String.Format("{0}&S was torn to pieces by {1}", hitted.ClassyName, hitted.ClassyName == by.ClassyName ? "theirself" : by.ClassyName));
            updates.Add(new BlockUpdate(null, pos, Block.TNT));
            world.AddPhysicsTask(new TNTTask(world, pos, by, false, true), 0);
            restDistance = 0;
        }
    }

    internal class FootballBehavior : IParticleBehavior
    {
        private static Random _r = new Random();

        public int ProcessingStepsPerSecond
        {
            get { return 20; }
        }

        public int MaxDistance
        {
            get { return 1024; } //check what happens at max
        }

        public int MovesPerProcessingStep
        {
            get { return 1; }
        }


        private const double G = 10; //blocks per second per second
        public void ModifyDirection(ref Vector3F direction, Block currentBlock) //change this
        {
            double t = 1.0 / (ProcessingStepsPerSecond * MovesPerProcessingStep);
            Vector3F v = direction * (ProcessingStepsPerSecond * MovesPerProcessingStep);
            Vector3F dv = new Vector3F(0, 0, (float)(-G * t));
            direction = (v + dv).Normalize();
        }

        public bool VisitBlock(World world, Vector3I pos, Block block, Player owner, ref int restDistance, IList<BlockUpdate> updates, Block sending)
        {
            //add some bounce and stuff in here
            if (Block.Air != block && Block.Water != block) //explode it
            {
                restDistance = 0;
                return false;
            }
            return true;
        }

        public bool CanKillPlayer
        {
            get { return false; }
        }

        public void HitPlayer(World world, Vector3I pos, Player hitted, Player by, ref int restDistance, IList<BlockUpdate> updates)
        {
            //if it hits a player, move at Delay to the floor in front of the player
            for (int z = pos.Z; z > 0; z--)
            {
                if (world.Map.GetBlock(pos.X, pos.Y, z) != Block.Air)
                {

                }
            }
        }
    }
}

