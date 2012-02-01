using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;

namespace fCraft.Utils
{
    class FlyHandler
    {
        private static FlyHandler instance;

        private FlyHandler()
        {
            // Empty, singleton
        }

        public static FlyHandler GetInstance()
        {
            if (instance == null)
            {
                instance = new FlyHandler();
                Player.Moved += new EventHandler<Events.PlayerMovedEventArgs>(Player_Moved);
            }

            return instance;
        }
        public static Thread flyThread;
        private static void Player_Moved(object sender, Events.PlayerMovedEventArgs e)
        {
            try
            {
                if (e.Player.IsFlying)
                {
                    flyThread = new Thread(new ThreadStart(delegate
                        {
                            // We need to have block positions, so we divide by 32
                            Vector3I oldPos = new Vector3I(e.OldPosition.X / 32, e.OldPosition.Y / 32, e.OldPosition.Z / 32);
                            Vector3I newPos = new Vector3I(e.NewPosition.X / 32, e.NewPosition.Y / 32, e.NewPosition.Z / 32);

                            // Check if the player actually moved and not just rotated
                            if ((oldPos.X != newPos.X) || (oldPos.Y != newPos.Y) || (oldPos.Z != newPos.Z))
                            {
                                // Create new blocks part
                                for (int i = -2; i <= 2; i++)
                                {
                                    for (int j = -2; j <= 2; j++)
                                    {
                                        Vector3I carpet = new Vector3I(newPos.X + i, newPos.Y + j, newPos.Z - 2);

                                        if (e.Player.World.Map.GetBlock(carpet) == Block.Air)
                                        {
                                            e.Player.Send(PacketWriter.MakeSetBlock(carpet, Block.Glass));
                                            e.Player.FlyCache.TryAdd(carpet.ToString(), carpet);
                                        }
                                    }
                                }
                                       

                                // Remove old blocks
                                foreach (Vector3I block in e.Player.FlyCache.Values)
                                {
                                    if (CanRemoveBlock(e.Player, block, newPos))
                                    {
                                        e.Player.Send(PacketWriter.MakeSetBlock(block, Block.Air));
                                        Vector3I removed;
                                        e.Player.FlyCache.TryRemove(block.ToString(), out removed);
                                    }
                                }
                            }
                        })); flyThread.Start();
                }
            }
            catch (Exception ex)
            {
                Logger.Log( LogType.Error, "FlyHandler.Player_Moved: " + ex);
            }
        }

        public void StartFlying(Player player)
        {
            player.IsFlying = true;
            player.FlyCache = new ConcurrentDictionary<string, Vector3I>();
        }

        public void StopFlying(Player player)
        {
            try
            {
                player.IsFlying = false;

                foreach (Vector3I block in player.FlyCache.Values)
                {
                    player.Send(PacketWriter.MakeSetBlock(block, Block.Air));
                }

                player.FlyCache = null;
            }
            catch (Exception ex)
            {
                Logger.Log( LogType.Error, "FlyHandler.StopFlying: " + ex);
            }
        }

        public static bool CanRemoveBlock(Player player, Vector3I block, Vector3I newPos)
        {
            int x = block.X - newPos.X;
            int y = block.Y - newPos.Y;
            int z = block.Z - newPos.Z;

            if (!(x >= -2 && x <= 2) || !(y >= -2 && y <= 2) || !(z >= -2 && z <= 2))
            {
                return true;
            }

            return false;
        }
    }
}