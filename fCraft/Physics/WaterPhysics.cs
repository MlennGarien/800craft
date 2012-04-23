using System;
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
                                e.Player.TowerCache = new fCraft.Collections.ConcurrentDictionary<string, Vector3I>();
                                for (int z = e.Coords.Z; z <= world.Map.Height; z++)
                                {
                                    Thread.Sleep(250);
                                    if (world.Map != null && world.IsLoaded)
                                    {
                                        if (world.Map.GetBlock(e.Coords.X, e.Coords.Y, z + 1) != Block.Air
                                            || world.Map.GetBlock(e.Coords) != Block.Iron
                                            || e.Player.towerOrigin != e.Coords
                                            || !e.Player.towerMode)
                                        {
                                            break;
                                        }
                                        else
                                        {
                                            Vector3I tower = new Vector3I(e.Coords.X, e.Coords.Y, z + 1);
                                            e.Player.TowerCache.TryAdd(tower.ToString(), tower);
                                            e.Player.Send(PacketWriter.MakeSetBlock(tower, Block.Water));
                                        }
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
                                    e.Player.Send(PacketWriter.MakeSetBlock(block, world.Map.GetBlock(block)));
                                }
                                e.Player.TowerCache.Clear();
                            }
                        }
                    }
                }
            }
        }

        public static void blockFloat(object sender, Events.PlayerPlacingBlockEventArgs e)
        {
            try
            {
                World world = e.Player.World;
                if (world.waterPhysics)
                {
                    if (e.Context == BlockChangeContext.Manual)
                    {
                        if (e.NewBlock == Block.TNT && world.tntPhysics)
                        {
                            return;
                        }
                        if (e.NewBlock == Block.Red && world.fireworkPhysics)
                        {
                            return;
                        }
                        if (Physics.CanFloat(e.NewBlock))
                        {
                            waterThread = new Thread(new ThreadStart(delegate
                            {
                                for (int z = e.Coords.Z; z <= world.Map.Height; z++)
                                {
                                    if (world.Map != null && world.IsLoaded)
                                    {
                                        if (world.Map.GetBlock(e.Coords.X, e.Coords.Y, z) != Block.Water)
                                        {
                                            break;
                                        }
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
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.SeriousError, "" + ex);
            }
        }

        public static void blockSink(object sender, Events.PlayerPlacingBlockEventArgs e)
        {
            try
            {
                World world = e.Player.World;
                if (world.waterPhysics)
                {
                    if (e.Context == BlockChangeContext.Manual)
                    {
                        if (!Physics.CanFloat(e.NewBlock)
                            && e.NewBlock != Block.Air
                            && e.NewBlock != Block.Water
                            && e.NewBlock != Block.Lava
                            && e.NewBlock != Block.BrownMushroom
                            && e.NewBlock != Block.RedFlower
                            && e.NewBlock != Block.RedMushroom
                            && e.NewBlock != Block.YellowFlower
                            && e.NewBlock != Block.Plant)
                        {
                            if (e.OldBlock == Block.Water)
                            {
                                waterThread = new Thread(new ThreadStart(delegate
                                  {
                                      for (int z = e.Coords.Z; z > 0; z--)
                                      {
                                          Thread.Sleep(Physics.Tick);
                                          Vector3I oldChange = new Vector3I(e.Coords.X, e.Coords.Y, z);
                                          Vector3I newChange = new Vector3I(e.Coords.X, e.Coords.Y, z - 1);
                                          if (world.Map.GetBlock(newChange) != Block.Water)
                                          {
                                              break;
                                          }
                                          world.Map.QueueUpdate(new BlockUpdate(null, oldChange, Block.Water));
                                          world.Map.QueueUpdate(new BlockUpdate(null, newChange, e.NewBlock));
                                      }
                                  })); waterThread.Start();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.SeriousError, "" + ex);
            }
        }

        public static void drownCheck(SchedulerTask task)
        {
            try
            {
                foreach (Player p in Server.Players)
                {
                    if (p.World != null) //ignore console
                    {
                        if (p.World.waterPhysics)
                        {
                            Position pos = new Position(
                                (short)(p.Position.X / 32),
                                (short)(p.Position.Y / 32),
                                (short)((p.Position.Z + 1) / 32)
                            );
                            if (p.WorldMap.GetBlock(pos.X, pos.Y, pos.Z) == Block.Water)
                            {
                                if (p.DrownTime == null || (DateTime.Now - p.DrownTime).TotalSeconds > 33)
                                {
                                    p.DrownTime = DateTime.Now;
                                }
                                if ((DateTime.Now - p.DrownTime).TotalSeconds > 30)
                                {
                                    p.TeleportTo(p.WorldMap.Spawn);
                                    p.World.Players.Message("{0}&S drowned and died", p.ClassyName);
                                }
                            }
                            else
                            {
                                p.DrownTime = DateTime.Now;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.SeriousError, "" + ex);
            }
        }
    }
}