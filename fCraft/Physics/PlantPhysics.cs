using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;
using fCraft.Physics;
using System.Threading;

namespace fCraft.Physics
{
    class PlantPhysics
    {

        private static Thread plantThread;
        private static Thread checkGrass;

        public static void TreeGrowing(object sender, Events.PlayerPlacingBlockEventArgs e)
        {
            World world = e.Player.World;
            if (!world.plantPhysics)
                return;
            if (world.Map != null && world.IsLoaded)
            {
                if (e.Context == BlockChangeContext.Manual)
                {
                    if (e.NewBlock == Block.Plant)
                    {
                        Random rand = new Random();
                        int Height = rand.Next(4, 7);
                        for (int x = e.Coords.X; x < e.Coords.X + 5; x++)
                        {
                            for (int y = e.Coords.Y; y < e.Coords.Y + 5; y++)
                            {
                                for (int z = e.Coords.Z; z < e.Coords.Z + Height + 1; z++)
                                {
                                    if (world.Map.GetBlock(x, y, z) != Block.Air)
                                        return;
                                }
                            }
                        }

                        for (int x = e.Coords.X; x > e.Coords.X - 5; x--)
                        {
                            for (int y = e.Coords.Y; y > e.Coords.Y - 5; y--)
                            {
                                for (int z = e.Coords.Z; z < e.Coords.Z + Height + 1; z++)
                                {
                                    if (world.Map.GetBlock(x, y, z) != Block.Air)
                                        return;
                                }
                            }
                        }

                        plantThread = new Thread(new ThreadStart(delegate
                        {
                            Thread.Sleep(5000);
                            if (e.Player.WorldMap.GetBlock(e.Coords) == Block.Plant)
                            {
                                string type = null;
                                if (e.Player.WorldMap.GetBlock(e.Coords.X, e.Coords.Y, e.Coords.Z - 1) == Block.Grass)
                                    type = "grass";
                                else if (e.Player.WorldMap.GetBlock(e.Coords.X, e.Coords.Y, e.Coords.Z - 1) == Block.Sand)
                                    type = "sand";
                                else return;
                                MakeTrunks(world, e.Coords, Height, type);
                            }
                        }));
                        plantThread.Start();
                    }
                }
            }
        }


        public static void MakeTrunks(World w, Vector3I Coords, int Height, string type)
        {
            if (!w.plantPhysics)
                return;
            if (w.Map != null && w.IsLoaded)
            {
                for (int i = 0; i < Height; i++)
                {
                    Thread.Sleep(Physics.Tick);
                    w.Map.QueueUpdate(new BlockUpdate(null, (short)Coords.X, (short)Coords.Y, (short)(Coords.Z + i), Block.Log));
                }
                if (type.Equals("grass"))
                    TreeGeneration.MakeNormalFoliage(w, Coords, Height + 1);

                else if (type.Equals("sand"))
                    TreeGeneration.MakePalmFoliage(w, Coords, Height);
            }
        }

        //grass physics

        public static void grassChecker(SchedulerTask task)
        {
            if (checkGrass != null)
            {
                if (checkGrass.ThreadState != ThreadState.Stopped) //stops multiple threads from opening
                {
                    return;
                }
            }
            checkGrass = new Thread(new ThreadStart(delegate
            {
                foreach (World world in WorldManager.Worlds)
                {
                    if (world.Map != null && world.IsLoaded) //for all loaded worlds
                    {
                        if (world.plantPhysics)
                        {
                            Map map = world.Map;

                            for (int x = world.Map.Bounds.XMin; x < world.Map.Bounds.XMax; x++)
                            {
                                if (world.Map == null) //unload protection
                                {
                                    break;
                                }

                                for (int y = world.Map.Bounds.YMin; y < world.Map.Bounds.YMax; y++)
                                {
                                    if (world.Map == null)
                                    {
                                        break;
                                    }

                                    for (int z = world.Map.Bounds.ZMin; z < world.Map.Bounds.ZMax; z++)
                                    {
                                        if (world.Map == null)
                                        {
                                            break;
                                        }
                                        if (map.InBounds(x, y, z) && Physics.CanPutGrassOn(new Vector3I(x, y, z), world)) //shadow detection
                                        {
                                            if (new Random().Next(1, 45) > 35) //random seed generation lolz
                                            {
                                                map.QueueUpdate(new BlockUpdate(null, (short)x, (short)y, (short)z, Block.Grass));
                                            }
                                            Thread.Sleep(25); //throttle, slow down horsey
                                        } //0.5 - 3% cpu average, better than the original ~17%
                                        //has not been tested with more than 5 maps loaded at once
                                    }
                                }
                            }
                        }
                    }
                }
            })); checkGrass.Start();
        }
    }
}
