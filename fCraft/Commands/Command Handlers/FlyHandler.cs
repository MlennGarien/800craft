using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using fCraft.Collections;
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
                Player.PlacingBlock += new EventHandler<Events.PlayerPlacingBlockEventArgs>(Player_Clicked);
            }

            return instance;
        }
        private static void Player_Clicked(object sender, Events.PlayerPlacingBlockEventArgs e) //placing air
        {
            if (e.Player.IsFlying)
            {
                if (e.Player.FlyCache.Values.Contains(e.Coords))
                {
                    e.Result = CanPlaceResult.Revert; //nothing saves to blockcount or blockdb
                }
            }
        }

        private static void Player_Moved(object sender, Events.PlayerMovedEventArgs e)
        {
            try
            {
                if (e.Player.IsFlying)
                {
                    Vector3I oldPos = new Vector3I(e.OldPosition.X / 32, e.OldPosition.Y / 32, e.OldPosition.Z / 32);
                    Vector3I newPos = new Vector3I(e.NewPosition.X / 32, e.NewPosition.Y / 32, e.NewPosition.Z / 32);
                    //Checking e.Old vs e.New increases accuracy, checking old vs new uses a lot less updates
                    if ((oldPos.X != newPos.X) || (oldPos.Y != newPos.Y) || (oldPos.Z != newPos.Z))
                    {
                        //finally, /fly decends
                        if((e.OldPosition.Z > e.NewPosition.Z))
                        {
                            foreach (Vector3I block in e.Player.FlyCache.Values)
                            {
                                e.Player.Send(PacketWriter.MakeSetBlock(block, Block.Air));
                                Vector3I removed;
                                e.Player.FlyCache.TryRemove(block.ToString(), out removed);
                            }
                        }
                        // Create new block parts
                        for (int i = -1; i <= 1; i++) //reduced width and length by 1
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                for (int k = 2; k <= 3; k++) //added a 2nd layer
                                {
                                    Vector3I layer = new Vector3I(newPos.X + i, newPos.Y + j, newPos.Z - k);
                                    if (e.Player.World.Map.GetBlock(layer) == Block.Air)
                                    {
                                        e.Player.Send(PacketWriter.MakeSetBlock(layer, Block.Glass));
                                        e.Player.FlyCache.TryAdd(layer.ToString(), layer);
                                    }
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
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, "FlyHandler.Player_Moved: " + ex);
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

            if (!(x >= -1 && x <= 1) || !(y >= -1 && y <= 1) || !(z >= -3 && z <= 4))
            {
                return true;
            }
            if (!(x >= -1 && x <= 1) || !(y >= -1 && y <= 1) || !(z >= -3 && z <= 4))
            {
                return true;
            }
            return false;
        }
    }
}