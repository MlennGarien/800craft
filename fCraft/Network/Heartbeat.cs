// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using fCraft.Events;
using JetBrains.Annotations;
using System.Runtime.Serialization.Json;
using ServiceStack.Text;

namespace fCraft {
    /// <summary> Static class responsible for sending heartbeats. </summary>
    public static class Heartbeat {
        static readonly Uri MinecraftNetUri;
        static readonly Uri WoMDirectUri;
        /// <summary> Delay between sending heartbeats. Default: 25s </summary>
        public static TimeSpan Delay { get; set; }

        public static List<string> HbData = new List<string>();

        /// <summary> Request timeout for heartbeats. Default: 10s </summary>
        public static TimeSpan Timeout { get; set; }

        /// <summary> Secret string used to verify players' names.
        /// Randomly generated at startup.
        /// Known only to this server and to heartbeat servers. </summary>
        public static string Salt { get; internal set; }


        static Heartbeat() {
            MinecraftNetUri = new Uri( "http://www.minecraft.net/heartbeat.jsp" );
            WoMDirectUri = new Uri( "http://direct.worldofminecraft.com/hb.php" );
            Delay = TimeSpan.FromSeconds( 15 );
            Timeout = TimeSpan.FromSeconds( 15 );
            Salt = Server.GetRandomString( 32 );
        }


        internal static void Start() {
            Scheduler.NewBackgroundTask( Beat ).RunForever( Delay );
            HbSave();
        }


        static void Beat( SchedulerTask scheduledTask ) {
            if( Server.IsShuttingDown ) return;

            if( ConfigKey.HeartbeatEnabled.Enabled() ) {
                SendMinecraftNetBeat();
                if( ConfigKey.IsPublic.Enabled() && ConfigKey.HeartbeatToWoMDirect.Enabled() ) {
                    SendWoMDirectBeat();
                }

            } else {
                // If heartbeats are disabled, the server data is written
                // to a text file instead (heartbeatdata.txt)
                string[] data = new[]{
                    Salt,
                    Server.InternalIP.ToString(),
                    Server.Port.ToString(),
                    Server.CountPlayers( false ).ToString(),
                    ConfigKey.MaxPlayers.GetString(),
                    ConfigKey.ServerName.GetString(),
                    ConfigKey.IsPublic.GetString()
                };

                const string tempFile = Paths.HeartbeatDataFileName + ".tmp";
                File.WriteAllLines( tempFile, data, Encoding.ASCII );
                Paths.MoveOrReplace( tempFile, Paths.HeartbeatDataFileName );
            }
        }

        public static void HbSave()
        {
            File.Delete(Paths.HbDataFileName);
            HbData.Add("port=" + Server.Port.ToString() + "&max=" + ConfigKey.MaxPlayers.GetString() + "&name=" + 
                Uri.EscapeDataString(ConfigKey.ServerName.GetString())+
                "&public=True" + "&salt=" + Salt + "&users=" + Server.CountPlayers(false).ToString());

            const string SaverFile = Paths.HbDataFileName;
                File.WriteAllLines( SaverFile, HbData, Encoding.ASCII );
        }

        static void SendMinecraftNetBeat() {
            HeartbeatData data = new HeartbeatData( MinecraftNetUri );
            if( !RaiseHeartbeatSendingEvent( data, MinecraftNetUri, true ) ) {
                return;
            }
            
                HttpWebRequest request = CreateRequest(data.CreateUri());
                var state = new HeartbeatRequestState(request, data, true);
                request.BeginGetResponse(ResponseCallback, state);
        }


        static void SendWoMDirectBeat() {
            HeartbeatData data = new HeartbeatData( WoMDirectUri );

            // we dont want WoM redirecting back to minecraft.net
            data.CustomData["noforward"] = "1";

            if( !RaiseHeartbeatSendingEvent( data, WoMDirectUri, false ) ) {
                return;
            }
            HttpWebRequest request = CreateRequest( data.CreateUri() );
            var state = new HeartbeatRequestState( request, data, false );
            request.BeginGetResponse( ResponseCallback, state );
        }


        // Creates an asynchrnous HTTP request to the given URL
        static HttpWebRequest CreateRequest( Uri uri ) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create( uri );
            request.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint( Server.BindIPEndPointCallback );
            request.Method = "GET";
            request.Timeout = (int)Timeout.TotalMilliseconds;
            request.CachePolicy = new HttpRequestCachePolicy( HttpRequestCacheLevel.BypassCache );
            request.UserAgent = Updater.UserAgent;
            return request;
        }


        // Called when the heartbeat server responds.
        static void ResponseCallback( IAsyncResult result ) {
            if( Server.IsShuttingDown ) return;
            HeartbeatRequestState state = (HeartbeatRequestState)result.AsyncState;
            try {
                string responseText;
                using( HttpWebResponse response = (HttpWebResponse)state.Request.EndGetResponse( result ) ) {
                    // ReSharper disable AssignNullToNotNullAttribute
                    using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ) ) {
                        // ReSharper restore AssignNullToNotNullAttribute
                        responseText = responseReader.ReadToEnd();
                    }
                    RaiseHeartbeatSentEvent( state.Data, response, responseText );
                }

