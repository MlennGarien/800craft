using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

//Copyright (C) <2012> <Jon Baker, Glenn Mariën and Lao Tszy>

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.


namespace fCraft {
    public class ShapesLib {
        #region Instancing

        public Player player; //player using command
        public int blockCount; //blockcount for player message. ++ when drawing
        int blocks = 0, //drawn blocks
            blocksDenied = 0; //denied blocks (zones, ect)
        fCraft.Drawing.UndoState undoState; //undostate
        Direction direction; //direction of the blocks (x++-- ect)
        Vector3I[] marks;
        int Radius = 0;

        //instance
        public ShapesLib ( Block BlockColor, Vector3I[] Marks, Player p, int radius, Direction dir ) {
            marks = Marks;
            direction = dir;
            blockCount = 0;
            player = p;
            PixelData.X = Marks[0].X;
            PixelData.Y = Marks[0].Y;
            PixelData.Z = Marks[0].Z;
            PixelData.BlockColor = BlockColor;
            undoState = player.DrawBegin( null );
            Radius = radius;
        }

        #endregion

        #region GoldenSpiral
        public void DrawSpiral () {
            DrawBitmap( Radius, Radius );
        }

        private void DrawBitmap ( int Width, int Height ) {
            // Determine the first rectangle's orientation and dimensions.
            double phi = ( 1 + Math.Sqrt( 5 ) ) / 2;
            RectOrientations orientation;
            int client_wid = Width;
            int client_hgt = Height;
            double wid, hgt;                // The rectangle's size.
            if ( client_wid > client_hgt ) {
                // Horizontal rectangle.
                orientation = RectOrientations.RemoveLeft;
                if ( client_wid / ( double )client_hgt > phi ) {
                    hgt = client_hgt;
                    wid = hgt * phi;
                } else {
                    wid = client_wid;
                    hgt = wid / phi;
                }
            } else {
                // Vertical rectangle.
                orientation = RectOrientations.RemoveTop;
                if ( client_hgt / ( double )client_wid > phi ) {
                    wid = client_wid;
                    hgt = wid * phi;
                } else {
                    hgt = client_hgt;
                    wid = hgt / phi;
                }
            }

            // Allow a margin.
            wid *= 0.9f;
            hgt *= 0.9f;

            // Center it.
            double x = ( client_wid - wid ) / 2;
            double y = ( client_hgt - hgt ) / 2;

            // Make the Bitmap.
            Bitmap bm = new Bitmap( client_wid, client_hgt );
            {

                // Draw the rectangles.
                using ( Graphics gr = Graphics.FromImage( bm ) ) {
                    gr.FillRectangle( Brushes.White, 0, 0, bm.Width, bm.Height );
                    // Draw the rectangles.
                    gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    List<PointF> points = new List<PointF>();
                    DrawPhiRectanglesOnGraphics( gr, points, x, y, wid, hgt, orientation );

                    // Draw the true spiral.
                    PointF start = points[0];
                    PointF origin = points[points.Count - 1];
                    float dx = start.X - origin.X;
                    float dy = start.Y - origin.Y;
                    double radius = Math.Sqrt( dx * dx + dy * dy );

                    double theta = Math.Atan2( dy, dx );
                    const int num_slices = 1000;
                    double dtheta = Math.PI / 2 / num_slices;
                    double factor = 1 - ( 1 / phi ) / num_slices * 0.78; //@
                    List<PointF> new_points = new List<PointF>();

                    // Repeat until dist is too small to see.
                    while ( radius > 0.1 ) {
                        PointF new_point = new PointF(
                            ( float )( origin.X + radius * Math.Cos( theta ) ),
                            ( float )( origin.Y + radius * Math.Sin( theta ) ) );
                        new_points.Add( new_point );
                        theta += dtheta;
                        radius *= factor;
                    }
                    gr.DrawLines( Pens.Blue, new_points.ToArray() );
                }
                bm = Crop( bm );
                bm.RotateFlip( RotateFlipType.Rotate180FlipX );
                Draw( bm );
                bm.Dispose();
            }
        }

