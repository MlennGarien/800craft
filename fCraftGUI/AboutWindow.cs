// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace fCraft.GUI {
    public sealed partial class AboutWindow : Form {
        public AboutWindow() {
            InitializeComponent();
            lSubheader.Text = String.Format( lSubheader.Text, Updater.CurrentRelease.VersionString );
            tCredits.Select( 0, 0 );
        }

        private void linkLabel1_LinkClicked( object sender, LinkLabelLinkClickedEventArgs e ) {
            try {
                Process.Start( "http://www.au70galaxy.com" );
            } catch { }
        }

        private void linkLabel2_LinkClicked( object sender, LinkLabelLinkClickedEventArgs e ) {
            try {
                Process.Start( "mailto:jonty800@gmail.com" );
            } catch { }
        }

        private void lSubheader_Click(object sender, EventArgs e)
        {

        }

        private void lHeader_Click(object sender, EventArgs e)
        {

        }

    }
}