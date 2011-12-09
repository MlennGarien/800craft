using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LuaInterface;
using System.IO;

namespace fCraft
{
    class PluginManager
    {
        private static PluginManager instance;
        private PluginFunctions functions;
        private Lua lua;

        private PluginManager()
        {
            // Empty bitch
        }

        public static PluginManager GetInstance()
        {
            if (instance == null)
            {
                instance = new PluginManager();
                instance.Initialize();
            }

            return instance;
        }

        private void Initialize()
        {
            // Lua, you jelly?
            lua = new Lua();

            // Initialize functions
            functions = new PluginFunctions();

            // Register motherfucking functions
            //lua.RegisterFunction("GlennSays", functions, functions.GetType().GetMethod("GlennSays"));

            if (!Directory.Exists("plugins"))
            {
                Directory.CreateDirectory("plugins");
            }

            // Load plugins
            foreach (String file in Directory.GetFiles("plugins"))
            {
                Logger.Log(LogType.ConsoleOutput, "Loading plugin: " + file);
                lua.DoFile(file);
            }
        }
    }
}
