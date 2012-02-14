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
        private static Thread waterThread;

        public static void towerInit(object sender, Events.PlayerPlacedBlockEventArgs e)
        {
            World world = e.Player.World;
            if (world.waterPhysics)
            {
                if (world.Map != null && world.IsLoaded)
                {
                    if (e.Context == BlockChangeContext.Manual)
                    {
                        if (e.NewBlock == Block.Iron)
                        {
                            waterThread = new Thread(new ThreadStart(delegate
                            {
                                e.Player.FlyCache = new System.Collections.Concurrent.ConcurrentDictionary<string, Vector3I>();
                                for (int z = e.Coords.Z; z <= world.Map.Height; z++)
                                {
                                    Thread.Sleep(250);
                                    if (world.Map.GetBlock(e.Coords.X, e.Coords.Y, z + 1) != Block.Air || world.Map.GetBlock(e.Coords) != Block.Iron)
                                        break;
                                    else
                                    {
                                        Vector3I tower = new Vector3I(e.Coords.X, e.Coords.Y, z + 1);
                                        e.Player.FlyCache.TryAdd(tower.ToString(), tower);
                                        e.Player.Send(PacketWriter.MakeSetBlock(tower, Block.Water));
                                        // world.Map.QueueUpdate(new BlockUpdate(
                                        // null, (short)e.Coords.X, (short)e.Coords.Y, (short)(z + 1), Block.Water));
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
            if (world.waterPhysics)
            {
                if (e.Player.TowerCache != null)
                {
                    if (world.Map != null && world.IsLoaded)
                    {
                        if (e.Block == Block.Iron)
                        {
                            waterThread = new Thread(new ThreadStart(delegate
                            {
                                Thread.Sleep(255);

                                foreach (Vector3I block in e.Player.FlyCache.Values)
                                {
                                    e.Player.Send(PacketWriter.MakeSetBlock(block, Block.Air));
                                }
                                e.Player.FlyCache.Clear();
                                //world.Map.QueueUpdate(new BlockUpdate(null, (short)e.Coords.X, (short)e.Coords.Y, (short)(z + 1), Block.Air));

                            })); waterThread.Start();
                        }
                    }
                }
            }
        }

        public static void waterPhysics(object sender, Events.PlayerPlacingBlockEventArgs e)
        {
            World world = e.Player.World;
            if (!world.waterPhysics)
                return;
            if (world.Map != null && world.IsLoaded)
            {
                if (e.Context == BlockChangeContext.Manual)
                {
                    if (e.NewBlock == Block.Water)
                    {
                        waterThread = new Thread(new ThreadStart(delegate
                        {
                            for (int x = e.Coords.X; x < world.Map.Volume; x++)
                            {
                                for (int y = e.Coords.Y; y < world.Map.Volume; y++)
                                {
                                    for (int z = e.Coords.Z; z >= 0; z--)
                                    {
                                        waterCheck(x - 1, y, z, world);
                                        waterCheck(x + 1, y, z, world);
                                        waterCheck(x, y - 1, z, world);
                                        waterCheck(x, y + 1, z, world);
                                    }
                                }
                            }
                            /*for (int z2 = z; z2 >= 0; z2--)
                            {
                                waterCheck(x - 1, y, z2, world.Map);
                                waterCheck(x + 1, y, z2, world.Map);
                                waterCheck(x, y - 1, z2, world.Map);
                                waterCheck(x, y + 1, z2, world.Map);
                            }*/

                        })); waterThread.Start();
                    }
                }
            }
        }


        public static bool waterCheck(int x, int y, int z, World world)
        {
            Thread.Sleep(Physics.Tick / 4);
            if (world.Map != null && world.IsLoaded &&
               world.Map.GetBlock(x, y, z) == Block.Air || world.Map.GetBlock(x, y, z) == Block.Water)
            {
                world.Map.QueueUpdate(new BlockUpdate(null, (short)x, (short)y, (short)z, Block.Water));
                return true;
            }
            else
            {
                return false;
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
                                    if (world.waterPhysics)
                                    {
                                        if (world.Map.GetBlock(e.Coords.X, e.Coords.Y, z) != Block.Water)
                                            break;
                                        else if (world.Map.GetBlock(e.Coords.X, e.Coords.Y, z) == Block.Water)
                                        {
                                            Thread.Sleep(Physics.Tick);
                                            if (z + 1 != e.Coords.Z + 1)
                                            {
                                                world.Map.QueueUpdate(new BlockUpdate(null, (short)e.Coords.X, (short)e.Coords.Y, (short)(z + 1), Block.Water)); //remove when water physics is done
                                            }
                                            world.Map.QueueUpdate(new BlockUpdate
                                                (null, (short)e.Coords.X, (short)e.Coords.Y, (short)(z), e.NewBlock));
                                        }
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
