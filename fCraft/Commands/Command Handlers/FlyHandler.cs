//Copyright (C) <2012>  <Jon Baker, Glenn Mariën and Lao Tszy>

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.

//Copyright (C) <2012> Glenn Mariën (http://project-vanilla.com) and Jon Baker (http://au70.net)

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
                Player.PlacingBlock += new EventHandler<Events.PlayerPlacingBlockEventArgs>(Player_Clicked);
            }

            return instance;
        }
        private static void Player_Clicked(object sender, Events.PlayerPlacingBlockEventArgs e) //placing air
        {
            if (e.Player.IsFlying)
            {
                if (e.Context == BlockChangeContext.Manual)//ignore all other things
                {
                    if (e.Player.FlyCache.Values.Contains(e.Coords))
                    {
                        e.Result = CanPlaceResult.Revert; //nothing saves to blockcount or blockdb
                    }
                }
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