using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Drawing
{
    class TreeDrawOperation : DrawOperation
    {
        public override int ExpectedMarks
        {
            get { return 1; }
        }

        public TreeDrawOperation(Player player)
            : base(player)
        {
            // Empty
        }

        public override string Name
        {
            get { throw new NotImplementedException(); }
        }

        public override int DrawBatch(int maxBlocksToDraw)
        {
            throw new NotImplementedException();
        }
    }
}
