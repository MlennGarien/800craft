// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.ComponentModel;
using System.Net;
using System.Windows.Forms;
using System.Text;
using System.IO;

namespace fCraft.ServerGUI {
    public sealed partial class UpdateWindow : Form {
        readonly UpdaterResult updateResult;
        readonly string updaterFullPath;
        readonly WebClient downloader = new WebClient();
        readonly bool autoUpdate;
        bool closeFormWhenDownloaded;

        public UpdateWindow( UpdaterResult update, bool auto ) {
            InitializeComponent();
            updaterFullPath = Path.Combine( Paths.WorkingPath, Paths.UpdaterFileName );
            updateResult = update;
            autoUpdate = auto;
            CreateDetailedChangeLog();
            lVersion.Text = String.Format( lVersion.Text,
                                           Updater.CurrentRelease.VersionString,
                                           updateResult.LatestRelease.VersionString,
                                           updateResult.LatestRelease.Age.TotalDays );
            Shown += Download;
        }


        void Download( object caller, EventArgs args ) {
            xShowDetails.Focus();
            downloader.DownloadProgressChanged += DownloadProgress;
            downloader.DownloadFileCompleted += DownloadComplete;
            downloader.DownloadFileAsync( updateResult.DownloadUri, updaterFullPath );
        }


        void DownloadProgress( object sender, DownloadProgressChangedEventArgs e ) {
            Invoke( (Action)delegate {
                progress.Value = e.ProgressPercentage;
                lProgress.Text = "Downloading (" + e.ProgressPercentage + "%)";
            } );
        }


        void DownloadComplete( object sender, AsyncCompletedEventArgs e ) {
            if( closeFormWhenDownloaded ) {
                Close();
            } else {
                progress.Value = 100;
                if( e.Cancelled || e.Error != null ) {
                    MessageBox.Show( e.Error.ToString(), "Error occured while trying to download " + Paths.UpdaterFileName );
                } else if( autoUpdate ) {
                    bUpdateNow_Click( null, null );
                } else {
                    bUpdateNow.Enabled = true;
                    bUpdateLater.Enabled = true;
                }
            }
        }


        private void bCancel_Click( object sender, EventArgs e ) {
            Close();
        }

        private void bUpdateNow_Click( object sender, EventArgs e ) {
            string args = Server.GetArgString() +
                          String.Format( "--restart=\"{0}\"", MonoCompat.PrependMono( "ServerGUI.exe" ) );
            MonoCompat.StartDotNetProcess( updaterFullPath, args, true );
            Application.Exit();
        }


        void CreateDetailedChangeLog() {
            StringBuilder sb = new StringBuilder();
            foreach( ReleaseInfo release in updateResult.History ) {
                sb.AppendFormat( "{0} - {1:0} days ago - {2}",
                                 release.VersionString,
                                 release.Age.TotalDays,
                                 String.Join( ", ", release.FlagsList ) );
                sb.AppendLine();
                if( xShowDetails.Checked ) {
                    sb.AppendFormat( "    {0}", String.Join( Environment.NewLine + "    ", release.ChangeLog ) );
                } else {
                    sb.AppendFormat( "    {0}", release.Summary );
                }
                sb.AppendLine().AppendLine();
            }
            tChangeLog.Text = sb.ToString();
        }

        private void xShowDetails_CheckedChanged( object sender, EventArgs e ) {
            CreateDetailedChangeLog();
        }

        private void bUpdateLater_Click( object sender, EventArgs e ) {
            Updater.RunAtShutdown = true;
            Close();
        }

        private void UpdateWindow_FormClosing( object sender, FormClosingEventArgs e ) {
            if( !downloader.IsBusy ) return;
            downloader.CancelAsync();
            closeFormWhenDownloaded = true;
            e.Cancel = true;
        }
    }
}