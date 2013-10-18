using System;
using System.Drawing;
using System.IO;
using System.Net;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    sealed class ProperImageDrawOperation : DrawOpWithBrush {
        static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(5);

        public Uri ImageUrl { get; private set; }
        public Direction Direction { get; private set; }
        public Bitmap ImageBitmap { get; private set; }

        public override string Name {
            get { return "Image"; }
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

            // TODO: add support for optional palette parameter

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

            // TODO: set proper bounds here
            Bounds = new BoundingBox(Marks[0], Marks[1]);

            return true;
        }


        public override int DrawBatch( int maxBlocksToDraw ) {
            // TODO: perform iteration here
            throw new NotImplementedException();
        }


        protected override Block NextBlock() {
            // TODO: perform color conversion here
            throw new NotImplementedException();
        }
    }

    enum BlockPalette {
        Basic,
        Layered
        // TODO: CPE basic/layered palettes
    }
}