        private void DrawPhiRectanglesOnGraphics ( Graphics gr, List<PointF> points, double x, double y, double wid, double hgt, RectOrientations orientation ) {
            if ( ( wid < 1 ) || ( hgt < 1 ) ) return;
            RectangleF rect;
            switch ( orientation ) {
                case RectOrientations.RemoveLeft:
                    rect = new RectangleF(
                        ( float )x, ( float )y, ( float )( 2 * hgt ), ( float )( 2 * hgt ) );
                    break;
                case RectOrientations.RemoveTop:
                    rect = new RectangleF(
                        ( float )( x - wid ), ( float )y, ( float )( 2 * wid ), ( float )( 2 * wid ) );
                    break;
                case RectOrientations.RemoveRight:
                    rect = new RectangleF(
                        ( float )( x + wid - 2 * hgt ),
                        ( float )( y - hgt ), ( float )( 2 * hgt ), ( float )( 2 * hgt ) );
                    break;
                case RectOrientations.RemoveBottom:
                    rect = new RectangleF( ( float )x, ( float )( y + hgt - 2 * wid ),
                        ( float )( 2 * wid ), ( float )( 2 * wid ) );
                    break;
            }


            // Recursively draw the next rectangle.
            switch ( orientation ) {
                case RectOrientations.RemoveLeft:
                    points.Add( new PointF( ( float )x, ( float )( y + hgt ) ) );
                    x += hgt;
                    wid -= hgt;
                    orientation = RectOrientations.RemoveTop;
                    break;
                case RectOrientations.RemoveTop:
                    points.Add( new PointF( ( float )x, ( float )y ) );
                    y += wid;
                    hgt -= wid;
                    orientation = RectOrientations.RemoveRight;
                    break;
                case RectOrientations.RemoveRight:
                    points.Add( new PointF( ( float )( x + wid ), ( float )y ) );
                    wid -= hgt;
                    orientation = RectOrientations.RemoveBottom;
                    break;
                case RectOrientations.RemoveBottom:
                    points.Add( new PointF( ( float )( x + wid ), ( float )( y + hgt ) ) );
                    hgt -= wid;
                    orientation = RectOrientations.RemoveLeft;
                    break;
            }
            DrawPhiRectanglesOnGraphics( gr, points, x, y, wid, hgt, orientation );
        }
        private enum RectOrientations {
            RemoveLeft,
            RemoveTop,
            RemoveRight,
            RemoveBottom
        }

        #endregion

        #region Polygon
        public void DrawRegularPolygon ( int sides, int startingAngle, bool FillPoly ) {
            //Get the location for each vertex of the polygon
            Point center = new Point( Radius / 2, Radius / 2 );
            Point[] verticies = CalculateVertices( sides, Radius / 2, startingAngle, center );

            //Render the polygon
            Bitmap polygon = new Bitmap( Radius + 1, Radius + 1 );
            using ( Graphics g = Graphics.FromImage( polygon ) ) {
                g.FillRectangle( Brushes.White, 0, 0, polygon.Width, polygon.Height );
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.DrawPolygon( Pens.Black, verticies );
                if ( FillPoly ) {
                    SolidBrush brush = new SolidBrush( System.Drawing.Color.Black );
                    g.FillPolygon( brush, verticies );
                }
            }
            polygon = Crop( polygon );
            polygon.RotateFlip( RotateFlipType.Rotate180FlipX );
            Draw( polygon );
            polygon.Dispose();
        }

        private Point[] CalculateVertices ( int sides, int radius, int startingAngle, Point center ) {
            if ( sides < 3 )
                throw new ArgumentException( "Polygon must have 3 sides or more." );

            List<Point> points = new List<Point>();
            float step = 360.0f / sides;

            float angle = startingAngle;
            for ( double i = startingAngle; i < startingAngle + 360.0; i += step ) {
                points.Add( DegreesToXY( angle, radius, center ) );
                angle += step;
            }

            return points.ToArray();
        }
        #endregion

        #region Multi-point star

