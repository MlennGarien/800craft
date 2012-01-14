using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;

namespace fCraft
{
    public static class GameEvents
    {
        public static int ClickCount = 0;
        static readonly object Red1Lock = new object();
        public static void PlayerClicked(object sender, PlayerClickedEventArgs e)
        {
            if (GameManager.GameIsOn)
            {
                if (e.Player.World.Equals( GameManager.GameWorld ))
                {
                    Zone[] allowed, denied;
                    if (e.Player.WorldMap.Zones.CheckDetailed(e.Coords, e.Player, out allowed, out denied))
                    {
                        foreach (Zone zone in allowed)
                        {
                            if (zone.Name.EndsWith("redbase1"))
                            {
                                BlockUpdate update = new BlockUpdate(null, e.Coords, Block.Red);
                                e.Player.World.Map.QueueUpdate(update);
                                ClickCount++;
                                e.Player.Message("{0}", ClickCount);
                                if (ClickCount == 15)
                                {
                                    CaptureBase(zone, e.Player);
                                    e.Player.Message("yes");
                                }
                            }
                        }
                    }
                }
            }
        }

        static void CaptureBase(Zone zone, Player player)
        {
            int sx = zone.Bounds.XMin;
            int ex = zone.Bounds.XMax;
            int sy = zone.Bounds.YMin;
            int ey = zone.Bounds.YMax;
            int sz = zone.Bounds.ZMin;
            int ez = zone.Bounds.ZMax;

            Block[] buffer = new Block[zone.Bounds.Volume];

            int counter = 0;
            for (int x = sx; x <= ex; x++)
            {
                for (int y = sy; y <= ey; y++)
                {
                    for (int z = sz; z <= ez; z++)
                    {
                        buffer[counter] = player.WorldMap.GetBlock(x, y, z);
                        player.WorldMap.QueueUpdate(new BlockUpdate(null, new Vector3I(x, y, z), Block.Blue));
                        counter++;
                    }
                }
            }
        }
    }
}

