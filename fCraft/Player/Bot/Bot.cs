using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using fCraft.Events;

namespace fCraft
{
    public sealed partial class Bot
    {
        public string Name; //name of the bot
        public Position Pos; //bots current position
        public int ID; //ID of the bot, should not change
        public World world; //world the bot is on
        private bool _isMoving; //if the bot can move, can be changed if it is not boxed in ect
        private Position nextPos;

        public Bot(string name, Position pos, int iD, World world_)
        {
            Name = name; //bots should be gray :P
            Pos = pos;
            ID = iD;
            world = world_;
            _isMoving = true;
            SetBot();
            Scheduler.NewBackgroundTask(t => StartNewAIMovement()).RunForever(TimeSpan.FromMilliseconds(500));
        }

        public Map WorldMap
        {
            get
            {
                World world_ = world;
                return world_.LoadMap();
            }
        }
        // creates the bot. Need to add a list so bots can be shown to people 
        // reloading the world / joined after it was created
        public void SetBot()
        {
            world.Players.Send(PacketWriter.MakeAddEntity(this.ID, Color.Gray + this.Name, new Position(Pos.X, Pos.Y, Pos.Z, Pos.R, Pos.L)));
        }

        public void MoveBot()
        {
            Position oldPos = Pos; //curent pos
            Position delta = new Position //delta of new - old
            {
                X = (short)(nextPos.X - oldPos.X),
                Y = (short)(nextPos.Y - oldPos.Y),
                Z = (short)(nextPos.Z - oldPos.Z),
                R = (byte)Math.Abs(nextPos.R - oldPos.R),
                L = (byte)Math.Abs(nextPos.L - oldPos.L)
            };
            //set the packet
            Packet packet = PacketWriter.MakeMoveRotate(ID, new Position
            {
                X = delta.X,
                Y = delta.Y,
                Z = delta.Z,
                R = Pos.R,
                L = Pos.L
            });
            //send packet to everyone in the world
            world.Players.Send(packet);
            Pos = nextPos;
            world.Players.Message(Pos.ToBlockCoords().ToString());
        }
        public void CheckIfCanMove()
        {
            double ksi = 2.0 * Math.PI * (-Pos.L) / 256.0;
            double phi = 2.0 * Math.PI * (Pos.R - 64) / 256.0;
            double sphi = Math.Sin(phi);
            double cphi = Math.Cos(phi);
            double sksi = Math.Sin(ksi);
            double cksi = Math.Cos(ksi);
            Position movePos;
            Vector3I BlockPos = new Vector3I((int)(cphi * cksi * 1 - sphi * (0.5 + 1) - cphi * sksi * (0.5 + 1)),
										  (int)(sphi * cksi * 1 + cphi * (0.5 + 1) - sphi * sksi * (0.5 + 1)),
										  (int)(sksi * 1 + cksi * (0.5 + 1)));
            BlockPos += Pos.ToBlockCoords();
            movePos = new Position((short)(BlockPos.X * 32), (short)(BlockPos.Y *32), (short)(BlockPos.Z* 32), Pos.R, Pos.L);

            switch (world.Map.GetBlock(BlockPos))
            {
                case Block.Air:
                case Block.Water:
                case Block.Lava:
                case Block.Plant:
                case Block.RedFlower:
                case Block.RedMushroom:
                case Block.YellowFlower:
                case Block.BrownMushroom:
                    nextPos = movePos;
                    Server.Players.Message("Can Move");
                    MoveBot();
                    break;
                default:
                    Server.Players.Message(BlockPos.ToString());
                    Server.Players.Message(world.Map.GetBlock(BlockPos).ToString());
                   // Pos.L -= 45;
                    break;

            }
        }

        public void StartNewAIMovement()
        {
            if (!_isMoving)
            {
                return;
            }
            CheckIfCanMove();
        }
    }
}
