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
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using fCraft.Events;
using Util = RandomMaze.MazeUtil;

namespace fCraft
{
    public class TNTTask : PhysicsTask
    {
        private struct BData
        {
            public int X, Y, Z;
            public Block PrevBlock;
        }
        public const int ExplosionDelay = 3000;
        private const int StepDelay = 80;
        private static ExplosionParticleBehavior _particleBehavior = new ExplosionParticleBehavior();

        private const int R = 5;

        private Vector3I _pos; //tnt position
        private Player _owner;
        private bool _gun;

        private Random _r = new Random();
        private List<BData> _explosion;

        private enum Stage
        {
            Waiting,
            Exploding,
        }

        private Stage _stage;
        private int _currentR = 0;
        private bool _particles;

        public TNTTask(World world, Vector3I position, Player owner, bool gun, bool particles)
            : base(world)
        {
            _particles = particles;
            _gun = gun;
            _pos = position;
            _owner = owner;
            _stage = Stage.Waiting;
        }

        protected override int PerformInternal()
        {
            lock (_world.SyncRoot)
            {
                switch (_stage)
                {
                    case Stage.Waiting:
                        if (!_gun)
                        {
                            if (!_world.tntPhysics)// || _map.GetBlock(_pos) != Block.TNT) //TNT was removed for some reason, forget the xplosion
                                return 0; //remove task
                        }

                        UpdateMap(new BlockUpdate(null, _pos, Block.Air));

                        _stage = Stage.Exploding; //switch to expansion stage
                        if (_particles)
                        {
                            CreateParticles();
                        }
                        Explosion();
                        return StepDelay;
                    case Stage.Exploding:
                        return Explosion() ? StepDelay : 0;//when done remove task
                    default:
                        throw new Exception("state machine ist kaputt! tnt panic!");
                }
            }
        }

        private void CreateParticles()
        {
            int n = _r.Next(2, 5);
            for (int i = 0; i < n; ++i)
            {
                double phi = _r.NextDouble() * 2 * Math.PI;
                double ksi = _r.NextDouble() * Math.PI - Math.PI / 2.0;

                Vector3F direction = (new Vector3F((float)(Math.Cos(phi) * Math.Cos(ksi)), (float)(Math.Sin(phi) * Math.Cos(ksi)), (float)Math.Sin(ksi))).Normalize();
                _world.AddPhysicsTask(new Particle(_world, (_pos + 2 * direction).Round(), direction, _owner, Block.Obsidian, _particleBehavior), 0);
            }
        }

        private bool Explosion()
        {
            List<BData> toClean = _explosion;

            if (++_currentR <= R)
            {
                _explosion = new List<BData>();
				int rPrev = 0;
				for (int z = _currentR; z >= 0; --z)
                {
                    double r2 = _currentR * _currentR - z * z;
                	double r = Math.Sqrt(r2);
                	double yPrev = r;
                    for (double x = 0; x <= Math.Round(r); ++x)
                    {
						if (x > r)
							x = r;
                        double y = Math.Sqrt(r2 - x * x);
                    	int ix = (int)Math.Round(x);
						for (int iy = ix>rPrev?0:(int)Math.Round(y); iy < Math.Max(Math.Round(y) + 1, Math.Round(yPrev)); ++iy )
							for (int mx = -1; mx < 2; mx += 2)
								for (int my = -1; my < 2; my += 2)
									for (int mz = -1; mz < 2; mz += 2)
										TryAddPoint(mx * ix + _pos.X, my * iy + _pos.Y, mz * z + _pos.Z);
                    	yPrev = y;
                    }
                	rPrev = (int) Math.Round(r);
                }

                Util.RndPermutate(_explosion);
                foreach (BData pt in _explosion)
                {
                    UpdateMap(new BlockUpdate(null, (short)pt.X, (short)pt.Y, (short)pt.Z, Block.Lava));
                    foreach (Player p in _world.Players)
                    {
                        if (p.CanBeKilled() && p.Position.DistanceSquaredTo((new Vector3I(pt.X, pt.Y, pt.Z)).ToPlayerCoords()) <= 64 * 64) //less or equal than 2 blocks
                            HitPlayer(_world, p, _owner);
                    }
                }
            }

            if (null != toClean)
            {
                foreach (BData pt in toClean)
                {
                    UpdateMap(new BlockUpdate(null, (short)pt.X, (short)pt.Y, (short)pt.Z,
                        pt.PrevBlock == Block.Water ? Block.Water : Block.Air));
                }
            }

            return _currentR <= R;
        }

