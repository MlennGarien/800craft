using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using fCraft.Events;
using System.Threading;

namespace fCraft.Physics
{
    class GunTest
    {
        public static void Init()
        {
            Player.Clicking += GunTest.ClickedGlass;//
            Player.Moving += GunTest.gunMove;//
            Player.JoinedWorld += GunTest.changedWorld;//
            Player.Moving += GunTest.movePortal;//
            Player.Disconnected += GunTest.playerDisconnected;//
            CommandManager.RegisterCommand(CdGun);
        }
        static readonly CommandDescriptor CdGun = new CommandDescriptor
        {
            Name = "Gun",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = false,
            Permissions = new[] { Permission.Chat },
            Usage = "/Gun",
            Help = "Fire At Will! TNT blocks explode TNT with physics on, Blue blocks make a Blue Portal, Orange blocks make an Orange Portal.",
            Handler = GunHandler
        };

        static void GunHandler(Player player, Command cmd)
        {
            if (player.GunMode)
            {
                player.GunMode = false;
                foreach (Vector3I block in player.GunCache.Values)
                {
                    player.Send(PacketWriter.MakeSetBlock(block.X, block.Y, block.Z, player.WorldMap.GetBlock(block)));
                    Vector3I removed;
                    player.GunCache.TryRemove(block.ToString(), out removed);
                }
                if (player.bluePortal.Count > 0)
                {
                    int i = 0;
                    foreach (Vector3I block in player.bluePortal)
                    {
                        player.WorldMap.QueueUpdate(new BlockUpdate(null, block, player.blueOld[i]));
                        i++;
                    }
                    player.blueOld.Clear();
                    player.bluePortal.Clear();
                }
                if (player.orangePortal.Count > 0)
                {
                    int i = 0;
                    foreach (Vector3I block in player.orangePortal)
                    {
                        player.WorldMap.QueueUpdate(new BlockUpdate(null, block, player.orangeOld[i]));
                        i++;
                    }
                    player.orangeOld.Clear();
                    player.orangePortal.Clear();
                }
                player.Message("&SGunMode deactivated");
            }
            else
            {
                player.GunMode = true;
                player.Message("&SGunMode activated. Fire at will!");
            }
        }
        private static Thread gunThread;
        public static void ClickedGlass(object sender, PlayerClickingEventArgs e)
        {
            if (e.Player.GunMode)
            {
                World world = e.Player.World;
                Map map = e.Player.World.Map;
                if (e.Player.GunCache.Values.Contains(e.Coords))
                {
                    Position p = e.Player.Position;
                    Pos pos;

                    double rSin = Math.Sin(((double)(128 - p.R) / 255) * 2 * Math.PI);
                    double rCos = Math.Cos(((double)(128 - p.R) / 255) * 2 * Math.PI);
                    double lCos = Math.Cos(((double)(p.L + 64) / 255) * 2 * Math.PI); //yaw = 64

                    ConcurrentDictionary<String, Vector3I> bullets = new ConcurrentDictionary<String, Vector3I>();
                    gunThread = new Thread(new ThreadStart(delegate
                    {
                        //gunThread.Priority = ThreadPriority.BelowNormal;
                        //start where the player is, not where he clicks
                        short startX = (short)(p.X / 32);
                        short startY = (short)(p.Y / 32);
                        short startZ = (short)(p.Z / 32);

                        e.Player.Send(PacketWriter.MakeSetBlock(e.Coords.X, e.Coords.Y, e.Coords.Z, Block.Glass)); //setblock

                        pos.X = (short)Math.Round((startX + (double)(rSin * 3))); //math.round improves accuracy
                        pos.Y = (short)Math.Round((startY + (double)(rCos * 3)));
                        pos.Z = (short)Math.Round((startZ + (double)(lCos * 3)));
                        for (int t = 4; t < map.Volume; t++)
                        {
                            pos.X = (short)Math.Round((startX + (double)(rSin * t)));
                            pos.Y = (short)Math.Round((startY + (double)(rCos * t)));
                            pos.Z = (short)Math.Round((startZ + (double)(lCos * t)));
                            bool hit = false;

                            Block toSend = Block.Admincrete;
                            if (e.Player.LastUsedBlockType == Block.Orange)
                            {
                                toSend = Block.Orange;
                            }

                            if (e.Player.LastUsedBlockType == Block.Blue)
                            {
                                toSend = Block.Blue;
                            }

                            if (e.Player.LastUsedBlockType == Block.TNT)
                            {
                                toSend = Block.TNT;
                            }
                            if (!map.InBounds(pos.X, pos.Y, pos.Z))
                            {
                                break;
                            }
                            if (map.GetBlock(pos.X, pos.Y, pos.Z) == Block.Air || map.GetBlock((int)pos.X, (int)pos.Y, (int)pos.Z) == toSend)
                            {
                                bullets.TryAdd(new Vector3I(pos.X, pos.Y, pos.Z).ToString(), new Vector3I(pos.X, pos.Y, pos.Z));
                                map.QueueUpdate(new BlockUpdate(null,
                                    (short)pos.X,
                                    (short)pos.Y,
                                    (short)pos.Z,
                                    toSend));
                                foreach (Player player in world.Players)
                                {
                                    if ((player.Position.X / 32) == pos.X || (player.Position.X / 32 + 1) == pos.X || (player.Position.X / 32 - 1) == pos.X)
                                    {
                                        if ((player.Position.Y / 32) == pos.Y || (player.Position.Y / 32 + 1) == pos.Y || (player.Position.Y / 32 - 1) == pos.Y)
                                        {
                                            if ((player.Position.Z / 32) == pos.Z || (player.Position.Z / 32 + 1) == pos.Z || (player.Position.Z / 32 - 1) == pos.Z)
                                            {
                                                if (world.tntPhysics && toSend == Block.TNT)
                                                {
                                                    int seed = new Random().Next(1, 6);
                                                    ExplodingPhysics.startExplosion(new Vector3I(pos.X, pos.Y, pos.Z), e.Player, world, seed);
                                                    world.Players.Message("{0}&S was blown up by {1}", player.ClassyName, e.Player.ClassyName);
                                                    player.TeleportTo(map.Spawn);
                                                    Thread.Sleep(Physics.Tick);
                                                    ExplodingPhysics.removeLava(new Vector3I(pos.X, pos.Y, pos.Z), e.Player, world, seed);
                                                    removal(bullets, map);
                                                    hit = true;
                                                }
                                                else
                                                {
                                                    world.Players.Message("{0}&S was shot by {1}", player.ClassyName, e.Player.ClassyName);
                                                    player.TeleportTo(map.Spawn);
                                                    removal(bullets, map);
                                                    hit = true;
                                                }
                                            }
                                        }
                                    }
                                }

                                Thread.Sleep(70);
                                removal(bullets, map);
                                if (hit)
                                {
                                    Server.Message("hit player");
                                    break;
                                }
                            }

                            else
                            {
                                //tnt
                                if (world.tntPhysics && toSend == Block.TNT)
                                {
                                    int seed = new Random().Next(1, 6);
                                    ExplodingPhysics.startExplosion(new Vector3I((int)pos.X, (int)pos.Y, (int)pos.Z), e.Player, world, seed);
                                    Thread.Sleep(Physics.Tick);
                                    ExplodingPhysics.removeLava(new Vector3I((int)pos.X, (int)pos.Y, (int)pos.Z), e.Player, world, seed);
                                    removal(bullets, map);
                                    hit = true;
                                }
                                if (e.Player.bluePortal.Count > 0)
                                {
                                    if (new Vector3I(pos.X, pos.Y, pos.Z) == e.Player.bluePortal[0] ||
                                        new Vector3I(pos.X, pos.Y, pos.Z) == e.Player.bluePortal[1])
                                    {
                                        break;
                                    }
                                }
                                if (e.Player.orangePortal.Count > 0)
                                {
                                    if (new Vector3I(pos.X, pos.Y, pos.Z) == e.Player.orangePortal[0] ||
                                        new Vector3I(pos.X, pos.Y, pos.Z) == e.Player.orangePortal[1])
                                    {
                                        break;
                                    }
                                }
                                //blue portal
                                if (toSend == Block.Blue)
                                {
                                    if (CanPlacePortal(pos.X, pos.Y, pos.Z, map))
                                    {
                                        if (e.Player.bluePortal.Count > 0)
                                        {
                                            int i = 0;
                                            foreach (Vector3I block in e.Player.bluePortal)
                                            {
                                                map.QueueUpdate(new BlockUpdate(null, block, e.Player.blueOld[i]));
                                                i++;
                                            }
                                            e.Player.blueOld.Clear();
                                            e.Player.bluePortal.Clear();
                                        }

                                        e.Player.blueOld.Add(map.GetBlock(pos.X, pos.Y, pos.Z));
                                        e.Player.blueOld.Add(map.GetBlock(pos.X, pos.Y, pos.Z + 1));
                                        for (double z = pos.Z; z < pos.Z + 2; z++)
                                        {
                                            map.QueueUpdate(new BlockUpdate(null, (short)(pos.X), (short)(pos.Y), (short)z, Block.Water));
                                            e.Player.bluePortal.Add(new Vector3I((int)pos.X, (int)pos.Y, (int)z));
                                        }
                                        break;
                                    }
                                }

                                    //orange portal
                                else if (toSend == Block.Orange)
                                {
                                    if (CanPlacePortal(pos.X, pos.Y, pos.Z, map))
                                    {
                                        if (e.Player.orangePortal.Count > 0)
                                        {
                                            int i = 0;
                                            foreach (Vector3I block in e.Player.orangePortal)
                                            {
                                                map.QueueUpdate(new BlockUpdate(null, block, e.Player.orangeOld[i]));
                                                i++;
                                            }
                                            e.Player.orangeOld.Clear();
                                            e.Player.orangePortal.Clear();

                                        }
                                        e.Player.orangeOld.Add(map.GetBlock(pos.X, pos.Y, pos.Z));
                                        e.Player.orangeOld.Add(map.GetBlock(pos.X, pos.Y, pos.Z + 1));
                                        for (double z = pos.Z; z < pos.Z + 2; z++)
                                        {
                                            map.QueueUpdate(new BlockUpdate(null, (short)(pos.X), (short)(pos.Y), (short)z, Block.Lava));
                                            e.Player.orangePortal.Add(new Vector3I((int)pos.X, (int)pos.Y, (int)z));
                                        }
                                        break;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    })); gunThread.Start();
                }
            }
        }

        public static void movePortal(object sender, PlayerMovingEventArgs e)
        {
            if (e.Player.LastUsedPortal != null && (DateTime.Now - e.Player.LastUsedPortal).TotalSeconds < 4)
            {
                return;
            }
            Vector3I newPos = new Vector3I(e.NewPosition.X / 32, e.NewPosition.Y / 32, (e.NewPosition.Z / 32));
            foreach (Player p in e.Player.World.Players)
            {
                foreach (Vector3I block in p.bluePortal)
                {
                    if (newPos == block)
                    {
                        if (p.World.Map.GetBlock(block) == Block.Water)
                        {
                            if (p.orangePortal.Count > 0)
                            {
                                e.Player.TeleportTo(new Position
                                {
                                    X = (short)((p.orangePortal[0].X) * 32),
                                    Y = (short)((p.orangePortal[0].Y) * 32),
                                    Z = (short)(((p.orangePortal[0].Z) + 1.59375) * 32),
                                    R = (byte)(e.Player.Position.R - 128),
                                    L = e.Player.Position.L
                                });
                            }
                            e.Player.LastUsedPortal = DateTime.Now;
                        }
                    }
                }

                foreach (Vector3I block in p.orangePortal)
                {
                    if (newPos == block)
                    {
                        if (p.World.Map.GetBlock(block) == Block.Lava)
                        {
                            if (p.bluePortal.Count > 0)
                            {
                                e.Player.TeleportTo(new Position
                                {
                                    X = (short)((p.bluePortal[0].X) * 32),
                                    Y = (short)((p.bluePortal[0].Y) * 32),
                                    Z = (short)(((p.bluePortal[0].Z) + 1.59375) * 32), //fixed point 1.59375 lol.
                                    R = (byte)(e.Player.Position.R - 128),
                                    L = e.Player.Position.L
                                });
                            }
                            e.Player.LastUsedPortal = DateTime.Now;
                        }
                    }
                }
            }
        }

        public static void changedWorld(object sender, PlayerJoinedWorldEventArgs e)
        {
            if (e.OldWorld != null)
            {
                if (e.OldWorld.Name == e.NewWorld.Name)
                {
                    e.Player.orangeOld.Clear();
                    e.Player.orangePortal.Clear();
                    e.Player.blueOld.Clear();
                    e.Player.bluePortal.Clear();
                }
                if (e.OldWorld.IsLoaded)
                {
                    Map map = e.OldWorld.Map;
                    if (e.Player.orangePortal.Count > 0)
                    {
                        int i = 0;
                        foreach (Vector3I block in e.Player.orangePortal)
                        {
                            map.QueueUpdate(new BlockUpdate(null, block, e.Player.orangeOld[i]));
                            i++;
                        }
                        e.Player.orangeOld.Clear();
                        e.Player.orangePortal.Clear();
                    }

                    if (e.Player.bluePortal.Count > 0)
                    {
                        int i = 0;
                        foreach (Vector3I block in e.Player.bluePortal)
                        {
                            map.QueueUpdate(new BlockUpdate(null, block, e.Player.blueOld[i]));
                            i++;
                        }
                        e.Player.blueOld.Clear();
                        e.Player.bluePortal.Clear();
                    }
                }
                else
                {
                    if (e.Player.bluePortal.Count > 0)
                    {
                        e.OldWorld.Map.Blocks[e.OldWorld.Map.Index(e.Player.bluePortal[0])] = (byte)e.Player.blueOld[0];
                        e.OldWorld.Map.Blocks[e.OldWorld.Map.Index(e.Player.bluePortal[1])] = (byte)e.Player.blueOld[1];
                        e.Player.blueOld.Clear();
                        e.Player.bluePortal.Clear();
                    }
                    if (e.Player.orangePortal.Count > 0)
                    {
                        e.OldWorld.Map.Blocks[e.OldWorld.Map.Index(e.Player.orangePortal[0])] = (byte)e.Player.orangeOld[0];
                        e.OldWorld.Map.Blocks[e.OldWorld.Map.Index(e.Player.orangePortal[1])] = (byte)e.Player.orangeOld[1];
                        e.Player.orangeOld.Clear();
                        e.Player.orangePortal.Clear();
                    }
                }
            }
        }

        public static void playerDisconnected(object sender, PlayerDisconnectedEventArgs e)
        {
            if (e.Player.World != null)
            {
                if (e.Player.World.IsLoaded)
                {
                    Map map = e.Player.World.Map;
                    if (e.Player.orangePortal.Count > 0)
                    {
                        int i = 0;
                        foreach (Vector3I block in e.Player.orangePortal)
                        {
                            map.QueueUpdate(new BlockUpdate(null, block, e.Player.orangeOld[i]));
                            i++;
                        }
                        e.Player.orangeOld.Clear();
                        e.Player.orangePortal.Clear();
                    }

                    if (e.Player.bluePortal.Count > 0)
                    {
                        int i = 0;
                        foreach (Vector3I block in e.Player.bluePortal)
                        {
                            map.QueueUpdate(new BlockUpdate(null, block, e.Player.blueOld[i]));
                            i++;
                        }
                        e.Player.blueOld.Clear();
                        e.Player.bluePortal.Clear();
                    }
                }
                else
                {
                    if (e.Player.bluePortal.Count > 0)
                    {
                        e.Player.World.Map.Blocks[e.Player.World.Map.Index(e.Player.bluePortal[0])] = (byte)e.Player.blueOld[0];
                        e.Player.World.Map.Blocks[e.Player.World.Map.Index(e.Player.bluePortal[1])] = (byte)e.Player.blueOld[1];
                    }
                    if (e.Player.orangePortal.Count > 0)
                    {
                        e.Player.WorldMap.Blocks[e.Player.WorldMap.Index(e.Player.orangePortal[0])] = (byte)e.Player.orangeOld[0];
                        e.Player.WorldMap.Blocks[e.Player.WorldMap.Index(e.Player.orangePortal[1])] = (byte)e.Player.orangeOld[1];
                    }
                }
            }
        }
        public static void removal(ConcurrentDictionary<String, Vector3I> bullets, Map map)
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

        public static void gunMove(object sender, PlayerMovingEventArgs e)
        {
            if (e.Player.GunMode)
            {
                Position p = e.Player.Position;
                Position pos = new Position();
                Map map = e.Player.World.Map;
                double rSin = Math.Sin(((double)(128 - p.R) / 255) * 2 * Math.PI);
                double rCos = Math.Cos(((double)(128 - p.R) / 255) * 2 * Math.PI);
                double lCos = Math.Cos(((double)(p.L + 64) / 255) * 2 * Math.PI);

                short x = (short)(p.X / 32);
                x = (short)Math.Round(x + (double)(rSin * 3));

                short y = (short)(p.Y / 32);
                y = (short)Math.Round(y + (double)(rCos * 3));

                short z = (short)(p.Z / 32);
                z = (short)Math.Round(z + (double)(lCos * 3));

                for (short x2 = (short)(x + 1); x2 >= x - 1; x2--)
                {
                    for (short y2 = (short)(y + 1); y2 >= y - 1; y2--)
                    {
                        for (short z2 = z; z2 <= z + 1; z2++)
                        {
                            if (map.GetBlock(x2, y2, z2) == Block.Air)
                            {
                                pos = new Position(x2, y2, z2);
                                if (!e.Player.GunCache.Values.Contains(new Vector3I(pos.X, pos.Y, pos.Z)))
                                {
                                    e.Player.Send(PacketWriter.MakeSetBlock(pos.X, pos.Y, pos.Z, Block.Glass));
                                    e.Player.GunCache.TryAdd(pos.ToVector3I().ToString(), pos.ToVector3I());
                                }
                            }
                        }
                    }
                }

                if (CanRemoveBlock(e.Player, e.OldPosition, e.NewPosition))
                {
                    foreach (Vector3I block in e.Player.GunCache.Values)
                    {
                        e.Player.Send(PacketWriter.MakeSetBlock(block.X, block.Y, block.Z, map.GetBlock(block)));
                        Vector3I removed;
                        e.Player.GunCache.TryRemove(block.ToString(), out removed);
                    }
                }

            }
        }
        public static bool CanRemoveBlock(Player player, Position oldpos, Position newPos)
        {
            int x = oldpos.X - newPos.X;
            int y = oldpos.Y - newPos.Y;
            int z = oldpos.Z - newPos.Z;
            int r = oldpos.R - newPos.R;
            int l = oldpos.L - newPos.L;

            if (!(x >= -2 && x <= 2) || !(y >= -2 && y <= 2) || !(z >= -3 && z <= 3))
            {
                return true;
            }
            if (!(x >= -2 && x <= 2) || !(y >= -2 && y <= 2) || !(z >= -2 && z <= 2))
            {
                return true;
            }

            if (!(r >= -2 && r <= 2) || !(l >= -2 && l <= 2))
            {
                return true;
            }

            return false;
        }
        public static bool CanPlacePortal(short x, short y, short z, Map map)
        {
            int Count = 0;
            for (short Z = z; Z < z + 2; Z++)
            {
                if (map.GetBlock(x, y, Z) != Block.Air)
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
        public struct Pos { public short X, Y, Z; }
    }
}