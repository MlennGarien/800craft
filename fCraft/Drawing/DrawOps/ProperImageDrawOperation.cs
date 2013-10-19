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
        public Direction Direction { get; private set; }
        public Bitmap ImageBitmap { get; private set; }
        public BlockPalette Palette { get; private set; }
        public int ImageX { get; private set; }
        public int ImageY { get; private set; }

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
                throw new ArgumentException( "DrawImage: Exactly 2 marks needed.", "marks" );

            // Make sure that a direction was given
            Direction = DirectionFinder.GetDirection( marks );
            if( Direction == Direction.Null ) {
                throw new ArgumentException( "No direction was set." );
            }

            // Download the image
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create( ImageUrl );
            request.Timeout = (int)DownloadTimeout.TotalMilliseconds;
            request.ServicePoint.BindIPEndPointDelegate = Server.BindIPEndPointCallback;
            request.UserAgent = Updater.UserAgent;
            using( HttpWebResponse response = (HttpWebResponse)request.GetResponse() ) {
                // Check that the remote file was found. The ContentType
                // check is performed since a request for a non-existent
                // image file might be redirected to a 404-page, which would
                // yield the StatusCode "OK", even though the image was not
                // found.
                if( ( response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Moved ||
                      response.StatusCode == HttpStatusCode.Redirect ) &&
                    response.ContentType.StartsWith( "image", StringComparison.OrdinalIgnoreCase ) ) {
                    // if the remote file was found, download it
                    using( Stream inputStream = response.GetResponseStream() ) {
                        // TODO: check filesize limit?
                        ImageBitmap = new Bitmap( inputStream );
                    }
                }
            }

            // Calculate maximum bounds, and warn if we're pushing out of the map
            Bounds = new BoundingBox( Marks[0], Marks[0] + GetSize() );
            if( Bounds.XMin < 0 || Bounds.XMax > Map.Width - 1 ) {
                Player.Message("&WDrawImage: Not enough room horizontally (X), image cut off.");
            }
            if( Bounds.YMin < 0 || Bounds.YMax > Map.Length - 1 ) {
                Player.Message("&WDrawImage: Not enough room horizontally (Y), image cut off.");
            }
            if( Bounds.ZMin < 0 || Bounds.ZMax > Map.Height - 1 ) {
                Player.Message("&WDrawImage: Not enough room vertically, image cut off.");
            }
            // clip bounds to world boundaries
            Bounds = Map.Bounds.GetIntersection(Bounds);
            Coords = Bounds.MinVertex;

            // TODO: compute starting/ending points on the image
            // TODO: set ImageX/ImageY

            return true;
        }


        // Gets the total maximum size of the drawn image, in blocks, by considering image dimensions and palette layers
        Vector3I GetSize() {
            switch( Direction ) {
                case Direction.one: // X++
                    return new Vector3I(ImageBitmap.Width, Palette.Layers, ImageBitmap.Height);
                case Direction.two: // X--
                    return new Vector3I(-ImageBitmap.Width, Palette.Layers, ImageBitmap.Height);
                case Direction.three: // Y++
                    return new Vector3I(Palette.Layers, ImageBitmap.Width, ImageBitmap.Height);
                case Direction.four: // Y--
                    return new Vector3I(Palette.Layers, -ImageBitmap.Width, ImageBitmap.Height);
                default:
                    throw new NotSupportedException("Out-of-range Dimension value");
            }
        }


        // TODO: write a function to go from ImageX/ImageY to world coords
        // TODO: write a function to advance by one pixel (modify ImageX/ImageY)



        public override int DrawBatch( int maxBlocksToDraw ) {
            // TODO: perform iteration here
            throw new NotImplementedException();
        }


        protected override Block NextBlock() {
            // TODO: perform color conversion here
            throw new NotImplementedException();
        }


        public void Dispose() {
            if( ImageBitmap != null ) {
                ImageBitmap.Dispose();
                ImageBitmap = null;
            }
        }
    }
}