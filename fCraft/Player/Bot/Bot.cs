using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace fCraft
{
    public sealed partial class Bot
    {
        public string Name; //name of the bot
        public Position Pos; //bots current position
        public int ID; //ID of the bot, should not change
        public World world; //world the bot is on
        private Thread _botThread; //bot thread
        private bool _isMoving; //if the bot can move, can be changed if it is not boxed in ect
        private Direction _direction; //direction the bot is facing, 0 = north

        public Bot(string name, Position pos, int iD, World world_)
        {
            Name = Color.Gray + name; //bots should be gray :P
            Pos = pos;
            ID = iD;
            world = world_;
            _isMoving = false;
            _direction = Direction.North; //start off at north
            StartNewAIMovement(); //start the while loop in a new thread. I want to change the way the thread works
            //its only like this for testing purposes
        }

        public enum Direction
        {
            North,
            South,
            East,
            West
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
            world.Players.Send(PacketWriter.MakeAddEntity(this.ID, this.Name, this.Pos));
        }
        //may not needs these rotate methods
        public void MakeRotLeft()
        {
            world.Players.Send(PacketWriter.MakeRotate(this.ID, new Position(Pos.X, Pos.Y, Pos.Z, (byte)(Pos.R - 90), Pos.L)));
            Pos = new Position(Pos.X, Pos.Y, Pos.Z, Pos.R, (byte)(Pos.L - 90));
        }
        public void MakeRotRight()
        {
            world.Players.Send(PacketWriter.MakeRotate(this.ID, new Position(Pos.X, Pos.Y, Pos.Z, (byte)(Pos.R + 90), Pos.L)));
            Pos = new Position(Pos.X, Pos.Y, Pos.Z, Pos.R, (byte)(Pos.L + 90));
        }
        //makes the bot walk in the north direction (-32 on the Y. -32 because block size)
        public void WalkNorth()
        {
            Pos.L = 0; //face north
            Position oldPos = Pos; //curent pos
            Position newPos = new Position(oldPos.X, (short)(oldPos.Y - 32), oldPos.Z, oldPos.R, oldPos.L); //desired pos
            Position delta = new Position //delta of new - old
            {
                X = (short)(newPos.X - oldPos.X),
                Y = (short)(newPos.Y - oldPos.Y),
                Z = (short)(newPos.Z - oldPos.Z),
                R = (byte)Math.Abs(newPos.R - oldPos.R),
                L = (byte)Math.Abs(newPos.L - oldPos.L)
            };
             //set the packet
            Packet packet = PacketWriter.MakeMoveRotate(ID, new Position
            {
                X = delta.X,
                Y = delta.Y,
                Z = delta.Z,
                R = newPos.R,
                L = newPos.L
            });
            //send packet to everyone in the world
            world.Players.Send(packet);
            Pos = new Position
            {
                X = delta.X,
                Y = delta.Y,
                Z = delta.Z,
                R = newPos.R,
                L = newPos.L
            }; //set the bots newposition
        }

        public void StartNewAIMovement()
        {
            _botThread = new Thread(new ThreadStart(delegate
            {
                _isMoving = true;
                while (_isMoving)
                {
                    if (_direction == Direction.North){
                        WalkNorth();
                    }
                    Thread.Sleep(350);
                }

            }));
            _botThread.Start();
        }
    }
    
}
