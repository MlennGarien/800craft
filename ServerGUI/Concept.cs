using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using fCraft.GUI;
using System.Drawing.Text;
using fCraft.ServerGUI.Properties;
using System.Runtime.InteropServices;

namespace fCraft.ServerGUI {
    public partial class Concept : Form {
        /// <summary>
        /// Creates a new Form of type "concept"
        /// MainControl is modified and uses TablessControl, 
        /// meaning the tab titles do not show
        /// </summary>
        public Concept () {
            InitializeComponent();
            SetFonts();
        }

        static PrivateFontCollection Fonts;
        static Font MinecraftFont;
        unsafe void SetFonts () {
            Fonts = new PrivateFontCollection();
            fixed ( byte* fontPointer = Resources.minecraft ) {
                Fonts.AddMemoryFont( ( IntPtr )fontPointer, Resources.minecraft.Length );
                
            }
            MinecraftFont = new Font( Fonts.Families[0], 14, FontStyle.Bold );
            label1.Font = MinecraftFont;
                label2.Font = MinecraftFont;
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImportAttribute( "user32.dll" )]
        public static extern int SendMessage ( IntPtr hWnd,
                         int Msg, int wParam, int lParam );
        [DllImportAttribute( "user32.dll" )]
        public static extern bool ReleaseCapture ();

        private void CloseButton_Click ( object sender, EventArgs e ) {
            Environment.Exit( 1 );//check waht exit codes are lal
        }

        private void MinimizeButton_Click ( object sender, EventArgs e ) {
            this.WindowState = FormWindowState.Minimized;
        }

        private void ConsoleButton_Click ( object sender, EventArgs e ) {
            MainControl.SelectedIndex = 0;
        }

        private void PlayersButton_Click ( object sender, EventArgs e ) {
            MainControl.SelectedIndex = 1;
        }

        private void WorldsButton_Click ( object sender, EventArgs e ) {
            MainControl.SelectedIndex = 2;
        }

        private void ConfigButton_Click ( object sender, EventArgs e ) {
            MainControl.SelectedIndex = 3;
        }

        /// <summary>
        /// This needs two images - one with "unavailable" and another with "online" 
        /// Clickable when "online"
        /// Opens the server in the browser
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayButton_Click ( object sender, EventArgs e ) {
            MainControl.SelectedIndex = 4;
        }

        private void AboutButton_Click ( object sender, EventArgs e ) {
            //run aboutwindow
            new AboutWindow().Show();
        }

        private void panel1_Click ( object sender, MouseEventArgs e ) {
            if ( e.Button == MouseButtons.Left ) {
                ReleaseCapture();
                SendMessage( Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0 );
            }
        }
    }
}
