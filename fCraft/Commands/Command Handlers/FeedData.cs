using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using fCraft.Drawing;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {

    public static class FeedSettings {
        public static Bitmap ImageCache = null;

        public static void PlayerJoiningWorld( object sender, PlayerJoinedWorldEventArgs e ) {
            foreach ( FeedData data in e.NewWorld.Feeds.Values.Where( f => !f.started ) ) {
                if ( data.world.Name == e.NewWorld.Name ) {
                    data.Start();
                }
            }
        }

        public static void PlayerPlacingBlock( object sender, PlayerPlacingBlockEventArgs e ) {
            foreach ( FeedData feed in e.Player.World.Feeds.Values ) {
                for ( int x = feed.StartPos.X; x <= feed.FinishPos.X; x++ ) {
                    for ( int y = feed.StartPos.Y; y <= feed.FinishPos.Y; y++ ) {
                        for ( int z = feed.StartPos.Z; z <= feed.FinishPos.Z; z++ ) {
                            if ( e.Coords == new Vector3I( x, y, z ) ) {
                                e.Result = CanPlaceResult.Revert;
                            }
                        }
                    }
                }
            }
        }
    }

    public sealed class FeedData {
        public bool started = false;
        public Vector3I StartPos; //the start position
        public Vector3I FinishPos; //the end position, start -> end

        public Vector3I EndPos; //the moving position

        public Vector3I Pos; //the position of the blockupdate
        public List<byte> last = new List<byte>();
        public List<byte> originalMap = new List<byte>();
        public byte textType; //font color
        public byte bgType; //background color

        public int SPACE_UNIT = 4; //width of space character in blocks
        public List<bool>[] chars = new List<bool>[126 - 32];
        public World world;
        public string Sentence;
        public int MessageCount;
        public ConcurrentDictionary<string, Vector3I> Blocks; //the blocks currently on the feed
        public readonly int Id;
        public static List<String> Messages;
        public Direction direction;
        public SchedulerTask task;
        public Player player;

        public FeedData( Block _textType, Vector3I _pos, Bitmap Image, World world, Direction direction_, Player player_ ) {
            direction = direction_;
            Blocks = new ConcurrentDictionary<string, Vector3I>();
            Init( Image, world );
            Pos = _pos;
            textType = ( byte )_textType;
            bgType = ( byte )Block.Air;
            FeedData.AddMessages();
            MessageCount = 0;
            Sentence = FeedData.Messages[MessageCount];
            Id = System.Threading.Interlocked.Increment( ref feedCounter );
            player = player_;
            NormalBrush brush = new NormalBrush( Block.Wood );
            DrawOperation Operation = new CuboidWireframeDrawOperation( player );
            Operation.AnnounceCompletion = false;
            Operation.Brush = brush;
            Operation.Context = BlockChangeContext.Drawn;

            if ( !Operation.Prepare( new[] { StartPos, FinishPos } ) ) {
                throw new Exception( "Unable to cubw frame." );
            }

            Operation.Begin();
            AddFeedToList( this, world );

            Start();
        }

        public void Start() {
            started = true;
            task = Scheduler.NewTask( StartFeed );
            task.RunForever( TimeSpan.FromMilliseconds( 600 ) );
        }

        public static int feedCounter;
        private static readonly object FeedListLock = new object();

        private static void AddFeedToList( [NotNull] FeedData data, [NotNull] World world ) {
            if ( data == null )
                throw new ArgumentNullException( "Feed" );
            lock ( FeedListLock ) {
                world.Feeds.Add( data.Id, data );
            }
        }

        public static void RemoveFeedFromList( [NotNull] FeedData data, [NotNull] World world ) {
            if ( data == null )
                throw new ArgumentNullException( "feed" );
            lock ( FeedListLock ) {
                world.Feeds.Remove( data.Id );
            }
        }

        public FeedData[] FeedList {
            get {
                lock ( FeedListLock ) {
                    return world.Feeds.Values.ToArray();
                }
            }
        }

        [CanBeNull]
        public static FeedData FindFeedById( int id, World world ) {
            lock ( FeedListLock ) {
                FeedData result;
                if ( world.Feeds.TryGetValue( id, out result ) ) {
                    return result;
                } else {
                    return null;
                }
            }
        }

        private static readonly Permission[] per = new Permission[] { Permission.Promote, Permission.Demote, Permission.ReadStaffChat };

        public static void AddMessages() {
            if ( Messages != null ) {
                Messages.Clear();
            }
            Messages = new List<string>
            {
                "Server Time: " + DateTime.UtcNow.ToString("HH:mm:ss tt"),
                "Welcome to " + ConfigKey.ServerName.GetString(),
                Server.Players.Count(p => !p.Info.IsHidden).ToString() + " players online",
                "Staff online: " +
                Server.Players.Where(p => !p.Info.IsHidden && p.Can(per))
                    .JoinToString(r => String.Format("{0}", r.Name)),
                "Counting " + PlayerDB.BannedCount + " banned players (" + Math.Round(PlayerDB.BannedPercentage) + "%)",
                "This server runs 800Craft " + Updater.CurrentRelease.VersionString,
                "Type /Review to get staff to review your builds",
                "Griefers are lame!",
                "This scrolling feed is cool!",
                "This server has had " + PlayerDB.PlayerInfoList.Count() + " unique visitors"
            };
        }

        public void Init( Bitmap image, [NotNull] World _world ) {
            if (_world == null) throw new ArgumentNullException("_world");
            world = _world;
            var pixels = new List<List<bool>>();
            //open up PNG file
            if ( FeedSettings.ImageCache == null ) {
                FeedSettings.ImageCache = image;
            }
            Bitmap bmp = FeedSettings.ImageCache;
            for ( int x = 0; x < bmp.Width; x++ ) {
                pixels.Add( new List<bool>() );
                for ( int y = 0; y < bmp.Height; y++ ) {
                    pixels[x].Add( bmp.GetPixel( x, y ).GetBrightness() > 0.5 );
                }
            }
            //extract characters from PNG file
            for ( int c = 33; c < 126; c++ ) {
                var charBlocks = new List<bool>();
                bool space = true;
                int bmpOffsetX = ( ( c - 32 ) % 16 ) * 8;
                int bmpOffsetY = ( ( int )( ( c - 32 ) / 16 ) ) * 8;
                for ( int X = 7; X >= 0; X-- ) {
                    for ( int Y = 0; Y < 8; Y++ ) {
                        bool type = pixels[X + bmpOffsetX][Y + bmpOffsetY];
                        if ( space ) {
                            if ( type ) {
                                Y = -1;
                                space = false;
                            }
                        } else {
                            charBlocks.Add( type );
                        }
                    }
                }
                charBlocks.Reverse();
                chars[c - 32] = charBlocks;
            }
            chars[0] = new List<bool>();
            for ( int i = 0; i < ( SPACE_UNIT - 2 ) * 8; i++ ) {
                chars[0].Add( false );
            }
        }

        public bool Done = true; //check to see if one cycle is complete

        private void StartFeed( SchedulerTask task ) {
            if ( !started ) { task.Stop(); return; }
            if ( !Done )
                return;
            try {
                Done = false;
                RemoveText();
                if ( ChangeMessage ) {
                    switch ( direction ) {
                        case Direction.one:
                        case Direction.two:
                            EndPos.X = FinishPos.X;
                            break;

                        case Direction.three:
                        case Direction.four:
                            EndPos.Y = FinishPos.Y;
                            break;
                    }
                    PickNewMessage();
                    ChangeMessage = false;
                }
                switch ( direction ) {
                    case Direction.one:
                        EndPos.X -= 7;
                        break;

                    case Direction.two:
                        EndPos.X += 7;
                        break;

                    case Direction.three:
                        EndPos.Y -= 7;
                        break;

                    case Direction.four:
                        EndPos.Y += 7;
                        break;
                }
                Render( Sentence );
            } catch ( Exception e ) {
                Logger.Log( LogType.Error, e.ToString() );
            }
        }

        public bool ChangeMessage = false; //a check if the previous sentence is complete

        public void RemoveText() {
            if ( Blocks.Values.Count < 1 ) { ChangeMessage = true; }
            foreach ( Vector3I block in Blocks.Values ) {
                if ( world.Map == null ) {
                    started = false;
                    return;
                }
                if ( world.IsLoaded ) {
                    Vector3I removed;
                    world.Map.QueueUpdate( new BlockUpdate( null, block, Block.Black ) );
                    Blocks.TryRemove( block.ToString(), out removed );
                }
            }
        }

        //makes the block updates
        public void Render( string text ) {
            var current = new List<byte>();
            for ( int p = 0; p < text.Length; p++ ) {
                char c = text[p];
                List<bool> charTemp = chars[c - 32];
                foreach (bool t in charTemp)
                {
                    current.Add(t ? textType : bgType);
                }
                if (p == text.Length - 1) continue;
                for ( int s = 0; s < 8; s++ ) {
                    current.Add( bgType );
                }
            }
            if ( current.Count < last.Count ) {
                for ( int j = current.Count; j < last.Count; j++ ) {
                    if ( last[j] != originalMap[j] ) {
                        SendWorldBlock( j, originalMap[j] );
                    }
                }
            }
            for ( int k = 0; k < current.Count; k++ ) {
                if ( k < last.Count && ( last[k] == current[k] ) ) {
                    continue;
                }
                SendWorldBlock( k, current[k] );
            }
            last = current;
            current.Clear();
            Done = true;
        }

        public void Render( string text, Block t ) {
            byte temp = textType;
            textType = ( byte )t;
            Render( text );
            textType = temp;
        }

        //gets the next sentence
        public void PickNewMessage() {
            if ( Messages.Any() ) {
                FeedData.AddMessages();
                if ( MessageCount == FeedData.Messages.Count() - 1 ) {
                    MessageCount = 0;
                    Sentence = FeedData.Messages[MessageCount];
                    return;
                }
                MessageCount++;
                Sentence = FeedData.Messages[MessageCount];
            }
        }

        //processess one blockupdate
        public void SendWorldBlock( int index, byte type ) {
            if ( world.Map == null ) {
                started = false;
                return;
            }
            if ( world.IsLoaded ) {
                int x = 0, y = 0, z = 0;
                switch ( direction ) {
                    case Direction.one:
                        x = ( int )( index / 8 ) + EndPos.X;
                        y = StartPos.Y;
                        z = StartPos.Z + ( index % 8 );
                        if ( world.map.InBounds( x, y, z ) ) {
                            if ( x >= StartPos.X && x <= FinishPos.X ) {
                                if ( ( Block )type != Block.Air ) {
                                    var Pos = new Vector3I( x, y, z );
                                    world.map.QueueUpdate( new BlockUpdate( null, Pos, ( Block )type ) );
                                    Blocks.TryAdd( Pos.ToString(), Pos );
                                }
                            }
                        }
                        break;

                    case Direction.two:
                        x = ( short )( EndPos.X - ( index / 8 ) );
                        y = ( short )StartPos.Y;
                        z = ( short )( StartPos.Z + ( index % 8 ) );
                        if ( world.map.InBounds( x, y, z ) ) {
                            if ( x <= StartPos.X && x >= FinishPos.X ) {
                                if ( ( Block )type != Block.Air ) {
                                    var Pos = new Vector3I( x, y, z );
                                    world.map.QueueUpdate( new BlockUpdate( null, Pos, ( Block )type ) );
                                    Blocks.TryAdd( Pos.ToString(), Pos );
                                }
                            }
                        }
                        break;

                    case Direction.three:
                        x = ( short )StartPos.X;
                        y = ( short )( EndPos.Y + ( index / 8 ) );
                        z = ( short )( StartPos.Z + ( index % 8 ) );
                        if ( world.map.InBounds( x, y, z ) ) {
                            if ( y >= StartPos.Y && y <= FinishPos.Y ) {
                                if ( ( Block )type != Block.Air ) {
                                    var Pos = new Vector3I( x, y, z );
                                    world.map.QueueUpdate( new BlockUpdate( null, Pos, ( Block )type ) );
                                    Blocks.TryAdd( Pos.ToString(), Pos );
                                }
                            }
                        }
                        break;

                    case Direction.four:
                        x = ( short )StartPos.X;
                        y = ( short )( EndPos.Y - ( index / 8 ) );
                        z = ( short )( StartPos.Z + ( index % 8 ) );
                        if ( world.map.InBounds( x, y, z ) ) {
                            if ( y <= StartPos.Y && y >= FinishPos.Y ) {
                                if ( ( Block )type != Block.Air ) {
                                    var Pos = new Vector3I( x, y, z );
                                    world.map.QueueUpdate( new BlockUpdate( null, Pos, ( Block )type ) );
                                    Blocks.TryAdd( Pos.ToString(), Pos );
                                }
                            }
                        }
                        break;
                }
            }
        }
    }
}