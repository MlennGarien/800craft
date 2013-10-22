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

        Block[] drawBlocks;

        int imageX,
            imageY,
            layer;

        public override string Name { get { return "Image"; } }

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
                throw new ArgumentException( "DrawImage: Exactly 2 marks needed.", "marks" );

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
                if( (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Moved ||
                     response.StatusCode == HttpStatusCode.Redirect) &&
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

            Vector3I endCoordOffset = CalculateCoordConversion( delta );

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
            BlocksTotalEstimate = Bounds.Volume;

            // set starting coordinate
            imageX = minX;
            imageY = minY;
            layer = 0;

            Brush = this;
            return true;
        }


        Vector3I coordOffsets = Vector3I.Zero;
        Vector3I layerVector = Vector3I.Zero;
        int coordMultiplierX;
        int coordMultiplierY;

        int IAH;
        int IAW;
        int minY;
        int maxY;
        int minX;
        int maxX;


        Vector3I CalculateCoordConversion( Vector3I delta ) {
            Vector3I endCoordOffset = Vector3I.Zero;
            int IH = ImageBitmap.Height;
            int IW = ImageBitmap.Width;

            // Figure out vertical drawing direction
            if( delta.Z < 0 ) {
                // drawing downwards
                IAH = Math.Min( Marks[0].Z + 1, IH );
                minY = 0;
                maxY = IAH - 1;
                coordOffsets.Z = Marks[0].Z;
            } else {
                // drawing upwards
                IAH = Math.Min( Marks[0].Z + IH, Map.Height ) - Marks[0].Z;
                minY = IH - IAH;
                maxY = IH - 1;
                coordOffsets.Z = (IAH - 1) + Marks[0].Z;
            }

            // Figure out horizontal drawing direction and orientation
            if( Math.Abs( delta.X ) > Math.Abs( delta.Y ) ) {
                // drawing along the X-axis
                bool faceTowardsOrigin = delta.Y < 0 || delta.Y == 0 && Marks[0].Y < Map.Length/2;
                coordOffsets.Y = Marks[0].Y;
                if( delta.X > 0 ) {
                    // X+
                    IAW = Math.Min( Marks[0].X + IW, Map.Width ) - Marks[0].X;
                    if( faceTowardsOrigin ) {
                        // X+y+
                        minX = IW - IAW;
                        maxX = IW - 1;
                        coordOffsets.X = Marks[0].X + (IAW - 1);
                        coordMultiplierX = -1;
                        layerVector.Y = -1;
                    } else {
                        // X+y-
                        minX = 0;
                        maxX = IAW - 1;
                        coordOffsets.X = Marks[0].X;
                        coordMultiplierX = 1;
                        layerVector.Y = 1;
                    }
                } else {
                    // X-
                    IAW = Math.Min( Marks[0].X + 1, IW );
                    if( faceTowardsOrigin ) {
                        // X-y+
                        minX = 0;
                        maxX = IAW - 1;
                        coordOffsets.X = Marks[0].X;
                        coordMultiplierX = -1;
                        layerVector.Y = -1;
                    } else {
                        // X-y-
                        minX = IW - IAW;
                        maxX = IW - 1;
                        coordOffsets.X = Marks[0].X - (IAW - 1);
                        coordMultiplierX = 1;
                        layerVector.Y = 1;
                    }
                }
            } else {
                // drawing along the Y-axis
                bool faceTowardsOrigin = delta.X < 0 || delta.X == 0 && Marks[0].X < Map.Width/2;
                coordOffsets.X = Marks[0].X;
                if( delta.Y > 0 ) {
                    // Y+
                    IAW = Math.Min( Marks[0].Y + IW, Map.Length ) - Marks[0].Y;
                    if( faceTowardsOrigin ) {
                        // Y+x+
                        minX = 0;
                        maxX = IAW - 1;
                        coordOffsets.Y = Marks[0].Y;
                        coordMultiplierY = 1;
                        layerVector.X = -1;
                    } else {
                        // Y+x-
                        minX = IW - IAW;
                        maxX = IW - 1;
                        coordOffsets.Y = Marks[0].Y + (IAW - 1);
                        coordMultiplierY = -1;
                        layerVector.X = 1;
                    }
                } else {
                    // Y-
                    IAW = Math.Min( Marks[0].Y + 1, IW );
                    if( faceTowardsOrigin ) {
                        // Y-x+
                        minX = IW - IAW;
                        maxX = IW - 1;
                        coordOffsets.Y = Marks[0].Y - (IAW - 1);
                        coordMultiplierY = 1;
                        layerVector.X = -1;
                    } else {
                        // Y-x-
                        minX = 0;
                        maxX = IAW - 1;
                        coordOffsets.Y = Marks[0].Y;
                        coordMultiplierY = -1;
                        layerVector.X = 1;
                    }
                }
            }
            return endCoordOffset;
        }


        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            for( ; imageX <= maxX; imageX++ ) {
                for( ; imageY <= maxY; imageY++ ) {
                    // find matching palette entry
                    System.Drawing.Color color = ImageBitmap.GetPixel( imageX, imageY );
                    drawBlocks = Palette.FindBestMatch( color );
                    Coords.Z = coordOffsets.Z - (imageY - minY);
                    // draw layers
                    for( ; layer < Palette.Layers; layer++ ) {
                        Coords.X = (imageX - minX)*coordMultiplierX + coordOffsets.X + layerVector.X*layer;
                        Coords.Y = (imageX - minX)*coordMultiplierY + coordOffsets.Y + layerVector.Y*layer;
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
                imageY = minY;
                if( TimeToEndBatch ) {
                    imageX++;
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