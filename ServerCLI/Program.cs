/*
 *  Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 *  THE SOFTWARE.
 *
 */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using fCraft.Events;

namespace fCraft.ServerCLI {

    internal static class Program {
        private static bool useColor = true;

        private static void Main( string[] args ) {
            Logger.Logged += OnLogged;
            Heartbeat.UriChanged += OnHeartbeatUriChanged;

            Console.Title = "800Craft " + Updater.CurrentRelease.VersionString + " - starting...";

#if !DEBUG
            try {
#endif
                Server.InitLibrary( args );
                useColor = !Server.HasArg( ArgKey.NoConsoleColor );

                Server.InitServer();

                Updater.UpdateCheck();
                Console.Title = "800Craft " + Updater.CurrentRelease.VersionString + " - " + ConfigKey.ServerName.GetString();

                if ( !ConfigKey.ProcessPriority.IsBlank() ) {
                    try {
                        Process.GetCurrentProcess().PriorityClass = ConfigKey.ProcessPriority.GetEnum<ProcessPriorityClass>();
                    } catch ( Exception ) {
                        Logger.Log( LogType.Warning, "Program.Main: Could not set process priority, using defaults." );
                    }
                }

                if ( Server.StartServer() ) {
                    Console.WriteLine( "** Running 800Craft version {0}. **", Updater.CurrentRelease.VersionString );
                    Console.WriteLine( "** Server is now ready. Type /Shutdown to exit safely. **" );

                    while ( !Server.IsShuttingDown ) {
                        string cmd = Console.ReadLine();
                        if ( cmd.Equals( "/Clear", StringComparison.OrdinalIgnoreCase ) ) {
                            Console.Clear();
                        } else {
                            try {
                                Player.Console.ParseMessage( cmd, true );
                            } catch ( Exception ex ) {
                                Logger.LogAndReportCrash( "Error while executing a command from console", "ServerCLI", ex, false );
                            }
                        }
                    }
                } else {
                    ReportFailure( ShutdownReason.FailedToStart );
                }
#if !DEBUG
            } catch ( Exception ex ) {
                Logger.LogAndReportCrash( "Unhandled exception in ServerCLI", "ServerCLI", ex, true );
                ReportFailure( ShutdownReason.Crashed );
            } finally {
                Console.ResetColor();
            }
#endif
        }

        private static void ReportFailure( ShutdownReason reason ) {
            Console.Title = String.Format( "800Craft {0} {1}", Updater.CurrentRelease.VersionString, reason );
            if ( useColor )
                Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine( "** {0} **", reason );
            if ( useColor )
                Console.ResetColor();
            Server.Shutdown( new ShutdownParams( reason, TimeSpan.Zero, false, false ), true );
            if ( !Server.HasArg( ArgKey.ExitOnCrash ) ) {
                Console.ReadLine();
            }
        }

        [DebuggerStepThrough]
        private static void OnLogged( object sender, LogEventArgs e ) {
            if ( !e.WriteToConsole )
                return;
            switch ( e.MessageType ) {
                case LogType.Error:
                    if ( useColor )
                        Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine( e.Message );
                    if ( useColor )
                        Console.ResetColor();
                    return;

                case LogType.SeriousError:
                    if ( useColor )
                        Console.ForegroundColor = ConsoleColor.White;
                    if ( useColor )
                        Console.BackgroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine( e.Message );
                    if ( useColor )
                        Console.ResetColor();
                    return;

                case LogType.Warning:
                    if ( useColor )
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine( e.Message );
                    if ( useColor )
                        Console.ResetColor();
                    return;

                case LogType.Debug:
                case LogType.Trace:
                    if ( useColor )
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine( e.Message );
                    if ( useColor )
                        Console.ResetColor();
                    return;

                default:
                    Console.WriteLine( e.Message );
                    return;
            }
        }

        private static void OnHeartbeatUriChanged( object sender, UriChangedEventArgs e ) {
            File.WriteAllText( "externalurl.txt", e.NewUri.ToString(), Encoding.ASCII );
            Console.WriteLine( "** URL: {0} **", e.NewUri );
            Console.WriteLine( "URL is also saved to file externalurl.txt" );
        }

        #region Updates

        private static readonly AutoResetEvent UpdateDownloadWaiter = new AutoResetEvent( false );
        private static bool updateFailed;

        private static readonly object progressReportLock = new object();

        private static void OnUpdateDownloadProgress( object sender, DownloadProgressChangedEventArgs e ) {
            lock ( progressReportLock ) {
                Console.CursorLeft = 0;
                int maxProgress = Console.WindowWidth - 9;
                int progress = ( int )Math.Round( ( e.ProgressPercentage / 100f ) * ( maxProgress - 1 ) );
                Console.Write( "{0,3}% |", e.ProgressPercentage );
                Console.Write( new String( '=', progress ) );
                Console.Write( '>' );
                Console.Write( new String( ' ', maxProgress - progress ) );
                Console.Write( '|' );
            }
        }

        private static void OnUpdateDownloadCompleted( object sender, AsyncCompletedEventArgs e ) {
            Console.WriteLine();
            if ( e.Error != null ) {
                updateFailed = true;
                if ( useColor )
                    Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( "Downloading the updater failed: {0}", e.Error );
                if ( useColor )
                    Console.ResetColor();
            } else {
                Console.WriteLine( "Update download finished." );
            }
            UpdateDownloadWaiter.Set();
        }

        private static void CheckForUpdates() {
            UpdaterMode updaterMode = ConfigKey.UpdaterMode.GetEnum<UpdaterMode>();
            if ( updaterMode == UpdaterMode.Disabled )
                return;

            UpdaterResult update = Updater.CheckForUpdates();

            if ( update.UpdateAvailable ) {
                Console.WriteLine( "** A new version of 800Craft is available: {0}, released {1:0} day(s) ago. **",
                                   update.LatestRelease.VersionString,
                                   update.LatestRelease.Age.TotalDays );
                if ( updaterMode != UpdaterMode.Notify ) {
                    WebClient client = new WebClient();
                    client.DownloadProgressChanged += OnUpdateDownloadProgress;
                    client.DownloadFileCompleted += OnUpdateDownloadCompleted;
                    client.DownloadFileAsync( update.DownloadUri, Paths.UpdaterFileName );
                    UpdateDownloadWaiter.WaitOne();
                    if ( updateFailed )
                        return;

                    if ( updaterMode == UpdaterMode.Prompt ) {
                        Console.WriteLine( "Restart the server and update now? y/n" );
                        var key = Console.ReadKey();
                        if ( key.KeyChar == 'y' ) {
                            RestartForUpdate();
                            return;
                        } else {
                            Console.WriteLine( "You can update manually by shutting down the server and running " + Paths.UpdaterFileName );
                        }
                    } else {
                        RestartForUpdate();
                        return;
                    }
                }
            }
        }

        private static void RestartForUpdate() {
            string restartArgs = String.Format( "{0} --restart=\"{1}\"",
                                                Server.GetArgString(),
                                                MonoCompat.PrependMono( "ServerGUI.exe" ) );
            MonoCompat.StartDotNetProcess( Paths.UpdaterFileName, restartArgs, true );
        }

        #endregion Updates
    }
}