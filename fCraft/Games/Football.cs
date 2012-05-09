using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft;
using fCraft.Events;
using System.Collections.Concurrent;
using System.Threading;

namespace fCraft.Games
{
    public class Football
    {
        public Vector3I footballPos;
        public World world;
        public Thread FootThread;
        public Football(Player player, World world_, Vector3I FootballPos)
        {
            footballPos = FootballPos;
            world = world_;
            Player.Clicked += ClickedFootball;
            drawFootball(footballPos);
        }

        public void ClickedFootball(object sender, PlayerClickedEventArgs e)
        {
            Position p = e.Player.Position;
            if (e.Coords == footballPos)
            {
                double rSin = Math.Sin(((double)(128 - p.R) / 255) * 2 * Math.PI);
                double rCos = Math.Cos(((double)(128 - p.R) / 255) * 2 * Math.PI);

                ConcurrentDictionary<String, Vector3I> balls = new ConcurrentDictionary<String, Vector3I>();
                FootThread = new Thread(new ThreadStart(delegate
                {
                    int startX = footballPos.X;
                    int startY = footballPos.Y;
                    int startZ = footballPos.Z;

                    Position pos = e.Player.Position;
                    pos.R = e.Player.Position.R;
                    for (int startB = 1; startB <= new Random().Next(4, 15); startB++)
                    {
                        if (world.Map != null && world.IsLoaded)
                        {
                            pos.X = (short)Math.Round((startX + (double)(rSin * startB)));
                            pos.Y = (short)Math.Round((startY + (double)(rCos * startB)));
                            pos.Z = (short)footballPos.Z;
                            if (world.Map.GetBlock(pos.X, pos.Y, pos.Z) != Block.Air)
                            {
                                break; //needs bounce... hmmm
                            }
                            foreach (Vector3I bp in balls.Values)
                            {
                                world.Map.QueueUpdate(new BlockUpdate(null,
                                    (short)bp.X,
                                    (short)bp.Y,
                                    (short)bp.Z,
                                    Block.Air));
                                Vector3I removed;
                                balls.TryRemove(bp.ToString(), out removed);
                            }
                            balls.TryAdd(new Vector3I(pos.X, pos.Y, pos.Z).ToString(), new Vector3I(pos.X, pos.Y, pos.Z));
                            drawFootball(new Vector3I(pos.X, pos.Y, pos.Z));
                            Thread.Sleep(80);
                        }
                    }
                })); FootThread.Start();
            }
        }

        public void drawFootball(Vector3I newPos)
        {
            world.Map.QueueUpdate(new BlockUpdate(null, newPos, Block.White));
            footballPos = newPos;
        }
    }
}
