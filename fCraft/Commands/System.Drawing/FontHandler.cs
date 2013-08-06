using System;
using System.Drawing;

/*        ----
        Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com>
        All rights reserved.

        Redistribution and use in source and binary forms, with or without
        modification, are permitted provided that the following conditions are met:
         * Redistributions of source code must retain the above copyright
              notice, this list of conditions and the following disclaimer.
            * Redistributions in binary form must reproduce the above copyright
             notice, this list of conditions and the following disclaimer in the
             documentation and/or other materials provided with the distribution.
            * Neither the name of 800Craft or the names of its
             contributors may be used to endorse or promote products derived from this
             software without specific prior written permission.

        THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
        ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
        WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
        DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
        DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
        (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
        LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
        ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
        (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
        SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
        ----*/

namespace fCraft {

    public class FontHandler {

        #region Instance and Drawing

        public Player player; //player using command
        public int blockCount; //blockcount for player message. ++ when drawing

        private int blocks = 0, //drawn blocks
            blocksDenied = 0; //denied blocks (zones, ect)

        private fCraft.Drawing.UndoState undoState; //undostate
        private Direction direction; //direction of the blocks (x++-- ect)

        //instance
        public FontHandler( Block textColor, Vector3I[] Marks, Player p, Direction dir ) {
            direction = dir;
            blockCount = 0;
            player = p;
            PixelData.X = Marks[0].X;
            PixelData.Y = Marks[0].Y;
            PixelData.Z = Marks[0].Z;
            PixelData.BlockColor = textColor;
            undoState = player.DrawBegin( null );
        }

        public void CreateGraphicsAndDraw( string Sentence ) {
            SizeF size = MeasureTextSize( Sentence, player.font ); //measure the text size to create a bmp)
            Bitmap img = new Bitmap( ( int )size.Width, ( int )size.Height ); //make an image based on string size
            using ( Graphics g = Graphics.FromImage( img ) ) { //IDisposable
                g.FillRectangle( Brushes.White, 0, 0, img.Width, img.Height ); //make background, else crop will not work
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit; //fix to bleeding
                g.DrawString( Sentence, player.font, Brushes.Black, 0, 0 ); //draw some sexytext
                img = Crop( img ); //crop the image to fix all problems with location
                img.RotateFlip( RotateFlipType.Rotate180FlipX ); //flip this badboy
                Draw( img ); //make the blockupdates
                img.Dispose(); //gtfo homeslice
            }
        }

