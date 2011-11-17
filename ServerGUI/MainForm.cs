// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using fCraft.Events;
using fCraft.GUI;
using System.Threading;

namespace fCraft.ServerGUI {

    public sealed partial class MainForm : Form {
        bool shutdownPending, shutdownComplete;
        const int MaxLinesInLog = 2000;
        delegate void SetTextCallback(string text);
        delegate void SetBooleanCallback(bool value);

        public MainForm() {
            InitializeComponent();
            Shown += StartUp;
            FormClosing += HandleShutDown;
            console.OnCommand += console_Enter;
        }


        void StartUp( object sender, EventArgs a ) {
            Thread startUpThread = new Thread(new ThreadStart(delegate
            {
                Logger.Logged += OnLogged;
                Heartbeat.UriChanged += OnHeartbeatUriChanged;
                Server.PlayerListChanged += OnPlayerListChanged;
                Server.ShutdownEnded += OnServerShutdownEnded;


#if !DEBUG
                try
                {
#endif
                    SetConsoleText("fCraft " + Updater.CurrentRelease.VersionString + " - starting...");
                    Server.InitLibrary(Environment.GetCommandLineArgs());
                    Server.InitServer();
                    SetConsoleText("fCraft " + Updater.CurrentRelease.VersionString + " - " + ConfigKey.ServerName.GetString());

                    Application.DoEvents();
                    //StartServer();

                    UpdaterResult update = Updater.CheckForUpdates();

                    if (update.UpdateAvailable)
                    {
                        new UpdateWindow(update, false).ShowDialog();
                    }

                    StartServer();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    Logger.LogAndReportCrash("Unhandled exception in ServerGUI.StartUp", "ServerGUI", ex, true);
                    Shutdown(ShutdownReason.Crashed, false);
                }
#endif
            }));
            startUpThread.Start();
        }

        public void StartServer() {
            Thread startServerThread = new Thread(new ThreadStart(delegate
            {
                if (!ConfigKey.ProcessPriority.IsBlank())
                {
                    try
                    {
                        Process.GetCurrentProcess().PriorityClass = ConfigKey.ProcessPriority.GetEnum<ProcessPriorityClass>();
                    }
                    catch (Exception)
                    {
                        Logger.Log(LogType.Warning,
                                    "MainForm.StartServer: Could not set process priority, using defaults.");
                    }
                }
                if (Server.StartServer())
                {
                    if (!ConfigKey.HeartbeatEnabled.Enabled())
                    {
                        SetURIText("Heartbeat disabled. See externalurl.txt");
                    }

                    SetConsoleEnabled(true);
                    SetConsoleText("");
                }
                else
                {
                    Shutdown(ShutdownReason.FailedToStart, false);
                }
            }));
            startServerThread.Start();
        }

        void HandleShutDown( object sender, CancelEventArgs e ) {
            if( shutdownComplete ) return;
            e.Cancel = true;
            Shutdown( ShutdownReason.ProcessClosing, true );
        }

        void Shutdown( ShutdownReason reason, bool quit ) {
            if( shutdownPending ) return;
            shutdownPending = true;
            SetURIDisplayEnabled(false);
            SetConsoleEnabled(false);
            SetConsoleText("Shutting down...");
            Server.Shutdown( new ShutdownParams( reason, TimeSpan.Zero, quit, false ), false );
        }

        private void SetConsoleText(String text)
        {
            if (console.InvokeRequired)
            {
                SetTextCallback callback = new SetTextCallback(delegate
                {
                    console.Text = text;
                });
                Invoke(callback, text);
            }
            else
            {
                console.Text = text;
            }
        }

        private void SetConsoleEnabled(Boolean value)
        {
            if (console.InvokeRequired)
            {
                SetBooleanCallback callback = new SetBooleanCallback(delegate
                {
                    console.Enabled = value;
                });
                Invoke(callback, value);
            }
            else
            {
                console.Enabled = value;
            }
        }

        private void SetURIText(String text)
        {
            if (uriDisplay.InvokeRequired)
            {
                SetTextCallback callback = new SetTextCallback(delegate
                {
                    uriDisplay.Text = text;
                });
                Invoke(callback, text);
            }
            else
            {
                uriDisplay.Text = text;
            }
        }

        private void SetURIDisplayEnabled(Boolean value)
        {
            if (uriDisplay.InvokeRequired)
            {
                SetBooleanCallback callback = new SetBooleanCallback(delegate
                {
                    uriDisplay.Enabled = value;
                });
                Invoke(callback, value);
            }
            else
            {
                uriDisplay.Enabled = value;
            }
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
                    SetURIText(e.NewUri.ToString());
                    SetURIDisplayEnabled(true);
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
            SetConsoleText("");
        }

        private void bPlay_Click( object sender, EventArgs e ) {
            try {
                Process.Start( uriDisplay.Text );
            } catch( Exception ) {
                MessageBox.Show( "Could not open server URL. Please copy/paste it manually." );
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }
}