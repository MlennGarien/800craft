using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;
using System.Diagnostics;
using System.IO;

namespace fCraft
{
    public class HeartbeatSaverUtil
    {
        public static void Init()
        {
            Server.ShutdownBegan += HbCrashEvent;
        }
        public static void HbCrashEvent(object sender, ShutdownEventArgs e)
        {
            if( e.ShutdownParams.Reason == ShutdownReason.Crashed)
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
