//Copyright (C) <2011 - 2013>  <Jon Baker, Glenn Mariën and Lao Tszy>

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.

//Copyright (C) <2011 - 2013> Glenn Mariën (http://project-vanilla.com)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Security.Policy;

namespace fCraft
{
    class PluginManager
    {
        private static PluginManager instance;
        public List<Plugin> Plugins = new List<Plugin>();

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
            try
            {
                if (!Directory.Exists("plugins"))
                {
                    Directory.CreateDirectory("plugins");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, "PluginManager.Initialize: " + ex);
                return;
            }

            // Load plugins
            String[] plugins = Directory.GetFiles("plugins", "*.dll");

            if (plugins.Length == 0)
            {
                Logger.Log(LogType.ConsoleOutput, "PluginManager: No plugins found");
                return;
            }
            else
            {
                Logger.Log(LogType.ConsoleOutput, "PluginManager: Loading " + plugins.Length + " plugins");

                foreach (String plugin in plugins)
                {
                    try
                    {
                    	object instance = null;
                        Assembly lib = null;
                        
                        //Use a memorystream to load the DLL so windows doesnt lock the file after unload
                        using (FileStream fs = File.Open(plugin, FileMode.Open)) 
                        {
                        	using (MemoryStream ms = new MemoryStream())
                        	{
                        		byte[] buffer = new byte[1024];
                        		int read = 0;
                        		while ((read = fs.Read(buffer, 0, 1024)) > 0)
                        			ms.Write(buffer, 0, read);
                        		lib = Assembly.Load(ms.ToArray());
                        		ms.Close();
                        	}
                        	fs.Close();
                        }
                        
                        try
                        {
                        	//Loop through all the types in the DLL so we load all plugins within the DLL.
                        	//This could also be used to find and load one plugin, just break after finding and adding the plugin
                        	foreach (Type t in lib.GetTypes())
                        	{
                        		if (t.BaseType == typeof(Plugin))
                        		{
                        			instance = Activator.CreateInstance(t);
                        			Plugins.Add((Plugin)instance);
                        		}
                        	}
                        }
                        catch (Exception ex) 
                        { 
                        	//TODO Print an error..?
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogType.Error, "PluginManager: Unable to load plugin at location " + plugin + ": " + ex);
                    }
                }
            }

            LoadPlugins();
        }

        private void LoadPlugins()
        {
            if (Plugins.Count > 0)
            {
                foreach (Plugin plugin in Plugins)
                {
                    Logger.Log(LogType.ConsoleOutput, "PluginManager: Loading plugin " + plugin.Name);

                    try
                    {
                        plugin.Initialize();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogType.Error, "PluginManager: Failed loading plugin " + plugin.Name + ": " + ex);
                    }
                }
            }
        }
    }
}
