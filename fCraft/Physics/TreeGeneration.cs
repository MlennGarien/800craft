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

// Code from Forester script by dudecon
// Original: http://www.minecraftforum.net/viewtopic.php?f=25&t=9426
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;
using fCraft;

namespace fCraft
{
    public static class TreeGeneration
    {
        public static Random Rand = new Random();

        public static void MakeNormalFoliage(World w, Vector3I Pos, int Height)
        {

            int topy = Pos.Z + Height - 1;
            int start = topy - 2;
            int end = topy + 2;

            for (int y = start; y < end; y++)
            {
                int rad;
                if (y > start + 1)
                {
                    rad = 1;
                }
                else
                {
                    rad = 2;
                }
                for (int xoff = -rad; xoff < rad + 1; xoff++)
                {
                    for (int zoff = -rad; zoff < rad + 1; zoff++)
                    {
                        if (w.Map != null && w.IsLoaded)
                        {
                            if (Rand.NextDouble() > .618 &&
                                Math.Abs(xoff) == Math.Abs(zoff) &&
                                Math.Abs(xoff) == rad)
                            {
                                continue;
                            }
                            w.Map.QueueUpdate(new
                                 BlockUpdate(null, (short)(Pos.X + xoff), (short)(Pos.Y + zoff), (short)y, Block.Leaves));
                        }
                    }
                }
            }
        }


        public static void MakePalmFoliage(World world, Vector3I Pos, int Height)
        {
            if (world.Map != null && world.IsLoaded)
            {
                int z = Pos.Z + Height;
                for (int xoff = -2; xoff < 3; xoff++)
                {
                    for (int yoff = -2; yoff < 3; yoff++)
                    {
                        if (Math.Abs(xoff) == Math.Abs(yoff))
                        {
                            if (world.Map != null && world.IsLoaded)
                            {
                                world.Map.QueueUpdate(new BlockUpdate(null, (short)(Pos.Z + xoff), (short)(Pos.Y + yoff), (short)z, Block.Leaves));
                            }
                        }
                    }
                }
            }
        }
    }
}

