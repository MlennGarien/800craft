using System;
using System.Collections.Generic;
using System.Text;

namespace fCraft.Physics
{
    public class BasicPhysics
    {
        public World world;
        private Queue<PhysicsBlock> updateQueue;
        private object queueLock = new object();
        public bool lavaSpongeEnabled = false;

        public BasicPhysics(World _world)
        {
            this.world = _world;
            this.updateQueue = new Queue<PhysicsBlock>();
        }

        public BasicPhysics(World _world, bool _lavaSpongeEnabled)
        {
            this.world = _world;
            this.lavaSpongeEnabled = _lavaSpongeEnabled;
            this.updateQueue = new Queue<PhysicsBlock>();
        }

        public void Update()
        {
            if (updateQueue.Count == 0)
            {
                return;
            }
            lock (queueLock)
            {
                int n = updateQueue.Count;
                for (int i = 0; i < n; i++)
                {
                    if (world.Map != null && world.IsLoaded)
                    {
                        PhysicsBlock block = updateQueue.Dequeue();
                        if (((TimeSpan)(DateTime.Now - block.startTime)).TotalMilliseconds >= PhysicsTime(block.type))
                        {
                            switch (block.type)
                            {
                                case Block.Water:
                                    GenericSpread(block.x, block.y, block.z, block.type);
                                    CheckWaterLavaCollide(block.x, block.y, block.z, block.type);
                                    break;
                                case Block.Lava:
                                    GenericSpread(block.x, block.y, block.z, block.type);
                                    CheckWaterLavaCollide(block.x, block.y, block.z, block.type);
                                    break;
                                case Block.Sponge:
                                    NewSponge(block.x, block.y, block.z);
                                    break;
                                default:
                                    if (Physics.AffectedByGravity(block.type))
                                    {
                                        SandGravelFall(block.x, block.y, block.z, block.type);
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            updateQueue.Enqueue(block);
                        }
                    }
                }
            }
        }

        public bool Queue(int x, int y, int z, Block type)
        {
            try
            {
                if (world.Map != null && world.IsLoaded)
                {
                    lock (queueLock)
                    {
                        this.updateQueue.Enqueue(new PhysicsBlock((short)x, (short)y, (short)z, type));
                    }
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static int PhysicsTime(Block type)
        {
            switch (type)
            {
                case Block.Water:
                    return 200;
                case Block.Lava:
                    return 800;
                default:
                    return 0;
            }
        }

        #region Individual Physics Handlers
        public void GenericSpread(short x, short y, short z, Block type)
        {
            if (world.Map.GetBlock(x, y, z) != type)
            {
                return;
            }
            if (world.Map.GetBlock(x + 1, y, z) == Block.Air)
            {
                world.Map.QueueUpdate(new BlockUpdate(null, (short)(x + 1), y, z, type));
            }
            if (world.Map.GetBlock(x - 1, y, z) == Block.Air)
            {
                world.Map.QueueUpdate(new BlockUpdate(null, (short)(x - 1), y, z, type));
            }
            if (world.Map.GetBlock(x, y - 1, z) == Block.Air)
            {
                world.Map.QueueUpdate(new BlockUpdate(null, x, (short)(y - 1), z, type));
            }
            if (world.Map.GetBlock(x, y + 1, z) == Block.Air)
            {
                world.Map.QueueUpdate(new BlockUpdate(null, x, (short)(y + 1), z, type));
            }
            if (world.Map.GetBlock(x, y, z - 1) == Block.Air)
            {
                world.Map.QueueUpdate(new BlockUpdate(null, x, y, (short)(z - 1), type));
            }
        }

        public void CheckWaterLavaCollide(short x, short y, short z, Block type)
        {
            if (world.Map.GetBlock(x, y, z) != type)
            {
                return;
            }
            if (LavaWaterCollide(world.Map.GetBlock(x + 1, y, z), type))
            {
                world.Map.QueueUpdate(new BlockUpdate(null, (short)(x + 1), y, z, Block.Obsidian));
            }
            if (LavaWaterCollide(world.Map.GetBlock(x - 1, y, z), type))
            {
                world.Map.QueueUpdate(new BlockUpdate(null, (short)(x - 1), y, z, Block.Obsidian));
            }
            if (LavaWaterCollide(world.Map.GetBlock(x, y, z + 1), type))
            {
                world.Map.QueueUpdate(new BlockUpdate(null, x, (short)(y + 1), z, Block.Obsidian));
            }
            if (LavaWaterCollide(world.Map.GetBlock(x, y, z - 1), type))
            {
                world.Map.QueueUpdate(new BlockUpdate(null, x, y, (short)(z - 1), Block.Obsidian));
            }
            if (LavaWaterCollide(world.Map.GetBlock(x, y - 1, z), type))
            {
                world.Map.QueueUpdate(new BlockUpdate(null, x, (short)(y - 1), z, Block.Obsidian));
            }
            if (LavaWaterCollide(world.Map.GetBlock(x, y + 1, z), type))
            {
                world.Map.QueueUpdate(new BlockUpdate(null, x, y, (short)(z + 1), Block.Obsidian));
            }
        }

        public void NewSponge(int x, int y, int z)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        if (world.Map != null && world.IsLoaded)
                        {
                            if (Physics.AffectedBySponges(world.Map.GetBlock(x + dx, y + dy, z + dz)))
                            {
                                world.Map.QueueUpdate(new BlockUpdate(null, (short)(x + dx), (short)(y + dy), (short)(z + dz), Block.Air));
                            }
                        }
                    }
                }
            }
        }

        public bool FindSponge(int x, int y, int z)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        if (world.Map != null && world.IsLoaded)
                        {
                            if (world.Map.GetBlock(x + dx, y + dy, z + dz) == Block.Sponge)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public void DeleteSponge(int x, int y, int z)
        {
            for (int dx = -3; dx <= 3; dx++)
            {
                for (int dy = -3; dy <= 3; dy++)
                {
                    for (int dz = -3; dz <= 3; dz++)
                    {
                        if (world.Map != null && world.IsLoaded)
                        {
                            if (Physics.BasicPhysics(world.Map.GetBlock(x + dx, y + dy, z + dz)) && world.Map.GetBlock(x + dx, y + dy, z + dz) != Block.Air)
                            {
                                Queue(x + dx, y + dy, z + dz, world.Map.GetBlock(x + dx, y + dy, z + dz));
                            }
                        }
                    }
                }
            }
        }

        public void SandGravelFall(int x, int y, int z, Block type)
        {
            if (world.Map.GetBlock(x, y, z) != type)
            {
                return;
            }

            int dz = z;
            while (dz > 0 && world.Map.GetBlock(x, y, dz - 1) == Block.Air)
            {
                dz--;
            }
            if (dz != y)
            {
                world.Map.QueueUpdate(new BlockUpdate(null, (short)x, (short)y, (short)z, Block.Air));
                Physics.SetTileNoPhysics(x, y, dz, type, world);
            }
        }

        #endregion

        public bool LavaWaterCollide(Block a, Block b)
        {
            if ((a == Block.Water && b == Block.Lava) || (a == Block.Lava && b == Block.Water))
            {
                return true;
            }
            return false;
        }
    }

    public class PhysicsBlock
    {
        public short x, y, z;
        public Block type;
        public DateTime startTime;

        public PhysicsBlock(short x, short y, short z, Block type)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.type = type;
            this.startTime = DateTime.Now;
        }
    }
}