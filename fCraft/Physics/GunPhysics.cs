using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Collections;

namespace fCraft
{
    public class Bullet : PhysicsTask
    {
        private const int Delay = 50;
        private Position p;
        private Block _type = Block.Undefined;
        private double rSin;
        private double rCos;
        private double lCos;
        private Position nextPos;
        private short startX;
        private short startY;
        private short startZ;
        private int startB = 4;
        private bool hit = false;
        private Player _sender;
        fCraft.Collections.ConcurrentDictionary<String, Vector3I> bullets = new fCraft.Collections.ConcurrentDictionary<String, Vector3I>();
        public Bullet(World world, Vector3I startPos, Position playerPos, Player sender)
            : base(world)
        {
            p = playerPos;
            _sender = sender;
            startX = (short)startPos.X;
            startY = (short)startPos.Y;
            startZ = (short)startPos.Z;
            nextPos.R = playerPos.R;
            nextPos.L = playerPos.L;
        }

        protected override int PerformInternal()
        {
            lock (_world.SyncRoot)
            {
                removal(bullets, _world.Map);
                if (hit)
                {
                    return 0;
                }
                if (_type == Block.Undefined)
                {
                    _type = Block.Admincrete;
                    if (_sender.LastUsedBlockType == Block.Orange)
                        _type = Block.Lava;
                    if (_sender.LastUsedBlockType == Block.Blue)
                        _type = Block.Water;
                    if (_sender.LastUsedBlockType == Block.TNT)
                        _type = Block.TNT;
                }
                hit = false;
                rSin = Math.Sin(((double)(128 - p.R) / 255) * 2 * Math.PI);
                rCos = Math.Cos(((double)(128 - p.R) / 255) * 2 * Math.PI);
                lCos = Math.Cos(((double)(p.L + 64) / 255) * 2 * Math.PI);
                nextPos.X = (short)Math.Round((startX + (double)(rSin * startB))); //math.round improves accuracy
                nextPos.Y = (short)Math.Round((startY + (double)(rCos * startB)));
                nextPos.Z = (short)Math.Round((startZ + (double)(lCos * startB)));
                if (_world.Map.GetBlock(nextPos.X, nextPos.Y, nextPos.Z) == Block.Air || _world.Map.GetBlock(nextPos.X, nextPos.Y, nextPos.Z) == _type)
                {
                    startB++;
                    bullets.TryAdd(new Vector3I(nextPos.X, nextPos.Y, nextPos.Z).ToString(), new Vector3I(nextPos.X, nextPos.Y, nextPos.Z));
                    _world.Map.QueueUpdate(new BlockUpdate(null,
                        (short)nextPos.X,
                        (short)nextPos.Y,
                        (short)nextPos.Z,
                        _type));
                    foreach (Player player in _world.Players)
                    {
                        if ((player.Position.X / 32) == nextPos.X || (player.Position.X / 32 + 1) == nextPos.X || (player.Position.X / 32 - 1) == nextPos.X)
                        {
                            if ((player.Position.Y / 32) == nextPos.Y || (player.Position.Y / 32 + 1) == nextPos.Y || (player.Position.Y / 32 - 1) == nextPos.Y)
                            {
                                if ((player.Position.Z / 32) == nextPos.Z || (player.Position.Z / 32 + 1) == nextPos.Z || (player.Position.Z / 32 - 1) == nextPos.Z)
                                {
                                    if (_world.tntPhysics && _type == Block.TNT)
                                    {
                                        if (player.LastTimeKilled == null || (DateTime.Now - player.LastTimeKilled).TotalSeconds > 15)
                                        {
                                            int seed = new Random().Next(1, 6);
                                            player.LastTimeKilled = DateTime.Now;
                                            _world._physScheduler.AddTask(new TNTTask(_world, new Vector3I(nextPos.X, nextPos.Y, nextPos.Z), _sender), 0);
                                            player.TeleportTo(_world.Map.Spawn);
                                            _world.Players.Message("{0}&S was blown up by {1}", player.ClassyName, _sender.ClassyName);
                                            removal(bullets, _world.Map);
                                            hit = true;
                                        }
                                    }

                                    else
                                    {
                                        if (player.LastTimeKilled == null || (DateTime.Now - player.LastTimeKilled).TotalSeconds > 15)
                                        {
                                            player.LastTimeKilled = DateTime.Now;
                                            _world.Players.Message("{0}&S was shot by {1}", player.ClassyName, _sender.ClassyName);
                                            player.TeleportTo(_world.Map.Spawn);
                                            removal(bullets, _world.Map);
                                            hit = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    //tnt
                    if (_world.tntPhysics && _type == Block.TNT)
                    {
                        int seed = new Random().Next(1, 6);
                        _world._physScheduler.AddTask(new TNTTask(_world, new Vector3I(nextPos.X, nextPos.Y, nextPos.Z), _sender), 0);
                        removal(bullets, _world.Map);
                        hit = true;
                    }
                    if (_sender.bluePortal.Count > 0)
                    {
                        if (new Vector3I(nextPos.X, nextPos.Y, nextPos.Z) == _sender.bluePortal[0] ||
                            new Vector3I(nextPos.X, nextPos.Y, nextPos.Z) == _sender.bluePortal[1])
                        {
                            return 0;
                        }
                    }
                    if (_sender.orangePortal.Count > 0)
                    {
                        if (new Vector3I(nextPos.X, nextPos.Y, nextPos.Z) == _sender.orangePortal[0] ||
                            new Vector3I(nextPos.X, nextPos.Y, nextPos.Z) == _sender.orangePortal[1])
                        {
                            return 0;
                        }
                    }
                    //blue portal
                    if (_type == Block.Water)
                    {
                        if (CanPlacePortal(nextPos.X, nextPos.Y, nextPos.Z, _world.Map))
                        {
                            if (_sender.bluePortal.Count > 0)
                            {
                                int i = 0;
                                foreach (Vector3I block in _sender.bluePortal)
                                {
                                    _world.Map.QueueUpdate(new BlockUpdate(null, block, _sender.blueOld[i]));
                                    i++;
                                }
                                _sender.blueOld.Clear();
                                _sender.bluePortal.Clear();
                            }

                            _sender.blueOld.Add(_world.Map.GetBlock(nextPos.X, nextPos.Y, nextPos.Z));
                            _sender.blueOld.Add(_world.Map.GetBlock(nextPos.X, nextPos.Y, nextPos.Z + 1));
                            _sender.orangeOut = nextPos.R;
                            for (double z = nextPos.Z; z < nextPos.Z + 2; z++)
                            {
                                _world.Map.QueueUpdate(new BlockUpdate(null, (short)(nextPos.X), (short)(nextPos.Y), (short)z, Block.Water));
                                _sender.bluePortal.Add(new Vector3I((int)nextPos.X, (int)nextPos.Y, (int)z));
                            }
                            return 0;
                        }
                    }

                        //orange portal
                    else if (_type == Block.Lava)
                    {
                        if (CanPlacePortal(nextPos.X, nextPos.Y, nextPos.Z, _world.Map))
                        {
                            if (_sender.orangePortal.Count > 0)
                            {
                                int i = 0;
                                foreach (Vector3I block in _sender.orangePortal)
                                {
                                    _world.Map.QueueUpdate(new BlockUpdate(null, block, _sender.orangeOld[i]));
                                    i++;
                                }
                                _sender.orangeOld.Clear();
                                _sender.orangePortal.Clear();
                            }
                            _sender.orangeOld.Add(_world.Map.GetBlock(nextPos.X, nextPos.Y, nextPos.Z));
                            _sender.orangeOld.Add(_world.Map.GetBlock(nextPos.X, nextPos.Y, nextPos.Z + 1));
                            _sender.blueOut = nextPos.R;
                            for (double z = nextPos.Z; z < nextPos.Z + 2; z++)
                            {
                                _world.Map.QueueUpdate(new BlockUpdate(null, (short)(nextPos.X), (short)(nextPos.Y), (short)z, Block.Lava));
                                _sender.orangePortal.Add(new Vector3I((int)nextPos.X, (int)nextPos.Y, (int)z));
                            }
                            return 0;
                        }
                    }
                    else
                    {
                        return 0;
                    }
                }
                return Delay;
            }
        }
        public static bool CanPlacePortal(short x, short y, short z, Map map)
        {
            int Count = 0;
            for (short Z = z; Z < z + 2; Z++)
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

        public static Vector3F FromAngle(float rotationAngle, float axisAngle, float r)
        {
            float f = (float)Math.Sin(axisAngle);
            return new Vector3F(
                (float)Math.Cos(rotationAngle) * f * r,
                (float)Math.Sin(rotationAngle) * f * r,
                (float)Math.Cos(axisAngle) * r
                );
        }
        public void removal(fCraft.Collections.ConcurrentDictionary<String, Vector3I> bullets, Map map)
        {
            if (bullets.Values.Count > 0)
            {
                foreach (Vector3I bp in bullets.Values)
                {
                    map.QueueUpdate(new BlockUpdate(null,
                        (short)bp.X,
                        (short)bp.Y,
                        (short)bp.Z,
                        Block.Air));
                    Vector3I removed;
                    bullets.TryRemove(bp.ToString(), out removed);
                }
            }
        }
    }
}