        // Draw the indicated star in the rectangle.
        public void DrawStar ( int num_points, int Radius, bool Fill ) {
            Bitmap bmp = new Bitmap( Radius, Radius );
            using ( Graphics g = Graphics.FromImage( bmp ) ) {
                g.FillRectangle( Brushes.White, 0, 0, bmp.Width, bmp.Height );
                // Get the star's points.
                PointF[] star_points = MakeStarPoints( -Math.PI / 2, num_points, Radius / 2 );

                // Draw the star.
                SolidBrush brush = new SolidBrush( System.Drawing.Color.Black );
                if ( Fill ) {
                    g.FillPolygon( brush, star_points );
                }
                g.DrawPolygon( Pens.Black, star_points );
                bmp = Crop( bmp );
                bmp.RotateFlip( RotateFlipType.Rotate180FlipX );
                Draw( bmp );
                bmp.Dispose();
            }
        }

        // Generate the points for a star.
        private PointF[] MakeStarPoints ( double start_theta, int num_points, int Radius ) {
            double theta, dtheta;
            PointF[] result;
            float cx = Radius;
            float cy = Radius;
            int skip = ( int )( ( num_points - 1 ) / 2.0 );

            // If this is a polygon, don't bother with concave points.
            if ( skip == 1 ) {
                result = new PointF[num_points];
                theta = start_theta;
                dtheta = 2 * Math.PI / num_points;
                for ( int i = 0; i < num_points; i++ ) {
                    result[i] = new PointF(
                        ( float )( cx + cx * Math.Cos( theta ) ),
                        ( float )( cy + cy * Math.Sin( theta ) ) );
                    theta += dtheta;
                }
                return result;
            }

            // Find the radius for the concave vertices.
            double concave_radius = CalculateConcaveRadius( num_points, skip );

            // Make the points.
            result = new PointF[2 * num_points];
            theta = start_theta;
            dtheta = Math.PI / num_points;
            for ( int i = 0; i < num_points; i++ ) {
                result[2 * i] = new PointF(
                    ( float )( cx + cx * Math.Cos( theta ) ),
                    ( float )( cy + cy * Math.Sin( theta ) ) );
                theta += dtheta;
                result[2 * i + 1] = new PointF(
                    ( float )( cx + cx * Math.Cos( theta ) * concave_radius ),
                    ( float )( cy + cy * Math.Sin( theta ) * concave_radius ) );
                theta += dtheta;
            }
            return result;
        }

        // Calculate the inner star radius.
        private double CalculateConcaveRadius ( int num_points, int skip ) {
            // For really small numbers of points.
            if ( num_points < 5 ) return 0.33f;

            // Calculate angles to key points.
            double dtheta = 2 * Math.PI / num_points;
            double theta00 = -Math.PI / 2;
            double theta01 = theta00 + dtheta * skip;
            double theta10 = theta00 + dtheta;
            double theta11 = theta10 - dtheta * skip;

            // Find the key points.
            PointF pt00 = new PointF(
                ( float )Math.Cos( theta00 ),
                ( float )Math.Sin( theta00 ) );
            PointF pt01 = new PointF(
                ( float )Math.Cos( theta01 ),
                ( float )Math.Sin( theta01 ) );
            PointF pt10 = new PointF(
                ( float )Math.Cos( theta10 ),
                ( float )Math.Sin( theta10 ) );
            PointF pt11 = new PointF(
                ( float )Math.Cos( theta11 ),
                ( float )Math.Sin( theta11 ) );

            // See where the segments connecting the points intersect.
            bool lines_intersect, segments_intersect;
            PointF intersection, close_p1, close_p2;
            FindIntersection( pt00, pt01, pt10, pt11,
                out lines_intersect, out segments_intersect,
                out intersection, out close_p1, out close_p2 );

            // Calculate the distance between the
            // point of intersection and the center.
            return Math.Sqrt(
                intersection.X * intersection.X +
                  intersection.Y * intersection.Y );
        }

