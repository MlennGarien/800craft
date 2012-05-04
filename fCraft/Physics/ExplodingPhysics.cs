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

        public TNTTask(World world, Vector3I position, Player owner, bool gun)
            : base(world)
        {
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
                            if (!_world.tntPhysics || _map.GetBlock(_pos) != Block.TNT) //TNT was removed for some reason, forget the xplosion
                                return 0; //remove task
                        }

                        UpdateMap(new BlockUpdate(null, _pos, Block.Air));

                        _stage = Stage.Exploding; //switch to expansion stage
                        CreateParticles();
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
            int n = _r.Next(5, 8);
            for (int i = 0; i < n; ++i)
            {
                double phi = _r.NextDouble() * 2 * Math.PI;
                double ksi = _r.NextDouble() * Math.PI - Math.PI / 2.0;

                Vector3F direction = (new Vector3F((float)(Math.Cos(phi) * Math.Cos(ksi)), (float)(Math.Sin(phi) * Math.Cos(ksi)), (float)Math.Sin(ksi))).Normalize();
                _world.AddTask(new Particle(_world, (_pos + 2 * direction).Round(), direction, _owner, Block.Obsidian, _particleBehavior), 0);
            }
        }

        private bool Explosion()
        {
            List<BData> toClean = _explosion;

            if (++_currentR <= R)
            {
                _explosion = new List<BData>();
                for (int z = 0; z <= _currentR; ++z)
                {
                    double r2 = _currentR * _currentR - z * z;
                    for (int x = 0; x <= _currentR; ++x)
                    {
                        int y = (int)Math.Round(Math.Sqrt(r2 - x * x));
                        for (int mx = -1; mx < 2; mx += 2)
                            for (int my = -1; my < 2; my += 2)
                                for (int mz = -1; mz < 2; mz += 2)
                                    TryAddPoint(mx * x + _pos.X, my * y + _pos.Y, mz * z + _pos.Z);
                    }
                }

                Util.RndPermutate(_explosion);
                _explosion.ForEach(delegate(BData pt)
                {
                    UpdateMap(new BlockUpdate(null, (short)pt.X, (short)pt.Y, (short)pt.Z, Block.Lava));
                    foreach (Player p in _world.Players)
                    {
                        if (p.CanBeKilled() && p.Position.DistanceSquaredTo((new Vector3I(pt.X, pt.Y, pt.Z)).ToPlayerCoords()) <= 64 * 64) //less or equal than 2 blocks
                            HitPlayer(_world, p, _owner);
                    }
                });
            }

            if (null != toClean)
            {
                toClean.ForEach(delegate(BData pt)
                {
                    UpdateMap(new BlockUpdate(null, (short)pt.X, (short)pt.Y, (short)pt.Z,
                        pt.PrevBlock == Block.Water ? Block.Water : Block.Air));
                });
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
            
            //chain explosion
            if (Block.TNT == prevBlock)
            {
                _world.AddTask(new TNTTask(_world, new Vector3I(x, y, z), _owner, false), _r.Next(50, 100));
                return;
            }

            if (0.45 + 0.55 * (R - _currentR) / R < _r.NextDouble())
                return;

            _explosion.Add(new BData() { X = x, Y = y, Z = z, PrevBlock = _map.GetBlock(x, y, z) });
        }

        public void HitPlayer(World world, Player hitted, Player by)
        {
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
                                        _world._physScheduler.AddTask(new FireworkParticle(_world, new Vector3I(X2, Y2, Z2), fBlock), rand.Next(1, 100));
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