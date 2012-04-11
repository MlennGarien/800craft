// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> Next file format that fCraft shall use. </summary>
    public sealed class MapFCMv4 : IMapConverter {
        public const int FormatID = 0x00FC0004;
        const string ZoneMetaGroupName = "fCraft.Zones",
                     BlockLayerName = "Blocks";


        /// <summary> Returns name(s) of the server(s) that uses this format. </summary>
        public string ServerName {
            get { return "fCraft"; }
        }


        /// <summary> Returns the format type (file-based or directory-based). </summary>
        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }


        /// <summary> Returns the format name. </summary>
        public MapFormat Format {
            get { return MapFormat.FCMv4; }
        }


        /// <summary> Returns true if the filename (or directory name) matches this format's expectations. </summary>
        public bool ClaimsName( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return fileName.EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase );
        }


        /// <summary> Allows validating the map format while using minimal resources. </summary>
        /// <returns> Returns true if specified file/directory is valid for this format. </returns>
        public bool Claims( [NotNull] string path ) {
            if( path == null ) throw new ArgumentNullException( "path" );
            using( FileStream fs = File.OpenRead( path ) ) {
                BinaryReader reader = new BinaryReader( fs );
                return (reader.ReadInt32() == FormatID);
            }
        }


        /// <summary> Attempts to load map dimensions from specified location. </summary>
        /// <returns> Map object on success, or null on failure. </returns>
        public Map LoadHeader( string path ) {
            if( path == null ) throw new ArgumentNullException( "path" );
            using( FileStream fs = File.OpenRead( path ) ) {
                return LoadInternal( fs, false );
            }
        }


        /// <summary> Fully loads map from specified location. </summary>
        /// <returns> Map object on success, or null on failure. </returns>
        public Map Load( [NotNull] string path ) {
            if( path == null ) throw new ArgumentNullException( "path" );
            using( FileStream fs = File.OpenRead( path ) ) {
                return LoadInternal( fs, true );
            }
        }


        /// <summary> Saves given map at the given location. </summary>
        /// <returns> true if saving succeeded. </returns>
        public bool Save( [NotNull] Map map, [NotNull] string path ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            if( path == null ) throw new ArgumentNullException( "path" );
            using( FileStream mapStream = File.Create( path ) ) {
                BinaryWriter writer = new BinaryWriter( mapStream );

                writer.Write( FormatID );

                // write out map dimensions
                writer.Write( map.Width );
                writer.Write( map.Height );
                writer.Write( map.Length );

                // write out the spawn
                Position spawn = map.Spawn;
                writer.Write( (int)spawn.X );
                writer.Write( (int)spawn.Z );
                writer.Write( (int)spawn.Y );
                writer.Write( spawn.R );
                writer.Write( spawn.L );

                // write out creation and modification time
                long createdTime = map.DateCreated.ToUnixTime();
                writer.Write( createdTime );
                map.DateModified = DateTime.UtcNow;
                long modifiedTime = map.DateModified.ToUnixTime();
                writer.Write( modifiedTime );

                // write out metadata
                lock( map.Metadata.SyncRoot ) {
                    lock( map.Zones.SyncRoot ) {
                        int metaCount = map.Metadata.Count;
                        metaCount += map.Zones.Count;

                        // TODO: count rank spawns

                        writer.Write( metaCount );

                        // write out metadata
                        foreach( var entry in map.Metadata ) {
                            WriteString( writer, entry.Group );
                            WriteString( writer, entry.Key );
                            WriteString( writer, entry.Value );
                        }

                        // write out zones
                        foreach( var zone in map.Zones ) {
                            WriteString( writer, ZoneMetaGroupName );
                            WriteString( writer, zone.Name );
                            WriteString( writer, zone.Serialize().ToString( SaveOptions.DisableFormatting ) );
                        }

                        // TODO: write out rank spawns
                    }
                }

                // write out the layer(s)
                writer.Write( 1 ); // layer count
                WriteString( writer, BlockLayerName );
                writer.Write( map.Volume );
                map.GetCompressedCopy( mapStream, true );

                return true;
            }
        }


        static Map LoadInternal( [NotNull] Stream stream, bool readLayers ) {
            if( stream == null ) throw new ArgumentNullException( "stream" );
            BinaryReader bs = new BinaryReader( stream );

            // headers
            if( bs.ReadInt32() != FormatID ) {
                throw new MapFormatException( "MapFCMv4: Invalid format ID." );
            }

            // map dimensions
            int width = bs.ReadInt32();
            int height = bs.ReadInt32();
            int length = bs.ReadInt32();

            // ReSharper disable UseObjectOrCollectionInitializer
            Map map = new Map( null, width, length, height, false );
            // ReSharper restore UseObjectOrCollectionInitializer

            // spawn
            map.Spawn = new Position {
                X = (short)bs.ReadInt32(),
                Z = (short)bs.ReadInt32(),
                Y = (short)bs.ReadInt32(),
                R = bs.ReadByte(),
                L = bs.ReadByte()
            };

            // creation/modification dates
            long createdTime = bs.ReadInt64();
            map.DateCreated = createdTime.ToDateTime();
            long modifiedTime = bs.ReadInt64();
            map.DateModified = modifiedTime.ToDateTime();

            int metaEntryCount = bs.ReadInt32();
            if( metaEntryCount < 0 ) throw new MapFormatException( "Negative metadata entry count." );

            // metadata
            for( int i = 0; i < metaEntryCount; i++ ) {
                string groupName = ReadString( bs );
                string keyName = ReadString( bs );
                string value = ReadString( bs );

                // check for duplicate keys
                string oldValue;
                if( map.Metadata.TryGetValue( groupName, keyName, out oldValue ) ) {
                    Logger.Log( LogType.Warning,
                                "MapFCMv4: Duplicate metadata entry \"{0}.{1}\". " +
                                "Old value: \"{2}\", new value \"{3}\"", 
                                groupName, keyName, oldValue, value );
                }

                // parse or store metadata
                switch( groupName ) {
                    case ZoneMetaGroupName:
                        try {
                            Zone newZone = new Zone( XElement.Parse( value ) );
                            map.Zones.Add( newZone );
                        } catch( Exception ex ) {
                            Logger.Log( LogType.Error,
                                        "MapFCMv4: Error importing zone definition: {0}",
                                        ex );
                        }
                        break;

                    default:
                        map.Metadata[groupName, keyName] = value;
                        break;
                }
            }

            int layerCount = bs.ReadInt32();
            if( layerCount < 0 ) throw new MapFormatException( "Negative layer count." );

            // layers
            if( readLayers ) {
                for( int l = 0; l < layerCount; l++ ) {
                    string layerName = ReadString( bs );
                    int layerSize = bs.ReadInt32();
                    if( layerSize < 0 ) throw new MapFormatException( "Invalid layer size." );

                    switch( layerName ) {
                        case BlockLayerName:
                            //long blockStart = stream.Position;
                            map.Blocks = new byte[map.Volume];
                            using( GZipStream gs = new GZipStream( stream, CompressionMode.Decompress ) ) {
                                gs.Read( map.Blocks, 0, 4 ); // skip the 4-byte header
                                gs.Read( map.Blocks, 0, layerSize );
                            }
                            // TODO: get a cached compressed copy
                            //int blockSize = (int)(stream.Position - blockStart);
                            //stream.Seek( blockStart, SeekOrigin.Begin );
                            //map.CachedCompressedMap = new byte[blockSize];
                            //stream.Read( map.CachedCompressedMap, 0, blockSize );
                            break;

                        default:
                            if( layerSize > 0 ) {
                                byte[] layerData = new byte[layerSize];
                                using( GZipStream gs = new GZipStream( stream, CompressionMode.Decompress ) ) {
                                    gs.Read( layerData, 0, layerSize );
                                }
                            }
                            Logger.Log( LogType.Warning,
                                        "MapFCMv4: Unsupported layer \"{0}\" discarded.", layerName );
                            break;
                    }
                }
            }

            return map;
        }


        static string ReadString( [NotNull] BinaryReader reader ) {
            if( reader == null ) throw new ArgumentNullException( "reader" );
            int stringLength = reader.ReadInt32();
            if( stringLength < 0 ) throw new MapFormatException( "Negative string length." );
            return Encoding.ASCII.GetString( reader.ReadBytes( stringLength ) );
        }


        static void WriteString( [NotNull] BinaryWriter writer, [NotNull] string str ) {
            if( writer == null ) throw new ArgumentNullException( "writer" );
            if( str == null ) throw new ArgumentNullException( "str" );
            byte[] stringData = Encoding.ASCII.GetBytes( str );
            writer.Write( stringData.Length );
            writer.Write( stringData, 0, stringData.Length );
        }
    }
}