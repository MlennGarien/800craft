using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using ServiceStack.Text;
using System.Collections;

namespace fCraft.Portals
{
    public class PortalDB
    {
        private static TimeSpan SaveInterval = TimeSpan.FromSeconds(90);
        private static readonly object SaveLoadLock = new object();

        public Portal[] Cache { get; private set; }


        public static void Save()
        {
            try
            {
                lock (SaveLoadLock)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    int worlds = 0;
                    int portals = 0;

                    using (FileStream fs = new FileStream(Paths.PortalDBFileName, FileMode.OpenOrCreate))
                    {
                        StringBuilder data = new StringBuilder();
                        World[] worldsCopy = WorldManager.WorldList;

                        foreach (World world in worldsCopy)
                        {
                            if (world.Portals != null)
                            {
                                ArrayList portalsCopy = world.Portals;
                                worlds++;

                                foreach (Portal portal in portalsCopy)
                                {
                                    portals++;
                                    
                                    data.AppendLine(JsonSerializer.SerializeToString(portal));
                                }
                            }
                        }

                        byte[] dataToWrite = Encoding.UTF8.GetBytes(data.ToString().ToCharArray(), 0, data.ToString().ToCharArray().Length);
                        fs.Write(dataToWrite, 0, dataToWrite.Length);
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

        public static void StartSaveTask()
        {
            SchedulerTask saveTask = Scheduler.NewBackgroundTask(delegate { Save(); }).RunForever(SaveInterval, TimeSpan.FromSeconds(15));
        }
    }
}
