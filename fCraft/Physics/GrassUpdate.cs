using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Physics
{
    class GrassUpdate
    {
        public World World { get; set; }
        public Vector3I Block { get; set; }
        public DateTime Scheduled { get; set; }

        public GrassUpdate(World World, Vector3I Block, DateTime Scheduled)
        {
            this.World = World;
            this.Block = Block;
            this.Scheduled = Scheduled;
        }
    }
}
