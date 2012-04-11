// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    public sealed class MapMyne : IMapConverter {

        const string BlockStoreFileName = "blocks.gz";
        const string MetaDataFileName = "world.meta";


        public string ServerName {
            get { return "Myne/MyneCraft/HyveBuild/iCraft"; }
        }


        public MapStorageType StorageType {
            get { return MapStorageType.Directory; }
        }


        public MapFormat Format {
            get { return MapFormat.Myne; }
        }


        public bool ClaimsName( [NotNull] string path ) {
            if( path == null ) throw new ArgumentNullException( "path" );
            return Directory.Exists( path ) &&
                   File.Exists( Path.Combine( path, BlockStoreFileName ) ) &&
                   File.Exists( Path.Combine( path, MetaDataFileName ) );
        }


        public bool Claims( [NotNull] string path ) {
            if( path == null ) throw new ArgumentNullException( "path" );
            return ClaimsName( path );
        }


        public Map LoadHeader( [NotNull] string path ) {
            if( path == null ) throw new ArgumentNullException( "path" );
            string fullMetaDataFileName = Path.Combine( path, MetaDataFileName );
            Map map;
            using( Stream metaStream = File.OpenRead( fullMetaDataFileName ) ) {
                map = LoadMeta( metaStream );
            }
            return map;
        }


        public Map Load( [NotNull] string path ) {
            if( path == null ) throw new ArgumentNullException( "path" );
            string fullBlockStoreFileName = Path.Combine( path, BlockStoreFileName );
            string fullMetaDataFileName = Path.Combine( path, MetaDataFileName );

            if( !File.Exists( fullBlockStoreFileName ) || !File.Exists( fullMetaDataFileName ) ) {
                throw new FileNotFoundException( "When loading myne maps, both .gz and .meta files are required." );
            }

            Map map;
            using( Stream metaStream = File.OpenRead( fullMetaDataFileName ) ) {
                map = LoadMeta( metaStream );
            }
            using( Stream dataStream = File.OpenRead( fullBlockStoreFileName ) ) {
                LoadBlocks( map, dataStream );
            }

            return map;
        }


        static void LoadBlocks( [NotNull] Map map, [NotNull] Stream mapStream ) {
            mapStream.Seek( 0, SeekOrigin.Begin );

            // Setup a GZipStream to decompress and read the map file
            GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true );
            BinaryReader bs = new BinaryReader( gs );

            int blockCount = IPAddress.HostToNetworkOrder( bs.ReadInt32() );
            if( blockCount != map.Volume ) {
                throw new Exception( "Map dimensions in the metadata do not match dimensions of the block array." );
            }

            map.Blocks = new byte[blockCount];
            bs.Read( map.Blocks, 0, map.Blocks.Length );
            map.RemoveUnknownBlocktypes();
        }


        static Map LoadMeta( [NotNull] Stream stream ) {
            if( stream == null ) throw new ArgumentNullException( "stream" );
            INIFile metaFile = new INIFile( stream );
            if( metaFile.IsEmpty ) {
                throw new Exception( "Metadata file is empty or incorrectly formatted." );
            }
            if( !metaFile.Contains( "size", "x", "y", "z" ) ) {
                throw new Exception( "Metadata file is missing map dimensions." );
            }

            int width = Int32.Parse( metaFile["size", "x"] );
            int length = Int32.Parse( metaFile["size", "z"] );
            int height = Int32.Parse( metaFile["size", "y"] );

            Map map = new Map( null, width, length, height, false );

            if( !map.ValidateHeader() ) {
                throw new MapFormatException( "One or more of the map dimensions are invalid." );
            }

            if( metaFile.Contains( "spawn", "x", "y", "z", "h" ) ) {
                map.Spawn = new Position {
                    X = (short)(Int16.Parse( metaFile["spawn", "x"] ) * 32 + 16),
                    Y = (short)(Int16.Parse( metaFile["spawn", "z"] ) * 32 + 16),
                    Z = (short)(Int16.Parse( metaFile["spawn", "y"] ) * 32 + 16),
                    R = Byte.Parse( metaFile["spawn", "h"] ),
                    L = 0
                };
            }
            return map;
        }


        public bool Save( [NotNull] Map mapToSave, [NotNull] string fileName ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            throw new NotImplementedException();
        }
    }
}