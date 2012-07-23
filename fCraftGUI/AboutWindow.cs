// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace fCraft.GUI {
    public sealed partial class AboutWindow : Form {
        public AboutWindow() {
            InitializeComponent();
            lSubheader.Text = String.Format( lSubheader.Text, Updater.CurrentRelease.VersionString );
        }

        private void linkLabel1_LinkClicked( object sender, LinkLabelLinkClickedEventArgs e ) {
            try {
                Process.Start( "http://www.fcraft.net" );
            } catch { }
        }

        private void linkLabel2_LinkClicked( object sender, LinkLabelLinkClickedEventArgs e ) {
            try {
                Process.Start( "mailto:jonty800@gmail.com" );
            } catch { }
        }

        private void linkLabel2_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("http://www.800craft.net");
            }
            catch { }
        }

    }
}