        public void Draw( Bitmap img ) {
            //guess how big the draw will be
            int Count = 0;
            for ( int x = 0; x < img.Width; x++ ) {
                for ( int z = 0; z < img.Height; z++ ) {
                    if ( img.GetPixel( x, z ).ToArgb() != System.Drawing.Color.White.ToArgb() ) {
                        Count++;
                    }
                }
            }
            //check if player can make the drawing
            if ( !player.CanDraw( Count ) ) {
                player.MessageNow( String.Format( "You are only allowed to run commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                               player.Info.Rank.DrawLimit, Count ) );
                return;
            }
            //check direction and draw
            switch ( direction ) {
                case Direction.one:
                    for ( int x = 0; x < img.Width; x++ ) {
                        for ( int z = 0; z < img.Height; z++ ) {
                            if ( img.GetPixel( x, z ).ToArgb() != System.Drawing.Color.White.ToArgb() ) {
                                DrawOneBlock( player, player.World.Map, PixelData.BlockColor,
                                      new Vector3I( ( PixelData.X + x ), PixelData.Y, ( PixelData.Z + z ) ), BlockChangeContext.Drawn,
                                      ref blocks, ref blocksDenied, undoState );
                                blockCount++;
                            }
                        }
                    }
                    break;

                case Direction.two:
                    for ( int x = 0; x < img.Width; x++ ) {
                        for ( int z = 0; z < img.Height; z++ ) {
                            if ( img.GetPixel( x, z ).ToArgb() != System.Drawing.Color.White.ToArgb() ) {
                                DrawOneBlock( player, player.World.Map, PixelData.BlockColor,
                                      new Vector3I( ( PixelData.X - x ), PixelData.Y, ( PixelData.Z + z ) ), BlockChangeContext.Drawn,
                                      ref blocks, ref blocksDenied, undoState );
                                blockCount++;
                            }
                        }
                    }
                    break;

                case Direction.three:
                    for ( int y = 0; y < img.Width; y++ ) {
                        for ( int z = 0; z < img.Height; z++ ) {
                            if ( img.GetPixel( y, z ).ToArgb() != System.Drawing.Color.White.ToArgb() ) {
                                DrawOneBlock( player, player.World.Map, PixelData.BlockColor,
                                      new Vector3I( PixelData.X, ( PixelData.Y + y ), ( PixelData.Z + z ) ), BlockChangeContext.Drawn,
                                      ref blocks, ref blocksDenied, undoState );
                                blockCount++;
                            }
                        }
                    }
                    break;

                case Direction.four:
                    for ( int y = 0; y < img.Width; y++ ) {
                        for ( int z = 0; z < img.Height; z++ ) {
                            if ( img.GetPixel( y, z ).ToArgb() != System.Drawing.Color.White.ToArgb() ) {
                                DrawOneBlock( player, player.World.Map, PixelData.BlockColor,
                                      new Vector3I( PixelData.X, ( ( PixelData.Y ) - y ), ( PixelData.Z + z ) ), BlockChangeContext.Drawn,
                                      ref blocks, ref blocksDenied, undoState );
                                blockCount++;
                            }
                        }
                    }
                    break;

                default:
                    break; //if blockcount = 0, message is shown and returned
            }
        }

        #endregion Instance and Drawing

        #region Helpers

        public static Bitmap Crop( Bitmap bmp ) {
            int w = bmp.Width;
            int h = bmp.Height;
            Func<int, bool> allWhiteRow = row => {
                for ( int i = 0; i < w; ++i )
                    if ( bmp.GetPixel( i, row ).R != 255 )
                        return false;
                return true;
            };
            Func<int, bool> allWhiteColumn = col => {
                for ( int i = 0; i < h; ++i )
                    if ( bmp.GetPixel( col, i ).R != 255 )
                        return false;
                return true;
            };
            int topmost = 0;
            for ( int row = 0; row < h; ++row ) {
                if ( allWhiteRow( row ) )
                    topmost = row;
                else
                    break;
            }
            int bottommost = 0;
            for ( int row = h - 1; row >= 0; --row ) {
                if ( allWhiteRow( row ) )
                    bottommost = row;
                else
                    break;
            }
            int leftmost = 0, rightmost = 0;
            for ( int col = 0; col < w; ++col ) {
                if ( allWhiteColumn( col ) )
                    leftmost = col;
                else
                    break;
            }
            for ( int col = w - 1; col >= 0; --col ) {
                if ( allWhiteColumn( col ) )
                    rightmost = col;
                else
                    break;
            }
            if ( rightmost == 0 )
                rightmost = w; // As reached left
            if ( bottommost == 0 )
                bottommost = h; // As reached top.
            int croppedWidth = rightmost - leftmost;
            int croppedHeight = bottommost - topmost;
            if ( croppedWidth == 0 ) {// No border on left or right
                leftmost = 0;
                croppedWidth = w;
            }
            if ( croppedHeight == 0 ) {// No border on top or bottom
                topmost = 0;
                croppedHeight = h;
            }
            try {
                var target = new Bitmap( croppedWidth, croppedHeight );
                using ( Graphics g = Graphics.FromImage( target ) ) {
                    g.DrawImage( bmp,
                      new RectangleF( 0, 0, croppedWidth, croppedHeight ),
                      new RectangleF( leftmost, topmost, croppedWidth, croppedHeight ),
                      GraphicsUnit.Pixel );
                }
                return target;
            } catch {
                return bmp; //return original image, I guess
            }
        }

        //Measure the size of the string length using IDisposable. Backport from 800Craft Client
        public static SizeF MeasureTextSize( string text, Font font ) {
            using ( Bitmap bmp = new Bitmap( 1, 1 ) ) {
                using ( Graphics g = Graphics.FromImage( bmp ) ) {
                    return g.MeasureString( text, font );
                }
            }
        }

        //stores information needed for each pixel
        public struct PixelData {
            public static int X;
            public static int Y;
            public static int Z;
            public static Block BlockColor;
        }

        //stolen from BuildingCommands

        #region DrawOneBlock

        private static void DrawOneBlock( Player player, Map map, Block drawBlock, Vector3I coord,
                                 BlockChangeContext context, ref int blocks, ref int blocksDenied, fCraft.Drawing.UndoState undoState ) {
            if ( map == null )
                return;
            if ( player == null )
                throw new ArgumentNullException( "player" );

            if ( !map.InBounds( coord ) )
                return;
            Block block = map.GetBlock( coord );
            if ( block == drawBlock )
                return;

            if ( player.CanPlace( map, coord, drawBlock, context ) != CanPlaceResult.Allowed ) {
                blocksDenied++;
                return;
            }

            map.QueueUpdate( new BlockUpdate( null, coord, drawBlock ) );
            Player.RaisePlayerPlacedBlockEvent( player, map, coord, block, drawBlock, context );

            if ( !undoState.IsTooLargeToUndo ) {
                if ( !undoState.Add( coord, block ) ) {
                    player.Message( "NOTE: This draw command is too massive to undo." );
                    player.LastDrawOp = null;
                }
            }
            blocks++;
        }

        #endregion DrawOneBlock

        #endregion Helpers
    }
}