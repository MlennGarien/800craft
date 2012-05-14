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

//Copyright (C) <2012> Jon Baker(http://au70.net)
using System;
using System.Collections.Generic;

namespace fCraft.Drawing
{
    public sealed class WallsDrawOperation : DrawOperation
    {
        bool fillInner;

        public override string Name
        {
            get { return "Walls"; }
        }

        public override string Description
        {
            get { return Name; }
        }

        public WallsDrawOperation(Player player)
            : base(player){
        }


        public override bool Prepare(Vector3I[] marks)
        {
            if (!base.Prepare(marks)) return false;
            

            fillInner = Brush.HasAlternateBlock && Bounds.Width > 2 && Bounds.Length > 2 && Bounds.Height > 2;

            BlocksTotalEstimate = Bounds.Volume;
            if (!fillInner)
            {
                BlocksTotalEstimate -= Math.Max(0, Bounds.Width - 2) * Math.Max(0, Bounds.Length - 2) * Math.Max(0, Bounds.Height - 2);
            }

            coordEnumerator = BlockEnumerator().GetEnumerator();
            return true;
        }


        IEnumerator<Vector3I> coordEnumerator;
        public override int DrawBatch(int maxBlocksToDraw)
        {
            int blocksDone = 0;
            while (coordEnumerator.MoveNext())
            {
                Coords = coordEnumerator.Current;
                if (DrawOneBlock())
                {
                    blocksDone++;
                    if (blocksDone >= maxBlocksToDraw) return blocksDone;
                }
            }
            IsDone = true;
            return blocksDone;
        }

        //all works. Maybe look at Block estimation.
        IEnumerable<Vector3I> BlockEnumerator()
        {
            for (int x = Bounds.XMin; x <= Bounds.XMax; x++)
            {
                for (int z = Bounds.ZMin - 1; z < Bounds.ZMax; z++)
                {
                    yield return new Vector3I(x, Bounds.YMin, z + 1);
                    if (Bounds.YMin != Bounds.YMax)
                    {
                        yield return new Vector3I(x, Bounds.YMax, z + 1);
                    }
                }
                for (int y = Bounds.YMin; y < Bounds.YMax; y++)
                {
                    for (int z = Bounds.ZMin - 1; z < Bounds.ZMax; z++)
                    {
                        yield return new Vector3I(Bounds.XMin, y, z + 1);
                        if (Bounds.XMin != Bounds.XMax)
                        {
                            yield return new Vector3I(Bounds.XMax, y, z + 1);
                        }
                    }
                }
            }

            if (fillInner)
            {
                UseAlternateBlock = true;
                for (int x = Bounds.XMin + 1; x < Bounds.XMax; x++)
                {
                    for (int y = Bounds.YMin + 1; y < Bounds.YMax; y++)
                    {
                        for (int z = Bounds.ZMin; z < Bounds.ZMax + 1; z++)
                        {
                            yield return new Vector3I(x, y, z);
                        }
                    }
                }
            }
        }
    }
}
