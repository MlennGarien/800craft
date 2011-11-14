using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Portals
{
    public class Portal
    {
        public String World { get; set; }
        public Vector3I[] AffectedBlocks { get; set; }
        public PortalRange Range { get; set; }

        public Portal(String world, Vector3I[] affectedBlocks)
        {
            this.World = world;
            this.AffectedBlocks = affectedBlocks;
            this.Range = Portal.CalculateRange(this);
        }

        public static PortalRange CalculateRange(Portal portal)
        {
            PortalRange range = new PortalRange(0, 0, 0, 0, 0, 0);

            foreach (Vector3I block in portal.AffectedBlocks)
            {
                if (range.Xmin == 0)
                {
                    range.Xmin = block.X;
                }
                else
                {
                    if (block.X < range.Xmin)
                    {
                        range.Xmin = block.X;
                    }
                }

                if (range.Xmax == 0)
                {
                    range.Xmax = block.X;
                }
                else
                {
                    if (block.X > range.Xmax)
                    {
                        range.Xmax = block.X;
                    }
                }

                if (range.Ymin == 0)
                {
                    range.Ymin = block.Y;
                }
                else
                {
                    if (block.Y < range.Ymin)
                    {
                        range.Ymin = block.Y;
                    }
                }

                if (range.Ymax == 0)
                {
                    range.Ymax = block.Y;
                }
                else
                {
                    if (block.Y > range.Ymax)
                    {
                        range.Ymax = block.Y;
                    }
                }

                if (range.Zmin == 0)
                {
                    range.Zmin = block.Z;
                }
                else
                {
                    if (block.Z < range.Zmin)
                    {
                        range.Zmin = block.Z;
                    }
                }

                if (range.Zmax == 0)
                {
                    range.Zmax = block.Z;
                }
                else
                {
                    if (block.Z > range.Zmax)
                    {
                        range.Zmax = block.Z;
                    }
                }
            }

            return range;
        }

        public bool IsInRange(Player player)
        {
            if ((player.Position.X / 32) <= Range.Xmax && (player.Position.X / 32) >= Range.Xmin)
            {
                if ((player.Position.Y / 32) <= Range.Ymax && (player.Position.Y / 32) >= Range.Ymin)
                {
                    if ((player.Position.Z / 32) <= Range.Zmax && (player.Position.Z / 32) >= Range.Zmin)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        public bool IsInRange(Vector3I vector)
        {
            if (vector.X <= Range.Xmax && vector.X >= Range.Xmin)
            {
                if (vector.Y <= Range.Ymax && vector.Y >= Range.Ymin)
                {
                    if (vector.Z <= Range.Zmax && vector.Z >= Range.Zmin)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
