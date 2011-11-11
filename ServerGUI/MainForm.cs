// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using fCraft.Events;
using fCraft.GUI;

namespace fCraft.ServerGUI {

    public sealed partial class MainForm : Form {
        bool shutdownPending, shutdownComplete;
        const int MaxLinesInLog = 2000;

        public MainForm() {
            InitializeComponent();
            Shown += StartUp;
            FormClosing += HandleShutDown;
            console.OnCommand += console_Enter;
        }


        void StartUp( object sender, EventArgs a ) {
            Logger.Logged += OnLogged;
            Heartbeat.UriChanged += OnHeartbeatUriChanged;
            Server.PlayerListChanged += OnPlayerListChanged;
            Server.ShutdownEnded += OnServerShutdownEnded;


#if !DEBUG
            try {
#endif
                Text = "fCraft " + Updater.CurrentRelease.VersionString + " - starting...";
                Server.InitLibrary( Environment.GetCommandLineArgs() );
                Server.InitServer();
                Text = "fCraft " + Updater.CurrentRelease.VersionString + " - " + ConfigKey.ServerName.GetString();

                Application.DoEvents();
                //StartServer();

                UpdaterResult update = Updater.CheckForUpdates();

                if( update.UpdateAvailable ) {
                    new UpdateWindow( update, false ).ShowDialog();
                }

                StartServer();
#if !DEBUG
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Unhandled exception in ServerGUI.StartUp", "ServerGUI", ex, true );
                Shutdown( ShutdownReason.Crashed, false );
            }
#endif
        }


        public void StartServer() {
            if( !ConfigKey.ProcessPriority.IsBlank() ) {
                try {
                    Process.GetCurrentProcess().PriorityClass = ConfigKey.ProcessPriority.GetEnum<ProcessPriorityClass>();
                } catch( Exception ) {
                    Logger.Log( LogType.Warning,
                                "MainForm.StartServer: Could not set process priority, using defaults." );
                }
            }
            if( Server.StartServer() ) {
                if( !ConfigKey.HeartbeatEnabled.Enabled() ) {
                    uriDisplay.Text = "Heartbeat disabled. See externalurl.txt";
                }
                console.Enabled = true;
                console.Text = "";
            } else {
                Shutdown( ShutdownReason.FailedToStart, false );
            }
        }

        void HandleShutDown( object sender, CancelEventArgs e ) {
            if( shutdownComplete ) return;
            e.Cancel = true;
            Shutdown( ShutdownReason.ProcessClosing, true );
        }

        void Shutdown( ShutdownReason reason, bool quit ) {
            if( shutdownPending ) return;
            shutdownPending = true;
            uriDisplay.Enabled = false;
            console.Enabled = false;
            console.Text = "Shutting down...";
            Server.Shutdown( new ShutdownParams( reason, TimeSpan.Zero, quit, false ), false );
        }


        public void OnLogged( object sender, LogEventArgs e ) {
            if( !e.WriteToConsole ) return;
            try {
                if( shutdownComplete ) return;
                if( logBox.InvokeRequired ) {
                    BeginInvoke( (EventHandler<LogEventArgs>)OnLogged, sender, e );
                } else {
                    Log( e.Message );
                }
            } catch( ObjectDisposedException ) {
            } catch( InvalidOperationException ) { }
        }

        void Log( string message ) {
            logBox.AppendText( message + Environment.NewLine );
            if( logBox.Lines.Length > MaxLinesInLog ) {
                logBox.Text = "----- cut off, see fCraft.log for complete log -----" +
                    Environment.NewLine +
                    logBox.Text.Substring( logBox.GetFirstCharIndexFromLine( 50 ) );
            }
            logBox.SelectionStart = logBox.Text.Length;
            logBox.ScrollToCaret();
            logBox.Refresh();
        }


        public void OnHeartbeatUriChanged( object sender, UriChangedEventArgs e ) {
            try {
                if( shutdownPending ) return;
                if( uriDisplay.InvokeRequired ) {
                    BeginInvoke( (EventHandler<UriChangedEventArgs>)OnHeartbeatUriChanged,
                            sender, e );
                } else {
                    uriDisplay.Text = e.NewUri.ToString();
                    uriDisplay.Enabled = true;
                    bPlay.Enabled = true;
                }
            } catch( ObjectDisposedException ) {
            } catch( InvalidOperationException ) { }
        }


        public void OnPlayerListChanged( object sender, EventArgs e ) {
            try {
                if( shutdownPending ) return;
                if( playerList.InvokeRequired ) {
                    BeginInvoke( (EventHandler)OnPlayerListChanged, null, EventArgs.Empty );
                } else {
                    playerList.Items.Clear();
                    Player[] playerListCache = Server.Players.OrderBy( p => p.Info.Rank.Index ).ToArray();
                    foreach( Player player in playerListCache ) {
                        playerList.Items.Add( player.Info.Rank.Name + " - " + player.Name );
                    }
                }
            } catch( ObjectDisposedException ) {
            } catch( InvalidOperationException ) { }
        }


        void OnServerShutdownEnded( object sender, ShutdownEventArgs e ) {
            try {
                BeginInvoke( (Action)delegate {
                    shutdownComplete = true;
                    switch( e.ShutdownParams.Reason ) {
                        case ShutdownReason.FailedToInitialize:
                        case ShutdownReason.FailedToStart:
                        case ShutdownReason.Crashed:
                            if( Server.HasArg( ArgKey.ExitOnCrash ) ) {
                                Application.Exit();
                            }
                            break;
                        default:
                            Application.Exit();
                            break;
                    }
                } );
            } catch( ObjectDisposedException ) {
            } catch( InvalidOperationException ) { }
        }


        private void console_Enter() {
            string[] separator = { Environment.NewLine };
            string[] lines = console.Text.Trim().Split( separator, StringSplitOptions.RemoveEmptyEntries );
            foreach( string line in lines ) {
#if !DEBUG
                try {
#endif
                    if( line.Equals( "/Clear", StringComparison.OrdinalIgnoreCase ) ) {
                        logBox.Clear();
                    } else if( line.Equals( "/credits", StringComparison.OrdinalIgnoreCase ) ) {
                        new AboutWindow().Show();
                    } else {
                        Player.Console.ParseMessage( line, true );
                    }
#if !DEBUG
                } catch( Exception ex ) {
                    Logger.LogToConsole( "Error occured while trying to execute last console command: " );
                    Logger.LogToConsole( ex.GetType().Name + ": " + ex.Message );
                    Logger.LogAndReportCrash( "Exception executing command from console", "ServerGUI", ex, false );
                }
#endif
            }
            console.Text = "";
        }

        private void bPlay_Click( object sender, EventArgs e ) {
            try {
                Process.Start( uriDisplay.Text );
            } catch( Exception ) {
                MessageBox.Show( "Could not open server URL. Please copy/paste it manually." );
            }
        }
    }
}