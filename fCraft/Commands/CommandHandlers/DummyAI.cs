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
            if (e.Player.World.Map.Dummys.Count() > 0)
            {
                foreach (Player d in e.Player.World.Map.Dummys)
                {
                    if (d.Info.IsFollowing)
                    {
                        if (d.Info.ID.ToString() == e.Player.Info.followingID)
                        {
                            Vector3I oldPos = new Vector3I(e.OldPosition.X, e.OldPosition.Y, e.OldPosition.Z);
                            Vector3I newPos = new Vector3I(e.NewPosition.X, e.NewPosition.Y, e.NewPosition.Z);
                            Packet packet = PacketWriter.MakeMoveRotate(d.Info.ID, new Position
                            {
                                X = (short)(newPos.X - oldPos.X),
                                Y = (short)(newPos.Y - oldPos.Y),
                                Z = (short)(newPos.Z - oldPos.Z),
                                R = (byte)Math.Abs(e.Player.Position.R),
                                L = (byte)Math.Abs(e.Player.Position.L)
                            }); ;

                            e.Player.World.Players.Send(packet);
                            d.Info.DummyPos = d.Position;
                        }
                    }
                }
            }
        }
        

        public static void DummyTurn(object sender, Events.PlayerMovingEventArgs e)
        {
            foreach (Player d in e.Player.World.Map.Dummys)
            {
                foreach (Player P in e.Player.World.Players)
                {
                    if (d.Info.Static)
                    {
                        Packet packet = PacketWriter.MakeMoveRotate(d.Info.ID, new Position
                        {
                            X = d.Position.X,
                            Y = d.Position.Y,
                            Z = d.Position.Z,
                            R = (byte)Math.Abs(P.Position.L * 2),
                            L = (byte)Math.Abs(P.Position.L * 2)
                        }); ;

                        P.Send(packet);
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
                    if (d.Info.DummyID.ToString() == e.Player.Info.followingID && !d.Info.Static)
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