                // try parse response as server Uri, if needed
                if( state.GetServerUri ) {
                    string replyString = responseText.Trim();
                    if( replyString.StartsWith( "bad heartbeat", StringComparison.OrdinalIgnoreCase ) ) {
                        Logger.Log( LogType.Error, "Heartbeat: {0}", replyString );
                    } else {
                        try {
                            Uri newUri = new Uri( replyString );
                            Uri oldUri = Server.Uri;
                            if( newUri != oldUri ) {
                                Server.Uri = newUri;
                                RaiseUriChangedEvent( oldUri, newUri );
                            }
                        } catch( UriFormatException ) {
                            Logger.Log( LogType.Error,
                                        "Heartbeat: Server replied with: {0}",
                                        replyString );
                        }
                    }
                }
            } catch( Exception ex ) {
                if( ex is WebException || ex is IOException ) {
                    Logger.Log( LogType.Warning,
                                "Heartbeat: {0} is probably down ({1})",
                                state.Request.RequestUri.Host,
                                ex.Message );
                } else {
                    Logger.Log( LogType.Error, "Heartbeat: {0}", ex );
                } 
            } 
        }
        

        #region Events

        /// <summary> Occurs when a heartbeat is about to be sent (cancellable). </summary>
        public static event EventHandler<HeartbeatSendingEventArgs> Sending;

        /// <summary> Occurs when a heartbeat has been sent. </summary>
        public static event EventHandler<HeartbeatSentEventArgs> Sent;

        /// <summary> Occurs when the server Uri has been set or changed. </summary>
        public static event EventHandler<UriChangedEventArgs> UriChanged;


        static bool RaiseHeartbeatSendingEvent( HeartbeatData data, Uri uri, bool getServerUri ) {
            var h = Sending;
            if( h == null ) return true;
            var e = new HeartbeatSendingEventArgs( data, uri, getServerUri );
            h( null, e );
            return !e.Cancel;
        }

        static void RaiseHeartbeatSentEvent( HeartbeatData heartbeatData,
                                             HttpWebResponse response,
                                             string text ) {
            var h = Sent;
            if( h != null ) {
                h( null, new HeartbeatSentEventArgs( heartbeatData,
                                                     response.Headers,
                                                     response.StatusCode,
                                                     text ) );
            }
        }

        static void RaiseUriChangedEvent( Uri oldUri, Uri newUri ) {
            var h = UriChanged;
            if( h != null ) h( null, new UriChangedEventArgs( oldUri, newUri ) );
        }

        #endregion


        sealed class HeartbeatRequestState {
            public HeartbeatRequestState( HttpWebRequest request, HeartbeatData data, bool getServerUri ) {
                Request = request;
                Data = data;
                GetServerUri = getServerUri;
            }
            public readonly HttpWebRequest Request;
            public readonly HeartbeatData Data;
            public readonly bool GetServerUri;
        }
    }


    public sealed class HeartbeatData {
        internal HeartbeatData( [NotNull] Uri heartbeatUri ) {
            if( heartbeatUri == null ) throw new ArgumentNullException( "heartbeatUri" );
            IsPublic = ConfigKey.IsPublic.Enabled();
            MaxPlayers = ConfigKey.MaxPlayers.GetInt();
            PlayerCount = Server.CountPlayers( false );
            ServerIP = Server.InternalIP;
            Port = Server.Port;
            ProtocolVersion = Config.ProtocolVersion;
            Salt = Heartbeat.Salt;
            ServerName = ConfigKey.ServerName.GetString();
            CustomData = new Dictionary<string, string>();
            HeartbeatUri = heartbeatUri;
        }

             

        [NotNull]
        public Uri HeartbeatUri { get; private set; }
        public string Salt { get; set; }
        public IPAddress ServerIP { get; set; }
        public int Port { get; set; }
        public int PlayerCount { get; set; }
        public int MaxPlayers { get; set; }
        public string ServerName { get; set; }
        public bool IsPublic { get; set; }
        public int ProtocolVersion { get; set; }
        public Dictionary<string, string> CustomData { get; private set; }

        public Uri CreateUri() {
            UriBuilder ub = new UriBuilder( HeartbeatUri );
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat( "public={0}&max={1}&users={2}&port={3}&version={4}&salt={5}&name={6}",
                             IsPublic,
                             MaxPlayers,
                             PlayerCount,
                             Port,
                             ProtocolVersion,
                             Uri.EscapeDataString( Salt ),
                             Uri.EscapeDataString( ServerName ) );
            foreach( var pair in CustomData ) {
                sb.AppendFormat( "&{0}={1}",
                                 Uri.EscapeDataString( pair.Key ),
                                 Uri.EscapeDataString( pair.Value ) );
            }
            ub.Query = sb.ToString();
            return ub.Uri;
            
        }

    }
}


namespace fCraft.Events {
    public sealed class HeartbeatSentEventArgs : EventArgs {
        internal HeartbeatSentEventArgs( HeartbeatData heartbeatData,
                                         WebHeaderCollection headers,
                                         HttpStatusCode status,
                                         string text ) {
            HeartbeatData = heartbeatData;
            ResponseHeaders = headers;
            ResponseStatusCode = status;
            ResponseText = text;
        }

        public HeartbeatData HeartbeatData { get; private set; }
        public WebHeaderCollection ResponseHeaders { get; private set; }
        public HttpStatusCode ResponseStatusCode { get; private set; }
        public string ResponseText { get; private set; }
    }


    public sealed class HeartbeatSendingEventArgs : EventArgs, ICancellableEvent {
        internal HeartbeatSendingEventArgs( HeartbeatData data, Uri uri, bool getServerUri ) {
            HeartbeatData = data;
            Uri = uri;
            GetServerUri = getServerUri;
        }

        public HeartbeatData HeartbeatData { get; private set; }
        public Uri Uri { get; set; }
        public bool GetServerUri { get; set; }
        public bool Cancel { get; set; }
    }


    public sealed class UriChangedEventArgs : EventArgs {
        internal UriChangedEventArgs( Uri oldUri, Uri newUri ) {
            OldUri = oldUri;
            NewUri = newUri;
        }

        public Uri OldUri { get; private set; }
        public Uri NewUri { get; private set; }
    }
}