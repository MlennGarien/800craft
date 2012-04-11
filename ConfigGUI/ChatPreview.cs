// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using fCraft.ConfigGUI.Properties;


namespace fCraft.ConfigGUI {
    sealed partial class ChatPreview : UserControl {

        struct ColorPair {
            public ColorPair( int r, int g, int b, int sr, int sg, int sb ) {
                Foreground = new SolidBrush( System.Drawing.Color.FromArgb( r, g, b ) );
                Shadow = new SolidBrush( System.Drawing.Color.FromArgb( sr, sg, sb ) );
            }
            public readonly Brush Foreground, Shadow;
        }

        static readonly PrivateFontCollection Fonts;
        static readonly Font MinecraftFont;
        static readonly ColorPair[] ColorPairs;

        unsafe static ChatPreview() {
            Fonts = new PrivateFontCollection();
            fixed( byte* fontPointer = Resources.MinecraftFont ) {
                Fonts.AddMemoryFont( (IntPtr)fontPointer, Resources.MinecraftFont.Length );
            }
            MinecraftFont = new Font( Fonts.Families[0], 12, FontStyle.Regular );
            ColorPairs = new[]{
                new ColorPair(0,0,0,0,0,0),
                new ColorPair(0,0,191,0,0,47),
                new ColorPair(0,191,0,0,47,0),
                new ColorPair(0,191,191,0,47,47),
                new ColorPair(191,0,0,47,0,0),
                new ColorPair(191,0,191,47,0,47),
                new ColorPair(191,191,0,47,47,0),
                new ColorPair(191,191,191,47,47,47),

                new ColorPair(64,64,64,16,16,16),
                new ColorPair(64,64,255,16,16,63),
                new ColorPair(64,255,64,16,63,16),
                new ColorPair(64,255,255,16,63,63),
                new ColorPair(255,64,64,63,16,16),
                new ColorPair(255,64,255,63,16,63),
                new ColorPair(255,255,64,63,63,16),
                new ColorPair(255,255,255,63,63,63)
            };
        }


        public ChatPreview() {
            InitializeComponent();
            DoubleBuffered = true;
        }


        sealed class TextSegment {
            public string Text;
            public ColorPair Color;
            public int X, Y;

            public void Draw( Graphics g ) {
                g.DrawString( Text, MinecraftFont, Color.Shadow, X + 2, Y + 2 );
                g.DrawString( Text, MinecraftFont, Color.Foreground, X, Y );
            }
        }

        static readonly Regex SplitByColorRegex = new Regex( "(&[0-9a-zA-Z])", RegexOptions.Compiled );
        TextSegment[] segments;

        public void SetText( string[] lines ) {
            List<TextSegment> newSegments = new List<TextSegment>();
            using( Bitmap b = new Bitmap( 1, 1 ) ) {
                using( Graphics g = Graphics.FromImage( b ) ) { // graphics for string mesaurement
                    g.TextRenderingHint = TextRenderingHint.SingleBitPerPixel;

                    int y = 5;
                    for( int i = 0; i < lines.Length; i++ ) {
                        if( lines[i] == null || lines[i].Length == 0 ) continue;
                        int x = 5;
                        string[] plainTextSegments = SplitByColorRegex.Split( lines[i] );

                        int color = Color.ParseToIndex( Color.White );

                        for( int j = 0; j < plainTextSegments.Length; j++ ) {
                            if( plainTextSegments[j].Length == 0 ) continue;
                            if( plainTextSegments[j][0] == '&' ) {
                                color = Color.ParseToIndex( plainTextSegments[j] );
                            } else {
                                newSegments.Add( new TextSegment {
                                    Color = ColorPairs[color],
                                    Text = plainTextSegments[j],
                                    X = x,
                                    Y = y
                                } );
                                x += (int)g.MeasureString( plainTextSegments[j], MinecraftFont ).Width;
                            }
                        }
                        y += 20;
                    }

                }
            }
            segments = newSegments.ToArray();
            Invalidate();
        }


        protected override void OnPaint( PaintEventArgs e ) {
            e.Graphics.DrawImageUnscaledAndClipped( Resources.ChatBackground, e.ClipRectangle );

            e.Graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixel;

            if( segments != null && segments.Length > 0 ) {
                for( int i = 0; i < segments.Length; i++ ) {
                    segments[i].Draw( e.Graphics );
                }
            }

            base.OnPaint( e );
        }
    }
}