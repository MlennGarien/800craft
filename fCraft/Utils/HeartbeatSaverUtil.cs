using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;
using System.Diagnostics;
using System.IO;

namespace fCraft
{
    class HeartbeatSaverUtil
    {
        public static void Init()
        {
            EventHandler<CrashedEventArgs> Crash = new EventHandler<CrashedEventArgs>(HbCrashEvent);
        }
        public static void HbCrashEvent(object sender, CrashedEventArgs e)
        {
            if (e.ShutdownImminent.Equals(true))
            {
                if (ConfigKey.HbSaverKey.Enabled())
                {
                    if (ConfigKey.HbSaverKey.Enabled())
                    {
                        if (!File.Exists("heartbeatsaver.exe"))
                        {
                            Logger.Log(LogType.Warning, "heartbeatsaver.exe does not exist and failed to launch");
                            return;
                        }

                        //start the heartbeat saver
                        Process HeartbeatSaver = new Process();
                        HeartbeatSaver.StartInfo.FileName = "heartbeatsaver.exe";
                        HeartbeatSaver.Start();
                    }
                }
            }
        }
    }
}
