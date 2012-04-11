// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> fCraft map format converter, for obsolete format version #2 (2010). </summary>
    public sealed class MapFCMv2 : IMapConverter {
        public const uint Identifier = 0xfc000002;

        public string ServerName {
            get { return "fCraft"; }
        }


        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }


        public MapFormat Format {
            get { return MapFormat.FCMv2; }
        }


        public bool ClaimsName( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return fileName.EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase );
        }


        public bool Claims( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                using( FileStream mapStream = File.OpenRead( fileName ) ) {
                    BinaryReader reader = new BinaryReader( mapStream );
                    return (reader.ReadUInt32() == Identifier);
                }
            } catch( Exception ) {
                return false;
            }

        }


        public Map LoadHeader( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                return LoadHeaderInternal( mapStream );
            }
        }


        static Map LoadHeaderInternal( [NotNull] Stream stream ) {
            if( stream == null ) throw new ArgumentNullException( "stream" );
            BinaryReader reader = new BinaryReader( stream );

            // Read in the magic number
            if( reader.ReadUInt32() != Identifier ) {
                throw new MapFormatException();
            }

            // Read in the map dimesions
            int width = reader.ReadInt16();
            int length = reader.ReadInt16();
            int height = reader.ReadInt16();

            // ReSharper disable UseObjectOrCollectionInitializer
            Map map = new Map( null, width, length, height, false );
            // ReSharper restore UseObjectOrCollectionInitializer

            // Read in the spawn location
            map.Spawn = new Position {
                X = reader.ReadInt16(),
                Y = reader.ReadInt16(),
                Z = reader.ReadInt16(),
                R = reader.ReadByte(),
                L = reader.ReadByte()
            };

            return map;
        }


        public Map Load( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {

                Map map = LoadHeaderInternal( mapStream );

                if( !map.ValidateHeader() ) {
                    throw new MapFormatException( "One or more of the map dimensions are invalid." );
                }

                BinaryReader reader = new BinaryReader( mapStream );

                // Read the metadata
                int metaSize = reader.ReadUInt16();

                for( int i = 0; i < metaSize; i++ ) {
                    string key = ReadLengthPrefixedString( reader );
                    string value = ReadLengthPrefixedString( reader );
                    if( key.StartsWith( "@zone", StringComparison.OrdinalIgnoreCase ) ) {
                        try {
                            map.Zones.Add( new Zone( value, map.World ) );
                        } catch( Exception ex ) {
                            Logger.Log( LogType.Error,
                                        "MapFCMv2.Load: Error importing zone definition: {0}", ex );
                        }
                    } else {
                        Logger.Log( LogType.Warning,
                                    "MapFCMv2.Load: Metadata discarded: \"{0}\"=\"{1}\"",
                                    key, value );
                    }
                }

                // Read in the map data
                map.Blocks = new Byte[map.Volume];
                using( GZipStream decompressor = new GZipStream( mapStream, CompressionMode.Decompress ) ) {
                    decompressor.Read( map.Blocks, 0, map.Blocks.Length );
                }

                map.RemoveUnknownBlocktypes();

                return map;
            }
        }


        public bool Save( [NotNull] Map mapToSave, [NotNull] string fileName ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            throw new NotImplementedException();
        }


        static string ReadLengthPrefixedString( [NotNull] BinaryReader reader ) {
            if( reader == null ) throw new ArgumentNullException( "reader" );
            int length = reader.ReadInt32();
            byte[] stringData = reader.ReadBytes( length );
            return Encoding.ASCII.GetString( stringData );
        }
    }
}