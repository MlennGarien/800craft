using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
    public interface Plugin
    {
        String Name { get; set; }
        String Version { get; set; }

        void Initialize();
    }
}
