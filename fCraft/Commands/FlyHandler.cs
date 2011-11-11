using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
    internal class FlyHandler
    {
        // Methods
        public static void ClearCache(Player player, World world)
        {
            try
            {
                if (player.FlyCache.Count > 0)
                {
                    foreach (BlockUpdate update in player.FlyCache.Values)
                    {
                        BlockUpdate update2;
                        if (world.Map.GetBlock(update.X, update.Y, update.Z) == Block.Glass)
                        {
                            world.Map.QueueUpdate(new BlockUpdate(null, update.X, update.Y, update.Z, Block.Air));
                        }
                        player.FlyCache.TryRemove(string.Concat(new object[] { update.X, ":", update.Y, ":", update.Z }), out update2);
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Log( LogType.Error, "Unable to clear fly-cache: " + exception);
            }
        }

        public static void UpdateFly(Player player, Position oldPos, Position newPos)
        {
            try
            {
                if (player.IsFlying)
                {
                    short num = (short)(newPos.X / 0x20);
                    short num2 = (short)(newPos.Y / 0x20);
                    short num3 = (short)(newPos.Z / 0x20);
                    short num4 = (short)(oldPos.X / 0x20);
                    short num5 = (short)(oldPos.Y / 0x20);
                    short num6 = (short)(oldPos.Z / 0x20);
                    if (((num4 != num) || (num5 != num2)) || (num6 != num3))
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                if (player.World.Map.GetBlock((short)num + i, (short)num2 + j, num3 - 2) == Block.Air)
                                {
                                    //BlockUpdate update = new BlockUpdate(null, (short) num + i, (short) num2 + j, (short) num3 - 2, Block.Glass);
                                    //player.World.Map.QueueUpdate(update);
                                    if (!player.FlyCache.ContainsKey(string.Concat(new object[] { num + i, ":", num2 + j, ":", num3 - 2 })))
                                    {
                                        //player.FlyCache.TryAdd(string.Concat(new object[] { num + i, ":", num2 + j, ":", num3 - 2 }), update);
                                    }
                                }
                            }
                        }
                        if (player.FlyCache.Count > 0)
                        {
                            foreach (BlockUpdate update2 in player.FlyCache.Values)
                            {
                                if (((((((update2.X != num) || (update2.Y != num2)) || (update2.Z != (num3 - 2))) && (((update2.X != (num - 1)) || (update2.Y != num2)) || (update2.Z != (num3 - 2)))) && ((((update2.X != (num - 1)) || (update2.Y != (num2 - 1))) || (update2.Z != (num3 - 2))) && (((update2.X != num) || (update2.Y != (num2 - 1))) || (update2.Z != (num3 - 2))))) && (((((update2.X != (num + 1)) || (update2.Y != num2)) || (update2.Z != (num3 - 2))) && (((update2.X != (num + 1)) || (update2.Y != (num2 + 1))) || (update2.Z != (num3 - 2)))) && ((((update2.X != num) || (update2.Y != (num2 + 1))) || (update2.Z != (num3 - 2))) && (((update2.X != (num - 1)) || (update2.Y != (num2 + 1))) || (update2.Z != (num3 - 2)))))) && (((update2.X != (num + 1)) || (update2.Y != (num2 - 1))) || (update2.Z != (num3 - 2))))
                                {
                                    BlockUpdate update3;
                                    player.World.Map.QueueUpdate(new BlockUpdate(null, update2.X, update2.Y, update2.Z, Block.Air));
                                    player.FlyCache.TryRemove(string.Concat(new object[] { update2.X, ":", update2.Y, ":", update2.Z }), out update3);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Log( LogType.Error, "Unable to update fly position: " + exception);
            }
        }
    }
}
