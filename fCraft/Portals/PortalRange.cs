using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Portals
{
    /// <summary>
    /// Class used for rapid check if user is in range of portal
    /// </summary>
    public class PortalRange
    {
        public int Xmin { get; set; }
        public int Xmax { get; set; }
        public int Ymin { get; set; }
        public int Ymax { get; set; }
        public int Zmin { get; set; }
        public int Zmax { get; set; }

        public PortalRange(int Xmin, int Xmax, int Ymin, int Ymax, int Zmin, int Zmax)
        {
            this.Xmin = Xmin;
            this.Xmax = Xmax;
            this.Ymin = Ymin;
            this.Ymax = Ymax;
            this.Zmin = Zmin;
            this.Zmax = Zmax;
        }
    }
}
