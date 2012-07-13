// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
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
        volatile bool shutdownPending, startupComplete, shutdownComplete;
        const int MaxLinesInLog = 2000,
                  LinesToTrimWhenExceeded = 50;

        public MainForm() {
            InitializeComponent();
            Shown += StartUp;
            console.OnCommand += console_Enter;
           // logBox.Lin += new LinkClickedEventHandler(Link_Clicked);
        }

        #region Startup
        Thread startupThread;

        void StartUp( object sender, EventArgs a ) {
            Logger.Logged += OnLogged;
            Heartbeat.UriChanged += OnHeartbeatUriChanged;
            Server.PlayerListChanged += OnPlayerListChanged;
            Server.ShutdownEnded += OnServerShutdownEnded;
            Text = "800Craft " + Updater.CurrentRelease.VersionString + " - starting...";
            startupThread = new Thread( StartupThread );
            startupThread.Name = "800Craft ServerGUI Startup";
            startupThread.Start();
        }


        void StartupThread() {
#if !DEBUG
            try {
#endif
                Server.InitLibrary( Environment.GetCommandLineArgs() );
                if( shutdownPending ) return;

                Server.InitServer();
                if( shutdownPending ) return;

                BeginInvoke( (Action)OnInitSuccess );

                // check for updates
                UpdaterMode updaterMode = ConfigKey.UpdaterMode.GetEnum<UpdaterMode>();
                if( updaterMode != UpdaterMode.Disabled ) {
                    UpdaterResult update = Updater.CheckForUpdates();
                    if( shutdownPending ) return;
                    if( update.UpdateAvailable ) {
                        if( updaterMode == UpdaterMode.Notify ) {
                            String updateMsg = String.Format( "An 800Craft update is available! Visit www.fCraft.net to download. " +
                                                              "Local version: {0}. Latest available version: {1}.",
                                                              Updater.CurrentRelease.VersionString,
                                                              update.LatestRelease.VersionString );
                            Logger.LogToConsole( updateMsg );
                        } else {
                            new UpdateWindow( update ).ShowDialog();
                        }
                    }
                }

                // set process priority
                if( !ConfigKey.ProcessPriority.IsBlank() ) {
                    try {
                        Process.GetCurrentProcess().PriorityClass = ConfigKey.ProcessPriority.GetEnum<ProcessPriorityClass>();
                    } catch( Exception ) {
                        Logger.Log( LogType.Warning,
                                    "MainForm.StartServer: Could not set process priority, using defaults." );
                    }
                }

                if( shutdownPending ) return;
                if( Server.StartServer() ) {
                    startupComplete = true;
                    BeginInvoke( (Action)OnStartupSuccess );
                } else {
                    BeginInvoke( (Action)OnStartupFailure );
                }
#if !DEBUG
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Unhandled exception in ServerGUI.StartUp", "ServerGUI", ex, true );
                Shutdown( ShutdownReason.Crashed, Server.HasArg( ArgKey.ExitOnCrash ) );
            }
#endif
        }


        void OnInitSuccess() {
            Text = "800Craft " + Updater.CurrentRelease.VersionString + " - " + ConfigKey.ServerName.GetString();
        }


        void OnStartupSuccess() {
            if( !ConfigKey.HeartbeatEnabled.Enabled() ) {
                uriDisplay.Text = "Heartbeat disabled. See externalurl.txt";
            }
            console.Enabled = true;
            console.Text = "";
        }


        void OnStartupFailure() {
            Shutdown( ShutdownReason.FailedToStart, Server.HasArg( ArgKey.ExitOnCrash ) );
        }

        #endregion


        #region Shutdown

        protected override void OnFormClosing( FormClosingEventArgs e ) {
            if( startupThread != null && !shutdownComplete ) {
                Shutdown( ShutdownReason.ProcessClosing, true );
                e.Cancel = true;
            } else {
                base.OnFormClosing( e );
            }
        }


        void Shutdown( ShutdownReason reason, bool quit ) {
            if( shutdownPending ) return;
            shutdownPending = true;
            console.Enabled = false;
            console.Text = "Shutting down...";
            Text = "800Craft " + Updater.CurrentRelease.VersionString + " - shutting down...";
            uriDisplay.Enabled = false;
            if( !startupComplete ) {
                startupThread.Join();
            }
            Server.Shutdown( new ShutdownParams( reason, TimeSpan.Zero, quit, false ), false );
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

        #endregion


        public void OnLogged( object sender, LogEventArgs e ) {
            if( !e.WriteToConsole ) return;
            try {
                if( shutdownComplete ) return;
                if( logBox.InvokeRequired ) {
                    BeginInvoke( (EventHandler<LogEventArgs>)OnLogged, sender, e );
                } else {
                    // store user's selection
                    int userSelectionStart = logBox.SelectionStart;
                    int userSelectionLength = logBox.SelectionLength;
                    bool userSelecting = (logBox.SelectionStart != logBox.Text.Length && logBox.Focused || logBox.SelectionLength > 0);

                    // insert and color a new message
                    int oldLength = logBox.Text.Length;
                    string msgToAppend = e.Message + Environment.NewLine;
                    logBox.AppendText(msgToAppend);
                        
                    }

                    // cut off the log, if too long
                    if( logBox.Lines.Length > MaxLinesInLog ) {
                                                logBox.SelectionStart = 0;
                        logBox.SelectionLength = logBox.GetFirstCharIndexFromLine( LinesToTrimWhenExceeded );
                        string textToAdd = "----- cut off, see " + Logger.CurrentLogFileName + " for complete log -----" + Environment.NewLine;
                        logBox.SelectedText = textToAdd;
                        // either restore user's selection, or scroll to end
                    }
                    logBox.SelectionStart = logBox.Text.Length;
                    logBox.ScrollToCaret();
                    if( !Server.IsRunning || shutdownPending ) logBox.Refresh();
                
        
            }  catch( InvalidOperationException ) { }
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

        private void logBox_TextChanged(object sender, EventArgs e)
        {

        }
        private void Link_Clicked (object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SizeBox.SelectedItem.ToString() == "Normal"){
                logBox.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            }
            if (SizeBox.SelectedItem.ToString() == "Big"){
                logBox.Font = new System.Drawing.Font("Lucida Console", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            }
            if (SizeBox.SelectedItem.ToString() == "Large"){
                logBox.Font = new System.Drawing.Font("Lucida Console", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            }
        }

        private bool _isBlackText = false;
        private void ThemeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ThemeBox.SelectedItem.ToString().Equals("New 800Craft")) { SetNewTheme(); }
            if (ThemeBox.SelectedItem.ToString().Equals("Old 800Craft")) { SetOldTheme(); }
            if (ThemeBox.SelectedItem.ToString().Equals("Pink")) { SetPinkTheme(); }
            if (ThemeBox.SelectedItem.ToString().Equals("Yellow")) { SetYellowTheme(); }
            if (ThemeBox.SelectedItem.ToString().Equals("Green")) { SetGreenTheme(); }
            if (ThemeBox.SelectedItem.ToString().Equals("Purple")) { SetPurpleTheme(); }
            if (ThemeBox.SelectedItem.ToString().Equals("Grey")) { SetGreyTheme(); }
        }

        public void SetNewTheme()
        {
            BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            playerList.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            if (logBox.BackColor != System.Drawing.Color.Black)
            {
                logBox.BackColor = System.Drawing.Color.Black;
                logBox.SelectAll();
                logBox.ForeColor = System.Drawing.Color.LightGray;
                logBox.Select(0, 0);
                _isBlackText = false;
            }
        }
        public void SetOldTheme()
        {
            BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            playerList.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            logBox.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            if (!_isBlackText)
            {
                logBox.SelectAll();
                logBox.ForeColor = System.Drawing.Color.Black;
                logBox.Select(0, 0);
                _isBlackText = true;
            }
        }
        public void SetPinkTheme()
        {
            BackColor = System.Drawing.Color.Pink;
            playerList.BackColor = System.Drawing.Color.LightPink;
            logBox.BackColor = System.Drawing.Color.LightPink;
            if (!_isBlackText)
            {
                logBox.SelectAll();
                logBox.ForeColor = System.Drawing.Color.Black;
                logBox.Select(0, 0);
                _isBlackText = true;
            }
        }

        public void SetYellowTheme()
        {
            BackColor = System.Drawing.Color.LightGoldenrodYellow;
            playerList.BackColor = System.Drawing.Color.LightYellow;
            logBox.BackColor = System.Drawing.Color.LightYellow;
            if (!_isBlackText)
            {
                logBox.SelectAll();
                logBox.ForeColor = System.Drawing.Color.Black;
                logBox.Select(0, 0);
                _isBlackText = true;
            }
        }

        public void SetGreenTheme()
        {
            BackColor = System.Drawing.Color.SpringGreen;
            playerList.BackColor = System.Drawing.Color.LightGreen;
            logBox.BackColor = System.Drawing.Color.LightGreen;
            if (!_isBlackText)
            {
                logBox.SelectAll();
                logBox.ForeColor = System.Drawing.Color.Black;
                logBox.Select(0, 0);
                _isBlackText = true;
            }
        }

        public void SetPurpleTheme()
        {
            BackColor = System.Drawing.Color.MediumPurple;
            playerList.BackColor = System.Drawing.Color.Plum;
            logBox.BackColor = System.Drawing.Color.Plum;
            if (!_isBlackText)
            {
                logBox.SelectAll();
                logBox.ForeColor = System.Drawing.Color.Black;
                logBox.Select(0, 0);
                _isBlackText = true;
            }
        }

        public void SetGreyTheme()
        {
            BackColor = System.Drawing.SystemColors.Control;
            playerList.BackColor = System.Drawing.SystemColors.ControlLight;
            logBox.BackColor = System.Drawing.SystemColors.ControlLight;
            if (!_isBlackText)
            {
                logBox.SelectAll();
                logBox.ForeColor = System.Drawing.Color.Black;
                logBox.Select(0, 0);
                _isBlackText = true;
            }
        }
    }
}