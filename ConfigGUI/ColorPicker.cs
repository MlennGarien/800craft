// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System.Collections.Generic;
using System.Windows.Forms;

namespace fCraft.ConfigGUI {
    internal sealed partial class ColorPicker : Form {
        public static readonly Dictionary<int, ColorPair> ColorPairs = new Dictionary<int, ColorPair>();
        public int ColorIndex;


        static ColorPicker() {
            ColorPairs.Add( 0, new ColorPair( System.Drawing.Color.White, System.Drawing.Color.Black ) );
            ColorPairs.Add( 8, new ColorPair( System.Drawing.Color.White, System.Drawing.Color.DimGray ) );
            ColorPairs.Add( 1, new ColorPair( System.Drawing.Color.White, System.Drawing.Color.Navy ) );
            ColorPairs.Add( 9, new ColorPair( System.Drawing.Color.White, System.Drawing.Color.RoyalBlue ) );
            ColorPairs.Add( 2, new ColorPair( System.Drawing.Color.White, System.Drawing.Color.Green ) );
            ColorPairs.Add( 10, new ColorPair( System.Drawing.Color.Black, System.Drawing.Color.Lime ) );
            ColorPairs.Add( 3, new ColorPair( System.Drawing.Color.White, System.Drawing.Color.Teal ) );
            ColorPairs.Add( 11, new ColorPair( System.Drawing.Color.Black, System.Drawing.Color.Aqua ) );
            ColorPairs.Add( 4, new ColorPair( System.Drawing.Color.White, System.Drawing.Color.Maroon ) );
            ColorPairs.Add( 12, new ColorPair( System.Drawing.Color.White, System.Drawing.Color.Red ) );
            ColorPairs.Add( 5, new ColorPair( System.Drawing.Color.White, System.Drawing.Color.Purple ) );
            ColorPairs.Add( 13, new ColorPair( System.Drawing.Color.Black, System.Drawing.Color.Magenta ) );
            ColorPairs.Add( 6, new ColorPair( System.Drawing.Color.White, System.Drawing.Color.Olive ) );
            ColorPairs.Add( 14, new ColorPair( System.Drawing.Color.Black, System.Drawing.Color.Yellow ) );
            ColorPairs.Add( 7, new ColorPair( System.Drawing.Color.Black, System.Drawing.Color.Silver ) );
            ColorPairs.Add( 15, new ColorPair( System.Drawing.Color.Black, System.Drawing.Color.White ) );
        }


        public ColorPicker( string title, int oldColorIndex ) {
            InitializeComponent();
            Text = title;
            ColorIndex = oldColorIndex;
            StartPosition = FormStartPosition.CenterParent;

            b0.Click += delegate { ColorIndex = 0; DialogResult = DialogResult.OK; Close(); };
            b1.Click += delegate { ColorIndex = 1; DialogResult = DialogResult.OK; Close(); };
            b2.Click += delegate { ColorIndex = 2; DialogResult = DialogResult.OK; Close(); };
            b3.Click += delegate { ColorIndex = 3; DialogResult = DialogResult.OK; Close(); };
            b4.Click += delegate { ColorIndex = 4; DialogResult = DialogResult.OK; Close(); };
            b5.Click += delegate { ColorIndex = 5; DialogResult = DialogResult.OK; Close(); };
            b6.Click += delegate { ColorIndex = 6; DialogResult = DialogResult.OK; Close(); };
            b7.Click += delegate { ColorIndex = 7; DialogResult = DialogResult.OK; Close(); };
            b8.Click += delegate { ColorIndex = 8; DialogResult = DialogResult.OK; Close(); };
            b9.Click += delegate { ColorIndex = 9; DialogResult = DialogResult.OK; Close(); };
            ba.Click += delegate { ColorIndex = 10; DialogResult = DialogResult.OK; Close(); };
            bb.Click += delegate { ColorIndex = 11; DialogResult = DialogResult.OK; Close(); };
            bc.Click += delegate { ColorIndex = 12; DialogResult = DialogResult.OK; Close(); };
            bd.Click += delegate { ColorIndex = 13; DialogResult = DialogResult.OK; Close(); };
            be.Click += delegate { ColorIndex = 14; DialogResult = DialogResult.OK; Close(); };
            bf.Click += delegate { ColorIndex = 15; DialogResult = DialogResult.OK; Close(); };
        }


        internal struct ColorPair {
            public ColorPair( System.Drawing.Color foreground, System.Drawing.Color background ) {
                Foreground = foreground;
                Background = background;
            }
            public System.Drawing.Color Foreground;
            public System.Drawing.Color Background;
        }
    }
}