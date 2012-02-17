using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Physics;
using System.Threading;
using fCraft.Events;
using fCraft.Drawing;

namespace fCraft.Physics
{
    class ExplodingPhysics
    {
        private static Thread explodeThread;

        public static void TNTClick(object sender, Events.PlayerClickedEventArgs e)
        {
            World world = e.Player.World;
            if (!world.tntPhysics)
                return;
            if (world.Map != null && world.IsLoaded)
            {
                if (world.Map.GetBlock(e.Coords) == Block.TNT)
                {
                    explodeThread = new Thread(new ThreadStart(delegate
                    {
                        Physics.size = 3;
                        world.Map.QueueUpdate(new BlockUpdate(null, e.Coords, Block.Air));
                        int Seed = new Random().Next(1, 15);
                        startExplosion(e.Coords, e.Player, world, Seed);
                        Scheduler.NewTask(t => removeLava(e.Coords, e.Player, world, Seed)).RunOnce(TimeSpan.FromMilliseconds(300));
                    }));
                    explodeThread.Start();
                }
            }
        }


        public static void TNTDrop(object sender, Events.PlayerPlacingBlockEventArgs e)
        {
            World world = e.Player.World;
            if (!world.tntPhysics)
                return;
            if (world.Map != null && world.IsLoaded)
            {
                if (e.Context == BlockChangeContext.Manual)
                {
                    if (e.NewBlock == Block.TNT)
                    {
                        explodeThread = new Thread(new ThreadStart(delegate
                        {
                            Vector3I tempPos = e.Coords;
                            int dropZ = e.Coords.Z;
                            while (Physics.BlockThrough(world.Map.GetBlock(e.Coords.X, e.Coords.Y, dropZ - 1)))
                            {
                                if (world.Map != null && world.IsLoaded)
                                {
                                    Thread.Sleep(Physics.Tick);
                                    dropZ--;
                                    if (dropZ == e.Coords.Z) return;
                                    world.Map.QueueUpdate(new BlockUpdate(null, tempPos, Block.Air));
                                    world.Map.QueueUpdate(new BlockUpdate(null, (short)e.Coords.X, (short)e.Coords.Y, (short)dropZ, Block.TNT));
                                    tempPos = new Vector3I(e.Coords.X, e.Coords.Y, dropZ);
                                }
                            }

                            Thread.Sleep(2000); //wait 2secs before big boom
                            Physics.size = 3;
                            SphereDrawOperation operation = new SphereDrawOperation(e.Player);
                            world.Map.QueueUpdate(new BlockUpdate(null, e.Coords, Block.Air));
                            int Seed = new Random().Next(1, 15);
                            startExplosion(e.Coords, e.Player, world, Seed);
                            Scheduler.NewTask(t => removeLava(e.Coords, e.Player, world, Seed)).RunOnce(TimeSpan.FromMilliseconds(300));
                        }));
                        explodeThread.Start(); //congrats
                    }
                }
            }
        }

        public static void startExplosion(Vector3I Coords, Player p, World world, int Seed)
        {
            if (world.Map != null && world.IsLoaded)
            {
                if (!world.tntPhysics)
                    return;
                SphereDrawOperation operation = new SphereDrawOperation(p);
                MarbledBrush brush = new MarbledBrush(Block.Lava, 1);
                Vector3I secPos = new Vector3I(Coords.X + Physics.size, Coords.Y, Coords.Z);
                Vector3I[] marks = { Coords, secPos };
                operation.Brush = brush;
                brush.Seed = Seed;
                operation.Prepare(marks);
                operation.AnnounceCompletion = false;
                operation.Context = BlockChangeContext.Explosion;
                operation.Begin();
            }
        }

