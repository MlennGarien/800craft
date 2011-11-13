using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Portals
{
    class PortalHandler
    {
        private static PortalHandler instance;

        private PortalHandler()
        {
            // Empty, singleton
        }

        public static PortalHandler GetInstance()
        {
            if (instance == null)
            {
                instance = new PortalHandler();
                Player.Moved += new EventHandler<Events.PlayerMovedEventArgs>(Player_Moved);
            }

            return instance;
        }

        static void Player_Moved(object sender, Events.PlayerMovedEventArgs e)
        {
            if (e.Player.Can(Permission.UsePortal))
            {
                if (PortalHandler.GetInstance().GetPortal(e.Player) != null)
                {
                    Portal portal = PortalHandler.GetInstance().GetPortal(e.Player);
                    e.Player.Message("Portal found");
                }
            }
        }

        public Portal GetPortal(Player player)
        {
            Portal portal = null;

            try
            {
                if (player.World.Portals != null && player.World.Portals.Count > 0)
                {
                    lock (player.World.Portals.SyncRoot)
                    {
                        foreach (Portal possiblePortal in player.World.Portals)
                        {
                            if (possiblePortal.IsInRange(player))
                            {
                                return possiblePortal;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, "PortalHandler.GetPortal: " + ex);
            }

            return portal;
        }

        public static void CreatePortal(World world, Vector3I[] affectedBlocks)
        {
            Portal portal = new Portal(world, affectedBlocks);

            lock (world.Portals.SyncRoot)
            {
                world.Portals.Add(portal);
            }
        }
    }
}
