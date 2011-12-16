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
                        Type pluginType = null;
                        String args = plugin.Substring(plugin.LastIndexOf("\\") + 1, plugin.IndexOf(".dll") - plugin.LastIndexOf("\\") - 1);
                        Assembly assembly = Assembly.LoadFile(Path.GetFullPath(plugin));

                        if (assembly != null)
                        {
                            pluginType = assembly.GetType(args + ".Init");

                            if (pluginType != null)
                            {
                                Plugins.Add((Plugin)Activator.CreateInstance(pluginType));
                            }
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
