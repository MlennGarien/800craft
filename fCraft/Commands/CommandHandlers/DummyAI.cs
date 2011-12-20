using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft;

namespace fCraft
{
    public class DummyAI
    {
        public static void DummyFollowing(object sender, Events.PlayerMovingEventArgs e)
        {
            foreach (Player d in e.Player.World.Map.Dummys)
            {
                if (d.Info.IsFollowing && d.Info.ID.ToString() == e.Player.Info.followingID)
                {
                    Vector3I oldPos = new Vector3I(e.OldPosition.X, e.OldPosition.Y, e.OldPosition.Z);
                    Vector3I newPos = new Vector3I(e.NewPosition.X, e.NewPosition.Y, e.NewPosition.Z);

                    if ((oldPos.X != newPos.X) || (oldPos.Y != newPos.Y) || (oldPos.Z != newPos.Z))
                    {
                        Position delta = new Position
                        {
                            X = (short)(newPos.X - oldPos.X),
                            Y = (short)(newPos.Y - oldPos.Y),
                            Z = (short)(newPos.Z - oldPos.Z),
                            R = (byte)Math.Abs(e.Player.Position.R),
                            L = (byte)Math.Abs(e.Player.Position.L)
                        };

                        Packet packet = PacketWriter.MakeMoveRotate(d.Info.ID, new Position
                        {
                            X = delta.X,
                            Y = delta.Y,
                            Z = delta.Z,
                            R = delta.R,
                            L = delta.L
                        }); ;

                        e.Player.World.Players.Send(packet);
                        d.Info.DummyPos = d.Position;
                    }
                }
            }
        }

        public static void Player_Disconnected(object sender, Events.PlayerDisconnectedEventArgs e)
        {
            if (e.Player.Info.IsFollowing)
            {
                foreach (Player d in e.Player.World.Map.Dummys)
                {
                    if (d.Info.ID.ToString() == e.Player.Info.followingID)
                    {
                        if (d.Info.IsFollowing)
                        {
                            d.Info.IsFollowing = false;
                            Logger.Log(LogType.SystemActivity, "Dummy '{0}' stopped following {1} (Disconnected)", d.Info.DummyName, e.Player.Name);
                        }
                    }
                }
            }
        }
    }
}