        // Find the point of intersection between
        // the lines p1 --> p2 and p3 --> p4.
        private void FindIntersection ( PointF p1, PointF p2, PointF p3, PointF p4,
            out bool lines_intersect, out bool segments_intersect,
            out PointF intersection, out PointF close_p1, out PointF close_p2 ) {
            // Get the segments' parameters.
            float dx12 = p2.X - p1.X;
            float dy12 = p2.Y - p1.Y;
            float dx34 = p4.X - p3.X;
            float dy34 = p4.Y - p3.Y;

            // Solve for t1 and t2
            float denominator = ( dy12 * dx34 - dx12 * dy34 );

            float t1;
            try {
                t1 = ( ( p1.X - p3.X ) * dy34 + ( p3.Y - p1.Y ) * dx34 ) / denominator;
            } catch {
                // The lines are parallel (or close enough to it).
                lines_intersect = false;
                segments_intersect = false;
                intersection = new PointF( float.NaN, float.NaN );
                close_p1 = new PointF( float.NaN, float.NaN );
                close_p2 = new PointF( float.NaN, float.NaN );
                return;
            }
            lines_intersect = true;

            float t2 = ( ( p3.X - p1.X ) * dy12 + ( p1.Y - p3.Y ) * dx12 ) / -denominator;

            // Find the point of intersection.
            intersection = new PointF( p1.X + dx12 * t1, p1.Y + dy12 * t1 );

            // The segments intersect if t1 and t2 are between 0 and 1.
            segments_intersect = ( ( t1 >= 0 ) && ( t1 <= 1 ) && ( t2 >= 0 ) && ( t2 <= 1 ) );

            // Find the closest points on the segments.
            if ( t1 < 0 ) {
                t1 = 0;
            } else if ( t1 > 1 ) {
                t1 = 1;
            }

            if ( t2 < 0 ) {
                t2 = 0;
            } else if ( t2 > 1 ) {
                t2 = 1;
            }

            close_p1 = new PointF( p1.X + dx12 * t1, p1.Y + dy12 * t1 );
            close_p2 = new PointF( p3.X + dx34 * t2, p3.Y + dy34 * t2 );
        }

        #endregion

        #region Draw
        public void Draw ( Bitmap img ) {
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
                player.Message( String.Format( "You are only allowed to run commands that affect up to {0} blocks. This one would affect {1} blocks.",
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
        #endregion

        #region Helpers

        private Point DegreesToXY ( float degrees, float radius, Point origin ) {
            Point xy = new Point();
            double radians = degrees * Math.PI / 180.0;

            xy.X = ( int )( Math.Cos( radians ) * radius + origin.X );
            xy.Y = ( int )( Math.Sin( -radians ) * radius + origin.Y );

            return xy;
        }

        public static Bitmap Crop ( Bitmap bmp ) {
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
                else break;
            }
            int bottommost = 0;
            for ( int row = h - 1; row >= 0; --row ) {
                if ( allWhiteRow( row ) )
                    bottommost = row;
                else break;
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
            if ( rightmost == 0 ) rightmost = w; // As reached left
            if ( bottommost == 0 ) bottommost = h; // As reached top.
            int croppedWidth = rightmost - leftmost;
            int croppedHeight = bottommost - topmost;
            if ( croppedWidth == 0 ) {// No border on left or right
                leftmost = 0;
                croppedWidth = w;
            }
            if ( croppedHeight == 0 ) {// No border on top or bottom
                topmost = 0;
                croppedHeight = h;
            } try {
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
        //stores information needed for each pixel
        public struct PixelData {
            public static int X;
            public static int Y;
            public static int Z;
            public static Block BlockColor;
        }

        //stolen from BuildingCommands
        #region DrawOneBlock
        static void DrawOneBlock ( Player player, Map map, Block drawBlock, Vector3I coord,
                                 BlockChangeContext context, ref int blocks, ref int blocksDenied, fCraft.Drawing.UndoState undoState ) {
            if ( map == null ) return;
            if ( player == null ) throw new ArgumentNullException( "player" );

            if ( !map.InBounds( coord ) ) return;
            Block block = map.GetBlock( coord );
            if ( block == drawBlock ) return;

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
        #endregion
        #endregion
    }
}