        public static void removeLava(Vector3I Coords, Player p, World world, int Seed)
        {
            if (world.Map != null && world.IsLoaded)
            {
                if (!world.tntPhysics)
                    return;
                SphereDrawOperation operation = new SphereDrawOperation(p);
                MarbledBrush brush = new MarbledBrush(Block.Air, 1);
                Vector3I secPos = new Vector3I(Coords.X + Physics.size, Coords.Y, Coords.Z);
                Vector3I[] marks = { Coords, secPos };
                operation.Brush = brush;
                brush.Seed = Seed;
                operation.Prepare(marks);
                operation.AnnounceCompletion = false;
                operation.Context = BlockChangeContext.Explosion;
                operation.Begin();
            }
        }


        public static void Firework(object sender, Events.PlayerPlacingBlockEventArgs e)
        {
            World world = e.Player.World;
            if (!world.tntPhysics)
                return;
            if (world.Map != null && world.IsLoaded)
            {
                if (e.Context == BlockChangeContext.Manual)
                {
                    if (e.NewBlock == Block.Red)
                    {
                        explodeThread = new Thread(new ThreadStart(delegate
                        {
                            int upZ = e.Coords.Z;
                            for (int up = 0; up < 12; up++)
                            {
                                Thread.Sleep(Physics.Tick);
                                if (!Physics.BlockThrough(world.Map.GetBlock(e.Coords.X, e.Coords.Y, upZ + 1)))
                                {
                                    Thread.Sleep(1000);
                                    break;
                                }
                                upZ++;
                                if (upZ == e.Coords.Z) return;
                                world.Map.QueueUpdate(new BlockUpdate(null, (short)e.Coords.X, (short)e.Coords.Y, (short)(upZ - 1), Block.Air));
                                world.Map.QueueUpdate(new BlockUpdate(null, (short)e.Coords.X, (short)e.Coords.Y, (short)upZ, Block.Red));
                            }
                            Physics.size = 4;
                            int X2, Y2, Z2;
                            Random rand = new Random();
                            int blockId = rand.Next(1, 9);
                            Block fBlock = new Block();
                            if (blockId == 1) fBlock = Block.Lava;
                            if (blockId <= 6 && blockId != 1) fBlock = (Block)rand.Next(21, 33);
                            world.Map.QueueUpdate(new BlockUpdate(
                                null, (short)e.Coords.X, (short)e.Coords.Y, (short)upZ, Block.Air));

                            for (X2 = e.Coords.X - (Physics.size + 1); X2 <= e.Coords.X + (Physics.size + 1); X2++)
                            {
                                for (Y2 = (e.Coords.Y - (Physics.size + 1)); Y2 <= (e.Coords.Y + (Physics.size + 1)); Y2++)
                                {
                                    for (Z2 = (upZ - (Physics.size + 1)); Z2 <= (upZ + (Physics.size + 1)); Z2++)
                                    {
                                        if (rand.Next(1, 50) < 3)
                                        {
                                            if (!Physics.BlockThrough(world.Map.GetBlock(X2, Y2, Z2)))
                                            {
                                                break;
                                            }
                                            if (blockId > 7)
                                                fBlock = (Block)rand.Next(21, 33);
                                            if (world.Map != null && world.IsLoaded)
                                            {
                                                if (!world.tntPhysics)
                                                    return;
                                                Explode(world, X2, Y2, Z2, (Block)fBlock);
                                                Removal(world, X2, Y2, Z2);
                                            }
                                        }
                                    }
                                }
                            }
                        }));
                        explodeThread.Start();
                    }
                }
            }
        }

        public static void Explode(World w, int X2, int Y2, int Z2, Block block)
        {
            BlockUpdate fwSender = new BlockUpdate(null, (short)X2, (short)Y2, (short)Z2, block);
            w.Map.QueueUpdate(fwSender);
        }

        public static void Removal(World w, int X2, int Y2, int Z2)
        {
            BlockUpdate fwSender = new BlockUpdate(null, (short)X2, (short)Y2, (short)Z2, Block.Air);
            Scheduler.NewTask(t => w.Map.QueueUpdate(fwSender)).RunOnce(TimeSpan.FromMilliseconds(300));
        }
    }
}
