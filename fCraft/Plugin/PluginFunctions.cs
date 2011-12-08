using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
    class PluginFunctions
    {
        public void GlennSays(string what)
        {
            Logger.Log(LogType.ConsoleOutput, "Glenn says " + what);
        }
    }
}
