using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Events
{
    class FeedEvents
    {
        public static void PlayerJoiningWorld(object sender, PlayerJoinedWorldEventArgs e)
        {
            foreach (FeedData data in FeedData.FeedList.Where(f => !f.started))
            {
                if (data.world.Name == e.NewWorld.Name)
                {
                    data.Start();
                }
            }
        }
    }
}