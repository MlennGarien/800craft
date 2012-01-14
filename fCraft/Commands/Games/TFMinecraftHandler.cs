using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
    public static class TFMinecraftHandler
    {
       public static void BaseAdd(Player player, Vector3I[] marks, object tag)
        {
            int sx = Math.Min(marks[0].X, marks[1].X);
            int ex = Math.Max(marks[0].X, marks[1].X);
            int sy = Math.Min(marks[0].Y, marks[1].Y);
            int ey = Math.Max(marks[0].Y, marks[1].Y);
            int sh = Math.Min(marks[0].Z, marks[1].Z);
            int eh = Math.Max(marks[0].Z, marks[1].Z);

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1); //just incase

            Zone Base = (Zone)tag;
            Base.Create(new BoundingBox(marks[0], marks[1]), player.Info);
            player.WorldMap.Zones.Add(Base);
            player.Message("Base created: {0}x{1}x{2}", Base.Bounds.Dimensions.X,
                                                        Base.Bounds.Dimensions.Y,
                                                        Base.Bounds.Dimensions.Z);
           //setting up game tasks
           if(!player.WorldMap.Zones.Contains("redbase2")){
           RedBase2(player);
               return;
           }
           if(!player.WorldMap.Zones.Contains("redbase3")){
           RedBase3(player);
               return;
           }
           if(!player.WorldMap.Zones.Contains("bluebase1")){
           BlueBase1(player);
               return;
           }
           if (!player.WorldMap.Zones.Contains("bluebase2")){
               BlueBase2(player);
               return;
           }
           if(!player.WorldMap.Zones.Contains("bluebase3")){
           BlueBase3(player);
               return;
           } 
        }

       static void RedBase2(Player player)
       {
           Zone BaseRed2 = new Zone();
           BaseRed2.Name = "redbase2";
           player.SelectionStart(2, TFMinecraftHandler.BaseAdd, BaseRed2, WorldCommands.CdBase.Permissions);
           player.Message("Red Base 2: Place 2 blocks to cuboid a Red base");
       }

       static void RedBase3(Player player)
       {
           Zone BaseRed3 = new Zone();
           BaseRed3.Name = "redbase3";
           player.SelectionStart(2, TFMinecraftHandler.BaseAdd, BaseRed3, WorldCommands.CdBase.Permissions);
           player.Message("Red Base 3: Place 2 blocks to cuboid a Red base");
       }

       static void BlueBase1(Player player)
       {
           Zone BaseBlue1 = new Zone();
           BaseBlue1.Name = "bluebase1";
           player.SelectionStart(2, TFMinecraftHandler.BaseAdd, BaseBlue1, WorldCommands.CdBase.Permissions);
           player.Message("Blue Base 1: Place 2 blocks to cuboid a Blue base");
       }

       static void BlueBase2(Player player)
       {
           Zone BaseBlue2 = new Zone();
           BaseBlue2.Name = "bluebase2";
           player.SelectionStart(2, TFMinecraftHandler.BaseAdd, BaseBlue2, WorldCommands.CdBase.Permissions);
           player.Message("Blue Base 2: Place 2 blocks to cuboid a Blue base");
       }

       static void BlueBase3(Player player)
       {
           Zone BaseBlue3 = new Zone();
           BaseBlue3.Name = "redbase3";
           player.SelectionStart(2, TFMinecraftHandler.BaseAdd, BaseBlue3, WorldCommands.CdBase.Permissions);
           player.Message("Blue Base 3: Place 2 blocks to cuboid a Blue base");
       }
    }
}
