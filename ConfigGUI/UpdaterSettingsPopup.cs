// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Windows.Forms;

namespace fCraft.ConfigGUI {
    public sealed partial class UpdaterSettingsPopup : Form {

        public string RunBeforeUpdate {
            get {
                if( xRunBeforeUpdate.Checked) return tRunBeforeUpdate.Text;
                else return "";
            }
            set { tRunBeforeUpdate.Text = value; }
        }

        public string RunAfterUpdate {
            get {
                if( xRunAfterUpdate.Checked ) return tRunAfterUpdate.Text;
                else return "";
            }
            set { tRunAfterUpdate.Text = value; }
        }

        public UpdaterMode UpdaterMode {
            get {
                if( rDisabled.Checked ) return UpdaterMode.Disabled;
                if( rNotify.Checked ) return UpdaterMode.Notify;
                if( rPrompt.Checked ) return UpdaterMode.Prompt;
                return UpdaterMode.Auto;
            }
            set {
                switch( value ) {
                    case UpdaterMode.Disabled:
                        rDisabled.Checked = true; break;
                    case UpdaterMode.Notify:
                        rNotify.Checked = true; break;
                    case UpdaterMode.Prompt:
                        rPrompt.Checked = true; break;
                    case UpdaterMode.Auto:
                        rAutomatic.Checked = true; break;
                }
            }
        }

        public bool BackupBeforeUpdate {
            get { return xBackupBeforeUpdating.Checked; }
            set { xBackupBeforeUpdating.Checked = value; }
        }

        string oldRunBeforeUpdate, oldRunAfterUpdate;
        UpdaterMode oldUpdaterMode;
        bool oldBackupBeforeUpdate;

        public UpdaterSettingsPopup() {
            InitializeComponent();
            Shown += delegate {
                oldRunBeforeUpdate = RunBeforeUpdate;
                oldRunAfterUpdate = RunAfterUpdate;
                oldUpdaterMode = UpdaterMode;
                oldBackupBeforeUpdate = BackupBeforeUpdate;
            };
            FormClosed += delegate {
                if( DialogResult != DialogResult.OK ) {
                    RunBeforeUpdate = oldRunBeforeUpdate;
                    RunAfterUpdate = oldRunAfterUpdate;
                    UpdaterMode = oldUpdaterMode;
                    BackupBeforeUpdate = oldBackupBeforeUpdate;
                }
            };
        }

        private void xRunBeforeUpdate_CheckedChanged( object sender, EventArgs e ) {
            tRunBeforeUpdate.Enabled = xRunBeforeUpdate.Checked;
        }

        private void xRunAfterUpdate_CheckedChanged( object sender, EventArgs e ) {
            tRunAfterUpdate.Enabled = xRunAfterUpdate.Checked;
        }

        private void rDisabled_CheckedChanged( object sender, EventArgs e ) {
            gOptions.Enabled = !rDisabled.Checked;
        }


        private void tRunBeforeUpdate_TextChanged( object sender, EventArgs e ) {
            if( tRunBeforeUpdate.Text.Length > 0 ) {
                xRunBeforeUpdate.Checked = true;
            }
        }

        private void tRunAfterUpdate_TextChanged( object sender, EventArgs e ) {
            if( tRunAfterUpdate.Text.Length > 0 ) {
                xRunAfterUpdate.Checked = true;
            }
        }
    }
}