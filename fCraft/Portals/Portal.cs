using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Portals
{
    public class Portal
    {
        public World World { get; set; }
        public Vector3I[] AffectedBlocks { get; set; }
        public PortalRange Range { get; set; }

        public Portal(World world, Vector3I[] affectedBlocks)
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
                if (block.X < portal.Range.Xmin)
                {
                    range.Xmin = block.X;
                }

                if (block.X > portal.Range.Xmax)
                {
                    range.Xmax = block.X;
                }

                if (block.Y < portal.Range.Ymin)
                {
                    range.Ymin = block.Y;
                }

                if (block.Y > portal.Range.Ymax)
                {
                    range.Ymax = block.Y;
                }

                if (block.Z < portal.Range.Zmin)
                {
                    range.Zmin = block.Z;
                }

                if (block.Z > portal.Range.Zmax)
                {
                    range.Zmax = block.Z;
                }
            }

            return range;
        }

        public bool IsInRange(Player player)
        {
            if (player.Position.X <= Range.Xmax && player.Position.X >= Range.Xmin)
            {
                if (player.Position.Y <= Range.Ymax && player.Position.Y >= Range.Ymin)
                {
                    if (player.Position.Z <= Range.Zmax && player.Position.Z >= Range.Zmin)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
    }
}
