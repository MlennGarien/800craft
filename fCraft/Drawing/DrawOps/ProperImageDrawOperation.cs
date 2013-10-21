using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    sealed class ProperImageDrawOperation : DrawOpWithBrush, IDisposable {
        static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds( 5 );

        public Uri ImageUrl { get; private set; }
        public Bitmap ImageBitmap { get; private set; }
        public BlockPalette Palette { get; private set; }
        
        Vector3I imageMultipliers;
        Vector3I layerOffset;

        int imageOffsetX,
            imageOffsetY;

        public override string Name {
            get {
                return "Image";
            }
        }

        public override string Description {
            get {
                // TODO: adjust name based on parameters
                return Name;
            }
        }


        public ProperImageDrawOperation( [NotNull] Player player )
            : base( player ) {
        }


        public override bool ReadParams( Command cmd ) {
            // get image URL
            string urlString = cmd.Next();
            if( String.IsNullOrEmpty( urlString ) ) {
                return false;
            }

            // if string starts with "++", load image from imgur
            if( urlString.StartsWith( "++" ) ) {
                urlString = "http://i.imgur.com/" + urlString.Substring( 2 );
            }

            // prepend the protocol, if needed (assume http)
            if( !urlString.StartsWith( "http://", StringComparison.OrdinalIgnoreCase ) ) {
                urlString = "http://" + urlString;
            }

            // validate the image URL
            Uri url;
            if( !Uri.TryCreate( urlString, UriKind.Absolute, out url ) ) {
                Player.Message( "DrawImage: Invalid URL given." );
                return false;
            } else if( !url.Scheme.Equals( Uri.UriSchemeHttp ) && !url.Scheme.Equals( Uri.UriSchemeHttps ) ) {
                Player.Message( "DrawImage: Invalid URL given. Only HTTP and HTTPS links are allowed." );
                return false;
            }
            ImageUrl = url;

            // Check if player gave optional second argument (palette name)
            string paletteName = cmd.Next();
            if( paletteName != null ) {
                StandardBlockPalettes paletteType;
                if( EnumUtil.TryParse( paletteName, out paletteType, true ) ) {
                    Palette = BlockPalette.GetPalette( paletteType );
                } else {
                    Player.Message( "DrawImage: Unrecognized palette \"{0}\". Available palettes are: \"{1}\"",
                                    paletteName,
                                    Enum.GetNames( typeof( StandardBlockPalettes ) ).JoinToString() );
                    return false;
                }
            } else {
                // default to "Light" (lit single-layer) palette
                Palette = BlockPalette.Light;
            }

            // All set
            return true;
        }


        public override bool Prepare( Vector3I[] marks ) {
            // Check the given marks
            if( marks == null )
                throw new ArgumentNullException( "marks" );
            if( marks.Length != 2 )
                throw new ArgumentException("DrawImage: Exactly 2 marks needed.", "marks");

            // Make sure that a direction was given
            Vector3I delta = marks[1] - marks[0];
            if( Math.Abs( delta.X ) == Math.Abs( delta.Y ) ) {
                throw new ArgumentException(
                    "DrawImage: Second mark must specify a definite direction (north, east, south, or west) from first mark.",
                    "marks" );
            }
            Marks = marks;

            // Download the image
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create( ImageUrl );
            request.Timeout = (int)DownloadTimeout.TotalMilliseconds;
            request.ServicePoint.BindIPEndPointDelegate = Server.BindIPEndPointCallback;
            request.UserAgent = Updater.UserAgent;
            using( HttpWebResponse response = (HttpWebResponse)request.GetResponse() ) {
                // Check that the remote file was found. The ContentType
                // check is performed since a request for a non-existent
                // image file might be redirected to a 404-page, which would
                // yield the StatusCode "OK", even though the image was not found.
                if( ( response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Moved ||
                      response.StatusCode == HttpStatusCode.Redirect ) &&
                    response.ContentType.StartsWith( "image", StringComparison.OrdinalIgnoreCase ) ) {
                    // if the remote file was found, download it
                    using( Stream inputStream = response.GetResponseStream() ) {
                        // TODO: check filesize limit?
                        ImageBitmap = new Bitmap( inputStream );
                    }
                } else {
                    throw new Exception( "Error downloading image: " + response.StatusCode );
                }
            }

            imageMultipliers = Vector3I.Zero;
            layerOffset = Vector3I.Zero;
            Vector3I endCoordOffset = Vector3I.Zero;

            // Figure out vertical drawing direction
            if( delta.Z < 0 ) {
                // drawing downwards
                imageMultipliers.Z = -1;
                imageOffsetY = Marks[0].Z;
                endCoordOffset.Z = 1 - ImageBitmap.Height;
            } else {
                // drawing upwards
                imageMultipliers.Z = 1;
                imageOffsetY = Math.Min( Map.Height, ImageBitmap.Height + Marks[0].Z ) - Marks[0].Z;
                endCoordOffset.Z = ImageBitmap.Height - 1;
            }

            // Figure out horizontal drawing direction and orientation
            if( Math.Abs( delta.X ) > Math.Abs( delta.Y ) ) {
                // drawing along the X-axis
                imageMultipliers.X = Math.Sign( delta.X );
                if( delta.X > 0 ) {
                    imageOffsetX = Marks[0].X;
                } else {
                    imageOffsetX = Math.Min( Map.Width, ImageBitmap.Width + Marks[0].X ) - Marks[0].X;
                }
                layerOffset.Y = ( delta.Y < 0 ) ? -1 : 1;
                endCoordOffset.X = ( ImageBitmap.Width - 1 )*Math.Sign( delta.X );
                endCoordOffset.Y = ( Palette.Layers - 1 )*Math.Sign( layerOffset.Y );

            } else {
                // drawing along the Y-axis
                imageMultipliers.Y = Math.Sign( delta.Y );
                if( delta.Y > 0 ) {
                    imageOffsetX = Marks[0].Y;
                } else {
                    imageOffsetX = Math.Min( Map.Length, ImageBitmap.Width + Marks[0].Y ) - Marks[0].Y;
                }
                layerOffset.X = ( delta.X < 0 ) ? -1 : 1;
                endCoordOffset.Y = ( ImageBitmap.Width - 1 )*Math.Sign( delta.Y );
                endCoordOffset.X = ( Palette.Layers - 1 )*Math.Sign( layerOffset.X );
            }

            // Calculate maximum bounds, and warn if we're pushing out of the map
            BoundingBox fullBounds = new BoundingBox( Marks[0], Marks[0] + endCoordOffset );
            if( fullBounds.XMin < 0 || fullBounds.XMax > Map.Width - 1 ) {
                Player.Message( "&WDrawImage: Not enough room horizontally (X), image cut off." );
            }
            if( fullBounds.YMin < 0 || fullBounds.YMax > Map.Length - 1 ) {
                Player.Message( "&WDrawImage: Not enough room horizontally (Y), image cut off." );
            }
            if( fullBounds.ZMin < 0 || fullBounds.ZMax > Map.Height - 1 ) {
                Player.Message( "&WDrawImage: Not enough room vertically, image cut off." );
            }

            // clip bounds to world boundaries
            Bounds = Map.Bounds.GetIntersection( fullBounds );
            refCoords = Marks[0];

            Brush = this;
            return true;
        }

        Block[] drawBlocks;
        int layer;
        Vector3I refCoords;

        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            for( ; refCoords.X <= Bounds.XMax; refCoords.X++ ) {
                for( ; refCoords.Y <= Bounds.YMax; refCoords.Y++ ) {
                    for( ; refCoords.Z <= Bounds.ZMax; refCoords.Z++ ) {
                        // find matching palette entry
                        int imageX = imageOffsetX + imageMultipliers.X*refCoords.X + imageMultipliers.Y*refCoords.Y;
                        int imageY = imageOffsetY + imageMultipliers.Z*refCoords.Z;
                        System.Drawing.Color color = ImageBitmap.GetPixel( imageX, imageY );
                        drawBlocks = Palette.FindBestMatch( color );

                        // draw layers
                        for( ; layer < Palette.Layers; layer++ ) {
                            Coords = refCoords + layerOffset*layer;
                            if( DrawOneBlock() ) {
                                blocksDone++;
                                if( blocksDone >= maxBlocksToDraw ) {
                                    layer++;
                                    return blocksDone;
                                }
                            }
                        }
                        layer = 0;
                    }
                    Coords.Z = Bounds.ZMin;
                }
                Coords.Y = Bounds.YMin;
                if( TimeToEndBatch ) {
                    Coords.X++;
                    return blocksDone;
                }
            }
            IsDone = true;
            return blocksDone;
        }


        protected override Block NextBlock() {
            return drawBlocks[layer];
        }


        public void Dispose() {
            if( ImageBitmap != null ) {
                ImageBitmap.Dispose();
                ImageBitmap = null;
            }
        }
    }
}