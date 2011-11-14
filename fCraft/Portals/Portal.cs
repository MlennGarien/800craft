using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Portals
{
    public class Portal
    {
        public String Name { get; set; }
        public String Creator { get; set; }
        public DateTime Created { get; set; }
        public String World { get; set; }
        public Vector3I[] AffectedBlocks { get; set; }
        public PortalRange Range { get; set; }

        public Portal(String world, Vector3I[] affectedBlocks, String Name, String Creator)
        {
            this.World = world;
            this.AffectedBlocks = affectedBlocks;
            this.Range = Portal.CalculateRange(this);
            this.Name = Name;
            this.Creator = Creator;
            this.Created = DateTime.Now;
        }

        public Portal(String world, Vector3I[] affectedBlocks, String Name, String Creator, DateTime Created)
        {
            this.World = world;
            this.AffectedBlocks = affectedBlocks;
            this.Range = Portal.CalculateRange(this);
            this.Name = Name;
            this.Creator = Creator;
            this.Created = Created;
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
                    if (((player.Position.Z / 32)-1) <= Range.Zmax && ((player.Position.Z / 32)-1) >= Range.Zmin)
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

        public static String GenerateName(World world)
        {
            if (world.Portals != null)
            {
                if (world.Portals.Count > 0)
                {
                    bool found = false;
                    int portalID = 1;

                    while (!found)
                    {
                        bool taken = false;

                        foreach (Portal portal in world.Portals)
                        {
                            if (portal.Name.Equals("Portal" + portalID))
                            {
                                taken = true;
                                break;
                            }
                        }

                        if (!taken)
                        {
                            found = true;
                        }
                        else
                        {
                            portalID++;
                        }
                    }

                    return "Portal" + portalID;
                }
            }

            return "Portal1";
        }

        public static bool DoesNameExist(World world, String name)
        {
            if (world.Portals != null)
            {
                if (world.Portals.Count > 0)
                {
                    foreach (Portal portal in world.Portals)
                    {
                        if (portal.Name.Equals(name))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void Remove()
        {
            World world = WorldManager.FindWorldExact(World);

            for (int x = Range.Xmin; x <= Range.Xmax; x++)
            {
                for (int y = Range.Ymin; y <= Range.Ymax; y++)
                {
                    for (int z = Range.Zmin; z <= Range.Zmax; z++)
                    {
                        Vector3I block = new Vector3I(x, y, z);
                        BlockUpdate update = new BlockUpdate(null, block, Block.Air);
                        world.Map.QueueUpdate(update);
                    }
                }
            }

            world.Portals.Remove(this);
            PortalDB.Save();
        }
    }
}
