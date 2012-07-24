using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
    class BotHelper
    {
        public bool CanWalkThrough(Block block)
        {
            switch (block)
            {
                case Block.Air:
                case Block.Lava:
                case Block.Water:
                case Block.StillLava:
                case Block.StillWater:
                case Block.RedFlower:
                case Block.YellowFlower:
                case Block.RedMushroom:
                case Block.BrownMushroom:
                case Block.Plant:
                    return true;
                default: 
                    return false;
            }
        }
    }
}
