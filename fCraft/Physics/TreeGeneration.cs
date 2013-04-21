/*        ----
        Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com>
        All rights reserved.

        Redistribution and use in source and binary forms, with or without
        modification, are permitted provided that the following conditions are met:
         * Redistributions of source code must retain the above copyright
              notice, this list of conditions and the following disclaimer.
            * Redistributions in binary form must reproduce the above copyright
             notice, this list of conditions and the following disclaimer in the
             documentation and/or other materials provided with the distribution.
            * Neither the name of 800Craft or the names of its
             contributors may be used to endorse or promote products derived from this
             software without specific prior written permission.

        THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
        ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
        WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
        DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
        DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
        (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
        LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
        ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
        (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
        SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
        ----*/
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

