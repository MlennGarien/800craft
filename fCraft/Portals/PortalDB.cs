using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using ServiceStack.Text;
using System.Collections;
using System.Runtime.Serialization.Json;

namespace fCraft.Portals
{
    public class PortalDB
    {
        private static TimeSpan SaveInterval = TimeSpan.FromSeconds(90);
        private static readonly object SaveLoadLock = new object();

        public static void Save()
        {
            try
            {
                lock (SaveLoadLock)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    int worlds = 0;
                    int portals = 0;

                    using (StreamWriter fs = new StreamWriter(Paths.PortalDBFileName, false))
                    {
                        ArrayList portalsList = new ArrayList();
                        World[] worldsCopy = WorldManager.Worlds;

                        foreach (World world in worldsCopy)
                        {
                            if (world.Portals != null)
                            {
                                ArrayList portalsCopy = world.Portals;
                                worlds++;

                                foreach (Portal portal in portalsCopy)
                                {
                                    portals++;
                                    fs.WriteLine(JsonSerializer.SerializeToString(portal));
                                }
                            }
                        }

                        fs.Flush();
                        fs.Close();
                    }

                    stopwatch.Stop();

                    Logger.Log(LogType.SystemActivity, "PortalDB.Save: Saved {0} portal(s) of {1} world(s) in {2}ms", portals, worlds, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, "PortalDB.Save: " + ex);
            }
        }

        public static void Load()
        {
            try
            {
                lock (SaveLoadLock)
                {
                    using (StreamReader fs = new StreamReader(Paths.PortalDBFileName))
                    {
                        String line;
                        int count = 0;

                        while ((line = fs.ReadLine()) != null)
                        {
                            Portal portal = (Portal)JsonSerializer.DeserializeFromString(line, typeof(Portal));
                            World world = WorldManager.FindWorldExact(portal.Place);

                            if (world.Portals == null)
                            {
                                world.Portals = new ArrayList();
                            }

                            lock (world.Portals.SyncRoot)
                            {
                                world.Portals.Add(portal);
                            }

                            count++;
                        }

                        if (count > 0)
                        {
                            Logger.Log(LogType.SystemActivity, "PortalDB.Load: Loaded " + count + " portals");
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                Logger.Log(LogType.Warning, "PortalDB file does not exist.");
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, "PortalDB.Load: " + ex);
            }
        }

        public static void StartSaveTask()
        {
            SchedulerTask saveTask = Scheduler.NewBackgroundTask(delegate { Save(); }).RunForever(SaveInterval, SaveInterval + TimeSpan.FromSeconds(15));
        }
    }
}