        private void TryAddPoint(int x, int y, int z)
        {
            if (x < 0 || x >= _map.Width
                || y < 0 || y >= _map.Length
                || z < 0 || z >= _map.Height)
                return;

            Block prevBlock = _map.GetBlock(x, y, z);
			if (Block.Lava == prevBlock)
				return;

            //chain explosion
            if (Block.TNT == prevBlock)
            {
                _world.AddPhysicsTask(new TNTTask(_world, new Vector3I(x, y, z), _owner, false, _particles), _r.Next(150, 300));
				UpdateMap(new BlockUpdate(null, (short)x, (short)y, (short)z, Block.Air));
            }

			if (0.2 + 0.75 * (R - _currentR) / R > _r.NextDouble())
				_explosion.Add(new BData() {X = x, Y = y, Z = z, PrevBlock = _map.GetBlock(x, y, z)});
        }

        public void HitPlayer(World world, Player hitted, Player by)
        {
            if (by == null)
            {
                if (MineField.Failed != null && !MineField.Failed.Contains(hitted))
                {
                    hitted.Kill(world, String.Format("{0}&S was torn to pieces and lost the game!", hitted.ClassyName));
                    if (MineField.PlayerBlowUpCheck(hitted))
                    {
                        hitted.Message("&WYou lost the game! You are now unable to win.");
                    }
                    return;
                }
                else return;
            }
			hitted.Kill(world, String.Format("{0}&S was torn to pieces by {1}", hitted.ClassyName, hitted.ClassyName==by.ClassyName?"theirself":by.ClassyName));
        }
    }

    public class Firework : PhysicsTask
    {
        private const int Delay = 150;
        private Vector3I _pos;
        private int _z;
        private bool _notSent = true;
        private int _height = new Random().Next(13, 20);
        private int _count = 0;

        public Firework(World world, Vector3I position)
            : base(world)
        {
            _pos = position;
            _z = position.Z + 1;
        }
        protected override int PerformInternal()
        {
            lock (_world.SyncRoot)
            {
                if (_world.fireworkPhysics)
                {
                    if (_world.Map.GetBlock(_pos.X, _pos.Y, _z) != Block.Air || _count >= _height)
                    {
                        _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)(_z - 1), Block.Air));
                        if (_world.Map.GetBlock(_pos.X, _pos.Y, _z - 2) == Block.Lava)
                        {
                            _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)(_z - 2), Block.Air));
                        }

                        Random rand = new Random();
                        int blockId = new Random().Next(1, 9);
                        Block fBlock = new Block();
                        if (blockId == 1)
                        {
                            fBlock = Block.Lava;
                        }
                        if (blockId <= 6 && blockId != 1)
                        {
                            fBlock = (Block)rand.Next(21, 33);
                        }
                        for (int X2 = _pos.X - 5; X2 <= _pos.X + 5; X2++)
                        {
                            for (int Y2 = (_pos.Y - 5); Y2 <= (_pos.Y + 5); Y2++)
                            {
                                for (int Z2 = (_z - 5); Z2 <= (_z + 5); Z2++)
                                {
                                    if (blockId >= 7)
                                    {
                                        fBlock = (Block)rand.Next(21, 33);
                                    }
                                    if (rand.Next(1, 50) < 3)
                                    {
                                        _world.AddPhysicsTask(new FireworkParticle(_world, new Vector3I(X2, Y2, Z2), fBlock), rand.Next(1, 100));
                                    }
                                }
                            }
                        }
                        _world.FireworkCount--;
                        return 0;
                    }
                    if (_notSent)
                    {
                        if (_world.Map.GetBlock(_pos) != Block.Gold)
                            return 0;
                    }
                    _notSent = false;
                    _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)_z, Block.Gold));
                    _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)(_z - 1), Block.Lava));
                    if (_world.Map.GetBlock(_pos.X, _pos.Y, _z - 2) == Block.Lava)
                    {
                        _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)(_z - 2), Block.Air));
                    }
                    _z++; _count++;
                    return Delay;
                }
            }
            return 0;
        }
    }
}