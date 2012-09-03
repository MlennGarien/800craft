using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
    public class DirectionFinder
    {
        public static Direction GetDirection(Vector3I[] marks)
        {
            if (Math.Abs(marks[1].X - marks[0].X) > Math.Abs(marks[1].Y - marks[0].Y))
            {
                if (marks[0].X < marks[1].X)
                {
                    return Direction.one;
                }
                else
                {
                    return Direction.two;
                }
            }
            else if (Math.Abs(marks[1].X - marks[0].X) < Math.Abs(marks[1].Y - marks[0].Y))
            {
                if (marks[0].Y < marks[1].Y)
                {
                    return Direction.three;
                }
                else
                {
                    return Direction.four;
                }
            }
            else
                return Direction.Null;
        }
    }
}
