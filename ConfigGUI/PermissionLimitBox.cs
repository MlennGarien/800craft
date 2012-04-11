// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Windows.Forms;

namespace fCraft.ConfigGUI {
    public sealed partial class PermissionLimitBox : UserControl {

        public Permission Permission { get; private set; }

        public string FirstItem { get; private set; }

        public Rank Rank { get; private set; }

        public PermissionLimitBox( string labelText, Permission permission, string firstItem ) {
            InitializeComponent();

            label.Text = labelText;
            label.Left = (comboBox.Left - comboBox.Margin.Left) - (label.Width + label.Margin.Right);

            Permission = permission;
            FirstItem = firstItem;
            RebuildList();

            comboBox.SelectedIndexChanged += OnPermissionLimitChanged;
        }


        void OnPermissionLimitChanged( object sender, EventArgs args ) {
            if( Rank == null ) return;
            Rank rankLimit = RankManager.FindRank( comboBox.SelectedIndex - 1 );
            if( rankLimit == null ) {
                Rank.ResetLimit( Permission );
            } else {
                Rank.SetLimit( Permission, rankLimit );
            }
        }


        public void Reset() {
            comboBox.SelectedIndex = 0;
        }


        public void RebuildList() {
            comboBox.Items.Clear();
            comboBox.Items.Add( FirstItem );
            foreach( Rank rank in RankManager.Ranks ) {
                comboBox.Items.Add( MainForm.ToComboBoxOption( rank ) );
            }
        }


        public void SelectRank( Rank rank ) {
            Rank = rank;
            if( rank == null ) {
                comboBox.SelectedIndex = -1;
                Visible = false;
            } else {
                comboBox.SelectedIndex = rank.GetLimitIndex( Permission );
                Visible = rank.Can( Permission );
            }
        }


        public void PermissionToggled( bool isOn ) {
            Visible = isOn;
        }
    }
}