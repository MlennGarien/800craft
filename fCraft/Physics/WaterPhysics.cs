using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;
using fCraft.Physics;
using System.Threading;

namespace fCraft.Physics
{
    class WaterPhysics
    {
        public static Thread waterThread;

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
                                    if (world.Map.GetBlock(e.Coords.X, e.Coords.Y, z + 1) != Block.Air
                                        || world.Map.GetBlock(e.Coords) != Block.Iron
                                        || e.Player.towerOrigin != e.Coords
                                        || !e.Player.towerMode)
                                        break;
                                    else
                                    {
                                        Vector3I tower = new Vector3I(e.Coords.X, e.Coords.Y, z + 1);
                                        e.Player.TowerCache.TryAdd(tower.ToString(), tower);
                                        e.Player.Send(PacketWriter.MakeSetBlock(tower, Block.Water));
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
                        if (world.Map != null && world.IsLoaded)
                        {
                            if (e.Block == Block.Iron)
                            {
                                e.Player.towerOrigin = new Vector3I();
                                foreach (Vector3I block in e.Player.TowerCache.Values)
                                {
                                    e.Player.Send(PacketWriter.MakeSetBlock(block, Block.Air));
                                }
                                e.Player.TowerCache.Clear();
                            }
                        }
                    }
                }
            }
        }


        public static void waterChecker(SchedulerTask task)
        {
            if (waterThread != null)
            {
                if (waterThread.ThreadState != ThreadState.Stopped) //stops multiple threads from opening
                {
                    return;
                }
            }
            waterThread = new Thread(new ThreadStart(delegate
            {
                waterThread.IsBackground = true;
                var worlds = WorldManager.Worlds.Where(w => w.IsLoaded && w.Map != null && w.waterPhysics);
                foreach (World world in worlds)
                {
                    if (world.IsLoaded && world.Map != null && world.waterPhysics)
                    {
                        Map map = world.Map;
                        int i = 0;
                        for (int x = world.Map.Bounds.XMin; x < world.Map.Bounds.XMax; x++)
                        {
                            for (int y = world.Map.Bounds.YMin; y < world.Map.Bounds.YMax; y++)
                            {
                                for (int z = world.Map.Bounds.ZMin; z < world.Map.Bounds.ZMax; z++)
                                {
                                    if (world.Map == null && !world.IsLoaded)
                                    {
                                        break;
                                    }

                                    if (world.waterPhysics)
                                    {
                                        if (map.GetBlock(x, y, z) == Block.Water)
                                        {
                                            if (world.waterQueue.Values.Contains(new Vector3I(x, y, z)))
                                            {
                                                return;
                                            }
                                            else
                                            {
                                                i++;
                                                world.waterQueue.TryAdd(new Vector3I(x, y, z).ToString(), new Vector3I(x, y, z));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            })); waterThread.Start();
        }


        public static void deQueueWater(SchedulerTask task)
        {
            if (waterThread != null)
            {
                if (waterThread.ThreadState != ThreadState.Stopped) //stops multiple threads from opening
                {
                    return;
                }
            }
            var worlds = WorldManager.Worlds.Where(w => w.IsLoaded && w.Map != null && w.waterPhysics);
            foreach (World world in worlds)
            {
                if (world.Map != null && world.IsLoaded)
                {
                    if (world.waterPhysics)
                    {
                        if (world.waterQueue.Values.Count > 0)
                        {
                            waterThread = new Thread(new ThreadStart(delegate
                             {
                                 //waterThread.IsBackground = true;
                                 foreach (Vector3I block in world.waterQueue.Values)
                                 {
                                     if (world.IsLoaded && world.Map != null && world.waterPhysics)
                                     {
                                         waterCheck(block.X - 1, block.Y, block.Z, world);
                                         waterCheck(block.X + 1, block.Y, block.Z, world);
                                         waterCheck(block.X, block.Y + 1, block.Z, world);
                                         waterCheck(block.X, block.Y - 1, block.Z, world);
                                         waterCheck(block.X, block.Y, block.Z - 1, world);
                                     }
                                 }
                             })); waterThread.Start();
                        }
                    }
                }
            }
        }

        public static void waterCheck(int x, int y, int z, World world)
        {
            if (world.Map != null && world.IsLoaded &&
               world.Map.GetBlock(x, y, z) == Block.Air)
            {
                world.Map.QueueUpdate(new BlockUpdate(null, (short)x, (short)y, (short)z, Block.Water));
                world.waterQueue.TryAdd(new Vector3I(x, y, z).ToString(), new Vector3I(x, y, z));
            }
        }

        public static void blockFloat(object sender, Events.PlayerPlacingBlockEventArgs e)
        {
            World world = e.Player.World;
            if (world.waterPhysics)
            {
                if (e.Context == BlockChangeContext.Manual)
                {
                    if (Physics.CanFloat(e.NewBlock))
                    {
                        if (e.NewBlock == Block.TNT && world.tntPhysics)
                        {
                            return;
                        }
                        if (e.NewBlock == Block.Red && world.tntPhysics)
                        {
                            return;
                        }
                        waterThread = new Thread(new ThreadStart(delegate
                        {
                            for (int z = e.Coords.Z; z <= world.Map.Height; z++)
                            {
                                if (world.Map != null && world.IsLoaded)
                                {
                                    if (world.Map.GetBlock(e.Coords.X, e.Coords.Y, z) != Block.Water)
                                        break;
                                    else if (world.Map.GetBlock(e.Coords.X, e.Coords.Y, z) == Block.Water)
                                    {
                                        Thread.Sleep(Physics.Tick);
                                        if (z - 1 != e.Coords.Z - 1)
                                        {
                                            e.Player.WorldMap.QueueUpdate(new BlockUpdate(null, (short)e.Coords.X, (short)e.Coords.Y, (short)(z - 1), Block.Water)); //remove when water physics is done
                                        }
                                        e.Player.WorldMap.QueueUpdate(new BlockUpdate
                                            (null, (short)e.Coords.X, (short)e.Coords.Y, (short)(z), e.NewBlock));
                                    }
                                }
                            }
                        })); waterThread.Start();
                    }
                }

                else if (e.NewBlock != Block.Air
                    && e.NewBlock != Block.Water
                    && e.NewBlock != Block.Lava
                    && e.NewBlock != Block.BrownMushroom
                    && e.NewBlock != Block.RedFlower
                    && e.NewBlock != Block.RedMushroom
                    && e.NewBlock != Block.YellowFlower
                    && e.NewBlock != Block.Plant)
                {
                    if (world.waterPhysics)
                    {
                        waterThread = new Thread(new ThreadStart(delegate
                        {
                            for (int z = e.Coords.Z; z >= 1; z--)
                            {
                                if (world.Map != null && world.IsLoaded)
                                {
                                    if (world.Map.GetBlock(e.Coords.X, e.Coords.Y, z) == Block.Water)
                                    {
                                        Thread.Sleep(Physics.Tick);
                                        world.Map.QueueUpdate(new BlockUpdate(null, (short)e.Coords.X, (short)e.Coords.Y, (short)(z - 1), Block.Water)); //remove when water physics is done

                                        world.Map.QueueUpdate(new BlockUpdate
                                            (null, (short)e.Coords.X, (short)e.Coords.Y, (short)(z), e.NewBlock));
                                    }
                                }
                            }
                        })); waterThread.Start();
                    }
                }
            }
        }
    }
}
