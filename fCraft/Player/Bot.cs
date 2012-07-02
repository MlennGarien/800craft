using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
    public sealed partial class Bot
    {
        public string Name;
        public Position Pos;
        public int ID;
        public World world;
        public Bot(string name, Position pos, int iD, World world_){
            Name = Color.Gray + name;
            Pos = pos;
            ID = iD;
            world = world_;
        }

        public Map WorldMap
        {
            get
            {
                World world_ = world;
                return world_.LoadMap();
            }
        }
        public void SetBot(){
            world.Players.Send(PacketWriter.MakeAddEntity(this.ID, this.Name, this.Pos));
        }
        public void MakeRotate(){
            world.Players.Send(PacketWriter.MakeRotate(this.ID, new Position(Pos.X, Pos.Y, Pos.Z, (byte)(Pos.R - 90), Pos.L)));
            Pos = new Position(Pos.X, Pos.Y, Pos.Z, Pos.R, (byte)(Pos.L - 90));
        }
    }
    
}
