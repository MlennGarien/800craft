using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        private static void Player_Moved(object sender, Events.PlayerMovedEventArgs e)
        {
            try
            {
                if (e.Player.IsFlying)
                {
                    // We need to have block positions, so we divide by 32
                    Vector3I oldPos = new Vector3I(e.OldPosition.X / 32, e.OldPosition.Y / 32, e.OldPosition.Z / 32);
                    Vector3I newPos = new Vector3I(e.NewPosition.X / 32, e.NewPosition.Y / 32, e.NewPosition.Z / 32);

                    // Check if the player actually moved and not just rotated
                    if ((oldPos.X != newPos.X) || (oldPos.Y != newPos.Y) || (oldPos.Z != newPos.Z))
                    {
                        // Thread safety
                        lock (e.Player.FlyLock)
                        {
                            int count = 0;

                            // Create new blocks part
                            for (int i = -2; i <= 2; i++)
                            {
                                for (int j = -2; j <= 2; j++)
                                {
                                    e.Player.NewFlyCache[count] = new Vector3I(newPos.X + i, newPos.Y + j, newPos.Z - 2);

                                    if (e.Player.World.Map.GetBlock(e.Player.NewFlyCache[count]) == Block.Air)
                                    {
                                        BlockUpdate magicCarpetBlock = new BlockUpdate(null, (short)e.Player.NewFlyCache[count].X, (short)e.Player.NewFlyCache[count].Y, (short)e.Player.NewFlyCache[count].Z, Block.Glass);
                                        e.Player.World.Map.QueueUpdate(magicCarpetBlock);
                                    }

                                    count++;
                                }
                            }

                            // Remove old blocks
                            if (e.Player.OldFlyCache.Length > 0)
                            {
                                foreach (Vector3I oldMagicCarpetBlock in e.Player.OldFlyCache)
                                {
                                    bool markedForDeletion = true;

                                    foreach (Vector3I newMagicCarpetBlock in e.Player.NewFlyCache)
                                    {
                                        if (oldMagicCarpetBlock.X == newMagicCarpetBlock.X && oldMagicCarpetBlock.Y == newMagicCarpetBlock.Y && oldMagicCarpetBlock.Z == newMagicCarpetBlock.Z)
                                        {
                                            markedForDeletion = false;
                                            // Break loop as we found our match
                                            break;
                                        }
                                    }

                                    if (markedForDeletion)
                                    {
                                        if (e.Player.World.Map.GetBlock(oldMagicCarpetBlock) == Block.Glass)
                                        {
                                            BlockUpdate oldMagicCarpetDeleteBlock = new BlockUpdate(null, (short)oldMagicCarpetBlock.X, (short)oldMagicCarpetBlock.Y, (short)oldMagicCarpetBlock.Z, Block.Air);
                                            e.Player.World.Map.QueueUpdate(oldMagicCarpetDeleteBlock);
                                        }
                                    }
                                }
                            }

                            // Flip caches
                            e.Player.OldFlyCache = e.Player.NewFlyCache;
                            e.Player.NewFlyCache = new Vector3I[25];
                        }
                    }
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
            player.NewFlyCache = new Vector3I[25];
            player.OldFlyCache = new Vector3I[25];
        }

        public void StopFlying(Player player)
        {
            try
            {
                player.IsFlying = false;
                player.NewFlyCache = null;

                foreach (Vector3I block in player.OldFlyCache)
                {
                    if (player.World.Map.GetBlock(block) == Block.Glass)
                    {
                        BlockUpdate removeBlock = new BlockUpdate(null, (short)block.X, (short)block.Y, (short)block.Z, Block.Air);
                        player.World.Map.QueueUpdate(removeBlock);
                    }
                }

                player.OldFlyCache = null;
            }
            catch (Exception ex)
            {
                Logger.Log( LogType.Error, "FlyHandler.StopFlying: " + ex);
            }
        }
    }
}